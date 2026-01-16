using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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

    [Header("일일퀘스트 미션 줄간격(일일 모드에서만)")]
    [SerializeField] private float dailyRowExtraSpacing = 20f; // Inspector에서 조절

    private Vector3[] conditionRowNormalPos;
    private bool isRowPosCached = false;

    [Header("보상 버튼")]
    //[SerializeField] private Button[] rewardButtons;
    [SerializeField] private Button rewardButton;

    [Header("보상 UI 루트(박스)")]
    [SerializeField] private GameObject rewardBoxRoot; // 보상 박스 전체(배경+아이콘+텍스트 포함)

    [Header("보상 슬롯 UI")]
    [SerializeField] private GameObject[] rewardSlotRoots;       // Slot0~2 루트
    [SerializeField] private Image[] rewardSlotIcons;            // Slot0~2 아이콘 Image
    [SerializeField] private TextMeshProUGUI[] rewardSlotTexts;  // Slot0~2 텍스트 TMP

    [Header("보상 아이콘 - 땅, 도구 (메인)")]
    [SerializeField] private Sprite iconLand;   // 땅
    [SerializeField] private Sprite iconNat;    // 낫
    [SerializeField] private Sprite iconSap;    // 모종삽
    [SerializeField] private Sprite iconGawi;   // 전지가위

    [SerializeField] private Sprite iconLandOff;
    [SerializeField] private Sprite iconNatOff;
    [SerializeField] private Sprite iconSapOff;
    [SerializeField] private Sprite iconGawiOff;

    [Header("보상 아이콘 - 포잉, 비료 (공통)")]
    [SerializeField] private Sprite iconPoing;
    [SerializeField] private Sprite iconFertilizer;

    [SerializeField] private Sprite iconPoingOff;
    [SerializeField] private Sprite iconFertilizerOff;

    [Header("보상 아이콘 - 물약 (rewardKey 100~107)")]
    [SerializeField] private Sprite[] potionIcons = new Sprite[8];
    [SerializeField] private Sprite[] potionIconsOff = new Sprite[8];

    private static readonly string[] PotionNames =
{
    "불", "소리", "번개", "바람", "물", "별", "꽃", "무지개" 
};

    [Header("아이템 아이콘 fallback")]
    [SerializeField] private Sprite defaultItemSprite;
    [SerializeField] private Sprite defaultItemSpriteOff;

    [Header("보상 수령 체크 오버레이 (빨간 체크)")]
    [SerializeField] private GameObject[] rewardClaimCheckOn; // Slot0~2에 대응

    private QuestData currentQuest;
    private List<QuestData> currentDailyQuests;

    private bool isDailyQuestView = false;  // 현재 화면이 '일일퀘스트(3개 리스트)' 모드인지 여부
    // 일일퀘스트는 최대 몇 개까지 UI 슬롯에 표시할지
    private const int DailyQuestSlotCount = 3;

    public void Show(QuestData data)
    {
        SetDetailVisible(true);

        currentQuest = data;
        isDailyQuestView = false;
        if (descText != null) descText.gameObject.SetActive(true);

        isDailyQuestView = false;
        ApplyDailyLayout(false);
        //ShowSingleRewardButtonOnly();
        if (rewardButton != null)
        {
            rewardButton.gameObject.SetActive(true);
            rewardButton.interactable = false;
        }


        // 0) 데이터가 없으면 깨끗이 지우고 끝
        if (data == null)
        {
            Clear();
            return;
        }

        // 1) 제목 / 본문 설명 세팅 (+보상 슬롯)
        titleText.text = data.title;
        descText.text = data.questDesc;

        // 보상 박스는 기본 표시
        if (rewardBoxRoot != null) rewardBoxRoot.SetActive(true);

        // 보상 슬롯 UI 채우기
        if (data.type == QuestType.Main) UpdateRewardUI_Main(data);
        else if (data.type == QuestType.Sub) UpdateRewardUI_Sub(data);

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
        SetDetailVisible(true);

        isDailyQuestView = true;
        ApplyDailyLayout(true);

        var orderedDaily = OrderDailyQuestsForUI(dailyQuests);
        currentDailyQuests = orderedDaily;

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

        // 단일 버튼 비활성화 
        if (orderedDaily == null || orderedDaily.Count == 0)
        {
            if (rewardButton != null)
            {
                rewardButton.gameObject.SetActive(true);
                rewardButton.interactable = false;
            }

            HideAllRewardSlots();
            if (rewardBoxRoot != null) rewardBoxRoot.SetActive(false);

            return;
        }

        //// 3) 입력이 없으면 여기서 끝
        //if (dailyQuests == null || dailyQuests.Count == 0)
        //{
        //    SetAllRewardButtons(false);
        //    return;
        //}

        // 4) 최대 3개까지만 표시
        if (conditionRows.Length < DailyQuestSlotCount || conditionTexts.Length < DailyQuestSlotCount)
        {
            Debug.LogWarning($"일일퀘스트는 {DailyQuestSlotCount}개 슬롯이 필요합니다. Hierarchy에서 conditionRow와 Text 슬롯을 추가하세요.");
        }

        int rowCount = Mathf.Min(DailyQuestSlotCount, orderedDaily.Count, conditionRows.Length, conditionTexts.Length);

        bool anyClaimable = false;
        for (int i = 0; i < rowCount; i++)
        {
            QuestData q = orderedDaily[i];

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

            bool canClaim = completed && q != null && !q.rewardClaimed && q.state != QuestState.Closed;
            anyClaimable |= canClaim;

            //// 일일퀘스트 버튼(0~2): 달성 && 미수령일 때만 활성
            //if (rewardButtons != null && i < rewardButtons.Length && rewardButtons[i] != null)
            //{
            //    bool canClaim = completed && q != null && !q.rewardClaimed && q.state != QuestState.Closed;
            //    rewardButtons[i].gameObject.SetActive(true);
            //    rewardButtons[i].interactable = canClaim;
            //}

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

        //// 남는 버튼 숨김
        //if (rewardButtons != null)
        //{
        //    for (int i = rowCount; i < rewardButtons.Length; i++)
        //    {
        //        if (rewardButtons[i] != null)
        //        {
        //            rewardButtons[i].gameObject.SetActive(false);
        //            rewardButtons[i].interactable = false;
        //        }
        //    }
        //}

        // 공용 '보상받기' 버튼: 활성화된 보상(=canClaim) 이 하나라도 있으면 활성
        if (rewardButton != null)
        {
            rewardButton.gameObject.SetActive(true);
            //rewardButton.interactable = anyClaimable;

            if (rewardBoxRoot != null) rewardBoxRoot.SetActive(true);
            UpdateRewardUI_Daily(orderedDaily);

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

        CacheRowPositionsIfNeeded();
        ApplyDailyRowSpacing(isDaily);
    }

    public void RefreshConditions()
    {
        //Button rewardButton = GetSingleRewardButton(); // rewardButtons[1]

        if (currentQuest == null)
        {
            if (rewardButton != null) rewardButton.interactable = false;
            return;
        }

        // 이미 보상 받았거나 닫힌 퀘스트면 버튼 비활성
        if (currentQuest.rewardClaimed || currentQuest.state == QuestState.Closed)
        {
            if (rewardButton != null) rewardButton.interactable = false;
            return;
        }

        //// 아이템 보상이 아니면(땅 확장 등) 이번 범위에서는 버튼 비활성
        //if (currentQuest.type != QuestType.Daily && currentQuest.rewardItem == null)
        //{
        //    if (rewardButton != null) rewardButton.interactable = false;
        //    return;
        //}


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
            if (i < conditionStrikeLine.Length && conditionStrikeLine[i] != null)
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

        // 추가: 메인/서브는 조건 변화에 따라 아이콘도 즉시 갱신
        if (currentQuest.type == QuestType.Main) UpdateRewardUI_Main(currentQuest);
        else if (currentQuest.type == QuestType.Sub) UpdateRewardUI_Sub(currentQuest);
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

        //var btn = GetSingleRewardButton();
        //if (btn != null) btn.interactable = false;
        if (rewardButton != null) rewardButton.interactable = false;

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
            rewardButton.onClick.AddListener(OnClickReward);
        }

        //if (rewardButtons == null) return;

        //for (int i = 0; i < rewardButtons.Length; i++)
        //{
        //    int idx = i;
        //    if (rewardButtons[idx] == null) continue;

        //    rewardButtons[idx].onClick.RemoveAllListeners();
        //    rewardButtons[idx].onClick.AddListener(() => OnClickReward(idx));
        //}
    }

    void OnClickReward()
    {
        // 단일 퀘스트(메인/서브)
        if (!isDailyQuestView)
        {
            if (currentQuest == null) return;

            if (QuestManager.Instance != null && QuestManager.Instance.ClaimRewardMainSub(currentQuest))
            {
                UIManager.Instance?.ShowAlertPopup("보상이 지급되었습니다.");
                RefreshConditions();
            }
            return;
        }

        // 일일퀘스트: "활성화된 보상"만 일괄 수령
        if (currentDailyQuests == null) return;

        int claimed = QuestManager.Instance != null
            ? QuestManager.Instance.ClaimRewardsDailyBatch(currentDailyQuests)
            : 0;

        if (claimed > 0)
        {
            UIManager.Instance?.ShowAlertPopup("보상이 지급되었습니다.");
            ShowDailyQuests(currentDailyQuests); // 버튼/체크 갱신
        }
    }

    //private void OnClickReward(int index)
    //{
    //    // 단일 퀘스트 화면: 1번 버튼만 동작
    //    if (!isDailyQuestView)
    //    {
    //        if (index != 1) return;
    //        if (currentQuest == null) return;

    //        if (QuestManager.Instance != null && QuestManager.Instance.ClaimRewardMainSub(currentQuest))
    //        {
    //            UIManager.Instance?.ShowAlertPopup("보상이 지급되었습니다.");
    //            RefreshConditions();
    //        }
    //        return;
    //    }

    //    // 일일퀘스트 화면: 0~2 버튼이 각각 해당 퀘스트를 처리
    //    if (currentDailyQuests == null) return;
    //    if (index < 0 || index >= currentDailyQuests.Count) return;

    //    QuestData q = currentDailyQuests[index];
    //    if (q == null) return;

    //    if (QuestManager.Instance != null && QuestManager.Instance.ClaimRewardDaily(q))
    //    {
    //        UIManager.Instance?.ShowAlertPopup("보상이 지급되었습니다.");
    //        ShowDailyQuests(currentDailyQuests); // 진행도/체크/버튼까지 재갱신
    //    }
    //}

    //private Button GetSingleRewardButton()
    //{
    //    if (rewardButtons == null) return null;
    //    if (rewardButtons.Length <= 1) return null;
    //    return rewardButtons[1];
    //}

    //private void SetAllRewardButtons(bool active)
    //{
    //    if (rewardButtons == null) return;
    //    for (int i = 0; i < rewardButtons.Length; i++)
    //    {
    //        if (rewardButtons[i] == null) continue;
    //        rewardButtons[i].gameObject.SetActive(active);
    //        rewardButtons[i].interactable = false;
    //    }
    //}

    //private void ShowSingleRewardButtonOnly()
    //{
    //    if (rewardButtons == null) return;

    //    for (int i = 0; i < rewardButtons.Length; i++)
    //    {
    //        if (rewardButtons[i] == null) continue;

    //        bool isSingle = (i == 1);
    //        rewardButtons[i].gameObject.SetActive(isSingle);
    //        rewardButtons[i].interactable = false;
    //    }
    //}

    // 지난 메인 퀘스트 UI 
    public void ShowClosed(QuestData data)
    {
        SetDetailVisible(true);

        currentQuest = data;
        isDailyQuestView = false;
        ApplyDailyLayout(false);

        if (descText != null) descText.gameObject.SetActive(true);

        // 제목/설명
        if (data == null)
        {
            Clear();
            return;
        }

        if (titleText != null) titleText.text = data.title;
        if (descText != null) descText.text = data.questDesc;

        // 보상 UI 숨김
        if (rewardButton != null) rewardButton.gameObject.SetActive(false);
        if (rewardBoxRoot != null) rewardBoxRoot.SetActive(false);

        // 조건행 초기화
        for (int i = 0; i < conditionRows.Length; i++)
        {
            if (conditionRows[i] != null) conditionRows[i].SetActive(false);
            if (i < conditionCheckOn.Length && conditionCheckOn[i] != null) conditionCheckOn[i].SetActive(false);
            if (i < conditionStrikeLine.Length && conditionStrikeLine[i] != null) conditionStrikeLine[i].SetActive(false);
        }

        // 조건을 "전부 완료"로 표시
        int count = Mathf.Min(
            data.conditionTexts != null ? data.conditionTexts.Length : 0,
            data.targetCounts != null ? data.targetCounts.Length : 0,
            conditionRows.Length,
            conditionTexts.Length,
            conditionCheckOn.Length,
            conditionStrikeLine.Length
        );

        for (int i = 0; i < count; i++)
        {
            if (conditionRows[i] != null) conditionRows[i].SetActive(true);

            int target = data.targetCounts[i];
            int cur = target; // 과거 퀘스트는 완료 처리

            if (conditionTexts[i] != null)
                conditionTexts[i].text = $"{data.conditionTexts[i]} ({cur}/{target})";

            if (i < conditionStrikeLine.Length && conditionStrikeLine[i] != null)
                conditionStrikeLine[i].SetActive(true);

            if (i < conditionCheckOn.Length && conditionCheckOn[i] != null)
                conditionCheckOn[i].SetActive(true);
        }
    }

    private void HideAllRewardSlots()
    {
        if (rewardSlotRoots == null) return;

        for (int i = 0; i < rewardSlotRoots.Length; i++)
            if (rewardSlotRoots[i] != null) rewardSlotRoots[i].SetActive(false);

        if (rewardClaimCheckOn != null)
        {
            for (int i = 0; i < rewardClaimCheckOn.Length; i++)
                if (rewardClaimCheckOn[i] != null) rewardClaimCheckOn[i].SetActive(false);
        }
    }


    private void SetRewardSlot(int idx, Sprite icon, string textOrEmpty)
    {
        if (rewardSlotRoots == null || idx < 0 || idx >= rewardSlotRoots.Length) return;

        if (rewardSlotRoots[idx] != null) rewardSlotRoots[idx].SetActive(true);

        if (rewardSlotIcons != null && idx < rewardSlotIcons.Length && rewardSlotIcons[idx] != null)
            rewardSlotIcons[idx].sprite = icon;

        if (rewardSlotTexts != null && idx < rewardSlotTexts.Length && rewardSlotTexts[idx] != null)
        {
            bool hasText = !string.IsNullOrEmpty(textOrEmpty);
            rewardSlotTexts[idx].text = hasText ? textOrEmpty : "";
            rewardSlotTexts[idx].gameObject.SetActive(hasText);
        }
    }

    // 보상 아이콘 찾기 (on, off)
    private Sprite TryGetItemIcon(ItemData item)
    {
        return item != null ? item.itemIcon : null;
    }

    private Sprite TryGetItemIconOff(ItemData item)
    {
        if (item == null) return null;
        return item.itemIconOffVer != null ? item.itemIconOffVer : item.itemIcon;
    }



    private bool IsQuestClaimable(QuestData q)
    {
        if (q == null) return false;
        if (q.rewardClaimed || q.state == QuestState.Closed) return false;

        if (q.targetCounts == null || q.currentCounts == null) return false;

        int count = Mathf.Min(q.targetCounts.Length, q.currentCounts.Length);
        if (count <= 0) return false;

        for (int i = 0; i < count; i++)
        {
            int target = q.targetCounts[i];
            int cur = q.currentCounts[i];
            if (!(target > 0 && cur >= target)) return false;
        }
        return true;
    }

    private bool IsDailyClaimable(QuestData q)
    {
        if (q == null) return false;
        if (q.rewardClaimed || q.state == QuestState.Closed) return false;

        int cur = 0;
        int target = 0;

        if (q.currentCounts != null && q.currentCounts.Length > 0) cur = q.currentCounts[0];
        else cur = q.currentCount;

        if (q.targetCounts != null && q.targetCounts.Length > 0) target = q.targetCounts[0];

        return (target > 0) && (cur >= target);
    }

    private bool IsDailyCompleted(QuestData q)
    {
        if (q == null) return false;
        if (q.state == QuestState.Closed) return false;

        int cur = (q.currentCounts != null && q.currentCounts.Length > 0) ? q.currentCounts[0] : q.currentCount;
        int target = (q.targetCounts != null && q.targetCounts.Length > 0) ? q.targetCounts[0] : 0;

        return (target > 0) && (cur >= target);
    }


    private Sprite TryGetItemIconOff(object item)
    {
        if (item == null) return null;

        string[] names = { "iconOff", "IconOff", "spriteOff", "SpriteOff", "itemSpriteOff", "ItemSpriteOff", "itemIconOff", "ItemIconOff" };
        var t = item.GetType();

        foreach (var n in names)
        {
            var f = t.GetField(n, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null && typeof(Sprite).IsAssignableFrom(f.FieldType))
                return f.GetValue(item) as Sprite;

            var p = t.GetProperty(n, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (p != null && typeof(Sprite).IsAssignableFrom(p.PropertyType))
                return p.GetValue(item) as Sprite;
        }
        return null;
    }


    // 메인 1칸 고정 
    private void UpdateRewardUI_Main(QuestData q)
    {
        HideAllRewardSlots();
        if (q == null) return;

        bool on = IsQuestClaimable(q);

        Sprite land = on ? iconLand : (iconLandOff != null ? iconLandOff : iconLand);
        Sprite nat = on ? iconNat : (iconNatOff != null ? iconNatOff : iconNat);
        Sprite sap = on ? iconSap : (iconSapOff != null ? iconSapOff : iconSap);
        Sprite gawi = on ? iconGawi : (iconGawiOff != null ? iconGawiOff : iconGawi);

        switch (q.key)
        {
            case 1: SetRewardSlot(1, land, ""); break;
            case 2: SetRewardSlot(1, nat, ""); break;
            case 3: SetRewardSlot(1, land, ""); break;
            case 4: SetRewardSlot(1, sap, ""); break;
            case 5: SetRewardSlot(1, land, ""); break;
            case 6: SetRewardSlot(1, gawi, ""); break;
            default: SetRewardSlot(1, land, ""); break;
        }
    }

    // 서브 2칸 : 씨앗 + 포잉 
    private void UpdateRewardUI_Sub(QuestData q)
    {
        HideAllRewardSlots();
        if (q == null) return;

        bool on = IsQuestClaimable(q);

        Sprite seedOn = TryGetItemIcon(q.rewardItem) ?? defaultItemSprite;
        Sprite seedOff = TryGetItemIconOff(q.rewardItem) ?? defaultItemSpriteOff ?? defaultItemSprite;

        Sprite seedIcon = on ? seedOn : seedOff;
        Sprite poingIcon = on ? iconPoing : (iconPoingOff != null ? iconPoingOff : iconPoing);

        SetRewardSlot(3, seedIcon, "");
        SetRewardSlot(4, poingIcon, $"{q.rewardPoing}");

    }

    // 일일 3칸 
    private void UpdateRewardUI_Daily(List<QuestData> daily)
    {
        HideAllRewardSlots();
        if (daily == null) return;

        QuestData qPoing = null;
        QuestData qFert = null;
        QuestData qPotion = null;

        // rewardKey로 A/B/C 분류
        foreach (var q in daily)
        {
            if (q == null) continue;

            if (q.rewardKey >= 100) qPotion = q;        // 물약
            else if (q.rewardKey == 0) qPoing = q;      // 포잉
            else if (q.rewardKey == 1) qFert = q;       // 비료
        }

        bool anyOn = false;

        // A: 포잉
        if (qPoing != null)
        {
            bool completedA = IsDailyCompleted(qPoing);
            bool claimableA = completedA && !qPoing.rewardClaimed && qPoing.state != QuestState.Closed;
            anyOn |= claimableA;

            // 완료면 항상 on 아이콘 유지
            Sprite iconA = completedA ? iconPoing : (iconPoingOff != null ? iconPoingOff : iconPoing);

            SetRewardSlot(0, iconA, $"{qPoing.rewardAmount}");
            SetRewardClaimCheck(0, qPoing.rewardClaimed);
        }

        // B: 비료
        if (qFert != null)
        {
            bool completedB = IsDailyCompleted(qFert);
            bool claimableB = completedB && !qFert.rewardClaimed && qFert.state != QuestState.Closed;
            anyOn |= claimableB;

            // 완료면 항상 on 아이콘 유지
            Sprite iconB = completedB ? iconFertilizer : (iconFertilizerOff != null ? iconFertilizerOff : iconFertilizer);

            SetRewardSlot(1, iconB, $"{qFert.rewardAmount}");
            SetRewardClaimCheck(1, qFert.rewardClaimed); 
        }

        // C: 물약
        if (qPotion != null)
        {
            bool completedC = IsDailyCompleted(qPotion);
            bool claimableC = completedC && !qPotion.rewardClaimed && qPotion.state != QuestState.Closed;
            anyOn |= claimableC;

            int idx = qPotion.rewardKey - 100;
            Sprite onIcon = (idx >= 0 && idx < potionIcons.Length) ? potionIcons[idx] : defaultItemSprite;
            Sprite offIcon =
                (potionIconsOff != null && idx >= 0 && idx < potionIconsOff.Length && potionIconsOff[idx] != null)
                ? potionIconsOff[idx]
                : (defaultItemSpriteOff != null ? defaultItemSpriteOff : onIcon);

            // 완료면 항상 on 아이콘 유지
            SetRewardSlot(2, completedC ? onIcon : offIcon, $"{qPotion.rewardAmount}");
            SetRewardClaimCheck(2, qPotion.rewardClaimed); 
        }

        if (rewardButton != null)
            rewardButton.interactable = anyOn; // 수령 가능한 게 있을 때만 true

    }

    // 일일퀘스트 UI 표시 순서를 "포잉 -> 비료 -> 물약"으로 고정
    private List<QuestData> OrderDailyQuestsForUI(List<QuestData> dailyQuests)
    {
        if (dailyQuests == null) return null;

        QuestData qPoing = null;
        QuestData qFert = null;
        QuestData qPotion = null;

        foreach (var q in dailyQuests)
        {
            if (q == null) continue;

            if (q.rewardKey >= 100) qPotion = q;     // 물약(100~)
            else if (q.rewardKey == 0) qPoing = q;   // 포잉(0)
            else if (q.rewardKey == 1) qFert = q;    // 비료(1)
        }

        var orderedDaily = new List<QuestData>(3);
        if (qPoing != null) orderedDaily.Add(qPoing);
        if (qFert != null) orderedDaily.Add(qFert);
        if (qPotion != null) orderedDaily.Add(qPotion);

        return orderedDaily;
    }


    // 일퀘 보상 수령 후 체크표시 
    private void SetRewardClaimCheck(int idx, bool on)
    {
        if (rewardClaimCheckOn == null) return;
        if (idx < 0 || idx >= rewardClaimCheckOn.Length) return;
        if (rewardClaimCheckOn[idx] == null) return;

        rewardClaimCheckOn[idx].SetActive(on);
    }

    // 일일퀘스트 화면에서만 미션 줄 간격 조정 
    private void CacheRowPositionsIfNeeded()
    {
        if (isRowPosCached) return;

        conditionRowNormalPos = new Vector3[conditionRows.Length];

        for (int i = 0; i < conditionRows.Length; i++)
        {
            if (conditionRows[i] == null) continue;

            // RectTransform이 있으면 UI 방식, 없으면 Transform 방식
            var rt = conditionRows[i].GetComponent<RectTransform>();
            if (rt != null) conditionRowNormalPos[i] = rt.anchoredPosition;
            else conditionRowNormalPos[i] = conditionRows[i].transform.localPosition;
        }

        isRowPosCached = true;
    }


    private void ApplyDailyRowSpacing(bool isDaily)
    {
        if (conditionRowNormalPos == null) return;

        // row가 아래로 내려갈수록 y가 줄어드는 구조면 dir = -1
        float dir = -1f;

        if (conditionRows.Length >= 2 && conditionRows[0] != null && conditionRows[1] != null)
        {
            Vector3 p0 = conditionRowNormalPos[0];
            Vector3 p1 = conditionRowNormalPos[1];
            dir = (p1.y < p0.y) ? -1f : 1f;
        }

        for (int i = 0; i < conditionRows.Length; i++)
        {
            if (conditionRows[i] == null) continue;

            Vector3 basePos = conditionRowNormalPos[i];
            Vector3 targetPos = basePos;

            if (isDaily)
            {
                // i가 커질수록 더 벌어지게 (Row0 0, Row1 1배, Row2 2배)
                targetPos += new Vector3(0f, dir * dailyRowExtraSpacing * i, 0f);
            }

            var rt = conditionRows[i].GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = targetPos;
            else conditionRows[i].transform.localPosition = targetPos;
        }
    }

    // closed 된 메인 퀘스트가 0개 일 때, 지난 퀘스트 화면을 누르면, 
    public void SetDetailVisible(bool visible)
    {
        if (titleText != null) titleText.gameObject.SetActive(visible);

        if (descText != null) descText.gameObject.SetActive(visible);

        if (conditionContainer != null) conditionContainer.gameObject.SetActive(visible);

        if (rewardButton != null) rewardButton.gameObject.SetActive(visible);
        if (rewardBoxRoot != null) rewardBoxRoot.SetActive(visible);
    }

    // 지난퀘스트(Closed) 리스트가 비었을 때 전용
    public void ShowEmptyRightPanel()
    {
        Clear();                // 텍스트/행들 정리
        SetDetailVisible(false); // "아예 안 보이게"
    }


}