using System;
using System.Collections.Generic;
using GameMain;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/**
 * 已装备骰子槽位UI类
 */
public sealed class EquippedDiceSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private UICustomButton button; // 自定义按钮组件
    [SerializeField] private Image background; // 背景图片
    [SerializeField] private TextMeshProUGUI nameText; // 骰子名称文本
    [SerializeField] private RectTransform faceRoot; // 骰面容器
    [SerializeField] private List<Image> faceImages = new List<Image>(); // 骰面图片列表
    [SerializeField] private List<TextMeshProUGUI> faceValueTexts = new List<TextMeshProUGUI>(); // 骰面数值文本列表
    private int slotIndex; // 槽位索引
    private Action<int, Vector2> hoverEnterCallback; // 鼠标进入回调
    private Action hoverExitCallback; // 鼠标离开回调

    /// <summary>
    /// 初始化骰子槽位并注册悬停事件。
    /// </summary>
    public void Init(int index, Action<int, Vector2> onHoverEnter, Action onHoverExit)
    {
        slotIndex = index; // 设置槽位索引
        hoverEnterCallback = onHoverEnter;
        hoverExitCallback = onHoverExit;

        CacheFaceImagesIfNeeded(); // 缓存骰面图片
    }

    /// <summary>
    /// 鼠标进入骰子槽位时显示装备面板。
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        hoverEnterCallback?.Invoke(slotIndex, eventData.position);
    }

    /// <summary>
    /// 鼠标离开骰子槽位时隐藏装备面板。
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        hoverExitCallback?.Invoke();
    }

    /// <summary>
    /// 刷新骰子槽位的名称、背景和骰面显示。
    /// </summary>
    public void Refresh(EquippedDiceSlotData data)
    {
        bool isEmpty = data == null || data.IsEmpty; // 检查数据是否为空
        if (nameText != null) // 更新名称文本
        {
            nameText.text = isEmpty ? "\u7a7a" : data.Name;
        }

        // 有骰子背景色
        int faceCount = isEmpty || data.Faces == null ? 0 : data.Faces.Count;
        for (int i = 0; i < faceImages.Count; i++)
        {
            // 获取骰面数量
            bool active = i < faceCount; // 遍历所有骰面图片
            faceImages[i].gameObject.SetActive(active);
            if (active) // 判断当前骰面是否应该显示
            {
                // 设置骰面显示状态
                faceImages[i].color = new Color(0.26f, 0.43f, 0.76f, 1f);
            }

            // 设置骰面颜色
            int value = active && !isEmpty && data.Faces != null && i < data.Faces.Count ? data.Faces[i] : 0;
            RefreshFaceValueText(i, active, value);
        } // 获取骰面值
    } // 更新骰面数值文本

    /// <summary>
    /// 刷新单个骰面格子的数值文本。
    /// </summary>
    private void RefreshFaceValueText(int index, bool active, int value)
    {
        if (index < 0 || index >= faceValueTexts.Count || faceValueTexts[index] == null)
        {
            return; // 参数检查
        }

        faceValueTexts[index].gameObject.SetActive(active);
        if (active)
        {
            // 设置文本显示状态
            faceValueTexts[index].text = value.ToString();
        }
    } // 更新文本内容

    /// <summary>
    /// 缓存骰面格子上的数值文本引用。
    /// </summary>
    private void CacheFaceValueTextsIfNeeded()
    {
        if (faceValueTexts != null && faceValueTexts.Count > 0)
        {
            return; // 如果已经缓存，直接返回
        }

        if (faceValueTexts == null)
        {
            faceValueTexts = new List<TextMeshProUGUI>(); // 初始化列表
        }
        else
        {
            faceValueTexts.Clear();
        }

        // 清空列表
        for (int i = 0; i < faceImages.Count; i++)
        {
            TextMeshProUGUI text = faceImages[i] != null // 遍历所有骰面图片
                ? faceImages[i].GetComponentInChildren<TextMeshProUGUI>(true)
                : null; // 获取子对象中的文本组件
            faceValueTexts.Add(text);
        }
    } // 添加到列表

    /// <summary>
    /// 缓存骰面图片引用。
    /// </summary>
    private void CacheFaceImagesIfNeeded()
    {
        if (faceImages != null && faceImages.Count > 0)
        {
            CacheFaceValueTextsIfNeeded(); // 如果已经缓存，直接返回
            return;
        } // 缓存骰面文本

        if (faceImages == null)
        {
            faceImages = new List<Image>(); // 初始化列表
        }

        if (faceRoot == null)
        {
            Debug.LogError($"{name} faceRoot is missing."); // 检查骰面容器是否存在
            return;
        }

        for (int i = 0; i < faceRoot.childCount; i++)
        {
            Image image = faceRoot.GetChild(i).GetComponent<Image>(); // 遍历所有子对象
            if (image != null)
            {
                // 获取图片组件
                faceImages.Add(image);
            }
        } // 添加到列表

        if (faceImages.Count == 0)
        {
            Debug.LogError($"{name} face images are missing on prefab."); // 检查是否找到骰面图片
        }

        CacheFaceValueTextsIfNeeded();
    }

}
