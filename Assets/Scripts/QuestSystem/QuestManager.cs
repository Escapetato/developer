using System;
using System.Collections;
using System.Collections.Generic;
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

    // 변수 추가
    private string lastSavedDate = "";

    // DB에서 불러오기가 끝났는지 확인하는 변수
    private bool isLoaded = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null); // 부모(@Managers)에서 탈출
        DontDestroyOnLoad(gameObject); // 파괴 방지

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
        RebuildActiveConditionIndex(); 


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

        // ⭐ 추가: 추적용 조건 정보 복사
        dst.conditionTypes = src.conditionTypes != null ? (QuestConditionType[])src.conditionTypes.Clone() : null;
        dst.conditionItems = src.conditionItems != null ? (ItemData[])src.conditionItems.Clone() : null;
        dst.countByAmount = src.countByAmount != null ? (bool[])src.countByAmount.Clone() : null;

        // ⭐ currentCounts도 src에 저장된 값이 있다면 복사(없으면 0으로 새로)
        if (src.currentCounts != null && src.currentCounts.Length > 0)
            dst.currentCounts = (int[])src.currentCounts.Clone();
        else
            dst.currentCounts = (dst.targetCounts != null) ? new int[dst.targetCounts.Length] : null;

        // 런타임 상태(새로 생성)
        dst.state = src.state;               // 혹은 Locked로 통일해도 됨(InitializeQuestStates가 어차피 초기화)
        dst.currentCount = src.currentCount;
        dst.rewardClaimed = src.rewardClaimed;
        //dst.currentCounts = (dst.targetCounts != null) ? new int[dst.targetCounts.Length] : null;

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

            //q.state = QuestState.Locked;

            EnsureConditionArrays(q);
            if (q.currentCounts != null)
            {
                for (int i = 0; i < q.currentCounts.Length; i++)
                    q.currentCounts[i] = 0;
            }
            q.currentCount = 0;
            q.rewardClaimed = false; // 세이브 시스템 후 수정 필요
            q.isNewlyOpened = false;

        }
    }

    // [QuestManager.cs] OpenInitialSlots 함수 수정
    // QuestManager.cs -> OpenInitialSlots 함수 (이걸로 교체!)
    private void OpenInitialSlots()
    {
        MainQuestSlot();
        SubQuestSlot(2);

        string today = System.DateTime.Today.ToString("yyyy-MM-dd");
        bool needSave = false;

        // [상황 A] 같은 날짜 접속 (유지해야 함)
        if (!string.IsNullOrEmpty(lastSavedDate) && lastSavedDate == today)
        {
            Debug.Log($"[QuestManager] 같은 날({today}) 접속. 일일 퀘스트 유지.");

            // ★ [핵심] 불러온 일일 퀘스트의 '목표 수치'가 0으로 날아갔다면 복구해줘야 함!
            foreach (var q in dailyQuests)
            {
                if (q.state == QuestState.Active || q.state == QuestState.Completed)
                {
                    // 목표가 없거나 0이면 -> 기본값으로 복구
                    if (q.targetCounts == null || q.targetCounts.Length == 0 || q.targetCounts[0] == 0)
                    {
                        int fallbackTarget = 3; // 기본 목표 3회
                        if (q.dailyTargets != null) fallbackTarget = q.dailyTargets.lowTarget; // 설정된 Low 값 있으면 그걸로

                        q.targetCounts = new int[] { fallbackTarget };
                        if (q.currentCounts == null || q.currentCounts.Length == 0) q.currentCounts = new int[] { 0 };

                        Debug.Log($"[복구] 일일 퀘스트({q.title}) 목표 수치 복구 완료: {fallbackTarget}");
                    }
                }
            }

            // 만약 활성화된 게 하나도 없다면(오류 등) 새로 생성
            if (CountActive(dailyQuests) == 0)
            {
                DailyQuestSelector.ConfigureDailyQuests(dailyQuests, 3);
                needSave = true;
            }
        }
        // [상황 B] 날짜가 변경됨 or 첫 시작 (리셋)
        else
        {
            Debug.Log($"[QuestManager] 새로운 날({today}) 접속. 일일 퀘스트 리셋!");
            foreach (var q in dailyQuests) q.state = QuestState.Locked;
            DailyQuestSelector.ConfigureDailyQuests(dailyQuests, 3);
            needSave = true;
        }

        // ★★★ [중요] 새로 만들었으면 즉시 저장해야, 껐다 켜도 안 바뀜!
        if (needSave)
        {
            SaveToDB();
        }
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
                q.isNewlyOpened = true;
                Debug.Log($"[QuestManager] 메인 퀘스트 새로 오픈: key={q.key}, title={q.title}");

                // ▼▼▼ 진행도가 올랐으니 저장 ▼▼▼
                SaveToDB();
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
                q.isNewlyOpened = true;
                openCount++;
                Debug.Log($"[QuestManager] 서브 퀘스트 오픈: key={q.key}, title={q.title}");
                // ▼▼▼ 진행도가 올랐으니 저장 ▼▼▼
                SaveToDB();
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

    // 수정
    public void LoadQuestData(List<QuestSaveData> savedQuests, string dateStr)
    {
        Debug.Log($"[QuestManager] LoadQuestData 호출됨. 저장된 퀘스트 수: {(savedQuests != null ? savedQuests.Count : 0)}");

        lastSavedDate = dateStr;

        // 1. 초기화 (일일 퀘스트 내용/목표 설정됨)
        InitializeIfNeeded();

        // [CASE 1] 저장된 데이터가 없는 경우 (신규 유저 / DB 초기화)
        if (savedQuests == null || savedQuests.Count == 0)
        {
            Debug.Log("[QuestManager] 저장된 퀘스트 데이터가 없습니다. (신규 시작)");

            if (string.IsNullOrEmpty(lastSavedDate))
                lastSavedDate = System.DateTime.Today.ToString("yyyy-MM-dd");

            isLoaded = true; // 로딩 완료 도장
            OpenInitialSlots(); // 초기 슬롯 오픈 + 저장
            return;
        }

        // [CASE 2] 저장된 데이터가 있는 경우 (기존 유저)

        // 상태 리셋
        foreach (var q in allQuestList)
        {
            q.state = QuestState.Locked;
            q.currentCount = 0;
            q.rewardClaimed = false;
            q.isNewlyOpened = false;
            if (q.currentCounts != null)
                for (int i = 0; i < q.currentCounts.Length; i++) q.currentCounts[i] = 0;
        }

        // 데이터 덮어쓰기
        foreach (var savedQ in savedQuests)
        {
            if (savedQ == null) continue;

            QuestData myQuest = allQuestList.Find(q => q.key == savedQ.key);
            if (myQuest != null)
            {
                // 저장된 상태 복구
                myQuest.state = (QuestState)savedQ.state;
                myQuest.currentCount = savedQ.currentCount;
                myQuest.rewardClaimed = savedQ.rewardClaimed;

                EnsureConditionArrays(myQuest);

                // 배열에도 값 동기화
                if (myQuest.currentCounts != null && myQuest.currentCounts.Length > 0)
                {
                    myQuest.currentCounts[0] = savedQ.currentCount;
                }

                // ▼▼▼ [여기 추가됨!] 일일 퀘스트 완료 체크 보정 ▼▼▼
                // 불러온 카운트가 목표치 이상이면, 상태를 'Completed(완료)'로 강제 변경
                if (myQuest.type == QuestType.Daily && myQuest.state == QuestState.Active)
                {
                    if (myQuest.targetCounts != null && myQuest.targetCounts.Length > 0)
                    {
                        int target = myQuest.targetCounts[0];
                        int current = myQuest.currentCounts[0];

                        // 목표 달성했으면 완료 상태로!
                        if (target > 0 && current >= target)
                        {
                            myQuest.state = QuestState.Completed;
                            Debug.Log($"[Load] 일일 퀘스트({myQuest.title}) 완료 상태로 보정됨 ({current}/{target})");
                        }
                    }
                }
                // ▲▲▲ [추가 끝] ▲▲▲
            }
        }

        // 로딩 완료 도장 찍기 (먼저 찍어야 OpenInitialSlots에서 저장됨)
        isLoaded = true;

        // 슬롯 갱신
        OpenInitialSlots();
        RebuildActiveConditionIndex();

        Debug.Log($"[QuestManager] 퀘스트 복구 최종 완료! 메인 진행중: {CountActive(mainQuests)}개");
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
        RebuildActiveConditionIndex();

        // 퀘스트 닫힌 뒤 다음 슬롯 자동 오픈 
        if (quest.type == QuestType.Main)
            MainQuestSlot();     // 다음 메인 1개 열기
        else if (quest.type == QuestType.Sub)
            SubQuestSlot(2);     // 서브 2개 유지

        OnQuestChanged?.Invoke();

        // ▼▼▼ 진행도가 올랐으니 저장 ▼▼▼
        SaveToDB();

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

        // ▼▼▼ 진행도가 올랐으니 저장 ▼▼▼
        SaveToDB();

        return true;
    }

    // [유틸] 퀘스트 조건 배열(Types/Items/Counts)의 null/길이 불일치 방지.
    // targetCounts 길이를 기준으로 currentCounts/conditionTypes/conditionItems/countByAmount를 자동 보정한다.
    private void EnsureConditionArrays(QuestData q)
    {
        if (q == null) return;

        int n = (q.targetCounts != null) ? q.targetCounts.Length : 0;
        if (n <= 0) return;

        if (q.currentCounts == null || q.currentCounts.Length != n) q.currentCounts = new int[n];
        if (q.conditionTypes == null || q.conditionTypes.Length != n) q.conditionTypes = new QuestConditionType[n];
        if (q.conditionItems == null || q.conditionItems.Length != n) q.conditionItems = new ItemData[n];
        if (q.countByAmount == null || q.countByAmount.Length != n) q.countByAmount = new bool[n];

        if (n == 1) q.currentCount = q.currentCounts[0]; // 레거시 동기화(옵션)
    }

    private class ConditionBinding
    {
        public QuestData quest;
        public int index;
    }

    private readonly Dictionary<QuestConditionType, List<ConditionBinding>> _activeBindings
        = new Dictionary<QuestConditionType, List<ConditionBinding>>();

    private int _evolveSuccessStreak = 0;

    // 인덱스 리빌드 함수 
    private void RebuildActiveConditionIndex()
    {
        Debug.Log("[Quest][Bind] RebuildActiveConditionIndex() CALLED");

        _activeBindings.Clear();

        foreach (var q in allQuestList)
        {
            if (q == null) continue;

            EnsureConditionArrays(q);

            if (q.state != QuestState.Active) continue;

            for (int i = 0; i < q.conditionTypes.Length; i++)
            {
                var type = q.conditionTypes[i];
                if (type == QuestConditionType.None) continue;

                if (!_activeBindings.TryGetValue(type, out var list))
                {
                    list = new List<ConditionBinding>();
                    _activeBindings[type] = list;
                }

                list.Add(new ConditionBinding { quest = q, index = i });
            }
        }

        Debug.Log($"[Quest][Bind] rebuilt. types={_activeBindings.Count}");
        foreach (var kv in _activeBindings) Debug.Log($"[Quest][Bind] type={kv.Key} bindings={kv.Value.Count}");

    }

    // ✨ 퀘스트 진행도 추적 - 알림 함수 ! 
    public void NotifyAction(QuestConditionType type, ItemData item = null, int amount = 1)
    {
        Debug.Log($"[Quest][Action][RECV] type={type} item={(item != null ? item.itemName : "null")} amount={amount}");

        if (!_activeBindings.TryGetValue(type, out var list) || list == null || list.Count == 0)
        {
            Debug.Log($"[Quest][Action][DROP] type={type} (no active binding)");
            return;
        }

        bool changedAny = false;

        foreach (var b in list)
        {
            var q = b.quest;
            int i = b.index;

            if (q == null || q.state != QuestState.Active) continue;
            EnsureConditionArrays(q);

            // 아이템 필터(조건에 특정 아이템이 지정된 경우만)
            ItemData required = q.conditionItems[i];
            if (required != null && item != required)
            {
                Debug.Log($"[Quest][Action][SKIP] type={type} required={required.itemName} got={(item != null ? item.itemName : "null")}");
                continue;
            }

            int target = q.targetCounts[i];
            int before = q.currentCounts[i];

            // countByAmount: true면 amount 만큼 증가(개수/금액), false면 1회로 증가
            int delta = q.countByAmount[i] ? Mathf.Max(0, amount) : 1;

            int after = Mathf.Clamp(before + delta, 0, target);

            if (after != before)
            {
                q.currentCounts[i] = after;
                if (q.targetCounts.Length == 1) q.currentCount = q.currentCounts[0];
                changedAny = true;

                Debug.Log($"[Quest][Action][APPLY] quest={q.key} idx={i} {before}->{after}/{target} delta={delta}");
            }

            if (IsCompletedByCounts(q) && q.state == QuestState.Active)
            {
                q.state = QuestState.Completed;
                changedAny = true;
            }
        }

        if (changedAny)
            OnQuestChanged?.Invoke();

        // ▼▼▼ 진행도가 올랐으니 저장 ▼▼▼
        SaveToDB();
    }

    // 진화 결과 추적용 함수 (연속 횟수)
    public void NotifyEvolutionResult(bool success, ItemData resultItem = null)
    {
        Debug.Log($"[Quest<-Lab] NotifyEvolutionResult RECEIVED. success={success}, item={(resultItem != null ? resultItem.itemName : "null")}");

        if (success)
        {
            _evolveSuccessStreak++;
            NotifyAction(QuestConditionType.EvolveSuccess, resultItem, 1);

            // 5연속 성공 타입은 '스트릭 값'으로
            if (_activeBindings.TryGetValue(QuestConditionType.EvolveSuccessStreak, out var list))
            {
                bool changed = false;

                foreach (var b in list)
                {
                    var q = b.quest;
                    int i = b.index;
                    if (q == null || q.state != QuestState.Active) continue;

                    EnsureConditionArrays(q);

                    int target = q.targetCounts[i];
                    int newValue = Mathf.Clamp(_evolveSuccessStreak, 0, target);

                    if (q.currentCounts[i] != newValue)
                    {
                        q.currentCounts[i] = newValue;
                        changed = true;
                    }

                    if (IsCompletedByCounts(q) && q.state == QuestState.Active)
                    {
                        q.state = QuestState.Completed;
                        changed = true;
                    }
                }

                if (changed) OnQuestChanged?.Invoke();
            }
        }
        else
        {
            _evolveSuccessStreak = 0;
            NotifyAction(QuestConditionType.EvolveFail, null, 1);

            // 실패하면 연속 성공 스트릭이 0
            if (_activeBindings.TryGetValue(QuestConditionType.EvolveSuccessStreak, out var list))
            {
                bool changed = false;
                foreach (var b in list)
                {
                    var q = b.quest;
                    int i = b.index;
                    if (q == null || q.state != QuestState.Active) continue;

                    EnsureConditionArrays(q);

                    if (q.currentCounts[i] != 0)
                    {
                        q.currentCounts[i] = 0;
                        changed = true;
                    }
                }
                if (changed) OnQuestChanged?.Invoke();
            }
        }

        // ▼▼▼ 진행도가 올랐으니 저장 ▼▼▼
        SaveToDB();
    }

    // 추가
    private void SaveToDB()
    {
        // ★ 아직 DB에서 로딩이 안 끝났으면 저장 x (초기화 덮어쓰기 방지)
        if (!isLoaded) return;

        if (Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser != null && DBManager.Instance != null)
        {
            string myId = Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            DBManager.Instance.SaveAllData(myId);
        }
    }
}
