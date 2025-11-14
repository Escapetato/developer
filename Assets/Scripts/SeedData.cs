using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Seed")]
public class SeedData : ScriptableObject
{
    public string seedName;
    public Sprite icon;
    public GameObject plantPrefab;
    public float growTime;
}
