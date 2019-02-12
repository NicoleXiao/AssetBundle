using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PathManager  {

    private  static string serverPath = "http://localhost/";

    #region Run Path
    /// <summary>
    /// 版本信息服务器的地址
    /// </summary>
    /// <returns></returns>
    public static string GetVersionServerPath() {
        string path = serverPath + "Version";
        return GetRunPath(path);
    }

    /// <summary>
    /// AB包服务器地址
    /// </summary>
    /// <returns></returns>
    public static string GetABServerPath() {
        string path = serverPath + "AssetBundle";
        return GetRunPath(path);
    }


    /// <summary>
    /// 获取运行条件下，StreamingAssets下Verison的路径
    /// </summary>
    /// <returns></returns>
    public static string GetRunVersionStreamingPath() {
        string path = Application.streamingAssetsPath + "/Version";
        return GetRunPath(path);
    }

    /// <summary>
    /// 获取持久化数据地址
    /// </summary>
    /// <returns></returns>
    public static string GetRunPerFilePath() {
        return GetRunPath(Application.persistentDataPath);
    }

    /// <summary>
    /// 运行条件下获取StreamingAssets下面的ab包路径
    /// </summary>
    /// <param name="isABLoad"></param>
    /// <returns></returns>
    public static string GetRunAbStreamingPath(bool isABLoad) {
        if (Application.platform == RuntimePlatform.WindowsEditor && !isABLoad) {
            return "Assets/PackageAssets/";
        }
        string path = Application.streamingAssetsPath + "/AssetBundle";
        return GetRunPath(path);

    }

    /// <summary>
    /// 游戏运行的路径
    /// </summary>
    /// <returns></returns>
    private static string GetRunPath(string path) {
        if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor) {
            return path + "/Windows/";
        } else if (Application.platform == RuntimePlatform.Android) {
            return path + "/Android/";
        } else if (Application.platform == RuntimePlatform.IPhonePlayer) {
            return path + "/IOS/";
        }
        return "";
    }


    #endregion

#if UNITY_EDITOR
    /// <summary>
    /// 项目打包存放路径
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public static string GetSaveABPath(UnityEditor.BuildTarget target) {
        string path = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf(@"/") + 1) + "AssetBundleBuild";
        return GetEditorPath(target,path);
    }

    /// <summary>
    /// 保存在项目下面的版本信息
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public static string GetSaveVersionPath(UnityEditor.BuildTarget target) {
        string path = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf(@"/") + 1) + "Version";
        return GetEditorPath(target,path);
    }

    /// <summary>
    /// 从外部复制ab包到StreamingAssets下面的路径
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public static string GetABCopyPath(UnityEditor.BuildTarget target) {
        string path = Application.streamingAssetsPath + "/AssetBundle";
        return GetEditorPath(target,path);
    }

    /// <summary>
    /// 从外部复制版本信息到StreamingAssets下面
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public static string GetVersionCopyPath(UnityEditor.BuildTarget target) {
        return GetEditorPath(target, Application.streamingAssetsPath+"/Version");
    }

    /// <summary>
    /// Editor 编辑器下面的路径
    /// </summary>
    /// <param name="target"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    private static string GetEditorPath(UnityEditor.BuildTarget target,string path) {
        if (target == UnityEditor.BuildTarget.StandaloneWindows) {
            return path + "/Windows/";
        } else if (target == UnityEditor.BuildTarget.Android) {
            return path + "/Android/";
        } else if (target == UnityEditor.BuildTarget.iOS) {
            return path + "/IOS/";
        }
        return "";
    }
#endif

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
                if (!destFile.EndsWith(".meta") &&!destFile.EndsWith(".manifest")) {
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
