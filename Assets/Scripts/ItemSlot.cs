using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public enum SlotType { Inventory, Material, Result, Lab_Inventory, Store }

public class ItemSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("Core Info")]
    public ItemData item;
    public SlotType slotType;

    [Header("UI Components (Optional)")]

    public Image itemIconDisplay;
    public Image slotBackground;
    public Text detailText; // (quantityText에서 이름 변경)
    public Image selectionBorder;

    [Header("Selection Sprites (Optional)")]

    public Sprite selectedSprite;
    private Sprite defaultSprite;

    void Awake()
    {

        if (detailText == null)
        {
            detailText = GetComponentInChildren<Text>(true);
        }

        if (selectionBorder == null && transform.Find("SelectionBorder") != null)
        {
            selectionBorder = transform.Find("SelectionBorder").GetComponent<Image>();
        }

        if (slotBackground == null)
        {
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
        ClearSlot();
    }

    public void SetSlot(ItemData newItem, int quantity = 1)
    {
        item = newItem;

        // --- 아이콘 표시 ---
        if (itemIconDisplay != null)
        {
            itemIconDisplay.sprite = newItem.itemIcon;
            itemIconDisplay.color = Color.white;
            itemIconDisplay.gameObject.SetActive(true);
        }

        // --- 텍스트 표시 (슬롯 타입에 따라 다름) ---
        if (detailText != null)
        {
            switch (slotType)
            {
                // 인벤토리 타입은 '수량' (xN) 표시
                case SlotType.Inventory:
                case SlotType.Lab_Inventory:
                    detailText.text = "x" + quantity.ToString();
                    detailText.gameObject.SetActive(true);
                    break;

                // 상점 타입은 '가격' (P: N) 표시
                case SlotType.Store:
                    detailText.text = "P: " + newItem.price.ToString();
                    detailText.gameObject.SetActive(true);
                    break;

                // 재료, 결과 슬롯은 텍스트 숨김
                case SlotType.Material:
                case SlotType.Result:
                    detailText.text = "";
                    detailText.gameObject.SetActive(false);
                    break;
            }
        }
    }

    public void ClearSlot()
    {
        item = null;

        // --- 아이콘 숨김 ---
        if (itemIconDisplay != null)
        {
            itemIconDisplay.sprite = null;
            itemIconDisplay.gameObject.SetActive(false);
        }

        // --- 텍스트 숨김 ---
        if (detailText != null)
        {
            detailText.text = "";
            detailText.gameObject.SetActive(false);
        }

        // --- 선택 테두리 숨김 ---
        if (selectionBorder != null)
        {
            selectionBorder.gameObject.SetActive(false);
        }

        // --- 배경 되돌리기 ---
        if (slotBackground != null)
        {
            slotBackground.sprite = defaultSprite;
        }
    }

    public void SetSelected(bool isSelected)
    {
        // --- 선택 테두리 켜고 끄기 ---
        if (selectionBorder != null)
        {
            selectionBorder.gameObject.SetActive(isSelected);
        }

        // --- 배경 스프라이트 변경 ---
        if (slotBackground != null && selectedSprite != null)
        {
            if (isSelected)
            {
                slotBackground.sprite = selectedSprite;
            }
            else
            {
                slotBackground.sprite = defaultSprite;
            }
        }
    }

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
            if (eventData.clickCount == 1)
            {
                ResearchLab.Instance.SelectSlot(this);
            }
            else if (eventData.clickCount == 2 && this.item != null)
            {
                ResearchLab lab = ResearchLab.Instance;
                ItemData itemToMove = this.item;

                if (itemToMove.itemCategory == "Crop" && lab.materialSlot.item == null)
                {
                    lab.materialSlot.SetSlot(itemToMove, 1);
                    InventoryManager.Instance.RemoveItem(itemToMove, 1);
                    lab.ClearSelection();
                }
                else if (itemToMove.itemCategory == "Potion" && lab.potionSlot.item == null)
                {
                    lab.potionSlot.SetSlot(itemToMove, 1);
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