using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 方块视图类，负责处理方块在游戏板上的显示和布局
/// </summary>
public sealed class BlockView : MonoBehaviour
{
    // 足球根节点的矩形变换组件
    public RectTransform BallRoot;

    public Image leftBg;
    public Image midBg;
    public Image rightBg;

    private RectTransform midRect;

    private float startMidWith;

    // 方块数据
    private BlockData data;
    public BlockData Data => data;

    // 游戏板尺寸
    private Vector2 boardSize;

    // 单元格大小
    private float cellSize;

    // 单元格间隙
    private float gap;

    // 方块的矩形变换组件
    public RectTransform RectTransform { get; private set; }

    private void Awake()
    {
        midRect = midBg.GetComponent<RectTransform>();
        startMidWith = midRect.rect.width;
    }

    /// <summary>
    /// 初始化方块视图
    /// </summary>
    /// <param name="blockData">方块数据</param>
    /// <param name="size">游戏板尺寸</param>
    /// <param name="gridCellSize">网格单元格大小</param>
    /// <param name="cellGap">单元格间隙</param>
    public void Init(BlockData blockData, Vector2 size, float gridCellSize, float cellGap)
    {
        data = blockData;
        boardSize = size;
        cellSize = gridCellSize;
        gap = cellGap;
        RectTransform = transform as RectTransform;

        // 创建足球根节点
        // 设置足球根节点的锚点和偏移
        BallRoot.anchorMin = Vector2.zero;
        BallRoot.anchorMax = Vector2.one;
        BallRoot.offsetMin = Vector2.zero;
        BallRoot.offsetMax = Vector2.zero;
        SetGridPosition(data, 0f);
    }

    /// <summary>
    /// 设置方块在网格中的位置
    /// </summary>
    /// <param name="blockData">方块数据</param>
    /// <param name="duration">动画持续时间</param>
    public void SetGridPosition(BlockData blockData, float duration)
    {
        // 计算方块大小和位置
        Vector2 size = new Vector2(blockData.Width * cellSize - gap, cellSize - gap);
        Vector2 position = new Vector2(
            -boardSize.x * 0.5f + (blockData.X + blockData.Width * 0.5f) * cellSize,
            -boardSize.y * 0.5f + (blockData.Y + 0.5f) * cellSize);
        // 设置方块大小
        RectTransform.sizeDelta = size;
        if (duration <= 0f)
        {
            // 直接设置位置
            RectTransform.anchoredPosition = position;
        }
        else
        {
            // 使用动画设置位置，带有弹性效果
            RectTransform.DOAnchorPos(position, duration).SetEase(Ease.OutBounce);
        }

        UpdateBgShow();
    }

    public void UpdateBgShow()
    {
        midBg.gameObject.SetActive(data.Width != 1);
        Vector2 old = midRect.sizeDelta;
        switch (data.Width)
        {
            case 2:
                midRect.sizeDelta = new Vector2(startMidWith * 3f, old.y);
                break;
            case 3:
                midRect.sizeDelta = new Vector2(startMidWith * 6.36f, old.y);
                break;
            case 4:
                midRect.sizeDelta = new Vector2(startMidWith * 9.8f, old.y);
                break;
            case 5:
                midRect.sizeDelta = new Vector2(startMidWith * 13.2f, old.y);
                break;
            case 6:
                midRect.sizeDelta = new Vector2(startMidWith * 16.59f, old.y);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 布局方块中的足球
    /// </summary>
    public void LayoutBalls()
    {
        List<FootballData> balls = data.Footballs;
        if (balls.Count == 0)
        {
            return;
        }

        // 计算可用空间和足球大小
        float availableWidth = RectTransform.rect.width > 0f ? RectTransform.rect.width : data.Width * cellSize - gap;
        float availableHeight = RectTransform.rect.height > 0f ? RectTransform.rect.height : cellSize - gap;
        float ballSize = Mathf.Min(availableHeight * 0.72f, availableWidth / (balls.Count + 0.4f));
        float spacing = Mathf.Min(ballSize * 1.2f, availableWidth / balls.Count);
        float startX = -spacing * (balls.Count - 1) * 0.5f;

        // 设置每个足球的位置和大小
        for (int i = 0; i < balls.Count; i++)
        {
            RectTransform ballRect = balls[i].View.RectTransform;
            ballRect.sizeDelta = Vector2.one * ballSize;
            ballRect.anchoredPosition = new Vector2(startX + spacing * i, 0f);
        }
    }
}