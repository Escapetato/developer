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
        itemIcon.color = new Color(1, 1, 1, 0);

        if (quantityText != null)
        {
            quantityText.text = "";
            quantityText.gameObject.SetActive(false);
        }

        // 비울 때 선택 테두리도 해제
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
        // 1. '메인 인벤토리' 슬롯을 클릭했을 때
        if (slotType == SlotType.Inventory)
        {
            InventoryUI.Instance.SelectSlot(this);
        }
        // 2. [추가] '연구실 안의 인벤토리' 슬롯을 클릭했을 때
        else if (slotType == SlotType.Lab_Inventory)
        {
            // ResearchLab의 선택 함수를 호출
            ResearchLab.Instance.SelectSlot(this);
        }
        // 3. '연구실 재료' 슬롯 (왼쪽 혼합기)을 클릭했을 때
        else if (slotType == SlotType.Material)
        {
            // 3a. 연구실 인벤토리에서 선택한 아이템이 있다면 (배치하기)
            if (ResearchLab.Instance.selectedItem != null)
            {
                this.SetItem(ResearchLab.Instance.selectedItem, 1);
                InventoryManager.Instance.RemoveItem(ResearchLab.Instance.selectedItem, 1);
                ResearchLab.Instance.ClearSelection();
            }
            // 3b. 선택한 아이템이 없고, 이 슬롯에 아이템이 있다면 (회수하기)
            else if (this.item != null)
            {
                InventoryManager.Instance.AddItem(this.item, 1);
                this.ClearSlot();
            }
        }
        // 4. '결과' 슬롯 (인벤토리/연구실 공통)
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