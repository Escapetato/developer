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

    // DBManager.cs 안에 있는 SaveAllData를 이걸로 교체하세요

    // DBManager.cs의 SaveAllData 함수를 이걸로 덮어씌우세요!

    public void SaveAllData(string userId)
    {
        // 1. 디버깅 시작 로그
        Debug.Log($"💾 [저장 시작] User ID: {userId}");

        try
        {
            UserData data = new UserData();

            // --- [1] 포잉 저장 ---
            if (PoingManager.Instance != null)
            {
                data.poing = PoingManager.Instance.currentPoing;
            }
            data.userName = "감자농부"; // (닉네임 시스템 있으면 교체)
            data.lastLoginDate = System.DateTime.Today.ToString("yyyy-MM-dd");


            // --- [2] 도감 & 진행도 저장 (안전장치 추가) ---
            if (GameProgressionManager.Instance != null)
            {
                data.isShopUnlocked = GameProgressionManager.Instance.isShopUnlocked;

                if (GameProgressionManager.Instance.unlockedItems != null)
                {
                    foreach (var item in GameProgressionManager.Instance.unlockedItems)
                        if (item != null) data.unlockedItemNames.Add(item.itemName);
                }

                if (GameProgressionManager.Instance.unlockedRecipes != null)
                {
                    foreach (var recipe in GameProgressionManager.Instance.unlockedRecipes)
                        if (recipe != null) data.unlockedRecipeNames.Add(recipe.name);
                }
            }


            // --- [3] 인벤토리 저장 ---
            if (InventoryManager.Instance != null && InventoryManager.Instance.items != null)
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


            // --- [4] 퀘스트 저장
            if (QuestManager.Instance != null)
            {
                if (QuestManager.Instance.allQuestList != null)
                {
                    foreach (var q in QuestManager.Instance.allQuestList)
                    {
                        if (q != null)
                        {
                            QuestSaveData qData = new QuestSaveData();
                            qData.key = q.key;
                            qData.state = (int)q.state;

                            if (q.currentCounts != null && q.currentCounts.Length > 0)
                            {
                                q.currentCount = q.currentCounts[0];
                            }
                            // ▲▲▲▲▲ [추가 끝] ▲▲▲▲▲

                            qData.currentCount = q.currentCount; // 이제 올바른 값이 저장됨
                            qData.rewardClaimed = q.rewardClaimed;
                            data.quests.Add(qData);
                        }
                    }
                }
            }


            // --- [5] 파이어베이스 전송 ---
            string json = JsonUtility.ToJson(data);

            if (reference == null)
            {
                // 혹시 연결 끊겼으면 재연결
                string dbUrl = "https://whatthefarm-893d5-default-rtdb.firebaseio.com/";
                reference = FirebaseDatabase.GetInstance(dbUrl).RootReference;
            }

            reference.Child("users").Child(userId).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted) Debug.Log("✅ [최종 저장 성공] 데이터 클라우드 업로드 완료!");
                else Debug.LogError("❌ [업로드 실패] : " + task.Exception);
            });

        }
        catch (System.Exception e)
        {
            // ★★★ 여기서 범인을 잡습니다! ★★★
            Debug.LogError($"❌ [저장 중단됨] 저장하다가 에러가 났습니다!\n내용: {e.Message}\n위치: {e.StackTrace}");
        }
    }

    public void LoadAllData(string userId)
    {
        reference.Child("users").Child(userId).GetValueAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                // [CASE 1] 데이터가 있을 때 (기존 유저)
                if (snapshot.Exists)
                {
                    string json = snapshot.GetRawJsonValue();
                    UserData data = JsonUtility.FromJson<UserData>(json);

                    Debug.Log("📥 로드 시작...");

                    if (PoingManager.Instance != null)
                        PoingManager.Instance.SetLoadedPoing(data.poing);

                    if (GameProgressionManager.Instance != null)
                        GameProgressionManager.Instance.LoadProgression(data.isShopUnlocked, data.unlockedItemNames, data.unlockedRecipeNames);

                    if (InventoryManager.Instance != null)
                        InventoryManager.Instance.LoadInventory(data.inventory);

                    if (QuestManager.Instance != null)
                        QuestManager.Instance.LoadQuestData(data.quests, data.lastLoginDate);
                }
                // [CASE 2] 데이터가 없을 때 (신규 유저 / DB 초기화 직후)
                else
                {
                    Debug.Log("신규 유저 -> 초기 데이터 저장");

                    if (QuestManager.Instance != null)
                    {
                        Debug.Log("[DBManager] QuestManager 찾음. 로딩 완료 신호 보냄.");
                        QuestManager.Instance.LoadQuestData(null, "");
                    }
                    else
                    {
                        // ★ 만약 이 로그가 뜬다면, QuestManager가 너무 늦게 켜지는 것임!
                        Debug.LogError("[DBManager] QuestManager가 아직 없습니다! (Instance is null)");
                    }

                    SaveAllData(userId);
                }
            }
        });
    }
}