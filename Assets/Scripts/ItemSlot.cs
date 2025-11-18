using UnityEngine;
using UnityEngine.UI; // Image 컴포넌트 때문에 필요
using UnityEngine.EventSystems;
using TMPro; // [필수] 네임스페이스 추가

public enum SlotType { Inventory, Material, Result, Lab_Inventory, Store }

public class ItemSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("Core Info")]
    public ItemData item;
    public SlotType slotType;

    [Header("UI Components (Optional)")]
    public Image itemIconDisplay;
    public Image slotBackground;

    // [변경] Text -> TextMeshProUGUI
    public TextMeshProUGUI detailText;

    public Image selectionBorder;

    [Header("Selection Sprites (Optional)")]
    public Sprite selectedSprite;
    private Sprite defaultSprite;

    void Awake()
    {
        // [변경] 자동으로 찾을 때도 TextMeshProUGUI를 찾아야 함
        if (detailText == null)
        {
            detailText = GetComponentInChildren<TextMeshProUGUI>(true);
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
            defaultSprite = slotBackground.sprite;
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
                case SlotType.Inventory:
                case SlotType.Lab_Inventory:
                    detailText.text = "" + quantity.ToString();
                    detailText.gameObject.SetActive(true);
                    break;

                case SlotType.Store:
                    detailText.text = "" + newItem.price.ToString();
                    detailText.gameObject.SetActive(true);
                    break;

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

        if (itemIconDisplay != null)
        {
            itemIconDisplay.sprite = null;
            itemIconDisplay.gameObject.SetActive(false);
        }

        if (detailText != null)
        {
            detailText.text = "";
            detailText.gameObject.SetActive(false);
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

    public void SetSelected(bool isSelected)
    {
        if (selectionBorder != null)
        {
            selectionBorder.gameObject.SetActive(isSelected);
        }

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
        // 로직 변경 없음 (그대로 사용)
        if (slotType == SlotType.Inventory)
        {
            if (eventData.clickCount == 1)
            {
                InventoryUI.Instance.SelectSlot(this);
            }
        }
        else if (slotType == SlotType.Store)
        {
            if (eventData.clickCount == 1)
            {
                StoreUI.Instance.SelectSlot(this);
            }
        }
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