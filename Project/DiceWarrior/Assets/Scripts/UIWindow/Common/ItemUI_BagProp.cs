using System;
using System.Collections;
using cfg;
using GameMain;
using SimpleJSON;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YangTools;
using YangTools.Scripts.Core.ResourceManager;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ItemUI_BagProp : MonoBehaviour
{
    public Image mImgPropBg;
    public TextMeshProUGUI mTxtPropCount;
    public Image mImgPropIcon;
    public TextMeshProUGUI mTextPropName;

    public UICustomButton clickBtn;
    public bool canClick;
    public Action clickCallBack;
    
    private ItemData_BagProp propData;
    public ItemData_BagProp PropData => propData;
    private YangEventGroup _eventGroup;
    
    public void RefreshBagPropUI(ItemData_BagProp _propData,bool isAutoSyncBag = false)
    {
        propData = _propData;
        RectTransform rect = mImgPropIcon.GetComponent<RectTransform>();
        mTxtPropCount.text = propData.PropId == 1 ? $"${_propData.PropCountString}" : $"{_propData.PropCountString}";
        if (!string.IsNullOrEmpty(_propData.Config.SpriteName))
        {
            ResourceManager.SetImageSprite(mImgPropIcon, _propData.Config.SpriteName);
        }
        else
        {
            Debug.LogWarning($"{_propData.Config.Name}没有配置Sprite");
        }

        if (mTextPropName != null)
        {
            mTextPropName.text = _propData.Config.Name;
        }

        if (isAutoSyncBag)
        {
            _eventGroup = new YangEventGroup();
            _eventGroup.AddListener<BagPropChange>(ProcessMessage);
        }
            
        if (clickBtn != null)
        {
            if (canClick)
            {
                if(clickBtn)clickBtn.TargetButton.enabled = true;
                mImgPropBg.raycastTarget = true;
                clickBtn?.AddListener(OnClick);
            }
            else
            {
                if(clickBtn)clickBtn.TargetButton.enabled = false;
                mImgPropBg.raycastTarget = false;
                clickBtn?.AddListener(null);
            }
        }
    }
    
    private void ProcessMessage(EventData message)
    {
        if (message.Args is BagPropChange)
        {
            SyncBagPropCount();
        }
    }

    public void SyncBagPropCount()
    {
        float data = BagMgr.Instance.GetBagPropCount(propData.PropId);
        propData.SetBagProp(data);
        mTxtPropCount.text = propData.PropId == 1 ? $"${propData.PropCountString}" : $"{propData.PropCountString}";
    }
    
    public void OnClick()
    {
        clickCallBack?.Invoke();
    }
}

[Serializable]
public class BagPropId
{
    [ValueDropdown("GetIdList")] [LabelText("道具Id")]
    public int bagPropId;

    public static implicit operator int(BagPropId boxItemId)
    {
        return boxItemId.bagPropId;
    }

    public static implicit operator BagPropId(int bagPropId)
    {
        return new BagPropId()
        {
            bagPropId = bagPropId
        };
    }

    public override string ToString()
    {
        return $"bagPropId={bagPropId}";
    }

#if UNITY_EDITOR
    private IEnumerable GetIdList()
    {
        var list = new ValueDropdownList<int>();

        TextAsset textAsset =
            AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/YangTools/LubanData/GenerateDatas/json/tbitemcategory.json");
        if (textAsset == null)
        {
            Debug.LogError("测试");
            return list;
        }

        var category = new TbItemCategory(JSON.Parse(textAsset.text));
        foreach (var config in category.DataList)
        {
            list.Add(new ValueDropdownItem<int>($"{config.Name}({config.Id})", config.Id));
        }

        return list;
    }
#endif
}
