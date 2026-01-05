using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("Details Panel")]
    public GameObject detailPanelObject;
    public Image detailImage;
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailPriceText;

    public Button openBuyPopupButton;

    [Header("Category Buttons")]
    public List<CategoryButton> categoryButtons;
    public CategoryButton defaultCategoryButton;

    // 구매 수량 조절 팝업 (인벤토리 판매 팝업과 비슷한 구조)
    [Header("Buy Popup Settings")]
    public GameObject buyPopupObject;       // 팝업 패널
    public Image popupItemIcon;             // 팝업 안 아이콘
    public TextMeshProUGUI popupNameText;   // 팝업 안 이름
    public Slider popupSlider;              // 팝업 안 슬라이더
    public TextMeshProUGUI popupCountText;  // 팝업 안 수량 텍스트
    public TextMeshProUGUI popupTotalCostText; // 팝업 안 총 가격 텍스트
    public Button popupConfirmButton;       // "구매" 확정 버튼
    public Button popupCancelButton;        // "취소" 버튼

    private string currentCategory = "All";
    private int currentBuyQuantity = 1; // 현재 설정된 구매 개수

    public TextMeshProUGUI buyButtonText;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        slots = new List<ItemSlot>();
        slotParent.GetComponentsInChildren<ItemSlot>(slots);

        if (detailPanelObject != null) detailPanelObject.SetActive(false);
        if (buyPopupObject != null) buyPopupObject.SetActive(false); // 시작할 때 팝업 끄기

        // 1. 상세창의 [구매] 버튼 -> 팝업 열기 연결
        if (openBuyPopupButton != null)
        {
            openBuyPopupButton.onClick.RemoveAllListeners();
            openBuyPopupButton.onClick.AddListener(OnOpenBuyPopupClick);
        }

        // 2. 팝업 슬라이더 연결
        if (popupSlider != null)
        {
            popupSlider.onValueChanged.RemoveAllListeners();
            popupSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        // 3. 팝업 확정 버튼 -> 진짜 구매
        if (popupConfirmButton != null)
        {
            popupConfirmButton.onClick.RemoveAllListeners();
            popupConfirmButton.onClick.AddListener(OnRealPurchaseClick);
        }

        // 4. 팝업 취소 버튼 -> 닫기
        if (popupCancelButton != null)
        {
            popupCancelButton.onClick.RemoveAllListeners();
            popupCancelButton.onClick.AddListener(CloseBuyPopup);
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

        if (selectedItem == lockedSeedItem) UpdateDetailPanel(lockedSeedItem, false);
        else if (selectedItem == randomSeedItem) UpdateDetailPanel(randomSeedItem, true);
        else UpdateDetailPanel(selectedItem, true);
    }

    private void UpdateDetailPanel(ItemData item, bool isUnlocked)
    {
        if (item != null)
        {
            detailPanelObject.SetActive(true);

            // 이미지 & 이름 갱신
            if (detailImage != null) detailImage.sprite = item.itemIcon;
            if (detailNameText != null) detailNameText.text = item.itemName;

            Debug.Log($"아이템: {item.itemName}, 카테고리: {item.itemCategory}, 내 개수: {InventoryManager.Instance.GetItemCount(item)}");

            detailPanelObject.SetActive(true);

            // 1. 인벤토리 확인
            int myCount = InventoryManager.Instance.GetItemCount(item);

            // 2. 도구이면서 가지고 있는지 확인
            bool isOwnedTool = (item.itemCategory == "Tool" && myCount > 0);

            if (isOwnedTool)
            {
                // [보유 중일 때]
                if (detailPriceText != null) detailPriceText.text = "구매할 수 없습니다."; // 가격 텍스트 변경

                if (openBuyPopupButton != null)
                {
                    openBuyPopupButton.gameObject.SetActive(true);
                    openBuyPopupButton.interactable = false; // 버튼 비활성화
                }

                // ★ 버튼 글자도 "보유 중"으로 변경
                if (buyButtonText != null) buyButtonText.text = "보유 중";
            }
            else
            {
                // [미보유 상태일 때]
                if (detailPriceText != null) detailPriceText.text = item.price.ToString(); // 가격 표시

                if (openBuyPopupButton != null)
                {
                    openBuyPopupButton.gameObject.SetActive(true);
                    openBuyPopupButton.interactable = isUnlocked; // 잠금 해제 여부에 따라 활성
                }

                // ★ 버튼 글자를 원래대로 "결제"로 복구
                if (buyButtonText != null) buyButtonText.text = "결제";
            }
        }
    }

    public void ClearSelection()
    {
        if (selectedSlot != null) selectedSlot.SetSelected(false);
        selectedItem = null;
        selectedSlot = null;
        if (detailPanelObject != null) detailPanelObject.SetActive(false);
        if (openBuyPopupButton != null) openBuyPopupButton.gameObject.SetActive(false);
    }

    // [1단계] 구매 버튼 클릭 -> 팝업 열기
    public void OnOpenBuyPopupClick()
    {
        if (selectedItem == null) return;

        // 1. 현재 내 돈 확인
        int myPoing = PoingManager.Instance.GetPoing();
        int price = selectedItem.price;

        if (price <= 0) return; // 공짜 아이템이 아니라면 방어 코드

        // 2. 최대로 살 수 있는 개수 계산 (내 돈 / 가격)
        int maxCanBuy = myPoing / price;

        if (maxCanBuy <= 0)
        {
            UIManager.Instance.ShowAlertPopup("포잉이 부족합니다!");
            return;
        }

        // 3. 팝업 UI 세팅
        buyPopupObject.SetActive(true);
        if (popupItemIcon != null) popupItemIcon.sprite = selectedItem.itemIcon;
        if (popupNameText != null) popupNameText.text = selectedItem.itemName;

        // 4. 슬라이더 세팅
        if (popupSlider != null)
        {
            popupSlider.minValue = 1;
            popupSlider.maxValue = maxCanBuy; // 내 돈으로 살 수 있는 최대치
                                              // 너무 많이 사는거 방지하려면 99개 제한 걸어도 됨
                                              // if (maxCanBuy > 99) popupSlider.maxValue = 99; 

            popupSlider.value = 1;
            currentBuyQuantity = 1;
        }

        UpdatePopupTexts();
        SoundManager.Instance.PlaySFX("PopupOpen");
    }

    // ★ [2단계] 슬라이더 움직임
    public void OnSliderValueChanged(float value)
    {
        currentBuyQuantity = (int)value;
        UpdatePopupTexts();
    }

    private void UpdatePopupTexts()
    {
        if (popupCountText != null)
            popupCountText.text = currentBuyQuantity.ToString();

        if (selectedItem != null && popupTotalCostText != null)
        {
            int total = selectedItem.price * currentBuyQuantity;
            popupTotalCostText.text = total.ToString() + " 포잉";
        }
    }

    // ★ [3단계] 진짜 구매 (팝업 내 확인 버튼)
    public void OnRealPurchaseClick()
    {
        if (selectedItem == null) return;

        int totalCost = selectedItem.price * currentBuyQuantity;

        // 돈 확인 (한 번 더 안전장치)
        if (PoingManager.Instance.HasEnoughPoing(totalCost))
        {
            // 1. 돈 차감
            PoingManager.Instance.DecreasePoing(totalCost);

            // 2. 아이템 추가
            InventoryManager.Instance.AddItem(selectedItem, currentBuyQuantity);

            // 3. 알림 & 소리
            UIManager.Instance.ShowAlertPopup($"{selectedItem.itemName} {currentBuyQuantity}개 구매 완료!");

            // 4. 팝업 닫기
            CloseBuyPopup();
        }
        else
        {
            UIManager.Instance.ShowAlertPopup("포잉이 부족합니다!");
        }
    }

    public void CloseBuyPopup()
    {
        if (buyPopupObject != null) buyPopupObject.SetActive(false);
        SoundManager.Instance.PlaySFX("Button");
    }
}