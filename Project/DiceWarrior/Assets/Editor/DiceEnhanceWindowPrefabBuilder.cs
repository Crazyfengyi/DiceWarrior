using System.Collections.Generic;
using GameMain;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class DiceEnhanceWindowPrefabBuilder
{
    private const string FolderPath = "Assets/AssetBundle/UIWindow/DiceEnhanceWindow";
    private const string PrefabPath = FolderPath + "/DiceEnhanceWindow.prefab";
    private const string FontPath = "Assets/AssetBundle/Fonts/wulinjianghuti SDF.asset";

    [InitializeOnLoadMethod]
    /// <summary>
    /// 编辑器加载后自动检查并补建强化页预制体。
    /// </summary>
    private static void BuildIfMissingOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            BuildIfMissing();
        };
    }

    [MenuItem("Tools/DiceWarrior/Rebuild Dice Enhance Window")]
    /// <summary>
    /// 通过菜单强制重建强化页预制体。
    /// </summary>
    public static void Rebuild()
    {
        Build(true);
    }

    /// <summary>
    /// 仅在预制体缺失或结构不完整时补建。
    /// </summary>
    private static void BuildIfMissing()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab != null && prefab.transform.Find("WindowRoot") != null)
        {
            return;
        }

        Build(false);
    }

    /// <summary>
    /// 构建或重建强化页预制体。
    /// </summary>
    private static void Build(bool rebuildExisting)
    {
        EnsureFolder(FolderPath);

        bool loadedFromPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null;
        GameObject root = loadedFromPrefab ? PrefabUtility.LoadPrefabContents(PrefabPath) : CreateRootObject();
        try
        {
            Transform windowRoot = root.transform.Find("WindowRoot");
            if (rebuildExisting)
            {
                ClearRootChildren(root.transform);
                windowRoot = null;
            }

            if (windowRoot == null)
            {
                ClearRootChildren(root.transform);
                BuildWindow(root);
            }
            else
            {
                BindExisting(root);
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (loadedFromPrefab)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            else
            {
                Object.DestroyImmediate(root);
            }
        }
    }

    /// <summary>
    /// 创建预制体根节点。
    /// </summary>
    private static GameObject CreateRootObject()
    {
        GameObject root = new GameObject("DiceEnhanceWindow", typeof(RectTransform), typeof(DiceEnhanceWindow));
        root.layer = GetUiLayer();
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return root;
    }

    /// <summary>
    /// 创建窗口基础层级并绑定字段。
    /// </summary>
    private static void BuildWindow(GameObject root)
    {
        DiceEnhanceWindow window = root.GetComponent<DiceEnhanceWindow>();
        RectTransform rootRect = root.transform as RectTransform;
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        CreatePanel("bgMask", rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0.55f), true);
        RectTransform windowRoot = CreatePanel("WindowRoot", rootRect, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1680f, 900f), new Color(0.95f, 0.95f, 0.95f, 0.98f),
            true);

        TextMeshProUGUI titleText = CreateText("Title", windowRoot, font, "\u9ab0\u5b50\u5f3a\u5316",
            new Vector2(0f, 388f), new Vector2(520f, 70f), 44f, TextAlignmentOptions.Center);
        RectTransform closeRect = CreateButton("CloseButton", windowRoot, font, "X", new Vector2(760f, 388f),
            new Vector2(72f, 56f));
        RectTransform listPanel = CreatePanel("DiceListPanel", windowRoot, new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(290f, 0f), new Vector2(620f, 700f), Color.clear, false);
        RectTransform previewPanel = CreatePanel("PreviewPanel", windowRoot, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(-110f, 50f), new Vector2(560f, 700f),
            new Color(0.28f, 0.46f, 0.79f, 0.94f), false);
        RectTransform effectPanel = CreatePanel("EffectPanel", windowRoot, new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f), new Vector2(-240f, 110f), new Vector2(420f, 260f),
            new Color(0.28f, 0.46f, 0.79f, 0.94f), false);
        RectTransform bottomBar = CreatePanel("BottomBar", windowRoot, new Vector2(1f, 0f),
            new Vector2(1f, 0f), new Vector2(-250f, 120f), new Vector2(460f, 180f), Color.clear, false);

        List<DiceEnhanceDiceItemUI> diceItems = CreateDiceItems(listPanel, font);
        TextMeshProUGUI previewNameText = CreateText("PreviewName", previewPanel, font, string.Empty,
            new Vector2(0f, 250f), new Vector2(440f, 68f), 42f, TextAlignmentOptions.Center);
        TextMeshProUGUI previewDescText = CreateText("PreviewDesc", previewPanel, font, string.Empty,
            new Vector2(0f, 172f), new Vector2(460f, 72f), 24f, TextAlignmentOptions.Center);
        TextMeshProUGUI modeHintText = CreateText("ModeHint", previewPanel, font, string.Empty,
            new Vector2(0f, 115f), new Vector2(460f, 42f), 22f, TextAlignmentOptions.Center);
        RectTransform faceRoot = CreatePanel("FaceRoot", previewPanel, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(468f, 90f), Color.clear, false);
        List<DiceEnhanceFaceItemUI> faceItems = CreateFaceItems(faceRoot, font);
        TextMeshProUGUI rangeText = CreateText("RangeText", previewPanel, font, string.Empty,
            new Vector2(0f, -80f), new Vector2(460f, 68f), 38f, TextAlignmentOptions.Center);
        TextMeshProUGUI probabilityText = CreateText("ProbabilityText", previewPanel, font, string.Empty,
            new Vector2(0f, -225f), new Vector2(460f, 200f), 26f, TextAlignmentOptions.TopLeft);

        TextMeshProUGUI effectTitleText = CreateText("EffectTitle", effectPanel, font, string.Empty,
            new Vector2(0f, 58f), new Vector2(360f, 70f), 40f, TextAlignmentOptions.Center);
        TextMeshProUGUI effectDescText = CreateText("EffectDesc", effectPanel, font, string.Empty,
            new Vector2(0f, -36f), new Vector2(340f, 90f), 24f, TextAlignmentOptions.Center);

        RectTransform skipRect = CreateButton("SkipButton", bottomBar, font, "\u4e0d\u5f3a\u5316",
            new Vector2(0f, 90f), new Vector2(220f, 84f), new Color(0.92f, 0.55f, 0.63f, 1f));
        RectTransform confirmRect = CreateButton("ConfirmButton", bottomBar, font, "\u5f3a\u5316",
            new Vector2(0f, -14f), new Vector2(260f, 92f), new Color(0.92f, 0.55f, 0.63f, 1f));

        BindWindow(window, font, titleText, effectTitleText, effectDescText, previewNameText, previewDescText,
            rangeText, probabilityText, modeHintText, diceItems, faceItems,
            closeRect.GetComponent<UICustomButton>(), skipRect.GetComponent<UICustomButton>(),
            confirmRect.GetComponent<UICustomButton>());
    }

    /// <summary>
    /// 绑定已有预制体中的控件引用。
    /// </summary>
    private static void BindExisting(GameObject root)
    {
        DiceEnhanceWindow window = root.GetComponent<DiceEnhanceWindow>();
        if (window == null)
        {
            return;
        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        List<DiceEnhanceDiceItemUI> diceItems = new List<DiceEnhanceDiceItemUI>(
            root.GetComponentsInChildren<DiceEnhanceDiceItemUI>(true));
        diceItems.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
        List<DiceEnhanceFaceItemUI> faceItems = new List<DiceEnhanceFaceItemUI>(
            root.GetComponentsInChildren<DiceEnhanceFaceItemUI>(true));
        faceItems.Sort((left, right) => string.CompareOrdinal(left.name, right.name));

        BindWindow(window, font,
            root.transform.Find("WindowRoot/Title")?.GetComponent<TextMeshProUGUI>(),
            root.transform.Find("WindowRoot/EffectPanel/EffectTitle")?.GetComponent<TextMeshProUGUI>(),
            root.transform.Find("WindowRoot/EffectPanel/EffectDesc")?.GetComponent<TextMeshProUGUI>(),
            root.transform.Find("WindowRoot/PreviewPanel/PreviewName")?.GetComponent<TextMeshProUGUI>(),
            root.transform.Find("WindowRoot/PreviewPanel/PreviewDesc")?.GetComponent<TextMeshProUGUI>(),
            root.transform.Find("WindowRoot/PreviewPanel/RangeText")?.GetComponent<TextMeshProUGUI>(),
            root.transform.Find("WindowRoot/PreviewPanel/ProbabilityText")?.GetComponent<TextMeshProUGUI>(),
            root.transform.Find("WindowRoot/PreviewPanel/ModeHint")?.GetComponent<TextMeshProUGUI>(),
            diceItems, faceItems,
            root.transform.Find("WindowRoot/CloseButton")?.GetComponent<UICustomButton>(),
            root.transform.Find("WindowRoot/BottomBar/SkipButton")?.GetComponent<UICustomButton>(),
            root.transform.Find("WindowRoot/BottomBar/ConfirmButton")?.GetComponent<UICustomButton>());
    }

    /// <summary>
    /// 创建左侧骰子列表项。
    /// </summary>
    private static List<DiceEnhanceDiceItemUI> CreateDiceItems(RectTransform parent, TMP_FontAsset font)
    {
        List<DiceEnhanceDiceItemUI> items = new List<DiceEnhanceDiceItemUI>();
        for (int i = 0; i < 4; i++)
        {
            RectTransform rect = CreatePanel($"DiceItem_{i + 1}", parent, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -i * 172f), new Vector2(600f, 150f), new Color(0.28f, 0.46f, 0.79f, 0.94f), true);
            rect.pivot = new Vector2(0f, 1f);
            rect.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            UICustomButton button = rect.gameObject.AddComponent<UICustomButton>();
            DiceEnhanceDiceItemUI item = rect.gameObject.AddComponent<DiceEnhanceDiceItemUI>();
            TextMeshProUGUI titleText = CreateText("Title", rect, font, string.Empty, new Vector2(-200f, 0f),
                new Vector2(140f, 90f), 34f, TextAlignmentOptions.Center);
            TextMeshProUGUI facesText = CreateText("Faces", rect, font, string.Empty, new Vector2(70f, 38f),
                new Vector2(360f, 46f), 30f, TextAlignmentOptions.Center);
            TextMeshProUGUI descText = CreateText("Desc", rect, font, string.Empty, new Vector2(70f, -38f),
                new Vector2(360f, 46f), 24f, TextAlignmentOptions.Center);
            item.Bind(button, rect.GetComponent<Image>(), titleText, facesText, descText);
            items.Add(item);
        }

        return items;
    }

    /// <summary>
    /// 创建中部骰面预览格子。
    /// </summary>
    private static List<DiceEnhanceFaceItemUI> CreateFaceItems(RectTransform parent, TMP_FontAsset font)
    {
        List<DiceEnhanceFaceItemUI> items = new List<DiceEnhanceFaceItemUI>();
        for (int i = 0; i < 6; i++)
        {
            RectTransform rect = CreatePanel($"Face_{i + 1}", parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(i * 78f, 0f), new Vector2(68f, 68f), new Color(0.28f, 0.46f, 0.79f, 1f), true);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
            UICustomButton button = rect.gameObject.AddComponent<UICustomButton>();
            DiceEnhanceFaceItemUI item = rect.gameObject.AddComponent<DiceEnhanceFaceItemUI>();
            TextMeshProUGUI valueText = CreateText("Value", rect, font, "0", Vector2.zero, new Vector2(54f, 54f), 34f,
                TextAlignmentOptions.Center);
            item.Bind(button, rect.GetComponent<Image>(), valueText);
            items.Add(item);
        }

        return items;
    }

    /// <summary>
    /// 将生成出来的控件引用回写到窗口脚本。
    /// </summary>
    private static void BindWindow(DiceEnhanceWindow window, TMP_FontAsset font, TextMeshProUGUI titleText,
        TextMeshProUGUI effectTitleText, TextMeshProUGUI effectDescText, TextMeshProUGUI previewNameText,
        TextMeshProUGUI previewDescText, TextMeshProUGUI rangeText, TextMeshProUGUI probabilityText,
        TextMeshProUGUI modeHintText, List<DiceEnhanceDiceItemUI> diceItems, List<DiceEnhanceFaceItemUI> faceItems,
        UICustomButton closeButton, UICustomButton skipButton, UICustomButton confirmButton)
    {
        SerializedObject serializedObject = new SerializedObject(window);
        serializedObject.FindProperty("font").objectReferenceValue = font;
        serializedObject.FindProperty("titleText").objectReferenceValue = titleText;
        serializedObject.FindProperty("effectTitleText").objectReferenceValue = effectTitleText;
        serializedObject.FindProperty("effectDescText").objectReferenceValue = effectDescText;
        serializedObject.FindProperty("previewNameText").objectReferenceValue = previewNameText;
        serializedObject.FindProperty("previewDescText").objectReferenceValue = previewDescText;
        serializedObject.FindProperty("rangeText").objectReferenceValue = rangeText;
        serializedObject.FindProperty("probabilityText").objectReferenceValue = probabilityText;
        serializedObject.FindProperty("modeHintText").objectReferenceValue = modeHintText;
        SetObjectList(serializedObject.FindProperty("diceItems"), diceItems);
        SetObjectList(serializedObject.FindProperty("faceItems"), faceItems);
        serializedObject.FindProperty("confirmButton").objectReferenceValue = confirmButton;
        serializedObject.FindProperty("skipButton").objectReferenceValue = skipButton;
        serializedObject.FindProperty("closeButton").objectReferenceValue = closeButton;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(window);
    }

    /// <summary>
    /// 创建基础面板节点。
    /// </summary>
    private static RectTransform CreatePanel(string objectName, RectTransform parent, Vector2 anchorMin,
        Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Color color, bool raycastTarget)
    {
        GameObject panelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.layer = GetUiLayer();
        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin == anchorMax ? anchorMin : new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        Image image = panelObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return rect;
    }

    /// <summary>
    /// 创建文本节点。
    /// </summary>
    private static TextMeshProUGUI CreateText(string objectName, RectTransform parent, TMP_FontAsset font, string text,
        Vector2 anchoredPosition, Vector2 sizeDelta, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.layer = GetUiLayer();
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12f;
        label.fontSizeMax = fontSize;
        label.alignment = alignment;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    /// <summary>
    /// 创建默认样式按钮。
    /// </summary>
    private static RectTransform CreateButton(string objectName, RectTransform parent, TMP_FontAsset font, string text,
        Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        return CreateButton(objectName, parent, font, text, anchoredPosition, sizeDelta,
            new Color(0.92f, 0.55f, 0.63f, 1f));
    }

    /// <summary>
    /// 创建指定样式按钮。
    /// </summary>
    private static RectTransform CreateButton(string objectName, RectTransform parent, TMP_FontAsset font, string text,
        Vector2 anchoredPosition, Vector2 sizeDelta, Color color)
    {
        RectTransform rect = CreatePanel(objectName, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            anchoredPosition, sizeDelta, color, true);
        rect.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
        rect.gameObject.AddComponent<UICustomButton>();
        CreateText("Text", rect, font, text, Vector2.zero, sizeDelta, 34f, TextAlignmentOptions.Center);
        return rect;
    }

    /// <summary>
    /// 批量设置序列化对象列表字段。
    /// </summary>
    private static void SetObjectList<T>(SerializedProperty property, List<T> objects) where T : UnityEngine.Object
    {
        property.arraySize = objects.Count;
        for (int i = 0; i < objects.Count; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
        }
    }

    /// <summary>
    /// 确保目标目录存在。
    /// </summary>
    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    /// <summary>
    /// 清理根节点下的全部旧子物体，避免重复节点残留。
    /// </summary>
    private static void ClearRootChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(root.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// 获取 UI 图层索引。
    /// </summary>
    private static int GetUiLayer()
    {
        int layer = LayerMask.NameToLayer("UI");
        return layer >= 0 ? layer : 0;
    }
}
