using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;
using System.Collections.Generic;

// --- 데이터 구조체들 ---

[System.Serializable]
public class InvenData
{
    public string itemId;
    public int count;
    public InvenData(string id, int c) { itemId = id; count = c; }
}

[System.Serializable]
public class QuestSaveData
{
    public int questId;
    public bool isClear;
    public int progress;

    public QuestSaveData(int id, bool clear, int prog) { questId = id; isClear = clear; progress = prog; }
}

[System.Serializable]
public class DogamData
{
    public string unlockedItemId;
}

[System.Serializable]
public class UserData
{
    public string userName;
    public int poing;

    public List<InvenData> inventory = new List<InvenData>();
    public List<QuestSaveData> questList = new List<QuestSaveData>();
    public List<string> dogamList = new List<string>(); // 레시피 도감

    // ▼ 진행 상황 저장용 변수들
    public bool isShopUnlocked;
    public List<string> unlockedItemNames = new List<string>(); // 해금된 아이템(씨앗, 작물 등) 이름

    public UserData(string name, int poing)
    {
        this.userName = name;
        this.poing = poing;
    }
}

public class DBManager : MonoBehaviour
{
    public static DBManager Instance;
    DatabaseReference reference;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    void Start()
    {
        string dbUrl = "https://whatthefarm-893d5-default-rtdb.firebaseio.com/";
        reference = FirebaseDatabase.GetInstance(dbUrl).RootReference;
    }

    public void SaveAllData(string userId)
    {
        int currentPoing = 0;
        if (PoingManager.Instance != null) currentPoing = PoingManager.Instance.GetPoing();

        UserData data = new UserData("감자농부", currentPoing);

        if (InventoryManager.Instance != null)
        {
            data.inventory = InventoryManager.Instance.GetInventorySaveData();
        }

        // 퀘스트 매니저한테 "저장용 데이터(QuestSaveData)" 달라고 하기
        if (QuestManager.Instance != null)
        {
            data.questList = QuestManager.Instance.GetQuestSaveList();
        }

        // 도감도 추가
        if (CollectionUI.Instance != null)
        {
            data.dogamList = CollectionUI.Instance.GetDogamSaveData();
        }

        // 5. 진행 상황 (상점, 아이템 해금) 수거 ★ [추가]
        if (GameProgressionManager.Instance != null)
        {
            data.isShopUnlocked = GameProgressionManager.Instance.isShopUnlocked;
            data.unlockedItemNames = GameProgressionManager.Instance.GetUnlockedItemNames();
        }

        string json = JsonUtility.ToJson(data);

        reference.Child("users").Child(userId).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted) Debug.Log("전체 저장 성공!");
            else Debug.LogError("저장 실패" + task.Exception);
        });
    }

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

                    Debug.Log("데이터 로드 성공!");

                    if (PoingManager.Instance != null)
                        PoingManager.Instance.SetLoadedPoing(data.poing);

                    // 진행 상황(해금 여부)을 가장 먼저 불러와야 함!
                    if (GameProgressionManager.Instance != null)
                    {
                        GameProgressionManager.Instance.LoadProgressionData(data.isShopUnlocked, data.unlockedItemNames);
                    }

                    // 그 다음 인벤토리 불러오기
                    if (InventoryManager.Instance != null)
                        InventoryManager.Instance.LoadInventoryData(data.inventory);

                    // 퀘스트 불러오기
                    if (QuestManager.Instance != null)
                        QuestManager.Instance.LoadQuestSaveList(data.questList);

                    // 도감은 진행 상황 로드가 끝난 뒤에 불러와야 함!
                    if (CollectionUI.Instance != null)
                        CollectionUI.Instance.LoadDogamData(data.dogamList);
                }
                else
                {
                    Debug.Log("신규 유저입니다.");
                    SaveAllData(userId);
                }
            }
        });
    }

    // [에러 해결용 1] 옛날 방식(SaveGameData)으로 불러도 -> SaveAllData로 연결해줌
    public void SaveGameData(string userId, int poing)
    {
        SaveAllData(userId);
    }

    // [에러 해결용 2] 옛날 방식(LoadGameData)으로 불러도 -> LoadAllData로 연결해줌
    public void LoadGameData(string userId)
    {
        LoadAllData(userId);
    }
}