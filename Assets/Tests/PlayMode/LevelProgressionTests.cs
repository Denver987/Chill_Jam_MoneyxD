using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using NewInput = UnityEngine.InputSystem.InputSystem;

/// <summary>
/// Plays the real gameplay scene and drives the level-up / card / XP loop
/// through the actual game code. The game scripts live in Assembly-CSharp and
/// have no asmdef, so they are reached through reflection.
/// </summary>
public class LevelProgressionTests
{
    const string ScenePath = "Assets/PlayableScenes/Gameplay.unity";

    // ---------------------------------------------------------------- helpers

    static Type FindType(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(name, false);
            if (type != null) return type;
        }
        return null;
    }

    static object Field(object target, string name)
    {
        Assert.IsNotNull(target, "target for field " + name);
        var field = target.GetType().GetField(name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, "field " + name + " on " + target.GetType().Name);
        return field.GetValue(target);
    }

    static void SetField(object target, string name, object value)
    {
        Assert.IsNotNull(target, "target for field " + name);
        var field = target.GetType().GetField(name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, "field " + name + " on " + target.GetType().Name);
        field.SetValue(target, value);
    }

    static object Call(object target, string name, params object[] args)
    {
        Assert.IsNotNull(target, "target for method " + name);
        var method = target.GetType().GetMethod(name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method, "method " + name + " on " + target.GetType().Name);
        return method.Invoke(target, args);
    }

    static Component Find(string typeName)
    {
        var type = FindType(typeName);
        Assert.IsNotNull(type, "type " + typeName + " not found");
        return UnityEngine.Object.FindAnyObjectByType(type) as Component;
    }

    static Component FindOn(GameObject go, string typeName)
    {
        var type = FindType(typeName);
        Assert.IsNotNull(type, "type " + typeName + " not found");
        return go.GetComponent(type) as Component;
    }

    static IEnumerator LoadGameplay()
    {
        int index = SceneUtility.GetBuildIndexByScenePath(ScenePath);
        Assert.GreaterOrEqual(index, 0, "Gameplay scene is in build settings");
        yield return SceneManager.LoadSceneAsync(index);
        yield return null;
        yield return null;
    }

    // Realtime wait: card selection pauses the game (timeScale = 0) and
    // WaitForSeconds would never complete.
    static IEnumerator WaitRealtime(float seconds)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < end) yield return null;
    }

    static void DisableSpawning()
    {
        var spawner = Find("EnemySpawner");
        if (spawner != null) spawner.gameObject.SetActive(false);
        var wave = Find("WaveManager");
        if (wave is Behaviour behaviour) behaviour.enabled = false;
    }

    static int LevelOf(object player) { return (int)Field(player, "level"); }

    static int PendingOf(object player) { return (int)Field(player, "pendingLevelUps"); }

    /// <summary>XP required to gain exactly <paramref name="levels"/> levels.</summary>
    static float XpForLevels(object player, int levels)
    {
        float threshold = (float)Field(player, "experienceToNextLevel");
        float multiplier = (float)Field(player, "xpGainMultiplier");
        float total = 0f;
        for (int i = 0; i < levels; i++)
        {
            total += threshold;
            threshold = Mathf.RoundToInt(threshold * multiplier);
        }
        return total;
    }

    static void GrantLevels(object player, int levels)
    {
        float needed = XpForLevels(player, levels) - (float)Field(player, "experience");
        Call(player, "addExperience", Mathf.CeilToInt(needed));
    }

    static GameObject CardsPanel()
    {
        var canvas = Find("CanvasSingleton");
        Assert.IsNotNull(canvas, "CanvasSingleton in scene");
        return (GameObject)Field(canvas, "CardsSelection");
    }

    static List<Component> ActiveCards()
    {
        var result = new List<Component>();
        var panel = CardsPanel();
        if (panel == null) return result;

        var spawnerType = FindType("CardsSpawner");
        var spawner = panel.GetComponentInChildren(spawnerType, true);
        if (spawner == null) return result;

        var cards = (IList)Field(spawner, "cards");
        foreach (var entry in cards)
        {
            var go = entry as GameObject;
            if (go != null && go.activeInHierarchy)
            {
                var card = go.GetComponent(FindType("Cards"));
                if (card != null) result.Add(card);
            }
        }
        return result;
    }

    static Component PickFirstCard()
    {
        var cards = ActiveCards();
        Assert.Greater(cards.Count, 0, "at least one card is offered");
        var card = cards[0];
        Call(card, "CardChoose");
        return card;
    }

    static int CountOrbs()
    {
        var type = FindType("Xpballs");
        if (type == null) return 0;
        int count = 0;
        foreach (var obj in Resources.FindObjectsOfTypeAll(type))
        {
            var component = obj as Component;
            if (component != null && component.gameObject.scene.IsValid()
                && component.gameObject.activeInHierarchy)
            {
                count++;
            }
        }
        return count;
    }

    static PropertyInfo leaderboardLogging;
    static bool previousLeaderboardLogging;
    Mouse testMouse;
    Keyboard testKeyboard;
    InputSettings.EditorInputBehaviorInPlayMode? previousEditorInputBehavior;
    InputSettings.BackgroundBehavior? previousBackgroundBehavior;

    // Silence only the optional network service. Gameplay errors must still
    // fail the test; never use LogAssert.ignoreFailingMessages here.
    static void IgnoreThirdPartyNetworkNoise()
    {
        // Keep this fixture-scoped: outstanding authorization requests can
        // finish between a test's TearDown and the next test's first frame.
        if (leaderboardLogging != null) return;
        leaderboardLogging = FindType("Dan.Main.LeaderboardCreator")?
            .GetProperty("LoggingEnabled", BindingFlags.Public | BindingFlags.Static);
        if (leaderboardLogging == null) return;
        previousLeaderboardLogging = (bool)leaderboardLogging.GetValue(null);
        leaderboardLogging.SetValue(null, false);
    }

    [OneTimeTearDown]
    public void RestoreLeaderboardLogging()
    {
        if (leaderboardLogging == null) return;
        // These service instances were started by this PlayMode test session.
        // Stop retries before restoring logging, so late responses cannot
        // leak into the runner's teardown frame.
        var type = FindType("Dan.Main.LeaderboardCreatorBehaviour");
        if (type != null)
        {
            foreach (var component in UnityEngine.Object.FindObjectsByType(type, FindObjectsSortMode.None))
                if (component is MonoBehaviour service) service.StopAllCoroutines();
        }
        leaderboardLogging.SetValue(null, previousLeaderboardLogging);
        leaderboardLogging = null;
    }

    [TearDown]
    public void TearDown()
    {
        if (testMouse != null) NewInput.RemoveDevice(testMouse);
        if (testKeyboard != null) NewInput.RemoveDevice(testKeyboard);
        if (previousEditorInputBehavior.HasValue)
        {
            NewInput.settings.editorInputBehaviorInPlayMode = previousEditorInputBehavior.Value;
            previousEditorInputBehavior = null;
        }
        if (previousBackgroundBehavior.HasValue)
        {
            NewInput.settings.backgroundBehavior = previousBackgroundBehavior.Value;
            previousBackgroundBehavior = null;
        }
        // Never leave a frozen editor behind for the next test.
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ------------------------------------------------------------------ tests

    [UnityTest]
    public IEnumerator SpellAimStaysOnCrosshairWithLowResolutionRenderTexture()
    {
        IgnoreThirdPartyNetworkNoise();
        yield return LoadGameplay();
        DisableSpawning();
        var player = Find("Player");
        var cameraObject = new GameObject("Aim test camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.enabled = false;
        camera.transform.SetPositionAndRotation(new Vector3(3, 4, 5), Quaternion.Euler(12, 37, 0));
        var texture = new RenderTexture(160, 90, 24);
        camera.targetTexture = texture;
        try
        {
            foreach (var type in new[] { "Knockback", "MagneticPull" })
            {
                var spell = FindOn(player.gameObject, type);
                var previousCamera = Field(spell, "cam");
                try
                {
                    SetField(spell, "cam", camera);
                    var ray = (Ray)Call(spell, "GetAimRay");
                    Assert.Greater(Vector3.Dot(ray.direction, camera.transform.forward), 0.99999f,
                        type + " aims through the center of its render texture, independent of screen size");
                }
                finally
                {
                    SetField(spell, "cam", previousCamera);
                }
            }
        }
        finally
        {
            camera.targetTexture = null;
            UnityEngine.Object.Destroy(cameraObject);
            UnityEngine.Object.Destroy(texture);
        }
    }

    [UnityTest]
    public IEnumerator MouseInputCastsAndSwitchesSpells()
    {
        IgnoreThirdPartyNetworkNoise();
        yield return LoadGameplay();
        DisableSpawning();
        var player = Find("Player");
        var input = player.GetComponent<PlayerInput>();
        previousEditorInputBehavior = NewInput.settings.editorInputBehaviorInPlayMode;
        NewInput.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        previousBackgroundBehavior = NewInput.settings.backgroundBehavior;
        NewInput.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        testMouse = NewInput.AddDevice<Mouse>();
        testKeyboard = NewInput.AddDevice<Keyboard>();
        input.SwitchCurrentControlScheme("Keyboard&Mouse", testKeyboard, testMouse);
        input.ActivateInput();
        input.SwitchCurrentActionMap("Player");
        yield return null; // Let device pairing and binding resolution settle.
        var shock = FindOn(player.gameObject, "Knockback");
        var pull = FindOn(player.gameObject, "MagneticPull");
        var selector = FindOn(player.gameObject, "SpellSelector");

        Assert.Greater(Time.timeScale, 0f, "casting test starts unpaused");
        Assert.IsTrue(((Behaviour)shock).isActiveAndEnabled, "initial Shockwave selection enables its component");

        int started = 0;
        int performed = 0;
        var attack = input.actions.FindAction("Player/Attack", true);
        Assert.IsTrue(attack.enabled, "Player action map remains enabled while Editor is unfocused");
        Assert.IsTrue(attack.controls.Contains(testMouse.leftButton), "attack is bound to the test mouse");
        attack.started += _ => started++;
        attack.performed += _ => performed++;
        NewInput.QueueStateEvent(testMouse, new MouseState().WithButton(MouseButton.Left));
        NewInput.Update();
        Assert.IsTrue((bool)Field(shock, "isCharging"),
            "left mouse starts shockwave charge; started=" + started + " performed=" + performed
            + " enabled=" + ((Behaviour)shock).isActiveAndEnabled + " scale=" + Time.timeScale
            + " selected=" + Field(selector, "currentSpell") + " map=" + input.currentActionMap?.name
            + " mouse=" + testMouse.leftButton.isPressed + " focus=" + Application.isFocused);
        NewInput.QueueStateEvent(testMouse, new MouseState());
        NewInput.Update();
        Assert.IsFalse((bool)Field(shock, "isCharging"), "left mouse release casts shockwave");

        SetField(selector, "magneticPullEnabled", true);
        NewInput.QueueStateEvent(testKeyboard, new KeyboardState(Key.Digit2));
        NewInput.Update();
        Assert.AreEqual("MagneticPull", Field(selector, "currentSpell").ToString());
        Assert.IsTrue(((Behaviour)pull).enabled, "selected spell is enabled");
        NewInput.QueueStateEvent(testKeyboard, new KeyboardState());
        NewInput.Update();
        NewInput.QueueStateEvent(testMouse, new MouseState().WithButton(MouseButton.Right));
        NewInput.Update();
        Assert.IsTrue((bool)Field(pull, "isHolding"), "right mouse starts magnetic pull");
        NewInput.QueueStateEvent(testMouse, new MouseState());
        NewInput.Update();
        Assert.IsFalse((bool)Field(pull, "isHolding"), "right mouse release cancels holding");

        NewInput.QueueStateEvent(testKeyboard, new KeyboardState(Key.Digit1));
        NewInput.Update();
        Assert.AreEqual("Shockwave", Field(selector, "currentSpell").ToString());
        Assert.IsTrue(((Behaviour)shock).enabled, "switching back restores shockwave");
    }

    [UnityTest]
    public IEnumerator GameplaySceneStartsUnpaused()
    {
        IgnoreThirdPartyNetworkNoise();
        yield return LoadGameplay();

        Assert.AreEqual(1f, Time.timeScale, "a freshly loaded scene is running");
        Assert.IsNotNull(Find("Player"), "Player exists");
        Assert.IsNotNull(Find("GameManager"), "GameManager exists");
    }

    [UnityTest]
    public IEnumerator LevelUpPausesThenResumesAfterPickingACard()
    {
        IgnoreThirdPartyNetworkNoise();
        yield return LoadGameplay();
        DisableSpawning();

        var player = Find("Player");
        var panel = CardsPanel();

        // Enough rounds to walk past the level where the old build stalled.
        for (int round = 0; round < 8; round++)
        {
            int before = LevelOf(player);
            GrantLevels(player, 1);
            yield return WaitRealtime(0.7f);

            Assert.AreEqual(before + 1, LevelOf(player), "level advanced (round " + round + ")");
            Assert.AreEqual(0f, Time.timeScale, "game paused while choosing (round " + round + ")");
            Assert.IsTrue(panel.activeSelf, "card panel visible (round " + round + ")");
            Assert.Greater(ActiveCards().Count, 0, "cards drawn (round " + round + ")");

            PickFirstCard();
            yield return WaitRealtime(0.4f);

            Assert.AreEqual(1f, Time.timeScale, "game resumed after pick (round " + round + ")");
            Assert.IsFalse(panel.activeSelf, "card panel hidden after pick (round " + round + ")");
            Assert.AreEqual(0, PendingOf(player), "no pending level ups (round " + round + ")");
        }
    }

    [UnityTest]
    public IEnumerator MultipleLevelsFromOnePickQueueCardScreens()
    {
        IgnoreThirdPartyNetworkNoise();
        yield return LoadGameplay();
        DisableSpawning();

        var player = Find("Player");
        int before = LevelOf(player);

        GrantLevels(player, 3);
        yield return WaitRealtime(0.7f);

        Assert.AreEqual(before + 3, LevelOf(player), "gained exactly 3 levels");
        Assert.AreEqual(3, PendingOf(player), "three choices pending");
        Assert.AreEqual(0f, Time.timeScale, "paused for the first choice");

        PickFirstCard();
        yield return WaitRealtime(0.2f);
        Assert.AreEqual(2, PendingOf(player), "two choices left");
        Assert.AreEqual(0f, Time.timeScale, "still paused for the second choice");

        PickFirstCard();
        yield return WaitRealtime(0.2f);
        Assert.AreEqual(1, PendingOf(player), "one choice left");

        PickFirstCard();
        yield return WaitRealtime(0.2f);
        Assert.AreEqual(0, PendingOf(player), "queue drained");
        Assert.AreEqual(1f, Time.timeScale, "game resumed after the last pick");
    }

    [UnityTest]
    public IEnumerator AllCardsCappedStillLetsTheGameContinue()
    {
        IgnoreThirdPartyNetworkNoise();
        yield return LoadGameplay();
        DisableSpawning();

        var player = Find("Player");
        var panel = CardsPanel();

        // Open the panel once so the card components are awake, then cap them.
        GrantLevels(player, 1);
        yield return WaitRealtime(0.7f);
        PickFirstCard();
        yield return WaitRealtime(0.3f);

        var spawnerType = FindType("CardsSpawner");
        var spawner = panel.GetComponentInChildren(spawnerType, true);
        var cards = (IList)Field(spawner, "cards");
        foreach (var entry in cards)
        {
            var go = entry as GameObject;
            var card = go != null ? go.GetComponent(FindType("Cards")) : null;
            if (card == null) continue;
            SetField(card, "cardCapCounter", Field(card, "maxCardCaps"));
            SetField(card, "CardCap", true);
        }

        int before = LevelOf(player);
        GrantLevels(player, 1);
        yield return WaitRealtime(0.7f);

        Assert.AreEqual(before + 1, LevelOf(player), "level up happened with every card capped");
        Assert.AreEqual(0f, Time.timeScale, "paused with a fallback card");
        Assert.Greater(ActiveCards().Count, 0, "fallback card is offered");

        PickFirstCard();
        yield return WaitRealtime(0.4f);

        Assert.AreEqual(1f, Time.timeScale, "game resumed from the fallback card");
    }

    [UnityTest]
    public IEnumerator SecondSpellUnlocksAtLevelFive()
    {
        IgnoreThirdPartyNetworkNoise();
        yield return LoadGameplay();
        DisableSpawning();

        var panel = CardsPanel();
        var spawnerType = FindType("CardsSpawner");
        var spawner = panel.GetComponentInChildren(spawnerType, true);
        Assert.IsNotNull(spawner, "CardsSpawner in the HUD");

        // Touch the panel so the card components run their Awake.
        var player = Find("Player");
        GrantLevels(player, 1);
        yield return WaitRealtime(0.7f);
        PickFirstCard();
        yield return WaitRealtime(0.3f);

        Component magneticPullCard = null;
        Component decreaseCdCard = null;
        Component increaseRangeCard = null;
        var cards = (IList)Field(spawner, "cards");
        foreach (var entry in cards)
        {
            var go = entry as GameObject;
            var card = go != null ? go.GetComponent(FindType("Cards")) : null;
            if (card == null) continue;
            string t = Field(card, "cardType").ToString();
            if (t == "MagneticPull") magneticPullCard = card;
            else if (t == "DecreasePullCd") decreaseCdCard = card;
            else if (t == "IncreasePullRange") increaseRangeCard = card;
        }

        Assert.IsNotNull(magneticPullCard, "MagneticPull card exists in the HUD");
        Assert.IsNotNull(decreaseCdCard, "DecreasePullCd card exists in the HUD");
        Assert.IsNotNull(increaseRangeCard, "IncreasePullRange card exists in the HUD");

        Assert.IsFalse((bool)Call(magneticPullCard, "IsAvailable", 4),
            "magnetic pull is locked before level 5");
        Assert.IsTrue((bool)Call(magneticPullCard, "IsAvailable", 5),
            "magnetic pull is offered at level 5");

        // Pull upgrades require Magnetic Pull unlocked
        Assert.IsFalse((bool)Call(decreaseCdCard, "IsAvailable", 6),
            "DecreasePullCd is locked until Magnetic Pull is unlocked");
        Assert.IsFalse((bool)Call(increaseRangeCard, "IsAvailable", 6),
            "IncreasePullRange is locked until Magnetic Pull is unlocked");

        // Magnetic Pull guaranteed when level >= 5 and not unlocked
        SetField(player, "level", 5);
        Call(spawner, "IndexsForCards");
        var spawnIndexes = (IList)Field(spawner, "spawnIndexes");
        int pullIdx = -1;
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i] == ((Component)magneticPullCard).gameObject) { pullIdx = i; break; }
        }
        Assert.IsTrue(spawnIndexes.Contains(pullIdx), "MagneticPull card is guaranteed at level 5");

        // Unlock Magnetic Pull
        var selector = FindOn(player.gameObject, "SpellSelector");
        SetField(selector, "magneticPullEnabled", true);

        Assert.IsFalse((bool)Call(magneticPullCard, "IsAvailable", 5),
            "MagneticPull card is no longer offered once unlocked");
        Assert.IsTrue((bool)Call(decreaseCdCard, "IsAvailable", 6),
            "DecreasePullCd is offered at level 6 once Magnetic Pull is unlocked");
        Assert.IsTrue((bool)Call(increaseRangeCard, "IsAvailable", 6),
            "IncreasePullRange is offered at level 6 once Magnetic Pull is unlocked");
    }

    [UnityTest]
    public IEnumerator EnemyDeathDropsAnXpOrb()
    {
        IgnoreThirdPartyNetworkNoise();
        yield return LoadGameplay();

        var spawner = Find("EnemySpawner");
        Assert.IsNotNull(spawner, "EnemySpawner in scene");

        var prefabs = (IList)Field(spawner, "EnemyPrefabs");
        var zones = (IList)Field(spawner, "spawnzones");
        Assert.Greater(prefabs.Count, 0, "enemy prefabs configured");
        Assert.Greater(zones.Count, 0, "spawn zones configured");

        int before = CountOrbs();
        var player = Find("Player");
        int levelBefore = LevelOf(player);
        float xpBefore = (float)Field(player, "experience");

        var enemy = Call(spawner, "SpawnEnemyAt", prefabs[0], zones[0]) as Component;
        Assert.IsNotNull(enemy, "enemy spawned from the pool");
        yield return new WaitForSeconds(0.5f);

        var movement = FindOn(enemy.gameObject, "EnnemyMovement");
        Assert.IsNotNull(movement, "enemy has EnnemyMovement");
        Call(movement, "Die");

        yield return new WaitForSeconds(1.8f);

        // The orb either still exists on the ground or was already pulled in
        // and collected by the player; both prove the death XP path works.
        bool orbAlive = CountOrbs() > before;
        bool xpCollected = LevelOf(player) > levelBefore || (float)Field(player, "experience") != xpBefore;

        Assert.IsTrue(orbAlive || xpCollected, "an XP orb dropped on death (alive: " + orbAlive + ", collected: " + xpCollected + ")");
    }

    [UnityTest]
    public IEnumerator DamageStillWorksAfterLevelUps()
    {
        IgnoreThirdPartyNetworkNoise();
        yield return LoadGameplay();
        DisableSpawning();

        var player = Find("Player");
        int before = LevelOf(player);
        GrantLevels(player, 1);
        yield return WaitRealtime(0.7f);
        PickFirstCard();
        yield return WaitRealtime(0.4f);

        Assert.AreEqual(before + 1, LevelOf(player), "leveled up once");
        Assert.AreEqual(1f, Time.timeScale, "game running");

        int health = (int)Field(player, "health");
        int maxHealth = (int)Field(player, "maxHealth");

        // A stray enemy hit during scene load can leave i-frames running;
        // make the damage check deterministic.
        SetField(player, "invulnerable", false);
        SetField(player, "health", maxHealth);
        health = maxHealth;

        Call(player, "TakeDamage", 1, Vector3.forward, 0f);

        Assert.AreEqual(health - 1, (int)Field(player, "health"), "damage reduced health by 1");
    }

    [Test]
    public void MagneticPullTargetingEnemiesCannotBePulledOnlyTelekinesised()
    {
        var pullType = FindType("MagneticPull");
        Assert.IsNotNull(pullType, "MagneticPull type exists");

        var canPullMethod = pullType.GetMethod("CanPull", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(canPullMethod, "CanPull method exists");

        var canTelekinesisMethod = pullType.GetMethod("CanTelekinesis", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(canTelekinesisMethod, "CanTelekinesis method exists");

        // 1. Pullable Stone
        var stone = new GameObject("TestStone");
        stone.tag = "Pullable";
        var stoneRb = stone.AddComponent<Rigidbody>();

        bool stoneCanPull = (bool)canPullMethod.Invoke(null, new object[] { stoneRb });
        bool stoneCanTK = (bool)canTelekinesisMethod.Invoke(null, new object[] { stoneRb });

        Assert.IsTrue(stoneCanPull, "Pullable stone can be pulled");
        Assert.IsTrue(stoneCanTK, "Pullable stone can be telekinesised");

        // 2. Enemy (with EnnemyMovement) - even if tagged Pullable, must not be pullable
        var enemy = new GameObject("TestEnemy");
        enemy.tag = "Pullable";
        var enemyRb = enemy.AddComponent<Rigidbody>();
        enemy.AddComponent<UnityEngine.AI.NavMeshAgent>();
        var enemyMoveType = FindType("EnnemyMovement");
        var enemyMovement = enemy.AddComponent(enemyMoveType);

        bool enemyCanPull = (bool)canPullMethod.Invoke(null, new object[] { enemyRb });
        bool enemyCanTK = (bool)canTelekinesisMethod.Invoke(null, new object[] { enemyRb });

        Assert.IsFalse(enemyCanPull, "Enemies CANNOT be pulled into hands");
        Assert.IsTrue(enemyCanTK, "Live enemies CAN be telekinesised");

        // 3. Dead Enemy
        SetField(enemyMovement, "isDead", true);
        bool deadEnemyCanTK = (bool)canTelekinesisMethod.Invoke(null, new object[] { enemyRb });
        Assert.IsFalse(deadEnemyCanTK, "Dead enemies cannot be telekinesised");

        // Clean up
        UnityEngine.Object.DestroyImmediate(stone);
        UnityEngine.Object.DestroyImmediate(enemy);
    }

    [Test]
    public void PlayerDamageClampsKnockbackAndSetsInvulnerableImmediately()
    {
        var playerGo = new GameObject("TestPlayer");
        var playerType = FindType("Player");
        var inputType = FindType("InputSystem");

        var rb = playerGo.AddComponent<Rigidbody>();
        var inputSys = playerGo.AddComponent(inputType);
        var player = playerGo.AddComponent(playerType);

        SetField(player, "inputSystem", inputSys);
        SetField(player, "health", 100);
        SetField(player, "invulnerable", false);

        // Enemy hits with massive knockback force (50f)
        Call(player, "TakeDamage", 10, Vector3.forward, 50f);

        bool isInvuln = (bool)Field(player, "invulnerable");
        Assert.IsTrue(isInvuln, "Player is immediately invulnerable upon taking damage");

        // Rigidbody should not have 50 m/s velocity - it was clamped to balanced knockback
        Assert.LessOrEqual(rb.linearVelocity.magnitude, 10f, "Player linear velocity is clamped and safe");

        UnityEngine.Object.DestroyImmediate(playerGo);
    }

    [UnityTest]
    public IEnumerator CardSelectionGracePeriodAndSpawnAnimation()
    {
        IgnoreThirdPartyNetworkNoise();
        yield return LoadGameplay();
        DisableSpawning();

        var player = Find("Player");
        var panel = CardsPanel();
        var cg = panel.GetComponent<CanvasGroup>();

        GrantLevels(player, 1);
        yield return null; // 1 frame for CardsUI to open

        Assert.IsTrue(panel.activeSelf, "Card panel is active");
        if (cg != null)
        {
            Assert.IsFalse(cg.blocksRaycasts, "Raycasts are blocked during 0.4s grace period");
        }

        var activeCards = ActiveCards();
        Assert.Greater(activeCards.Count, 0, "Cards are spawned");
        var btn = activeCards[0].GetComponent<UnityEngine.UI.Button>();
        if (btn != null)
        {
            Assert.IsFalse(btn.interactable, "Card button is non-interactable during grace period");
        }

        yield return WaitRealtime(0.45f);

        Assert.AreEqual(0.45f, activeCards[0].transform.localScale.x, 0.02f,
            "Card scales to native 0.45f, not 1.0f");

        if (cg != null)
        {
            Assert.IsTrue(cg.blocksRaycasts, "Raycasts are unblocked after grace period expires");
        }
        if (btn != null)
        {
            Assert.IsTrue(btn.interactable, "Card button is interactable after grace period expires");
        }

        PickFirstCard();
        yield return WaitRealtime(0.2f);
        Assert.IsFalse(panel.activeSelf, "Card panel closed after pick");
        Assert.AreEqual(1f, Time.timeScale, "Time scale restored to 1");
    }
}
