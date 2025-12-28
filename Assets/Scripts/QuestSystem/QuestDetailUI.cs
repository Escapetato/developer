using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuestDetailUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descText;

    // 필요하면 추가: 조건, 보상 등등
    [Header("달성 조건 UI")]
    [SerializeField] private GameObject[] conditionRows;        // ConditionRow1, 2 ...
    [SerializeField] private TextMeshProUGUI[] conditionTexts;  // ContentText1, 2 ...
    [SerializeField] private GameObject[] conditionStrikeLine;   // complete_line1, complete_line2
    [SerializeField] private GameObject[] conditionCheckOn;     // 빨간 체크 아이콘들

    [Header("일일퀘스트 레이아웃")]
    [SerializeField] private RectTransform conditionContainer; // ConditionContainer
    [SerializeField] private float dailyContainerOffsetY = 2f; // 위로 올릴 값 (Inspector에서 조절)

    private Vector2 conditionContainerNormalPos;
    private bool isConditionPosCached = false;

    [Header("보상 버튼")]
    [SerializeField] private Button rewardButton;

    private QuestData currentQuest;

    private bool isDailyQuestView = false;  // 현재 화면이 '일일퀘스트(3개 리스트)' 모드인지 여부
    // 일일퀘스트는 최대 몇 개까지 UI 슬롯에 표시할지
    private const int DailyQuestSlotCount = 3;

    public void Show(QuestData data)
    {
        currentQuest = data;
        isDailyQuestView = false;
        if (descText != null) descText.gameObject.SetActive(true);

        isDailyQuestView = false;
        ApplyDailyLayout(false);

        // 0) 데이터가 없으면 깨끗이 지우고 끝
        if (data == null)
        {
            Clear();
            return;
        }

        // 1) 제목 / 본문 설명 세팅
        titleText.text = data.title;
        descText.text = data.questDesc;

        // 2) 모든 조건 행/체크 초기화 (일단 다 끄기)
        for (int i = 0; i < conditionRows.Length; i++)
        {
            if (conditionRows[i] != null)
                conditionRows[i].SetActive(false);

            if (i < conditionCheckOn.Length && conditionCheckOn[i] != null)
                conditionCheckOn[i].SetActive(false); // 체크표시 초기화

            if (i < conditionStrikeLine.Length && conditionStrikeLine[i] != null)
                conditionStrikeLine[i].SetActive(false); // 취소선 초기화
        }

        // 3) QuestData 안의 conditionTexts를 UI에 뿌리기
        if (data.conditionTexts != null)
        {
            int count = Mathf.Min(
                data.conditionTexts.Length,
                conditionRows.Length,
                conditionTexts.Length
               );


            for (int i = 0; i < count; i++)
            {
                // 행 보이게
                if (conditionRows[i] != null)
                    conditionRows[i].SetActive(true);

                // 텍스트 넣기
                if (conditionTexts[i] != null)
                    conditionTexts[i].text = data.conditionTexts[i];

                // 아직 달성 전이니까 체크는 꺼둠
                if (i < conditionCheckOn.Length && conditionCheckOn[i] != null)
                    conditionCheckOn[i].SetActive(false);
            }
        }

        // 4) currentCounts / targetCounts 기준으로 취소선 + 체크 + 버튼 상태 갱신
        RefreshConditions();
    }

    public void ShowDailyQuests(List<QuestData> dailyQuests)
    {
        isDailyQuestView = true;
        ApplyDailyLayout(true);

        // 1) 제목 통합 변경
        if (titleText != null) titleText.text = "일일퀘스트";

        // 2) 설명 텍스트는 사용하지 않음
        if (descText != null)
        {
            descText.text = "";
            descText.gameObject.SetActive(false);
        }

        // 일일퀘스트는 특정 QuestData 하나를 추적하지 않으므로 null 처리
        currentQuest = null;


        // 3) 모든 행 초기화 (일단 다 끄기)
        for (int i = 0; i < conditionRows.Length; i++)
        {
            if (conditionRows[i] != null)
                conditionRows[i].SetActive(false);

            if (i < conditionCheckOn.Length && conditionCheckOn[i] != null)
                conditionCheckOn[i].SetActive(false);

            if (i < conditionStrikeLine.Length && conditionStrikeLine[i] != null)
                conditionStrikeLine[i].SetActive(false);
        }

        // 3) 입력이 없으면 여기서 끝
        if (dailyQuests == null || dailyQuests.Count == 0)
            return;

        // 4) 최대 3개까지만 표시
        if (conditionRows.Length < DailyQuestSlotCount || conditionTexts.Length < DailyQuestSlotCount)
        {
            Debug.LogWarning($"일일퀘스트는 {DailyQuestSlotCount}개 슬롯이 필요합니다. Hierarchy에서 conditionRow와 Text 슬롯을 추가하세요.");
        }

        int rowCount = Mathf.Min(DailyQuestSlotCount, dailyQuests.Count, conditionRows.Length, conditionTexts.Length);

        for (int i = 0; i < rowCount; i++)
        {
            QuestData q = dailyQuests[i];

            if (conditionRows[i] != null)
                conditionRows[i].SetActive(true);

            // 안전하게 현재/목표값을 가져옴
            int cur = 0;
            int target = 0;

            if (q != null)
            {
                // 일일 퀘스트는 단일 조건(1개)이라고 가정
                if (q.currentCounts != null && q.currentCounts.Length > 0)
                    cur = q.currentCounts[0];
                else
                    cur = q.currentCount;

                if (q.targetCounts != null && q.targetCounts.Length > 0)
                    target = q.targetCounts[0];
            }

            bool completed = (target > 0) && (cur >= target);

            // 3) Quest Database의 title을 contentText로 사용
            if (conditionTexts[i] != null)
            {
                string questTitle = (q != null) ? q.title : "";
                questTitle = System.Text.RegularExpressions.Regex.Replace(
                    questTitle,
                    @"\s*\d+\s*회",
                    ""
                );

                if (target > 0)
                    conditionTexts[i].text = $"{questTitle} ({cur}/{target})";
                else
                    conditionTexts[i].text = questTitle;
            }

            // 취소선/체크
            if (i < conditionStrikeLine.Length && conditionStrikeLine[i] != null)
                conditionStrikeLine[i].SetActive(completed);

            if (i < conditionCheckOn.Length && conditionCheckOn[i] != null)
                conditionCheckOn[i].SetActive(completed);
        }
    }

    private void ApplyDailyLayout(bool isDaily)
    {
        if (conditionContainer == null) return;

        // 설명 텍스트는 일일퀘스트에서 숨김
        if (descText != null)
            descText.gameObject.SetActive(!isDaily);

        if (!isConditionPosCached)
        {
            conditionContainerNormalPos = conditionContainer.anchoredPosition;
            isConditionPosCached = true;
        }

        if (isDaily)
        {
            conditionContainer.anchoredPosition =
                conditionContainerNormalPos + new Vector2(0f, dailyContainerOffsetY);
        }
        else
        {
            conditionContainer.anchoredPosition = conditionContainerNormalPos;
        }
    }

    public void RefreshConditions()
    {
        if (currentQuest == null)
        {
            if (rewardButton != null) rewardButton.interactable = false;
            return;
        }

        // ✅ 이미 보상 받았거나 닫힌 퀘스트면 버튼 비활성
        if (currentQuest.rewardClaimed || currentQuest.state == QuestState.Closed)
        {
            if (rewardButton != null) rewardButton.interactable = false;
            return;
        }

        // ✅ 아이템 보상이 아니면(땅 확장 등) 이번 범위에서는 버튼 비활성
        if (currentQuest.type != QuestType.Daily && currentQuest.rewardItem == null)
        {
            if (rewardButton != null) rewardButton.interactable = false;
            return;
        }


        if (currentQuest.conditionTexts == null ||
            currentQuest.targetCounts == null ||
            currentQuest.currentCounts == null)
        {
            if (rewardButton != null) rewardButton.interactable = false;
            return;
        }

        int count = Mathf.Min(
            currentQuest.conditionTexts.Length,
            currentQuest.targetCounts != null ? currentQuest.targetCounts.Length : 0,
            currentQuest.currentCounts != null ? currentQuest.currentCounts.Length : 0,
            conditionRows.Length,
            conditionTexts.Length,
            conditionCheckOn.Length,
            conditionStrikeLine.Length
        );

        // 조건이 한 개도 없으면 보상 버튼은 비활성
        if (count == 0)
        {
            if (rewardButton != null) rewardButton.interactable = false;
            return;
        }

        bool allCompleted = true;

        for (int i = 0; i < count; i++)
        {
            var rowGO = conditionRows[i];
            var textUI = conditionTexts[i];
            var checkGO = conditionCheckOn[i];

            if (rowGO != null) rowGO.SetActive(true);

            int cur = currentQuest.currentCounts[i];
            int target = currentQuest.targetCounts[i];

            bool completed = target > 0 && cur >= target;

            // 1) 텍스트: "설명 (cur/target)" 형식으로 출력
            if (textUI != null)
            {
                textUI.text = $"{currentQuest.conditionTexts[i]} ({cur}/{target})";
            }

            // 2) 취소선 on/off
            conditionStrikeLine[i].SetActive(completed);

            // 3) 빨간 체크 on/off
            if (checkGO != null)
                checkGO.SetActive(completed);

            if (!completed)
                allCompleted = false;
        }

        // 4) 모든 조건 달성 시 보상 버튼 활성화
        if (rewardButton != null)
            rewardButton.interactable = allCompleted;
    }

    public void Clear()
    {
        isDailyQuestView = false;
        if (descText != null) descText.gameObject.SetActive(true);

        if (titleText != null) titleText.text = "";
        if (descText != null) descText.text = "";

        for (int i = 0; i < conditionRows.Length; i++)
        {
            if (conditionRows[i] != null)
                conditionRows[i].SetActive(false);
        }

        for (int i = 0; i < conditionCheckOn.Length; i++)
        {
            if (conditionCheckOn[i] != null)
                conditionCheckOn[i].SetActive(false);
        }

        for (int i = 0; i < conditionStrikeLine.Length; i++)
        {
            if (conditionStrikeLine[i] != null)
                conditionStrikeLine[i].SetActive(false);
        }

        if (rewardButton != null)
            rewardButton.interactable = false;
    }

    private void Awake()
    {
        if (conditionContainer != null && !isConditionPosCached)
        {
            conditionContainerNormalPos = conditionContainer.anchoredPosition;
            isConditionPosCached = true;
        }

        if (rewardButton != null)
        {
            rewardButton.onClick.RemoveAllListeners();
            rewardButton.onClick.AddListener(() =>
            {
                if (isDailyQuestView) return;
                if (currentQuest == null) return;

                if (QuestManager.Instance != null && QuestManager.Instance.ClaimRewardMainSub(currentQuest))
                {
                    RefreshConditions();
                }
            });
        }
    }

}