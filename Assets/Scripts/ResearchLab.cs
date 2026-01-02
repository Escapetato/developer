using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class CategoryTab
{
    public string categoryName;        // 예: "Crop", "Potion"
    public Button buttonObj;           // 버튼 컴포넌트
    public Image buttonBackground;     // 배경 이미지를 바꿀 타겟
    public TextMeshProUGUI buttonText; // 텍스트 색을 바꿀 타겟
}


public class ResearchLab : MonoBehaviour
{
    public static ResearchLab Instance { get; private set; }

    [Header("1. Settings")]
    public int defaultFailureCost = 100; // 실패 비용

    [Header("2. Mixer Slots")]
    public ItemSlot materialSlot;
    public ItemSlot potionSlot;

    [Header("3. Inventory Grid")]
    public Transform slotParent;
    private List<ItemSlot> inventorySlots;

    [Header("4. Confirm Popup")]
    public GameObject confirmPopupObject;
    public TextMeshProUGUI confirmPopupText;

    [Header("5. Success Popup")]
    public GameObject successPopupObject;
    public Image successItemIcon;
    public TextMeshProUGUI successNameText;
    public TextMeshProUGUI successMessageText;

    [Header("6. Category Tabs UI")]
    public List<CategoryTab> categoryTabs;
    public Sprite tabSelectedSprite;
    public Sprite tabNormalSprite;
    public Color tabSelectedColor = Color.white;
    public Color tabNormalColor = Color.gray;

    private EvolutionRecipe pendingRecipe;
    private int currentEvolutionCost = 0;
    private string currentCategory = "Crop";
    public ItemData selectedItem { get; private set; }
    public ItemSlot selectedSlot { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        inventorySlots = new List<ItemSlot>();
        if (slotParent != null)
            slotParent.GetComponentsInChildren<ItemSlot>(inventorySlots);

        if (confirmPopupObject != null) confirmPopupObject.SetActive(false);
        if (successPopupObject != null) successPopupObject.SetActive(false);
    }

    void OnEnable()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += RedrawInventory;

        SetCategory("Crop");
    }

    void OnDisable()
    {
        if (InventoryManager.Instance != null)
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
        UpdateTabUI();
    }

    private void RedrawInventory()
    {
        if (InventoryManager.Instance == null) return;

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

    private void UpdateTabUI()
    {
        foreach (var tab in categoryTabs)
        {
            bool isSelected = (tab.categoryName == currentCategory);
            if (tab.buttonBackground != null) tab.buttonBackground.sprite = isSelected ? tabSelectedSprite : tabNormalSprite;
            if (tab.buttonText != null) tab.buttonText.color = isSelected ? tabSelectedColor : tabNormalColor;
            if (tab.buttonObj != null) tab.buttonObj.interactable = !isSelected;
        }
    }

    public void OnEvolutionButtonClick()
    {
        if (materialSlot.item == null || potionSlot.item == null)
        {
            UIManager.Instance.ShowAlertPopup("재료가 부족합니다!");
            return;
        }

        ItemData inputMaterial = materialSlot.item;
        ItemData inputPotion = potionSlot.item;
        EvolutionRecipe foundRecipe = null;

        // DBManager에서 레시피 검색
        if (DBManager.Instance != null)
        {
            foreach (var recipe in DBManager.Instance.allGameRecipes)
            {
                if (recipe.material == inputMaterial && recipe.potion == inputPotion)
                {
                    foundRecipe = recipe;
                    break;
                }
            }
        }

        pendingRecipe = foundRecipe;

        if (foundRecipe != null)
            currentEvolutionCost = foundRecipe.evolutionCost;
        else
            currentEvolutionCost = defaultFailureCost;

        if (confirmPopupText != null)
            confirmPopupText.text = $"{currentEvolutionCost} 포잉으로 진화하시겠습니까?";

        if (confirmPopupObject != null) confirmPopupObject.SetActive(true);
        else OnConfirmEvolution();
    }

    public void OnConfirmEvolution()
    {
        if (confirmPopupObject != null) confirmPopupObject.SetActive(false);

        if (!PoingManager.Instance.HasEnoughPoing(currentEvolutionCost))
        {
            UIManager.Instance.ShowAlertPopup("포잉이 부족합니다!");
            return;
        }

        PoingManager.Instance.DecreasePoing(currentEvolutionCost);

        if (materialSlot.item != null) InventoryManager.Instance.RemoveItem(materialSlot.item, 1);
        if (potionSlot.item != null) InventoryManager.Instance.RemoveItem(potionSlot.item, 1);

        materialSlot.ClearSlot();
        potionSlot.ClearSlot();

        if (pendingRecipe != null)
        {
            ItemData newItem = pendingRecipe.resultItem;
            GameProgressionManager.Instance.UnlockItem(newItem);

            // 레시피 해금 저장
            GameProgressionManager.Instance.UnlockRecipe(pendingRecipe);

            InventoryManager.Instance.AddItem(newItem, 1);
            SoundManager.Instance.PlaySFX("ev_success");
            OpenSuccessPopup(newItem);
        }
        else
        {
            UIManager.Instance.ShowAlertPopup("아무런 반응이 없습니다...\n 재료가 모두 사라졌습니다.");
            SoundManager.Instance.PlaySFX("ev_fail");
        }

        pendingRecipe = null;
        currentEvolutionCost = 0;
    }

    public void OnCancelEvolution()
    {
        if (confirmPopupObject != null) confirmPopupObject.SetActive(false);
        pendingRecipe = null;
        currentEvolutionCost = 0;
    }

    private void OpenSuccessPopup(ItemData item)
    {
        if (successPopupObject != null)
        {
            successPopupObject.SetActive(true);
            if (successItemIcon != null) successItemIcon.sprite = item.itemIcon;
            if (successNameText != null) successNameText.text = item.itemName;
            if (successMessageText != null) successMessageText.text = "진화에 성공했습니다!";
        }
    }

    public void OnCloseSuccessPopup()
    {
        if (successPopupObject != null) successPopupObject.SetActive(false);
    }
}