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

    [Header("UI 상태")]
    public bool isNewlyOpened = false;   // 새로 열린 퀘스트(빨간 점)

    [Header("달성 조건")]
    public string[] conditionTexts;  // 조건 설명들
    public int[] targetCounts;       // 각 조건의 목표 수치
    public int[] currentCounts;      // 각 조건의 현재 수치

    [Header("달성 미션 진행도 추적")]
    public QuestConditionType[] conditionTypes; // ⭐ 위 배열들과 같은 길이
    public ItemData[] conditionItems;           // ⭐ 특정 아이템 조건이면 넣고, 아니면 null
    public bool[] countByAmount;                // ⭐ 판매/구매/비료: true면 개수(amount)로, false면 1회로 카운트

    [Header("보상 정보")]
    public ItemData rewardItem;
    public int rewardKey; // 보상 아이템 ID
    public int rewardAmount; // 보상 수량

    [Header("보상(포잉)")]
    [Min(0)] public int rewardPoing = 0;   // 서브퀘스트에서 사용할 정적 포잉 보상

    [Header("체인 연결")]
    public int nextKey; // 다음 퀘스트 ID (없으면 0이나 -1로 처리)
    
    [Header("일일 퀘스트 설정")]
    public DailyQuestDifficulty dailyDifficulty = DailyQuestDifficulty.Low;
    public DailyQuestTargetConfig dailyTargets;
}

// (밭) 심기, 수확, 비료 주입
// (상점) 판매, 구매(수확도구,꾸미기테마,작물 씨앗,물약)
// (연구실) 진화 성공, (5연속 성공), 진화 실패 
// (포잉) 골드 소비 
// (기타) 7일 연속 접속 

public enum QuestConditionType
{
    None = 0,

    // (밭)
    PlantCrop,
    HarvestCrop,
    UseFertilizer,

    // (상점)
    SellItem,
    BuySeed,
    BuyPotion,
    BuyTool,     // 수확도구 포함
    BuyTheme,

    // (연구실)
    EvolveSuccess,
    EvolveFail,
    EvolveSuccessStreak,  // 5연속 성공

    // (포잉)
    SpendPoing       // 골드 소비(금액 누적)
}
