using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

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
            transform.SetParent(null); // 부모(@Managers)에서 탈출하여 독립
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

    // ▼▼▼ [핵심] 씬 이동 완료 시 데이터 주입 ▼▼▼
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Main")
        {
            // 씬 로드 직후에는 매니저들이 초기화 상태(0)이므로,
            // 들고 있던 loadedUserData를 다시 꽂아줘야 합니다.
            ApplyDataToManagers();
            ApplyFieldDataToScene();
        }
    }

    // 매니저들에게 데이터 뿌리기
    void ApplyDataToManagers()
    {
        if (loadedUserData == null) return;

        Debug.Log("🔄 [데이터 복구] 씬 로드 후 매니저들에게 데이터 주입 중...");

        if (PoingManager.Instance != null)
            PoingManager.Instance.SetLoadedPoing(loadedUserData.poing);

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.LoadInventory(loadedUserData.inventory);

        if (GameProgressionManager.Instance != null)
            GameProgressionManager.Instance.LoadProgression(loadedUserData.isShopUnlocked, loadedUserData.unlockedItemNames, loadedUserData.unlockedRecipeNames);

        if (QuestManager.Instance != null)
            QuestManager.Instance.LoadQuestData(loadedUserData.quests, loadedUserData.lastLoginDate);
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
            if (loadedUserData == null) loadedUserData = new UserData("감자농부", 0);

            // 매니저가 살아있을 때만 최신 값으로 갱신 (죽었으면 기존 값 유지)
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
                    // Auth씬에서는 매니저가 없으니 여기선 적용 안 됨 -> OnSceneLoaded에서 적용됨
                }
                else
                {
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