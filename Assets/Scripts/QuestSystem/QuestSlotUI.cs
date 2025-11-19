using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestSlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("슬롯 이미지 (클릭 시 테두리)")]
    [SerializeField] private Sprite normalSprite;     
    [SerializeField] private Sprite selectedSprite;   

    private QuestData boundQuest;
    private QuestListController ownerController;

    // 현재 선택 여부
    private bool isSelected = false;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnClickSlot);
        }

        SetSelected(false);
    }

    // QuestManager에서 데이터 넘겨줄 때 호출
    public void Setup(QuestData data)
    {
        boundQuest = data;

        if (titleText != null)
        {
            if (data.type == QuestType.Daily)
            {
                titleText.text = "일일 퀘스트";
            }
            else
            {
                titleText.text = data.title;
            }
        }

        // 새로운 데이터 바인딩 시 초기화 
        SetSelected(false);
    }

    public void SetOwner(QuestListController controller)
    {
        ownerController = controller;
    }

    // 컨트롤러에서 호출하는 선택/해제 함수
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        var img = GetComponent<Image>();
        if (img == null) return;

        if (selected)
        {
            if (selectedSprite != null)
                img.sprite = selectedSprite;   
        }
        else
        {
            if (normalSprite != null)
                img.sprite = normalSprite;     
        }
    }


    public bool IsSelected()
    {
        return isSelected;
    }

    private void OnClickSlot()
    {
        // 클릭 상태 알림 
        if (ownerController != null)
        {
            ownerController.OnSlotClicked(this);
        }
    }

    public QuestData GetQuest()
    {
        return boundQuest;
    }
}