using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class PoolManager : MonoSingleton<PoolManager> {
    private static string POOL_ROOT = "PoolRoot";
    private bool isInitialized = false;

    /**这里的int类型应该是AssetType的类型*/
    private Dictionary<string, object> mapPools = new Dictionary<string, object> ();
    Dictionary<string, Transform> mapNodes = new Dictionary<string, Transform> ();

    public Transform GetParent(AssetType obj) {
        return mapNodes[obj.ToString ()];
    }

    public void Initialize() {
        if (isInitialized) return;
        //创建节点
        var root = GameObject.Find (POOL_ROOT);
        if (!root) {
            root = new GameObject (POOL_ROOT);
            root.AddComponent<DontDestroyOnload> ();
        }
        var types = Enum.GetNames (typeof (AssetType));
        foreach (var type in types) {
            var trans = new GameObject (type.ToString ()).transform;
            trans.SetParent (root.transform);
            mapNodes[type] = trans;
        }
        isInitialized = true;
    }

    public ObjectPool<T> GetPool<T>(AssetType assetType) where T : UnityEngine.Object {
        var type = typeof (T);
        var key = Enum.GetName (typeof (AssetType), assetType);
        ObjectPool<T> pool = null;
        if (!mapPools.ContainsKey (key)) {
            pool = new ObjectPool<T> ();
            mapPools.Add (key, pool);
        } else {
            pool = mapPools[key] as ObjectPool<T>;
        }
        return pool;
    }

    private int GetHashCode(string abName, string assetName) {
        return (abName + assetName).GetHashCode ();
    }

    public bool isExitObject<T>(string assetPath, string assetName, AssetType assetType) where T : UnityEngine.Object {
        return GetPool<T> (assetType).IsPoolExitObj (assetPath, assetName);
    }

    public T GetObject<T>(string assetPath, string assetName, AssetType assetType) where T : UnityEngine.Object {
        return GetPool<T> (assetType).GetFromPool (assetPath, assetName);
    }

    public void DeSpawn<T>(AssetType assetType, T obj) where T : UnityEngine.Object {
        if (assetType == AssetType.Prefab) {
            GameObject g = obj as GameObject;
            g.gameObject.SetActive (false);
            g.transform.SetParent (GameObject.Find (Enum.GetName (typeof (AssetType), assetType)).transform);
        }
        GetPool<T> (assetType).Recycle (obj);
    }




}
