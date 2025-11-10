using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Field : MonoBehaviour
{
    public enum FieldState { Empty, Planted, Growing, Ready }
    public FieldState state = FieldState.Empty;

    public Transform plantAnchor;
    private SeedData plantedSeed;
    private GameObject plantInstance;

    private void OnMouseDown()
    {
        FarmManager.Instance.OnFieldClicked(this);
        Debug.Log("밭 클릭");
    }

    public void Plant(SeedData seed)
    {
        if (state != FieldState.Empty) return;
        plantedSeed = seed;
        state = FieldState.Planted;

        if (seed.plantPrefab != null && plantAnchor != null)
        {
            plantInstance = Instantiate(seed.plantPrefab, plantAnchor.position, Quaternion.identity, plantAnchor);
            plantInstance.transform.localScale = Vector3.one * 0.3f;
        }
        StartCoroutine(GrowRoutine(seed.growTime));
    }
    private IEnumerator GrowRoutine(float growTime)
    {
        state = FieldState.Growing;
        float t = 0;
        while (t < growTime)
        {
            t += Time.deltaTime;
            if (plantInstance != null)
            {
                float scale = Mathf.Lerp(0.3f, 1f, t / growTime);
                plantInstance.transform.localScale = Vector3.one * scale;
                yield return null;
            }
            state = FieldState.Ready;
        }
    }

    public void Harvest()
    {
        if (state != FieldState.Ready) return;
        Destroy(plantInstance);
        plantInstance = null;
        plantedSeed = null;
        state = FieldState.Empty;
    }
    public bool IsEmpty() => state == FieldState.Empty;
    public bool IsReady() => state == FieldState.Ready;
    
}