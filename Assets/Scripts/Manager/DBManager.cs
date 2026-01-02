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

    public List<string> unlockedItemNames = new List<string>();
    public List<InventorySaveData> inventory = new List<InventorySaveData>();
    public List<QuestSaveData> quests = new List<QuestSaveData>();

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

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
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

    // [SAVE]
    public void SaveAllData(string userId)
    {
        int currentPoing = 0;
        if (PoingManager.Instance != null) currentPoing = PoingManager.Instance.currentPoing;

        UserData data = new UserData("감자농부", currentPoing);

        // 1. 진행도 & 도감 (아이템 + 레시피)
        if (GameProgressionManager.Instance != null)
        {
            data.isShopUnlocked = GameProgressionManager.Instance.isShopUnlocked;

            // 아이템 저장
            foreach (var item in GameProgressionManager.Instance.unlockedItems)
            {
                if (item != null) data.unlockedItemNames.Add(item.itemName);
            }

            // ▼▼▼ [수정됨] 레시피 저장 (EvolutionRecipe) ▼▼▼
            foreach (var recipe in GameProgressionManager.Instance.unlockedRecipes)
            {
                if (recipe != null) data.unlockedRecipeNames.Add(recipe.name);
            }
        }

        // 2. 인벤토리
        if (InventoryManager.Instance != null)
        {
            foreach (var kvp in InventoryManager.Instance.items)
            {
                if (kvp.Key != null)
                {
                    InventorySaveData invData = new InventorySaveData();
                    invData.itemName = kvp.Key.itemName;
                    invData.amount = kvp.Value;
                    data.inventory.Add(invData);
                }
            }
        }

        // 3. 퀘스트
        if (QuestManager.Instance != null)
        {
            foreach (var q in QuestManager.Instance.allQuestList)
            {
                QuestSaveData qData = new QuestSaveData();
                qData.key = q.key;
                qData.state = (int)q.state;
                qData.currentCount = q.currentCount;
                qData.rewardClaimed = q.rewardClaimed;
                data.quests.Add(qData);
            }
        }

        string json = JsonUtility.ToJson(data);
        reference.Child("users").Child(userId).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted) Debug.Log("✅ 저장 성공!");
            else Debug.LogError("❌ 저장 실패: " + task.Exception);
        });
    }

    // [LOAD]
    public void LoadAllData(string userId)
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

                    Debug.Log("📥 로드 시작...");

                    if (PoingManager.Instance != null)
                        PoingManager.Instance.SetLoadedPoing(data.poing);

                    // ▼▼▼ [수정됨] 레시피 이름 리스트도 같이 넘김 ▼▼▼
                    if (GameProgressionManager.Instance != null)
                        GameProgressionManager.Instance.LoadProgression(data.isShopUnlocked, data.unlockedItemNames, data.unlockedRecipeNames);

                    if (InventoryManager.Instance != null)
                        InventoryManager.Instance.LoadInventory(data.inventory);

                    if (QuestManager.Instance != null)
                        QuestManager.Instance.LoadQuestData(data.quests);
                }
                else
                {
                    Debug.Log("신규 유저 -> 초기 데이터 저장");
                    SaveAllData(userId);
                }
            }
        });
    }
}