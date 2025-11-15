using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    // 1. 싱글톤
    public static InventoryUI Instance { get; private set; }

    [Header("Inventory Slots")]
    public Transform slotParent;
    private List<ItemSlot> slots;

    // 2. 현재 선택한 아이템 (연구실로 보낼 아이템)
    public ItemData selectedItem { get; private set; }
    public ItemSlot selectedSlot { get; private set; }

    [Header("Details Panel")]
    public GameObject detailPanelObject; // Right_Detail_Panel 자체
    public Image detailImage;           // Detail_Image
    public Text detailNameText;         // Detail_Name_Text

    [Header("Category Buttons")]
    public List<CategoryButton> categoryButtons; // (Inspector에서 'CategoryButton' 스크립트 연결)
    public CategoryButton defaultCategoryButton; // (Inspector에서 'All' 또는 'Seed' 버튼 연결)

    private string currentCategory = "All";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        slots = new List<ItemSlot>();
        slotParent.GetComponentsInChildren<ItemSlot>(slots);

        if (detailPanelObject != null)
            detailPanelObject.SetActive(false);
    }

    void OnEnable()
    {
        InventoryManager.Instance.OnInventoryChanged += RedrawInventory;

        if (defaultCategoryButton != null)
        {
            SetCategory(defaultCategoryButton);
        }
        else if (categoryButtons != null && categoryButtons.Count > 0)
        {
            SetCategory(categoryButtons[0]);
        }
    }

    void OnDisable()
    {
        InventoryManager.Instance.OnInventoryChanged -= RedrawInventory;
    }

    // 카테고리 버튼 클릭 시 호출되는 함수
    public void SetCategory(CategoryButton clickedButton)
    {
        foreach (CategoryButton btn in categoryButtons)
        {
            btn.SetSelected(false);
        }
        clickedButton.SetSelected(true);
        currentCategory = clickedButton.categoryName;

        ClearSelection();
        RedrawInventory();
    }

    // 슬롯 선택 함수
    public void SelectSlot(ItemSlot slot)
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }

        if (slot.item != null)
        {
            selectedItem = slot.item;
            selectedSlot = slot;
            selectedSlot.SetSelected(true);
            UpdateDetailPanel(slot.item);
        }
        else
        {
            ClearSelection(); // 빈 슬롯 클릭 시 선택 해제
        }
    }

    // 선택 해제 함수
    public void ClearSelection()
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }
        selectedItem = null;
        selectedSlot = null;

        if (detailPanelObject != null)
            detailPanelObject.SetActive(false);
    }

    // 상세 정보 패널 업데이트 전용 함수
    private void UpdateDetailPanel(ItemData item)
    {
        if (item != null)
        {
            detailPanelObject.SetActive(true);
            detailImage.sprite = item.itemIcon;
            detailImage.color = Color.white;
            detailNameText.text = item.itemName;

            // [!!! 가격(Price) 표시 코드 제거 !!!]
        }
    }

    // 인벤토리 다시 그리기 함수
    private void RedrawInventory()
    {
        Dictionary<ItemData, int> allItems = InventoryManager.Instance.items;
        int i = 0;

        foreach (KeyValuePair<ItemData, int> itemPair in allItems)
        {
            if (currentCategory == "All" || itemPair.Key.itemCategory == currentCategory)
            {
                if (i < slots.Count)
                {
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
        }

        if (i == 0)
        {
            ClearSelection();
        }
    }
}