using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using System;

[Serializable]
public class UserData
{
    public string userName;
    public int poing;
    public bool isShopUnlocked;
    // ▼▼▼ [추가] 마지막 접속 날짜
    public string lastLoginDate;

    public List<string> unlockedItemNames = new List<string>();
    public List<InventorySaveData> inventory = new List<InventorySaveData>();
    public List<QuestSaveData> quests = new List<QuestSaveData>();

    // ▼▼▼ [추가됨] 밭 저장 데이터 리스트 ▼▼▼
    public List<FieldSaveData> fields = new List<FieldSaveData>();
    // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

    // 해금된 레시피 이름들
    public List<string> unlockedRecipeNames = new List<string>();

    public UserData() { }
    public UserData(string name, int poing)
    {
        this.userName = name;
        this.poing = poing;
    }
}

[Serializable]
public class InventorySaveData
{
    public string itemName;
    public int amount;
}

[Serializable]
public class QuestSaveData
{
    public int key;
    public int state;
    public int currentCount;
    public bool rewardClaimed;

    // ▼▼▼ [추가] 일일 퀘스트 복구용 데이터 ▼▼▼
    public int dailyDifficulty; // 0:Low, 1:Medium, 2:High
    public int dailyRewardKey;  // 보상 아이템 ID
}

[Serializable]
public class FieldSaveData
{
    public int fieldId;
    public string plantedSeedName;
    public float remainingTime;
    public int fertilizerCount;
    public int state;
}

public class DBManager : MonoBehaviour
{
    public static DBManager Instance;

    [Header("★ 게임의 모든 아이템 등록")]
    public List<ItemData> allGameItems = new List<ItemData>();

    // ▼▼▼ [수정됨] 여기가 EvolutionRecipe로 바뀜! ▼▼▼
    [Header("★ 게임의 모든 레시피 등록")]
    public List<EvolutionRecipe> allGameRecipes = new List<EvolutionRecipe>();

    DatabaseReference reference;

    // ▼▼▼ [추가됨] 로드된 데이터를 임시 저장할 변수 (여기에 있어야 함!) ▼▼▼
    public UserData loadedUserData;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null); // [핵심] 부모(@Managers)에서 탈출!
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 이미 Auth씬에서 넘어온 DBManager가 있다면, 
            // Main씬에 있는 "새로운 DBManager"는 필요 없으니 삭제.
            Destroy(gameObject);
        }
    }

    void Start()
    {
        string dbUrl = "https://whatthefarm-893d5-default-rtdb.firebaseio.com/";
        reference = FirebaseDatabase.GetInstance(dbUrl).RootReference;
    }

    public ItemData FindItemByName(string name)
    {
        return allGameItems.Find(item => item.itemName == name);
    }

    // ▼▼▼ [수정됨] 레시피 찾는 함수 (파일 이름 .name 사용) ▼▼▼
    public EvolutionRecipe FindRecipeByName(string name)
    {
        return allGameRecipes.Find(recipe => recipe.name == name);
    }

    // DBManager.cs 안에 있는 SaveAllData를 이걸로 교체하세요

    // DBManager.cs의 SaveAllData 함수를 이걸로 덮어씌우세요!

    // ▼▼▼ [수정됨] 저장 로직에 '밭 저장' 추가 ▼▼▼
    public void SaveAllData(string userId)
    {
        Debug.Log($"💾 [저장 시작] User ID: {userId}");

        try
        {
            UserData data = new UserData();

            // 1. 포잉
            if (PoingManager.Instance != null) data.poing = PoingManager.Instance.currentPoing;
            data.userName = "감자농부";
            data.lastLoginDate = DateTime.Today.ToString("yyyy-MM-dd");

            // 2. 도감 & 진행도
            if (GameProgressionManager.Instance != null)
            {
                data.isShopUnlocked = GameProgressionManager.Instance.isShopUnlocked;
                foreach (var item in GameProgressionManager.Instance.unlockedItems) if (item != null) data.unlockedItemNames.Add(item.itemName);
                foreach (var recipe in GameProgressionManager.Instance.unlockedRecipes) if (recipe != null) data.unlockedRecipeNames.Add(recipe.name);
            }

            // 3. 인벤토리
            if (InventoryManager.Instance != null)
            {
                foreach (var kvp in InventoryManager.Instance.items)
                {
                    if (kvp.Key != null)
                    {
                        InventorySaveData invData = new InventorySaveData { itemName = kvp.Key.itemName, amount = kvp.Value };
                        data.inventory.Add(invData);
                    }
                }
            }

            // 4. 퀘스트
            if (QuestManager.Instance != null)
            {
                foreach (var q in QuestManager.Instance.allQuestList)
                {
                    if (q != null)
                    {
                        QuestSaveData qData = new QuestSaveData
                        {
                            key = q.key,
                            state = (int)q.state,
                            currentCount = q.currentCount,
                            rewardClaimed = q.rewardClaimed,

                            // ▼▼▼ [수정됨] 일일 퀘스트 정보 저장 추가 ▼▼▼
                            dailyDifficulty = (int)q.dailyDifficulty,
                            dailyRewardKey = q.rewardKey
                            // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲
                        };
                        data.quests.Add(qData);
                    }
                }
            }

            // ▼▼▼ [추가됨] 5. 밭 데이터 저장 ▼▼▼
            Field[] fields = FindObjectsOfType<Field>();
            foreach (Field f in fields)
            {
                if (f != null) data.fields.Add(f.GetSaveData());
            }
            // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

            // 파이어베이스 전송
            string json = JsonUtility.ToJson(data);

            if (reference == null)
            {
                string dbUrl = "https://whatthefarm-893d5-default-rtdb.firebaseio.com/";
                reference = FirebaseDatabase.GetInstance(dbUrl).RootReference;
            }

            reference.Child("users").Child(userId).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted) Debug.Log("✅ [저장 성공]");
                else Debug.LogError("❌ [저장 실패] : " + task.Exception);
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ 저장 에러: {e.Message}");
        }
    }

    // ▼▼▼ [수정됨] 로딩 완료 시점을 알 수 있게 Action 추가 ▼▼▼
    public void LoadAllData(string userId, Action onComplete = null)
    {
        reference.Child("users").Child(userId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.Exists)
                {
                    string json = snapshot.GetRawJsonValue();
                    UserData data = JsonUtility.FromJson<UserData>(json);

                    // [중요] 로드된 데이터를 메모리에 저장 (씬 이동 후 밭 복구용)
                    loadedUserData = data;

                    Debug.Log("📥 데이터 로드 완료!");

                    // 매니저들에 데이터 뿌리기
                    if (PoingManager.Instance != null) PoingManager.Instance.SetLoadedPoing(data.poing);
                    if (GameProgressionManager.Instance != null) GameProgressionManager.Instance.LoadProgression(data.isShopUnlocked, data.unlockedItemNames, data.unlockedRecipeNames);
                    if (InventoryManager.Instance != null) InventoryManager.Instance.LoadInventory(data.inventory);
                    if (QuestManager.Instance != null) QuestManager.Instance.LoadQuestData(data.quests, data.lastLoginDate);
                }
                else
                {
                    Debug.Log("신규 유저 -> 초기 데이터 생성");
                    loadedUserData = new UserData("감자농부", 0); // 빈 데이터 초기화
                    SaveAllData(userId);
                }

                // ▼▼▼ [핵심] 로딩 끝났으니 씬 넘어가라고 신호 보냄 ▼▼▼
                if (onComplete != null)
                {
                    onComplete.Invoke();
                }
            }
        });
    }

    // ▼▼▼ [추가됨] 게임 종료 시 자동 저장 ▼▼▼
    private void OnApplicationQuit()
    {
        if (Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            SaveAllData(Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId);
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause && Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            SaveAllData(Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId);
        }
    }

    // ▼▼▼ [추가됨] 메인 씬 시작 시 밭 복구 함수 ▼▼▼
    public void ApplyFieldDataToScene()
    {
        if (loadedUserData == null || loadedUserData.fields == null) return;

        Field[] sceneFields = FindObjectsOfType<Field>();

        foreach (var savedField in loadedUserData.fields)
        {
            foreach (var realField in sceneFields)
            {
                if (realField.fieldID == savedField.fieldId)
                {
                    realField.RestoreState(savedField);
                    break;
                }
            }
        }
        Debug.Log("🌱 밭 상태 복구 완료!");
    }
}