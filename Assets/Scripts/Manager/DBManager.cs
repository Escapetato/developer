using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using System.Collections;

[Serializable]
public class UserData
{
    public string userName;
    public int poing;
    public bool isShopUnlocked;
    public string lastLoginDate;
    public string logoutTime; // 오프라인 시간 저장용

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

    // ▼▼▼ [사라졌던 핵심 변수 복구] ▼▼▼
    private bool isDataRestored = false;

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
        if (scene.name == "Main")
        {
            // ▼▼▼ 메인 씬 진입 시 저장 잠금 (0원 초기화 방지) ▼▼▼
            isDataRestored = false;
            Debug.Log("🚫 [안전장치] 데이터 복구 전까지 저장을 차단합니다.");

            StartCoroutine(DelayedRestore());
        }
        else
        {
            // 로그인 화면 등에서는 해제
            isDataRestored = true;
        }
    }

    IEnumerator DelayedRestore()
    {
        yield return null; // 1프레임 대기 (매니저/밭 생성 대기)

        Debug.Log("[데이터 복구] 매니저들에게 데이터 주입 시작");

        CheckDuplicateFieldIDs();

        ApplyDataToManagers();
        ApplyFieldDataToScene();

        // 복구 완료 후 저장 잠금 해제
        isDataRestored = true;
    }

    // 밭 ID 중복 감별사 (함수로 분리)
    void CheckDuplicateFieldIDs()
    {
        Field[] allFields = FindObjectsOfType<Field>();
        Debug.Log($"Main 씬에서 발견된 밭 개수: {allFields.Length}개");

        Dictionary<int, string> idCheck = new Dictionary<int, string>();

        foreach (var f in allFields)
        {
            // 부모 이름까지 포함해서 출력 (예: primary_fields > Field (1))
            string fieldName = $"{f.transform.parent.name} > {f.name}";

            if (idCheck.ContainsKey(f.fieldID))
            {
                // ★★★ 중복 발견! 범인 출력 ★★★
                Debug.LogError($"🚨 [중복 검거] ID {f.fieldID}번이 겹칩니다");
                Debug.LogError($"   1. 먼저 발견된: {idCheck[f.fieldID]}");
                Debug.LogError($"   2. 지금 발견된: {fieldName}");
                Debug.LogError("👉 [해결법] '잠긴 밭(lock_...)'에 Field 스크립트가 붙어 있다면 제거하거나, ID를 바꾸세요");
            }
            else
            {
                idCheck.Add(f.fieldID, fieldName);
            }
        }
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

    public void SaveAllData(string userId)
    {
        // ▼▼▼ [사라졌던 방어 코드 복구] ▼▼▼
        if (SceneManager.GetActiveScene().name == "Main" && isDataRestored == false)
        {
            Debug.LogWarning("⛔ [저장 거부] 아직 데이터 복구 중입니다! (초기화 방지됨)");
            return;
        }
        // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

        Debug.Log($"💾 [저장 시작] User ID: {userId}");
        try
        {
            if (loadedUserData == null) loadedUserData = new UserData("감자농부", 1000); 

            // 매니저 살아있으면 데이터 갱신
            if (PoingManager.Instance != null) loadedUserData.poing = PoingManager.Instance.currentPoing;

            loadedUserData.lastLoginDate = DateTime.Today.ToString("yyyy-MM-dd");
            loadedUserData.logoutTime = DateTime.UtcNow.ToString();

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

                    loadedUserData = new UserData("감자농부", 1000);

                    isDataRestored = true;
                    SaveAllData(userId);
                }
                if (onComplete != null) onComplete.Invoke();
            }
        });
    }

    private void OnApplicationQuit() { if (Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser != null) SaveAllData(Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId); }
    private void OnApplicationPause(bool pause) { if (pause && Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser != null) SaveAllData(Firebase.Auth.FirebaseAuth.DefaultInstance.CurrentUser.UserId); }

    public void ApplyFieldDataToScene()
    {
        if (loadedUserData == null || loadedUserData.fields == null) return;

        double secondsPassed = 0;
        if (!string.IsNullOrEmpty(loadedUserData.logoutTime))
        {
            DateTime lastSaveTime;
            if (DateTime.TryParse(loadedUserData.logoutTime, out lastSaveTime))
            {
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
                    if (savedField.state != 0 && savedField.state != 4)
                    {
                        savedField.remainingTime -= (float)secondsPassed;
                        if (savedField.remainingTime <= 0) savedField.remainingTime = 0;
                    }
                    realField.RestoreState(savedField);
                    break;
                }
            }
        }
        Debug.Log("🌱 밭 상태 복구 완료! (시간 경과 적용됨)");
    }
}