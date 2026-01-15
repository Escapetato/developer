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

    // 마지막으로 유저가 선택한 슬롯이 '일일'이었는지
    private bool lastSelectedWasDaily = false;

    // QuestChanged로 리프레시할 때, 첫 슬롯 자동선택을 잠깐 막기
    private bool suppressAutoSelectOnce = false;

    // 추후 세이브 로드, 진행도 반영 이벤트 연결 필요 
    private void Start()
    {
        RefreshSlots();

        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestChanged += HandleQuestChanged;
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestChanged -= HandleQuestChanged;
    }

    private void HandleQuestChanged()
    {
        bool wantKeepDaily = lastSelectedWasDaily && !showClosedMains;

        // 직전 선택이 일일 슬롯이면: 왼쪽 리스트를 Refresh 하지 않는다.
        // -> daily_lineO(선택 테두리) 그대로 유지
        if (wantKeepDaily && currentSelectedSlot != null && currentSelectedSlot.IsDailySlot())
        {
            // 오른쪽 패널만 최신 데이터로 갱신
            if (questDetailUI != null)
                questDetailUI.ShowDailyQuests(GetDailyQuestsForUI());

            // 빨간 점만 최신화(필요 시)
            currentSelectedSlot.RefreshNewDot();

            // 혹시라도 선택이 풀렸을 가능성 대비
            currentSelectedSlot.SetSelected(true);
            return;
        }

        // ====== 기존 로직(일일이 아니면 전체 리프레시) ======
        suppressAutoSelectOnce = true;

        if (showClosedMains) RefreshClosedMainSlots();
        else RefreshSlots();

        if (wantKeepDaily)
        {
            var dailySlot = FindDailySlotInChildren();
            if (dailySlot != null) SelectSlot(dailySlot, false);
        }
        else
        {
            var first = FindFirstSlotInChildren();
            if (first != null) SelectSlot(first, false);
        }

        suppressAutoSelectOnce = false;
    }


    private QuestSlotUI FindDailySlotInChildren()
    {
        if (slotParent == null) return null;

        for (int i = 0; i < slotParent.childCount; i++)
        {
            var ui = slotParent.GetChild(i).GetComponent<QuestSlotUI>();
            if (ui == null) continue;

            var q = ui.GetQuest();
            bool isDaily = ui.IsDailySlot() || (q != null && q.type == QuestType.Daily);

            if (isDaily) return ui;
        }
        return null;
    }


    private QuestSlotUI FindFirstSlotInChildren()
    {
        if (slotParent == null || slotParent.childCount == 0) return null;
        return slotParent.GetChild(0).GetComponent<QuestSlotUI>();
    }


    // 현재 열린 퀘스트들을 UI로 나타내는 함수 
    public void RefreshSlots()
    {
        // 1) 기존 슬롯 삭제 
        ClearChildren(slotParent);
        currentSelectedSlot = null;

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
        currentSelectedSlot = null;

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

            // 첫 슬롯 자동 선택 (일일 제외)
            if (currentSelectedSlot == null && !suppressAutoSelectOnce)
            {
                SelectSlot(ui, false);
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

    public void OnSlotClicked(QuestSlotUI clickedSlot)
    {
        SelectSlot(clickedSlot, true); // ✅ 유저 클릭
    }

    // 클릭 이벤트 발생 알림이 올 때 호출되는 함수 
    private void SelectSlot(QuestSlotUI clickedSlot, bool isUserClick)
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
        // 현재 선택이 일일인지 기억
        lastSelectedWasDaily = currentSelectedSlot.IsDailySlot() || (currentSelectedSlot.GetQuest() != null && currentSelectedSlot.GetQuest().type == QuestType.Daily);
        

        // 유저가 눌렀을 때만 "새로 열림" 해제
        if (isUserClick)
            MarkQuestAsSeen(clickedSlot);

        // 3) 상세사항 보여주는 오른쪽 패널 업데이트
        if (questDetailUI != null)
        {
            QuestData data = clickedSlot.GetQuest();

            // 일일 퀘스트 슬롯이면 단일 상세가 아니라 일일 목록 UI를 보여준다.
            bool isDaily = (clickedSlot.IsDailySlot()) || (data != null && data.type == QuestType.Daily);
            if (isDaily)
            {
                questDetailUI.ShowDailyQuests(GetDailyQuestsForUI());
            }
            else
            {
                bool isClosedView = showClosedMains || (data != null && data.state == QuestState.Closed);

                if (isClosedView)
                    questDetailUI.ShowClosed(data);   
                else
                    questDetailUI.Show(data);
            }

        }

        currentSelectedSlot.SetSelected(true);
    }


    private void MarkQuestAsSeen(QuestSlotUI clickedSlot)
    {
        QuestData data = clickedSlot.GetQuest();
        bool isDaily = clickedSlot.IsDailySlot() || (data != null && data.type == QuestType.Daily);

        if (isDaily)
        {
            var qm = QuestManager.Instance;
            if (qm != null && qm.dailyQuests != null)
            {
                foreach (var q in qm.dailyQuests)
                {
                    if (q == null) continue;
                    if (q.state == QuestState.Active || q.state == QuestState.Completed)
                        q.isNewlyOpened = false;
                }
            }
        }
        else
        {
            if (data != null)
                data.isNewlyOpened = false;
        }

        // 클릭한 슬롯의 점 즉시 갱신
        clickedSlot.RefreshNewDot();
    }

    // 자식 모두 삭제
    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    // 일일 퀘스트(Active/Completed) 목록을 UI에 보여주기 위한 리스트로 모은다.
    private List<QuestData> GetDailyQuestsForUI()
    {
        var result = new List<QuestData>();
        if (QuestManager.Instance == null || QuestManager.Instance.dailyQuests == null)
            return result;

        foreach (var q in QuestManager.Instance.dailyQuests)
        {
            if (q == null) continue;
            if (q.state == QuestState.Active || q.state == QuestState.Completed)
                result.Add(q);
        }
        return result;
    }

}
