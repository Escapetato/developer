using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class QuestDetailUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descText;
    // 필요하면 추가: 조건, 보상 등등

    public void Show(QuestData data)
    {
        if (data == null) return;

        titleText.text = data.title;
        descText.text  = data.questDesc;   // QuestData 안 필드 이름에 맞게!
        // 조건, 보상 등도 여기서 채우기
    }

    public void Clear()
    {
        titleText.text = "";
        descText.text  = "";
    }
}