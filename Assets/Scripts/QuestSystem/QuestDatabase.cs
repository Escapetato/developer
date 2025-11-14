using System.Collections.Generic;
using UnityEngine;

// 퀘스트 공통 목록 (정의 + 기본 상태)
[CreateAssetMenu(fileName = "QuestDatabase", menuName = "Data/Quest Database")]
public class QuestDatabase : ScriptableObject
{
    public List<QuestData> quests = new List<QuestData>();
}
