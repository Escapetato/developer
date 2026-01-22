using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreUI : MonoBehaviour
{
    public static StoreUI Instance { get; private set; }

    [Header("Store Slots")]
    public Transform slotParent;
    private List<ItemSlot> slots;

    [Header("Databases")]
    public StoreDatabase storeDB;
    public ItemData lockedSeedItem;
    public ItemData randomSeedItem;

    private ItemData selectedItem;
    private ItemSlot selectedSlot;

    [Header("Details Panel UI")]
    public GameObject detailPanelObject;
    public Image detailImage;
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailTotalPriceText;
    public TextMeshProUGUI detailOwnedText;

    [Header("Quantity Controls")]
    public Button decreaseButton;
    public Button increaseButton;
    public TextMeshProUGUI quantityText;

    // ▼▼▼ [수정] 버튼 자체의 이미지를 바꿀 변수들 ▼▼▼
    private Image decreaseBtnImage; // 버튼에 붙은 Image 컴포넌트 (코드에서 찾음)
    private Image increaseBtnImage; // 버튼에 붙은 Image 컴포넌트 (코드에서 찾음)

    [Space(10)]
    [Header("Button Sprites (이미지 파일 연결)")]
    // 여기에 유니티 프로젝트 창에 있는 PNG 파일들을 드래그해서 넣으세요
    public Sprite leftBtnOn;   // ui_amount_pre (갈색)
    public Sprite leftBtnOff;  // ui_amount_pre_off (회색)
    public Sprite rightBtnOn;  // ui_amount_next (갈색)
    public Sprite rightBtnOff; // ui_amount_next_off (회색)
    // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

    public Button buyButton;
    public TextMeshProUGUI buyButtonText;

    [Header("Category Buttons")]
    public List<CategoryButton> categoryButtons;
    public CategoryButton defaultCategoryButton;

    private string currentCategory = "All";
    private int currentBuyQuantity = 1;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        slots = new List<ItemSlot>();
        slotParent.GetComponentsInChildren<ItemSlot>(slots);

        if (detailPanelObject != null) detailPanelObject.SetActive(false);

        // ▼▼▼ [핵심] 버튼 오브젝트에서 바로 Image 컴포넌트를 가져옴 ▼▼▼
        if (decreaseButton != null)
        {
            decreaseButton.onClick.RemoveAllListeners();
            decreaseButton.onClick.AddListener(OnDecreaseQuantity);
            decreaseBtnImage = decreaseButton.GetComponent<Image>(); // 이거 가져와서 이미지 교체함
        }

        if (increaseButton != null)
        {
            increaseButton.onClick.RemoveAllListeners();
            increaseButton.onClick.AddListener(OnIncreaseQuantity);
            increaseBtnImage = increaseButton.GetComponent<Image>(); // 이거 가져와서 이미지 교체함
        }
        // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(OnRealPurchaseClick);
        }
    }

    void OnEnable()
    {
        if (defaultCategoryButton != null) SetCategory(defaultCategoryButton);
        else if (categoryButtons != null && categoryButtons.Count > 0) SetCategory(categoryButtons[0]);
    }

    public void SetCategory(CategoryButton clickedButton)
    {
        foreach (CategoryButton btn in categoryButtons) btn.SetSelected(false);
        clickedButton.SetSelected(true);
        currentCategory = clickedButton.categoryName;
        ClearSelection();
        RedrawStore();
    }

    private void RedrawStore()
    {
        int i = 0;
        if (storeDB == null) return;

        foreach (ItemData item in storeDB.itemsForSale)
        {
            if (i >= slots.Count) break;

            if (item.itemCategory == currentCategory || currentCategory == "All")
            {
                slots[i].gameObject.SetActive(true);
                if (item.isDefaultUnlocked || GameProgressionManager.Instance.IsItemUnlocked(item))
                    slots[i].SetSlot(item);
                else
                    slots[i].SetSlot(lockedSeedItem);
                i++;
            }
        }

        if (currentCategory == "Seed" || currentCategory == "All")
        {
            if (i < slots.Count && randomSeedItem != null)
            {
                slots[i].gameObject.SetActive(true);
                slots[i].SetSlot(randomSeedItem);
                i++;
            }
        }

        for (int j = i; j < slots.Count; j++)
        {
            slots[j].ClearSlot();
            slots[j].gameObject.SetActive(false);
        }
        ClearSelection();
    }

    public void SelectSlot(ItemSlot slot)
    {
        if (selectedSlot != null) selectedSlot.SetSelected(false);

        if (slot.item == null)
        {
            ClearSelection();
            return;
        }

        selectedItem = slot.item;
        selectedSlot = slot;
        selectedSlot.SetSelected(true);

        currentBuyQuantity = 1;

        if (selectedItem == lockedSeedItem) UpdateDetailPanel(lockedSeedItem, false);
        else if (selectedItem == randomSeedItem) UpdateDetailPanel(randomSeedItem, true);
        else UpdateDetailPanel(selectedItem, true);
    }

    private void UpdateDetailPanel(ItemData item, bool isUnlocked)
    {
        if (item != null)
        {
            detailPanelObject.SetActive(true);

            if (detailImage != null) detailImage.sprite = item.itemIcon;
            if (detailNameText != null) detailNameText.text = item.itemName;

            if (buyButton != null) buyButton.gameObject.SetActive(true);

            int myCount = InventoryManager.Instance.GetItemCount(item);
            if (detailOwnedText != null)
            {
                detailOwnedText.text = myCount.ToString();
            }

            // 1. 잠긴 아이템
            if (item.itemName == "???")
            {
                SetQuantityControlsActive(false);
                if (detailTotalPriceText != null) detailTotalPriceText.text = "???";

                if (buyButton != null)
                {
                    buyButton.interactable = false;
                    if (buyButtonText != null) buyButtonText.text = "해금 필요";
                }
            }
            // 2. 도구 (Tool)
            else if (item.itemCategory == "Tool")
            {
                SetQuantityControlsActive(false);

                if (detailTotalPriceText != null) detailTotalPriceText.text = item.price.ToString();

                if (myCount > 0)
                {
                    if (buyButton != null)
                    {
                        buyButton.interactable = false;
                        if (buyButtonText != null) buyButtonText.text = "보유 중";
                    }
                }
                else
                {
                    if (buyButton != null)
                    {
                        buyButton.interactable = isUnlocked;
                        if (buyButtonText != null) buyButtonText.text = "구매하기";
                    }
                }
            }
            // 3. 일반 아이템 (수량 조절 가능)
            else
            {
                SetQuantityControlsActive(true);
                UpdateQuantityUI(); // 여기서 버튼 이미지를 갱신함

                if (buyButton != null)
                {
                    buyButton.interactable = isUnlocked;
                }
            }
        }
    }

    private void SetQuantityControlsActive(bool isActive)
    {
        if (decreaseButton != null) decreaseButton.gameObject.SetActive(isActive);
        if (increaseButton != null) increaseButton.gameObject.SetActive(isActive);
        if (quantityText != null) quantityText.gameObject.SetActive(isActive);
    }

    public void OnDecreaseQuantity()
    {
        if (currentBuyQuantity > 1)
        {
            currentBuyQuantity--;
            UpdateQuantityUI();
        }
    }

    public void OnIncreaseQuantity()
    {
        if (selectedItem == null) return;

        int myPoing = PoingManager.Instance.GetPoing();
        int price = selectedItem.price > 0 ? selectedItem.price : 999999;
        int maxAffordable = myPoing / price;
        if (maxAffordable == 0) maxAffordable = 1;

        int finalMax = Mathf.Min(maxAffordable, 99);

        if (currentBuyQuantity < finalMax)
        {
            currentBuyQuantity++;
            UpdateQuantityUI();
        }
        else
        {
            UIManager.Instance.ShowAlertPopup("최대 수량입니다.");
        }
    }

    private void UpdateQuantityUI()
    {
        if (selectedItem == null) return;

        // 텍스트 갱신
        if (quantityText != null)
            quantityText.text = currentBuyQuantity.ToString();

        int totalCost = selectedItem.price * currentBuyQuantity;
        if (detailTotalPriceText != null)
            detailTotalPriceText.text = totalCost.ToString();

        if (buyButtonText != null)
            buyButtonText.text = $"{currentBuyQuantity}개 구매";

        // ▼▼▼ [핵심] 버튼 이미지 교체 로직 ▼▼▼
        UpdateButtonSprites();
    }

    private void UpdateButtonSprites()
    {
        // 최대 구매 가능 개수 계산
        int myPoing = PoingManager.Instance.GetPoing();
        int price = selectedItem.price > 0 ? selectedItem.price : 999999;
        int maxAffordable = myPoing / price;
        if (maxAffordable == 0) maxAffordable = 1;
        int finalMax = Mathf.Min(maxAffordable, 99);

        // 1. 왼쪽(-) 버튼 처리
        if (decreaseButton != null && decreaseBtnImage != null)
        {
            if (currentBuyQuantity <= 1)
            {
                // 최소값이면 비활성화 + 회색 이미지
                decreaseButton.interactable = false;
                if (leftBtnOff != null) decreaseBtnImage.sprite = leftBtnOff;
            }
            else
            {
                // 아니면 활성화 + 원래 이미지
                decreaseButton.interactable = true;
                if (leftBtnOn != null) decreaseBtnImage.sprite = leftBtnOn;
            }
        }

        // 2. 오른쪽(+) 버튼 처리
        if (increaseButton != null && increaseBtnImage != null)
        {
            if (currentBuyQuantity >= finalMax)
            {
                // 최대값이면 비활성화 + 회색 이미지
                increaseButton.interactable = false;
                if (rightBtnOff != null) increaseBtnImage.sprite = rightBtnOff;
            }
            else
            {
                // 아니면 활성화 + 원래 이미지
                increaseButton.interactable = true;
                if (rightBtnOn != null) increaseBtnImage.sprite = rightBtnOn;
            }
        }
    }

    public void OnRealPurchaseClick()
    {
        if (selectedItem == null) return;

        int totalCost = selectedItem.price * currentBuyQuantity;

        if (PoingManager.Instance.HasEnoughPoing(totalCost))
        {
            PoingManager.Instance.DecreasePoing(totalCost);
            InventoryManager.Instance.AddItem(selectedItem, currentBuyQuantity);

            NotifyPurchaseToQuest(selectedItem, currentBuyQuantity);

            UIManager.Instance.ShowAlertPopup($"{selectedItem.itemName} {currentBuyQuantity}개 구매 완료!");
            SoundManager.Instance.PlaySFX("Money");

            UpdateDetailPanel(selectedItem, true);
        }
        else
        {
            UIManager.Instance.ShowAlertPopup("포잉이 부족합니다!");
        }
    }

    public void ClearSelection()
    {
        if (selectedSlot != null) selectedSlot.SetSelected(false);
        selectedItem = null;
        selectedSlot = null;
        if (detailPanelObject != null) detailPanelObject.SetActive(false);
        if (buyButton != null) buyButton.gameObject.SetActive(false);
    }

    private void NotifyPurchaseToQuest(ItemData item, int quantity)
    {
        if (QuestManager.Instance == null || item == null) return;

        switch (item.itemCategory)
        {
            case "Seed": QuestManager.Instance.NotifyAction(QuestConditionType.BuySeed, item, quantity); break;
            case "Potion": QuestManager.Instance.NotifyAction(QuestConditionType.BuyPotion, item, quantity); break;
            case "Tool": QuestManager.Instance.NotifyAction(QuestConditionType.BuyTool, item, quantity); break;
            case "Theme": QuestManager.Instance.NotifyAction(QuestConditionType.BuyTheme, item, quantity); break;
        }
    }
}