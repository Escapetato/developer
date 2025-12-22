using System;
using System.Collections.Generic;
using UnityEngine;

public enum DailyQuestDifficulty
{
    Low,
    Medium,
    High
}

public enum DailyRewardType
{
    Poing,
    Fertilizer,
    Potion
}

[Serializable]
public class DailyQuestTargetConfig
{
    public int lowTarget = 1;
    public int mediumTarget = 1;
    public int highTarget = 1;

    public int GetTarget(DailyQuestDifficulty difficulty)
    {
        switch (difficulty)
        {
            case DailyQuestDifficulty.High:
                return highTarget;
            case DailyQuestDifficulty.Medium:
                return mediumTarget;
            default:
                return lowTarget;
        }
    }
}

public static class DailyQuestSelector
{
    private static readonly DailyQuestDifficulty[] difficultyPermutation =
    {
        DailyQuestDifficulty.High,
        DailyQuestDifficulty.Medium,
        DailyQuestDifficulty.Low
    };

    private static readonly DailyRewardType[] slotRewards =
    {
        DailyRewardType.Poing,
        DailyRewardType.Fertilizer,
        DailyRewardType.Potion
    };

    private static readonly string[] potionTypes = { "물", "불", "바람" };

    public static void ConfigureDailyQuests(List<QuestData> dailyQuests, int targetCount)
    {
        if (dailyQuests == null || dailyQuests.Count == 0)
        {
            Debug.Log("[DailyQuestSelector] 일일 퀘스트 데이터가 없습니다.");
            return;
        }

        ResetDailyQuestStates(dailyQuests);

        List<QuestData> candidates = new List<QuestData>(dailyQuests);
        Shuffle(candidates);

        List<DailyQuestDifficulty> diffOrder = new List<DailyQuestDifficulty>(difficultyPermutation);
        Shuffle(diffOrder);

        int openMax = Mathf.Min(targetCount, slotRewards.Length, diffOrder.Count, candidates.Count);
        for (int i = 0; i < openMax; i++)
        {
            QuestData quest = candidates[i];
            DailyQuestDifficulty difficulty = diffOrder[i];
            DailyRewardType rewardType = slotRewards[i];

            DailyRewardInfo rewardInfo = BuildRewardInfo(rewardType, difficulty);
            int target = GetTargetCount(quest, difficulty);

            quest.dailyDifficulty = difficulty;
            quest.title = $"{quest.title} {target}회";
            quest.questDesc = $"난이도: {GetDifficultyLabel(difficulty)}\n보상: {rewardInfo.label} x{rewardInfo.amount}";
            quest.targetCounts = new[] { target };
            quest.currentCounts = new[] { 0 };

            quest.rewardKey = rewardInfo.rewardKey;
            quest.rewardAmount = rewardInfo.amount;
            quest.state = QuestState.Active;
            quest.currentCount = 0;
            quest.rewardClaimed = false;

            Debug.Log($"[DailyQuestSelector] 오늘의 일일 퀘스트 오픈: slot={(char)('A' + i)}, key={quest.key}, diff={difficulty}, reward={rewardInfo.label} x{rewardInfo.amount}");
        }
    }

    private static void ResetDailyQuestStates(List<QuestData> dailyQuests)
    {
        foreach (QuestData quest in dailyQuests)
        {
            if (quest == null) continue;

            quest.state = QuestState.Locked;
            quest.currentCount = 0;
            quest.rewardClaimed = false;
            quest.currentCounts = new int[1] { 0 };
        }
    }

    private static DailyRewardInfo BuildRewardInfo(DailyRewardType rewardType, DailyQuestDifficulty difficulty)
    {
        var info = new DailyRewardInfo
        {
            rewardType = rewardType,
            label = string.Empty,
            amount = 0,
            rewardKey = (int)rewardType
        };

        switch (rewardType)
        {
            case DailyRewardType.Poing:
                info.label = "포잉";
                (int min, int max) range = GetPoingRange(difficulty);
                info.amount = UnityEngine.Random.Range(range.min, range.max + 1);
                break;
            case DailyRewardType.Fertilizer:
                info.label = "비료";
                info.amount = GetStackCount(difficulty);
                break;
            case DailyRewardType.Potion:
                string potion = potionTypes.Length > 0
                    ? potionTypes[UnityEngine.Random.Range(0, potionTypes.Length)]
                    : "물약";
                info.label = $"{potion} 물약";
                info.amount = GetStackCount(difficulty);
                info.rewardKey = 100 + Array.IndexOf(potionTypes, potion);
                break;
        }

        return info;
    }

    private static (int min, int max) GetPoingRange(DailyQuestDifficulty difficulty)
    {
        switch (difficulty)
        {
            case DailyQuestDifficulty.High:
                return (1100, 1500);
            case DailyQuestDifficulty.Medium:
                return (600, 1000);
            default:
                return (100, 500);
        }
    }

    private static int GetStackCount(DailyQuestDifficulty difficulty)
    {
        switch (difficulty)
        {
            case DailyQuestDifficulty.High:
                return 10;
            case DailyQuestDifficulty.Medium:
                return 5;
            default:
                return 3;
        }
    }

    private static string GetDifficultyLabel(DailyQuestDifficulty difficulty)
    {
        switch (difficulty)
        {
            case DailyQuestDifficulty.High:
                return "상";
            case DailyQuestDifficulty.Medium:
                return "중";
            default:
                return "하";
        }
    }

    private static int GetTargetCount(QuestData quest, DailyQuestDifficulty difficulty)
    {
        if (quest == null)
            return 0;

        if (quest.dailyTargets != null)
        {
            return quest.dailyTargets.GetTarget(difficulty);
        }

        if (quest.targetCounts != null && quest.targetCounts.Length > 0)
        {
            return quest.targetCounts[0];
        }

        return 0;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int swapIndex = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
        }
    }

    private struct DailyRewardInfo
    {
        public DailyRewardType rewardType;
        public string label;
        public int amount;
        public int rewardKey;
    }
}