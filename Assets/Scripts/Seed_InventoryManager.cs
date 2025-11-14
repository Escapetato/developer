using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Seed_InventoryManager : MonoBehaviour
{
    public static Seed_InventoryManager Instance { get; private set; }
    private Dictionary<SeedData, int> seedCounts = new Dictionary<SeedData, int>();
    public SeedData star;

    private void Start()
    {
        if (star != null)
        {
            AddSeed(star, 5);
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public void AddSeed(SeedData seed, int amount)
    {
        if (!seedCounts.ContainsKey(seed)) seedCounts[seed] = 0;
        seedCounts[seed] += amount;
    }
    public bool HasSeed(SeedData seed)
    {
        return seedCounts.ContainsKey(seed) && seedCounts[seed] > 0;
    }

    public bool UseSeed(SeedData seed)
    {
        if (!HasSeed(seed)) return false;
        seedCounts[seed]--;

        if (seedCounts[seed] <= 0)
        {
            seedCounts.Remove(seed);
        }
        return true; 
    }

    public int GetSeedCount(SeedData seed)
    {
        return seedCounts.ContainsKey(seed) ? seedCounts[seed] : 0;
    }

    public Dictionary<SeedData, int> GetAllSeeds()
    {
        return new Dictionary<SeedData, int>(seedCounts);
    }
}
