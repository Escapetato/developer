using System.Collections; // [필수] 코루틴 사용을 위해 추가
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

    // ▼▼▼ [NEW] 로딩(연출) 팝업 연결 변수 추가 ▼▼▼
    [Header("7. Effect Popup")]
    public GameObject loadingPopupObject; // "수상한 일이 벌어지고 있습니다..." 팝업
    public float evolutionDelay = 2.0f;   // 대기 시간 (2초)

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

        // [NEW] 시작할 때 로딩 팝업 끄기
        if (loadingPopupObject != null) loadingPopupObject.SetActive(false);
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

    // ... (SelectSlot, ClearSelection, SetCategory, RedrawInventory, UpdateTabUI는 기존과 동일) ...
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

        if (foundRecipe != null)
            currentEvolutionCost = foundRecipe.evolutionCost;
        else
            currentEvolutionCost = defaultFailureCost;

        if (confirmPopupText != null)
            confirmPopupText.text = $"{currentEvolutionCost} 포잉으로 진화하시겠습니까?";

        if (confirmPopupObject != null) confirmPopupObject.SetActive(true);
        else OnConfirmEvolution();
    }

    // ▼▼▼ [MODIFIED] 확인 버튼 클릭 시 바로 결과가 아니라 '연출' 시작 ▼▼▼
    public void OnConfirmEvolution()
    {
        if (confirmPopupObject != null) confirmPopupObject.SetActive(false);

        // 1. 돈 확인
        if (!PoingManager.Instance.HasEnoughPoing(currentEvolutionCost))
        {
            UIManager.Instance.ShowAlertPopup("포잉이 부족합니다!");
            return;
        }

        // 2. 돈과 재료 먼저 차감 (연출 중에 템 바꾸는 꼼수 방지)
        PoingManager.Instance.DecreasePoing(currentEvolutionCost);

        if (materialSlot.item != null) InventoryManager.Instance.RemoveItem(materialSlot.item, 1);
        if (potionSlot.item != null) InventoryManager.Instance.RemoveItem(potionSlot.item, 1);

        materialSlot.ClearSlot();
        potionSlot.ClearSlot();

        // 3. 코루틴 시작 (대기 후 결과 표시)
        StartCoroutine(ProcessEvolutionRoutine());
    }

    // ▼▼▼ [NEW] 로딩 연출 코루틴 ▼▼▼
    IEnumerator ProcessEvolutionRoutine()
    {
        // 1. 로딩 팝업 띄우기
        if (loadingPopupObject != null)
        {
            loadingPopupObject.SetActive(true);

            // 혹시 팝업 안에 텍스트가 있다면 여기서 설정 가능
            // ex) loadingText.text = "수상한 일이 벌어지고 있습니다..."; 
        }

        // [소리] 뭔가 끓는 소리나 신비한 소리 넣으면 좋음
        // SoundManager.Instance.PlaySFX("ev_processing"); 

        // 2. 대기 (2초)
        yield return new WaitForSeconds(evolutionDelay);

        // 3. 로딩 팝업 끄기
        if (loadingPopupObject != null) loadingPopupObject.SetActive(false);

        // 4. 결과 처리 (기존 로직 이동)
        ShowEvolutionResult();
    }

    // ▼▼▼ [NEW] 실제 결과 처리 로직 (분리됨) ▼▼▼
    private void ShowEvolutionResult()
    {
        if (pendingRecipe != null)
        {
            // 성공
            ItemData newItem = pendingRecipe.resultItem;
            GameProgressionManager.Instance.UnlockItem(newItem);
            GameProgressionManager.Instance.UnlockRecipe(pendingRecipe);

            InventoryManager.Instance.AddItem(newItem, 1);
            SoundManager.Instance.PlaySFX("ev_success");
            OpenSuccessPopup(newItem);

            QuestManager.Instance?.NotifyEvolutionResult(true, newItem);
        }
        else
        {
            // 실패
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
}