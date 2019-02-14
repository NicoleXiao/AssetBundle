using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UObject = UnityEngine.Object;
using UnityEngine.Networking;

public class AssetBundleMgr : MonoSingleton<AssetBundleMgr> {

    public Action m_LoadFailEvent = null;
    private string m_manifestName = "";
    private string[] m_AllManifest = null;
    private AssetBundleManifest m_abManifest = null;
    /**记录加载过的ab包依赖项*/
    private Dictionary<string, string[]> m_dependencies = new Dictionary<string, string[]> ();
    /**记录已经加载的ab包*/
    private Dictionary<string, LoadedAssetBundle> m_loadAssetDic = new Dictionary<string, LoadedAssetBundle> ();
    /**加载过一次之后持久存在的资源*/
    private List<string> m_permanentAssetBundle = new List<string> ();
    /**记录加载请求*/
    private Dictionary<string, List<LoadAssetRequest>> m_LoadRequests = new Dictionary<string, List<LoadAssetRequest>> ();
    /**记录正在等待的加载请求*/
    private Dictionary<string, List<LoadAssetRequest>> m_waitRequests = new Dictionary<string, List<LoadAssetRequest>> ();

    private Dictionary<string, float> m_loadTime = new Dictionary<string, float> ();
    private List<string> m_LoadingABName = new List<string> ();

    class LoadAssetRequest {
        public Type assetType;
        public string assetName;
        public Action<UObject> sharpFunc;
    }

    private void SetManifestName() {
        if (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer) {
            m_manifestName = "Windows";
        } else if (Application.platform == RuntimePlatform.Android) {
            m_manifestName = "Android";
        } else if (Application.platform == RuntimePlatform.IPhonePlayer) {
            m_manifestName = "IOS";
        }
    }

    public void Initialize(Action initOK = null) {
        SetManifestName ();
        LoadFormAB<AssetBundleManifest> (m_manifestName, "AssetBundleManifest", delegate (UObject obj) {
            m_abManifest = obj as AssetBundleManifest;
            m_AllManifest = m_abManifest.GetAllAssetBundles ();
            if (initOK != null) initOK ();
        });
    }

    /// <summary>
    /// 添加永久存在的资源，游戏内不卸载
    /// </summary>
    /// <param name="abName"></param>
    public void AddPermanentAssetBundle(string abName) {
        if (!m_permanentAssetBundle.Contains (abName)) {
            m_permanentAssetBundle.Add (abName);
        }
    }

   public void LoadAsset<T>(string abName, string assetName, Action<UObject> func) where T: UObject {
        LoadFormAB<T> (abName, assetName, func);
    }

    private void LoadFormAB<T>(string abName, string assetName, Action<UObject> action = null) where T : UObject {
        abName = GetRealAssetPath (abName);
        if (!m_loadTime.ContainsKey (abName)) {
            m_loadTime.Add (abName, Time.realtimeSinceStartup);
        }
        LoadAssetRequest request = new LoadAssetRequest ();
        request.assetType = typeof (T);
        request.assetName = assetName;
        request.sharpFunc = action;
        List<LoadAssetRequest> requests = null;
        if (!m_LoadRequests.TryGetValue (abName, out requests)) {
            requests = new List<LoadAssetRequest> ();
            requests.Add (request);
            m_LoadRequests.Add (abName, requests);
            StartCoroutine (OnLoadAsset<T> (abName));
        } else {
            requests.Add (request);
        }
    }

    IEnumerator OnLoadAsset<T>(string abName) where T : UObject {
        LoadedAssetBundle abInfo = GetLoadedAssetBundle (abName);
        if (abInfo == null) {
            if (typeof (T) != typeof (AssetBundleManifest)) {
                List<string> load = new List<string> ();
                GetLoadAssetBundleDependencies (load, abName);
                if (load.Count == 0) {
                    m_waitRequests.Add (abName, m_LoadRequests[abName]);
                    m_LoadRequests.Remove (abName);
                    yield break;
                }
                for (int i = load.Count - 1; i >= 0; i--) {
                    yield return StartCoroutine (DownLoad (load[i], typeof (T)));
                }
            } else {
                yield return StartCoroutine (DownLoad (abName, typeof (T)));
            }
            abInfo = GetLoadedAssetBundle (abName);
            if (abInfo == null) {
                m_LoadRequests.Remove (abName);
                UnityEngine.Debug.LogError (string.Format ("加载 {0} 包失败！", abName));
                if (m_LoadFailEvent != null) {
                    m_LoadFailEvent ();
                }
                yield break;
            }
        }
        List<LoadAssetRequest> list = null;
        if (!m_LoadRequests.TryGetValue (abName, out list)) {
            m_LoadRequests.Remove (abName);
            yield break;
        }
        yield return StartCoroutine (DealAssetBundle (abInfo, abName, list));
        m_LoadRequests.Remove (abName);
    }

    public void LoadScene(string abName, Action func) {
        abName = GetRealAssetPath (abName);
        StartCoroutine (OnLoadScene (abName, func));
    }

    IEnumerator OnLoadScene(string abName, Action func) {
        LoadedAssetBundle abInfo = GetLoadedAssetBundle (abName);
        if (abInfo == null) {
            List<string> load = new List<string> ();
            GetLoadAssetBundleDependencies (load, abName);
            for (int i = load.Count - 1; i >= 0; i--) {
                yield return StartCoroutine (DownLoad (load[i]));
            }
            abInfo = GetLoadedAssetBundle (abName);
        }
        if (abInfo == null) {
            m_LoadRequests.Remove (abName);
            UnityEngine.Debug.LogError (string.Format ("加载 {0} 包失败！", abName));
            if (m_LoadFailEvent != null) {
                m_LoadFailEvent ();
            }
            yield break;
        }
        func ();
    }

    /// <summary>
    /// 处理ab包加载完之后的回调
    /// </summary>
    /// <param name="abInfo"></param>
    /// <param name="abName"></param>
    /// <param name="list"></param>
    /// <returns></returns>
    IEnumerator DealAssetBundle(LoadedAssetBundle abInfo, string abName, List<LoadAssetRequest> list) {
        for (int i = 0; i < list.Count; i++) {
            /**只请求了AB包，没有请求具体数据的情况下，直接返回空的回调*/
            if (string.IsNullOrEmpty (list[i].assetName)) {
                if (list[i].sharpFunc != null) {
                    list[i].sharpFunc (null);
                    list[i].sharpFunc = null;
                }
            } else {
                string assetPath = list[i].assetName;
                AssetBundle ab = abInfo.m_AssetBundle;
                AssetBundleRequest request = ab.LoadAssetAsync (assetPath, list[i].assetType);
                yield return request;
                if (request.asset == null) {
                    UnityEngine.Debug.LogError (string.Format ("{0} 包里不存在名字为 {1} 的物体", abName, assetPath));
                }
                if (list[i].sharpFunc != null) {
                    list[i].sharpFunc (request.asset);
                    list[i].sharpFunc = null;
                }
            }
        }
    }

    private LoadedAssetBundle GetLoadedAssetBundle(string name) {
        LoadedAssetBundle bundle = null;
        if (!m_loadAssetDic.TryGetValue (name, out bundle)) {
            return null;
        }
        string[] dependencies = null;
        if (!m_dependencies.TryGetValue (name, out dependencies)) {
            return bundle;
        }

        foreach (var dependcy in dependencies) {
            LoadedAssetBundle dependentBundle;
            m_loadAssetDic.TryGetValue (dependcy, out dependentBundle);
            if (dependentBundle == null) {
                return null;
            }
        }
        return bundle;
    }

    /// <summary>
    /// 获取ab包所有的依赖对象 
    /// </summary>
    /// <param name="loadList"></param>
    /// <param name="abName"></param>
    public void GetLoadAssetBundleDependencies(List<string> loadList, string abName) {
        if (!m_LoadingABName.Contains (abName)) {
            if (!loadList.Contains (abName)) {
                loadList.Add (abName);
            }
            m_LoadingABName.Add (abName);
        }
        string[] dependencies = m_abManifest.GetAllDependencies (abName);
        if (dependencies.Length > 0) {
            if (!m_dependencies.ContainsKey (abName)) {
                m_dependencies.Add (abName, dependencies);
            }
            for (int i = 0; i < dependencies.Length; i++) {
                string deName = dependencies[i];
                LoadedAssetBundle load_Asset = null;
                if (m_loadAssetDic.TryGetValue (deName, out load_Asset)) {
                    load_Asset.m_referenceCount++;
                    Debug.Log (string.Format ("增加 {0} 的引用，引用计数为 {1}", deName, m_loadAssetDic[deName].m_referenceCount));
                } else if (!m_LoadRequests.ContainsKey (deName)) {
                    GetLoadAssetBundleDependencies (loadList, deName);
                }
            }
        }
    }

    private IEnumerator DownLoad(string abName, Type type = null) {
        string url = PathManager.GetRunPerFilePath () + abName;
        if (!File.Exists (url)) {
            url = PathManager.GetRunAbStreamingPath (true) + abName;
        }

        UnityWebRequest www = UnityWebRequestAssetBundle.GetAssetBundle (url);
        yield return www.SendWebRequest ();
        if (www.error != null) {
            Debug.Log (string.Format ("www 失败 ：{0}", www.error));
            yield break;
        }
        AssetBundle ab = DownloadHandlerAssetBundle.GetContent (www);
        if (m_LoadingABName.Contains (ab.name)) {
            m_LoadingABName.Remove (ab.name);
        }
        if (m_loadTime.ContainsKey (ab.name)) {
            Debug.Log (string.Format ("{0} 包加载的时间为 ： {1}", ab.name, (Time.realtimeSinceStartup - m_loadTime[ab.name])));
            m_loadTime.Remove (ab.name);
        }
        if (ab != null) {
            LoadedAssetBundle abInfo = new LoadedAssetBundle (ab);
            StartWaitRequest (abInfo);
            m_loadAssetDic.Add (abName, abInfo);
        }
    }

    /// <summary>
    /// 开始处理等待队列里面的请求
    /// </summary>
    /// <param name="abName"></param>
    /// <param name="isThorough"></param>
    private void StartWaitRequest(LoadedAssetBundle ab) {
        if (m_waitRequests.Count > 0) {
            string key = ab.m_AssetBundle.name;
            if (m_waitRequests.ContainsKey (key)) {
                List<LoadAssetRequest> list = null;
                if (m_waitRequests.TryGetValue (key, out list)) {
                    StartCoroutine (DealAssetBundle (ab, key, list));
                    m_waitRequests.Remove (key);
                }
            }
        }
    }

    /// <summary>
    /// 引用ab包
    /// </summary>
    /// <param name="abName"></param>
    /// <param name="isThorough"></param>
    public void AddAssetBundleReference(string abName) {
        if (!string.IsNullOrEmpty (abName)) {
            string realPath = abName.ToLower ();
            if (m_loadAssetDic.ContainsKey (realPath)) {
                m_loadAssetDic[realPath].m_referenceCount++;
                AddDependenciesReference (realPath);
                Debug.Log (string.Format ("增加 {0} 的引用，引用计数为 {1}", realPath, m_loadAssetDic[realPath].m_referenceCount));
            }
        }
    }

    private void AddReference(string abName) {
        LoadedAssetBundle abInfo = GetLoadedAssetBundle (abName);
        if (abInfo == null) {
            return;
        }
        abInfo.m_referenceCount++;
        Debug.Log (string.Format ("增加 {0} 的引用，引用计数为 {1}", abName, m_loadAssetDic[abName].m_referenceCount));
    }

    private void AddDependenciesReference(string name) {
        string[] dependencies = null;
        if (!m_dependencies.TryGetValue (name, out dependencies)) {
            return;
        }
        foreach (var de in dependencies) {
            AddReference (de);
        }
    }

    /// <summary>
    /// 卸载的时候要判断是否是永久存在的资源
    /// </summary>
    /// <param name="abName"></param>
    /// <param name="isThorough"></param>
    public void UnloadAssetBundle(string abName, bool isThorough = false) {
        abName = GetRealAssetPath (abName);
        if (m_permanentAssetBundle.Contains (abName)) {
            return;
        }
        UnloadAssetBundleInternal (abName, isThorough);
        UnloadDependencies (abName, isThorough);
        //Debug.Log (string.Format ("减少 {0} 的引用，引用计数为 {1}", abName, m_loadAssetDic[abName].m_referenceCount));
    }

    /// <summary>
    /// 判断是否AB包已经被卸载了
    /// </summary>
    /// <param name="abName"></param>
    /// <returns></returns>
    public bool IsUnLoadAB(string abName) {
        return m_loadAssetDic.ContainsKey (abName);
    }

    private void UnloadAssetBundleInternal(string abName, bool isThorough) {
        LoadedAssetBundle abInfo = GetLoadedAssetBundle (abName);
        if (abInfo == null) {
            return;
        }
        if (--abInfo.m_referenceCount <= 0) {
            if (m_LoadRequests.ContainsKey (abName)) {
                return;
            }
            abInfo.m_AssetBundle.Unload (isThorough);
            m_loadAssetDic.Remove (abName);
            UnityEngine.Debug.Log (string.Format ("{0}引用计数为 0，释放成功 ", abName));
            Resources.UnloadUnusedAssets ();
            System.GC.Collect ();
        } else {
            Debug.Log (string.Format ("减少 {0} 的引用，引用计数为 {1}", abName, m_loadAssetDic[abName].m_referenceCount));
        }
    }

    private void UnloadDependencies(string name, bool isThorough) {
        string[] dependencies = null;
        if (!m_dependencies.TryGetValue (name, out dependencies)) {
            return;
        }
        foreach (var de in dependencies) {
            UnloadAssetBundleInternal (de, isThorough);
        }
        m_dependencies.Remove (name);
    }

    private string GetRealAssetPath(string abName) {
        if (abName.Equals (m_manifestName)) {
            return abName;
        }
        abName = abName.ToLower ();
        if (!abName.EndsWith (".unity3d")) {
            abName += ".unity3d";
        }
        if (abName.Contains ("/")) {
            return abName;
        }
        for (int i = 0; i < m_AllManifest.Length; i++) {
            int index = m_AllManifest[i].LastIndexOf ('/');
            string path = m_AllManifest[i].Remove (0, index + 1);
            if (path.Equals (abName)) {
                return m_AllManifest[i];
            }
        }
        UnityEngine.Debug.LogError (string.Format ("GetRealAssetPath Error:>>{0}", abName));
        return null;
    }

}

public class LoadedAssetBundle {
    public AssetBundle m_AssetBundle;
    /**引用计数*/
    public int m_referenceCount;

    public LoadedAssetBundle(AssetBundle assetBundle) {
        m_AssetBundle = assetBundle;
        m_referenceCount = 0;
    }

}
