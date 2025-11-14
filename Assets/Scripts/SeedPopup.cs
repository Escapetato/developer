using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SeedPopup : MonoBehaviour
{
    public GameObject panel;
    public Transform buttonContainer;
    public GameObject seedButtonPrefab;
    private Field currentField;
    private List<GameObject> spawnedButtons = new List<GameObject>();

    private void Start() => Hide();
    public void Show(Field field)
    {
        currentField = field;
        RefreshButtons();
        panel.SetActive(true);
        Debug.Log("Seed count: " + Seed_InventoryManager.Instance.GetAllSeeds().Count);

    }
    public void Hide()
    {
        currentField = null;
        panel.SetActive(false);
        ClearButtons();
    }
    private void ClearButtons()
    {
        foreach (var b in spawnedButtons) Destroy(b);
        spawnedButtons.Clear();
    }
    private void RefreshButtons()
{

    ClearButtons();
    var dict = Seed_InventoryManager.Instance.GetAllSeeds();
    Debug.Log("총 씨앗 종류: " + dict.Count);

    foreach (var kv in dict)
    {
        SeedData seed = kv.Key;
        int count = kv.Value;

        GameObject btn = Instantiate(seedButtonPrefab, buttonContainer);
        spawnedButtons.Add(btn);

        Transform icon = btn.transform.Find("Icon");
        Transform name = btn.transform.Find("Name");
        Transform cnt = btn.transform.Find("Count");

        if (icon != null) icon.GetComponent<Image>().sprite = seed.icon;
        if (name != null) name.GetComponent<TextMeshProUGUI>().text = seed.seedName;
        if (cnt != null) cnt.GetComponent<TextMeshProUGUI>().text = count.ToString();

        Button btnComp = btn.GetComponent<Button>();
        if(btnComp != null)
            btnComp.onClick.AddListener(() => OnSeedClicked(seed));
        else
            Debug.LogError("Button 컴포넌트가 btn에 없음!");
    }
}

    private void OnSeedClicked(SeedData seed)
    {
        if (currentField == null) return;
        if (Seed_InventoryManager.Instance.UseSeed(seed))
        {
            currentField.Plant(seed);
            Hide();
        }
        else Debug.Log("씨앗이 없습니다.");
    }
}
