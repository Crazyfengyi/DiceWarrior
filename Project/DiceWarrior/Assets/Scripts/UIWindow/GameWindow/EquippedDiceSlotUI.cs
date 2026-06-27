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
        }
    }

    private void CacheFaceImagesIfNeeded()
    {
        if (faceImages != null && faceImages.Count > 0)
        {
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

        faceImages.AddRange(faceRoot.GetComponentsInChildren<Image>(true));
        if (faceImages.Count == 0)
        {
            Debug.LogError($"{name} face images are missing on prefab.");
        }
    }

    private void OnClick()
    {
        clickCallback?.Invoke(slotIndex);
    }

}
