using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 모든 아이템(작물, 씨앗, 포션 등)의 기본 정보
[CreateAssetMenu(fileName = "NewItem", menuName = "Data/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public string description;
    public Sprite itemIcon;

    public string itemCategory;
    public int price; // 아이템의 구매 가격
}