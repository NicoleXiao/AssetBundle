using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadPathMgr 
{
    public static string Asset_Tail = ".unity3d";
    public static string Build_Path = "Package";
    public static string Image_Path = "UI/UI_hero_";
    public static string Hero_Prefab = "Prefab/HeroItem.unity3d";
}

public static class StringExtension {
    public static string GetAssetName(this string assetPath) {
        var index1 = assetPath.LastIndexOf ('/');
        var index2 = assetPath.LastIndexOf ('.');
        return assetPath.Substring (index1 + 1, index2 - index1 - 1);
    }
}
