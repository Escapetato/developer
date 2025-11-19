using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResearchLab : MonoBehaviour
{
    public static ResearchLab Instance { get; private set; }

    [Header("1. Evolution Recipe")]
    public EvolutionRecipe currentRecipe;

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

        // ★ [수정] 연구실 열릴 때 'Crop'(작물)부터 보여주기
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
        if (materialSlot.item == null || potionSlot.item == null)
        {
            UIManager.Instance.ShowAlertPopup("재료가 부족합니다!");
            return;
        }

        if (!PoingManager.Instance.HasEnoughPoing(currentRecipe.evolutionCost))
        {
            UIManager.Instance.ShowAlertPopup("포잉이 부족합니다!");
            return;
        }

        bool isRecipeCorrect = (materialSlot.item == currentRecipe.material) &&
                               (potionSlot.item == currentRecipe.potion);

        materialSlot.ClearSlot();
        potionSlot.ClearSlot();
        PoingManager.Instance.DecreasePoing(currentRecipe.evolutionCost);

        if (isRecipeCorrect)
        {
            ItemData newItem = currentRecipe.resultItem;
            UIManager.Instance.ShowItemAcquiredPopup(newItem);
        }
        else
        {
            UIManager.Instance.ShowAlertPopup("진화 실패... (재료/포잉 모두 소멸됨)");
        }
    }
}