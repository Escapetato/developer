using UnityEngine;
using UnityEngine.UI;
using TMPro;  

public class QuestSlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI titleText;   

    // 해당 슬롯이 나타내는 퀘스트 
    private QuestData boundQuest;  

    // QuestManager에서 데이터 넘겨줄 때 호출
    public void Setup(QuestData data)
    {
        boundQuest = data;

        if (titleText != null)
            titleText.text = data.title;

        // 클릭 이벤트 시 오른쪽 상세 내용 띄우기 기능 확장 
    }

    public QuestData GetQuest()
    {
        return boundQuest;
    }
}

