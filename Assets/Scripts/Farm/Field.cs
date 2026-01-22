using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Field : MonoBehaviour
{

    // ▼▼▼ [추가] 밭 고유 ID (인스펙터에서 0, 1, 2... 지정 필수!) ▼▼▼
    public int fieldID;

    // [수정] state를 currentState와 일치시킴
    public enum FieldState { Empty, Seed, Youth, Adult, Ready }
    public FieldState currentState = FieldState.Empty; // private -> public으로 변경 (저장용)

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
        // Start에서는 초기화하지 않습니다. (DBManager가 로드해 줄 것이라)
        // 만약 로드할 데이터가 없을 때만 초기화하려면 아래처럼 작성
        // UpdateFieldVisual(); 
    }

    // ▼▼▼ [추가 1] 현재 상태를 저장 데이터로 변환해서 반환 ▼▼▼
    public FieldSaveData GetSaveData()
    {
        FieldSaveData data = new FieldSaveData();
        data.fieldId = this.fieldID;
        data.state = (int)this.currentState;
        data.fertilizerCount = this.fertilizerCount;
        data.remainingTime = this.remainingTime;

        if (plantedSeed != null)
            data.plantedSeedName = plantedSeed.itemName;
        else
            data.plantedSeedName = "";

        return data;
    }

    // ▼▼▼ [추가 2] 저장된 데이터를 받아서 밭 상태 복구 ▼▼▼
    public void RestoreState(FieldSaveData data)
    {
        // 1. 기본 수치 복구
        this.currentState = (FieldState)data.state;
        this.fertilizerCount = data.fertilizerCount;
        this.remainingTime = data.remainingTime;

        // 2. 기존 식물 제거
        if (plantInstance != null) Destroy(plantInstance);
        if (growCoroutine != null) StopCoroutine(growCoroutine);

        // 3. 심겨진 씨앗이 있었다면 복구
        if (!string.IsNullOrEmpty(data.plantedSeedName) && currentState != FieldState.Empty)
        {
            // DBManager에서 이름으로 아이템 데이터 찾기
            ItemData seedData = DBManager.Instance.FindItemByName(data.plantedSeedName);

            if (seedData != null)
            {
                plantedSeed = seedData;

                // [중요] 성장 중이거나 다 자랐다면 프리팹 생성
                if (seedData.plantPrefab != null)
                {
                    plantInstance = Instantiate(seedData.plantPrefab, plantAnchor.position, Quaternion.identity, plantAnchor);

                    // 크기(Scale) 복구 로직 (성장 비율에 맞춰서)
                    float totalTime = seedData.growTime;
                    float progress = 1f;
                    if (totalTime > 0) progress = 1f - (remainingTime / totalTime);

                    if (currentState == FieldState.Ready)
                        plantInstance.transform.localScale = Vector3.one;
                    else
                        plantInstance.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, progress);
                }

                // [중요] 아직 자라는 중이면 코루틴 재시작 (남은 시간만큼만)
                if (currentState != FieldState.Ready)
                {
                    growCoroutine = StartCoroutine(GrowRoutine(remainingTime));
                }
            }
        }
        else
        {
            plantedSeed = null;
            currentState = FieldState.Empty;
        }

        // 4. 이미지 갱신
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