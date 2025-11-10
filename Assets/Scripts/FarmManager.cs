using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FarmManager : MonoBehaviour
{
    public static FarmManager Instance { get; private set; }
    public SeedPopup seedPopup;
    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }
    public void OnFieldClicked(Field field)
    {
        if (field.IsEmpty()) seedPopup.Show(field);
        else if (field.IsReady()) field.Harvest();
        else Debug.Log("성장 중입니다...");
    }
}
