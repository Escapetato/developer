using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using System; // 날짜 계산용
using UnityEngine.SceneManagement;
using System.Collections;

[Serializable]
public class UserData
{
    public string userName;
    public int poing;
    public bool isShopUnlocked;
    public string lastLoginDate;

    // ▼▼▼ [추가] 마지막으로 저장한 시간 (로그아웃 시간) ▼▼▼
    public string logoutTime;
    // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

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

// (InventorySaveData, QuestSaveData, FieldSaveData 클래스는 그대로 둠)
[Serializable] public class InventorySaveData { public string itemName; public int amount; }
[Serializable] public class QuestSaveData { public int key; public int state; public int currentCount; public bool rewardClaimed; public int dailyDifficulty; public int dailyRewardKey; }
[Serializable] public class FieldSaveData { public int fieldId; public string plantedSeedName; public float remainingTime; public int fertilizerCount; public int state; }

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
        else { Destroy(gameObject); }
    }

    void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void Start()
    {
        string dbUrl = "https://whatthefarm-893d5-default-rtdb.firebaseio.com/";
        reference = FirebaseDatabase.GetInstance(dbUrl).RootReference;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Main") StartCoroutine(DelayedRestore());
    }

    IEnumerator DelayedRestore()
    {
        yield return null;
        Debug.Log("🔄 [데이터 복구] 매니저들에게 데이터 주입 시작");
        ApplyDataToManagers();
        ApplyFieldDataToScene();
    }

    void ApplyDataToManagers()
    {
        if (loadedUserData == null) return;
        if (PoingManager.Instance != null) PoingManager.Instance.SetLoadedPoing(loadedUserData.poing);
        if (InventoryManager.Instance != null) InventoryManager.Instance.LoadInventory(loadedUserData.inventory);
        if (GameProgressionManager.Instance != null) GameProgressionManager.Instance.LoadProgression(loadedUserData.isShopUnlocked, loadedUserData.unlockedItemNames, loadedUserData.unlockedRecipeNames);
        if (QuestManager.Instance != null) QuestManager.Instance.LoadQuestData(loadedUserData.quests, loadedUserData.lastLoginDate);
    }

    public ItemData FindItemByName(string name) { return allGameItems.Find(item => item.itemName == name); }
    public EvolutionRecipe FindRecipeByName(string name) { return allGameRecipes.Find(recipe => recipe.name == name); }

    // ▼▼▼ [수정됨] SaveAllData: 현재 시간을 logoutTime에 기록 ▼▼▼
    public void SaveAllData(string userId)
    {
        Debug.Log($"💾 [저장 시작] User ID: {userId}");
        try
        {
            if (loadedUserData == null) loadedUserData = new UserData("감자농부", 0);

            if (PoingManager.Instance != null) loadedUserData.poing = PoingManager.Instance.currentPoing;

            // [날짜 저장]
            loadedUserData.lastLoginDate = DateTime.Today.ToString("yyyy-MM-dd");

            // ★★★ [시간 저장] 현재 시간(세계 표준시)을 저장 ★★★
            loadedUserData.logoutTime = DateTime.UtcNow.ToString();

            // (매니저 데이터 갱신 로직들...)
            if (GameProgressionManager.Instance != null)
            {
                loadedUserData.isShopUnlocked = GameProgressionManager.Instance.isShopUnlocked;
                loadedUserData.unlockedItemNames.Clear();
                foreach (var item in GameProgressionManager.Instance.unlockedItems) if (item != null) loadedUserData.unlockedItemNames.Add(item.itemName);
                loadedUserData.unlockedRecipeNames.Clear();
                foreach (var recipe in GameProgressionManager.Instance.unlockedRecipes) if (recipe != null) loadedUserData.unlockedRecipeNames.Add(recipe.name);
            }

            if (InventoryManager.Instance != null)
            {
                loadedUserData.inventory.Clear();
                foreach (var kvp in InventoryManager.Instance.items) if (kvp.Key != null) loadedUserData.inventory.Add(new InventorySaveData { itemName = kvp.Key.itemName, amount = kvp.Value });
            }

            if (QuestManager.Instance != null)
            {
                loadedUserData.quests.Clear();
                foreach (var q in QuestManager.Instance.allQuestList) if (q != null) loadedUserData.quests.Add(new QuestSaveData { key = q.key, state = (int)q.state, currentCount = q.currentCount, rewardClaimed = q.rewardClaimed, dailyDifficulty = (int)q.dailyDifficulty, dailyRewardKey = q.rewardKey });
            }

            Field[] fields = FindObjectsOfType<Field>();
            if (fields.Length > 0)
            {
                loadedUserData.fields.Clear();
                foreach (Field f in fields) if (f != null) loadedUserData.fields.Add(f.GetSaveData());
            }

            string json = JsonUtility.ToJson(loadedUserData);
            if (reference == null) { string dbUrl = "https://whatthefarm-893d5-default-rtdb.firebaseio.com/"; reference = FirebaseDatabase.GetInstance(dbUrl).RootReference; }

            reference.Child("users").Child(userId).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
            {
                if (!task.IsCompleted) Debug.LogError("❌ [저장 실패] : " + task.Exception);
            });
        }
        catch (Exception e) { Debug.LogError($"❌ 저장 에러: {e.Message}"); }
    }

    // LoadAllData는 기존과 동일
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
                    loadedUserData = new UserData("감자농부", 0);
                    SaveAllData(userId);
                }
                if (onComplete != null) onComplete.Invoke();
            }
        });
    }

    private void OnApplicationQuit() { if (Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser != null) SaveAllData(Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId); }
    private void OnApplicationPause(bool pause) { if (pause && Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser != null) SaveAllData(Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId); }

    // ▼▼▼ [대폭 수정됨] 밭 복구 시 '흐른 시간' 계산 적용 ▼▼▼
    public void ApplyFieldDataToScene()
    {
        if (loadedUserData == null || loadedUserData.fields == null) return;

        // 1. 흐른 시간 계산하기
        double secondsPassed = 0;

        // 저장된 시간이 있다면 계산
        if (!string.IsNullOrEmpty(loadedUserData.logoutTime))
        {
            DateTime lastSaveTime;
            // 문자열을 날짜로 변환 시도
            if (DateTime.TryParse(loadedUserData.logoutTime, out lastSaveTime))
            {
                // 현재 시간(UTC) - 저장된 시간(UTC)
                TimeSpan timeSpan = DateTime.UtcNow - lastSaveTime;
                secondsPassed = timeSpan.TotalSeconds;
                Debug.Log($"⏰ 오프라인 경과 시간: {secondsPassed:F1}초");
            }
        }

        Field[] sceneFields = FindObjectsOfType<Field>();
        if (sceneFields.Length == 0) return;

        foreach (var savedField in loadedUserData.fields)
        {
            foreach (var realField in sceneFields)
            {
                if (realField.fieldID == savedField.fieldId)
                {
                    // 2. 흐른 시간만큼 밭의 남은 시간을 줄여줌 (심어져 있을 때만)
                    if (savedField.state != 0 && savedField.state != 4) // Empty(0)나 Ready(4)가 아닐 때
                    {
                        savedField.remainingTime -= (float)secondsPassed;

                        // 만약 시간이 다 지났으면 0으로 맞춤 (Field 스크립트가 알아서 자라게 처리함)
                        if (savedField.remainingTime <= 0)
                        {
                            savedField.remainingTime = 0;
                        }
                    }

                    // 3. 변경된 시간으로 밭 상태 복구
                    realField.RestoreState(savedField);
                    break;
                }
            }
        }
        Debug.Log("🌱 밭 상태 복구 완료! (시간 경과 적용됨)");
    }
}