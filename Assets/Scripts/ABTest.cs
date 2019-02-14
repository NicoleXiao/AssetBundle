using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ABTest : MonoBehaviour {
    public Button m_loadBtn;
    public Button m_unLoadBtn;
    public Transform m_heroTrans;
    private int index = 1;
    private List<HeroItem> m_heros = new List<HeroItem> ();
    /// <summary>
    /// 初始化
    /// </summary>
    private void Start() {
        PoolManager.instance.Initialize ();
        AssetBundleMgr.instance.Initialize (() => {
            m_loadBtn.onClick.AddListener (() => {
                ClickLoadHero ();
            });
            m_unLoadBtn.onClick.AddListener (() => {
                ClickUnLoad ();
            });
        });
    }


    private void ClickUnLoad() {
        if (m_heros.Count > 0) {
            index = m_heros.Count;
            m_heros[index-1].DespawnSprite ();
            PoolManager.instance.DeSpawn<GameObject> (AssetType.Prefab, m_heros[index-1].gameObject);
            m_heros.RemoveAt (index-1);
        }
    }


    private void ClickLoadHero() {
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
            m_heros.Add (item);
        });
        index++;
    }



}
