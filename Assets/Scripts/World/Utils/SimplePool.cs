using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple generic object pool for GameObjects.
/// </summary>
public class SimplePool
{
    readonly GameObject prefab;
    readonly Transform parent;
    readonly Queue<GameObject> pool = new Queue<GameObject>();

    public SimplePool(GameObject prefab, Transform parent = null)
    {
        this.prefab = prefab;
        this.parent = parent;
    }

    public GameObject Get()
    {
        if (pool.Count > 0)
        {
            GameObject go = pool.Dequeue();
            go.SetActive(true);
            return go;
        }
        return Object.Instantiate(prefab, parent);
    }

    public void Return(GameObject go)
    {
        go.SetActive(false);
        pool.Enqueue(go);
    }
}
