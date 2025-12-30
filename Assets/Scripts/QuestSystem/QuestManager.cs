using System;
using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("퀘스트 데이터베이스 (정적 데이터)")]
    public QuestDatabase questDatabase;

    [Header("원본 퀘스트 리스트")]
    public List<QuestData> allQuestList = new List<QuestData>();

    [Header("타입별 퀘스트 리스트")]
    public List<QuestData> mainQuests = new List<QuestData>();
    public List<QuestData> subQuests = new List<QuestData>();
    public List<QuestData> dailyQuests = new List<QuestData>();

    [Header("일일퀘스트 보상 아이템 매핑")]
    [SerializeField] private ItemData dailyFertilizerItem;   // 비료 ItemData (1개)
    [SerializeField] private ItemData[] dailyPotionItems;    // 포션 ItemData 8개 (불~무지개 순서)

    // 한 플레이 세션에서 한 번만 초기화 
    private bool initialized = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(transform.root.gameObject); // 씬이 바뀌어도 유지 

        InitializeIfNeeded();
    }

    private void InitializeIfNeeded()
    {
        if (initialized) return;
        initialized = true;

        LoadFromDatabase();
        BuildQuestLists();
        InitializeQuestStates();
        OpenInitialSlots();

        Debug.Log($"[QuestManager] 초기 슬롯 오픈 완료 - " +
                  $"메인 Active={CountActive(mainQuests)}, " +
                  $"서브 Active={CountActive(subQuests)}, " +
                  $"일일 Active={CountActive(dailyQuests)}");
    }

    private QuestData CloneQuest(QuestData src)
    {
        var dst = new QuestData();

        // 기본 정보
        dst.key = src.key;
        dst.title = src.title;
        dst.type = src.type;
        dst.questDesc = src.questDesc;

        // 조건(배열은 깊은 복사)
        dst.conditionTexts = src.conditionTexts != null ? (string[])src.conditionTexts.Clone() : null;
        dst.targetCounts = src.targetCounts != null ? (int[])src.targetCounts.Clone() : null;

        // 런타임 상태(새로 생성)
        dst.state = src.state;               // 혹은 Locked로 통일해도 됨(InitializeQuestStates가 어차피 초기화)
        dst.currentCount = src.currentCount;
        dst.rewardClaimed = src.rewardClaimed;
        dst.currentCounts = (dst.targetCounts != null) ? new int[dst.targetCounts.Length] : null;

        // 보상/체인
        dst.rewardKey = src.rewardKey;
        dst.rewardAmount = src.rewardAmount;
        dst.rewardPoing = src.rewardPoing;
        dst.nextKey = src.nextKey;
        dst.rewardItem = src.rewardItem;

        // 일일 퀘스트 설정(참조는 그대로 둬도 OK)
        dst.dailyDifficulty = src.dailyDifficulty;
        dst.dailyTargets = src.dailyTargets;

        return dst;
    }

    // DB에서 퀘스트 정보 가져오기 
    private void LoadFromDatabase()
    {
        allQuestList.Clear();

        if (questDatabase == null)
        {
            Debug.LogWarning("[QuestManager] QuestDatabase가 연결되지 않았습니다.");
            return;
        }

        foreach (var q in questDatabase.quests)
        {
            if (q == null) continue;
            allQuestList.Add(CloneQuest(q)); // DB 원본 건드리지 않고 클론해서 사용
        }

        Debug.Log($"[QuestManager] QuestDatabase에서 {allQuestList.Count}개 퀘스트 로드.");
    }

    // 퀘스트 타입 분류 + KEY 기준 오름차순 정렬  
    private void BuildQuestLists()
    {
        mainQuests.Clear();
        subQuests.Clear();
        dailyQuests.Clear();

        foreach (var q in allQuestList)
        {
            if (q == null) continue;

            switch (q.type)
            {
                case QuestType.Main:
                    mainQuests.Add(q);
                    break;

                case QuestType.Sub:
                    subQuests.Add(q);
                    break;

                case QuestType.Daily:
                    dailyQuests.Add(q);
                    break;
            }
        }

        mainQuests.Sort((a, b) => a.key.CompareTo(b.key));
        subQuests.Sort((a, b) => a.key.CompareTo(b.key));
        dailyQuests.Sort((a, b) => a.key.CompareTo(b.key));
    }

    // 모든 퀘스트의 런타임 상태 초기화 
    // 추후 세이브 불러오기로 수정 필요 
    private void InitializeQuestStates()
    {
        foreach (var q in allQuestList)
        {
            if (q == null) continue;

            if (q.state == QuestState.Closed)
            {
                // 끝난 메인 퀘스트 슬롯 테스트 : Closed 로 시작 후 유지 
                continue;
            }

            q.state = QuestState.Locked;
            q.currentCount = 0;
            q.rewardClaimed = false;
        }
    }

    // 초기 퀘스트 슬롯 오픈 
    private void OpenInitialSlots()
    {
        MainQuestSlot();
        SubQuestSlot(2);

        // 일일 퀘스트는 별도 로직(DailyQuestSelector)에서 3개(A/B/C) 생성
        DailyQuestSelector.ConfigureDailyQuests(dailyQuests, 3);
        Debug.Log("[QuestManager] 일일 퀘스트 생성 완료 (DailyQuestSelector)");
    }

    // 메인 퀘스트 슬롯 관리
    // 1. Closed 가 아닌 메인 퀘스트가 있다면 유지 
    // 2. 없다면 Locked 중 가장 작은 key 값의 퀘스트 1개 오픈 
    private void MainQuestSlot()
    {
        foreach (var q in mainQuests)
        {
            if (q.state == QuestState.Active || q.state == QuestState.Completed)
            {
                Debug.Log($"[QuestManager] 기존 메인 퀘스트 유지: key={q.key}, title={q.title}, state={q.state}");
                return;
            }
        }

        foreach (var q in mainQuests)
        {
            if (q.state == QuestState.Locked)
            {
                q.state = QuestState.Active;
                Debug.Log($"[QuestManager] 메인 퀘스트 새로 오픈: key={q.key}, title={q.title}");
                return;
            }
        }

        Debug.Log("[QuestManager] 열 수 있는 메인 퀘스트가 없습니다.");
    }


    // 서브 퀘스트 슬롯 관리
    private void SubQuestSlot(int targetCount)
    {
        int openCount = 0;

        foreach (var q in subQuests)
        {
            if (q.state == QuestState.Active || q.state == QuestState.Completed)
                openCount++;
        }

        foreach (var q in subQuests)
        {
            if (openCount >= targetCount)
                break;

            if (q.state == QuestState.Locked)
            {
                q.state = QuestState.Active;
                openCount++;
                Debug.Log($"[QuestManager] 서브 퀘스트 오픈: key={q.key}, title={q.title}");
            }
        }

        Debug.Log($"[QuestManager] 서브 퀘스트 슬롯 상태: 진행중 {openCount}/{targetCount}");
    }


    // 퀘스트 개수 세기 (Active, Completed)
    private int CountActive(List<QuestData> list)
    {
        int cnt = 0;

        foreach (var q in list)
        {
            if (q == null) continue;

            if (q.state == QuestState.Active || q.state == QuestState.Completed)
                cnt++;
        }

        return cnt;
    }

    public event Action OnQuestChanged;

    private bool IsCompletedByCounts(QuestData q)
    {
        if (q == null || q.targetCounts == null || q.currentCounts == null) return false;

        int n = Mathf.Min(q.targetCounts.Length, q.currentCounts.Length);
        if (n == 0) return false;

        for (int i = 0; i < n; i++)
        {
            int target = q.targetCounts[i];
            int cur = q.currentCounts[i];
            if (target > 0 && cur < target) return false;
        }
        return true;
    }

    // 메인/서브 퀘스트 - 보상 로직 
    public bool ClaimRewardMainSub(QuestData quest)
    {
        if (quest == null) return false;

        // ⚠️ 일일 제외
        if (quest.type == QuestType.Daily) return false;

        // 이미 받았으면 종료
        if (quest.rewardClaimed) return false;

        // 완료 안 됐으면 종료 (테스트로 counts 조작하면 통과 가능)
        if (!IsCompletedByCounts(quest)) return false;

        // ⚠️ 땅 확장 구현 필요 
        if (quest.rewardItem == null) return false;

        int amount = Mathf.Max(1, quest.rewardAmount);

        // 인벤 추가, 도감 해금 
        InventoryManager.Instance.AddItem(quest.rewardItem, amount);
        Debug.Log($"[QuestReward] Added {quest.rewardItem.itemName} x{amount}, quest={quest.key} -> Closed");
        GameProgressionManager.Instance?.UnlockItem(quest.rewardItem);

        // 서브 퀘스트: 포잉(정적) 추가 지급
        if (quest.type == QuestType.Sub && quest.rewardPoing > 0)
        {
            PoingManager.Instance.AddPoing(quest.rewardPoing);
            Debug.Log($"[QuestReward] Added Poing +{quest.rewardPoing}, quest={quest.key}");
        }

        // 퀘스트 상태 변경: Closed
        quest.rewardClaimed = true;
        quest.state = QuestState.Closed;

        // 퀘스트 닫힌 뒤 다음 슬롯 자동 오픈 
        if (quest.type == QuestType.Main)
            MainQuestSlot();     // 다음 메인 1개 열기
        else if (quest.type == QuestType.Sub)
            SubQuestSlot(2);     // 서브 2개 유지

        OnQuestChanged?.Invoke();
        return true;
    }
    
    public bool ClaimRewardDaily(QuestData quest)
    {
        if (quest == null) return false;

        // 일일만 처리
        if (quest.type != QuestType.Daily) return false;

        // 이미 받았으면 종료
        if (quest.rewardClaimed) return false;

        // 완료 안 됐으면 종료
        if (!IsCompletedByCounts(quest)) return false;

        int amount = Mathf.Max(1, quest.rewardAmount);

        // rewardKey 규칙:
        // 0 = 포잉, 1 = 비료, 100~107 = 포션(8종)
        const int KEY_POING = 0;
        const int KEY_FERTILIZER = 1;
        const int POTION_BASE = 100;

        // 중복지급 방지: 지급 전에 먼저 true
        quest.rewardClaimed = true;

        if (quest.rewardKey == KEY_POING)
        {
            PoingManager.Instance.AddPoing(amount);
            Debug.Log($"[DailyReward] Added Poing +{amount}, quest={quest.key}");
        }
        else if (quest.rewardKey == KEY_FERTILIZER)
        {
            if (dailyFertilizerItem == null)
            {
                Debug.LogWarning("[DailyReward] dailyFertilizerItem이 연결되지 않았습니다.");
                quest.rewardClaimed = false; // 실패 처리(선택)
                return false;
            }

            InventoryManager.Instance.AddItem(dailyFertilizerItem, amount);
            Debug.Log($"[DailyReward] Added Fertilizer {dailyFertilizerItem.itemName} x{amount}, quest={quest.key}");
        }
        else if (quest.rewardKey >= POTION_BASE)
        {
            int idx = quest.rewardKey - POTION_BASE;

            if (dailyPotionItems == null || idx < 0 || idx >= dailyPotionItems.Length || dailyPotionItems[idx] == null)
            {
                Debug.LogWarning($"[DailyReward] dailyPotionItems 매핑이 잘못되었습니다. key={quest.rewardKey}, idx={idx}");
                quest.rewardClaimed = false; // 실패 처리(선택)
                return false;
            }

            var potionItem = dailyPotionItems[idx];
            InventoryManager.Instance.AddItem(potionItem, amount);
            Debug.Log($"[DailyReward] Added Potion {potionItem.itemName} x{amount}, quest={quest.key}");
        }
        else
        {
            Debug.LogWarning($"[DailyReward] Unknown rewardKey={quest.rewardKey}, quest={quest.key}");
            quest.rewardClaimed = false; // 실패 처리(선택)
            return false;
        }


        OnQuestChanged?.Invoke();
        return true;
    }

}
