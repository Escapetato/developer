using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // [필수] TextMeshPro 사용

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

    // [변경] 텍스트들을 TextMeshPro로 변경
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailQuantityText; // (원래 가격 뜨던 곳에 연결)

    [Header("Category Buttons")]
    public List<CategoryButton> categoryButtons;
    public CategoryButton defaultCategoryButton; 

    // 기본값을 "All"에서 "Seed"로 변경
    private string currentCategory = "Seed";

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
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RedrawInventory;
        }

        // [수정] 시작할 때 무조건 defaultCategoryButton(Seed)을 누른 것처럼 처리
        if (defaultCategoryButton != null)
        {
            SetCategory(defaultCategoryButton);
        }
        else
        {
            // 혹시 연결 안 했을 때를 대비해 강제로 Seed로 그림
            currentCategory = "Seed";
            RedrawInventory();
        }
    }

    void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RedrawInventory;
        }
    }

    // 1. 카테고리 버튼 로직 (상점과 100% 동일)
    public void SetCategory(CategoryButton clickedButton)
    {
        // 모든 버튼 선택 해제
        foreach (CategoryButton btn in categoryButtons)
        {
            btn.SetSelected(false);
        }

        // 클릭한 버튼만 선택
        clickedButton.SetSelected(true);
        currentCategory = clickedButton.categoryName;

        // 선택 초기화 후 다시 그리기
        ClearSelection();
        RedrawInventory();
    }

    // 2. 슬롯 클릭 시 호출
    public void SelectSlot(ItemSlot slot)
    {
        // 이미 선택된 게 있다면 해제
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }

        if (slot.item != null)
        {
            selectedItem = slot.item;
            selectedSlot = slot;
            selectedSlot.SetSelected(true);

            // 상세 패널 업데이트
            UpdateDetailPanel(slot.item);
        }
        else
        {
            ClearSelection();
        }
    }

    // 3. 상세 정보 패널 업데이트 (가격 대신 수량 표시)
    private void UpdateDetailPanel(ItemData item)
    {
        if (item != null)
        {
            detailPanelObject.SetActive(true);
            detailImage.sprite = item.itemIcon;
            detailImage.color = Color.white;

            // 이름 표시
            detailNameText.text = item.itemName;

            // [핵심] 인벤토리 매니저에서 현재 이 아이템을 몇 개 가지고 있는지 확인
            int count = 0;
            if (InventoryManager.Instance.items.ContainsKey(item))
            {
                count = InventoryManager.Instance.items[item];
            }

            // 가격 대신 수량 표시
            if (detailQuantityText != null)
            {
                detailQuantityText.text = count.ToString();
            }
        }
    }

    // 인벤토리 다시 그리기 함수
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
                    slots[i].SetSlot(itemPair.Key, itemPair.Value);

                    if (selectedSlot == slots[i])
                    {
                        selectedSlot.SetSelected(true);

                        UpdateDetailPanel(slots[i].item);
                    }
                    i++;
                }
            }
        }


        for (int j = i; j < slots.Count; j++)
        {
            slots[j].ClearSlot();
        }

        if (i == 0) ClearSelection();
    }

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
}