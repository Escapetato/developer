using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using System.Collections; // 코루틴용

[Serializable]
public class UserData
{
    public string userName;
    public int poing;
    public bool isShopUnlocked;
    public string lastLoginDate;

    public List<string> unlockedItemNames = new List<string>();
    public List<InventorySaveData> inventory = new List<InventorySaveData>();
    public List<QuestSaveData> quests = new List<QuestSaveData>();
    public List<FieldSaveData> fields = new List<FieldSaveData>();
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
    public int dailyDifficulty;
    public int dailyRewardKey;
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

    [Header("★ 게임의 모든 레시피 등록")]
    public List<EvolutionRecipe> allGameRecipes = new List<EvolutionRecipe>();

    DatabaseReference reference;

    public UserData loadedUserData;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        string dbUrl = "https://whatthefarm-893d5-default-rtdb.firebaseio.com/";
        reference = FirebaseDatabase.GetInstance(dbUrl).RootReference;
    }

    // ▼▼▼ [핵심 수정] 씬 로드 시 데이터 주입 (1프레임 대기) ▼▼▼
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Main")
        {
            // 바로 실행하면 매니저들이 준비 안 됐을 수도 있으니 1프레임 쉼
            StartCoroutine(DelayedRestore());
        }
    }

    IEnumerator DelayedRestore()
    {
        yield return null; // 1프레임 대기 (매니저들 Awake/Start 완료 대기)

        Debug.Log("🔄 [데이터 복구] 매니저들에게 데이터 주입 시작");
        ApplyDataToManagers(); // 포잉, 인벤, 도감 등 복구
        ApplyFieldDataToScene(); // 밭 복구
    }

    // ▼▼▼ [새로 추가된 함수] 매니저들에게 데이터 꽂아주기 ▼▼▼
    void ApplyDataToManagers()
    {
        if (loadedUserData == null) return;

        // 1. 포잉 복구
        if (PoingManager.Instance != null)
        {
            PoingManager.Instance.SetLoadedPoing(loadedUserData.poing);
        }

        // 2. 인벤토리 복구
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.LoadInventory(loadedUserData.inventory);
        }

        // 3. 도감 복구
        if (GameProgressionManager.Instance != null)
        {
            GameProgressionManager.Instance.LoadProgression(
                loadedUserData.isShopUnlocked,
                loadedUserData.unlockedItemNames,
                loadedUserData.unlockedRecipeNames
            );
        }

        // 4. 퀘스트 복구
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.LoadQuestData(loadedUserData.quests, loadedUserData.lastLoginDate);
        }
    }

    public ItemData FindItemByName(string name)
    {
        return allGameItems.Find(item => item.itemName == name);
    }

    public EvolutionRecipe FindRecipeByName(string name)
    {
        return allGameRecipes.Find(recipe => recipe.name == name);
    }

    public void SaveAllData(string userId)
    {
        Debug.Log($"💾 [저장 시작] User ID: {userId}");

        try
        {
            // 메모리에 데이터가 없으면 새로 만듦
            if (loadedUserData == null) loadedUserData = new UserData("감자농부", 0);

            // 매니저가 살아있으면 최신 값으로 업데이트
            if (PoingManager.Instance != null) loadedUserData.poing = PoingManager.Instance.currentPoing;

            loadedUserData.lastLoginDate = DateTime.Today.ToString("yyyy-MM-dd");

            if (GameProgressionManager.Instance != null)
            {
                loadedUserData.isShopUnlocked = GameProgressionManager.Instance.isShopUnlocked;

                loadedUserData.unlockedItemNames.Clear();
                foreach (var item in GameProgressionManager.Instance.unlockedItems)
                    if (item != null) loadedUserData.unlockedItemNames.Add(item.itemName);

                loadedUserData.unlockedRecipeNames.Clear();
                foreach (var recipe in GameProgressionManager.Instance.unlockedRecipes)
                    if (recipe != null) loadedUserData.unlockedRecipeNames.Add(recipe.name);
            }

            if (InventoryManager.Instance != null)
            {
                loadedUserData.inventory.Clear();
                foreach (var kvp in InventoryManager.Instance.items)
                {
                    if (kvp.Key != null) loadedUserData.inventory.Add(new InventorySaveData { itemName = kvp.Key.itemName, amount = kvp.Value });
                }
            }

            if (QuestManager.Instance != null)
            {
                loadedUserData.quests.Clear();
                foreach (var q in QuestManager.Instance.allQuestList)
                {
                    if (q != null)
                    {
                        loadedUserData.quests.Add(new QuestSaveData
                        {
                            key = q.key,
                            state = (int)q.state,
                            currentCount = q.currentCount,
                            rewardClaimed = q.rewardClaimed,
                            dailyDifficulty = (int)q.dailyDifficulty,
                            dailyRewardKey = q.rewardKey
                        });
                    }
                }
            }

            Field[] fields = FindObjectsOfType<Field>();
            if (fields.Length > 0)
            {
                loadedUserData.fields.Clear();
                foreach (Field f in fields) if (f != null) loadedUserData.fields.Add(f.GetSaveData());
            }

            string json = JsonUtility.ToJson(loadedUserData);

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
                    loadedUserData = JsonUtility.FromJson<UserData>(json);

                    if (loadedUserData.inventory == null) loadedUserData.inventory = new List<InventorySaveData>();
                    if (loadedUserData.fields == null) loadedUserData.fields = new List<FieldSaveData>();
                    if (loadedUserData.quests == null) loadedUserData.quests = new List<QuestSaveData>();

                    Debug.Log("📥 데이터 로드 완료!");
                }
                else
                {
                    Debug.Log("신규 유저 -> 초기 데이터 생성");
                    loadedUserData = new UserData("감자농부", 0);
                    SaveAllData(userId);
                }

                if (onComplete != null) onComplete.Invoke();
            }
        });
    }

    private void OnApplicationQuit()
    {
        if (Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser != null)
            SaveAllData(Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId);
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause && Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser != null)
            SaveAllData(Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId);
    }

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