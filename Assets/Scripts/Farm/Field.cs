using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Field : MonoBehaviour
{
    // 1. 밭의 현재 상태 (Planted 제거)
    private enum FieldState { Empty, Growing, Ready }
    private FieldState currentState = FieldState.Empty;

    [Header("Field Visuals")]
    public Sprite emptySprite;
    public Sprite plantedSprite; // (이건 Plant()에서 잠깐 쓸 수 있음)
    public Sprite growingSprite;
    public Sprite readySprite;
    public SpriteRenderer fieldImage;

    [Header("Plant Logic")]
    public Transform plantAnchor; // 식물이 자라날 위치
    private GameObject plantInstance; // 심겨진 식물 오브젝트
    private ItemData plantedSeed;
    private float remainingTime;
    private Coroutine growCoroutine; // 성장 코루틴 저장

    [Header("비료 상태")]
    public int fertilizerCount = 0; // 이 밭에 적용된 비료 개수
    private const float BASE_SPEED_MULTIPLIER = 1f;
    private const float FERTILIZER_EFFECT = 0.25f;

    private void Start()
    {
        UpdateFieldVisual();
    }

    private IEnumerator GrowRoutine(float duration)
    {
        // [수정] state -> currentState
        currentState = FieldState.Growing;
        UpdateFieldVisual();

        // [추가] 이미 얼마나 성장했는지 (스케일 계산을 위한) 초기 비율을 계산합니다.
    float totalGrowTime = plantedSeed.growTime;
    float currentProgress = 1f - (duration / totalGrowTime);

        float t = 0;
        while (t < duration)
        {
            t += Time.deltaTime*GetGrowthMultiplier();
            remainingTime = duration - t; // 남은 시간 갱신

            if (plantInstance != null)
        {
            // 최종 성장 비율 = 현재 진행된 비율 + (남은 성장량 비율 * t/duration)
            float scaleProgress = currentProgress + (1f - currentProgress) * (t / duration);
            float scale = Mathf.Lerp(0.3f, 1f, scaleProgress);
            plantInstance.transform.localScale = Vector3.one * scale;
        }
        
        yield return null;
    }

    remainingTime = 0;
    currentState = FieldState.Ready;
    UpdateFieldVisual();
}

    private void UpdateFieldVisual()
    {
        if (fieldImage == null) return;

        // [수정] switch (state) -> switch (currentState)
        switch (currentState)
        {
            case FieldState.Empty:
                fieldImage.sprite = emptySprite;
                break;
            // [제거] FieldState.Planted 케이스 제거
            case FieldState.Growing:
                fieldImage.sprite = growingSprite;
                break;
            case FieldState.Ready:
                fieldImage.sprite = readySprite;
                break;
        }
    }

    public Sprite GetFieldSprite()
{
    // 가장 정확한 방법: 현재 SpriteRenderer가 렌더링하고 있는 Sprite를 반환
    if (fieldImage != null)
    {
        return fieldImage.sprite;
    }
    
    // Fallback: fieldImage가 null일 경우, 상태에 따라 미리 정의된 Sprite 반환
    switch (currentState)
    {
        case FieldState.Empty:
            return emptySprite;
        case FieldState.Growing:
            return growingSprite;
        case FieldState.Ready:
            return readySprite;
        default:
            return null; // 모든 경우가 아니라면 null 반환
    }
}

    // [수정] state -> currentState
    public bool IsEmpty() => currentState == FieldState.Empty;
    public bool IsReady() => currentState == FieldState.Ready;

   private void OnMouseDown()
{
        // [추가] 마우스가 UI(팝업창, 버튼 등) 위에 있다면 밭 클릭 로직 실행 안 함
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Debug.Log($"클릭됨! 현재 상태: {currentState}");
        if (currentState == FieldState.Empty)
        {
            UIManager.Instance.OpenSeedPopup(this);
        }
        else if (currentState == FieldState.Ready)
        {
            Harvest();
        }
        else if (currentState == FieldState.Growing)
        {
            UIManager.Instance.OpenFertilizerPopup(this);
        }
}

    public void Plant(ItemData seed)
    {
        plantedSeed = seed;
        remainingTime = seed.growTime;

        // [수정] 심자마자 '성장 중' 상태로 변경하고 코루틴 시작
        currentState = FieldState.Growing;

        // [추가] 식물 프리팹 생성 (팀원 로직)
        if (plantInstance != null) Destroy(plantInstance);
        plantInstance = Instantiate(seed.plantPrefab, plantAnchor.position, Quaternion.identity, plantAnchor);
        plantInstance.transform.localScale = Vector3.one * 0.3f; // 시작 크기

        // [추가] 성장 코루틴 시작
        if (growCoroutine != null) StopCoroutine(growCoroutine);
        growCoroutine = StartCoroutine(GrowRoutine(remainingTime));

        Debug.Log(seed.itemName + "을(를) 심었습니다.");
        UpdateFieldVisual(); // (필요 시 plantedSprite로 변경)
    }

    // 비료 개수에 따른 최종 성장 속도 배율 계산
    public float GetGrowthMultiplier()
    {
        return BASE_SPEED_MULTIPLIER + (fertilizerCount * FERTILIZER_EFFECT);
    }

public void ApplyFertilizer(int count)
{
    // 비료 적용 전 현재 배율 저장 (남은 시간 계산에 필요)
    float oldMultiplier = GetGrowthMultiplier(); 
    
    // 1. 비료 개수 증가
    fertilizerCount += count;
    
    // 비료 적용 후 새로운 배율 계산
    float newMultiplier = GetGrowthMultiplier();

    // 2. 남은 성장 시간 업데이트
    // 현재까지 진행된 성장 시간 (총 걸릴 시간 - 현재 남은 시간)
    float totalGrowTime = plantedSeed.growTime; 
    float timeElapsed = totalGrowTime - remainingTime;
    
    // 비료 적용 전의 성장 속도를 적용한 실제 남은 시간 (TimeLeft * Old_Speed)
    float actualRemainingGrowth = remainingTime * oldMultiplier;
    
    // 새로운 배속에 따른 실제 남은 시간 (ActualGrowth / New_Speed)
    remainingTime = actualRemainingGrowth / newMultiplier;

    // 3. 성장 코루틴 재시작
    if (growCoroutine != null)
    {
        StopCoroutine(growCoroutine);
    }
    growCoroutine = StartCoroutine(GrowRoutine(remainingTime));

    Debug.Log($"{count}개 비료 적용. 새 배율: {newMultiplier}배. 남은 시간: {remainingTime:F2}초");
}

    public void Harvest()
{
    if (currentState != FieldState.Ready) return;

    // 매니저에서 현재 클릭으로 선택해둔 티어를 가져옴
    int myTier = HarvestToolManager.Instance.currentToolTier;

    // 작물의 요구 티어와 비교
    if (myTier == plantedSeed.requiredToolTier)
    {
        // 성공 로직 (인벤토리 추가 등)
        InventoryManager.Instance.AddItem(plantedSeed.harvestItem, 1);

        if (plantInstance != null) Destroy(plantInstance);
        plantedSeed = null;
        currentState = FieldState.Empty;
        UpdateFieldVisual();
    }
    else
    {
        // 실패: 작물 데이터에 적어둔 도구 이름가져옴
        string neededTool = plantedSeed.harvestToolName;
        Debug.Log($"{neededTool}(이)가 필요합니다!");
    }
}
}