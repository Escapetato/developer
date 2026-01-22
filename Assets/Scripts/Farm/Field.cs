using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Field : MonoBehaviour
{
    // [수정] state를 currentState와 일치시킴
    public enum FieldState { Empty, Seed, Youth, Adult, Ready }
    private FieldState currentState = FieldState.Empty;

    [Header("Field Visuals (Generic)")]
    public Sprite emptySprite;
    public Sprite seedSprite;
    public Sprite youthSprite;
    public Sprite adultSprite;
    public Sprite readySprite;
    public SpriteRenderer fieldImage;

    [Header("Plant Logic")]
    public Transform plantAnchor;
    private GameObject plantInstance;
    private ItemData plantedSeed;
    
    // [수정] 시간 계산의 핵심이 되는 변수
    private float remainingTime; 
    private Coroutine growCoroutine;

    [Header("비료 상태")]
    public int fertilizerCount = 0;
    private const float BASE_SPEED_MULTIPLIER = 1f;
    private const float FFERTILIZER_EFFECT = 0.25f;

    private void Start()
    {
        UpdateFieldVisual();
    }

    // [추가/수정] FertilizerPopupUI에서 호출할 남은 시간 텍스트 함수
    public string GetRemainingTimeText()
    {
        // [수정] growthTimer 대신 실제 사용 중인 remainingTime을 사용
        int totalSeconds = Mathf.CeilToInt(remainingTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public Sprite GetFieldSprite()
    {
        if (fieldImage != null && fieldImage.sprite != null) return fieldImage.sprite;
        
        switch (currentState)
        {
            case FieldState.Empty: return emptySprite;
            case FieldState.Seed: return seedSprite;
            case FieldState.Youth: return youthSprite;
            case FieldState.Adult: return adultSprite;
            case FieldState.Ready:
                if (plantedSeed != null && plantedSeed.readyFieldSprite != null)
                    return plantedSeed.readyFieldSprite;
                return readySprite;
            default: return emptySprite;
        }
    }

    private IEnumerator GrowRoutine(float duration)
    {
        // 처음부터 시작하는 게 아니라 남은 시간(duration)부터 시작하도록 설정
        float totalGrowTime = plantedSeed.growTime;
        float elapsedWork = totalGrowTime - duration; 

        while (elapsedWork < totalGrowTime)
        {
            float multiplier = GetGrowthMultiplier();
            elapsedWork += Time.deltaTime * multiplier;
            
            // [중요] 실시간으로 남은 시간 변수 갱신 (UI에서 참조함)
            remainingTime = Mathf.Max(0, (totalGrowTime - elapsedWork) / multiplier);

            float progress = elapsedWork / totalGrowTime;

            if (progress < 0.33f) SetState(FieldState.Seed);
            else if (progress < 0.66f) SetState(FieldState.Youth);
            else SetState(FieldState.Adult);

            if (plantInstance != null)
            {
                float scale = Mathf.Lerp(0.3f, 1f, progress);
                plantInstance.transform.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        remainingTime = 0;
        SetState(FieldState.Ready);
    }

    private void SetState(FieldState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        UpdateFieldVisual();
    }

    private void UpdateFieldVisual()
    {
        if (fieldImage == null) return;
        switch (currentState)
        {
            case FieldState.Empty: fieldImage.sprite = emptySprite; break;
            case FieldState.Seed: fieldImage.sprite = seedSprite; break;
            case FieldState.Youth: fieldImage.sprite = youthSprite; break;
            case FieldState.Adult: fieldImage.sprite = adultSprite; break;
            case FieldState.Ready:
                fieldImage.sprite = (plantedSeed != null && plantedSeed.readyFieldSprite != null) 
                                    ? plantedSeed.readyFieldSprite : readySprite;
                break;
        }
    }

    public bool IsEmpty() => currentState == FieldState.Empty;
    public bool IsReady() => currentState == FieldState.Ready;

    private void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;

        if (currentState == FieldState.Empty) UIManager.Instance.OpenSeedPopup(this);
        else if (currentState == FieldState.Ready) Harvest();
        else UIManager.Instance.OpenFertilizerPopup(this);
    }

    public void Plant(ItemData seed)
    {
        plantedSeed = seed;
        remainingTime = seed.growTime;
        fertilizerCount = 0;
        SetState(FieldState.Seed);

        if (plantInstance != null) Destroy(plantInstance);
        plantInstance = Instantiate(seed.plantPrefab, plantAnchor.position, Quaternion.identity, plantAnchor);
        plantInstance.transform.localScale = Vector3.one * 0.3f;

        if (growCoroutine != null) StopCoroutine(growCoroutine);
        growCoroutine = StartCoroutine(GrowRoutine(remainingTime));
    }

    public float GetGrowthMultiplier() => BASE_SPEED_MULTIPLIER + (fertilizerCount * FFERTILIZER_EFFECT);

    public void ApplyFertilizer(int count)
    {
        fertilizerCount += count;
        // 비료 적용 시 루틴을 재시작하여 바뀐 배율을 즉시 적용
        if (growCoroutine != null) StopCoroutine(growCoroutine);
        growCoroutine = StartCoroutine(GrowRoutine(remainingTime));
    }

    public void Harvest()
    {
        if (currentState != FieldState.Ready) return;
        if (HarvestToolManager.Instance.currentToolTier == plantedSeed.requiredToolTier)
        {
            InventoryManager.Instance.AddItem(plantedSeed.harvestItem, 1);
            if (plantInstance != null) Destroy(plantInstance);
            plantedSeed = null;
            SetState(FieldState.Empty);
        }
    }
}