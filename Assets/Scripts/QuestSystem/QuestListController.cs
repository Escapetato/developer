using System.Collections.Generic;
using UnityEngine;

public class QuestListController : MonoBehaviour
{
    // 스크롤뷰 확장에 필요 
    [Header("슬롯이 붙을 부모")]
    [SerializeField] private Transform slotParent;

    [Header("타입별 슬롯 프리팹")]
    [SerializeField] private GameObject mainSlotPrefab;
    [SerializeField] private GameObject subSlotPrefab;
    [SerializeField] private GameObject dailySlotPrefab;

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

        // 4) 일일 3개
        CreateMultipleSlots(
            QuestManager.Instance.dailyQuests,
            dailySlotPrefab,
            3
        );
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
        }
        else
        {
            Debug.LogWarning("[QuestListController] 프리팹에 QuestSlotUI 컴포넌트가 없습니다.");
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
