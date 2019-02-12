using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ObjectPool<T> where T : UnityEngine.Object {
    public string name { get; set; }
    /** 每次自动清理几个游戏对象。 */
    public int cullMaxPerPass = 5;
    /** 每过多久执行一遍自动清理，单位是秒。从上一次清理过后开始计时 */
    public float cullDelay = 30f;
    /** 物体多久没有使用将会被清除 */
    public float cullIdleTime = 30f;
    /** 清理闲置对象协程是否启动 */
    private bool cullingActive = false;

    private float currentTime = 0f;
    /**这里应该是abName+assetName的HasCode 的值*/
    public Dictionary<int, List<PoolItem<T>>> m_spawned = new Dictionary<int, List<PoolItem<T>>>();
    private List<PoolItem<T>> m_poolItems = new List<PoolItem<T>>();

    /// <summary>
    /// 将对象实例化并且加入到对象池中
    /// use 代表这个物体是否是被克隆并马上使用
    /// </summary>
    /// <param name="key"></param>
    /// <param name="obj"></param>
    /// <param name="use"></param>
    /// <returns></returns>
    public T AddItemToPool(string abName,string assetName, T obj, bool use = false) {
        T prefab = UnityEngine.Object.Instantiate(obj);
        PoolItem<T> item = new PoolItem<T>();
        item.obj = prefab;
        item.abName = abName;
        item.poolState = use ? PoolState.Use : PoolState.Idle;
        item.useTime = Time.realtimeSinceStartup;
        int key = GetHashCode(abName,assetName);
        if (!m_spawned.ContainsKey(key)) {
            m_spawned.Add(key, new List<PoolItem<T>>());
        }
        if (use) {
            AssetBundleMgr.instance.AddAssetBundleReference(abName);
        }
        m_poolItems.Add(item);
        m_spawned[key].Add(item);
        SetParent(prefab);
        //Log.Info(string.Format("{0} 对象加入对象池",abName));
        return item.obj;
    }

    private int GetHashCode(string abName, string assetName) {
        return (abName  + assetName).GetHashCode();
    }

    public bool IsPoolExitObj(string abName, string assetName) {
        int key = GetHashCode (abName, assetName);
        if (m_spawned.ContainsKey (key)) {
            return true;
        }
        return false;
   }

    /// <summary>
    /// 从对象池中获取物体。
    /// 获取到物体是空的，需要获取的物体还没有加载过。
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public T GetFromPool(string abName, string assetName) {
        T obj = null;
        int key = GetHashCode(abName, assetName);
        if (m_spawned.ContainsKey(key)) {
            if (m_spawned[key].Count > 0) {
                for (int i = 0; i < m_spawned[key].Count; i++) {
                    PoolItem<T> item = m_spawned[key][i];
                    if (item.poolState == PoolState.Idle) {
                        item.poolState = PoolState.Use;
                        item.useTime = Time.realtimeSinceStartup;
                        obj = item.obj;
                        AssetBundleMgr.instance.AddAssetBundleReference(abName);
                        break;
                    }
                }
            }
            if (obj == null) {
                obj = AddItemToPool(abName,assetName, m_spawned[key][0].obj, true);
            }
        }
        return obj;
    }

    /// <summary>
    /// 回收对象
    /// </summary>
    /// <param name="name"></param>
    /// <param name="obj"></param>
    public void Recycle(T obj) {
        for(int i = 0; i < m_poolItems.Count; i++) {
            if (m_poolItems[i].obj.Equals(obj)) {
#if AB_LOADER
                AssetBundleMgr.instance.UnloadAssetBundle (m_poolItems[i].abName, true);
#endif
                m_poolItems[i].poolState = PoolState.Idle;
                break;
            }
        }
    }

    /// <summary>
    /// 用于自动释放对象检测
    /// </summary>
    public bool CanReleaseGameObject() {
        currentTime += Time.deltaTime;
        if (currentTime > cullDelay && !cullingActive) {
            currentTime = 0;
            return true;
        }
        return false;
    }

    /// <summary>
    /// 自动释放闲置的物体
    /// </summary>
    /// <returns></returns>
    public IEnumerator CullDespawned() {
        Debug.Log("开始清理");
        cullingActive = true;
        int releaseCount = 0;
        foreach (List<PoolItem<T>> items in m_spawned.Values) {
            for (int i = 0; i < items.Count; i++) {
                if (items[i].poolState == PoolState.Idle) {
                    float seconds = Time.realtimeSinceStartup - items[i].useTime;
                    if (seconds > cullIdleTime) {
#if UNITY_EDITOR
                        UnityEngine.Object.DestroyImmediate(items[i].obj);
#else
                        UnityEngine.Object.Destroy(items[i].obj);
#endif
#if AB_LOADER
                        AssetBundleMgr.instance.UnloadAssetBundle(items[i].abName,true);
#endif
                        items.RemoveAt(i);
                        releaseCount++;
                    }
                    if (releaseCount >= cullMaxPerPass) {
                        releaseCount = 0;
                        yield return new WaitForSeconds(cullDelay);
                    }
                }
            }
            cullingActive = false;
        }
    }

    private void SetParent(T obj) 
    {
        if (obj.GetType() == typeof(GameObject))
        {
            (obj as GameObject).transform.SetParent(PoolManager.instance.GetParent(AssetType.Prefab));
        }
         
    }

}

public enum PoolState {
    Use,
    Idle
}

public class PoolItem<T> {
   
    public T obj;
    /**物体的ab包名字*/
    public string abName;
    /**物体最近一次使用的时间*/
    public float useTime;
    /**物体状态(闲置/使用)*/
    public PoolState poolState { get; set; }

}
