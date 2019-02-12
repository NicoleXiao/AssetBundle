using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.U2D;
using UObject = UnityEngine.Object;

public enum AssetType {
    Prefab,
    Sprite
}

/// <summary>
/// 用于外部调用的加载类
/// </summary>
public class LoadAssetMgr : Singleton<LoadAssetMgr> {

    private AssetBundleMgr resLoader;
    public override void Init() {
       resLoader = AssetBundleMgr.instance;
    }
    /// <summary>
    /// isUse表示是否在创建这个物体后立马使用它
    /// 不使用它要将状态设置为待机
    /// 先从对象池中取，取不到就加载
    /// </summary>
    /// <param name="abName"></param>
    /// <param name="assetName"></param>
    /// <param name="isUse"></param>
    /// <param name="action"></param>
    public void LoadPrefab(string abName, string assetName, bool isUse, Action<GameObject> action = null) {
        GameObject prefab = null;
        if (PoolManager.instance.isExitObject<GameObject> (abName, assetName, AssetType.Prefab)) {
            if (isUse) {
                prefab = PoolManager.instance.GetObject<GameObject> (abName, assetName, AssetType.Prefab);
                prefab.gameObject.SetActive (true);
            }
            if (action != null) {
                action (prefab);
            }
            return;
        }
        resLoader.LoadAsset<GameObject> (abName, assetName, delegate (UnityEngine.Object obj) {
            if (obj != null) {
                prefab = PoolManager.instance.GetPool<GameObject> (AssetType.Prefab).AddItemToPool (abName, assetName, (GameObject)obj, isUse);
                if(!isUse)
                  prefab.gameObject.SetActive (false);
            } else {
                Debug.LogError("加载物体 "+assetName+" 失败");
            }
            if (action != null) {
                action (prefab);
            }
        });
    }


    public void LoadSprite(string abName, string assetName, bool isUse, Action<Sprite> action = null) {
        Sprite sp = null;
        if (PoolManager.instance.isExitObject<Sprite> (abName, assetName, AssetType.Sprite)) {
            if (isUse) {
                sp = PoolManager.instance.GetObject<Sprite> (abName, assetName, AssetType.Sprite);
            }
            if (action != null) {
                action (sp);
            }
            return;
        }
        LoadTexture2D (abName, assetName, delegate (Texture2D tex) {
            if (tex != null) {
                sp = Sprite.Create (tex, new Rect (0, 0, tex.width, tex.height), Vector2.zero);
                sp = PoolManager.instance.GetPool<Sprite> (AssetType.Sprite).AddItemToPool (abName,assetName,sp,isUse);
            } else {
               Debug.LogError ("加载精灵 " + assetName + " 失败");
            }
            if (action != null) {
                action (sp);
            }
        });

    }

    private void LoadTexture2D(string abName, string assetName, Action<Texture2D> action = null) {
        resLoader.LoadAsset<Texture2D> (abName, assetName, delegate (UnityEngine.Object obj) {
            if (obj != null) {
                if (action != null) {
                    action ((Texture2D)obj);
                }
            } else {
               Debug.LogError ("加载图片 " + assetName + " 失败");
            }
        });
    }

 


   
}

