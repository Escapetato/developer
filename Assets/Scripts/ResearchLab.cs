using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CategoryTab
{
    public string categoryName;
    public Button buttonObj;
    public Image buttonBackground;
    public TextMeshProUGUI buttonText;
}

public class ResearchLab : MonoBehaviour
{
    public static ResearchLab Instance { get; private set; }

    [Header("1. Settings")]
    public int defaultFailureCost = 100;

    [Header("2. Mixer Slots")]
    public ItemSlot materialSlot;
    public ItemSlot potionSlot;

    [Header("3. Inventory Grid")]
    public Transform slotParent;
    private List<ItemSlot> inventorySlots;

    [Header("4. Confirm Popup")]
    public GameObject confirmPopupObject;
    public TextMeshProUGUI confirmPopupText;

    [Header("5. Success Popup (Result)")]
    public GameObject successPopupObject;
    public Image successItemIcon;
    public TextMeshProUGUI successNameText;
    public TextMeshProUGUI successMessageText;

    // ▼▼▼ [NEW] 성공 팝업 버튼 2개 추가 ▼▼▼
    public Button successConfirmButton;    // [확인] 버튼
    public Button successCollectionButton; // [도감에서 보기] 버튼

    [Header("6. Category Tabs UI")]
    public List<CategoryTab> categoryTabs;
    public Sprite tabSelectedSprite;
    public Sprite tabNormalSprite;
    public Color tabSelectedColor = Color.white;
    public Color tabNormalColor = Color.gray;

    [Header("7. Effect Popup")]
    public GameObject loadingPopupObject;
    public float evolutionDelay = 2.0f;

    private EvolutionRecipe pendingRecipe;
    private int currentEvolutionCost = 0;
    private string currentCategory = "Crop";
    public ItemData selectedItem { get; private set; }
    public ItemSlot selectedSlot { get; private set; }

    // [NEW] 도감으로 바로가기 위해 방금 만든 아이템 저장용
    private ItemData currentResultItem;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        inventorySlots = new List<ItemSlot>();
        if (slotParent != null)
            slotParent.GetComponentsInChildren<ItemSlot>(inventorySlots);

        if (confirmPopupObject != null) confirmPopupObject.SetActive(false);
        if (successPopupObject != null) successPopupObject.SetActive(false);
        if (loadingPopupObject != null) loadingPopupObject.SetActive(false);

        // ▼▼▼ [NEW] 성공 팝업 버튼 리스너 연결 ▼▼▼
        if (successConfirmButton != null)
        {
            successConfirmButton.onClick.RemoveAllListeners();
            successConfirmButton.onClick.AddListener(OnCloseSuccessPopup);
        }
        if (successCollectionButton != null)
        {
            successCollectionButton.onClick.RemoveAllListeners();
            successCollectionButton.onClick.AddListener(OnGoToCollectionClick);
        }
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

    // ... (SelectSlot, ClearSelection, SetCategory, RedrawInventory, UpdateTabUI는 기존 코드 유지) ...
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
        // ... (기존과 동일) ...
        if (materialSlot.item == null || potionSlot.item == null)
        {
            UIManager.Instance.ShowAlertPopup("재료가 부족합니다!");
            return;
        }

        ItemData input1 = materialSlot.item;
        ItemData input2 = potionSlot.item;
        EvolutionRecipe foundRecipe = null;

        if (DBManager.Instance != null)
        {
            foreach (var recipe in DBManager.Instance.allGameRecipes)
            {
                bool matchDirect = (recipe.material == input1 && recipe.potion == input2);
                bool matchReverse = (recipe.material == input2 && recipe.potion == input1);

                if (matchDirect || matchReverse)
                {
                    foundRecipe = recipe;
                    break;
                }
            }
        }

        pendingRecipe = foundRecipe;
        currentEvolutionCost = (foundRecipe != null) ? foundRecipe.evolutionCost : defaultFailureCost;

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

        StartCoroutine(ProcessEvolutionRoutine());
    }

    IEnumerator ProcessEvolutionRoutine()
    {
        if (loadingPopupObject != null) loadingPopupObject.SetActive(true);
        // SoundManager.Instance.PlaySFX("ev_processing"); 

        yield return new WaitForSeconds(evolutionDelay);

        if (loadingPopupObject != null) loadingPopupObject.SetActive(false);
        ShowEvolutionResult();
    }

    private void ShowEvolutionResult()
    {
        if (pendingRecipe != null)
        {
            // 성공
            ItemData newItem = pendingRecipe.resultItem;
            currentResultItem = newItem; // [NEW] 도감 이동을 위해 저장

            GameProgressionManager.Instance.UnlockItem(newItem);
            GameProgressionManager.Instance.UnlockRecipe(pendingRecipe);

            InventoryManager.Instance.AddItem(newItem, 1);
            SoundManager.Instance.PlaySFX("ev_success");

            OpenSuccessPopup(newItem); // 팝업 띄우기

            QuestManager.Instance?.NotifyEvolutionResult(true, newItem);
        }
        else
        {
            // 실패
            currentResultItem = null;
            UIManager.Instance.ShowAlertPopup("아무런 반응이 없습니다...\n 재료가 모두 사라졌습니다.");
            SoundManager.Instance.PlaySFX("ev_fail");
            QuestManager.Instance?.NotifyEvolutionResult(false, null);
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

    // ▼▼▼ [NEW] 도감 버튼 클릭 시 실행될 함수 ▼▼▼
    public void OnGoToCollectionClick()
    {
        // 1. 팝업 닫기
        OnCloseSuccessPopup();

        // 2. 도감 열기 (UIManager에 요청)
        // 만약 UIManager에 OpenCollection 함수가 없다면 아래 2번 항목 참고해서 추가하세요.
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenCollectionPanel(currentResultItem);
        }
        else
        {
            Debug.LogError("UIManager가 없습니다.");
        }
    }
}