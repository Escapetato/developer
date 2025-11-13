using System;
using UnityEngine;

[Serializable] 
public enum QuestType
{
    Main, 
    Sub, 
    Daily   
}

[Serializable] 
public enum QuestState
{
    Locked, 
    Active,     
    Completed, 
    Closed    
}

[Serializable] 
public class QuestData
{
    [Header("기본 정보")]
    public int key;              // 고유 ID
    public string title;        // 퀘스트 이름
    public QuestType type;      // 메인 / 서브 / 일일
    [TextArea]
    public string questDesc;  // 퀘스트 설명

    [Header("달성 조건")]
    public int conditionCount;       // 목표 수치 

    [Header("보상 정보")]
    public int rewardKey;    // 보상 아이템 ID
    public int rewardAmount;    // 보상 수량

    [Header("체인 연결")]
    public int nextKey;     // 다음 퀘스트 ID (없으면 0이나 -1로 처리)
}

