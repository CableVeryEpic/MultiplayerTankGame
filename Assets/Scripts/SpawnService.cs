using System;
using System.Collections.Generic;
using UnityEngine;

public class SpawnService : MonoBehaviour
{
    public static SpawnService Instance { get; private set; }

    [Header("Clear Check")]
    [SerializeField] private int maxAttempts = 32;

    private readonly List<SpawnPoint> points = new List<SpawnPoint>();
    private System.Random rng;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Initialize(int seed)
    {
        rng = new System.Random(seed);
        Refresh();
    }

    public void Refresh()
    {
        points.Clear();
        points.AddRange(FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None));
        points.Sort((a, b) => a.Index.CompareTo(b.Index));
    }

    public bool TryGetSpawn(out SpawnPoint chosen)
    {
        chosen = null;
        if (points.Count == 0) return false;

        float now = Time.time;
        int attempts = 0;
        while (chosen == null && attempts++ < maxAttempts)
        {
            int idx = rng != null ? rng.Next(0, points.Count) : UnityEngine.Random.Range(0, points.Count);
            SpawnPoint sp = points[idx];

            if (!sp.OffCooldown(now) || !sp.IsClear()) continue;

            chosen = sp;
            sp.MarkUsed(now);
            return true;
        }
        return false;
    }
}
