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
    // (상,중,하) 를 섞어서 A/B/C에 배정 -> 중복 불가능
    private static readonly DailyQuestDifficulty[] difficultyPermutation =
    {
        DailyQuestDifficulty.High,
        DailyQuestDifficulty.Medium,
        DailyQuestDifficulty.Low
    };

    // A/B/C 슬롯 보상 고정: A=포잉, B=비료, C=물약(종류 랜덤)
    private static readonly DailyRewardType[] slotRewards =
    {
        DailyRewardType.Poing,
        DailyRewardType.Fertilizer,
        DailyRewardType.Potion
    };

    private static readonly Dictionary<DailyQuestDifficulty, int[]> poingTable
        = new Dictionary<DailyQuestDifficulty, int[]>
    {
        { DailyQuestDifficulty.Low,    new[] { 100, 150, 200 } },
        { DailyQuestDifficulty.Medium, new[] { 300, 400, 500 } },
        { DailyQuestDifficulty.High,   new[] { 700, 850, 1000 } },
    };

    private static readonly string[] potionTypes = { "불", "소리", "번개", "바람", "물", "별", "꽃", "무지개" };

    // 템플릿(원본) 값을 보관해서 ConfigureDailyQuests가 여러 번 호출되어도 누적되지 않게 한다.
    private static readonly Dictionary<int, string> baseTitleByKey = new Dictionary<int, string>();
    private static readonly Dictionary<int, string> baseDescByKey = new Dictionary<int, string>();
    private static readonly Dictionary<int, int[]> baseTargetCountsByKey = new Dictionary<int, int[]>();
    private static readonly Dictionary<int, int> baseRewardKeyByKey = new Dictionary<int, int>();
    private static readonly Dictionary<int, int> baseRewardAmountByKey = new Dictionary<int, int>();

    /// <summary>
    /// 규칙:
    /// 1) 8개(일일퀘스트 후보) 중 3개 랜덤 선정 (중복 X)
    /// 2) 선정된 3개를 A/B/C로 칭하고 보상 타입 고정 (A=포잉, B=비료, C=물약(랜덤))
    /// 3) A/B/C 난이도 상/중/하 랜덤 배정 (중복 X)
    /// 4) 난이도에 따라 목표 횟수/보상 개수 결정
    /// </summary>
    public static void ConfigureDailyQuests(List<QuestData> dailyQuests, int targetCount)
    {
        if (dailyQuests == null || dailyQuests.Count == 0)
        {
            Debug.Log("[DailyQuestSelector] 일일 퀘스트 데이터가 없습니다.");
            return;
        }

        // 원본(템플릿) 캐싱 -> 타이틀/설명/보상/목표 누적 및 잔상 방지
        CacheBaseDailyQuestTemplates(dailyQuests);

        // 전체 상태를 원복 + 잠금
        ResetDailyQuestStates(dailyQuests);

        // 1) 후보(8개) 중 3개 랜덤 선정
        List<QuestData> candidates = new List<QuestData>(dailyQuests);
        Shuffle(candidates);

        // 3) 상/중/하 랜덤 배정 (중복 X)
        List<DailyQuestDifficulty> diffOrder = new List<DailyQuestDifficulty>(difficultyPermutation);
        Shuffle(diffOrder);

        // 일일 퀘스트는 항상 3개(A/B/C)만 오픈한다.
        int openMax = Mathf.Min(3, targetCount, slotRewards.Length, diffOrder.Count, candidates.Count);

        // 디버깅용: 오늘 뽑힌 3개(A/B/C)를 요약 출력하기 위해 저장
        List<QuestData> openedToday = new List<QuestData>(openMax);

        for (int i = 0; i < openMax; i++)
        {
            // i=0,1,2 -> A,B,C
            QuestData quest = candidates[i];

            // 3) 난이도 배정 (중복 X)
            DailyQuestDifficulty difficulty = diffOrder[i];

            // 2) 보상 타입 고정 (A=Poing, B=Fertilizer, C=Potion)
            DailyRewardType rewardType = slotRewards[i];

            // 4) 난이도 기반 보상/목표 결정
            DailyRewardInfo rewardInfo = BuildRewardInfo(rewardType, difficulty);
            int target = GetTargetCount(quest, difficulty);

            // 퀘스트 적용
            quest.dailyDifficulty = difficulty;

            // 타이틀/설명 누적 방지: 캐싱해둔 원본 타이틀에서 시작
            quest.title = $"{GetBaseTitle(quest)} {target}회";
            quest.questDesc = $"난이도: {GetDifficultyLabel(difficulty)}\n보상: {rewardInfo.label} x{rewardInfo.amount}";

            // 일일 퀘스트는 1조건만 사용
            quest.targetCounts = new[] { target };
            quest.currentCounts = new[] { 0 };

            quest.rewardKey = rewardInfo.rewardKey;
            quest.rewardAmount = rewardInfo.amount;

            quest.state = QuestState.Active;
            quest.isNewlyOpened = true;   // 빨간 점 추가
            quest.currentCount = 0;
            quest.rewardClaimed = false;

            openedToday.Add(quest);

            // 슬롯별 상세 로그
            Debug.Log(
                $"[DailyQuestSelector] 오늘의 일일 퀘스트 오픈: slot={(char)('A' + i)} | key={quest.key} | title=\"{quest.title}\" | diff={GetDifficultyLabel(difficulty)}"
                + $" | target={target} | reward={rewardInfo.label} x{rewardInfo.amount} (rewardKey={rewardInfo.rewardKey})");
        }
    }

    private static void ResetDailyQuestStates(List<QuestData> dailyQuests)
    {
        foreach (QuestData quest in dailyQuests)
        {
            if (quest == null) continue;

            // 템플릿 값으로 원복 (이전 Configure 결과가 남지 않게)
            if (baseTitleByKey.TryGetValue(quest.key, out var baseTitle))
                quest.title = baseTitle;
            if (baseDescByKey.TryGetValue(quest.key, out var baseDesc))
                quest.questDesc = baseDesc;
            if (baseTargetCountsByKey.TryGetValue(quest.key, out var baseTargets))
                quest.targetCounts = baseTargets != null ? (int[])baseTargets.Clone() : null;
            if (baseRewardKeyByKey.TryGetValue(quest.key, out var baseRewardKey))
                quest.rewardKey = baseRewardKey;
            if (baseRewardAmountByKey.TryGetValue(quest.key, out var baseRewardAmount))
                quest.rewardAmount = baseRewardAmount;

            // 런타임 상태 리셋
            quest.state = QuestState.Locked;
            quest.currentCount = 0;
            quest.rewardClaimed = false;
            quest.isNewlyOpened = false; // 빨간점 잔상 추가 

            // 일일 퀘스트는 1조건만 사용
            quest.currentCounts = new int[1] { 0 };
        }
    }

    private static void CacheBaseDailyQuestTemplates(List<QuestData> dailyQuests)
    {
        foreach (var quest in dailyQuests)
        {
            if (quest == null) continue;

            // 이미 캐시돼있으면 스킵
            if (!baseTitleByKey.ContainsKey(quest.key))
            {
                baseTitleByKey[quest.key] = quest.title;
                baseDescByKey[quest.key] = quest.questDesc;

                baseTargetCountsByKey[quest.key] = quest.targetCounts != null ? (int[])quest.targetCounts.Clone() : null;
                baseRewardKeyByKey[quest.key] = quest.rewardKey;
                baseRewardAmountByKey[quest.key] = quest.rewardAmount;
            }
        }
    }

    private static string GetBaseTitle(QuestData quest)
    {
        if (quest == null)
            return string.Empty;

        if (baseTitleByKey.TryGetValue(quest.key, out var baseTitle) && !string.IsNullOrEmpty(baseTitle))
            return baseTitle;

        return quest.title;
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
                switch (difficulty)
                {
                    case DailyQuestDifficulty.High:
                        info.amount = 850;
                        break;
                    case DailyQuestDifficulty.Medium:
                        info.amount = 400;
                        break;
                    default:
                        info.amount = 150;
                        break;                    
                }
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

        // quest.dailyTargets가 있으면 난이도별 테이블 사용
        if (quest.dailyTargets != null)
        {
            return quest.dailyTargets.GetTarget(difficulty);
        }

        // fallback: 기존 targetCounts[0] 사용
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

    // 추가
    public static void RestoreDailyQuest(QuestData quest, int difficultyInt, int rewardKey)
    {
        if (quest == null) return;

        // 1. 난이도 복구
        DailyQuestDifficulty difficulty = (DailyQuestDifficulty)difficultyInt;
        quest.dailyDifficulty = difficulty;
        quest.rewardKey = rewardKey;

        // 2. 제목 복구 (캐시된 원본 사용)
        string baseTitle = GetBaseTitle(quest);
        if (string.IsNullOrEmpty(baseTitle)) baseTitle = quest.title;

        // 3. 목표 횟수 재계산
        int target = GetTargetCount(quest, difficulty);

        // 4. 보상 수량 재계산
        DailyRewardType rType = DailyRewardType.Poing;
        if (rewardKey == 1) rType = DailyRewardType.Fertilizer;
        else if (rewardKey >= 100) rType = DailyRewardType.Potion;

        DailyRewardInfo rewardInfo = BuildRewardInfo(rType, difficulty);
        quest.rewardAmount = rewardInfo.amount;

        // 5. 텍스트 및 목표 배열 적용
        quest.title = $"{baseTitle} {target}회";
        quest.questDesc = $"난이도: {GetDifficultyLabel(difficulty)}\n보상: {rewardInfo.label} x{rewardInfo.amount}";

        quest.targetCounts = new int[] { target };

        // currentCounts 배열 안전장치
        if (quest.currentCounts == null || quest.currentCounts.Length == 0)
            quest.currentCounts = new int[] { 0 };

        Debug.Log($"[DailyRestore] {quest.title} 복구 완료 (난이도:{difficulty}, 목표:{target})");
    }
}