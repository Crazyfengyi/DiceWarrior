using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameMain;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YangTools;
using YangTools.Scripts.Core.YangSaveData;
using YangTools.Scripts.Core.YangUGUI;

/**
 * 主窗口卡片类，继承自UGUIPanelBase，实现了拖拽相关接口
 * 用于管理主界面中的卡片菜单，支持旋转选择和点击交互
 */
public sealed class CardMainWindow : UGUIPanelBase<DefaultUGUIDataBase>, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // 卡片数量常量
    private const int CardCount = 5;
    // 每张卡片之间的角度间隔
    private const float AngleStep = 360f / CardCount;
    // 卡片在X轴上的旋转半径
    private const float RadiusX = 430f;
    // 卡片在Y轴上的旋转半径
    private const float RadiusY = 48f;
    // 每屏幕像素对应的角度变化
    private const float DragAnglePerScreen = 180f;
    // 卡片旋转动画持续时间
    private const float SnapDuration = 0.25f;
    // 游戏开始的关卡ID
    private const int StartLevelId = 1;

    // 中间窗口组类型
    private static readonly GroupType MiddleGroup = (GroupType) Enum.Parse(typeof(GroupType), "\u4e2d\u95f4");
    // 弹出窗口2组类型
    private static readonly GroupType Popup2Group = (GroupType) Enum.Parse(typeof(GroupType), "\u5f39\u7a972");

    // 卡片标题数组
    private static readonly string[] CardTitles =
    {
        "\u7ee7\u7eed\u6e38\u620f",  // 继续游戏
        "\u5f00\u59cb\u6e38\u620f",  // 开始游戏
        "\u8d44\u6599",              // 资料
        "\u8bbe\u7f6e",              // 设置
        "\u9000\u51fa"               // 退出
    };



    // 序列化引用的UI元素
    [SerializeField] private RectTransform windowRoot;  // 窗口根节点
    [SerializeField] private RectTransform cardRoot;    // 卡片根节点
    [SerializeField] private Button leftArrow;          // 左箭头按钮
    [SerializeField] private Button rightArrow;         // 右箭头按钮
    [SerializeField] private List<CardMainMenuItem> cardItems = new List<CardMainMenuItem>(); // 卡片菜单项列表

    // 当前旋转角度
    private float currentAngle;
    // 拖拽开始时的角度
    private float dragStartAngle;
    // 拖拽开始时的X坐标
    private float dragStartX;
    // 当前选中的卡片索引
    private int selectedIndex;
    // 旋转动画序列
    private Sequence rotateSequence;
    // 是否正在执行打开动作
    private bool actionOpening;

    /**
     * 窗口打开时的回调函数
     * 初始化UI、注册事件、重置状态并刷新卡片
     */
    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        EnsureUI();
        RegisterEvents();
        actionOpening = false;
        selectedIndex = 0;
        currentAngle = 0f;
        RefreshCards();
    }

    /**
     * 窗口关闭时的回调函数
     * 停止旋转动画并调用基类的关闭方法
     */
    public override void OnClose(bool isShutdown, object userData)
    {
        KillRotate();
        base.OnClose(isShutdown, userData);
    }

    /**
     * 拖拽开始时的回调函数
     * 停止当前旋转动画，记录拖拽起始角度和X坐标
     */
    public void OnBeginDrag(PointerEventData eventData)
    {
        KillRotate();
        dragStartAngle = currentAngle;
        dragStartX = eventData.position.x;
    }

    /**
     * 拖拽过程中的回调函数
     * 根据拖拽距离计算新的旋转角度并刷新卡片
     */
    public void OnDrag(PointerEventData eventData)
    {
        float screenWidth = Mathf.Max(1f, Screen.width);
        float dragOffset = eventData.position.x - dragStartX;
        currentAngle = dragStartAngle - dragOffset / screenWidth * DragAnglePerScreen;
        RefreshCards();
    }

    /**
     * 拖拽结束时的回调函数
     * 将卡片吸附到最近的位置
     */
    public void OnEndDrag(PointerEventData eventData)
    {
        SnapToNearest();
    }

    /**
     * 注册事件处理函数
     * 为左右箭头按钮和卡片按钮添加点击事件
     */
    private void RegisterEvents()
    {
        if (leftArrow != null)
        {
            leftArrow.onClick.RemoveListener(RotateLeft);
            leftArrow.onClick.AddListener(RotateLeft);
        }

        if (rightArrow != null)
        {
            rightArrow.onClick.RemoveListener(RotateRight);
            rightArrow.onClick.AddListener(RotateRight);
        }

        for (int i = 0; i < cardItems.Count; i++)
        {
            int index = i;
            Button button = cardItems[i] != null ? cardItems[i].Button : null;
            if (button == null)
            {
                continue;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnCardClicked(index));
        }
    }

    /**
     * 向左旋转卡片
     */
    private void RotateLeft()
    {
        AnimateToIndex(selectedIndex - 1);
    }

    /**
     * 向右旋转卡片
     */
    private void RotateRight()
    {
        AnimateToIndex(selectedIndex + 1);
    }

    /**
     * 将卡片吸附到最近的位置
     */
    private void SnapToNearest()
    {
        int index = Mathf.RoundToInt(-currentAngle / AngleStep);
        AnimateToIndex(index);
    }

    /**
     * 动画旋转到指定索引的卡片
     * @param index 目标卡片索引
     */
    private void AnimateToIndex(int index)
    {
        selectedIndex = WrapIndex(index);
        float targetAngle = -index * AngleStep;
        KillRotate();
        rotateSequence = DOTween.Sequence()
            .Append(DOTween.To(() => currentAngle, value =>
            {
                currentAngle = value;
                RefreshCards();
            }, targetAngle, SnapDuration).SetEase(Ease.OutCubic))
            .SetTarget(this);
    }

    /**
     * 卡片点击处理函数
     * 根据点击的卡片执行相应的操作
     */
    private async void OnCardClicked(int index)
    {
        if (actionOpening)
        {
            return;
        }

        if (index != selectedIndex)
        {
            AnimateToIndex(index);
            return;
        }

        switch (index)
        {
            case 0:  // 继续游戏
                actionOpening = true;
                await OpenGameWindow();
                break;
            case 1:  // 开始游戏
                actionOpening = true;
                ResetSaveLevel();
                await OpenGameWindow();
                break;
            case 2:  // 资料
                FloatTipWindow.Show("\u8d44\u6599\u5e93\u6682\u672a\u5f00\u653e");
                break;
            case 3:  // 设置
                actionOpening = true;
                await UIMonoInstance.OpenPanel<SettingWindow>(Popup2Group);
                actionOpening = false;
                break;
            case 4:  // 退出
                QuitGame();
                break;
        }
    }

    /**
     * 打开游戏窗口
     */
    private async UniTask OpenGameWindow()
    {
        CloseSelfPanel();
        await UIMonoInstance.OpenPanel<GameWindow>(MiddleGroup);
    }

    /**
     * 重置保存的游戏关卡
     */
    private static void ResetSaveLevel()
    {
        Save_GameData gameData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameData>(true);
        gameData.currentLevelId = StartLevelId;
    }

    /**
     * 退出游戏
     * 在编辑器模式下显示提示，实际构建时退出游戏
     */
    private static void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("Quit game requested from CardMainWindow.");
        FloatTipWindow.Show("\u7f16\u8f91\u5668\u4e2d\u4e0d\u9000\u51fa\u6e38\u620f");
#else
        Application.Quit();
#endif
    }

    /**
     * 刷新卡片显示
     * 根据当前旋转角度更新每张卡片的位置、缩放和透明度
     */
    private void RefreshCards()
    {
        int frontIndex = WrapIndex(Mathf.RoundToInt(-currentAngle / AngleStep));
        for (int i = 0; i < cardItems.Count; i++)
        {
            float angle = currentAngle + i * AngleStep;
            float radians = angle * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            float frontAmount = Mathf.InverseLerp(-1f, 1f, cos);
            float scale = Mathf.Lerp(0.62f, 1.15f, frontAmount);
            float alpha = Mathf.Lerp(0.38f, 1f, frontAmount);
            float y = Mathf.Lerp(-RadiusY, RadiusY, frontAmount);
            int siblingOrder = Mathf.RoundToInt(frontAmount * 100f);

            cardItems[i].Refresh(CardTitles[i], i == frontIndex);
            cardItems[i].ApplyPose(new Vector2(sin * RadiusX, y), scale, alpha, -sin * 38f, siblingOrder);
        }
    }

    /**
     * 确保UI元素正确初始化
     * 设置窗口根节点、卡片根节点、箭头和卡片
     */
    private void EnsureUI()
    {
        RectTransform rootRect = transform as RectTransform;
        if (rootRect != null)
        {
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
        }

        Image raycastImage = GetComponent<Image>();
        if (raycastImage == null)
        {
            raycastImage = gameObject.AddComponent<Image>();
        }

        raycastImage.color = new Color(0.05f, 0.07f, 0.12f, 1f);
        raycastImage.raycastTarget = true;

        if (windowRoot == null)
        {
            windowRoot = CreateRect("WindowRoot", transform as RectTransform);
            windowRoot.anchorMin = Vector2.zero;
            windowRoot.anchorMax = Vector2.one;
            windowRoot.offsetMin = Vector2.zero;
            windowRoot.offsetMax = Vector2.zero;
        }

        if (cardRoot == null)
        {
            cardRoot = CreateRect("CardRoot", windowRoot);
            cardRoot.anchorMin = cardRoot.anchorMax = new Vector2(0.5f, 0.52f);
            cardRoot.sizeDelta = new Vector2(980f, 650f);
        }

        EnsureArrows();
        EnsureCards();
    }

    /**
     * 确保箭头按钮正确初始化
     */
    private void EnsureArrows()
    {
        if (leftArrow == null)
        {
            leftArrow = CreateArrow("LeftArrow", windowRoot, new Vector2(-560f, 0f), "\u25c0");
        }

        if (rightArrow == null)
        {
            rightArrow = CreateArrow("RightArrow", windowRoot, new Vector2(560f, 0f), "\u25b6");
        }
    }

    /**
     * 确保卡片正确初始化
     * 清理空引用并创建足够的卡片
     */
    private void EnsureCards()
    {
        if (cardItems == null)
        {
            cardItems = new List<CardMainMenuItem>();
        }

        for (int i = cardItems.Count - 1; i >= 0; i--)
        {
            if (cardItems[i] == null)
            {
                cardItems.RemoveAt(i);
            }
        }

        while (cardItems.Count < CardCount)
        {
            cardItems.Add(CreateCard(cardItems.Count));
        }
    }

    /**
     * 创建新的卡片
     * @param index 卡片索引
     * @return 创建的卡片菜单项
     */
    private CardMainMenuItem CreateCard(int index)
    {
        GameObject cardObject = new GameObject($"Card_{index + 1}", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(CanvasGroup), typeof(Button), typeof(CardMainMenuItem));
        cardObject.layer = GetUiLayer();
        RectTransform rect = cardObject.GetComponent<RectTransform>();
        rect.SetParent(cardRoot, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(320f, 440f);

        Image background = cardObject.GetComponent<Image>();
        background.color = new Color(0.24f, 0.45f, 0.86f, 1f);
        background.raycastTarget = true;

        Button button = cardObject.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;

        Image imagePlaceholder = CreateImage("Image", rect, new Vector2(0f, 42f), new Vector2(238f, 220f),
            new Color(0.94f, 0.48f, 0.15f, 1f));
        TextMeshProUGUI titleText = CreateText("Title", rect, CardTitles[index], new Vector2(0f, 170f), 34f);
        TextMeshProUGUI buttonText = CreateText("ButtonText", rect, CardTitles[index], new Vector2(0f, -168f), 30f);

        CardMainMenuItem item = cardObject.GetComponent<CardMainMenuItem>();
        item.Bind(rect, cardObject.GetComponent<CanvasGroup>(), background, imagePlaceholder, titleText, buttonText,
            button);
        return item;
    }

    /**
     * 创建箭头按钮
     * @param objectName 对象名称
     * @param parent 父节点
     * @param position 位置
     * @param text 箭头文本
     * @return 创建的按钮
     */
    private static Button CreateArrow(string objectName, RectTransform parent, Vector2 position, string text)
    {
        GameObject arrowObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(Button));
        arrowObject.layer = GetUiLayer();
        RectTransform rect = arrowObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(130f, 96f);

        Image image = arrowObject.GetComponent<Image>();
        image.color = new Color(0.18f, 0.36f, 0.78f, 0.9f);

        TextMeshProUGUI label = CreateText("Text", rect, text, Vector2.zero, 50f);
        label.color = Color.white;

        return arrowObject.GetComponent<Button>();
    }

    /**
     * 创建图片元素
     * @param objectName 对象名称
     * @param parent 父节点
     * @param position 位置
     * @param size 尺寸
     * @param color 颜色
     * @return 创建的图片
     */
    private static Image CreateImage(string objectName, RectTransform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.layer = GetUiLayer();
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    /**
     * 创建文本元素
     * @param objectName 对象名称
     * @param parent 父节点
     * @param text 文本内容
     * @param position 位置
     * @param fontSize 字体大小
     * @return 创建的文本
     */
    private static TextMeshProUGUI CreateText(string objectName, RectTransform parent, string text, Vector2 position,
        float fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.layer = GetUiLayer();
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(250f, 58f);

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.font = GameManager.Instance.font;
        label.text = text;
        label.fontSize = fontSize;
        label.enableAutoSizing = true;
        label.fontSizeMin = 18f;
        label.fontSizeMax = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    /**
     * 创建矩形变换对象
     * @param objectName 对象名称
     * @param parent 父节点
     * @return 创建的矩形变换
     */
    private static RectTransform CreateRect(string objectName, RectTransform parent)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.layer = GetUiLayer();
        RectTransform rect = rectObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    /**
     * 将索引包装在有效范围内
     * @param index 原始索引
     * @return 包装后的索引
     */
    private static int WrapIndex(int index)
    {
        int result = index % CardCount;
        return result < 0 ? result + CardCount : result;
    }

    /**
     * 获取UI层
     * @return UI层索引
     */
    private static int GetUiLayer()
    {
        int layer = LayerMask.NameToLayer("UI");
        return layer >= 0 ? layer : 0;
    }

    /**
     * 停止旋转动画
     */
    private void KillRotate()
    {
        if (rotateSequence == null)
        {
            return;
        }

        rotateSequence.Kill();
        rotateSequence = null;
    }
}
