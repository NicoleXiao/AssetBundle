using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HeroItem : MonoBehaviour
{
    public Image m_heroHead;
    public Text m_heroName;

    public void ChangeHero(int index) {
        string path = "UI_hero_"+index;
        m_heroHead.enabled = false;
        LoadAssetMgr.instance.LoadSprite (LoadPathMgr.Hero_Atlas_Path, path, true, delegate (Sprite sp) {
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
