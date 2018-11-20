using System;
using UnityEngine;
using Object = UnityEngine.Object;

[AutoSingleton(true)]
public class MonoSingleton<T>: MonoBehaviour  where T:Component{

    private static T _instance;
    private static bool _destoryed;
    public static T instance {
        get {
            return GetInstance();
        }
    }
    public static T GetInstance() {
        if (_instance == null && !_destoryed) {
            Type typeFromHandle = typeof(T);
            _instance = (T)FindObjectOfType(typeFromHandle);
            if (_instance == null) {
                object[] customAttributes = typeFromHandle.GetCustomAttributes(typeof(AutoSingletonAttribute), true);
                if (customAttributes.Length > 0 && !((AutoSingletonAttribute)customAttributes[0]).bAutoCreate) {
                    return default(T);
                }
                GameObject val = new GameObject(typeof(T).Name);
                _instance = val.AddComponent<T>();
            }
        }
        return _instance;
    }

    public static void DestoryInstance() {
        if (_instance != null) {
            Object.DestroyImmediate(_instance.gameObject);
        }
        _instance = null;
        _destoryed = true;
    }

    public static void ClearDestory() {
        DestoryInstance();
        _destoryed = false;
    }

    protected virtual void Awake() {
        if (_instance != null && _instance.gameObject != this.gameObject) {
            if (Application.isPlaying) {
                Destroy(this.gameObject);
            } else {
                DestroyImmediate(this.gameObject);
            }
        } else if (_instance == null) {
            _instance = this.GetComponent<T>();
        }
        if (transform.parent == null) {
            DontDestroyOnLoad(this.gameObject);
        }
        Init();
    }

    protected virtual void OnDestroy() {
        if (_instance != null && _instance.gameObject == this.gameObject) {
            _instance = null;
        }
    }

    public static bool HasInstance() {
        return _instance != null;
    }

    protected virtual void Init() {
    }
}
