using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MyAssets.Scripts.Utility
{
    /// <summary>
    /// シングルトン基底クラス
    /// </summary>
    /// <typeparam name="T">シングルトンにするクラスの型</typeparam>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        //シングルトンのインスタンスを保持するstatic変数
        private static T _instance;
        private static bool _isQuitting = false;
        private static readonly object _lock = new Object();
        
        //インスタンスにアクセスするためのプロパティ
        public static T Instance
        {
            get
            {
                if (_isQuitting)
                {
                    Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null.");
                    return null;
                }

                lock (_lock)
                {
                    //インスタンスがシーン内に存在しない場合、新しいゲームオブジェクトを作成シングルトンインスタンスを生成
                    if (_instance == null)
                    {
                        _instance = FindObjectOfType<T>();
                        if (_instance == null)
                        {
                            GameObject obj = new GameObject(typeof(T).Name);
                            _instance = obj.AddComponent<T>();
                        }
                    }
                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[Singleton] Instance of '{typeof(T)}' already exists. Destroying duplicate.");
                Destroy(this.gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _isQuitting = true;
        }
    }
}