using System.Collections.Generic;
using UnityEngine;

// 상점에서 팔 아이템 목록을 저장하는 데이터베이스
[CreateAssetMenu(fileName = "StoreDB", menuName = "Data/Store Database")]
public class StoreDatabase : ScriptableObject
{
    public List<ItemData> itemsForSale;
}