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
        // 1. 상점이 켜져 있으면 -> 상점에 알림
        if (StoreUI.Instance != null && StoreUI.Instance.gameObject.activeInHierarchy)
        {
            StoreUI.Instance.SetCategory(this);
        }
        // 2. 인벤토리가 켜져 있으면 -> 인벤토리에 알림
        else if (InventoryUI.Instance != null && InventoryUI.Instance.gameObject.activeInHierarchy)
        {
            InventoryUI.Instance.SetCategory(this);
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