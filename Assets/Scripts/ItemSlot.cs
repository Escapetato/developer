using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public enum SlotType { Inventory, Material, Result, Lab_Inventory }

public class ItemSlot : MonoBehaviour, IPointerClickHandler 
{
    public ItemData item; // 이 슬롯이 현재 가지고 있는 아이템 정보
    public SlotType slotType; // [추가] Inspector에서 설정할 슬롯 타입

    private Image itemIcon;
    public Text quantityText;

    public Image selectionBorder;

    void Awake()
    {
        itemIcon = GetComponent<Image>();

        if (quantityText == null)
        {
            quantityText = GetComponentInChildren<Text>();
        }

        // [추가] 선택 테두리가 있다면 처음엔 숨김
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
        itemIcon.sprite = item.itemIcon;
        itemIcon.color = Color.white;

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

        itemIcon.sprite = null;
        itemIcon.color = Color.white;

        if (quantityText != null)
        {
            quantityText.text = "";
            quantityText.gameObject.SetActive(false);
        }

        if (selectionBorder != null)
        {
            selectionBorder.gameObject.SetActive(false);
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (selectionBorder != null)
        {
            selectionBorder.gameObject.SetActive(isSelected);
        }
    }


    // 슬롯이 클릭되었을 때 호출되는 함수
    public void OnPointerClick(PointerEventData eventData)
    {
        // 1. '메인 인벤토리' 슬롯을 클릭했을 때 (기존과 동일)
        if (slotType == SlotType.Inventory)
        {
            // 한 번 클릭: 선택
            if (eventData.clickCount == 1)
            {
                InventoryUI.Instance.SelectSlot(this);
            }
        }
        // 2. '연구실 안의 인벤토리' 슬롯을 클릭했을 때
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

                // 2c. 아이템 카테고리에 따라 적절한 혼합기 슬롯에 배치
                if (itemToMove.itemCategory == "Crop" && lab.materialSlot.item == null)
                {
                    // 작물이면 '재료' 슬롯에 배치
                    lab.materialSlot.SetItem(itemToMove, 1);
                    InventoryManager.Instance.RemoveItem(itemToMove, 1);
                    lab.ClearSelection();
                }
                else if (itemToMove.itemCategory == "Potion" && lab.potionSlot.item == null)
                {
                    // 포션이면 '포션' 슬롯에 배치
                    lab.potionSlot.SetItem(itemToMove, 1);
                    InventoryManager.Instance.RemoveItem(itemToMove, 1);
                    lab.ClearSelection();
                }
            }
        }
        // 3. '연구실 재료' 슬롯 (왼쪽 혼합기)을 클릭했을 때
        else if (slotType == SlotType.Material)
        {
            // [!!! 수정 !!!]
            if (this.item != null)
            {
                InventoryManager.Instance.AddItem(this.item, 1);
                this.ClearSlot();

                if (ResearchLab.Instance != null)
                    ResearchLab.Instance.ClearSelection();
            }
        }
        // 4. '결과' 슬롯 (기존과 동일)
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