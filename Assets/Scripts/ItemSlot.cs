using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum SlotType { Inventory, Material, Result, Lab_Inventory, Store }

public class ItemSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("Core Info")]
    public ItemData item;
    public SlotType slotType;

    [Header("UI Components (Must be connected)")]

    public Image itemIconDisplay; // (Inspector에서 'Item_Icon_Image' 자식 연결)
    public Image slotBackground;  // (Inspector에서 '자기 자신'의 Image 컴포넌트 연결)
    public Text quantityText;
    public Image selectionBorder;

    [Header("Selection Sprites (Must be connected)")]

    public Sprite selectedSprite; // 선택됐을 때의 '진한' 스프라이트
    private Sprite defaultSprite; // 원래 '연한' 스프라이트 (자동 저장)

    void Awake()
    {
        // 1. 자식에서 Price_Text (QuantityText) 찾기
        if (quantityText == null)
        {
            quantityText = GetComponentInChildren<Text>(true);
        }

        // 2. 자식에서 SelectionBorder 찾기
        if (selectionBorder == null && transform.Find("SelectionBorder") != null)
        {
            selectionBorder = transform.Find("SelectionBorder").GetComponent<Image>();
        }

        // 3. 배경 이미지 찾기 및 원래 스프라이트 저장
        if (slotBackground == null)
        {
            // '부모'의 Image는 배경
            slotBackground = GetComponent<Image>();
        }

        if (slotBackground != null)
        {
            defaultSprite = slotBackground.sprite; // 원래 스프라이트 저장
        }

        if (selectionBorder != null)
        {
            selectionBorder.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        ClearSlot(); // 시작할 땐 무조건 빈 슬롯으로
    }

    // 아이템과 수량을 설정
    public void SetItem(ItemData newItem, int quantity)
    {
        item = newItem;

        // '자식'의 아이콘을 변경
        itemIconDisplay.sprite = newItem.itemIcon;
        itemIconDisplay.color = Color.white;
        itemIconDisplay.gameObject.SetActive(true); // 켜기

        if (quantityText != null)
        {
            quantityText.text = "x" + quantity.ToString();
            quantityText.gameObject.SetActive(true);
        }
    }

    // 슬롯 비우기
    public void ClearSlot()
    {
        item = null;

        // '자식'의 아이콘만 숨김
        if (itemIconDisplay != null)
        {
            itemIconDisplay.sprite = null;
            itemIconDisplay.gameObject.SetActive(false); // 끄기
        }

        if (quantityText != null)
        {
            quantityText.text = "";
            quantityText.gameObject.SetActive(false);
        }

        if (selectionBorder != null)
        {
            selectionBorder.gameObject.SetActive(false);
        }

        if (slotBackground != null)
        {
            slotBackground.sprite = defaultSprite;
        }
    }

    // 상점 슬롯 전용: 아이템과 '가격'을 설정
    public void SetStoreSlot(ItemData newItem)
    {
        item = newItem;

        // '자식'의 아이콘을 변경
        itemIconDisplay.sprite = newItem.itemIcon;
        itemIconDisplay.color = Color.white;
        itemIconDisplay.gameObject.SetActive(true); // 켜기

        if (quantityText != null)
        {
            quantityText.text = "P: " + newItem.price.ToString();
            quantityText.gameObject.SetActive(true);
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (selectionBorder != null)
        {
            selectionBorder.gameObject.SetActive(isSelected);
        }

        // 배경 스프라이트(이미지) 변경
        if (slotBackground != null)
        {
            if (isSelected)
            {
                slotBackground.sprite = selectedSprite; // 선택됨 (진한 이미지)
            }
            else
            {
                slotBackground.sprite = defaultSprite; // 선택 해제 (원래 이미지)
            }
        }
    }

    // (이전 '더블 클릭' 로직이 포함된 완전한 버전)
    public void OnPointerClick(PointerEventData eventData)
    {
        // 1. '메인 인벤토리' 슬롯
        if (slotType == SlotType.Inventory)
        {
            if (eventData.clickCount == 1)
            {
                InventoryUI.Instance.SelectSlot(this);
            }
        }
        // 1-A. '상점' 슬롯
        else if (slotType == SlotType.Store)
        {
            if (eventData.clickCount == 1)
            {
                StoreUI.Instance.SelectSlot(this);
            }
        }
        // 2. '연구실 안의 인벤토리' 슬롯
        else if (slotType == SlotType.Lab_Inventory)
        {
            // 2a. 한 번 클릭 (선택)
            if (eventData.clickCount == 1)
            {
                ResearchLab.Instance.SelectSlot(this);
            }
            // 2b. 두 번 클릭 (자동 배치)
            else if (eventData.clickCount == 2 && this.item != null)
            {
                ResearchLab lab = ResearchLab.Instance;
                ItemData itemToMove = this.item;

                if (itemToMove.itemCategory == "Crop" && lab.materialSlot.item == null)
                {
                    lab.materialSlot.SetItem(itemToMove, 1);
                    InventoryManager.Instance.RemoveItem(itemToMove, 1);
                    lab.ClearSelection();
                }
                else if (itemToMove.itemCategory == "Potion" && lab.potionSlot.item == null)
                {
                    lab.potionSlot.SetItem(itemToMove, 1);
                    InventoryManager.Instance.RemoveItem(itemToMove, 1);
                    lab.ClearSelection();
                }
            }
        }
        // 3. '연구실 재료' 슬롯 (왼쪽 혼합기)
        else if (slotType == SlotType.Material)
        {
            if (this.item != null)
            {
                InventoryManager.Instance.AddItem(this.item, 1);
                this.ClearSlot();

                if (ResearchLab.Instance != null)
                    ResearchLab.Instance.ClearSelection();
            }
        }
        // 4. '결과' 슬롯
        else if (slotType == SlotType.Result)
        {
            if (this.item != null)
            {
                InventoryManager.Instance.AddItem(this.item, 1);
                this.ClearSlot();
            }
        }
    }
}