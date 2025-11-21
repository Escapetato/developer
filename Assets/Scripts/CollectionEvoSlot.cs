using UnityEngine;
using UnityEngine.UI;

public class CollectionEvoSlot : MonoBehaviour
{
    [Header("UI Components")]
    public Image iconImage;           // 아이콘 (물약 or 물음표)
    public Button button;             // 버튼
    public Image slotBackgroundImage; // 배경 (테두리 포함된 판)

    [Header("UI Settings")]
    [Range(0.1f, 1f)]
    public float iconScale = 0.7f;    // 아이콘 크기 (0.7배)

    [Header("Assets")]
    public Sprite questionMarkSprite; // 잠겼을 때 뜰 '???' 이미지
    public Sprite defaultBackground;  // 선택 안 됨 (회색 배경)
    public Sprite selectedBackground; // 선택됨 (밝은 배경)

    private EvolutionRecipe linkedRecipe;
    private bool isSlotUnlocked = false; // 내 슬롯의 잠금 상태

    // ★ 외부에서 이 슬롯이 해금됐는지(isUnlocked) 알려줘야 함
    public void Setup(EvolutionRecipe recipe, bool isUnlocked)
    {
        linkedRecipe = recipe;
        isSlotUnlocked = isUnlocked;

        gameObject.SetActive(true);
        SetSelected(false); // 기본 배경(회색)으로 시작

        // 1. 데이터가 없는 빈 슬롯 -> 투명 처리
        if (recipe == null)
        {
            if (iconImage != null) iconImage.color = Color.clear;
            if (button != null) button.interactable = false;
            return;
        }

        // 2. 데이터가 있음 -> 아이콘 표시
        if (iconImage != null)
        {
            iconImage.color = Color.white;
            iconImage.transform.localScale = Vector3.one * iconScale;

            if (isUnlocked)
            {
                // [상황 A] 진화 성공함 -> 물약 그림 보여줌
                if (recipe.potion != null)
                    iconImage.sprite = recipe.potion.itemIcon;
            }
            else
            {
                // [상황 B] 아직 못 깸 (또는 작물 자체가 잠김) -> 물음표 보여줌
                if (questionMarkSprite != null)
                    iconImage.sprite = questionMarkSprite;
            }
        }

        // 3. 버튼 설정
        if (button != null)
        {
            button.interactable = true; // 잠겨도 눌러서 "잠겨있음" 메시지는 띄워야 하니까
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnSlotClicked);
        }
    }

    private void OnSlotClicked()
    {
        if (isSlotUnlocked)
        {
            // 해금됐으면 도감 팝업 띄우기
            CollectionUI.Instance.OnEvoSlotClicked(this, linkedRecipe);
        }
        else
        {
            // 안 됐으면 안내 메시지
            Debug.Log("🔒 아직 발견하지 못한 진화입니다.");
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (slotBackgroundImage == null) return;

        // 선택 여부에 따라 배경 이미지 통째로 갈아끼우기
        if (isSelected)
        {
            if (selectedBackground != null)
                slotBackgroundImage.sprite = selectedBackground;
        }
        else
        {
            if (defaultBackground != null)
                slotBackgroundImage.sprite = defaultBackground;
        }
    }
}