using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Field : MonoBehaviour
{
    public enum FieldState { Empty, Planted, Growing, Ready }
    public FieldState state = FieldState.Empty;
    public Sprite emptySprite;
    public Sprite plantedSprite;
    public Sprite growingSprite;
    public Sprite readySprite;

    public Transform plantAnchor;
    public SpriteRenderer fieldImage;
    private SeedData plantedSeed;
    private GameObject plantInstance;

    private void Start()
    {
        UpdateFieldVisual();
    }

    private void OnMouseDown()
    {
        if (state == FieldState.Empty)
        {
            FarmManager.Instance.OnFieldClicked(this);
            Debug.Log("빈 밭 클릭");
        }
        else if (state == FieldState.Ready)
        {
            FarmManager.Instance.OnFieldClicked(this);
            Debug.Log("수확 가능");
        }
        else
        {
            FarmManager.Instance.OnFieldClicked(this);
            Debug.Log("성장 중...");
        }
    }

    public void Plant(SeedData seed)
    {
        if (state != FieldState.Empty) return;
        plantedSeed = seed;
        state = FieldState.Planted;
        UpdateFieldVisual();

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
        UpdateFieldVisual();
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
        }
        state = FieldState.Ready;
        UpdateFieldVisual();
    }

    public void Harvest()
    {
        if (state != FieldState.Ready) return;
        Destroy(plantInstance);
        plantInstance = null;
        plantedSeed = null;
        state = FieldState.Empty;
        UpdateFieldVisual();
    }

    private void UpdateFieldVisual()
    {
        if (fieldImage == null) return;

        switch (state)
        {
            case FieldState.Empty:
                fieldImage.sprite = emptySprite; break;
            case FieldState.Planted:
                fieldImage.sprite = plantedSprite; break;
            case FieldState.Growing:
                fieldImage.sprite = growingSprite; break;
            case FieldState.Ready:
                fieldImage.sprite = readySprite; break;
        }
    }
    public bool IsEmpty() => state == FieldState.Empty;
    public bool IsReady() => state == FieldState.Ready;
    
}