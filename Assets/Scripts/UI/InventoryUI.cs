using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public TextMeshProUGUI detailPriceText;

    // 상세 정보창에 있는 팝업 열기 버튼
    public Button openSellPopupButton;

    [Header("Sell Popup Settings")]
    public GameObject sellPopupObject;      // 판매 수량 조절 팝업 패널
    public Image popupItemIcon;             // 팝업 내 아이콘
    public TextMeshProUGUI popupNameText;   // 팝업 내 이름
    public Slider popupSlider;              // 팝업 내 수량 조절 슬라이더
    public TextMeshProUGUI popupCountText;  // 팝업 내 수량 텍스트
    public TextMeshProUGUI popupPriceText;  // 팝업 내 총 가격 텍스트
    public Button popupConfirmButton;       // 판매 확정 버튼
    public Button popupCancelButton;        // 취소(닫기) 버튼

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
        if (sellPopupObject != null) sellPopupObject.SetActive(false);

        // 상세창의 판매 버튼(팝업 열기) 연결
        if (openSellPopupButton != null)
        {
            openSellPopupButton.onClick.RemoveAllListeners();
            openSellPopupButton.onClick.AddListener(OnOpenSellPopupClick);
        }

        // 팝업 내부 슬라이더 연결
        if (popupSlider != null)
        {
            popupSlider.onValueChanged.RemoveAllListeners();
            popupSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        // 팝업 내부 확정 버튼 연결
        if (popupConfirmButton != null)
        {
            popupConfirmButton.onClick.RemoveAllListeners();
            popupConfirmButton.onClick.AddListener(OnRealSellClick);
        }

        // 팝업 내부 취소 버튼 연결
        if (popupCancelButton != null)
        {
            popupCancelButton.onClick.RemoveAllListeners();
            popupCancelButton.onClick.AddListener(CloseSellPopup);
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
            UpdateDetailPanel(slot.item);
        }
        else ClearSelection();
    }

    // 오른쪽 상세 정보 패널 갱신
    private void UpdateDetailPanel(ItemData item)
    {
        if (item != null)
        {
            detailPanelObject.SetActive(true);

            if (detailImage != null) detailImage.sprite = item.itemIcon;
            if (detailNameText != null) detailNameText.text = item.itemName;
            if (detailPriceText != null) detailPriceText.text = item.price.ToString();

            // 보유 수량 확인
            int ownedCount = 0;
            if (InventoryManager.Instance.items.ContainsKey(item))
                ownedCount = InventoryManager.Instance.items[item];

            // 보유 수량이 있어야 판매 팝업 버튼 활성화
            if (openSellPopupButton != null)
                openSellPopupButton.interactable = (ownedCount > 0);
        }
    }

    // 판매 팝업 열기 (상세창 판매 버튼 클릭 시)
    public void OnOpenSellPopupClick()
    {
        if (selectedItem == null) return;

        int ownedCount = 0;
        if (InventoryManager.Instance.items.ContainsKey(selectedItem))
            ownedCount = InventoryManager.Instance.items[selectedItem];

        if (ownedCount <= 0) return;

        // 팝업 활성화 및 UI 초기화
        sellPopupObject.SetActive(true);

        if (popupItemIcon != null) popupItemIcon.sprite = selectedItem.itemIcon;
        if (popupNameText != null) popupNameText.text = selectedItem.itemName;

        // 슬라이더 설정 (최대값 = 보유 수량)
        if (popupSlider != null)
        {
            popupSlider.minValue = 1;
            popupSlider.maxValue = ownedCount;
            popupSlider.value = 1;
            currentSellQuantity = 1;
        }

        UpdatePopupTexts();
    }

    // 슬라이더 값 변경 시 호출
    public void OnSliderValueChanged(float value)
    {
        currentSellQuantity = (int)value;
        UpdatePopupTexts();
    }

    // 팝업 내 텍스트(수량, 총 가격) 갱신
    private void UpdatePopupTexts()
    {
        if (popupCountText != null)
            popupCountText.text = currentSellQuantity.ToString();

        if (selectedItem != null && popupPriceText != null)
        {
            int total = selectedItem.price * currentSellQuantity;
            popupPriceText.text = total.ToString() + " 포잉";
        }
    }

    // 실제 판매 로직 (팝업 내 확인 버튼 클릭 시)
    public void OnRealSellClick()
    {
        if (selectedItem == null) return;

        int totalPrice = selectedItem.price * currentSellQuantity;

        // 아이템 차감 및 재화 증가
        InventoryManager.Instance.RemoveItem(selectedItem, currentSellQuantity);
        PoingManager.Instance.IncreasePoing(totalPrice);

        // 알림 및 소리
        UIManager.Instance.ShowAlertPopup($"판매 완료! (+{totalPrice} 포잉)");
        SoundManager.Instance.PlaySFX("button");

        // 팝업 닫기 및 인벤토리 갱신
        CloseSellPopup();
        RedrawInventory();

        // 판매 후 잔여 수량에 따라 상세창 갱신 또는 선택 해제
        if (InventoryManager.Instance.items.ContainsKey(selectedItem))
            UpdateDetailPanel(selectedItem);
        else
            ClearSelection();
    }

    public void CloseSellPopup()
    {
        if (sellPopupObject != null) sellPopupObject.SetActive(false);
        SoundManager.Instance.PlaySFX("button");
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
    }
}