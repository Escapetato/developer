using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Firebase.Auth; // (내 ID 알아야 저장하니까)

public class PoingManager : MonoBehaviour
{
    public static PoingManager Instance { get; private set; }

    [SerializeField] private int currentPoing = 500;

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
        // 1. 로그인되어 있는지 확인
        if (FirebaseAuth.DefaultInstance.CurrentUser != null)
        {
            string myId = FirebaseAuth.DefaultInstance.CurrentUser.UserId;
            // 2. DB매니저한테 저장해 달라고 부탁하기
            FindObjectOfType<DBManager>().SaveGameData(myId, currentPoing);
        }
    }

    // DB에서 불러온 돈을 적용하는 함수 (DBManager가 호출함)
    public void SetLoadedPoing(int loadedPoing)
    {
        currentPoing = loadedPoing; // 1. 돈 덮어씌우기
        OnPoingChanged?.Invoke(currentPoing); // 2. UI 갱신
        Debug.Log("서버에서 불러온 포잉 적용 완료: " + currentPoing);
    }

    public void AddPoing(int amount)
    {
        currentPoing += amount;
        OnPoingChanged?.Invoke(currentPoing);
        Debug.Log(amount + " 포잉 획득.");

        SaveToDB(); // 돈 벌었으니 저장
    }

    public void IncreasePoing(int amount)
    {
        currentPoing += amount;
        OnPoingChanged?.Invoke(currentPoing);
        Debug.Log(amount + " 포잉 판매 획득!");

        SaveToDB(); // 돈 벌었으니 저장
    }

    public void DecreasePoing(int amount)
    {
        currentPoing -= amount;
        OnPoingChanged?.Invoke(currentPoing);
        Debug.Log(amount + " 포잉 사용.");

        SaveToDB(); // 돈 썼으니 저장
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