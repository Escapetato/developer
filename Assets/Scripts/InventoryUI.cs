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
    public Text detailNameText;        // Detail_Name_Text

    private string currentCategory = "All";

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        slots = new List<ItemSlot>();
        slotParent.GetComponentsInChildren<ItemSlot>(slots);

        // 시작할 때 상세 정보 패널을 숨김
        if (detailPanelObject != null)
            detailPanelObject.SetActive(false);
    }

    void OnEnable()
    {
        InventoryManager.Instance.OnInventoryChanged += RedrawInventory;
        SetCategory("All");
    }

    void OnDisable()
    {
        InventoryManager.Instance.OnInventoryChanged -= RedrawInventory;
    }

    public void SetCategory(string category)
    {
        currentCategory = category;
        ClearSelection(); // 카테고리 바꾸면 선택 해제
        RedrawInventory(); // 인벤토리 다시 그리기
    }

    // 슬롯 선택 함수 (가장 중요)
    public void SelectSlot(ItemSlot slot)
    {
        // 이전에 선택한 슬롯이 있다면 선택 해제
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }

        // 새로 선택 (단, 아이템이 있는 슬롯만)
        if (slot.item != null)
        {
            // (연구실용) 아이템 선택
            selectedItem = slot.item;
            selectedSlot = slot;
            selectedSlot.SetSelected(true);

            // 상세 정보 패널 업데이트
            UpdateDetailPanel(slot.item);
        }
    }

    // 6. [!!! 수정 !!!] 선택 해제 함수
    public void ClearSelection()
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }
        selectedItem = null;
        selectedSlot = null;

        // 7. [추가] 선택 해제 시 상세 정보 패널도 숨김
        if (detailPanelObject != null)
            detailPanelObject.SetActive(false);
    }

    // 8. [추가] 상세 정보 패널 업데이트 전용 함수
    private void UpdateDetailPanel(ItemData item)
    {
        if (item != null)
        {
            detailPanelObject.SetActive(true); // 1. 패널을 켠다

            detailImage.sprite = item.itemIcon; // 2. 큰 이미지 교체
            detailImage.color = Color.white; // (투명도 복구)

            detailNameText.text = item.itemName; // 3. 텍스트 교체
        }
    }

    // 인벤토리 다시 그리기 함수
    private void RedrawInventory()
    {
        // 4a. 인벤토리 매니저에서 '전체' 아이템 목록을 가져옴
        Dictionary<ItemData, int> allItems = InventoryManager.Instance.items;
        int i = 0; // UI 슬롯 인덱스

        // 4b. '전체' 아이템 목록을 하나씩 검사
        foreach (KeyValuePair<ItemData, int> itemPair in allItems)
        {
            // 4c. [필터링]
            // "All" 카테고리를 선택했거나, 
            // 아이템의 카테고리가 'currentCategory'와 일치할 때만 그림
            if (currentCategory == "All" || itemPair.Key.itemCategory == currentCategory)
            {
                if (i < slots.Count)
                {
                    // UI 슬롯에 아이템 정보와 수량을 전달
                    slots[i].SetItem(itemPair.Key, itemPair.Value);

                    if (selectedSlot == slots[i])
                    {
                        selectedSlot.SetSelected(true);
                    }
                    i++; // 그린 아이템 수 증가
                }
            }
        }

        // 4d. 남은 슬롯들은 모두 비움
        for (int j = i; j < slots.Count; j++)
        {
            slots[j].ClearSlot();
        }

        // 4e. 이 카테고리에 아이템이 없으면 상세 정보도 끈다
        if (i == 0)
        {
            ClearSelection();
        }
    }
}