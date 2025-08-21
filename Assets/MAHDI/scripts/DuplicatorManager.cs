// DuplicatorManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DuplicatorManager : MonoBehaviour
{
    public static DuplicatorManager Instance { get; private set; }
    [SerializeField] private Transform duplicatesParent;

    // track the prepared (inactive) clone for each source
    private readonly Dictionary<GameObject, GameObject> prepared = new();
    private readonly Dictionary<GameObject, Coroutine> running = new();

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// Prepare an INACTIVE clone immediately when the spell starts interacting.
    public static void PrepareClone(GameObject source, bool stripDuplicatorOnClone = true)
    {
        if (!Instance || !source) return;

        // If a clone already exists for this source, do nothing.
        if (Instance.prepared.ContainsKey(source)) return;

        var parent = Instance.duplicatesParent ? Instance.duplicatesParent : source.transform.parent;
        var clone = Instantiate(source, source.transform.position, source.transform.rotation, parent);

        // Optional: prevent chain-duplication on the clone itself
        if (stripDuplicatorOnClone && clone.TryGetComponent<DuplicateOnSpellInteract>(out var dup))
            Destroy(dup);

        // Make sure it's inactive until thrown + delay
        clone.SetActive(false);

        Instance.prepared[source] = clone;
    }

    /// Call this when the object is THROWN. Starts the countdown, then activates the prepared clone.
    public static void BeginActivationAfterThrow(GameObject source, float delaySeconds)
    {
        if (!Instance || !source) return;
        if (!Instance.prepared.TryGetValue(source, out var clone) || clone == null) return;

        // start a coroutine that waits for the ORIGINAL to be destroyed first
        if (Instance.running.TryGetValue(source, out var old) && old != null)
            Instance.StopCoroutine(old);

        Instance.running[source] = Instance.StartCoroutine(
            Instance.EnableAfterDestroyedThenDelay(source, clone, delaySeconds)
        );
    }


    private IEnumerator EnableAfterDestroyedThenDelay(GameObject source, GameObject clone, float delay)
    {
        // 1) wait until the original is actually destroyed
        yield return new WaitUntil(() => source == null);
        // ensure the destruction finishes at end-of-frame
        yield return new WaitForEndOfFrame();

        // 2) THEN start counting your activation delay
        yield return new WaitForSeconds(delay);

        // 3) activate the prepared clone
        if (clone)
        {
            if (clone.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = true;
            }
            clone.SetActive(true);
        }

        // cleanup
        prepared.Remove(source);
        running.Remove(source);
    }


    /// Optional helper (if you want to cancel when dropped instead of thrown)
    public static void CancelPrepared(GameObject source, bool destroyPrepared = true)
    {
        if (!Instance || !source) return;

        if (Instance.running.TryGetValue(source, out var co) && co != null)
            Instance.StopCoroutine(co);
        Instance.running.Remove(source);

        if (Instance.prepared.TryGetValue(source, out var clone))
        {
            if (destroyPrepared && clone) Destroy(clone);
            Instance.prepared.Remove(source);
        }
    }
}
