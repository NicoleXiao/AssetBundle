using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class BuildAssetBundle:Editor{
    /**指定需要打包的路径，从这个路径开始查找打包物体*/
    private static string sourcePath = Application.dataPath + "/PackageAssets";
    private static string streamPath = Application.streamingAssetsPath;
    private static string assetTail = ".unity3d";
    private static List<string> m_ShaderPath = new List<string>();
      
    [MenuItem("Build/Build Windows Resource",false,102)]
    public static void BuildWindowsResource() {
        BuildAssetResource(BuildTarget.StandaloneWindows);
    }


    public static void BuildAssetResource(BuildTarget target) {
       
        if (Directory.Exists(streamPath)) {
            Directory.Delete(streamPath,true);
        }
        Directory.CreateDirectory(streamPath);
        AssetDatabase.Refresh();
        BuildPipeline.BuildAssetBundles(streamPath,BuildAssetBundleOptions.None,target);
        AssetDatabase.Refresh();
    }

  
    [MenuItem("Build/Clear All AssetBundle Name", false, 100)]
    public static void ClearAssetBundlesName() {
        string[] abNames = AssetDatabase.GetAllAssetBundleNames();
        for(int i = 0; i < abNames.Length; i++) {
            AssetDatabase.RemoveAssetBundleName(abNames[i],true);
        }
    }

    [MenuItem("Build/Set AssetBundle Name", false, 101)]
    public static void SetAssetBunldeName() {
        SetName(sourcePath);
        SetShaderAssetBundle();
    }

  
    /// <summary>
    /// 根据目录打包。
    /// shader的打在一起，所以要单独处理(这里根据项目需求改动)
    /// </summary>
    /// <param name="assetsPath"></param>
    public static void SetName(string assetsPath) {
        DirectoryInfo dir = new DirectoryInfo(assetsPath);
        //获取指定目录下所有的文件夹和子文件夹
        FileSystemInfo[] files = dir.GetFileSystemInfos();
        for(int i = 0; i < files.Length; i++) {
            if (files[i] is DirectoryInfo) {
                SetName(files[i].FullName);
            }else if (files[i].Name.EndsWith(".shader")) {
                m_ShaderPath.Add(files[i].FullName);
            }else if (!files[i].Name.EndsWith(".meta")) {
                SetSingleAssetBundleName(files[i].FullName);
            }
            
        }
    }

    private static void SetShaderAssetBundle() {
        if (m_ShaderPath.Count > 0) {
            for (int i = 0; i < m_ShaderPath.Count; i++) {
                GetAssetImport(m_ShaderPath[i]).assetBundleName = "shader.unity3d";
            }
        }
    }

    private static void SetSingleAssetBundleName(string assetPath) {
        AssetImporter assetImporter = GetAssetImport(assetPath);
        string tempName = assetPath.Substring(assetPath.LastIndexOf("PackageAssets")+14);
        string assetName = tempName.Remove(tempName.LastIndexOf("."))+assetTail;
        Debug.Log(assetName.ToLower());
        assetImporter.assetBundleName = assetName.ToLower();
    }
    
    private static AssetImporter GetAssetImport(string assetPath) {
        string importerPath = "Assets" + assetPath.Substring(Application.dataPath.Length);
        return AssetImporter.GetAtPath(importerPath);
    }

}
