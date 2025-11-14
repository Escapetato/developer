using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PoingDisplayUI : MonoBehaviour
{
    public Text poingText;

    void Awake()
    {
        if (poingText == null)
        {
            poingText = GetComponent<Text>();
        }
    }

    void OnEnable()
    {
        // PoingManager 구독
        PoingManager.Instance.OnPoingChanged += UpdatePoingText;
    }

    void OnDisable()
    {
        // 구독 취소
        PoingManager.Instance.OnPoingChanged -= UpdatePoingText;
    }

    // 함수 내용
    private void UpdatePoingText(int newPoingAmount)
    {
        poingText.text = "" + newPoingAmount.ToString();
    }
}