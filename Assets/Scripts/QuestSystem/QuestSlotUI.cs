
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

    [Header("새로 열린 퀘스트 (빨간 점)")]
    [SerializeField] private GameObject newDotObject;

    [Header("일일퀘스트 선택 상태 계속 유지(보상 완료 버튼 후에도)")]
    [SerializeField] private Image borderImage; // 실제 테두리(라인) Image

    private QuestData boundQuest;
    private bool isDailySlot = false;
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
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        //SetSelected(false);
    }

    // QuestManager에서 데이터 넘겨줄 때 호출
    public void Setup(QuestData data)
    {
        boundQuest = data;
        isDailySlot = (data != null && data.type == QuestType.Daily);

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
        RefreshNewDot(); 

    }

    public void SetOwner(QuestListController controller)
    {
        ownerController = controller;
    }

    // 컨트롤러에서 호출하는 선택/해제 함수
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        // 1) 테두리(라인)용 이미지가 지정되어 있으면 그걸 최우선으로 사용
        if (borderImage != null)
        {
            if (selected)
            {
                if (selectedSprite != null) borderImage.sprite = selectedSprite;
            }
            else
            {
                if (normalSprite != null) borderImage.sprite = normalSprite;
            }
            return;
        }

        // 2) fallback: 슬롯 자신에 Image가 있으면 그걸 사용
        var img = GetComponent<Image>();
        if (img == null)
        {
            Debug.LogWarning($"[QuestSlotUI] No Image / borderImage on {name}. " +
                             $"Please assign 'borderImage' in prefab or add Image component.");
            return;
        }

        if (selected)
        {
            if (selectedSprite != null) img.sprite = selectedSprite;
        }
        else
        {
            if (normalSprite != null) img.sprite = normalSprite;
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

    public bool IsDailySlot()
    {
        return isDailySlot;
    }

    public void RefreshNewDot()
    {
        if (newDotObject == null)
            return;

        bool show = false;

        if (boundQuest == null)
        {
            newDotObject.SetActive(false);
            return;
        }

        if (isDailySlot)
        {
            var qm = QuestManager.Instance;
            if (qm != null && qm.dailyQuests != null)
            {
                foreach (var q in qm.dailyQuests)
                {
                    if (q == null) continue;
                    if ((q.state == QuestState.Active || q.state == QuestState.Completed) && q.isNewlyOpened)
                    {
                        show = true;
                        break;
                    }
                }
            }
        }
        else
        {
            show = boundQuest.isNewlyOpened;
        }

        newDotObject.SetActive(show);
    }

}