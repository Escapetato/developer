using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("퀘스트 데이터베이스 (정적 데이터)")]
    public QuestDatabase questDatabase;

    [Header("원본 퀘스트 리스트 (런타임용)")]
    public List<QuestData> allQuestList = new List<QuestData>();

    [Header("타입별 퀘스트 리스트")]
    public List<QuestData> mainQuests = new List<QuestData>();
    public List<QuestData> subQuests = new List<QuestData>();
    public List<QuestData> dailyQuests = new List<QuestData>();

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

    // DB에서 퀘스트 정보 가져오기 
    private void LoadFromDatabase()
    {
        allQuestList.Clear();

        if (questDatabase == null)
        {
            Debug.LogWarning("[QuestManager] QuestDatabase가 연결되지 않았습니다. Inspector를 확인하세요.");
            return;
        }

        foreach (var q in questDatabase.quests)
        {
            if (q == null) continue;
            allQuestList.Add(q);   
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
        RandomDailyQuestSlot(3);
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


    // 일일 퀘스트
    // 하루 기준으로 전부 리셋, "그룹당 최대 1개" 규칙으로 랜덤 오픈
    private void RandomDailyQuestSlot(int targetCount)
    {
        // 1) 상태 리셋
        foreach (var q in dailyQuests)
        {
            if (q == null) continue;

            q.state = QuestState.Locked;
            q.currentCount = 0;
            q.rewardClaimed = false;
        }

        // 2) 그룹별로 묶기
        var groupMap = new Dictionary<int, List<QuestData>>();

        foreach (var q in dailyQuests)
        {
            if (q == null) continue;

            int groupId = (q.key - 1) / 3;

            if (!groupMap.ContainsKey(groupId))
            {
                groupMap[groupId] = new List<QuestData>();
            }

            groupMap[groupId].Add(q);
        }

        if (groupMap.Count == 0)
        {
            Debug.Log("[QuestManager] 일일 퀘스트 후보 그룹이 없습니다.");
            return;
        }

        // 3) 그룹 순서를 랜덤 섞기
        var groupIds = new List<int>(groupMap.Keys);
        for (int i = 0; i < groupIds.Count; i++)
        {
            int swapIndex = UnityEngine.Random.Range(i, groupIds.Count);
            int tmp = groupIds[i];
            groupIds[i] = groupIds[swapIndex];
            groupIds[swapIndex] = tmp;
        }

        int opened = 0;
        int maxOpen = Mathf.Min(targetCount, groupIds.Count);

        // 4) 각 그룹에서 1개씩만 랜덤 선택하여 오픈
        for (int i = 0; i < maxOpen; i++)
        {
            int groupId = groupIds[i];
            List<QuestData> group = groupMap[groupId];

            if (group == null || group.Count == 0)
                continue;

            int pickIndex = UnityEngine.Random.Range(0, group.Count);
            QuestData q = group[pickIndex];

            q.state = QuestState.Active;
            Debug.Log($"[QuestManager] 오늘의 일일 퀘스트 오픈: key={q.key}, title={q.title}, group={groupId}");

            opened++;
        }

        Debug.Log($"[QuestManager] 오늘 일일 퀘스트 개수: {opened}/{targetCount}");
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
}
