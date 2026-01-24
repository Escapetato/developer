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

    // 가격 대신 '판매 불가' 텍스트를 띄울 곳
    public TextMeshProUGUI detailTotalPriceText;

    // 현재 보유 수량 표시
    public TextMeshProUGUI detailOwnedCountText;

    [Header("Quantity Controls")]
    public Button decreaseButton;
    public Button increaseButton;
    public TextMeshProUGUI quantityText;

    // 버튼 이미지 제어 변수
    private Image decreaseBtnImage;
    private Image increaseBtnImage;

    [Space(10)]
    [Header("Button Sprites (드래그해서 채워주세요)")]
    public Sprite arrowLeftOn;          // 켜짐 (<) - ui_amount_pre
    public Sprite arrowLeftOff;         // 꺼짐 (<) - ui_amount_pre_off
    public Sprite arrowRightOn;         // 켜짐 (>) - ui_amount_next
    public Sprite arrowRightOff;        // 꺼짐 (>) - ui_amount_next_off

    // 판매 버튼
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
        if (slotParent != null)
            slotParent.GetComponentsInChildren<ItemSlot>(slots);

        if (detailPanelObject != null) detailPanelObject.SetActive(false);

        // 버튼 이미지 자동 찾기
        if (decreaseButton != null)
        {
            decreaseButton.onClick.RemoveAllListeners();
            decreaseButton.onClick.AddListener(OnDecreaseQuantity);
            decreaseBtnImage = decreaseButton.GetComponent<Image>();
        }
        if (increaseButton != null)
        {
            increaseButton.onClick.RemoveAllListeners();
            increaseButton.onClick.AddListener(OnIncreaseQuantity);
            increaseBtnImage = increaseButton.GetComponent<Image>();
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

    private void UpdateDetailPanel(ItemData item)
    {
        if (item != null)
        {
            detailPanelObject.SetActive(true);

            if (detailImage != null) detailImage.sprite = item.itemIcon;
            if (detailNameText != null) detailNameText.text = item.itemName;

            int myCount = 0;
            if (InventoryManager.Instance.items.ContainsKey(item))
                myCount = InventoryManager.Instance.items[item];

            if (detailOwnedCountText != null)
                detailOwnedCountText.text = myCount.ToString();

            if (sellButton != null) sellButton.gameObject.SetActive(true);

            // [분기점] 작물(Crop) vs 그 외
            if (item.itemCategory == "Crop")
            {
                SetQuantityControlsActive(true);
                UpdateQuantityUI(); // 버튼 이미지 갱신

                if (sellButton != null)
                {
                    sellButton.interactable = (myCount > 0);
                    if (sellButtonText != null) sellButtonText.text = $"{currentSellQuantity}개 판매";
                }
            }
            else
            {
                // 작물이 아니면 수량 조절 숨김
                if (decreaseButton != null) decreaseButton.gameObject.SetActive(false);
                if (increaseButton != null) increaseButton.gameObject.SetActive(false);
                if (quantityText != null) quantityText.gameObject.SetActive(false);

                if (detailTotalPriceText != null)
                {
                    detailTotalPriceText.gameObject.SetActive(true);
                    detailTotalPriceText.text = "판매 불가";
                }

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
        if (detailTotalPriceText != null) detailTotalPriceText.text = totalEarnings.ToString();

        // 버튼 텍스트
        if (sellButtonText != null) sellButtonText.text = $"{currentSellQuantity}개 판매";

        // ▼▼▼ [핵심] 버튼 이미지 갱신 ▼▼▼
        UpdateArrowButtons();
    }

    private void UpdateArrowButtons()
    {
        if (selectedItem == null) return;
        int maxCount = InventoryManager.Instance.GetItemCount(selectedItem);

        // 1. 감소 버튼 (최소 1개)
        if (decreaseButton != null && decreaseBtnImage != null)
        {
            if (currentSellQuantity <= 1)
            {
                decreaseButton.interactable = false;
                if (arrowLeftOff != null) decreaseBtnImage.sprite = arrowLeftOff;
            }
            else
            {
                decreaseButton.interactable = true;
                if (arrowLeftOn != null) decreaseBtnImage.sprite = arrowLeftOn;
            }
        }

        // 2. 증가 버튼 (최대 보유량)
        if (increaseButton != null && increaseBtnImage != null)
        {
            if (currentSellQuantity >= maxCount)
            {
                increaseButton.interactable = false;
                if (arrowRightOff != null) increaseBtnImage.sprite = arrowRightOff;
            }
            else
            {
                increaseButton.interactable = true;
                if (arrowRightOn != null) increaseBtnImage.sprite = arrowRightOn;
            }
        }
    }


    public void OnRealSellClick()
    {
        if (selectedItem == null) return;
        if (selectedItem.itemCategory != "Crop") return;

        // 1. [안전장치] 판매할 아이템과 정보를 로컬 변수에 미리 복사 (에러 방지 핵심)
        ItemData itemToSell = selectedItem;
        int quantityToSell = currentSellQuantity;
        int totalEarnings = itemToSell.price * quantityToSell;

        // 2. 버튼 비활성화 (중복 클릭 방지)
        sellButton.interactable = false;

        // 3. 확인 팝업 띄우기
        UIManager.Instance.ShowConfirmPopup(
            $"{totalEarnings} 포잉에 판매하시겠습니까?",
            () => // [네] 눌렀을 때
            {
                // 미리 저장해둔 itemToSell 정보를 사용합니다.
                InventoryManager.Instance.RemoveItem(itemToSell, quantityToSell);
                PoingManager.Instance.IncreasePoing(totalEarnings);

                if (QuestManager.Instance != null)
                    QuestManager.Instance.NotifyAction(QuestConditionType.SellItem, itemToSell, quantityToSell);

                UIManager.Instance.ShowAlertPopup($"판매 완료! (+{totalEarnings} 포잉)");

                // [수정] 현재 선택된 아이템이 아직 남아있는지 체크 (null 여부 포함)
                if (selectedItem != null && InventoryManager.Instance.GetItemCount(selectedItem) > 0)
                {
                    currentSellQuantity = 1;
                    UpdateDetailPanel(selectedItem);
                    sellButton.interactable = true; // 남은 게 있으면 버튼 다시 활성화
                }
                else
                {
                    ClearSelection(); // 다 팔았으면 패널 닫기
                }
            },
            () => // [아니오] 눌렀을 때 (취소)
            {
                // 판매를 안 하기로 했으니 버튼을 다시 활성화해줍니다.
                sellButton.interactable = true;
            }
        );
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