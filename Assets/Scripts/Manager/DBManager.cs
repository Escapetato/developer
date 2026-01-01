using UnityEngine;
using Firebase.Database;
using Firebase.Extensions;

// 저장할 데이터 목록 
[System.Serializable]
public class UserData
{
    public int poing;       // 포잉
    public string userName; // 닉네임


    // 생성자
    public UserData(string name, int poing)
    {
        this.userName = name;
        this.poing = poing;
    }
}

public class DBManager : MonoBehaviour
{
    DatabaseReference reference;

    void Start()
    {
        string dbUrl = "https://whatthefarm-893d5-default-rtdb.firebaseio.com/";

        reference = FirebaseDatabase.GetInstance(dbUrl).RootReference;
    }

    // 데이터 저장하기 (Save) 
    public void SaveGameData(string userId, int poing)
    {
        // 1. 포잉이랑 이름만 포장
        UserData data = new UserData("감자농부", poing);

        // 2. JSON 변환
        string json = JsonUtility.ToJson(data);

        // 3. 저장
        reference.Child("users").Child(userId).SetRawJsonValueAsync(json).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("저장 성공!");
            }
            else
            {
                Debug.LogError("저장 실패" + task.Exception);
            }
        });
    }

    // 데이터 불러오기 (Load)
    public void LoadGameData(string userId)
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

                    Debug.Log($"불러오기 성공! 포잉: {data.poing}");

                    // 진짜 (PoingManager)에 적용하기
                    if (PoingManager.Instance != null)
                    {
                        PoingManager.Instance.SetLoadedPoing(data.poing);
                    }
                }
                else
                {
                    Debug.Log("데이터 없음 (신규 유저)");
                    // 신규 유저면 기본값 500으로 저장
                    SaveGameData(userId, 500);
                }
            }
        });
    }
}