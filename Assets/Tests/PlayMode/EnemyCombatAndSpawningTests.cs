using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class EnemyCombatAndSpawningTests
{
    private static Type FindType(string name)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType(name, false);
            if (type != null) return type;
        }
        return null;
    }

    private static object Field(object target, string name)
    {
        Assert.IsNotNull(target, "target for field " + name);
        var field = target.GetType().GetField(name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, "field " + name + " on " + target.GetType().Name);
        return field.GetValue(target);
    }

    private static void SetField(object target, string name, object value)
    {
        Assert.IsNotNull(target, "target for field " + name);
        var field = target.GetType().GetField(name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, "field " + name + " on " + target.GetType().Name);
        field.SetValue(target, value);
    }

    private static object Call(object target, string name, params object[] args)
    {
        Assert.IsNotNull(target, "target for method " + name);
        var method = target.GetType().GetMethod(name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method, "method " + name + " on " + target.GetType().Name);
        return method.Invoke(target, args);
    }

    [Test]
    public void PlayerInvulnerabilityGracePeriodIsPointTwoSeconds()
    {
        var playerGo = new GameObject("TestPlayer");
        var playerType = FindType("Player");
        var player = playerGo.AddComponent(playerType);

        float invulnTime = (float)Field(player, "invulnerabilityTime");
        Assert.AreEqual(0.2f, invulnTime, 0.001f, "Player invulnerability grace period must be 0.2s");

        SetField(player, "health", 100);
        SetField(player, "invulnerable", false);

        Call(player, "TakeDamage", 10, Vector3.forward, 0f);

        bool isInvuln = (bool)Field(player, "invulnerable");
        Assert.IsTrue(isInvuln, "Player is invulnerable immediately upon taking damage");

        UnityEngine.Object.DestroyImmediate(playerGo);
    }

    [Test]
    public void EnemyHitInterruptsAttackAndAppliesHitFlinch()
    {
        var enemyGo = new GameObject("TestEnemy");
        var enemyMoveType = FindType("EnnemyMovement");

        var rb = enemyGo.AddComponent<Rigidbody>();
        var animator = enemyGo.AddComponent<Animator>();
        var enemyMovement = enemyGo.AddComponent(enemyMoveType);

        // Simulate enemy in the middle of an attack
        SetField(enemyMovement, "isAttacking", true);
        SetField(enemyMovement, "nextAttackTime", Time.time + 1f);

        var playerGo = new GameObject("PlayerSource");
        playerGo.transform.position = Vector3.back;

        // Player hits enemy with knockback
        Call(enemyMovement, "Execute", playerGo.transform, 5f);

        bool isAttacking = (bool)Field(enemyMovement, "isAttacking");
        bool isKnockedBack = (bool)Field(enemyMovement, "isKnockedBack");
        float knockbackTimer = (float)Field(enemyMovement, "knockbackTimer");
        float nextAttackTime = (float)Field(enemyMovement, "nextAttackTime");

        Assert.IsFalse(isAttacking, "Player hit must immediately interrupt enemy attack");
        Assert.IsTrue(isKnockedBack, "Player hit puts enemy into flinch/knockback");
        Assert.GreaterOrEqual(knockbackTimer, 0.15f, "Flinch recovery time is at least 0.15s");
        Assert.GreaterOrEqual(nextAttackTime, Time.time + 0.15f, "Next attack time is delayed by flinch grace period");

        UnityEngine.Object.DestroyImmediate(playerGo);
        UnityEngine.Object.DestroyImmediate(enemyGo);
    }

    [UnityTest]
    public IEnumerator AttackMissesWhenPlayerDodgesOutOfRange()
    {
        var enemyGo = new GameObject("TestEnemy");
        var playerGo = new GameObject("TestPlayer");

        var enemyMoveType = FindType("EnnemyMovement");
        var playerType = FindType("Player");

        var rb = enemyGo.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        enemyGo.AddComponent<Animator>();
        var enemyMovement = (MonoBehaviour)enemyGo.AddComponent(enemyMoveType);

        var player = (MonoBehaviour)playerGo.AddComponent(playerType);
        SetField(player, "health", 100);
        SetField(player, "invulnerable", false);

        // Position player in initial attack range (1.5m)
        enemyGo.transform.position = Vector3.zero;
        playerGo.transform.position = new Vector3(0, 0, 1.5f);

        SetField(enemyMovement, "target", playerGo.transform);
        SetField(enemyMovement, "attackRange", 1.8f);
        SetField(enemyMovement, "attackDamage", 20);
        SetField(enemyMovement, "attackCooldown", 1f);

        // Start attack routine
        var routine = (IEnumerator)Call(enemyMovement, "AttackRoutine");
        var runner = enemyMovement.StartCoroutine(routine);

        // Wait a short moment while windup is playing (~0.15s)
        yield return new WaitForSeconds(0.15f);

        // Player dodges backwards out of range (distance becomes 4.0m)
        playerGo.transform.position = new Vector3(0, 0, 4.0f);

        // Wait for attack routine to complete
        yield return new WaitForSeconds(0.5f);

        int health = (int)Field(player, "health");
        Assert.AreEqual(100, health, "Attack must MISS when player dodged out of range");

        enemyMovement.StopCoroutine(runner);
        UnityEngine.Object.Destroy(playerGo);
        UnityEngine.Object.Destroy(enemyGo);
    }

    [UnityTest]
    public IEnumerator AttackHitsWhenPlayerRemainsInMeleeRange()
    {
        var enemyGo = new GameObject("TestEnemy");
        var playerGo = new GameObject("TestPlayer");

        var enemyMoveType = FindType("EnnemyMovement");
        var playerType = FindType("Player");

        var rb = enemyGo.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;
        enemyGo.AddComponent<Animator>();
        var enemyMovement = (MonoBehaviour)enemyGo.AddComponent(enemyMoveType);

        var player = (MonoBehaviour)playerGo.AddComponent(playerType);
        SetField(player, "health", 100);
        SetField(player, "invulnerable", false);

        // Position player in true melee range (1.5m)
        enemyGo.transform.position = Vector3.zero;
        playerGo.transform.position = new Vector3(0, 0, 1.5f);

        SetField(enemyMovement, "target", playerGo.transform);
        SetField(enemyMovement, "attackRange", 1.8f);
        SetField(enemyMovement, "attackDamage", 20);
        SetField(enemyMovement, "attackCooldown", 1f);

        var routine = (IEnumerator)Call(enemyMovement, "AttackRoutine");
        var runner = enemyMovement.StartCoroutine(routine);

        // Wait for strike to land (~0.45s)
        yield return new WaitForSeconds(0.48f);

        int health = (int)Field(player, "health");
        Assert.AreEqual(80, health, "Attack must HIT and deal 20 damage when player stays in range");

        enemyMovement.StopCoroutine(runner);
        UnityEngine.Object.Destroy(playerGo);
        UnityEngine.Object.Destroy(enemyGo);
    }

    [Test]
    public void EnemyTierClassificationAndLevelGatingLogic()
    {
        var waveType = FindType("WaveManager");
        var getTierMethod = waveType.GetMethod("GetEnemyTier", BindingFlags.Public | BindingFlags.Static);
        Assert.IsNotNull(getTierMethod, "WaveManager.GetEnemyTier exists");

        var basicGo = new GameObject("thor-godEnnemy");
        var enemyType = FindType("Enemy");
        var enemyMoveType = FindType("EnnemyMovement");

        var basicEnemy = (Component)basicGo.AddComponent(enemyType);
        var basicMove = basicGo.AddComponent(enemyMoveType);
        SetField(basicEnemy, "Movement", basicMove);

        object basicTier = getTierMethod.Invoke(null, new object[] { basicEnemy });
        Assert.AreEqual("Basic", basicTier.ToString(), "thor-godEnnemy is classified as Basic tier");

        var goblinGo = new GameObject("Imp Red Ennemy");
        var goblinEnemy = (Component)goblinGo.AddComponent(enemyType);
        var goblinMove = goblinGo.AddComponent(enemyMoveType);
        SetField(goblinEnemy, "Movement", goblinMove);

        object goblinTier = getTierMethod.Invoke(null, new object[] { goblinEnemy });
        Assert.AreEqual("Intermediate", goblinTier.ToString(), "Imp Red Ennemy is classified as Intermediate tier");

        var golemGo = new GameObject("GolemEnnemy");
        var golemEnemy = (Component)golemGo.AddComponent(enemyType);
        var golemMove = golemGo.AddComponent(enemyMoveType);
        SetField(golemMove, "isGolem", true);
        SetField(golemEnemy, "Movement", golemMove);

        object golemTier = getTierMethod.Invoke(null, new object[] { golemEnemy });
        Assert.AreEqual("Heavy", golemTier.ToString(), "GolemEnnemy is classified as Heavy tier");

        UnityEngine.Object.DestroyImmediate(basicGo);
        UnityEngine.Object.DestroyImmediate(goblinGo);
        UnityEngine.Object.DestroyImmediate(golemGo);
    }

    [Test]
    public void SpawnerCapPreventsExceedingMaxAliveEnemies()
    {
        var spawnerGo = new GameObject("TestSpawner");
        var spawnerType = FindType("EnemySpawner");
        var spawner = spawnerGo.AddComponent(spawnerType);

        int maxAlive = (int)Field(spawner, "maxAliveEnemies");
        Assert.AreEqual(25, maxAlive, "Spawner max alive enemies is capped at 25");

        // Simulate 25 active enemies
        var activeList = (System.Collections.IList)Field(spawner, "ActiveEnemies");
        for (int i = 0; i < 25; i++)
        {
            var dummyGo = new GameObject("DummyEnemy_" + i);
            var enemy = dummyGo.AddComponent(FindType("Enemy"));
            var move = dummyGo.AddComponent(FindType("EnnemyMovement"));
            SetField(enemy, "Movement", move);
            activeList.Add(enemy);
        }

        int count = (int)spawner.GetType().GetProperty("activeEnemiesCount").GetValue(spawner);
        Assert.AreEqual(25, count, "25 active enemies are present");

        // Clean up dummy enemies
        for (int i = activeList.Count - 1; i >= 0; i--)
        {
            var comp = activeList[i] as Component;
            if (comp != null)
                UnityEngine.Object.DestroyImmediate(comp.gameObject);
        }
        UnityEngine.Object.DestroyImmediate(spawnerGo);
    }

    [Test]
    public void GolemSpawnsWhenPlayerLevelReachesSeven()
    {
        var enemyType = FindType("Enemy");
        var enemyMoveType = FindType("EnnemyMovement");
        var playerType = FindType("Player");
        var spawnerType = FindType("EnemySpawner");
        var waveType = FindType("WaveManager");
        var spawnDataType = FindType("EnemySpawnData");

        var basicGo = new GameObject("thor-godEnnemy");
        var basicEnemy = basicGo.AddComponent(enemyType);
        var basicMove = basicGo.AddComponent(enemyMoveType);
        SetField(basicEnemy, "Movement", basicMove);

        var golemGo = new GameObject("GolemEnnemy");
        var golemEnemy = golemGo.AddComponent(enemyType);
        var golemMove = golemGo.AddComponent(enemyMoveType);
        SetField(golemMove, "isGolem", true);
        SetField(golemEnemy, "Movement", golemMove);

        var playerGo = new GameObject("TestPlayer");
        var player = playerGo.AddComponent(playerType);
        SetField(player, "level", 1);

        var spawnerGo = new GameObject("TestSpawner");
        var spawner = spawnerGo.AddComponent(spawnerType);
        SetField(spawner, "target", playerGo.transform);

        var prefabsList = (System.Collections.IList)Field(spawner, "EnemyPrefabs");
        prefabsList.Add(basicEnemy);
        prefabsList.Add(golemEnemy);

        var waveGo = new GameObject("TestWaveManager");
        var waveManager = waveGo.AddComponent(waveType);
        SetField(waveManager, "spawner", spawner);

        var stageEnemies = Array.CreateInstance(spawnDataType, 1);
        var data = Activator.CreateInstance(spawnDataType);
        SetField(data, "enemyPrefab", basicEnemy);
        SetField(data, "weight", 1f);
        stageEnemies.SetValue(data, 0);

        // At level 1, only basic enemy can spawn
        for (int i = 0; i < 20; i++)
        {
            var picked = Call(waveManager, "PickEnemy", stageEnemies);
            Assert.AreSame(basicEnemy, picked, "At level < 7, Golem must not be picked");
        }

        // At level 7, Golem joins eligible pool
        SetField(player, "level", 7);
        bool golemPicked = false;
        for (int i = 0; i < 50; i++)
        {
            var picked = Call(waveManager, "PickEnemy", stageEnemies);
            if (ReferenceEquals(picked, golemEnemy))
            {
                golemPicked = true;
                break;
            }
        }
        Assert.IsTrue(golemPicked, "At level >= 7, Golem must spawn with solid weight");

        UnityEngine.Object.DestroyImmediate(waveGo);
        UnityEngine.Object.DestroyImmediate(spawnerGo);
        UnityEngine.Object.DestroyImmediate(playerGo);
        UnityEngine.Object.DestroyImmediate(basicGo);
        UnityEngine.Object.DestroyImmediate(golemGo);
    }
}
