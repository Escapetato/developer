using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResearchLab : MonoBehaviour
{
    public static ResearchLab Instance { get; private set; }

    [Header("1. Evolution Data")]
    public List<EvolutionRecipe> allRecipes;

    [Header("2. Mixer Slots (왼쪽 혼합기)")]
    public ItemSlot materialSlot;
    public ItemSlot potionSlot;

    [Header("3. Inventory Grid (오른쪽 그리드)")]
    public Transform slotParent;
    private List<ItemSlot> inventorySlots;

    private string currentCategory = "Crop";

    public ItemData selectedItem { get; private set; }
    public ItemSlot selectedSlot { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        inventorySlots = new List<ItemSlot>();
        if (slotParent != null)
        {
            slotParent.GetComponentsInChildren<ItemSlot>(inventorySlots);
        }
    }

    void OnEnable()
    {
        InventoryManager.Instance.OnInventoryChanged += RedrawInventory;
        SetCategory("Crop");
    }

    void OnDisable()
    {
        InventoryManager.Instance.OnInventoryChanged -= RedrawInventory;
    }

    public void SelectSlot(ItemSlot slot)
    {
        if (selectedSlot != null) selectedSlot.SetSelected(false);
        if (slot.item != null)
        {
            selectedItem = slot.item;
            selectedSlot = slot;
            selectedSlot.SetSelected(true);
        }
    }

    public void ClearSelection()
    {
        if (selectedSlot != null) selectedSlot.SetSelected(false);
        selectedItem = null;
        selectedSlot = null;
    }

    public void SetCategory(string category)
    {
        currentCategory = category;
        ClearSelection();
        RedrawInventory();
    }

    private void RedrawInventory()
    {
        Dictionary<ItemData, int> allItems = InventoryManager.Instance.items;
        int i = 0;

        foreach (KeyValuePair<ItemData, int> itemPair in allItems)
        {
            if (currentCategory == "All" || itemPair.Key.itemCategory == currentCategory)
            {
                if (i < inventorySlots.Count)
                {
                    inventorySlots[i].gameObject.SetActive(true);
                    inventorySlots[i].SetSlot(itemPair.Key, itemPair.Value);
                    if (selectedSlot == inventorySlots[i]) selectedSlot.SetSelected(true);
                    i++;
                }
            }
        }

        for (int j = i; j < inventorySlots.Count; j++)
        {
            inventorySlots[j].ClearSlot();
            inventorySlots[j].gameObject.SetActive(false);
        }
    }

    public void OnEvolutionButtonClick()
    {
        // 1. 재료가 비었는지 확인
        if (materialSlot.item == null || potionSlot.item == null)
        {
            UIManager.Instance.ShowAlertPopup("재료가 부족합니다!");
            return;
        }

        ItemData inputMaterial = materialSlot.item;
        ItemData inputPotion = potionSlot.item;

        // 2. 내가 넣은 재료랑 딱 맞는 레시피가 있는지 '전체 리스트'에서 검색
        EvolutionRecipe foundRecipe = null;

        foreach (var recipe in allRecipes)
        {
            // 재료와 물약이 둘 다 일치하는 레시피 찾기
            if (recipe.material == inputMaterial && recipe.potion == inputPotion)
            {
                foundRecipe = recipe;
                break; // 찾았으면 반복문 종료
            }
        }

        // 3. 결과 판정

        // [CASE A] 맞는 레시피가 없음 -> 실패 (재료만 날림)
        if (foundRecipe == null)
        {
            // 재료 삭제
            InventoryManager.Instance.RemoveItem(inputMaterial, 1);
            InventoryManager.Instance.RemoveItem(inputPotion, 1);

            // 슬롯 비우기
            materialSlot.ClearSlot();
            potionSlot.ClearSlot();

            UIManager.Instance.ShowAlertPopup("아무런 반응이 없습니다...\n(재료가 소멸되었습니다)");
            return;
        }

        // [CASE B] 맞는 레시피 찾음 -> 성공 조건(돈) 체크
        if (!PoingManager.Instance.HasEnoughPoing(foundRecipe.evolutionCost))
        {
            // 레시피는 맞는데 돈이 없으면? 재료 날리지 말고 경고만
            UIManager.Instance.ShowAlertPopup("포잉이 부족합니다!");
            return;
        }

        // 4. 진짜 성공 처리

        // 비용 지불 및 재료 삭제
        PoingManager.Instance.DecreasePoing(foundRecipe.evolutionCost);
        InventoryManager.Instance.RemoveItem(inputMaterial, 1);
        InventoryManager.Instance.RemoveItem(inputPotion, 1);

        // 슬롯 비우기
        materialSlot.ClearSlot();
        potionSlot.ClearSlot();

        // 결과물 지급 및 해금
        ItemData newItem = foundRecipe.resultItem;

        GameProgressionManager.Instance.UnlockItem(newItem); // 도감 해금
        InventoryManager.Instance.AddItem(newItem, 1);       // 인벤토리 지급
        UIManager.Instance.ShowItemAcquiredPopup(newItem);   // 축하 팝업
    }
}