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

    // ★ [추가] 아이템 이름을 표시할 텍스트
    public TextMeshProUGUI itemNameText;

    public Image selectionBorder;

    [Header("Selection Sprites")]
    public Sprite selectedSprite;
    private Sprite defaultSprite;

    void Awake()
    {
        if (selectionBorder == null && transform.Find("SelectionBorder") != null)
            selectionBorder = transform.Find("SelectionBorder").GetComponent<Image>();

        if (slotBackground == null)
            slotBackground = GetComponent<Image>();

        if (slotBackground != null)
            defaultSprite = slotBackground.sprite;

        if (selectionBorder != null)
            selectionBorder.gameObject.SetActive(false);
    }

    public void SetSlot(ItemData newItem, int quantity = 1)
    {
        item = newItem;

        // 1. 아이콘
        if (itemIconDisplay != null)
        {
            itemIconDisplay.sprite = newItem.itemIcon;
            itemIconDisplay.color = Color.white;
            itemIconDisplay.gameObject.SetActive(true);
        }

        // 2. ★ [추가] 이름 표시 (이름 텍스트가 연결되어 있다면)
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
                    detailText.text = "x" + quantity.ToString();
                    detailText.gameObject.SetActive(true);
                    break;

                case SlotType.Store:
                    detailText.text = newItem.price.ToString(); // "P:" 뺌 (이미지처럼 숫자만 나오게)
                    detailText.gameObject.SetActive(true);
                    break;

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
        if (itemIconDisplay != null) itemIconDisplay.gameObject.SetActive(false);
        if (detailText != null) detailText.gameObject.SetActive(false);
        if (itemNameText != null) itemNameText.gameObject.SetActive(false); // 이름 숨기기
        if (selectionBorder != null) selectionBorder.gameObject.SetActive(false);
        if (slotBackground != null) slotBackground.sprite = defaultSprite;
    }

    // (나머지 OnPointerClick, SetSelected는 기존과 동일하므로 생략 가능, 그대로 두세요)
    public void SetSelected(bool isSelected)
    {
        if (selectionBorder != null) selectionBorder.gameObject.SetActive(isSelected);
        if (slotBackground != null && selectedSprite != null)
            slotBackground.sprite = isSelected ? selectedSprite : defaultSprite;
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