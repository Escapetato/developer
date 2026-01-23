using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Field : MonoBehaviour
{
    // ▼▼▼ 밭 고유 ID (인스펙터에서 지정 필수) ▼▼▼
    public int fieldID;

    public enum FieldState { Empty, Seed, Youth, Adult, Ready }
    public FieldState currentState = FieldState.Empty;

    [Header("Field Visuals (Generic)")]
    public Sprite emptySprite;
    public Sprite seedSprite;
    public Sprite youthSprite;
    public Sprite adultSprite;
    public Sprite readySprite;
    public SpriteRenderer fieldImage;

    [Header("Plant Logic")]
    // plantAnchor와 plantInstance 관련 변수는 더 이상 사용하지 않으므로 제거 가능하지만, 
    // 기존 구조 유지를 위해 변수 선언만 남겨두거나 삭제하셔도 됩니다.
    private ItemData plantedSeed;
    private float remainingTime;
    private Coroutine growCoroutine;

    [Header("비료 상태")]
    public int fertilizerCount = 0;
    private const float BASE_SPEED_MULTIPLIER = 1f;
    private const float FFERTILIZER_EFFECT = 0.25f;

    [Header("랜덤 씨앗 전용 비주얼")]
    public Sprite randomSeedReadySprite;
    private bool isFromRandomSeed = false; // 랜덤 씨앗으로 심어졌는지 여부

    private void Start()
    {
        // DBManager 로드를 기다리거나 기본 비주얼 업데이트
        UpdateFieldVisual();
    }

    // [저장 데이터 변환]
    public FieldSaveData GetSaveData()
    {
        FieldSaveData data = new FieldSaveData();
        data.fieldId = this.fieldID;
        data.state = (int)this.currentState;
        data.fertilizerCount = this.fertilizerCount;
        data.remainingTime = this.remainingTime;
        data.plantedSeedName = (plantedSeed != null) ? plantedSeed.itemName : "";
        return data;
    }

    // [데이터 복구] 프리팹 생성 로직 삭제됨
    public void RestoreState(FieldSaveData data)
    {
        this.currentState = (FieldState)data.state;
        this.fertilizerCount = data.fertilizerCount;
        this.remainingTime = data.remainingTime;

        if (growCoroutine != null) StopCoroutine(growCoroutine);

        if (!string.IsNullOrEmpty(data.plantedSeedName) && currentState != FieldState.Empty)
        {
            ItemData seedData = DBManager.Instance.FindItemByName(data.plantedSeedName);
            if (seedData != null)
            {
                plantedSeed = seedData;
                // 아직 자라는 중이면 코루틴 재시작
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

        UpdateFieldVisual();
    }

    public string GetRemainingTimeText()
    {
        if (currentState == FieldState.Empty) return "작물 없음";
        if (currentState == FieldState.Ready) return "수확 가능!";

        int totalSeconds = Mathf.CeilToInt(remainingTime);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int seconds = totalSeconds % 60;
        return string.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
    }

    public Sprite GetFieldSprite() => fieldImage.sprite;

    private IEnumerator GrowRoutine(float duration)
    {
        float totalGrowTime = plantedSeed.growTime;
        float elapsedWork = totalGrowTime - duration;

        while (elapsedWork < totalGrowTime)
        {
            float multiplier = GetGrowthMultiplier();
            elapsedWork += Time.deltaTime * multiplier;

            remainingTime = Mathf.Max(0, (totalGrowTime - elapsedWork) / multiplier);
            float progress = elapsedWork / totalGrowTime;

            // 진행도에 따라 스프라이트 상태만 변경
            if (progress < 0.33f) SetState(FieldState.Seed);
            else if (progress < 0.66f) SetState(FieldState.Youth);
            else SetState(FieldState.Adult);

            yield return null;
        }

        remainingTime = 0;
        SetState(FieldState.Ready);
        UpdateFieldVisual();
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

        // 현재 상태가 Ready(수확 가능)일 때만 랜덤 씨앗 체크
        if (currentState == FieldState.Ready)
        {
            if (isFromRandomSeed && randomSeedReadySprite != null)
            {
                fieldImage.sprite = randomSeedReadySprite;
                Debug.Log("랜덤 씨앗 전용 스프라이트 적용됨!");
            }
            else
            {
                fieldImage.sprite = (plantedSeed != null && plantedSeed.readyFieldSprite != null)
                                    ? plantedSeed.readyFieldSprite : readySprite;
            }
        }
        else // 성장 단계(Seed, Youth, Adult)일 때는 기존 스프라이트 사용
        {
            switch (currentState)
            {
                case FieldState.Empty: fieldImage.sprite = emptySprite; break;
                case FieldState.Seed: fieldImage.sprite = seedSprite; break;
                case FieldState.Youth: fieldImage.sprite = youthSprite; break;
                case FieldState.Adult: fieldImage.sprite = adultSprite; break;
            }
        }
    }

    private void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (currentState == FieldState.Empty) UIManager.Instance.OpenSeedPopup(this);
        else if (currentState == FieldState.Ready) Harvest();
        else UIManager.Instance.OpenFertilizerPopup(this);
    }

    public void Plant(ItemData seed, bool isRandom = false)
    {
        Debug.Log($"<color=cyan>심기 시도 - 씨앗: {seed.itemName}, 랜덤여부: {isRandom}</color>");
        plantedSeed = seed;
        remainingTime = seed.growTime;
        fertilizerCount = 0;
        isFromRandomSeed = isRandom;

        SetState(FieldState.Seed);

        // 프리팹 생성(Instantiate) 코드 삭제
        if (growCoroutine != null) StopCoroutine(growCoroutine);
        growCoroutine = StartCoroutine(GrowRoutine(remainingTime));

        // 퀘스트 진행도 : "심기"
        QuestManager.Instance?.NotifyAction(QuestConditionType.PlantCrop, seed, 1);
    }

    public float GetGrowthMultiplier() => BASE_SPEED_MULTIPLIER + (fertilizerCount * FFERTILIZER_EFFECT);

    public void ApplyFertilizer(int count)
    {
        fertilizerCount += count;
        if (growCoroutine != null) StopCoroutine(growCoroutine);
        growCoroutine = StartCoroutine(GrowRoutine(remainingTime));

        // 퀘스트 진행도: 비료 주입
        QuestManager.Instance?.NotifyAction(QuestConditionType.UseFertilizer, null, count);
    }
    public void Harvest()
    {
        if (currentState != FieldState.Ready) return;

        int myTier = HarvestToolManager.Instance.currentToolTier;
        int requiredTier = plantedSeed.requiredToolTier;

        if (myTier >= requiredTier)
        {
            // 1. 수확할 아이템의 아이콘 미리 저장
            Sprite itemIcon = plantedSeed.harvestItem.itemIcon;

            // 2. 아이템 지급
            InventoryManager.Instance.AddItem(plantedSeed.harvestItem, 1);

            // 3. 수확 애니메이션 실행 (완성된 밭 이미지가 아니라 '아이템 아이콘'을 넘김)
            StartCoroutine(HarvestPopUpRoutine(itemIcon));

            // 4. 상태 초기화
            plantedSeed = null;
            isFromRandomSeed = false;
            SetState(FieldState.Empty);
        }
    }

    private IEnumerator HarvestPopUpRoutine(Sprite iconSprite)
    {
        // 수확물 이미지를 보여줄 임시 오브젝트 생성
        GameObject popUp = new GameObject("HarvestPopUp");
        popUp.transform.position = transform.position + Vector3.up * 0.8f; // 밭보다 살짝 위

        // 이미지 컴포넌트 추가 및 설정
        SpriteRenderer sr = popUp.AddComponent<SpriteRenderer>();
        sr.sprite = iconSprite;
        sr.sortingOrder = 50; // 밭보다 훨씬 앞에 보이도록

        float duration = 0.6f; // 전체 애니메이션 시간
        float elapsed = 0f;

        Vector3 startPos = popUp.transform.position;
        Vector3 targetPos = startPos + Vector3.up * 1.4f; // 위로 1.2유닛만큼 이동

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 1. 위치 이동: Lerp로 부드럽게 위로
            popUp.transform.position = Vector3.Lerp(startPos, targetPos, t);

            // 2. 크기 조절: 0에서 시작해서 1.5배까지 커졌다가 1로 수렴 (뾰롱!)
            // 애니메이션 커브 느낌을 내기 위해 Sin 함수 사용
            float scale = Mathf.Sin(t * Mathf.PI) * 0.3f + 1.0f;
            popUp.transform.localScale = Vector3.one * scale;

            // 3. 투명도 조절: 절반 이후부터 서서히 사라짐
            if (t > 0.5f)
            {
                Color c = sr.color;
                c.a = 1f - ((t - 0.5f) * 2f);
                sr.color = c;
            }

            yield return null;
        }

        Destroy(popUp);
    }
}