/*
 *Copyright(C) 2020 by DefaultCompany
 *All rights reserved.
 *Author:       DESKTOP-AJS8G4U
 *UnityVersion：2021.2.1f1c1
 *创建时间:         2021-12-23
 */

using UnityEngine;

namespace YangTools.Scripts.Core
{
    /// <summary>
    /// 饿汉模式-单例模板
    /// 这种单例模式在程序启动时就创建实例，适用于实例创建成本不高且确定会使用的场景
    /// </summary>
    public abstract class Singleton<T> where T : class, new()
    {
        // 静态只读属性，确保实例在程序运行期间只被创建一次
        public static T Instance { get; } = new T();
    }

    /// <summary>
    /// 简单的线程安全单例模板
    /// 这种单例模式使用双重锁定机制确保线程安全，并在第一次使用时才创建实例
    /// </summary>
    public abstract class SimpleSingleton<T> where T : class, new()
    {
        // 静态变量，用于存储单例实例
        private static T instance = default(T);

        //线程锁对象，确保在多线程环境下的安全性
        private static readonly object obj = new object();

        // 静态属性，用于获取单例实例
        public T Instance
        {
            get
            {
                // 使用lock确保线程安全
                lock (obj)
                {
                    // 如果实例不存在，则创建新实例
                    if (instance == null)
                    {
                        instance = new T();
                        //支持非公共的无参构造函数，new T()不支持非公共的无参构造函数
                        //instance = (T)Activator.CreateInstance(typeof(T), true);
                    }

                    return instance;
                }
            }
        }
    }

    /// <summary>
    /// Mono单例模板
    /// </summary>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T instance = null;
        private static readonly object locker = new object();
        private static bool isInstanceDestory;
        public static bool IsInit => instance = null;
        public static T Instance
        {
            get
            {
                if (isInstanceDestory)
                {
                    instance = null;
                    return instance;
                }

                lock (locker)
                {
                    if (instance == null)
                    {
                        instance = FindFirstObjectByType<T>();
                        if (FindObjectsByType<T>(FindObjectsSortMode.InstanceID).Length > 1)
                        {
                            Debug.LogError($"不应该存在多个{typeof(T)}单例！");
                            return instance;
                        }

                        if (instance == null)
                        {
                            Debug.Log($"初始化{typeof(T)}");
                            GameObject singleton = new GameObject();
                            instance = singleton.AddComponent<T>();
                            singleton.name = typeof(T) + "(Singleton)";
                            singleton.hideFlags = HideFlags.None;
                            DontDestroyOnLoad(singleton);
                        }
                        else
                        {
                            if (Application.isPlaying)
                            {
                                instance.hideFlags = HideFlags.None;
                                DontDestroyOnLoad(instance.gameObject);
                            }
                        }
                    }

                    return instance;
                }
            }
        }

        protected virtual void Awake()
        {
            isInstanceDestory = false;
        }

        protected virtual void OnDestroy()
        {
            isInstanceDestory = true;
        }
    }

    /// <summary>
    /// Mono注册性单例
    /// </summary>
    public static class MonoSingletonRegister<T> where T : MonoBehaviour
    {
        private static T instance;
        private static readonly object locker = new object();

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (locker)
                    {
                        if (instance == null)
                        {
                            GameObject gameObject = new GameObject(typeof(T).Name + "(SingletonRegister)");
                            Object.DontDestroyOnLoad(gameObject);
                            instance = gameObject.AddComponent<T>();
                        }
                    }
                }

                return instance;
            }
        }
    }
    
   /*
   *public class Example : MonoBehaviour
    {
        public static Example Instance => MonoSingletonRegister<Example>.Instance;
    }
   * 
   */
}