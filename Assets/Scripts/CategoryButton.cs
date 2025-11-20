using UnityEngine;
using UnityEngine.UI;

public class CategoryButton : MonoBehaviour
{
    [Header("Category Info")]
    public string categoryName; // [중요] 이 버튼의 카테고리 (예: "Seed", "Tool")

    [Header("Sprites")]
    public Sprite defaultSprite;  // 이 버튼의 '기본' 이미지
    public Sprite selectedSprite; // 이 버튼의 '선택된' 이미지

    private Image backgroundImage;
    private Button button;

    void Awake()
    {
        backgroundImage = GetComponent<Image>();
        button = GetComponent<Button>();

        // 1. 버튼 클릭 시 StoreUI의 SetCategory 함수를 호출
        button.onClick.AddListener(OnClicked);

        // 2. 시작 시 '기본' 이미지로 강제 설정
        SetSelected(false);
    }

    // 버튼이 클릭되면 StoreUI에 '나'를 보냄
    void OnClicked()
    {
        StoreUI.Instance.SetCategory(this);
    }

    // StoreUI가 호출할 함수
    public void SetSelected(bool isSelected)
    {
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