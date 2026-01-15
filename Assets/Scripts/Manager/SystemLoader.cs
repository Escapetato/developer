using UnityEngine;

public class SystemLoader : MonoBehaviour
{
    public GameObject managersPrefab;

    void Awake()
    {
        // 게임에 DBManager가 존재하는지 확인 (대표로 하나만 검사)
        if (DBManager.Instance == null)
        {
            // 없으면 프리팹 생성!
            GameObject go = Instantiate(managersPrefab);
            go.name = "@Managers"; // 이름 깔끔하게 정리

            // ★핵심★ 생성된 덩어리를 통째로 파괴 금지시킴
            DontDestroyOnLoad(go);
        }
        else
        {
            // 이미 매니저들이 살아있다면, 이 로더는 할 일이 없으니 조용히 사라짐
            Destroy(gameObject);
        }
    }
}