using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CollectionUI : MonoBehaviour
{
    public static CollectionUI Instance { get; private set; }

    [Header("--- Data Sources ---")]
    public List<ItemData> allBaseCrops;
    public List<EvolutionRecipe> allRecipes;

    [Header("--- Category Buttons (탭) ---")]
    public List<CategoryButton> categoryButtons;

    [Header("--- Assets ---")]
    public Sprite lockedSprite;

    [Header("--- Left Page UI ---")]
    public Image baseCropImage;
    public TextMeshProUGUI baseCropName;
    public TextMeshProUGUI growTimeText;
    public TextMeshProUGUI toolText;

    public CollectionEvoSlot evoSlot1;
    public CollectionEvoSlot evoSlot2;

    [Header("--- Right Page UI ---")]
    public GameObject rightPageGroup;
    public Image resultCropImage;
    public TextMeshProUGUI resultName;
    public TextMeshProUGUI potionUsedText;
    public Image potionUsedImage;
    public TextMeshProUGUI descriptionText;

    [Header("--- Navigation ---")]
    public Button prevButton; // 이전 버튼 (첫 장이면 숨김)
    public Button nextButton; // 다음 버튼 (마지막 장이면 숨김)

    private List<ItemData> currentCategoryList;
    private int currentIndex = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (rightPageGroup != null) rightPageGroup.SetActive(false);
    }

    void OnEnable()
    {
        string defaultCategory = "Fruit"; // ★ 여기에 원하는 카테고리 이름(ItemData와 동일해야 함)을 적으세요!

        CategoryButton targetBtn = null;

        // 1. 버튼 목록에서 해당 카테고리 찾기
        if (categoryButtons != null)
        {
            targetBtn = categoryButtons.Find(btn => btn.categoryName == defaultCategory);
        }

        // 2. 찾았으면 그 버튼 선택, 못 찾았으면 목록의 첫 번째 선택
        if (targetBtn != null)
        {
            SetCategoryButton(targetBtn);
        }
        else if (categoryButtons != null && categoryButtons.Count > 0)
        {
            SetCategoryButton(categoryButtons[0]); // fallback
        }
        else
        {
            SetCategory(defaultCategory); // 버튼이 아예 없을 때
        }
    }

    // 탭 버튼 클릭 시 호출
    public void SetCategoryButton(CategoryButton clickedButton)
    {
        foreach (CategoryButton btn in categoryButtons)
        {
            btn.SetSelected(false);
        }
        clickedButton.SetSelected(true);
        SetCategory(clickedButton.categoryName);
    }

    public void SetCategory(string category)
    {
        currentCategoryList = new List<ItemData>();

        foreach (var item in allBaseCrops)
        {
            if (item.collectionCategory == category)
            {
                currentCategoryList.Add(item);
            }
        }

        currentIndex = 0;
        UpdateLeftPage();

        // 카테고리 바꿀 때 버튼 상태 갱신
        UpdateNavigationButtons();
    }

    public void OnPrevBtnClick()
    {
        if (currentCategoryList == null || currentCategoryList.Count == 0) return;

        // 0보다 클 때만 감소
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateLeftPage();
            UpdateNavigationButtons(); // 버튼 상태 갱신
        }
    }

    public void OnNextBtnClick()
    {
        if (currentCategoryList == null || currentCategoryList.Count == 0) return;

        // 끝보다 작을 때만 증가
        if (currentIndex < currentCategoryList.Count - 1)
        {
            currentIndex++;
            UpdateLeftPage();
            UpdateNavigationButtons(); // 버튼 상태 갱신
        }
    }

    // ▼▼▼ [수정] 버튼 숨김 처리 함수 ▼▼▼
    private void UpdateNavigationButtons()
    {
        int totalCount = (currentCategoryList != null) ? currentCategoryList.Count : 0;

        // 1. 이전(Prev) 버튼: 첫 페이지(0)면 아예 안 보이게
        if (prevButton != null)
        {
            prevButton.gameObject.SetActive(currentIndex > 0);
        }

        // 2. 다음(Next) 버튼: 마지막 페이지면 아예 안 보이게
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(currentIndex < totalCount - 1);
        }
    }
    // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

    public void ShowItem(ItemData itemToShow)
    {
        if (itemToShow == null) return;

        EvolutionRecipe targetRecipe = allRecipes.Find(r => r.resultItem == itemToShow);
        ItemData baseItem = null;

        if (targetRecipe != null)
        {
            baseItem = targetRecipe.material;
        }
        else
        {
            baseItem = itemToShow;
        }

        if (categoryButtons != null)
        {
            foreach (var btn in categoryButtons)
            {
                if (btn.categoryName == baseItem.collectionCategory)
                {
                    SetCategoryButton(btn);
                    break;
                }
            }
        }
        else
        {
            SetCategory(baseItem.collectionCategory);
        }

        int targetIndex = currentCategoryList.FindIndex(x => x == baseItem);
        if (targetIndex != -1)
        {
            currentIndex = targetIndex;
            UpdateLeftPage();
            UpdateNavigationButtons(); // 여기서도 버튼 상태 갱신
        }

        if (targetRecipe != null)
        {
            ShowRightPage(targetRecipe);
        }
    }

    private void UpdateLeftPage()
    {
        if (currentCategoryList == null || currentCategoryList.Count == 0) return;

        if (rightPageGroup != null) rightPageGroup.SetActive(false);
        if (evoSlot1 != null) evoSlot1.SetSelected(false);
        if (evoSlot2 != null) evoSlot2.SetSelected(false);

        ItemData currentBaseItem = currentCategoryList[currentIndex];
        bool isBaseUnlocked = GameProgressionManager.Instance.IsItemUnlocked(currentBaseItem);

        if (isBaseUnlocked)
        {
            if (baseCropImage != null) baseCropImage.sprite = currentBaseItem.itemIcon;
            if (baseCropName != null) baseCropName.text = currentBaseItem.itemName;
            if (growTimeText != null) growTimeText.text = "재배 시간: " + currentBaseItem.growTimeDisplay;
            if (toolText != null) toolText.text = "수확 도구: " + currentBaseItem.harvestToolName;
        }
        else
        {
            if (baseCropImage != null && lockedSprite != null) baseCropImage.sprite = lockedSprite;
            if (baseCropName != null) baseCropName.text = "???";
            if (growTimeText != null) growTimeText.text = "재배 시간: ???";
            if (toolText != null) toolText.text = "수확 도구: ???";
        }

        List<EvolutionRecipe> myRecipes = new List<EvolutionRecipe>();
        foreach (var recipe in allRecipes)
        {
            if (recipe.material == currentBaseItem)
            {
                myRecipes.Add(recipe);
            }
        }

        if (myRecipes.Count > 0)
        {
            bool isResultUnlocked = GameProgressionManager.Instance.IsItemUnlocked(myRecipes[0].resultItem);
            bool finalShow = isBaseUnlocked && isResultUnlocked;
            evoSlot1.Setup(myRecipes[0], finalShow);
        }
        else
        {
            evoSlot1.Setup(null, false);
        }

        if (myRecipes.Count > 1)
        {
            bool isResultUnlocked = GameProgressionManager.Instance.IsItemUnlocked(myRecipes[1].resultItem);
            bool finalShow = isBaseUnlocked && isResultUnlocked;
            evoSlot2.Setup(myRecipes[1], finalShow);
        }
        else
        {
            evoSlot2.Setup(null, false);
        }
    }

    public void OnEvoSlotClicked(CollectionEvoSlot clickedSlot, EvolutionRecipe recipe)
    {
        ShowRightPage(recipe);

        if (clickedSlot == evoSlot1)
        {
            evoSlot1.SetSelected(true);
            evoSlot2.SetSelected(false);
        }
        else if (clickedSlot == evoSlot2)
        {
            evoSlot1.SetSelected(false);
            evoSlot2.SetSelected(true);
        }
    }

    public void ShowRightPage(EvolutionRecipe recipe)
    {
        if (rightPageGroup != null) rightPageGroup.SetActive(true);

        if (resultCropImage != null) resultCropImage.sprite = recipe.resultItem.itemIcon;
        if (resultName != null) resultName.text = recipe.resultItem.itemName;
        if (potionUsedText != null) potionUsedText.text = recipe.potion.itemName;
        if (potionUsedImage != null) potionUsedImage.sprite = recipe.potion.itemIcon;
        if (descriptionText != null) descriptionText.text = recipe.resultItem.itemDescription;
    }

    public void OnCloseBtnClick()
    {
        gameObject.SetActive(false);
    }
}