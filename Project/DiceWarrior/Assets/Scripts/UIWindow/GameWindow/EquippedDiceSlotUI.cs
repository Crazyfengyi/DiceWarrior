using System;
using System.Collections.Generic;
using GameMain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EquippedDiceSlotUI : MonoBehaviour
{
    [SerializeField] private UICustomButton button;
    [SerializeField] private Image background;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private RectTransform faceRoot;
    [SerializeField] private List<Image> faceImages = new List<Image>();
    [SerializeField] private List<TextMeshProUGUI> faceValueTexts = new List<TextMeshProUGUI>();
    private int slotIndex;
    private Action<int> clickCallback;

    public void Bind(UICustomButton bindButton, Image bindBackground, TextMeshProUGUI bindNameText,
        RectTransform bindFaceRoot, List<Image> bindFaceImages)
    {
        button = bindButton;
        background = bindBackground;
        nameText = bindNameText;
        faceRoot = bindFaceRoot;
        faceImages = bindFaceImages;
    }

    public void Init(int index, Action<int> onClick)
    {
        slotIndex = index;
        clickCallback = onClick;

        if (button == null)
        {
            button = GetComponent<UICustomButton>();
        }

        if (button != null)
        {
            button.AddListener(OnClick);
        }

        CacheFaceImagesIfNeeded();
    }

    public void Refresh(EquippedDiceSlotData data)
    {
        bool isEmpty = data == null || data.IsEmpty;
        if (nameText != null)
        {
            nameText.text = isEmpty ? "\u7a7a" : data.Name;
        }

        if (background != null)
        {
            background.color = isEmpty
                ? new Color(0.22f, 0.34f, 0.57f, 1f)
                : new Color(0.28f, 0.45f, 0.78f, 1f);
        }

        int faceCount = isEmpty || data.Faces == null ? 0 : data.Faces.Count;
        for (int i = 0; i < faceImages.Count; i++)
        {
            bool active = i < faceCount;
            faceImages[i].gameObject.SetActive(active);
            if (active)
            {
                faceImages[i].color = new Color(0.26f, 0.43f, 0.76f, 1f);
            }

            int value = active && !isEmpty && data.Faces != null && i < data.Faces.Count ? data.Faces[i] : 0;
            RefreshFaceValueText(i, active, value);
        }
    }

    /// <summary>
    /// 刷新单个骰面格子的数值文本。
    /// </summary>
    private void RefreshFaceValueText(int index, bool active, int value)
    {
        if (index < 0 || index >= faceValueTexts.Count || faceValueTexts[index] == null)
        {
            return;
        }

        faceValueTexts[index].gameObject.SetActive(active);
        if (active)
        {
            faceValueTexts[index].text = value.ToString();
        }
    }

    /// <summary>
    /// 缓存骰面格子上的数值文本引用。
    /// </summary>
    private void CacheFaceValueTextsIfNeeded()
    {
        if (faceValueTexts != null && faceValueTexts.Count > 0)
        {
            return;
        }

        if (faceValueTexts == null)
        {
            faceValueTexts = new List<TextMeshProUGUI>();
        }
        else
        {
            faceValueTexts.Clear();
        }

        for (int i = 0; i < faceImages.Count; i++)
        {
            TextMeshProUGUI text = faceImages[i] != null
                ? faceImages[i].GetComponentInChildren<TextMeshProUGUI>(true)
                : null;
            faceValueTexts.Add(text);
        }
    }

    private void CacheFaceImagesIfNeeded()
    {
        if (faceImages != null && faceImages.Count > 0)
        {
            CacheFaceValueTextsIfNeeded();
            return;
        }

        if (faceImages == null)
        {
            faceImages = new List<Image>();
        }

        if (faceRoot == null)
        {
            Debug.LogError($"{name} faceRoot is missing.");
            return;
        }

        for (int i = 0; i < faceRoot.childCount; i++)
        {
            Image image = faceRoot.GetChild(i).GetComponent<Image>();
            if (image != null)
            {
                faceImages.Add(image);
            }
        }

        if (faceImages.Count == 0)
        {
            Debug.LogError($"{name} face images are missing on prefab.");
        }

        CacheFaceValueTextsIfNeeded();
    }

    private void OnClick()
    {
        clickCallback?.Invoke(slotIndex);
    }

}
