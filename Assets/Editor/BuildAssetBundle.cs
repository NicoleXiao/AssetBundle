﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class BuildAssetBundle : Editor {
    private static string buildPath = "";
    private static string copyPath = "";
    private static string assetTail = ".unity3d";
    private static string manifestPath = "";
    /**设置打包路径*/
    private static string sourcePath = Application.dataPath + "/" + LoadPathMgr.Build_Path;
    private static List<string> m_ShaderPath = new List<string> ();

    [MenuItem ("Build/Build Windows Resource", false, 102)]
    public static void BuildWindowsResource() {
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows) {
            BuildAssetResource (BuildTarget.StandaloneWindows);
        } else {
            EditorUtility.DisplayDialog ("Bulide Fail", "Current BuildTarget is not Windows，please switch", "ok");
        }

    }

    [MenuItem ("Build/Build Android Resource", false, 103)]
    public static void BuildAndroidResource() {
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android) {
            BuildAssetResource (BuildTarget.Android);
        } else {
            EditorUtility.DisplayDialog ("Bulide Fail", "Current BuildTarget is not Android，please switch", "ok");
        }

    }

    [MenuItem ("Build/Build IOS Resource", false, 104)]
    public static void BuildIOSResource() {
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS) {
            BuildAssetResource (BuildTarget.iOS);
        } else {
            EditorUtility.DisplayDialog ("Bulide Fail", "Current BuildTarget is not IOS，please switch", "ok");
        }
    }

    [MenuItem ("Build/Clear All AssetBundle Name", false, 100)]
    public static void ClearAssetBundlesName() {
        string[] abNames = AssetDatabase.GetAllAssetBundleNames ();
        for (int i = 0; i < abNames.Length; i++) {
            AssetDatabase.RemoveAssetBundleName (abNames[i], true);
        }
    }

    [MenuItem ("Build/Set AssetBundle Name", false, 101)]
    public static void SetAssetBunldeName() {
        SetName (sourcePath);
        SetShaderAssetBundle ();
    }

    [MenuItem ("Build/Move AssetBunlde To Windows", false, 105)]
    public static void MoveToStreamingAssetsForWindows() {
        buildPath = PathManager.GetSaveABPath (BuildTarget.StandaloneWindows);
        copyPath = PathManager.GetABCopyPath (BuildTarget.StandaloneWindows);
        PathManager.Move (buildPath, copyPath);
        AssetDatabase.Refresh ();
    }


    [MenuItem ("Build/Move AssetBunlde To Android", false, 106)]
    public static void MoveToStreamingAssetsForAndroid() {
        buildPath = PathManager.GetSaveABPath (BuildTarget.Android);
        copyPath = PathManager.GetABCopyPath (BuildTarget.Android);
        PathManager.Move (buildPath, copyPath);
        AssetDatabase.Refresh ();
    }

    [MenuItem ("Build/Move AssetBunlde To IOS", false, 107)]
    public static void MoveToStreamingAssetsForIOS() {
        buildPath = PathManager.GetSaveABPath (BuildTarget.iOS);
        copyPath = PathManager.GetABCopyPath (BuildTarget.iOS);
        PathManager.Move (buildPath, copyPath);
        AssetDatabase.Refresh ();
    }

    //[MenuItem ("Assets/Set Select AssetBundle Name", false)]
    //public static void BulidSelect() {
    //    string[] strs = Selection.assetGUIDs;
    //    string path = AssetDatabase.GUIDToAssetPath (strs[0]);

    //}


    public static void BuildAssetResource(BuildTarget target) {
        buildPath = PathManager.GetSaveABPath (target);
        string path = buildPath.Substring (0, buildPath.LastIndexOf ('/'));
        manifestPath = buildPath + path.Substring (path.LastIndexOf ('/') + 1) + "";
        if (!Directory.Exists (buildPath)) {
            Directory.CreateDirectory (buildPath);
        }
        List<string> oldFile = new List<string> ();
        if (File.Exists (manifestPath)) {
            AssetBundleManifest old = LoadManifest (manifestPath);
            oldFile = old.GetAllAssetBundles ().ToList<string> ();

        }
        AssetDatabase.RemoveUnusedAssetBundleNames ();
        AssetDatabase.Refresh ();
        BuildPipeline.BuildAssetBundles (buildPath, BuildAssetBundleOptions.None, target);
        if (oldFile.Count > 0) {
            AssetBundleManifest newManifestInfo = LoadManifest (manifestPath);
            RemoveUnuseFile (oldFile, newManifestInfo.GetAllAssetBundles ().ToList<string> ());
        }
        AssetDatabase.Refresh ();
        Debug.Log ("Finish Bulid Asset Bundle");

    }


    public static AssetBundleManifest LoadManifest(string path) {
        AssetBundleManifest manifest = null;
        var bundle = AssetBundle.LoadFromFile (manifestPath);
        manifest = bundle.LoadAsset<AssetBundleManifest> ("AssetBundleManifest");
        // 压缩包释放掉
        bundle.Unload (false);
        bundle = null;
        return manifest;
    }

    public static void RemoveUnuseFile(List<string> oldFile, List<string> newFile) {
        for (int i = 0; i < newFile.Count; i++) {
            if (oldFile.Contains (newFile[i])) {
                oldFile.Remove (newFile[i]);
            }
        }
        if (oldFile.Count > 0) {
            for (int j = 0; j < oldFile.Count; j++) {
                string path = buildPath + oldFile[j];
                string manifestPath = path + ".manifest";
                if (File.Exists (path)) {
                    File.Delete (path);
                }
                if (File.Exists (manifestPath)) {
                    File.Delete (manifestPath);
                }
            }
        }
        RemoveUnuseFolder ();
    }

    public static void RemoveUnuseFolder() {
        DirectoryInfo dir = new DirectoryInfo (buildPath);
        DirectoryInfo[] subDirs = dir.GetDirectories ("*.*", SearchOption.AllDirectories);
        foreach (DirectoryInfo subdir in subDirs) {
            FileSystemInfo[] subFiles = subdir.GetFileSystemInfos ();
            if (subFiles.Count () == 0) {
                subdir.Delete ();
            }
        }
    }


    public static void SetName(string assetsPath) {
        DirectoryInfo dir = new DirectoryInfo (assetsPath);
        //获取指定目录下所有的文件夹和子文件夹
        FileSystemInfo[] files = dir.GetFileSystemInfos ();
        for (int i = 0; i < files.Length; i++) {
            if (files[i].Name == "Heros" || files[i].Name == "Scenes") {
                // continue;
            }
            if (files[i] is DirectoryInfo) {
                SetName (files[i].FullName);
            } else if (files[i].Name.EndsWith (".shader")) {
                m_ShaderPath.Add (files[i].FullName);
            } else if ((!files[i].Name.EndsWith (".meta")) && (!files[i].Name.EndsWith (".FBX"))) {
                SetSingleAssetBundleName (files[i].FullName);
            }
        }
    }

    private static void SetShaderAssetBundle() {
        if (m_ShaderPath.Count > 0) {
            for (int i = 0; i < m_ShaderPath.Count; i++) {
                GetAssetImport (m_ShaderPath[i]).assetBundleName = "shader.unity3d";
            }
        }
    }

    private static void SetSingleAssetBundleName(string assetPath) {
        AssetImporter assetImporter = GetAssetImport(assetPath);
        string tempName = assetPath.Substring(assetPath.LastIndexOf(LoadPathMgr.Build_Path) + LoadPathMgr.Build_Path.Length + 1);
        string assetName = tempName.Remove(tempName.LastIndexOf(".")) + assetTail;
        string specilFolder = "Atlas";
        if (assetName.Contains (specilFolder)) {
            assetName = assetName.Substring (0, assetName.LastIndexOf ('\\')) + assetTail;
        }
        assetImporter.assetBundleName = assetName.ToLower();
    }



    private static AssetImporter GetAssetImport(string assetPath) {
        string importerPath = "Assets" + assetPath.Substring (Application.dataPath.Length);
        return AssetImporter.GetAtPath (importerPath);
    }

}
