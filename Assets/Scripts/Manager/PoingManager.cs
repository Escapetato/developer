using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Firebase.Auth;

public class PoingManager : MonoBehaviour
{
    public static PoingManager Instance { get; private set; }

    // ▼▼▼ [수정 1] private -> public으로 변경 (DBManager가 가져갈 수 있게) ▼▼▼
    public int currentPoing = 500;

    public event Action<int> OnPoingChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        OnPoingChanged?.Invoke(currentPoing);
    }

    //  DB에 저장하라고 시키는 함수
    void SaveToDB()
    {
        if (FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            string myId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;

            // ▼▼▼ [수정 2] SaveGameData -> SaveAllData로 변경 ▼▼▼
            // (이제 currentPoing을 인자로 넘길 필요 없이, ID만 주면 알아서 가져갑니다)
            if (DBManager.Instance != null)
            {
                DBManager.Instance.SaveAllData(myId);
            }
        }
    }

    // DB에서 불러온 돈을 적용하는 함수 (DBManager가 호출함)
    public void SetLoadedPoing(int loadedPoing)
    {
        currentPoing = loadedPoing;
        OnPoingChanged?.Invoke(currentPoing);
        Debug.Log("서버에서 불러온 포잉 적용 완료: " + currentPoing);
    }

    public void AddPoing(int amount)
    {
        currentPoing += amount;
        OnPoingChanged?.Invoke(currentPoing);
        Debug.Log(amount + " 포잉 획득.");

        SaveToDB();
    }

    public void IncreasePoing(int amount)
    {
        currentPoing += amount;
        OnPoingChanged?.Invoke(currentPoing);
        Debug.Log(amount + " 포잉 판매 획득!");

        SaveToDB();
    }

    public void DecreasePoing(int amount)
    {
        currentPoing -= amount;
        OnPoingChanged?.Invoke(currentPoing);
        Debug.Log(amount + " 포잉 사용.");

        SaveToDB();
    }

    public bool HasEnoughPoing(int amount)
    {
        return (currentPoing >= amount);
    }

    public int GetPoing()
    {
        return currentPoing;
    }
}