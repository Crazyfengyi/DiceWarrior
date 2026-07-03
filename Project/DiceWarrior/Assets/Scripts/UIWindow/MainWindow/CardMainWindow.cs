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
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using System;

/// <summary>
/// 卡片主窗口类
/// </summary>
public sealed class CardMainWindow : UGUIPanelBase<DefaultUGUIDataBase>, IBeginDragHandler, IDragHandler,
    IEndDragHandler
{
    // 卡片相关常量
    private const int CardCount = 5; // 卡片总数
    private const float AngleStep = 360f / CardCount; // 每张卡片的角度间隔
    private const float RadiusX = 400f; // 卡片圆形布局的X轴半径
    private const float PerspectiveSkewX = 0f; // 透视效果的X轴倾斜
    private const float PerspectiveSkewY = -100f; // 透视效果的Y轴倾斜
    private const float FrontScale = 1f; // 前景卡片的缩放比例
    private const float BackScale = 0.6f; // 背景卡片的缩放比例
    private const float FrontAlpha = 1f; // 前景卡片的不透明度
    private const float BackAlpha = 0.24f; // 背景卡片的不透明度
    private const float MaxYRotation = 58f; // 卡片Y轴最大旋转角度
    private const float DragAnglePerScreen = 180f; // 每屏拖动对应的角度变化
    private const float SnapDuration = 0.25f; // 卡片吸附动画的持续时间
    private const int StartLevelId = 1; // 开始游戏的关卡ID

    // 分组类型定义
    private static readonly GroupType MiddleGroup = (GroupType)Enum.Parse(typeof(GroupType), "\u4e2d\u95f4"); // 中间分组
    private static readonly GroupType Popup2Group = (GroupType)Enum.Parse(typeof(GroupType), "\u5f39\u7a972"); // 弹窗2分组

    // 卡片标题数组
    private static readonly string[] CardTitles =
    {
        "\u7ee7\u7eed\u6e38\u620f", // 继续游戏
        "\u5f00\u59cb\u6e38\u620f", // 开始游戏
        "\u8d44\u6599", // 资料
        "\u8bbe\u7f6e", // 设置
        "\u9000\u51fa" // 退出
    };


    // UI组件引用
    [SerializeField] private RectTransform windowRoot; // 窗口根节点
    [SerializeField] private RectTransform cardRoot; // 卡片根节点
    [SerializeField] private UICustomButton leftArrow; // 左箭头按钮
    [SerializeField] private UICustomButton rightArrow; // 右箭头按钮
    [SerializeField] private CardMainMenuItem cardPrefab; // 卡片预制体
    [SerializeField] private List<CardMainMenuItem> cardItems = new List<CardMainMenuItem>(); // 卡片列表

    // 卡片深度排序相关
    private readonly List<CardDepthOrder> cardDepthOrders = new List<CardDepthOrder>(CardCount);
    private float currentAngle; // 当前旋转角度
    private float dragStartAngle; // 拖动开始角度
    private float dragStartX; // 拖动开始X坐标
    private int selectedIndex; // 当前选中的卡片索引
    private Sequence rotateSequence; // 旋转动画序列
    private bool actionOpening; // 是否正在执行打开动作

    /// <summary>
    /// 窗口打开时的回调
    /// </summary>
    /// <param name="userData">用户数据</param>
    public override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        EnsureUI(); // 确保UI元素正确创建
        RegisterEvents(); // 注册事件监听
        actionOpening = false;
        selectedIndex = 0; // 重置选中索引
        currentAngle = 0f; // 重置当前角度
        RefreshCards(); // 刷新卡片显示
    }

    /// <summary>
    /// 窗口关闭时的回调
    /// </summary>
    /// <param name="isShutdown">是否正在关闭应用</param>
    /// <param name="userData">用户数据</param>
    public override void OnClose(bool isShutdown, object userData)
    {
        KillRotate(); // 停止旋转动画
        actionOpening = false;
        base.OnClose(isShutdown, userData);
    }

    /// <summary>
    /// 开始拖动时的回调
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        KillRotate(); // 停止旋转动画
        dragStartAngle = currentAngle; // 记录拖动开始角度
        dragStartX = eventData.position.x; // 记录拖动开始X坐标
    }

    /// <summary>
    /// 拖动过程中的回调
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public void OnDrag(PointerEventData eventData)
    {
        float screenWidth = Mathf.Max(1f, Screen.width);
        float dragOffset = eventData.position.x - dragStartX;
        currentAngle = dragStartAngle + dragOffset / screenWidth * DragAnglePerScreen;
        selectedIndex = WrapIndex(Mathf.RoundToInt(-currentAngle / AngleStep));
        RefreshCards();
    }

    /// <summary>
    /// 结束拖动时的回调
    /// </summary>
    /// <param name="eventData">事件数据</param>
    public void OnEndDrag(PointerEventData eventData)
    {
        SnapToNearest(); // 吸附到最近的卡片
    }

    /// <summary>
    /// 注册事件监听
    /// </summary>
    private void RegisterEvents()
    {
        // 注册左右箭头按钮点击事件
        if (leftArrow != null)
        {
            leftArrow.AddListener(RotateLeft);
        }

        if (rightArrow != null)
        {
            rightArrow.AddListener(RotateRight);
        }

        // 注册卡片点击事件
        for (int i = 0; i < cardItems.Count; i++)
        {
            int index = i;
            UICustomButton button = cardItems[i] != null ? cardItems[i].Button : null;
            if (button == null)
            {
                continue;
            }

            button.AddListener(() => OnCardClicked(index));
        }
    }

    /// <summary>
    /// 向左旋转卡片
    /// </summary>
    private void RotateLeft()
    {
        AnimateToIndex(selectedIndex - 1);
    }

    /// <summary>
    /// 向右旋转卡片
    /// </summary>
    private void RotateRight()
    {
        AnimateToIndex(selectedIndex + 1);
    }

    /// <summary>
    /// 吸附到最近的卡片
    /// </summary>
    private void SnapToNearest()
    {
        AnimateToIndex(Mathf.RoundToInt(-currentAngle / AngleStep));
    }

    /// <summary>
    /// 动画切换到指定索引的卡片
    /// </summary>
    /// <param name="index">目标索引</param>
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

    /// <summary>
    /// 卡片点击事件处理
    /// </summary>
    /// <param name="index">卡片索引</param>
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
            case 0: // 继续游戏
                actionOpening = true;
                await OpenGameWindow();
                break;
            case 1: // 开始游戏
                actionOpening = true;
                ResetSaveLevel();
                await OpenGameWindow();
                break;
            case 2: // 资料
                FloatTipWindow.Show("\u8d44\u6599\u5e93\u6682\u672a\u5f00\u653e");
                break;
            case 3: // 设置
                actionOpening = true;
                await UIMonoInstance.OpenPanel<SettingWindow>(Popup2Group);
                actionOpening = false;
                break;
            case 4: // 退出
                QuitGame();
                break;
        }
    }

    /// <summary>
    /// 打开游戏窗口
    /// </summary>
    private async UniTask OpenGameWindow()
    {
        CloseSelfPanel();
        await UIMonoInstance.OpenPanel<GameWindow>(MiddleGroup);
    }

    /// <summary>
    /// 重置保存的关卡
    /// </summary>
    private static void ResetSaveLevel()
    {
        Save_GameData gameData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameData>(true);
        gameData.currentLevelId = StartLevelId;
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    private static void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("Quit game requested from CardMainWindow.");
        FloatTipWindow.Show("\u7f16\u8f91\u5668\u4e2d\u4e0d\u9000\u51fa\u6e38\u620f");
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 刷新卡片显示
    /// </summary>
    private void RefreshCards()
    {
        int frontIndex = WrapIndex(Mathf.RoundToInt(-currentAngle / AngleStep));
        cardDepthOrders.Clear();

        for (int i = 0; i < cardItems.Count; i++)
        {
            CardMainMenuItem item = cardItems[i];
            if (item == null)
            {
                continue;
            }

            float angle = currentAngle + i * AngleStep;
            float radians = angle * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float z = Mathf.Cos(radians);
            float depth = Mathf.InverseLerp(-1f, 1f, z);
            float screenX = sin * RadiusX + z * PerspectiveSkewX;
            float screenY = z * PerspectiveSkewY;
            float scale = Mathf.Lerp(BackScale, FrontScale, depth);
            float alpha = Mathf.Lerp(BackAlpha, FrontAlpha, depth);
            float yRotation = -sin * MaxYRotation;

            item.Refresh(CardTitles[i], i == frontIndex);
            item.ApplyPose(new Vector2(screenX, screenY), scale, alpha, yRotation, 0);
            cardDepthOrders.Add(new CardDepthOrder(item, depth));
        }

        cardDepthOrders.Sort((left, right) => left.Depth.CompareTo(right.Depth));
        for (int i = 0; i < cardDepthOrders.Count; i++)
        {
            cardDepthOrders[i].Item.transform.SetSiblingIndex(i);
        }
    }

    /// <summary>
    /// 确保UI元素正确创建
    /// </summary>
    private void EnsureUI()
    {
        // 设置窗口根节点
        RectTransform rootRect = transform as RectTransform;
        if (rootRect != null)
        {
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
        }

        // 添加背景图片
        Image background = GetComponent<Image>();
        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
        }

        background.color = new Color(0.05f, 0.07f, 0.12f, 1f);
        background.raycastTarget = true;

        // 创建和设置窗口根节点
        windowRoot = EnsureRect(windowRoot, "WindowRoot", transform as RectTransform);
        StretchToParent(windowRoot);

        // 创建和设置卡片根节点
        cardRoot = EnsureRect(cardRoot, "CardRoot", windowRoot);
        cardRoot.anchorMin = cardRoot.anchorMax = new Vector2(0.5f, 0.5f);
        cardRoot.anchoredPosition = Vector2.zero;
        cardRoot.sizeDelta = new Vector2(1100f, 620f);

        // 创建左右箭头按钮
        leftArrow = EnsureArrow(leftArrow, "LeftArrow", windowRoot, new Vector2(-520f, 0f), "<");
        rightArrow = EnsureArrow(rightArrow, "RightArrow", windowRoot, new Vector2(520f, 0f), ">");
        EnsureCards(); // 确保卡片正确创建
    }

    /// <summary>
    /// 确保卡片正确创建
    /// </summary>
    private void EnsureCards()
    {
        if (cardItems == null)
        {
            cardItems = new List<CardMainMenuItem>();
        }

        // 移除空引用
        for (int i = cardItems.Count - 1; i >= 0; i--)
        {
            if (cardItems[i] == null)
            {
                cardItems.RemoveAt(i);
            }
        }

        // 创建缺失的卡片
        while (cardItems.Count < CardCount)
        {
            cardItems.Add(CreateCard(cardItems.Count));
        }

        // 初始化所有卡片
        for (int i = 0; i < cardItems.Count; i++)
        {
            cardItems[i].Init();
        }
    }

    /// <summary>
    /// 创建卡片实例
    /// </summary>
    /// <param name="index">卡片索引</param>
    /// <returns>创建的卡片实例</returns>
    private CardMainMenuItem CreateCard(int index)
    {
        CardMainMenuItem item = Instantiate(cardPrefab, cardRoot, false);

        item.gameObject.name = $"Card_{index}";
        item.Init();
        return item;
    }

    /// <summary>
    /// 确保箭头按钮正确创建
    /// </summary>
    private UICustomButton EnsureArrow(UICustomButton current, string objectName, RectTransform parent,
        Vector2 position, string text)
    {
        if (current != null)
        {
            return current;
        }

        // 创建箭头按钮对象
        GameObject arrowObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Button), typeof(UICustomButton));
        arrowObject.layer = GetUiLayer();

        // 设置箭头按钮的RectTransform
        RectTransform rect = arrowObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(92f, 92f);

        // 设置箭头按钮的图片
        Image image = arrowObject.GetComponent<Image>();
        image.color = new Color(0.16f, 0.26f, 0.43f, 0.88f);

        // 设置箭头按钮的Button组件
        Button button = arrowObject.GetComponent<Button>();
        button.transition = Selectable.Transition.None;

        // 创建箭头按钮的文本
        TextMeshProUGUI label = CreateText("Text", rect, text, Vector2.zero, 44f);
        label.color = Color.white;

        // 设置箭头按钮的UICustomButton组件
        UICustomButton customButton = arrowObject.GetComponent<UICustomButton>();
        customButton.needClickAudio = true;
        customButton.needAni = true;
        return customButton;
    }

    /// <summary>
    /// 确保RectTransform正确创建
    /// </summary>
    private static RectTransform EnsureRect(RectTransform current, string objectName, RectTransform parent)
    {
        if (current != null)
        {
            return current;
        }

        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.layer = GetUiLayer();
        RectTransform rect = rectObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    /// <summary>
    /// 创建文本UI元素
    /// </summary>
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
        rect.sizeDelta = new Vector2(260f, 62f);

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

    /// <summary>
    /// 将RectTransform拉伸至父节点大小
    /// </summary>
    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// 环绕索引处理，确保索引在有效范围内
    /// </summary>
    private static int WrapIndex(int index)
    {
        int result = index % CardCount;
        return result < 0 ? result + CardCount : result;
    }

    /// <summary>
    /// 获取UI层的Layer值
    /// </summary>
    private static int GetUiLayer()
    {
        int layer = LayerMask.NameToLayer("UI");
        return layer >= 0 ? layer : 0;
    }

    /// <summary>
    /// 停止旋转动画
    /// </summary>
    private void KillRotate()
    {
        if (rotateSequence == null)
        {
            return;
        }

        rotateSequence.Kill();
        rotateSequence = null;
    }

    /// <summary>
    /// 卡片深度排序结构体
    /// </summary>
    private readonly struct CardDepthOrder
    {
        public readonly CardMainMenuItem Item; // 卡片项
        public readonly float Depth; // 深度值

        public CardDepthOrder(CardMainMenuItem item, float depth)
        {
            Item = item;
            Depth = depth;
        }
    }
}