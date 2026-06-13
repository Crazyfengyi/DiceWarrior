/*
 *Copyright(C) 2020 by Test
 *All rights reserved.
 *Author:DESKTOP-JVG8VG4
 *UnityVersion：6000.0.17f1c1
 *创建时间:2025-05-26
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using cfg;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YangTools;
using YangTools.Scripts.Core;

namespace YangTools.Scripts.Core.YangSaveData
{
    public class YangSaveDataManager : MonoSingleton<YangSaveDataManager>
    {
        private DataCenter dataCenter;
        public DataCenter DataCenter => dataCenter;

        private const string PLAYER_LOCAL_SAVE_DATA_KEY = "PlayerLocalSaveData";

        public void OnEnable()
        {
            string saveData = PlayerPrefs.GetString(PLAYER_LOCAL_SAVE_DATA_KEY);
            if (string.IsNullOrEmpty(saveData) == false)
            {
                dataCenter = JsonUtility.FromJson<DataCenter>(saveData);
                dataCenter.LoadLocalDataed();
                Debug.Log($"加载玩家本地数据:{saveData}");
            }
            else
            {
                dataCenter = new DataCenter();
                dataCenter.Initialize();
                Debug.Log($"创建玩家本地数据");
            }
        }

        protected override void OnDestroy()
        {
            SaveLocalData(true);
        }

        private readonly float intervalTime = 15f;
        private float time;

        public void Update()
        {
            time += Time.unscaledDeltaTime;
            if (time >= intervalTime)
            {
                time = 0;
                if (dataCenter != null)
                {
                    SaveLocalData(true);
                }
            }

            if (dataCenter != null && dataCenter.DirtyKey.Count > 0)
            {
                SaveLocalData(false);
            }
        }

        /// <summary>
        /// 保存本地数据 
        /// </summary>
        public void SaveLocalData(bool force)
        {
            if (dataCenter != null)
            {
                dataCenter.SaveDirtyData(force);
                string saveData = JsonUtility.ToJson(dataCenter, true);
                PlayerPrefs.SetString(PLAYER_LOCAL_SAVE_DATA_KEY, saveData);
                PlayerPrefs.Save();

#if UNITY_EDITOR
                string saveLocalFile = $"{Application.persistentDataPath}/{PLAYER_LOCAL_SAVE_DATA_KEY}";
                if (!Directory.Exists(saveLocalFile))
                {
                    Directory.CreateDirectory(saveLocalFile);
                }

                string filePath = $"{saveLocalFile}/PLAYER_LOCAL_SAVE_DATA.json";
                if (!File.Exists(filePath))
                {
                    File.Create(filePath).Close();
                }

                File.WriteAllText(filePath, saveData);
#endif
            }
        }

        /// <summary>
        /// 清空存档
        /// </summary>
        public async UniTask ClearSaveData()
        {
            dataCenter = new DataCenter();
            dataCenter.Initialize();
            SaveLocalData(true);
        }
    }

    [Serializable]
    public class DataCenter
    {
        //分块保存数据
        public List<LocalSaveData> localSaves;
        public HashSet<string> DirtyKey;

        public DataCenter()
        {
        }

        public void Initialize()
        {
            localSaves = new();
            DirtyKey = new();
        }

        /// <summary>
        /// 加载本地数据结束
        /// </summary>
        public void LoadLocalDataed()
        {
            DirtyKey = new();
        }

        /// <summary>
        /// 获得本地数据
        /// </summary>
        public T GetLocalSave<T>(bool isDirty = false) where T : ISaveData, new()
        {
            var saveTypeKey = typeof(T).Name;
            if (isDirty) DirtyKey.Add(saveTypeKey);

            foreach (var item in localSaves)
            {
                if (item.saveKey.Equals(saveTypeKey))
                {
                    if (item.saveData == null)
                    {
                        item.Deserialize<T>();
                    }

                    if (item.saveData is T t)
                    {
                        return t;
                    }

                    Debug.LogError($"数据错误:{saveTypeKey} = null");
                    break;
                }
            }

            var newSave = new T();
            //全局表数据
            var globalData = GameTableManager.Instance.Tables.GlobalConfigCategory;
            newSave.SetDefaultData(globalData);
            var newCell = new LocalSaveData(saveTypeKey, newSave);
            localSaves.Add(newCell);
            Debug.Log($"添加本地数据:{newCell.saveKey}");
            return newSave;
        }

        /// <summary>
        /// 设置脏标记
        /// </summary>
        public void SaveDirtyData(bool force)
        {
            if (force)
            {
                foreach (var localSave in localSaves)
                {
                    localSave.Serialize();
                }
            }
            else if (DirtyKey.Count > 0)
            {
                foreach (var dirtyKey in DirtyKey)
                {
                    foreach (var localSave in localSaves)
                    {
                        if (localSave.saveKey.Equals(dirtyKey))
                        {
                            localSave.Serialize();
                            break;
                        }
                    }
                }

                DirtyKey.Clear();
            }
        }
    }

    [Serializable]
    public class LocalSaveData
    {
        public string saveKey;
        public string saveJson;
        [NonSerialized] public ISaveData saveData;

        public LocalSaveData(string saveName, ISaveData _saveData)
        {
            saveKey = saveName;
            saveData = _saveData;
        }

        public void Serialize()
        {
            if (saveData != null) saveJson = JsonUtility.ToJson(saveData);
        }

        public void Deserialize<T>() where T : ISaveData, new()
        {
            if (!string.IsNullOrEmpty(saveJson))
            {
                saveData = JsonUtility.FromJson<T>(saveJson);
                saveData.OnAfterDeserialize();
            }
        }
    }

    [Serializable]
    public abstract class ISaveData
    {
        public abstract void SetDefaultData(GlobalConfigCategory tableData);

        public virtual void OnAfterDeserialize()
        {
        }
    }

    /// <summary>
    /// 游戏设置
    /// </summary>
    public class Save_GameSet : ISaveData
    {
        /// <summary>
        /// 音乐开关
        /// </summary>
        public float musicValue;
        /// <summary>
        /// 音效开关
        /// </summary>
        public float soundValue;
        /// <summary>
        /// 震动
        /// </summary>
        public bool isOnShake;
        
        public override void SetDefaultData(GlobalConfigCategory tableData)
        {
            musicValue = 1;
            soundValue = 1;
            isOnShake = true;
        }
    }
    
    /// <summary>
    /// 游戏信息存储
    /// </summary>
    public class Save_GameData : ISaveData
    {
        public const int DefaultLevelId = 1001;

        /// <summary>
        /// 是否首次进入
        /// </summary>
        public bool isFirstEnter;

        /// <summary>
        /// 当前游戏关卡表ID
        /// </summary>
        public int currentLevelId;

        public int lastSelectPayPassIndex;

        /// <summary>
        /// 用户提现账号
        /// </summary>
        public string accountId;

        public override void SetDefaultData(GlobalConfigCategory tableData)
        {
            isFirstEnter = true;
            currentLevelId = DefaultLevelId;
            accountId = string.Empty;
        }

        public override void OnAfterDeserialize()
        {
            accountId ??= string.Empty;
        }
    }
    
    /// <summary>
    /// 背包数据
    /// </summary>
    public class Save_BagProp : ISaveData
    {
        public List<SaveBagPropItem> savePropList;

        public override void SetDefaultData(GlobalConfigCategory category)
        {
            savePropList = new List<SaveBagPropItem>();

            foreach (var item in category.InitBagProps)
            {
                AddBagPropItem(item.Key, item.Value);
            }
        }

        private SaveBagPropItem SafeGetBagPropItem(int propId)
        {
            for (int i = 0; i < savePropList.Count; i++)
            {
                if (savePropList[i].bagPropId == propId)
                {
                    return savePropList[i];
                }
            }

            var newBagProp = new SaveBagPropItem() {
                bagPropId = propId,
                bagPropCount = 0
            };
            
            savePropList.Add(newBagProp);
            return newBagProp;
        }

        private void RemoveBagPropItem(int propId)
        {
            for (int i = 0; i < savePropList.Count; i++)
            {
                if (savePropList[i].bagPropId == propId)
                {
                    savePropList.RemoveAt(i);
                    break;
                }
            }
        }

        public void AddBagPropItem(int propId, float addCount)
        {
            var bagPropItem = SafeGetBagPropItem(propId);
            bagPropItem.bagPropCount += addCount;
        }

        public void SetBagPropItem(int propId, float propCount)
        {
            var bagPropItem = SafeGetBagPropItem(propId);
            bagPropItem.bagPropCount = propCount;
        }

        public void RemoveBagPropItem(int propId, float removeCount)
        {
            var bagPropItem = SafeGetBagPropItem(propId);

            if (bagPropItem.bagPropCount < removeCount)
            {
                Debug.LogError($"道具不足不能移除:{bagPropItem} 移除道具:{removeCount}");
            }

            bagPropItem.bagPropCount -= removeCount;
            if (bagPropItem.bagPropCount <= 0)
            {
                RemoveBagPropItem(bagPropItem.bagPropId);
            }
        }
    }
    
    [System.Serializable]
    public class SaveBagPropItem
    {
        public int bagPropId;
        public float bagPropCount;
        public override string ToString()
        {
            return $"背包,道具Id:{bagPropId} 道具数量:{bagPropCount}";
        }
    }

    /// <summary>
    /// 道具每日使用次数
    /// </summary>
    public class Save_PropDailyUse : ISaveData
    {
        public const int DefaultDailyLimit = 3;

        public List<SavePropDailyUseItem> savePropUseList;

        public override void SetDefaultData(GlobalConfigCategory tableData)
        {
            savePropUseList = new List<SavePropDailyUseItem>();
        }

        public override void OnAfterDeserialize()
        {
            savePropUseList ??= new List<SavePropDailyUseItem>();
        }

        public bool CanUse(int propId, int limit = DefaultDailyLimit)
        {
            return GetRemainCount(propId, limit) > 0;
        }

        public void RecordUse(int propId)
        {
            SavePropDailyUseItem item = SafeGetUseItem(propId);
            item.usedCount++;
        }
        /// <summary>
        /// 获取剩余数量
        /// </summary>
        public int GetRemainCount(int propId, int limit = DefaultDailyLimit)
        {
            SavePropDailyUseItem item = SafeGetUseItem(propId);
            return Mathf.Max(0, limit - item.usedCount);
        }

        private SavePropDailyUseItem SafeGetUseItem(int propId)
        {
            savePropUseList ??= new List<SavePropDailyUseItem>();
            string today = DateTime.Now.ToString("yyyyMMdd");

            for (int i = 0; i < savePropUseList.Count; i++)
            {
                if (savePropUseList[i].propId != propId)
                {
                    continue;
                }

                if (savePropUseList[i].dateKey != today)
                {
                    savePropUseList[i].dateKey = today;
                    savePropUseList[i].usedCount = 0;
                }

                return savePropUseList[i];
            }

            SavePropDailyUseItem newItem = new SavePropDailyUseItem
            {
                propId = propId,
                dateKey = today,
                usedCount = 0
            };
            savePropUseList.Add(newItem);
            return newItem;
        }
    }

    [Serializable]
    public class SavePropDailyUseItem
    {
        public int propId;
        public string dateKey;
        public int usedCount;
    }
}
