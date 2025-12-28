using System.Collections;
using UnityEngine;

[System.Serializable] 
public enum QuestType
{
    Main, 
    Sub, 
    Daily   
}

[System.Serializable] 
public enum QuestState
{
    Locked, 
    Active,     
    Completed, 
    Closed    
}

// 퀘스트 데이터 구조 정의
[System.Serializable]
public class QuestData
{
    [Header("기본 정보")]
    public int key; // 고유 ID
    public string title; // 퀘스트 이름
    public QuestType type; // 메인 / 서브 / 일일
    [TextArea]
    public string questDesc; // 퀘스트 설명

    [Header("런타임 상태")]
    public QuestState state = QuestState.Locked; // Locked / Active / Completed / Closed
    public int currentCount = 0; // 현재 달성 수치 
    public bool rewardClaimed = false; // 보상 수령 여부 

    [Header("달성 조건")]
    public string[] conditionTexts;  // 조건 설명들
    public int[] targetCounts;       // 각 조건의 목표 수치
    public int[] currentCounts;      // 각 조건의 현재 수치

    [Header("보상 정보")]
    public ItemData rewardItem;
    public int rewardKey; // 보상 아이템 ID
    public int rewardAmount; // 보상 수량

    [Header("체인 연결")]
    public int nextKey; // 다음 퀘스트 ID (없으면 0이나 -1로 처리)
    
    [Header("일일 퀘스트 설정")]
    public DailyQuestDifficulty dailyDifficulty = DailyQuestDifficulty.Low;
    public DailyQuestTargetConfig dailyTargets;
}

