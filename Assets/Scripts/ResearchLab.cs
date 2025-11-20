using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // ★ [필수] 이게 있어야 텍스트 에러가 안 납니다!

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

    [Header("4. Confirm Popup (결제 확인창)")]
    public GameObject confirmPopupObject;     // "300포잉 결제?" 패널
    public TextMeshProUGUI confirmPopupText;  // "300포잉..." 텍스트

    // ★ [추가] 5. Success Popup (성공 결과창)
    [Header("5. Success Popup (진화 성공창)")]
    public GameObject successPopupObject;   // 성공 팝업 패널 전체
    public Image successItemIcon;           // 결과 아이템 아이콘 보여줄 곳
    public TextMeshProUGUI successNameText; // "황금사과" 이름 보여줄 곳
    public TextMeshProUGUI successMessageText; // "진화에 성공했습니다!" 텍스트

    private EvolutionRecipe pendingRecipe; // 결제 대기 중인 레시피
    private string currentCategory = "Crop";

    // 선택된 아이템 정보
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

        // 시작할 때 팝업들 다 끄기
        if (confirmPopupObject != null) confirmPopupObject.SetActive(false);
        if (successPopupObject != null) successPopupObject.SetActive(false);
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

    // --- (슬롯 선택 및 인벤토리 그리기 로직: 기존과 동일) ---
    public void SelectSlot(ItemSlot slot) { if (selectedSlot != null) selectedSlot.SetSelected(false); if (slot.item != null) { selectedItem = slot.item; selectedSlot = slot; selectedSlot.SetSelected(true); } }
    public void ClearSelection() { if (selectedSlot != null) selectedSlot.SetSelected(false); selectedItem = null; selectedSlot = null; }
    public void SetCategory(string category) { currentCategory = category; ClearSelection(); RedrawInventory(); }
    private void RedrawInventory() { Dictionary<ItemData, int> allItems = InventoryManager.Instance.items; int i = 0; foreach (KeyValuePair<ItemData, int> itemPair in allItems) { if (currentCategory == "All" || itemPair.Key.itemCategory == currentCategory) { if (i < inventorySlots.Count) { inventorySlots[i].gameObject.SetActive(true); inventorySlots[i].SetSlot(itemPair.Key, itemPair.Value); if (selectedSlot == inventorySlots[i]) selectedSlot.SetSelected(true); i++; } } } for (int j = i; j < inventorySlots.Count; j++) { inventorySlots[j].ClearSlot(); inventorySlots[j].gameObject.SetActive(false); } }
    // ---------------------------------------------------------


    // [1] 진화 버튼 클릭 (재료 검사 -> 레시피 확인 -> 결제 팝업 띄우기)
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

        foreach (var recipe in allRecipes)
        {
            if (recipe.material == inputMaterial && recipe.potion == inputPotion)
            {
                foundRecipe = recipe;
                break;
            }
        }

        // [실패] 맞는 레시피 없음 -> 재료 소멸
        if (foundRecipe == null)
        {
            InventoryManager.Instance.RemoveItem(inputMaterial, 1);
            InventoryManager.Instance.RemoveItem(inputPotion, 1);
            materialSlot.ClearSlot();
            potionSlot.ClearSlot();

            UIManager.Instance.ShowAlertPopup("아무런 반응이 없습니다... \n진화에 실패하였습니다.");
            return;
        }

        // [성공 대기] 결제 팝업 띄우기
        pendingRecipe = foundRecipe;

        if (confirmPopupText != null)
            confirmPopupText.text = $"{foundRecipe.evolutionCost} 포잉으로 진화하시겠습니까?";

        if (confirmPopupObject != null) confirmPopupObject.SetActive(true);
        else OnConfirmEvolution(); // 팝업 없으면 바로 진행
    }

    // [2] 결제 팝업에서 '네' 클릭
    public void OnConfirmEvolution()
    {
        if (confirmPopupObject != null) confirmPopupObject.SetActive(false);
        if (pendingRecipe == null) return;

        // 돈 검사
        if (!PoingManager.Instance.HasEnoughPoing(pendingRecipe.evolutionCost))
        {
            UIManager.Instance.ShowAlertPopup("포잉이 부족합니다!");
            return;
        }

        // === 진짜 성공 처리 시작 ===

        // 1. 비용 및 재료 차감
        PoingManager.Instance.DecreasePoing(pendingRecipe.evolutionCost);
        if (materialSlot.item != null) InventoryManager.Instance.RemoveItem(materialSlot.item, 1);
        if (potionSlot.item != null) InventoryManager.Instance.RemoveItem(potionSlot.item, 1);

        materialSlot.ClearSlot();
        potionSlot.ClearSlot();

        // 2. 결과 아이템 지급 및 해금
        ItemData newItem = pendingRecipe.resultItem;
        GameProgressionManager.Instance.UnlockItem(newItem);
        InventoryManager.Instance.AddItem(newItem, 1);

        // 3. ★ [변경] 내 전용 '성공 팝업' 띄우기 (UIManager 안 씀!)
        OpenSuccessPopup(newItem);

        pendingRecipe = null;
    }

    // [3] 결제 팝업에서 '아니오' 클릭
    public void OnCancelEvolution()
    {
        if (confirmPopupObject != null) confirmPopupObject.SetActive(false);
        pendingRecipe = null;
    }

    // ★ [추가] 성공 팝업 띄우는 함수
    private void OpenSuccessPopup(ItemData item)
    {
        if (successPopupObject != null)
        {
            successPopupObject.SetActive(true);

            // 아이콘과 이름 설정
            if (successItemIcon != null) successItemIcon.sprite = item.itemIcon;
            if (successNameText != null) successNameText.text = item.itemName; // "황금사과"

            // 만약 "진화에 성공했습니다!" 텍스트도 코드로 바꾸고 싶다면 여기에 추가
            if (successMessageText != null) successMessageText.text = "진화에 성공했습니다!";
        }
    }

    // ★ [추가] 성공 팝업 닫기 버튼에 연결할 함수
    public void OnCloseSuccessPopup()
    {
        if (successPopupObject != null)
        {
            successPopupObject.SetActive(false);
        }
    }
}