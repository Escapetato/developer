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

    // ▼▼▼ [NEW] 그룹 오브젝트 변수 추가 ▼▼▼
    [Header("Info Groups (For Layout)")]
    public GameObject priceGroupObject; // 코인 아이콘 + 가격 텍스트가 묶인 그룹
    public GameObject ownedGroupObject; // 박스 아이콘 + 보유 텍스트가 묶인 그룹 (혹시 몰라 선언)

    public TextMeshProUGUI detailTotalPriceText;
    public TextMeshProUGUI detailOwnedCountText;

    public Button decreaseButton;
    public Button increaseButton;
    public TextMeshProUGUI quantityText;

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

    // ▼▼▼ 수정된 UpdateDetailPanel ▼▼▼
    private void UpdateDetailPanel(ItemData item)
    {
        if (item != null)
        {
            detailPanelObject.SetActive(true);

            if (detailImage != null) detailImage.sprite = item.itemIcon;
            if (detailNameText != null) detailNameText.text = item.itemName;

            // 보유 수량 갱신 (항상 표시)
            int myCount = 0;
            if (InventoryManager.Instance.items.ContainsKey(item))
                myCount = InventoryManager.Instance.items[item];

            if (detailOwnedCountText != null)
                detailOwnedCountText.text = myCount.ToString();

            // 판매 버튼 항상 켜두기 (나중에 interactable로 조절)
            if (sellButton != null) sellButton.gameObject.SetActive(true);

            // ---------------------------------------------------------
            // 작물(Crop) 여부에 따른 UI 배치 변경
            // ---------------------------------------------------------
            if (item.itemCategory == "Crop")
            {
                // [작물일 때]

                // 1. 가격 그룹(코인 등) 보이기 -> Layout Group이 자동으로 양옆 정렬함
                if (priceGroupObject != null) priceGroupObject.SetActive(true);

                // 2. 수량 조절 버튼 활성화
                SetQuantityControlsActive(true);
                UpdateQuantityUI();

                // 3. 판매 버튼 활성화 (개수 있으면)
                if (sellButton != null)
                {
                    sellButton.interactable = (myCount > 0);
                    if (sellButtonText != null) sellButtonText.text = $"{currentSellQuantity}개 판매";
                }
            }
            else
            {
                // [작물이 아닐 때]

                // 1. 가격 그룹(코인 등) 숨기기 -> Layout Group이 남은 '보유그룹'을 중앙 정렬함
                if (priceGroupObject != null) priceGroupObject.SetActive(false);

                // 2. 수량 조절 버튼 숨기기
                SetQuantityControlsActive(false);

                // 3. 판매 버튼 비활성화 + 텍스트 변경
                if (sellButton != null)
                {
                    sellButton.interactable = false;
                    if (sellButtonText != null) sellButtonText.text = "판매 불가";
                }
            }
        }
    }

    private void SetQuantityControlsActive(bool isActive)
    {
        if (decreaseButton != null) decreaseButton.gameObject.SetActive(isActive);
        if (increaseButton != null) increaseButton.gameObject.SetActive(isActive);
        if (quantityText != null) quantityText.gameObject.SetActive(isActive);
        // detailTotalPriceText는 priceGroupObject 안에 포함되어 있다면 굳이 여기서 안 꺼도 됨
        // 하지만 혹시 모르니 남겨둠
        if (detailTotalPriceText != null) detailTotalPriceText.gameObject.SetActive(isActive);
    }

    // ... (나머지 OnDecreaseQuantity, OnIncreaseQuantity, OnRealSellClick 등은 기존과 동일) ...

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
        if (quantityText != null) quantityText.text = currentSellQuantity.ToString();

        int totalEarnings = selectedItem.price * currentSellQuantity;
        if (detailTotalPriceText != null) detailTotalPriceText.text = totalEarnings.ToString();

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