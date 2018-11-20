﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class BuildAssetBundle : Editor {
    private static string buildPath = "";
    private static string copyPath = "";
    private static string manifestPath = "";
    private static string assetTail = ".unity3d";
    /**设置打包路径*/
    private static string sourcePath = Application.dataPath + "/PackageAssets";
    /**所有shader单独处理，打进一个包里面，这里根据具体情况设置*/
    private static List<string> m_ShaderPath = new List<string>();

    [MenuItem("Build/Build Windows Resource", false, 102)]
    public static void BuildWindowsResource() {
        /**打对应平台的包，检查是否是对应平台，没有则需要提醒。*/
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows) {
            BuildAssetResource(BuildTarget.StandaloneWindows);
        } else {
            EditorUtility.DisplayDialog("Bulide Fail", "Current BuildTarget is not Windows，please switch", "ok");
        }

    }

    [MenuItem("Build/Build Android Resource", false, 103)]
    public static void BuildAndroidResource() {
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android) {
            BuildAssetResource(BuildTarget.Android);
        } else {
            EditorUtility.DisplayDialog("Bulide Fail", "Current BuildTarget is not Android，please switch", "ok");
        }

    }

    [MenuItem("Build/Build IOS Resource", false, 104)]
    public static void BuildIOSResource() {
        if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS) {
            BuildAssetResource(BuildTarget.iOS);
        } else {
            EditorUtility.DisplayDialog("Bulide Fail", "Current BuildTarget is not IOS，please switch", "ok");
        }
    }

    [MenuItem("Build/Clear All AssetBundle Name", false, 100)]
    public static void ClearAssetBundlesName() {
        string[] abNames = AssetDatabase.GetAllAssetBundleNames();
        for (int i = 0; i < abNames.Length; i++) {
            AssetDatabase.RemoveAssetBundleName(abNames[i], true);
        }
    }

    [MenuItem("Build/Set AssetBundle Name", false, 101)]
    public static void SetAssetBunldeName() {
        SetName(sourcePath);
        SetShaderAssetBundle();
    }

    [MenuItem("Build/Move AssetBunlde To Windows", false, 105)]
    public static void MoveToStreamingAssetsForWindows() {
        buildPath = GetSaveABPath(BuildTarget.StandaloneWindows);
        copyPath = GetABCopyPath(BuildTarget.StandaloneWindows);
        Move(buildPath, copyPath);
        AssetDatabase.Refresh();
    }

    [MenuItem("Build/Move AssetBunlde To Android", false, 106)]
    public static void MoveToStreamingAssetsForAndroid() {
        buildPath = GetSaveABPath(BuildTarget.Android);
        copyPath = GetABCopyPath(BuildTarget.Android);
        Move(buildPath, copyPath);
        AssetDatabase.Refresh();
    }

    [MenuItem("Build/Move AssetBunlde To IOS", false, 107)]
    public static void MoveToStreamingAssetsForIOS() {
        buildPath = GetSaveABPath(BuildTarget.iOS);
        copyPath = GetABCopyPath(BuildTarget.iOS);
        Move(buildPath, copyPath);
        AssetDatabase.Refresh();
    }


    public static void BuildAssetResource(BuildTarget target) {
        buildPath = GetSaveABPath(target);
        string path = buildPath.Substring(0, buildPath.LastIndexOf('/'));
        manifestPath = buildPath + path.Substring(path.LastIndexOf('/') + 1) + "";
        if (!Directory.Exists(buildPath)) {
            Directory.CreateDirectory(buildPath);
        }
        List<string> oldFile = new List<string>();
        if (File.Exists(manifestPath)) {
            AssetBundleManifest old = LoadManifest(manifestPath);
            oldFile = old.GetAllAssetBundles().ToList<string>();

        }
        AssetDatabase.RemoveUnusedAssetBundleNames();
        AssetDatabase.Refresh();
        BuildPipeline.BuildAssetBundles(buildPath, BuildAssetBundleOptions.None, target);
        if (oldFile.Count > 0) {
            AssetBundleManifest newManifestInfo = LoadManifest(manifestPath);
            RemoveUnuseFile(oldFile, newManifestInfo.GetAllAssetBundles().ToList<string>());
        }
        AssetDatabase.Refresh();

    }


    public static AssetBundleManifest LoadManifest(string path) {
        AssetBundleManifest manifest = null;
        var bundle = AssetBundle.LoadFromFile(manifestPath);
        manifest = bundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        // 压缩包释放掉
        bundle.Unload(false);
        bundle = null;
        return manifest;
    }

    /// <summary>
    /// 移除不需要的文件
    /// </summary>
    /// <param name="oldFile"></param>
    /// <param name="newFile"></param>
    public static void RemoveUnuseFile(List<string> oldFile, List<string> newFile) {
        for (int i = 0; i < newFile.Count; i++) {
            if (oldFile.Contains(newFile[i])) {
                oldFile.Remove(newFile[i]);
            }
        }
        if (oldFile.Count > 0) {
            for (int j = 0; j < oldFile.Count; j++) {
                string path = buildPath + oldFile[j];
                string manifestPath = path + ".manifest";
                if (File.Exists(path)) {
                    File.Delete(path);
                }
                if (File.Exists(manifestPath)) {
                    File.Delete(manifestPath);
                }
            }
        }
        RemoveUnuseFolder();
    }

    /// <summary>
    /// 移除空文件夹
    /// </summary>
    public static void RemoveUnuseFolder() {
        DirectoryInfo dir = new DirectoryInfo(buildPath);
        DirectoryInfo[] subDirs = dir.GetDirectories("*.*", SearchOption.AllDirectories);
        foreach (DirectoryInfo subdir in subDirs) {
            FileSystemInfo[] subFiles = subdir.GetFileSystemInfos();
            if (subFiles.Count() == 0) {
                subdir.Delete();
            }
        }
    }


    /// <summary>
    /// 设置对应路径下所有资源的ab包名字
    /// 需要特殊处理shader（这个根据自己实际情况来）
    /// </summary>
    /// <param name="assetsPath"></param>
    public static void SetName(string assetsPath) {
        DirectoryInfo dir = new DirectoryInfo(assetsPath);
        //获取指定目录下所有的文件夹和子文件夹
        FileSystemInfo[] files = dir.GetFileSystemInfos();
        for (int i = 0; i < files.Length; i++) {
            if (files[i] is DirectoryInfo) {
                SetName(files[i].FullName);
            } else if (files[i].Name.EndsWith(".shader")) {
                m_ShaderPath.Add(files[i].FullName);
            } else if ((!files[i].Name.EndsWith(".meta")) && (!files[i].Name.EndsWith(".FBX"))) {
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
        string tempName = assetPath.Substring(assetPath.LastIndexOf("PackageAssets") + 14);
        string assetName = tempName.Remove(tempName.LastIndexOf(".")) + assetTail;
        Debug.Log(assetName.ToLower());
        assetImporter.assetBundleName = assetName.ToLower();
    }

    private static AssetImporter GetAssetImport(string assetPath) {
        string importerPath = "Assets" + assetPath.Substring(Application.dataPath.Length);
        return AssetImporter.GetAtPath(importerPath);
    }

    public static string GetSaveABPath(UnityEditor.BuildTarget target) {
        string path = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf(@"/") + 1) + "AssetBundleBuild";
        return GetEditorPath(target, path);
    }

    public static string GetABCopyPath(UnityEditor.BuildTarget target) {
        string path = Application.streamingAssetsPath + "/AssetBundle";
        return GetEditorPath(target, path);
    }

    private static string GetEditorPath(UnityEditor.BuildTarget target, string path) {
        if (target == UnityEditor.BuildTarget.StandaloneWindows) {
            return path + "/Windows/";
        } else if (target == UnityEditor.BuildTarget.Android) {
            return path + "/Android/";
        } else if (target == UnityEditor.BuildTarget.iOS) {
            return path + "/IOS/";
        }
        return "";
    }

    /// <summary>
    ///  复制数据
    /// </summary>
    /// <param name="oriPath"></param>
    /// <param name="movePath"></param>
    public static void Move(string oriPath, string movePath) {
        if (Directory.Exists(oriPath)) {
            if (!Directory.Exists(movePath)) {
                Directory.CreateDirectory(movePath);
            }
            List<string> files = new List<string>(Directory.GetFiles(oriPath));
            List<string> folders = new List<string>(Directory.GetDirectories(oriPath));
            files.ForEach(c => {
                string destFile = Path.Combine(movePath, Path.GetFileName(c));
                if (!destFile.EndsWith(".meta") && !destFile.EndsWith(".manifest")) {
                    File.Copy(c, destFile, true);//覆盖模式
                }
            });
            folders.ForEach(c => {
                string destDir = Path.Combine(movePath, Path.GetFileName(c));
                Move(c, destDir);
            });
        } else {
            Debug.Log(" 没有可复制的资源，请检查打包资源 ！");
        }
    }
}
