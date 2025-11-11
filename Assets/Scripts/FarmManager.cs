using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FarmManager : MonoBehaviour
{
    public static FarmManager Instance { get; private set; }
    public SeedPopup seedPopup;
    private Field currentField;
    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }
    public void OnFieldClicked(Field field)
    {
        if (seedPopup.panel.activeSelf && currentField == field)
        {
            seedPopup.Hide();
            currentField = null;
            return;
        }
        if (field.IsEmpty()) {
            currentField = field;   // 클릭한 밭 기억
            seedPopup.Show(field);
        }
        else if (field.IsReady()) field.Harvest();
        else Debug.Log("성장 중입니다...");
    }
}
