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

    [Header("Details Panel")]
    public GameObject detailPanelObject;
    public Image detailImage;
    public TextMeshProUGUI detailNameText;

    // ▼ [NEW] 판매 시 받을 '총 금액' 표시 텍스트
    public TextMeshProUGUI detailTotalPriceText;

    // ▼ [NEW] 현재 보유 수량 표시 (상세창 내)
    public TextMeshProUGUI detailOwnedCountText;

    // ▼ [NEW] 수량 조절 버튼
    public Button decreaseButton;      // (<)
    public Button increaseButton;      // (>)
    public TextMeshProUGUI quantityText; // 판매할 수량 (1)

    // ▼ [CHANGED] 판매 버튼
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

            // 선택 시 판매 수량 1로 초기화
            currentSellQuantity = 1;
            UpdateDetailPanel(slot.item);
        }
        else ClearSelection();
    }

    // ▼▼▼ 여기가 핵심 수정 부분입니다 ▼▼▼
    private void UpdateDetailPanel(ItemData item)
    {
        if (item != null)
        {
            detailPanelObject.SetActive(true);

            if (detailImage != null) detailImage.sprite = item.itemIcon;
            if (detailNameText != null) detailNameText.text = item.itemName;

            // 1. 현재 보유 수량 가져오기 및 표시 (이건 항상 보여줌)
            int myCount = 0;
            if (InventoryManager.Instance.items.ContainsKey(item))
                myCount = InventoryManager.Instance.items[item];

            if (detailOwnedCountText != null)
                detailOwnedCountText.text = myCount.ToString();

            // 2. 카테고리에 따라 UI 분기 처리
            if (item.itemCategory == "Crop")
            {
                // [작물일 때] -> 판매 UI 모두 켜기

                // 수량 조절 버튼들 활성화 (< 1 >)
                SetQuantityControlsActive(true);

                // 판매 버튼 활성화 (개수가 있어야 활성화)
                if (sellButton != null)
                {
                    sellButton.gameObject.SetActive(true); // 버튼 자체를 보이게 함
                    sellButton.interactable = (myCount > 0);
                    if (sellButtonText != null) sellButtonText.text = $"{currentSellQuantity}개 판매";
                }

                // 가격 및 텍스트 갱신
                UpdateQuantityUI();
            }
            else
            {
                // [작물이 아닐 때] -> 판매 UI 모두 끄기

                // 수량 조절 버튼들 숨김
                SetQuantityControlsActive(false);

                // 판매 버튼 자체를 아예 숨김 (요청하신 부분)
                if (sellButton != null)
                {
                    sellButton.gameObject.SetActive(false);
                }
            }
        }
    }

    // 수량 조절 버튼 및 가격 텍스트 표시 여부 제어
    private void SetQuantityControlsActive(bool isActive)
    {
        if (decreaseButton != null) decreaseButton.gameObject.SetActive(isActive);
        if (increaseButton != null) increaseButton.gameObject.SetActive(isActive);
        if (quantityText != null) quantityText.gameObject.SetActive(isActive);

        // 총 가격 텍스트도 안 쓸 거면 숨김
        if (detailTotalPriceText != null) detailTotalPriceText.gameObject.SetActive(isActive);
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

        // 1. 수량 텍스트
        if (quantityText != null)
            quantityText.text = currentSellQuantity.ToString();

        // 2. 총 판매 가격 계산
        int totalEarnings = selectedItem.price * currentSellQuantity;

        // 3. 가격 텍스트 갱신
        if (detailTotalPriceText != null)
            detailTotalPriceText.text = totalEarnings.ToString();

        // 4. 판매 버튼 텍스트
        if (sellButtonText != null)
            sellButtonText.text = $"{currentSellQuantity}개 판매";
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

                    if (selectedSlot == slots[i])
                    {
                        selectedSlot.SetSelected(true);
                    }
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