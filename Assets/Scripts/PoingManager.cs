using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PoingManager : MonoBehaviour
{
    public static PoingManager Instance { get; private set; }

    // (Inspector에서 테스트용 초기 자금 설정)
    [SerializeField] private int currentPoing = 500;

    // 포잉이 변경될 때 UI에 알려주기 위한 이벤트
    public event Action<int> OnPoingChanged;

    void Awake()
    {
        // 싱글톤 인스턴스 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 게임 시작 시, UI에 현재 포잉을 알려 줌
        OnPoingChanged?.Invoke(currentPoing);
    }

    // 포잉 획득 함수
    public void AddPoing(int amount)
    {
        currentPoing += amount;
        // 포잉 변경
        OnPoingChanged?.Invoke(currentPoing);
        Debug.Log(amount + " 포잉 획득. 현재 포잉: " + currentPoing);
    }

    // 포잉 차감 함수
    public void DecreasePoing(int amount)
    {
        currentPoing -= amount;
        OnPoingChanged?.Invoke(currentPoing);
        Debug.Log(amount + " 포잉 사용. 현재 포잉: " + currentPoing);
    }

    // 포잉이 충분한지 확인하는 함수
    public bool HasEnoughPoing(int amount)
    {
        return (currentPoing >= amount);
    }
}