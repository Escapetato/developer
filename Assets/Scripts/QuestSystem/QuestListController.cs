using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestListController : MonoBehaviour
{
    // 스크롤뷰 확장에 필요 
    [Header("슬롯이 붙을 부모")]
    [SerializeField] private Transform slotParent;

    [Header("타입별 슬롯 프리팹")]
    [SerializeField] private GameObject mainSlotPrefab;
    [SerializeField] private GameObject subSlotPrefab;
    [SerializeField] private GameObject dailySlotPrefab;

    [Header("과거 메인 슬롯 프리팹")]
    [SerializeField] private GameObject closedMainSlotPrefab;

    [Header("과거 퀘스트 박스 아이콘")]
    [SerializeField] private GameObject boxIconObject;
    [SerializeField] private Sprite boxOffSprite;      
    [SerializeField] private Sprite boxOnSprite;

    [Header("퀘스트 상세 패널")]
    [SerializeField] private QuestDetailUI questDetailUI;

    // 현재 모드 (평소 퀘스트 데이터) 
    private bool showClosedMains = false;

    // 현재 선택된 슬롯 
    private QuestSlotUI currentSelectedSlot;

    // 추후 세이브 로드, 진행도 반영 이벤트 연결 필요 
    private void Start()
    {
        RefreshSlots();
    }

    // 현재 열린 퀘스트들을 UI로 나타내는 함수 
    public void RefreshSlots()
    {
        // 1) 기존 슬롯 삭제 
        ClearChildren(slotParent);

        // 2) 메인 1개
        QuestData main = FindFirstActiveOrCompleted(QuestManager.Instance.mainQuests);
        if (main != null)
        {
            CreateSlot(mainSlotPrefab, main);
        }

        // 3) 서브 2개
        CreateMultipleSlots(
            QuestManager.Instance.subQuests,
            subSlotPrefab,
            2
        );

        // 4) 일일 대표 1개만
        QuestData dailyRep = FindFirstActiveOrCompleted(QuestManager.Instance.dailyQuests);
        if (dailyRep != null)
        {
            CreateSlot(dailySlotPrefab, dailyRep);
        }

    }

    // 박스 선택 시 Closed 된 메인 퀘스트만 
    private void RefreshClosedMainSlots()
    {
        ClearChildren(slotParent);

        var closedList = new List<QuestData>();
        foreach (var q in QuestManager.Instance.mainQuests)
        {
            if (q == null) continue;
            if (q.state == QuestState.Closed)
            {
                closedList.Add(q);
            }
        }

        if (closedMainSlotPrefab == null)
        {
            Debug.LogWarning("[QuestListController] closedMainSlotPrefab 이 비어 있습니다.");
            return;
        }

        foreach (var q in closedList)
        {
            CreateSlot(closedMainSlotPrefab, q); 
        }
    }


    // 리스트에서 Active/Completed 중 제일 먼저 나오는 퀘스트 하나 찾기
    private QuestData FindFirstActiveOrCompleted(List<QuestData> list)
    {
        foreach (var q in list)
        {
            if (q == null) continue;
            if (q.state == QuestState.Active || q.state == QuestState.Completed)
                return q;
        }
        return null;
    }

    // 같은 타입 슬롯 여러 개 (서브/일일)
    private void CreateMultipleSlots(List<QuestData> list, GameObject prefab, int maxCount)
    {
        int created = 0;
        foreach (var q in list)
        {
            if (q == null) continue;
            if (q.state != QuestState.Active && q.state != QuestState.Completed)
                continue;

            CreateSlot(prefab, q);
            created++;

            if (created >= maxCount)
                break;
        }
    }

    // 실제 프리팹 Instantiate + QuestSlotUI.Setup
    private void CreateSlot(GameObject prefab, QuestData data)
    {
        if (prefab == null || slotParent == null)
        {
            Debug.LogWarning("[QuestListController] 프리팹 또는 slotParent가 비어 있습니다.");
            return;
        }

        GameObject go = Instantiate(prefab, slotParent);
        QuestSlotUI ui = go.GetComponent<QuestSlotUI>();
        if (ui != null)
        {
            ui.Setup(data);
            ui.SetOwner(this);

            ui.SetSelected(false);

            // 첫 슬롯 자동 선택 
            if (currentSelectedSlot == null)
            {
                OnSlotClicked(ui);
            }
        }
    }

    // 박스 아이콘 눌렀을 때 
    public void OnClickBoxIcon()
    {
        showClosedMains = !showClosedMains;
        Debug.Log("[QuestListController] Box clicked, mode = " + showClosedMains);

        if (boxIconObject != null)
        {
            var img = boxIconObject.GetComponent<UnityEngine.UI.Image>();
            var sr = boxIconObject.GetComponent<SpriteRenderer>();

            if (img != null)
                img.sprite = showClosedMains ? boxOnSprite : boxOffSprite;
            else if (sr != null)
                sr.sprite = showClosedMains ? boxOnSprite : boxOffSprite;
        }

        // 슬롯 모드 변경
        currentSelectedSlot = null;

        if (showClosedMains)
            RefreshClosedMainSlots();
        else
            RefreshSlots();
    }

    // 클릭 이벤트 발생 알림이 올 때 호출되는 함수 
    public void OnSlotClicked(QuestSlotUI clickedSlot)
    {
        if (clickedSlot == null)
            return;

        // 1) 이전 선택된 슬롯이 있고, 그 슬롯이 이번에 클릭된 슬롯이 아니라면 선택 해제
        if (currentSelectedSlot != null && currentSelectedSlot != clickedSlot)
        {
            currentSelectedSlot.SetSelected(false);
        }

        // 2) 새 슬롯을 선택 상태로
        currentSelectedSlot = clickedSlot;
        currentSelectedSlot.SetSelected(true);

        // 3) 상세사항 보여주는 오른쪽 패널 업데이트
        if (questDetailUI != null)
        {
            QuestData data = clickedSlot.GetQuest();
            questDetailUI.Show(data);
        }
    }


    // 자식 모두 삭제
    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}
