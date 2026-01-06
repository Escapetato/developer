using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public enum SlotType { Inventory, Material, Result, Lab_Inventory, Store }

public class ItemSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("Core Info")]
    public ItemData item;
    public SlotType slotType;

    [Header("UI Components")]
    public Image itemIconDisplay;
    public Image slotBackground;
    public TextMeshProUGUI detailText;   // 가격 or 수량
    public TextMeshProUGUI itemNameText; // 아이템 이름

    public Image selectionBorder;

    [Header("Selection Sprites")]
    public Sprite selectedSprite;
    public Sprite defaultBackground;

    [Header("Settings")]
    public Sprite defaultIcon;

    void Start()
    {
        if (item != null)
        {
            int realCount = 1;
            if (InventoryManager.Instance != null && InventoryManager.Instance.items.ContainsKey(item))
            {
                realCount = InventoryManager.Instance.items[item];
            }
            SetSlot(item, realCount);
        }
    }

    void Awake()
    {
        if (selectionBorder == null && transform.Find("SelectionBorder") != null)
            selectionBorder = transform.Find("SelectionBorder").GetComponent<Image>();

        if (slotBackground == null)
            slotBackground = GetComponent<Image>();

        if (defaultBackground == null && slotBackground != null)
            defaultBackground = slotBackground.sprite;

        if (selectionBorder != null)
            selectionBorder.gameObject.SetActive(false);
    }

    public void SetSlot(ItemData newItem, int quantity = 1)
    {
        item = newItem;

        // 1. 아이콘 설정
        if (itemIconDisplay != null)
        {
            itemIconDisplay.sprite = newItem.itemIcon;
            itemIconDisplay.color = Color.white;
            itemIconDisplay.gameObject.SetActive(true);
        }

        // 2. 이름 표시
        if (itemNameText != null)
        {
            itemNameText.text = newItem.itemName;
            itemNameText.gameObject.SetActive(true);
        }

        // 3. 하단 텍스트 (가격 or 수량)
        if (detailText != null)
        {
            switch (slotType)
            {
                case SlotType.Inventory:
                case SlotType.Lab_Inventory:
                    detailText.text = "" + quantity.ToString();
                    detailText.gameObject.SetActive(true);
                    break;

                // ▼▼▼ [수정된 부분] 상점일 때 가격 표시 로직 ▼▼▼
                case SlotType.Store:
                    // 이름이 "???" 라면 가격도 "???"로 표시
                    if (newItem.itemName == "???")
                    {
                        detailText.text = "???";
                    }
                    else
                    {
                        // 아니면 정상 가격 표시
                        detailText.text = newItem.price.ToString();
                    }
                    detailText.gameObject.SetActive(true);
                    break;
                // ▲▲▲ 수정 끝 ▲▲▲

                default:
                    detailText.text = "";
                    detailText.gameObject.SetActive(false);
                    break;
            }
        }
    }

    public void ClearSlot()
    {
        item = null;

        if (itemIconDisplay != null)
        {
            if (defaultIcon != null)
            {
                itemIconDisplay.sprite = defaultIcon;
                itemIconDisplay.color = Color.white;
                itemIconDisplay.gameObject.SetActive(true);
            }
            else
            {
                itemIconDisplay.sprite = null;
                itemIconDisplay.gameObject.SetActive(false);
            }
        }

        if (detailText != null) detailText.gameObject.SetActive(false);
        if (itemNameText != null) itemNameText.gameObject.SetActive(false);
        if (selectionBorder != null) selectionBorder.gameObject.SetActive(false);

        if (slotBackground != null) slotBackground.sprite = defaultBackground;
    }

    public void SetSelected(bool isSelected)
    {
        if (selectionBorder != null) selectionBorder.gameObject.SetActive(isSelected);

        if (slotBackground != null && selectedSprite != null)
            slotBackground.sprite = isSelected ? selectedSprite : defaultBackground;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (slotType == SlotType.Inventory && eventData.clickCount == 1) InventoryUI.Instance.SelectSlot(this);
        else if (slotType == SlotType.Store && eventData.clickCount == 1) StoreUI.Instance.SelectSlot(this);
        else if (slotType == SlotType.Lab_Inventory)
        {
            if (eventData.clickCount == 1) ResearchLab.Instance.SelectSlot(this);
            else if (eventData.clickCount == 2 && this.item != null)
            {
                ResearchLab lab = ResearchLab.Instance;
                if (this.item.itemCategory == "Crop" && lab.materialSlot.item == null) { lab.materialSlot.SetSlot(this.item, 1); InventoryManager.Instance.RemoveItem(this.item, 1); lab.ClearSelection(); }
                else if (this.item.itemCategory == "Potion" && lab.potionSlot.item == null) { lab.potionSlot.SetSlot(this.item, 1); InventoryManager.Instance.RemoveItem(this.item, 1); lab.ClearSelection(); }
            }
        }
        else if (slotType == SlotType.Material && this.item != null) { InventoryManager.Instance.AddItem(this.item, 1); this.ClearSlot(); ResearchLab.Instance.ClearSelection(); }
        else if (slotType == SlotType.Result && this.item != null) { InventoryManager.Instance.AddItem(this.item, 1); this.ClearSlot(); }
    }
}