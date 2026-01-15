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
    public Button prevButton;
    public Button nextButton;

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
        if (categoryButtons != null && categoryButtons.Count > 0)
        {
            SetCategoryButton(categoryButtons[0]);
        }
        else
        {
            SetCategory("Vegetable");
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
    }

    public void OnPrevBtnClick()
    {
        if (currentCategoryList == null || currentCategoryList.Count == 0) return;
        currentIndex--;
        if (currentIndex < 0) currentIndex = currentCategoryList.Count - 1;
        UpdateLeftPage();
    }

    public void OnNextBtnClick()
    {
        if (currentCategoryList == null || currentCategoryList.Count == 0) return;
        currentIndex++;
        if (currentIndex >= currentCategoryList.Count) currentIndex = 0;
        UpdateLeftPage();
    }

    // ▼▼▼ [핵심] 외부에서 특정 아이템을 보여달라고 할 때 쓰는 함수 ▼▼▼
    public void ShowItem(ItemData itemToShow)
    {
        if (itemToShow == null) return;

        // 1. 이 아이템이 진화 결과물인지 확인하고, 베이스 작물(어미) 찾기
        EvolutionRecipe targetRecipe = allRecipes.Find(r => r.resultItem == itemToShow);
        ItemData baseItem = null;

        if (targetRecipe != null)
        {
            baseItem = targetRecipe.material; // 진화 재료(베이스 작물)를 찾음
        }
        else
        {
            baseItem = itemToShow; // 진화 결과물이 아니면 그 자체가 베이스라고 가정
        }

        // 2. 해당 카테고리 탭으로 이동 (버튼 색상도 같이 갱신)
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

        // 3. 리스트에서 해당 작물 페이지 찾기
        int targetIndex = currentCategoryList.FindIndex(x => x == baseItem);
        if (targetIndex != -1)
        {
            currentIndex = targetIndex;
            UpdateLeftPage(); // 왼쪽 페이지 갱신
        }

        // 4. 진화 결과물이라면 오른쪽 상세페이지도 즉시 보여주기
        if (targetRecipe != null)
        {
            ShowRightPage(targetRecipe);
        }
    }
    // ▲▲▲ 추가 완료 ▲▲▲

    private void UpdateLeftPage()
    {
        if (currentCategoryList == null || currentCategoryList.Count == 0) return;

        if (rightPageGroup != null) rightPageGroup.SetActive(false);
        if (evoSlot1 != null) evoSlot1.SetSelected(false);
        if (evoSlot2 != null) evoSlot2.SetSelected(false);

        ItemData currentBaseItem = currentCategoryList[currentIndex];
        bool isBaseUnlocked = GameProgressionManager.Instance.IsItemUnlocked(currentBaseItem);

        // 왼쪽 페이지 표시
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

        // 하단 진화 슬롯 설정
        List<EvolutionRecipe> myRecipes = new List<EvolutionRecipe>();
        foreach (var recipe in allRecipes)
        {
            if (recipe.material == currentBaseItem)
            {
                myRecipes.Add(recipe);
            }
        }

        // 슬롯 1
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

        // 슬롯 2
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