using System.Collections.Generic; // List, Dictionary
using UnityEngine;
using UnityEngine.UI; // Text

// ResearchLab.cs
public class ResearchLab : MonoBehaviour
{
    // 1. [추가] 싱글톤 설정 (ItemSlot이 접근해야 함)
    public static ResearchLab Instance { get; private set; }

    [Header("1. Evolution Recipe")]
    public EvolutionRecipe currentRecipe;

    [Header("2. Mixer Slots (왼쪽 혼합기)")]
    public ItemSlot materialSlot; // (Inspector에서 '혼합기'의 첫 번째 슬롯 연결)
    public ItemSlot potionSlot;   // (Inspector에서 '혼합기'의 두 번째 슬롯 연결)

    [Header("3. Inventory Grid (오른쪽 그리드)")]
    public Transform slotParent;  // (Inspector에서 9칸 그리드의 부모인 'Grid_Panel' 연결)
    private List<ItemSlot> inventorySlots;
    private string currentCategory = "All"; // 현재 선택된 카테고리

    // (InventoryUI에서 가져온 로직)
    public ItemData selectedItem { get; private set; }
    public ItemSlot selectedSlot { get; private set; }

    void Awake()
    {
        // 1-1. 싱글톤
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 3-1. 오른쪽 그리드의 9개 슬롯을 찾아 리스트에 담음
        inventorySlots = new List<ItemSlot>();
        if (slotParent != null)
        {
            slotParent.GetComponentsInChildren<ItemSlot>(inventorySlots);
        }
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

    // 4-1. [추가] 슬롯 선택 함수 (InventoryUI에서 복사)
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
        }
    }

    // 4-2. [추가] 선택 해제 함수 (InventoryUI에서 복사)
    public void ClearSelection()
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }
        selectedItem = null;
        selectedSlot = null;
    }

    // 3-5. [추가] 카테고리 버튼들이 호출할 함수
    public void SetCategory(string category)
    {
        currentCategory = category;
        ClearSelection(); // 카테고리 바꾸면 선택 해제
        RedrawInventory(); // 인벤토리 다시 그리기
    }

    // 3-6. [추가] 인벤토리 다시 그리기 (InventoryUI에서 복사)
    private void RedrawInventory()
    {
        Dictionary<ItemData, int> allItems = InventoryManager.Instance.items;
        int i = 0; // UI 슬롯 인덱스

        foreach (KeyValuePair<ItemData, int> itemPair in allItems)
        {
            // [필터링]
            if (currentCategory == "All" || itemPair.Key.itemCategory == currentCategory)
            {
                if (i < inventorySlots.Count)
                {
                    inventorySlots[i].SetItem(itemPair.Key, itemPair.Value);
                    if (selectedSlot == inventorySlots[i])
                    {
                        selectedSlot.SetSelected(true);
                    }
                    i++;
                }
            }
        }
        for (int j = i; j < inventorySlots.Count; j++)
        {
            inventorySlots[j].ClearSlot();
        }
    }

    // 5. '진화' 버튼이 호출할 함수 (이제 왼쪽 혼합기 슬롯을 참조)
    public void OnEvolutionButtonClick()
    {
        // 5-1. 왼쪽 '혼합기' 슬롯에 재료가 다 찼는지 확인
        if (materialSlot.item == null || potionSlot.item == null)
        {
            UIManager.Instance.ShowAlertPopup("재료가 부족합니다!");
            return;
        }

        // 5-2. 포잉(Poing) 확인
        if (!PoingManager.Instance.HasEnoughPoing(currentRecipe.evolutionCost))
        {
            UIManager.Instance.ShowAlertPopup("포잉이 부족합니다!");
            return;
        }

        // 5-3. 레시피 일치 검사
        bool isRecipeCorrect = (materialSlot.item == currentRecipe.material) &&
                               (potionSlot.item == currentRecipe.potion);

        // 5-4. 재료 및 포잉 소멸
        materialSlot.ClearSlot();
        potionSlot.ClearSlot();
        PoingManager.Instance.DecreasePoing(currentRecipe.evolutionCost);

        // 결과 처리
        if (isRecipeCorrect)
        {
            // [성공]
            ItemData newItem = currentRecipe.resultItem;

            // 1. [삭제] 인벤토리에 바로 추가하는 로직 삭제
            // InventoryManager.Instance.AddItem(newItem, 1); 

            // 2. [삭제] 연구실 결과 슬롯에 보여주는 로직 삭제
            // (새 팝업이 보여줄 것이므로)
            // resultSlot.SetItem(newItem, 1); 

            UIManager.Instance.ShowItemAcquiredPopup(newItem);
        }
        else
        {
            // [실패]
            // resultSlot.ClearSlot(); // (결과 슬롯이 없다면 이 줄도 삭제)
            UIManager.Instance.ShowAlertPopup("진화 실패... (재료/포잉 모두 소멸됨)");
        }
    }
}