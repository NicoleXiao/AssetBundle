using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeroItem : MonoBehaviour
{
    public Image m_heroHead;
    public Text m_heroName;

    public void ChangeHero(int index) {
        string path = LoadPathMgr.Image_Path + index.ToString () + LoadPathMgr.Asset_Tail;
        m_heroHead.enabled = false;
        LoadAssetMgr.instance.LoadSprite (path, path.GetAssetName (), true, delegate (Sprite sp) {
            DespawnSprite ();
            m_heroHead.sprite = sp;
            m_heroHead.SetNativeSize ();
            m_heroHead.enabled = true;
        });
      
    }

    /// <summary>
    /// 回收Sprite
    /// </summary>
    public void DespawnSprite() {
        if (m_heroHead.sprite) {
            PoolManager.instance.DeSpawn<Sprite> (AssetType.Sprite, m_heroHead.sprite);
            m_heroHead.sprite = null;
           
        }
    }
}
