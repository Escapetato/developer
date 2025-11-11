using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldSpawner : MonoBehaviour
{
    public GameObject fieldPrefab;
    public int rows = 2;
    public int cols = 3;
    public float spacing = 2f;

    void Start()
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Vector3 pos = new Vector3(c * spacing, r * spacing, 0);
                Instantiate(fieldPrefab, pos, Quaternion.identity, transform);
            }
        }
    }
}
