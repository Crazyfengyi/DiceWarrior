using System.Collections.Generic;
using GameMain;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class GameWindowRouteHudPrefabBuilder
{
    private const string PrefabPath = "Assets/AssetBundle/UIWindow/GameWindow/GameWindow.prefab";
    private const string FontPath = "Assets/AssetBundle/Fonts/wulinjianghuti SDF.asset";

    [InitializeOnLoadMethod]
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

    [MenuItem("Tools/DiceWarrior/Rebuild GameWindow Route HUD")]
    public static void Rebuild()
    {
        Build(true);
    }

    private static void BuildIfMissing()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null || prefab.transform.Find("RouteHudRoot") != null)
        {
            return;
        }

        Build(false);
    }

    private static void Build(bool rebuildExisting)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Transform existing = root.transform.Find("RouteHudRoot");
            if (existing != null)
            {
                if (!rebuildExisting)
                {
                    BindExisting(root);
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                    return;
                }

                Object.DestroyImmediate(existing.gameObject);
            }

            BuildRouteHud(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log("GameWindow Route HUD prefab built.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void BindExisting(GameObject root)
    {
        GameWindow window = root.GetComponent<GameWindow>();
        RectTransform routeHudRoot = root.transform.Find("RouteHudRoot") as RectTransform;
        if (window == null || routeHudRoot == null)
        {
            return;
        }

        BindGameWindow(window, routeHudRoot,
            SortByName(new List<EquippedDiceSlotUI>(routeHudRoot.GetComponentsInChildren<EquippedDiceSlotUI>(true))),
            SortByName(new List<PathCardUI>(routeHudRoot.GetComponentsInChildren<PathCardUI>(true))),
            routeHudRoot.Find("DiscardPile")?.GetComponent<PileCounterUI>(),
            routeHudRoot.Find("DrawPile")?.GetComponent<PileCounterUI>(),
            routeHudRoot.Find("DiceEquipPanelRoot")?.GetComponent<DiceEquipmentPanelUI>(),
            routeHudRoot.Find("HoverTip") as RectTransform,
            routeHudRoot.Find("HoverTip/Text")?.GetComponent<TextMeshProUGUI>(),
            routeHudRoot.Find("HpBarRoot/Fill")?.GetComponent<Image>(),
            routeHudRoot.Find("HpBarRoot/HpText")?.GetComponent<TextMeshProUGUI>());
    }

    private static void BuildRouteHud(GameObject root)
    {
        GameWindow window = root.GetComponent<GameWindow>();
        RectTransform rootRect = root.transform as RectTransform;
        if (window == null || rootRect == null)
        {
            Debug.LogError("GameWindow prefab root is invalid.");
            return;
        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        RectTransform routeHudRoot = CreatePanel("RouteHudRoot", rootRect, Vector2.zero, Vector2.one, Vector2.zero,
            Vector2.zero, new Color(0.96f, 0.96f, 0.94f, 1f), false);

        RectTransform topBarRoot = CreatePanel("TopBarRoot", routeHudRoot, new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(900f, 96f), Color.clear, false);
        RectTransform equippedDiceRoot = CreatePanel("EquippedDiceRoot", routeHudRoot, new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(330f, 420f), Color.clear, false);
        RectTransform pathRoot = CreatePanel("PathRoot", routeHudRoot, new Vector2(0.5f, 0.55f),
            new Vector2(0.5f, 0.55f), new Vector2(0f, 20f), new Vector2(930f, 470f), Color.clear, false);
        RectTransform propBarRoot = CreatePanel("PropBarRoot", routeHudRoot, new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(620f, 92f), Color.clear, false);
        RectTransform hpBarRoot = CreatePanel("HpBarRoot", routeHudRoot, new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 42f), new Vector2(430f, 26f),
            new Color(0.14f, 0.2f, 0.33f, 1f), true);

        AlignExistingHudItems(window, routeHudRoot, topBarRoot, propBarRoot);

        List<EquippedDiceSlotUI> diceSlots = CreateDiceSlots(equippedDiceRoot, font);
        List<PathCardUI> pathCards = CreatePathCards(pathRoot, font);
        PileCounterUI discardPile = CreatePileCounter(routeHudRoot, font, "DiscardPile", "\u5f03\u724c\u5806",
            new Vector2(0f, 0f), new Vector2(155f, 120f), new Vector2(185f, 155f));
        PileCounterUI drawPile = CreatePileCounter(routeHudRoot, font, "DrawPile", "\u62bd\u724c\u5806",
            new Vector2(1f, 0f), new Vector2(-185f, 140f), new Vector2(235f, 120f));
        Image hpFill = CreateImage("Fill", hpBarRoot, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero,
            hpBarRoot.sizeDelta, new Color(0.28f, 0.46f, 0.79f, 1f), false);
        RectTransform hpFillRect = hpFill.transform as RectTransform;
        hpFillRect.pivot = new Vector2(0f, 0.5f);
        hpFillRect.anchorMax = new Vector2(1f, 1f);
        TextMeshProUGUI hpText = CreateText("HpText", hpBarRoot, font, string.Empty, Vector2.zero,
            new Vector2(260f, 28f), 18f, TextAlignmentOptions.Center);
        RectTransform hoverTip = CreatePanel("HoverTip", routeHudRoot, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260f, 58f),
            new Color(0.1f, 0.12f, 0.16f, 0.88f), true);
        TextMeshProUGUI hoverText = CreateText("Text", hoverTip, font, string.Empty, Vector2.zero,
            new Vector2(236f, 46f), 18f, TextAlignmentOptions.Center);
        hoverTip.gameObject.SetActive(false);
        DiceEquipmentPanelUI dicePanel = CreateDiceEquipmentPanel(routeHudRoot, font);

        BindGameWindow(window, routeHudRoot, diceSlots, pathCards, discardPile, drawPile, dicePanel, hoverTip,
            hoverText, hpFill, hpText);
        DisableOldEventCardRoot(root);
    }

    private static void AlignExistingHudItems(GameWindow window, RectTransform routeHudRoot, RectTransform topBarRoot,
        RectTransform propBarRoot)
    {
        SetRectParent(window.moneyProp != null ? window.moneyProp.transform as RectTransform : null, topBarRoot,
            new Vector2(0.5f, 0.5f), new Vector2(-130f, 0f), new Vector2(180f, 70f));
        SetRectParent(window.goldProp != null ? window.goldProp.transform as RectTransform : null, topBarRoot,
            new Vector2(0.5f, 0.5f), new Vector2(80f, 0f), new Vector2(180f, 70f));
        SetRectParent(window.setBtn != null ? window.setBtn.transform as RectTransform : null, routeHudRoot,
            new Vector2(1f, 1f), new Vector2(-78f, -46f), new Vector2(122f, 64f));

        if (window.useBagPropsBtns == null)
        {
            return;
        }

        for (int i = 0; i < window.useBagPropsBtns.Count; i++)
        {
            ItemUI_UseBagProp prop = window.useBagPropsBtns[i];
            SetRectParent(prop != null ? prop.transform as RectTransform : null, propBarRoot,
                new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 56f, 0f), new Vector2(48f, 48f));
        }
    }

    private static List<EquippedDiceSlotUI> CreateDiceSlots(RectTransform parent, TMP_FontAsset font)
    {
        List<EquippedDiceSlotUI> slots = new List<EquippedDiceSlotUI>();
        for (int i = 0; i < 4; i++)
        {
            RectTransform rect = CreatePanel($"EquippedDiceSlot_{i + 1}", parent, new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(0f, -i * 112f), new Vector2(320f, 94f),
                new Color(0.28f, 0.45f, 0.78f, 1f), true);
            Button button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            UICustomButton customButton = rect.gameObject.AddComponent<UICustomButton>();
            EquippedDiceSlotUI slot = rect.gameObject.AddComponent<EquippedDiceSlotUI>();
            TextMeshProUGUI nameText = CreateText("Name", rect, font, string.Empty, new Vector2(48f, -47f),
                new Vector2(84f, 60f), 30f, TextAlignmentOptions.Center);
            RectTransform faceRoot = CreatePanel("FaceRoot", rect, new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(124f, 0f), new Vector2(186f, 76f), Color.clear, false);
            List<Image> faceImages = new List<Image>();
            for (int faceIndex = 0; faceIndex < 6; faceIndex++)
            {
                Image face = CreateImage($"Face_{faceIndex + 1}", faceRoot, new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(faceIndex * 32f, 0f), new Vector2(24f, 24f),
                    new Color(0.26f, 0.43f, 0.76f, 1f), false);
                faceImages.Add(face);
            }

            slot.Bind(customButton, rect.GetComponent<Image>(), nameText, faceRoot, faceImages);
            slots.Add(slot);
        }

        return slots;
    }

    private static List<PathCardUI> CreatePathCards(RectTransform parent, TMP_FontAsset font)
    {
        List<PathCardUI> cards = new List<PathCardUI>();
        for (int i = 0; i < 3; i++)
        {
            RectTransform rect = CreatePanel($"PathCard_{i + 1}", parent, new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 290f, -30f), new Vector2(250f, 340f),
                new Color(0.28f, 0.46f, 0.79f, 1f), true);
            Button button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            UICustomButton customButton = rect.gameObject.AddComponent<UICustomButton>();
            PathCardUI card = rect.gameObject.AddComponent<PathCardUI>();
            TextMeshProUGUI pathTitle = CreateText("PathTitle", rect, font, $"\u8def\u5f84{i + 1}",
                new Vector2(0f, 205f), new Vector2(180f, 50f), 30f, TextAlignmentOptions.Center);
            TextMeshProUGUI cardName = CreateText("CardName", rect, font, string.Empty, new Vector2(0f, 110f),
                new Vector2(210f, 46f), 25f, TextAlignmentOptions.Center);
            TextMeshProUGUI typeText = CreateText("Type", rect, font, string.Empty, new Vector2(0f, 68f),
                new Vector2(210f, 36f), 20f, TextAlignmentOptions.Center);
            TextMeshProUGUI descText = CreateText("Desc", rect, font, string.Empty, new Vector2(0f, -40f),
                new Vector2(210f, 138f), 18f, TextAlignmentOptions.Center);
            Image icon = CreateImage("Icon", rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 28f), new Vector2(54f, 54f), new Color(1f, 1f, 1f, 0.25f), false);
            card.Bind(customButton, rect.GetComponent<Image>(), icon, pathTitle, cardName, typeText, descText);
            cards.Add(card);
        }

        return cards;
    }

    private static PileCounterUI CreatePileCounter(RectTransform parent, TMP_FontAsset font, string objectName,
        string title, Vector2 anchor, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreatePanel(objectName, parent, anchor, anchor, position, size,
            new Color(0.27f, 0.45f, 0.78f, 1f), true);
        Button button = rect.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        UICustomButton customButton = rect.gameObject.AddComponent<UICustomButton>();
        PileCounterUI pile = rect.gameObject.AddComponent<PileCounterUI>();
        TextMeshProUGUI titleText = CreateText("Title", rect, font, title, new Vector2(0f, size.y * 0.5f + 28f),
            new Vector2(180f, 42f), 26f, TextAlignmentOptions.Center);
        TextMeshProUGUI countText = CreateText("Count", rect, font, "0", Vector2.zero, new Vector2(160f, 56f),
            30f, TextAlignmentOptions.Center);
        pile.Bind(customButton, titleText, countText, rect.GetComponent<Image>());
        return pile;
    }

    private static DiceEquipmentPanelUI CreateDiceEquipmentPanel(RectTransform parent, TMP_FontAsset font)
    {
        RectTransform rect = CreatePanel("DiceEquipPanelRoot", parent, new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(420f, 300f),
            new Color(0.08f, 0.1f, 0.14f, 0.94f), true);
        DiceEquipmentPanelUI panel = rect.gameObject.AddComponent<DiceEquipmentPanelUI>();
        TextMeshProUGUI titleText = CreateText("Title", rect, font, string.Empty, new Vector2(0f, 112f),
            new Vector2(360f, 42f), 28f, TextAlignmentOptions.Center);
        TextMeshProUGUI descText = CreateText("Desc", rect, font, string.Empty, new Vector2(0f, 58f),
            new Vector2(360f, 54f), 20f, TextAlignmentOptions.Center);
        TextMeshProUGUI listText = CreateText("List", rect, font, string.Empty, new Vector2(0f, -28f),
            new Vector2(360f, 112f), 20f, TextAlignmentOptions.Left);
        UICustomButton closeButton = CreateButton("CloseButton", rect, font, "\u5173\u95ed", new Vector2(0f, -120f),
            new Vector2(130f, 42f));
        panel.Bind(closeButton, titleText, descText, listText);
        rect.gameObject.SetActive(false);
        return panel;
    }

    private static RectTransform CreatePanel(string objectName, RectTransform parent, Vector2 anchorMin,
        Vector2 anchorMax, Vector2 position, Vector2 size, Color color, bool raycastTarget)
    {
        GameObject panelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.layer = GetUiLayer();
        RectTransform rect = panelObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin == anchorMax ? anchorMin : new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = panelObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;
        return rect;
    }

    private static Image CreateImage(string objectName, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size, Color color, bool raycastTarget)
    {
        RectTransform rect = CreatePanel(objectName, parent, anchorMin, anchorMax, position, size, color, raycastTarget);
        return rect.GetComponent<Image>();
    }

    private static TextMeshProUGUI CreateText(string objectName, RectTransform parent, TMP_FontAsset font, string text,
        Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.layer = GetUiLayer();
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

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

    private static UICustomButton CreateButton(string objectName, RectTransform parent, TMP_FontAsset font, string text,
        Vector2 position, Vector2 size)
    {
        RectTransform rect = CreatePanel(objectName, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            position, size, new Color(0.28f, 0.45f, 0.78f, 1f), true);
        Button button = rect.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        UICustomButton customButton = rect.gameObject.AddComponent<UICustomButton>();
        CreateText("Text", rect, font, text, Vector2.zero, size, 22f, TextAlignmentOptions.Center);
        return customButton;
    }

    private static void SetRectParent(RectTransform rect, RectTransform parent, Vector2 anchor, Vector2 position,
        Vector2 size)
    {
        if (rect == null)
        {
            return;
        }

        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void BindGameWindow(GameWindow window, RectTransform routeHudRoot,
        List<EquippedDiceSlotUI> diceSlots, List<PathCardUI> pathCards, PileCounterUI discardPile,
        PileCounterUI drawPile, DiceEquipmentPanelUI dicePanel, RectTransform hoverTip,
        TextMeshProUGUI hoverText, Image hpFill, TextMeshProUGUI hpText)
    {
        SerializedObject serializedWindow = new SerializedObject(window);
        serializedWindow.FindProperty("routeHudRoot").objectReferenceValue = routeHudRoot;
        SetObjectList(serializedWindow.FindProperty("equippedDiceSlotItems"), diceSlots);
        SetObjectList(serializedWindow.FindProperty("pathCardItems"), pathCards);
        serializedWindow.FindProperty("discardPileUI").objectReferenceValue = discardPile;
        serializedWindow.FindProperty("drawPileUI").objectReferenceValue = drawPile;
        serializedWindow.FindProperty("diceEquipmentPanelUI").objectReferenceValue = dicePanel;
        serializedWindow.FindProperty("hoverTipRoot").objectReferenceValue = hoverTip;
        serializedWindow.FindProperty("hoverTipText").objectReferenceValue = hoverText;
        serializedWindow.FindProperty("hpFillImage").objectReferenceValue = hpFill;
        serializedWindow.FindProperty("hpText").objectReferenceValue = hpText;
        serializedWindow.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(window);
    }

    private static void SetObjectList<T>(SerializedProperty property, List<T> objects) where T : UnityEngine.Object
    {
        property.arraySize = objects.Count;
        for (int i = 0; i < objects.Count; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i];
        }
    }

    private static void DisableOldEventCardRoot(GameObject root)
    {
        Transform eventCardRoot = FindChildRecursive(root.transform, "EventCardRoot");
        if (eventCardRoot != null)
        {
            eventCardRoot.gameObject.SetActive(false);
        }
    }

    private static List<T> SortByName<T>(List<T> items) where T : Component
    {
        items.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
        return items;
    }

    private static Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform found = FindChildRecursive(child, childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static int GetUiLayer()
    {
        int layer = LayerMask.NameToLayer("UI");
        return layer >= 0 ? layer : 0;
    }
}
