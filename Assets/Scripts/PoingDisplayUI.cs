using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class PoingDisplayUI : MonoBehaviour
{
    public TextMeshProUGUI poingText;

    void Awake()
    {
        if (poingText == null)
        {
            poingText = GetComponent<TextMeshProUGUI>();
        }
    }

    void OnEnable()
    {
        if (PoingManager.Instance != null)
        {
            PoingManager.Instance.OnPoingChanged += UpdatePoingText;
            UpdatePoingText(PoingManager.Instance.GetPoing());
        }
    }

    void OnDisable()
    {
        if (PoingManager.Instance != null)
        {
            PoingManager.Instance.OnPoingChanged -= UpdatePoingText;
        }
    }

    private void UpdatePoingText(int newPoingAmount)
    {
        poingText.text = newPoingAmount.ToString();
    }
}