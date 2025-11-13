using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("원본 퀘스트 리스트")]
    public List<QuestData> allQuestList = new List<QuestData>();

    [Header("타입별 퀘스트 리스트")]
    public List<QuestData> mainQuests = new List<QuestData>();
    public List<QuestData> subQuests = new List<QuestData>();
    public List<QuestData> dailyQuests = new List<QuestData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // 초기화 -> 세이브 데이터 불러오기로 추후 수정 
    private void Start()
    {
        BuildQuestLists();     
        InitializeQuestStates(); 

        UnityEngine.Debug.Log($"[QuestManager] 메인 {mainQuests.Count}, 서브 {subQuests.Count}, 일일 {dailyQuests.Count} 개 분류 완료");

        foreach (var q in allQuestList)
        {
            if (q == null) continue;
            UnityEngine.Debug.Log($"[QuestManager] 초기 상태 확인 - key={q.key}, type={q.type}, state={q.state}, current={q.currentCount}, reward={q.rewardClaimed}");
        }

    }

    // 퀘스트 타입 분류 
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
    }

    // 모든 퀘스트의 런타임 상태 초기화 
    private void InitializeQuestStates()
    {
        foreach (var q in allQuestList)
        {
            if (q == null) continue;

            q.state = QuestState.Locked;
            q.currentCount = 0;
            q.rewardClaimed = false;
        }
    }
}
