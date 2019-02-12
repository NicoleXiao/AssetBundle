using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ABTest : MonoBehaviour
{
    public Button m_btn;
    public Transform m_heroTrans;
    private int index = 1;

    /// <summary>
    /// 初始化
    /// </summary>
    private void Start() {
        PoolManager.instance.Initialize ();
        AssetBundleMgr.instance.Initialize (() => {
            m_btn.onClick.AddListener (() => {
                ClickLoadHero ();
            });
        });
    }


    public void ClickLoadHero() {
        if (index > 5) {
            return;
        }
       int image_index = index;
       LoadAssetMgr.instance.LoadPrefab (LoadPathMgr.Hero_Prefab, LoadPathMgr.Hero_Prefab.GetAssetName (), true, delegate (GameObject obj) {
                obj.transform.SetParent (m_heroTrans);
                obj.transform.localScale = Vector3.one;
                obj.transform.localPosition = Vector3.one;
                HeroItem item = obj.GetComponent<HeroItem> ();
                item.ChangeHero (image_index);
        });
        index++;
    }

 

}
