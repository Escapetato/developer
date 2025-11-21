using UnityEngine;
using UnityEngine.UI;

public class CategoryButton : MonoBehaviour
{
    [Header("Category Info")]
    public string categoryName;

    [Header("Sprites")]
    public Sprite defaultSprite;   // 선택 안 됐을 때 이미지
    public Sprite selectedSprite;  // 선택 됐을 때 이미지

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
        // 3. 연구실
        else if (ResearchLab.Instance != null && ResearchLab.Instance.gameObject.activeInHierarchy)
        {
            ResearchLab.Instance.SetCategory(categoryName);

            SetSelected(true);
        }
        // ★ 4. [추가] 도감이 켜져 있으면 -> 도감에 알림
        else if (CollectionUI.Instance != null && CollectionUI.Instance.gameObject.activeInHierarchy)
        {
            // CollectionUI의 SetCategoryButton 함수를 호출
            // 이 함수가 내부적으로 "나머지 버튼은 default이미지로, 나만 selected이미지로" 바꿔 줌
            CollectionUI.Instance.SetCategoryButton(this);
        }
    }

    // 이미지를 교체하는 함수 (기존 유지)
    public void SetSelected(bool isSelected)
    {
        if (backgroundImage == null) return;

        if (isSelected)
        {
            backgroundImage.sprite = selectedSprite; // 선택된 이미지로 변경
        }
        else
        {
            backgroundImage.sprite = defaultSprite;  // 기본 이미지로 변경
        }
    }
}