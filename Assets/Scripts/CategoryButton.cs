using UnityEngine;
using UnityEngine.UI;

public class CategoryButton : MonoBehaviour
{
    [Header("Category Info")]
    public string categoryName;

    [Header("Sprites")]
    public Sprite defaultSprite;
    public Sprite selectedSprite;

    private Image backgroundImage;
    private Button button;

    void Awake()
    {
        backgroundImage = GetComponent<Image>();
        button = GetComponent<Button>();

        button.onClick.AddListener(OnClicked);

        // 시작 시 선택 해제 상태로 초기화
        SetSelected(false);
    }

    void OnClicked()
    {
        // 1. 상점
        if (StoreUI.Instance != null && StoreUI.Instance.gameObject.activeInHierarchy)
        {
            StoreUI.Instance.SetCategory(this);
        }
        // 2. 인벤토리
        else if (InventoryUI.Instance != null && InventoryUI.Instance.gameObject.activeInHierarchy)
        {
            InventoryUI.Instance.SetCategory(this);
        }
        // ★ 3. [추가] 연구실이 켜져 있으면 -> 연구실에 알림
        else if (ResearchLab.Instance != null && ResearchLab.Instance.gameObject.activeInHierarchy)
        {
            // ResearchLab에는 CategoryButton을 받는 함수가 없고 string만 받으므로 이름만 넘김
            ResearchLab.Instance.SetCategory(categoryName);

            // (선택 이미지 변경 로직은 버튼 자체적으로 처리되거나 ResearchLab에서 처리해야 함)
            // 간단하게는 여기서 본인만 선택되고 나머지는 꺼지게 할 수도 있음
            SetSelected(true);
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (backgroundImage == null) return; 

        if (isSelected)
        {
            backgroundImage.sprite = selectedSprite;
        }
        else
        {
            backgroundImage.sprite = defaultSprite;
        }
    }

}