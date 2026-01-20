using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Field : MonoBehaviour
{
    private enum FieldState { Empty, Seed, Youth, Adult, Ready }
    private FieldState currentState = FieldState.Empty;

    [Header("Field Visuals (Generic)")]
    public Sprite emptySprite;  // 빈 흙
    public Sprite seedSprite;   // 씨앗 심긴 흙 (공통)
    public Sprite youthSprite;  // 청소년기 흙 (공통)
    public Sprite adultSprite;  // 성장기 흙 (공통)
    public Sprite readySprite;  // 완성 (기본값)
    public SpriteRenderer fieldImage;

    [Header("Plant Logic")]
    public Transform plantAnchor; 
    private GameObject plantInstance; 
    private ItemData plantedSeed;
    private float remainingTime;
    private Coroutine growCoroutine; 

    [Header("비료 상태")]
    public int fertilizerCount = 0; 
    private const float BASE_SPEED_MULTIPLIER = 1f;
    private const float FERTILIZER_EFFECT = 0.25f;

   private void Start()
{
    UpdateFieldVisual();
}
   public Sprite GetFieldSprite()
{
    // 1순위: 현재 눈에 보이는 스프라이트Renderer에서 직접 가져옴
    if (fieldImage != null && fieldImage.sprite != null)
    {
        return fieldImage.sprite;
    }
    
    // 2순위: (이미지가 아직 갱신 전일 때) 상태별 변수에서 가져옴
    switch (currentState)
    {
        case FieldState.Empty: return emptySprite;
        case FieldState.Seed:  return seedSprite;
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
        float totalGrowTime = plantedSeed.growTime;
        float t = totalGrowTime - duration;

        while (t < totalGrowTime)
        {
            t += Time.deltaTime * GetGrowthMultiplier();
            remainingTime = Mathf.Max(0, totalGrowTime - t);

            float progress = t / totalGrowTime;

            // 3등분 성장 로직
            if (progress < 0.33f) 
                SetState(FieldState.Seed);
            else if (progress < 0.66f) 
                SetState(FieldState.Youth);
            else 
                SetState(FieldState.Adult);

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
        case FieldState.Empty:
            fieldImage.sprite = emptySprite;
            break;
        case FieldState.Seed:
            fieldImage.sprite = seedSprite;
            break;
        case FieldState.Youth:
            fieldImage.sprite = youthSprite;
            break;
        case FieldState.Adult:
            fieldImage.sprite = adultSprite;
            break;
        case FieldState.Ready:
            if (plantedSeed != null && plantedSeed.readyFieldSprite != null)
            {
                fieldImage.sprite = plantedSeed.readyFieldSprite;
            }
            else
            {
                // 씨앗 데이터에 이미지가 없을 경우를 대비한 보험용
                fieldImage.sprite = readySprite; 
            }
            break;
    }
}

    public bool IsEmpty() => currentState == FieldState.Empty;
    public bool IsReady() => currentState == FieldState.Ready;

    private void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;

        if (currentState == FieldState.Empty)
        {
            UIManager.Instance.OpenSeedPopup(this);
        }
        else if (currentState == FieldState.Ready)
        {
            Harvest();
        }
        else 
        {
            UIManager.Instance.OpenFertilizerPopup(this);
        }
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

        QuestManager.Instance?.NotifyAction(QuestConditionType.PlantCrop, seed, 1);
    }

    public float GetGrowthMultiplier() => BASE_SPEED_MULTIPLIER + (fertilizerCount * FERTILIZER_EFFECT);

    public void ApplyFertilizer(int count)
    {
        float oldMultiplier = GetGrowthMultiplier(); 
        fertilizerCount += count;
        float newMultiplier = GetGrowthMultiplier();

        float actualRemainingGrowth = remainingTime * oldMultiplier;
        remainingTime = actualRemainingGrowth / newMultiplier;

        if (growCoroutine != null) StopCoroutine(growCoroutine);
        growCoroutine = StartCoroutine(GrowRoutine(remainingTime));

        QuestManager.Instance?.NotifyAction(QuestConditionType.UseFertilizer, null, count);
    }

    public void Harvest()
    {
        if (currentState != FieldState.Ready) return;

        int myTier = HarvestToolManager.Instance.currentToolTier;

        if (myTier == plantedSeed.requiredToolTier)
        {
            InventoryManager.Instance.AddItem(plantedSeed.harvestItem, 1);
            QuestManager.Instance?.NotifyAction(QuestConditionType.HarvestCrop, plantedSeed.harvestItem, 1);

            if (plantInstance != null) Destroy(plantInstance);
            plantedSeed = null;
            SetState(FieldState.Empty);
        }
        else
        {
            Debug.Log($"{plantedSeed.harvestToolName}(이)가 필요합니다!");
        }
    }
}