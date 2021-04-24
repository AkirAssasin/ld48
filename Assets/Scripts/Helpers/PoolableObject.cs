using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolableObject<T> : MonoBehaviour {

    public static List<T> actives = new List<T>();
    public static List<T> pool = new List<T>();
    public bool inPool = true;

    public static T GetFromPool (GameObject _prefab) {

        T result;

        if (pool.Count > 0) {
            result = pool[pool.Count - 1];
            pool.RemoveAt(pool.Count - 1);
        } else {
            result = Instantiate(_prefab).GetComponent<T>();
        }

        return result;

    }

    protected virtual T self {
        get {
            return default(T);
        }
    }

    void OnDestroy () {

        // Debug.Log(typeof(T) + " count before removal: " + pool.Count + ", " + actives.Count);

        if (inPool) {
            pool.Remove(self);
        } else actives.Remove(self);

    }
        
    protected void Initialize () {

        if (inPool) {

            inPool = false;
            actives.Add(self);

        }

    }

    protected bool Pool () {

        if (inPool) return false;

        inPool = true;
        pool.Add(self);
        actives.Remove(self);

        return true;

    }
}