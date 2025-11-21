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

    [Header("보상 버튼")]
    [SerializeField] private Button rewardButton;

    private QuestData currentQuest;

    public void Show(QuestData data)
    {
        currentQuest = data;

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
            // 데이터 개수와 UI 슬롯 개수 중 작은 쪽까지만 사용
            int count = Mathf.Min(data.conditionTexts.Length, conditionRows.Length);

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
    
    public void RefreshConditions()
    {
        if (currentQuest == null)
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
            conditionCheckOn.Length
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
            var rowGO   = conditionRows[i];
            var textUI  = conditionTexts[i];
            var checkGO = conditionCheckOn[i];

            if (rowGO != null) rowGO.SetActive(true);

            int cur    = currentQuest.currentCounts[i];
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
}