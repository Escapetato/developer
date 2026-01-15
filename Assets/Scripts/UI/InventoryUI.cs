using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("Inventory Slots")]
    public Transform slotParent;
    private List<ItemSlot> slots;

    public ItemData selectedItem { get; private set; }
    public ItemSlot selectedSlot { get; private set; }

    [Header("Details Panel UI")]
    public GameObject detailPanelObject;
    public Image detailImage;
    public TextMeshProUGUI detailNameText;

    // ▼ 가격 대신 '판매 불가' 텍스트를 띄울 곳
    public TextMeshProUGUI detailTotalPriceText;

    // ▼ 현재 보유 수량 표시
    public TextMeshProUGUI detailOwnedCountText;

    // ▼ 수량 조절 버튼들
    public Button decreaseButton;
    public Button increaseButton;
    public TextMeshProUGUI quantityText;

    // ▼ 판매 버튼
    public Button sellButton;
    public TextMeshProUGUI sellButtonText;

    [Header("Category Buttons")]
    public List<CategoryButton> categoryButtons;
    public CategoryButton defaultCategoryButton;

    private string currentCategory = "Seed";
    private int currentSellQuantity = 1;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        slots = new List<ItemSlot>();
        slotParent.GetComponentsInChildren<ItemSlot>(slots);

        if (detailPanelObject != null) detailPanelObject.SetActive(false);

        // 버튼 리스너 연결
        if (decreaseButton != null)
        {
            decreaseButton.onClick.RemoveAllListeners();
            decreaseButton.onClick.AddListener(OnDecreaseQuantity);
        }
        if (increaseButton != null)
        {
            increaseButton.onClick.RemoveAllListeners();
            increaseButton.onClick.AddListener(OnIncreaseQuantity);
        }
        if (sellButton != null)
        {
            sellButton.onClick.RemoveAllListeners();
            sellButton.onClick.AddListener(OnRealSellClick);
        }
    }

    void OnEnable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += RedrawInventory;

        if (defaultCategoryButton != null) SetCategory(defaultCategoryButton);
        else
        {
            currentCategory = "Seed";
            RedrawInventory();
        }
        ClearSelection();
    }

    void OnDisable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= RedrawInventory;
    }

    public void SetCategory(CategoryButton clickedButton)
    {
        foreach (CategoryButton btn in categoryButtons) btn.SetSelected(false);
        clickedButton.SetSelected(true);
        currentCategory = clickedButton.categoryName;
        ClearSelection();
        RedrawInventory();
    }

    public void SelectSlot(ItemSlot slot)
    {
        if (selectedSlot != null) selectedSlot.SetSelected(false);

        if (slot.item != null)
        {
            selectedItem = slot.item;
            selectedSlot = slot;
            selectedSlot.SetSelected(true);

            currentSellQuantity = 1;
            UpdateDetailPanel(slot.item);
        }
        else ClearSelection();
    }

    // ▼▼▼ [수정됨] UI 갱신 로직 ▼▼▼
    private void UpdateDetailPanel(ItemData item)
    {
        if (item != null)
        {
            detailPanelObject.SetActive(true);

            // 기본 정보 표시 (아이콘, 이름)
            if (detailImage != null) detailImage.sprite = item.itemIcon;
            if (detailNameText != null) detailNameText.text = item.itemName;

            // 보유 수량 표시 (항상 표시)
            int myCount = 0;
            if (InventoryManager.Instance.items.ContainsKey(item))
                myCount = InventoryManager.Instance.items[item];

            if (detailOwnedCountText != null)
                detailOwnedCountText.text = myCount.ToString();

            // 판매 버튼 일단 켜두기 (위치 잡기용)
            if (sellButton != null) sellButton.gameObject.SetActive(true);

            // ------------------------------------------------
            // [분기점] 작물(Crop) vs 그 외
            // ------------------------------------------------
            if (item.itemCategory == "Crop")
            {
                // 1. 수량 조절 버튼 활성화
                SetQuantityControlsActive(true);

                // 2. 가격 및 수량 텍스트 정상 갱신
                UpdateQuantityUI();

                // 3. 판매 버튼 활성화 (보유량이 있을 때만 클릭 가능)
                if (sellButton != null)
                {
                    sellButton.interactable = (myCount > 0);
                    if (sellButtonText != null) sellButtonText.text = $"{currentSellQuantity}개 판매";
                }
            }
            else
            {
                // [작물이 아님 -> 판매 불가]

                // 1. 수량 조절 버튼(<, >)과 숫자 숨기기
                if (decreaseButton != null) decreaseButton.gameObject.SetActive(false);
                if (increaseButton != null) increaseButton.gameObject.SetActive(false);
                if (quantityText != null) quantityText.gameObject.SetActive(false);

                // 2. [요청사항] 포잉(가격) 텍스트에 "판매 불가" 띄우기
                if (detailTotalPriceText != null)
                {
                    detailTotalPriceText.gameObject.SetActive(true); // 텍스트는 켜고
                    detailTotalPriceText.text = "판매 불가";         // 내용을 변경
                }

                // 3. [요청사항] 판매 버튼은 보이지만 "판매 불가"로 변경
                if (sellButton != null)
                {
                    sellButton.interactable = false; // 클릭 금지
                    if (sellButtonText != null) sellButtonText.text = "판매 불가";
                }
            }
        }
    }

    // 수량 조절 버튼 활성/비활성 헬퍼 함수
    private void SetQuantityControlsActive(bool isActive)
    {
        if (decreaseButton != null) decreaseButton.gameObject.SetActive(isActive);
        if (increaseButton != null) increaseButton.gameObject.SetActive(isActive);
        if (quantityText != null) quantityText.gameObject.SetActive(isActive);

        // 가격 텍스트는 위에서 따로 제어하므로 여기서는 켜줍니다.
        if (detailTotalPriceText != null) detailTotalPriceText.gameObject.SetActive(true);
    }

    public void OnDecreaseQuantity()
    {
        if (currentSellQuantity > 1)
        {
            currentSellQuantity--;
            UpdateQuantityUI();
        }
    }

    public void OnIncreaseQuantity()
    {
        if (selectedItem == null) return;
        int myCount = InventoryManager.Instance.GetItemCount(selectedItem);

        if (currentSellQuantity < myCount)
        {
            currentSellQuantity++;
            UpdateQuantityUI();
        }
    }

    private void UpdateQuantityUI()
    {
        if (selectedItem == null) return;

        // 수량 텍스트
        if (quantityText != null) quantityText.text = currentSellQuantity.ToString();

        // 가격 계산
        int totalEarnings = selectedItem.price * currentSellQuantity;

        // 가격 텍스트
        if (detailTotalPriceText != null) detailTotalPriceText.text = totalEarnings.ToString();

        // 버튼 텍스트
        if (sellButtonText != null) sellButtonText.text = $"{currentSellQuantity}개 판매";
    }

    public void OnRealSellClick()
    {
        if (selectedItem == null) return;
        if (selectedItem.itemCategory != "Crop") return;

        int totalEarnings = selectedItem.price * currentSellQuantity;

        InventoryManager.Instance.RemoveItem(selectedItem, currentSellQuantity);
        PoingManager.Instance.IncreasePoing(totalEarnings);

        if (QuestManager.Instance != null)
            QuestManager.Instance.NotifyAction(QuestConditionType.SellItem, selectedItem, currentSellQuantity);

        UIManager.Instance.ShowAlertPopup($"판매 완료! (+{totalEarnings} 포잉)");
        SoundManager.Instance.PlaySFX("Money");

        if (InventoryManager.Instance.GetItemCount(selectedItem) > 0)
        {
            currentSellQuantity = 1;
            UpdateDetailPanel(selectedItem);
        }
        else
        {
            ClearSelection();
        }
    }

    private void RedrawInventory()
    {
        Dictionary<ItemData, int> allItems = InventoryManager.Instance.items;
        int i = 0;
        foreach (KeyValuePair<ItemData, int> itemPair in allItems)
        {
            if (itemPair.Key.itemCategory == currentCategory)
            {
                if (i < slots.Count)
                {
                    slots[i].gameObject.SetActive(true);
                    slots[i].SetSlot(itemPair.Key, itemPair.Value);
                    if (selectedSlot == slots[i]) selectedSlot.SetSelected(true);
                    i++;
                }
            }
        }
        for (int j = i; j < slots.Count; j++)
        {
            slots[j].ClearSlot();
            slots[j].gameObject.SetActive(false);
        }
        if (i == 0) ClearSelection();
    }

    public void ClearSelection()
    {
        if (selectedSlot != null) selectedSlot.SetSelected(false);
        selectedItem = null;
        selectedSlot = null;
        if (detailPanelObject != null) detailPanelObject.SetActive(false);
        if (sellButton != null) sellButton.gameObject.SetActive(false);
    }
}