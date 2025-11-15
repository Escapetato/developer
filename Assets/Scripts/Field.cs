using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    private void Start()
    {
        UpdateFieldVisual();
    }

    private IEnumerator GrowRoutine(float growTime)
    {
        // [수정] state -> currentState
        currentState = FieldState.Growing;
        UpdateFieldVisual();

        float t = 0;
        while (t < growTime)
        {
            t += Time.deltaTime;
            remainingTime = growTime - t; // 남은 시간 갱신

            if (plantInstance != null)
            {
                // (팀원분의 성장 로직)
                float scale = Mathf.Lerp(0.3f, 1f, t / growTime);
                plantInstance.transform.localScale = Vector3.one * scale;
            }
            yield return null;
        }

        remainingTime = 0;
        // [수정] state -> currentState
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

    // [수정] state -> currentState
    public bool IsEmpty() => currentState == FieldState.Empty;
    public bool IsReady() => currentState == FieldState.Ready;

    private void OnMouseDown()
    {
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
            Debug.Log("성장 중입니다...");
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

    public void Harvest()
    {
        if (currentState != FieldState.Ready) return;

        // [수정] ItemData.cs에 harvestItem을 추가했으므로 이 코드가 작동
        if (plantedSeed.harvestItem != null)
        {
            InventoryManager.Instance.AddItem(plantedSeed.harvestItem, 1);
            UIManager.Instance.ShowItemAcquiredPopup(plantedSeed.harvestItem);
        }

        // [추가] 심겨진 식물 오브젝트 삭제
        if (plantInstance != null)
        {
            Destroy(plantInstance);
        }

        // 밭 초기화
        plantedSeed = null;
        currentState = FieldState.Empty;
        UpdateFieldVisual();
        Debug.Log("수확 완료!");
    }
}