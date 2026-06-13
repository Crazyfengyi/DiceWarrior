using System;
using System.Collections.Generic;
using System.Linq;
using cfg;
using DG.Tweening;
using GameMain;
using UnityEngine;
using UnityEngine.UI;
using YangTools.Scripts.Core.YangAudio;
using YangTools.Scripts.Core.YangExtend;
using YangTools.Scripts.Core.YangSaveData;
using YangTools.Scripts.Core.YangUGUI;
using Random = UnityEngine.Random;

/// <summary>
/// 游戏根类，负责管理游戏的核心逻辑和状态
/// </summary>
public class GameRoot : MonoBehaviour
{
    // 常量定义
    private const int WaitSlotCount = 8; // 等待区槽位数量
    private const int AutoBackupSlotCount = 8; // 自动备份区槽位数量
    private const float CellGap = 6f; // 单元格之间的间隙
    private const float WaitingBallScale = 0.86f; //等待区球缩放
    private const float AutoBackupAddDuration = 0.24f; // 自动备份区添加动画时长
    private const float AutoBackupRemoveDuration = 0.2f; // 自动备份区移除动画时长
    private const float AutoBackupReflowDuration = 0.18f; // 自动备份区回流动画时长
    private const float AutoBackupSlotAppearDuration = 0.18f; // 自动备份区槽位出现动画时长
    private static readonly float GravityAnimationDuration = 0.76f;
    private static readonly float GravityAnimationCompleteDelay = 0.762f;
    private static readonly float EmptyBlockRemoveDelay = 0.06f;
    private const int CoinBallCategory = 1;
    private const int NormalBallCategory = 2;
    private const int SpecialBallCategory = 3;
    private const int MatchGroupSize = 3;
    private const float ComboTimeoutSeconds = 8f; //连击超时时间
    private const int ComboDisplayStartCount = 2;
    private const int MaxReviveCount = 3;
    private const int CoinPropId = 2;
    private const string UnlockWaitSlotAdType = "UnlockWaitSlot";

    private int ActiveWaitSlotCount => isLastWaitSlotUnlocked ? WaitSlotCount : WaitSlotCount - 1;
    private int LastWaitSlotIndex => WaitSlotCount - 1;

    // 数据列表
    private readonly List<BlockData> allBlocks = new List<BlockData>(); // 所有方块数据
    public readonly List<FootballData> waitingBalls = new List<FootballData>(); // 等待区的球数据
    private readonly List<WaitSlotView> waitSlots = new List<WaitSlotView>(); // 等待区槽位视图
    private readonly List<FootballData> autoBackupBalls = new List<FootballData>(); // 自动备份区的球数据
    private readonly List<WaitSlotView> autoBackupSlots = new List<WaitSlotView>(); // 自动备份区槽位视图

    private readonly List<FootballType> pendingSpawnBalls = new List<FootballType>(); // 待生成的球池
    private readonly List<FootballType> roundFootballTypes = new List<FootballType>(); // 本局固定的普通足球类型
    private readonly List<FootballType> roundSpecialFootballTypes = new List<FootballType>();
    private readonly HashSet<FootballType> pendingPrioritySpawnTypes = new HashSet<FootballType>();
    private readonly HashSet<FootballData> movingToWaitingBalls = new HashSet<FootballData>();
    private readonly HashSet<BlockData> droppingSpawnBlocks = new HashSet<BlockData>();
    private readonly List<BlockData> currentTopSpawnBlocks = new List<BlockData>();
    private readonly List<BlockData> currentBoardFillSpawnBlocks = new List<BlockData>();
    private bool collectingTopSpawnBlocks;
    private bool boardFillInProgress;
    private bool boardFillRequested;
    private Action boardFillCompleteCallbacks;

    // 预制体引用
    public BlockView prefabBlock; // 方块预制体
    public FootballView prefabBall; // 足球预制体
    public WaitSlotView prefabWaitAre; // 等待区槽位预制体

    // UI根节点
    public RectTransform boardRoot; // 游戏面板根节点
    public RectTransform waitRoot; // 等待区根节点
    public RectTransform flyingRoot; // 飞行物体根节点
    public RectTransform autoBackupRoot; // 自动备份区根节点

    /// <summary>
    /// 进度
    /// </summary>
    public float Progress => level == null || level.MatchableBallCount <= 0
        ? 1f
        : Mathf.Clamp01(matchedRemovedBallCount / (float) level.MatchableBallCount);

    private BlockData[,] occupancy; // 方块占用情况
    private LevelData level; // 关卡数据
    private LevelConfig levelConfig = LevelConfig.CreateDefault(); // 关卡配置
    private float cellSize; // 单元格大小
    public bool inputLocked; // 输入锁定状态
    private bool initialized; // 初始化状态
    private bool waitingAreaProcessing;
    private bool waitingAreaProcessRequested;
    private bool isLastWaitSlotUnlocked;
    private bool unlockWaitSlotAdShowing;
    private bool gameEnded;
    private int reviveUseCount;
    private bool reviveWindowOpening;
    private ReviveWindow currentReviveWindow;
    private int comboCount;
    private float lastComboMatchTime = float.NegativeInfinity;
    private int nextSourceRow; // 下一行源行号
    private int matchedRemovedBallCount; // 已通过三消移除的球数
    private int moneyRewardMatchCount; // 金币球三消次数
    private Action<Vector3> normalMatchCoinRewardHandler;

    /// <summary>
    /// 初始化游戏
    /// </summary>
    public void Initialize()
    {
        ApplyCurrentLevelConfig();
        if (initialized)
        {
            RestartGame(); // 如果已经初始化，则重新开始游戏
            return;
        }

        initialized = true;
        BuildLayout(); // 构建游戏布局
        Canvas.ForceUpdateCanvases(); // 强制更新画布
        RestartGame(); // 重新开始游戏
    }

    public void SetNormalMatchCoinRewardHandler(Action<Vector3> handler)
    {
        normalMatchCoinRewardHandler = handler;
    }

    /// <summary>
    /// 开始游戏关卡
    /// </summary>
    public void StartLevel(LevelConfig config)
    {
        // 初始化关卡配置，如果传入的config为null，则使用默认配置
        levelConfig = config ?? LevelConfig.CreateDefault();
        // 检查游戏是否已经初始化
        if (initialized)
        {
            // 如果已初始化，则重新开始游戏
            RestartGame();
        }
    }

    /// <summary>
    /// 开始新关卡
    /// </summary>
    public void StartLevel(TbLevelData tableData)
    {
        // 从表格数据创建关卡配置并开始关卡
        StartLevel(LevelConfig.CreateFromTable(tableData));
    }

    public void ApplyCurrentLevelConfig()
    {
        TbLevelData tableData = GetCurrentLevelTableData();
        levelConfig = LevelConfig.CreateFromTable(tableData);
    }

    private TbLevelData GetCurrentLevelTableData()
    {
        TBLevelCategory category = GameTableManager.Instance?.Tables?.TBLevelCategory;
        if (category == null || category.DataList == null || category.DataList.Count == 0)
        {
            Debug.LogError("关卡表为空，使用默认关卡配置.");
            return null;
        }

        Save_GameData saveData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameData>();
        TbLevelData tableData = category.GetOrDefault(saveData.currentLevelId);
        Debug.Log($"关卡表：{saveData.currentLevelId}");
        if (tableData != null)
        {
            return tableData;
        }

        TbLevelData firstLevel = GetFirstLevelTableData(category);
        if (firstLevel == null)
        {
            Debug.LogError("关卡表没有有效关卡，使用默认关卡配置.");
            return null;
        }

        Save_GameData dirtySaveData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameData>(true);
        dirtySaveData.currentLevelId = firstLevel.Id;
        return firstLevel;
    }

    private void AdvanceSavedLevel()
    {
        TBLevelCategory category = GameTableManager.Instance?.Tables?.TBLevelCategory;
        if (category == null || category.DataList == null || category.DataList.Count == 0)
        {
            return;
        }

        Save_GameData saveData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameData>(true);
        TbLevelData currentLevel = category.GetOrDefault(saveData.currentLevelId) ?? GetFirstLevelTableData(category);
        if (currentLevel == null)
        {
            return;
        }

        int nextLevelId = currentLevel.Id;
        List<TbLevelData> sortedLevels = category.DataList.OrderBy(item => item.Id).ToList();
        for (int i = 0; i < sortedLevels.Count; i++)
        {
            if (sortedLevels[i].Id > currentLevel.Id)
            {
                nextLevelId = sortedLevels[i].Id;
                break;
            }
        }

        saveData.currentLevelId = nextLevelId;
    }

    private TbLevelData GetFirstLevelTableData(TBLevelCategory category)
    {
        return category?.DataList?
            .Where(item => item != null)
            .OrderBy(item => item.Id)
            .FirstOrDefault();
    }

    /// <summary>
    /// 添加待生成的足球到队列中，并尝试填充行
    /// </summary>
    /// <param name="type">足球的颜色</param>
    /// <param name="count">要添加的足球数量</param>
    public void AddPendingSpawnBalls(FootballType type, int count)
    {
        // 如果数量小于等于0，直接返回，不执行任何操作
        if (count <= 0)
        {
            return;
        }

        // 确保颜色值在有效范围内
        FootballType safeType = ClampFootballColor(type);
        // 循环将指定数量的足球颜色添加到待生成队列中
        for (int i = 0; i < count; i++)
        {
            pendingSpawnBalls.Add(safeType);
        }

        // 添加足球后尝试填充行
        TryFillRowsAfterAddingPendingBalls();
    }

    public void AddPendingSpawnBallGroups(FootballType type, int groupCount)
    {
        AddPendingSpawnBalls(type, Mathf.Max(0, groupCount) * 3);
    }

    public void AddRandomPendingSpawnBallGroups(int groupCount, int ballTypeCount)
    {
        if (groupCount <= 0)
        {
            return;
        }

        List<FootballType> balls = new List<FootballType>(groupCount * 3);
        for (int i = 0; i < groupCount; i++)
        {
            FootballType type = GetRandomRoundFootballType(ballTypeCount);
            balls.Add(type);
            balls.Add(type);
            balls.Add(type);
        }

        ShuffleBalls(balls);
        AddPendingSpawnBallColors(balls);
        TryFillRowsAfterAddingPendingBalls();
    }

    public void RestartGame()
    {
        DOTween.Kill(this);
        boardRoot.DestroyAllChild();
        flyingRoot.DestroyAllChild();
        autoBackupRoot.DestroyAllChild();

        allBlocks.Clear();
        waitingBalls.Clear();
        autoBackupBalls.Clear();
        autoBackupSlots.Clear();
        pendingSpawnBalls.Clear();
        roundFootballTypes.Clear();
        roundSpecialFootballTypes.Clear();
        pendingPrioritySpawnTypes.Clear();
        movingToWaitingBalls.Clear();
        droppingSpawnBlocks.Clear();
        currentTopSpawnBlocks.Clear();
        currentBoardFillSpawnBlocks.Clear();
        collectingTopSpawnBlocks = false;
        boardFillInProgress = false;
        boardFillRequested = false;
        boardFillCompleteCallbacks = null;
        occupancy = null;
        inputLocked = false;
        waitingAreaProcessing = false;
        waitingAreaProcessRequested = false;
        isLastWaitSlotUnlocked = false;
        unlockWaitSlotAdShowing = false;
        gameEnded = false;
        reviveUseCount = 0;
        reviveWindowOpening = false;
        currentReviveWindow = null;
        matchedRemovedBallCount = 0;
        moneyRewardMatchCount = 0;
        ResetCombo();

        foreach (WaitSlotView slot in waitSlots)
        {
            slot.SetOccupied(false);
        }

        level = LevelData.CreateFromConfig(levelConfig);
        CreatePendingSpawnBallPool(levelConfig);
        nextSourceRow = 0;
        occupancy = new BlockData[level.Width, level.Height];
        Canvas.ForceUpdateCanvases();
        SpawnLevel(level);
        ApplyGravity(false, () => StartBoardFill(false, () => SetStatus("Tap a football")));
        
        Save_GameData saveData = YangSaveDataManager.Instance.DataCenter.GetLocalSave<Save_GameData>();
        var leveData = GameTableManager.Instance.Tables.TBLevelCategory.GetOrDefault(saveData.currentLevelId);
        GameStart temp = new GameStart();
        temp.levelID = saveData.currentLevelId;
        temp.levelName = leveData.LevelName;
        temp.SendEvent();
    }

    public bool CanUseUndoProp()
    {
        return !inputLocked && !HasActiveWaitingAreaOperation() && waitingBalls.Count > 0;
    }

    public bool CanUseClearProp()
    {
        if (inputLocked || HasActiveWaitingAreaOperation() || waitingBalls.Count == 0)
        {
            return false;
        }

        int clearCount = Mathf.Min(3, waitingBalls.Count);
        int remainingBackupSlots = AutoBackupSlotCount - autoBackupBalls.Count;
        return clearCount > 0 && remainingBackupSlots >= clearCount;
    }

    public bool CanUseShuffleProp()
    {
        if (inputLocked || HasActiveWaitingAreaOperation())
        {
            return false;
        }

        int shuffleBallCount = GetBoardBallCount() + pendingSpawnBalls.Count;
        return shuffleBallCount > 1;
    }

    public bool UseUndoProp()
    {
        return UseUndoProp(GetDefaultEffectWorldPosition());
    }

    public bool UseUndoProp(Vector3 propWorldPosition)
    {
        if (inputLocked || HasActiveWaitingAreaOperation())
        {
            FloatTipWindow.Show("暂时无法使用");
            return false;
        }

        if (waitingBalls.Count == 0)
        {
            SetStatus("Nothing to undo");
            FloatTipWindow.Show("等待区为空");
            return false;
        }

        inputLocked = true;
        int lastIndex = waitingBalls.Count - 1;
        FootballData ball = waitingBalls[lastIndex];
        FootballType returnedType = ball.Type;
        waitingBalls.RemoveAt(lastIndex);
        RefreshWaitSlots();

        ball.View.SetInteractable(false);
        if (flyingRoot != null)
        {
            ball.View.transform.SetParent(flyingRoot, true);
        }

        Sequence sequence = DOTween.Sequence().SetTarget(this);
        sequence.Join(ball.View.transform.DOMove(propWorldPosition, 0.28f)
            .SetEase(Ease.InCubic));
        sequence.Join(ball.View.transform.DOScale(Vector3.zero, 0.28f)
            .SetEase(Ease.InBack));
        sequence
            .SetTarget(this)
            .OnComplete(() =>
            {
                Destroy(ball.View.gameObject);
                ReturnBallToPendingPool(returnedType);
                inputLocked = false;
                //FillEmptyRowsAfterGravity(false, FinishTurn);
            });
        return true;
    }

    public bool UseClearProp()
    {
        return UseClearProp(3);
    }

    public bool UseClearProp(int count)
    {
        if (inputLocked || HasActiveWaitingAreaOperation())
        {
            FloatTipWindow.Show("暂时无法使用");
            return false;
        }

        if (waitingBalls.Count == 0)
        {
            FloatTipWindow.Show("等待区为空");
            return false;
        }

        int clearCount = Mathf.Min(count, waitingBalls.Count);
        if (clearCount <= 0)
        {
            return false;
        }

        int remainingBackupSlots = AutoBackupSlotCount - autoBackupBalls.Count;
        if (remainingBackupSlots <= 0)
        {
            SetStatus("Auto backup full");
            FloatTipWindow.Show("备选区已满");
            return false;
        }

        if (remainingBackupSlots < clearCount)
        {
            SetStatus("Not enough auto backup slots");
            FloatTipWindow.Show("备选区位置不足");
            return false;
        }

        inputLocked = true;
        int firstMovingIndex = waitingBalls.Count - clearCount;
        List<FootballData> movingBalls = waitingBalls.GetRange(firstMovingIndex, clearCount);
        waitingBalls.RemoveRange(firstMovingIndex, clearCount);
        RefreshWaitSlots();

        foreach (FootballData ball in movingBalls)
        {
            ball.View.SetInteractable(false);
            ball.View.transform.SetParent(flyingRoot, true);
            autoBackupBalls.Add(ball);
        }

        AnimateAutoBackupAdd(ProcessWaitingArea);
        return true;
    }

    /// <summary>
    /// 使用洗牌功能
    /// </summary>
    public bool UseShuffleProp()
    {
        return UseShuffleProp(GetDefaultEffectWorldPosition());
    }

    public bool UseShuffleProp(Vector3 centerWorldPosition)
    {
        // 如果输入被锁定，直接返回，不执行任何操作
        if (inputLocked || HasActiveWaitingAreaOperation())
        {
            FloatTipWindow.Show("暂时无法使用");
            return false;
        }

        List<FootballData> boardBalls = GetBoardBalls();
        // 计算需要洗牌的球的数量，包括当前板上的球和待生成的球
        int shuffleBallCount = boardBalls.Count + pendingSpawnBalls.Count;
        // 如果球的数量小于等于1，无法进行洗牌操作
        if (shuffleBallCount <= 1)
        {
            SetStatus("Not enough balls to shuffle");
            return false;
        }

        // 锁定输入，防止玩家在洗牌过程中进行其他操作
        inputLocked = true;
        if (boardBalls.Count == 0)
        {
            ShuffleBalls(pendingSpawnBalls);
            DOVirtual.DelayedCall(0.2f, FinishTurn).SetTarget(this);
            return true;
        }

        AnimateShuffleBoardBalls(boardBalls, centerWorldPosition, FinishTurn);
        return true;
    }

    private void BuildLayout()
    {
        RectTransform root = transform as RectTransform;
        if (root == null)
        {
            return;
        }

        if (flyingRoot != null)
        {
            flyingRoot.SetAsLastSibling();
        }

        for (int i = 0; i < WaitSlotCount; i++)
        {
            WaitSlotView slot = Instantiate(prefabWaitAre, waitRoot);
            slot.Init(i);
            slot.SetLockedClick(OnWaitSlotLockedClicked);
            waitSlots.Add(slot);
        }
    }

    private void SpawnLevel(LevelData data)
    {
        cellSize = CalculateCellSize(data);

        int initialRowCount = Mathf.Min(data.InitialVisibleRows, data.Height);
        for (int i = 0; i < initialRowCount; i++)
        {
            if (!SpawnRow(i, false))
            {
                break;
            }
        }

        RefreshWaitSlots();
    }

    /// <summary>
    /// 生成一个方块并初始化其相关数据
    /// </summary>
    /// <param name="definition">方块的定义数据，包含位置、大小等信息</param>
    /// <param name="animate">是否生成动画效果</param>
    /// <returns>返回生成的方块数据</returns>
    private BlockData SpawnBlock(BlockDefinition definition, bool animate)
    {
        // 计算游戏板的大小 720x810
        Vector2 boardSize = new Vector2(level.Width * cellSize, level.Height * cellSize);
        // 创建方块数据对象
        BlockData block = new BlockData(definition.X, definition.Y, definition.Width, definition.SourceRow);
        // 实例化方块视图
        BlockView blockView = Instantiate(prefabBlock, boardRoot);
        block.View = blockView;
        // 初始化方块视图
        blockView.Init(block, boardSize, cellSize, CellGap);

        // 为方块添加足球
        foreach (FootballType footballColor in definition.Footballs)
        {
            // 创建足球数据
            FootballData ball = new FootballData(footballColor, block);
            // 实例化足球视图
            FootballView view = Instantiate(prefabBall, blockView.BallRoot);
            // 初始化足球视图并设置点击事件
            view.Init(blockView.BallRoot, ball, OnFootballClicked);
            ball.View = view;
            // 将足球添加到方块的足球列表中
            block.Footballs.Add(ball);
        }

        // 布局方块中的足球
        blockView.LayoutBalls();
        // 将方块添加到所有方块列表中
        allBlocks.Add(block);

        if (collectingTopSpawnBlocks)
        {
            currentTopSpawnBlocks.Add(block);
            currentBoardFillSpawnBlocks.Add(block);
        }

        // 如果需要动画效果
        if (animate)
        {
            blockView.transform.localScale = Vector3.one;
            droppingSpawnBlocks.Add(block);
        }

        return block;
    }

    /// <summary>
    /// 计算单个单元格的大小
    /// </summary>
    /// <param name="data">关卡数据，包含宽度和高度信息</param>
    /// <returns>返回计算出的单元格大小</returns>
    private float CalculateCellSize(LevelData data)
    {
        // 获取棋盘根对象的矩形区域
        Rect rect = boardRoot.rect;
        // 计算有效宽度，如果宽度小于等于0则使用默认值900
        float width = rect.width > 0f ? rect.width : 900f;
        // 计算有效高度，如果高度小于等于0则使用默认值900
        float height = rect.height > 0f ? rect.height : 900f;
        // 返回宽度和高度除以对应维度后较小的值，确保单元格能完整显示在区域内
        return Mathf.Min(width / data.Width, height / data.Height); //90x95.5
    }

    private void OnFootballClicked(FootballData ball)
    {
        if (inputLocked || ball == null || movingToWaitingBalls.Contains(ball))
        {
            return;
        }

        if (autoBackupBalls.Contains(ball))
        {
            MoveAutoBackupBallToWaitingArea(ball);
            return;
        }

        BlockData owner = ball.Owner;
        if (owner == null || !allBlocks.Contains(owner) || ball.View == null || !owner.Footballs.Contains(ball))
        {
            return;
        }

        if (waitingBalls.Count >= ActiveWaitSlotCount)
        {
            SetStatus("Wait area full");
            return;
        }

        owner.Footballs.Remove(ball);
        bool removeOwnerBlock = owner.Footballs.Count == 0;
        ball.Owner = null;
        ball.View.SetInteractable(false);
        ball.View.transform.SetParent(flyingRoot, true);

        if (removeOwnerBlock)
        {
            RemoveBlockAndFillDuringAnimation(owner, null);
        }

        int insertIndex = InsertBallIntoWaitingArea(ball);
        movingToWaitingBalls.Add(ball);
        RefreshWaitSlots();
        Vector3 target = waitSlots[insertIndex].transform.position;
        Sequence sequence = DOTween.Sequence().SetTarget(this);
        ball.View.PlayMoveTrail();
        sequence.Join(ball.View.transform.DOMove(target, 0.32f)
            .SetEase(Ease.OutCubic));
        sequence.Join(ball.View.transform.DOScale(Vector3.one * WaitingBallScale, 0.32f)
            .SetEase(Ease.OutCubic));
        JoinWaitingBallReflow(sequence, ball, 0.18f);
        sequence.OnComplete(() => { CompleteMovingToWaitingBall(ball, () => { ProcessWaitingArea(); }); });
    }

    /// <summary>
    /// 将自动备份的球移动到等待区域
    /// </summary>
    /// <param name="ball">要移动的足球数据</param>
    private void MoveAutoBackupBallToWaitingArea(FootballData ball)
    {
        // 检查等待区域是否已满
        if (waitingBalls.Count >= ActiveWaitSlotCount)
        {
            SetStatus("Wait area full"); // 设置状态为等待区域已满
            return;
        }

        // 获取球在自动备份列表中的索引
        int backupIndex = autoBackupBalls.IndexOf(ball);
        if (backupIndex < 0)
        {
            return; // 如果球不在自动备份列表中，直接返回
        }

        //从自动备份列表中移除球,销毁对应的自动备份槽位
        autoBackupBalls.RemoveAt(backupIndex);
        DestroyAutoBackupSlotAt(backupIndex);

        // 设置球的交互状态为不可交互
        ball.View.SetInteractable(false);
        ball.View.transform.SetParent(flyingRoot, true);

        int insertIndex = InsertBallIntoWaitingArea(ball);
        movingToWaitingBalls.Add(ball);
        RefreshWaitSlots();
        Vector3 target = waitSlots[insertIndex].transform.position;

        Sequence sequence = DOTween.Sequence().SetTarget(this);
        ball.View.PlayMoveTrail();
        sequence.Join(ball.View.transform.DOMove(target, 0.32f)
            .SetEase(Ease.OutCubic));
        sequence.Join(ball.View.transform.DOScale(Vector3.one * WaitingBallScale, 0.32f)
            .SetEase(Ease.OutCubic));
        JoinWaitingBallReflow(sequence, ball, 0.18f);

        List<AutoBackupSlotLayout> layouts = CalculateAutoBackupSlotLayout(autoBackupSlots.Count);
        for (int i = 0; i < autoBackupSlots.Count; i++)
        {
            WaitSlotView slot = autoBackupSlots[i];
            slot.SetIndex(i);
            slot.SetOccupied(true);
            sequence.Join(AnimateAutoBackupSlotTo(slot, layouts[i], AutoBackupReflowDuration));
        }

        int ballLayoutCount = Mathf.Min(autoBackupBalls.Count, layouts.Count);
        for (int i = 0; i < ballLayoutCount; i++)
        {
            FootballView view = autoBackupBalls[i].View;
            if (view == null)
            {
                continue;
            }

            view.SetInteractable(false);
            sequence.Join(view.transform.DOMove(GetAutoBackupSlotWorldPosition(layouts[i]), AutoBackupReflowDuration)
                .SetEase(Ease.OutCubic));
        }

        sequence.OnComplete(() =>
        {
            CompleteMovingToWaitingBall(ball, () =>
            {
                ApplyAutoBackupLayoutImmediate();
                ProcessWaitingArea();
            });
        });
    }

    private void RemoveBlock(BlockData block, Action onComplete)
    {
        if (block == null || !allBlocks.Contains(block))
        {
            onComplete?.Invoke();
            return;
        }

        allBlocks.Remove(block);
        if (block.View != null)
        {
            Destroy(block.View.gameObject);
            block.View = null;
        }

        StartBoardFill(true, onComplete);
    }

    private void RemoveBlockAndFillDuringAnimation(BlockData block, Action onComplete)
    {
        if (block == null || !allBlocks.Contains(block))
        {
            onComplete?.Invoke();
            return;
        }

        BlockView blockView = block.View;
        allBlocks.Remove(block);
        block.View = null;

        if (blockView != null)
        {
            Sequence sequence = DOTween.Sequence().SetTarget(this);
            sequence.AppendInterval(EmptyBlockRemoveDelay);
            sequence.OnComplete(() => Destroy(blockView.gameObject));
        }

        StartBoardFill(true, onComplete, true);
    }

    /// <summary>
    /// 延迟后移除方块
    /// </summary>
    /// <param name="block">要移除的方块数据</param>
    /// <param name="onComplete">移除完成后的回调动作</param>
    private void RemoveBlockAfterDelay(BlockData block, Action onComplete)
    {
        // 检查方块是否存在或是否在方块集合中
        if (block == null || !allBlocks.Contains(block))
        {
            // 如果方块无效，直接执行回调并返回
            onComplete?.Invoke();
            return;
        }

        // 创建一个DOTween序列动画
        Sequence sequence = DOTween.Sequence().SetTarget(this);
        // 添加一个等待间隔，时间由EmptyBlockRemoveDelay决定
        sequence.AppendInterval(EmptyBlockRemoveDelay);
        // 设置序列完成时的回调，执行实际的方块移除操作
        sequence.OnComplete(() => RemoveBlock(block, onComplete));
    }

    /// <summary>
    /// 应用重力效果，使所有方块下落到可能的最低位置
    /// </summary>
    /// <param name="animate">是否启用动画效果</param>
    /// <param name="onComplete">完成后的回调函数</param>
    private void ApplyGravity(bool animate, Action onComplete)
    {
        // 初始化占用数组，记录每个位置的占用情况
        occupancy = new BlockData[level.Width, level.Height];
        // 按Y坐标升序，X坐标升序对所有方块进行排序
        // 这样可以从下往上处理方块，确保下落正确
        List<BlockData> sortedBlocks = allBlocks.OrderBy(block => block.Y).ThenBy(block => block.X).ToList();
        // 标记是否有方块发生了移动
        bool hasMovement = false;

        // 遍历所有已排序的方块
        foreach (BlockData block in sortedBlocks)
        {
            // 记录方块的原始Y坐标
            int oldY = block.Y;
            // 尝试将方块向下移动，直到无法继续下落
            while (CanMoveDown(block))
            {
                block.Y--;
            }

            // 标记方块当前占用的位置
            MarkOccupied(block);
            // 设置方块在网格中的位置，根据animate参数决定是否使用动画
            block.View.SetGridPosition(block, animate ? GravityAnimationDuration : 0f);
            // 检查方块是否发生了移动，或者是正在下落的生成方块
            hasMovement |= oldY != block.Y || droppingSpawnBlocks.Remove(block);
        }

        // 如果启用动画且有方块发生了移动
        if (animate && hasMovement)
        {
            // 创建DOTween序列动画
            Sequence sequence = DOTween.Sequence().SetTarget(this);
            // 设置动画间隔时间
            sequence.AppendInterval(GravityAnimationCompleteDelay);
            // 设置动画完成后的回调
            sequence.OnComplete(() => onComplete?.Invoke());
            return;
        }

        // 如果没有动画或没有移动，直接调用完成回调
        onComplete?.Invoke();
    }

    private bool IsBoardRowEmpty(int y)
    {
        return !allBlocks.Any(block => block.Y == y);
    }

    private void StartBoardFill(bool animate, Action onComplete, bool requestWhenBusy = true)
    {
        if (onComplete != null)
        {
            boardFillCompleteCallbacks += onComplete;
        }

        if (boardFillInProgress)
        {
            if (requestWhenBusy)
            {
                boardFillRequested = true;
            }

            return;
        }

        boardFillInProgress = true;
        boardFillRequested = false;
        FillEmptyRowsAfterGravity(animate, () => CompleteBoardFill(animate));
    }

    private void CompleteBoardFill(bool animate)
    {
        if (boardFillRequested)
        {
            boardFillRequested = false;
            FillEmptyRowsAfterGravity(animate, () => CompleteBoardFill(animate));
            return;
        }

        Action callbacks = boardFillCompleteCallbacks;
        boardFillCompleteCallbacks = null;
        boardFillInProgress = false;
        callbacks?.Invoke();
    }

    private void FillEmptyRowsAfterGravity(bool animate, Action onComplete)
    {
        currentBoardFillSpawnBlocks.Clear();
        int safetyLimit = Mathf.Max(level.Height * 2 + 8, level.Height + pendingSpawnBalls.Count + allBlocks.Count);
        for (int i = 0; i < safetyLimit; i++)
        {
            bool settledAny = SettleBlocksForGravity();
            bool spawnedAny = pendingSpawnBalls.Count > 0 && SpawnRowsAtTopBeforeGravity(animate);

            if (!settledAny && !spawnedAny)
            {
                break;
            }
        }

        ApplyBoardFillSpawnVisualOffsets(animate);
        ApplyGravity(animate, () =>
        {
            currentBoardFillSpawnBlocks.Clear();
            onComplete?.Invoke();
        });
    }

    private bool SettleBlocksForGravity()
    {
        occupancy = new BlockData[level.Width, level.Height];
        List<BlockData> sortedBlocks = allBlocks.OrderBy(block => block.Y).ThenBy(block => block.X).ToList();
        bool hasMovement = false;

        foreach (BlockData block in sortedBlocks)
        {
            int oldY = block.Y;
            while (CanMoveDown(block))
            {
                block.Y--;
            }

            MarkOccupied(block);
            if (oldY != block.Y)
            {
                hasMovement = true;
                droppingSpawnBlocks.Add(block);
            }
        }

        return hasMovement;
    }

    private bool SpawnRowsAtTopBeforeGravity(bool animate)
    {
        int topEmptyRowCount = GetTopEmptyRowCount();
        if (topEmptyRowCount == 0)
        {
            return false;
        }

        currentTopSpawnBlocks.Clear();
        collectingTopSpawnBlocks = true;
        int spawnedRowCount = 0;
        try
        {
            int startY = level.Height - topEmptyRowCount;
            for (int y = startY; y < level.Height && pendingSpawnBalls.Count > 0; y++)
            {
                if (!SpawnRow(y, animate))
                {
                    break;
                }

                spawnedRowCount++;
            }
        }
        finally
        {
            collectingTopSpawnBlocks = false;
        }

        AlignTopSpawnRowsAndApplyOffset(topEmptyRowCount, spawnedRowCount, animate);
        currentTopSpawnBlocks.Clear();
        return spawnedRowCount > 0;
    }

    private void AlignTopSpawnRowsAndApplyOffset(int topEmptyRowCount, int spawnedRowCount, bool animate)
    {
        if (spawnedRowCount <= 0)
        {
            return;
        }

        int rowShift = topEmptyRowCount - spawnedRowCount;
        for (int i = 0; i < currentTopSpawnBlocks.Count; i++)
        {
            BlockData block = currentTopSpawnBlocks[i];
            block.Y += rowShift;

            BlockView view = block.View;
            if (view != null)
            {
                view.SetGridPosition(block, 0f);
            }
        }
    }

    private void ApplyBoardFillSpawnVisualOffsets(bool animate)
    {
        if (!animate || currentBoardFillSpawnBlocks.Count == 0)
        {
            return;
        }

        List<int> orderedRows = currentBoardFillSpawnBlocks
            .Select(block => block.Y)
            .Distinct()
            .OrderBy(y => y)
            .ToList();

        Dictionary<int, int> rowOffsets = new Dictionary<int, int>(orderedRows.Count);
        for (int i = 0; i < orderedRows.Count; i++)
        {
            rowOffsets[orderedRows[i]] = orderedRows.Count - i;
        }

        for (int i = 0; i < currentBoardFillSpawnBlocks.Count; i++)
        {
            BlockData block = currentBoardFillSpawnBlocks[i];
            BlockView view = block.View;
            if (view != null)
            {
                view.SetGridPosition(block, 0f);
                view.RectTransform.anchoredPosition += Vector2.up * (rowOffsets[block.Y] * cellSize);
            }
        }
    }

    private int GetTopEmptyRowCount()
    {
        int count = 0;
        for (int y = level.Height - 1; y >= 0; y--)
        {
            if (!IsBoardRowEmpty(y))
            {
                break;
            }

            count++;
        }

        return count;
    }

    /// <summary>
    /// 在指定行生成一行方块
    /// </summary>
    /// <param name="boardY">棋盘的Y坐标</param>
    /// <param name="animate">是否使用动画效果生成</param>
    /// <returns>如果成功生成一行方块返回true，否则返回false</returns>
    private bool SpawnRow(int boardY, bool animate)
    {
        // 检查是否有待生成的方块
        if (pendingSpawnBalls.Count == 0)
        {
            return false;
        }

        // 创建一个随机可生成的方块行
        List<BlockDefinition> row = CreateRandomSpawnableRow(nextSourceRow, boardY);
        // 如果创建的行没有方块，则返回false
        if (row.Count == 0)
        {
            return false;
        }

        // 遍历行中的每个方块定义并生成方块
        foreach (BlockDefinition definition in row)
        {
            SpawnBlock(definition, animate);
        }

        // 增加下一行的源行索引
        nextSourceRow++;
        return true;
    }

    /// <summary>
    /// 创建待生成的足球池，根据关卡配置生成足球序列
    /// </summary>
    /// <param name="config">关卡配置对象，包含足球数量和类型进度等信息</param>
    private void CreatePendingSpawnBallPool(LevelConfig config)
    {
        List<FootballType> normalCandidates;
        List<FootballType> specialCandidates;
        List<FootballType> coinCandidates;
        CreateFootballTypeCandidates(out normalCandidates, out specialCandidates, out coinCandidates);

        CreateRoundFootballTypes(config, normalCandidates);
        CreateRoundSpecialFootballTypes(config, specialCandidates);

        List<FootballType> balls = new List<FootballType>();
        AddGroupedBalls(balls, roundFootballTypes, config.TotalBallCount, "普通球");
        AddEvenlyGroupedBalls(balls, roundSpecialFootballTypes, config.SpecialBallCount, "特殊球");
        AddCoinBalls(balls, coinCandidates, config.CoinCount);
        ShuffleBalls(balls);

        AddPendingSpawnBallColors(balls);

        pendingPrioritySpawnTypes.Clear();
        AddPrioritySpawnTypes(roundFootballTypes);
        AddPrioritySpawnTypes(roundSpecialFootballTypes);
    }

    /// <summary>
    /// 创建回合制足球类型
    /// 根据关卡配置生成并随机选择足球类型
    /// </summary>
    /// <param name="config">关卡配置对象，包含初始球类型数量和新增球类型进度等信息</param>
    private void CreateRoundFootballTypes(LevelConfig config, List<FootballType> normalTypes)
    {
        roundFootballTypes.Clear();
        if (normalTypes.Count == 0)
        {
            Debug.LogError("普通球候选为空，无法按关卡配置创建普通球池.");
            return;
        }

        ShuffleBalls(normalTypes);
        int requestedTypeCount = config.InitialBallTypeCount;
        int selectedTypeCount = Mathf.Clamp(requestedTypeCount, 1, normalTypes.Count);
        for (int i = 0; i < selectedTypeCount; i++)
        {
            roundFootballTypes.Add(normalTypes[i]);
        }
    }

    private void CreateRoundSpecialFootballTypes(LevelConfig config, List<FootballType> specialTypes)
    {
        roundSpecialFootballTypes.Clear();
        int requestedTypeCount = Mathf.Max(0, config.InitialSpecialBallTypeCount);
        if (requestedTypeCount <= 0 || config.SpecialBallCount <= 0)
        {
            return;
        }

        if (specialTypes.Count == 0)
        {
            Debug.LogError("特殊球候选为空，无法按关卡配置创建特殊球池.");
            return;
        }

        ShuffleBalls(specialTypes);
        int selectedTypeCount = Mathf.Clamp(requestedTypeCount, 0, specialTypes.Count);
        for (int i = 0; i < selectedTypeCount; i++)
        {
            roundSpecialFootballTypes.Add(specialTypes[i]);
        }
    }

    /// <summary>
    /// 创建足球类型候选列表，包括普通类型、特殊类型和金币类型
    /// </summary>
    /// <param name="normalTypes">普通足球类型列表</param>
    /// <param name="specialTypes">特殊足球类型列表</param>
    /// <param name="coinTypes">金币足球类型列表</param>
    private void CreateFootballTypeCandidates(out List<FootballType> normalTypes, out List<FootballType> specialTypes,
        out List<FootballType> coinTypes)
    {
        // 初始化三种类型的足球列表
        normalTypes = new List<FootballType>();
        specialTypes = new List<FootballType>();
        coinTypes = new List<FootballType>();

        // 获取足球配置类别
        BallConfigCategory category = GameTableManager.Instance?.Tables?.BallConfigCategory;
        if (category != null)
        {
            // 遍历所有足球配置
            List<BallConfig> configs = category.DataList;
            for (int i = 0; i < configs.Count; i++)
            {
                BallConfig ballConfig = configs[i];
                // 尝试解析足球类型
                if (!TryParseFootballType(ballConfig.Type, out FootballType type))
                {
                    Debug.LogError($"球表类型无法解析:{ballConfig.Type}");
                    continue; // 解析失败则跳过当前配置
                }

                // 根据类别添加到对应的足球类型列表中
                AddFootballTypeByCategory(type, ballConfig.TypeOfBall, normalTypes, specialTypes, coinTypes);
            }
        }

        // 如果任一类型列表为空，则添加默认的足球类型作为后备方案
        if (normalTypes.Count == 0 || specialTypes.Count == 0 || coinTypes.Count == 0)
        {
            AddFallbackFootballTypes(normalTypes, specialTypes, coinTypes);
        }
    }

    private static void AddFootballTypeByCategory(FootballType type, int category, List<FootballType> normalTypes,
        List<FootballType> specialTypes, List<FootballType> coinTypes)
    {
        if (type == FootballType.None)
        {
            return;
        }

        switch (category)
        {
            case CoinBallCategory:
                AddUniqueFootballType(coinTypes, type);
                break;
            case NormalBallCategory:
                if (type != FootballType.Glod)
                {
                    AddUniqueFootballType(normalTypes, type);
                }

                break;
            case SpecialBallCategory:
                if (type != FootballType.Glod)
                {
                    AddUniqueFootballType(specialTypes, type);
                }

                break;
            default:
                Debug.LogError($"未知球类别:{category}, type:{type}");
                break;
        }
    }

    private static void AddFallbackFootballTypes(List<FootballType> normalTypes, List<FootballType> specialTypes,
        List<FootballType> coinTypes)
    {
        Array values = Enum.GetValues(typeof(FootballType));
        for (int i = 0; i < values.Length; i++)
        {
            FootballType type = (FootballType) values.GetValue(i);
            if (type == FootballType.None)
            {
                continue;
            }

            if (type == FootballType.Glod)
            {
                AddUniqueFootballType(coinTypes, type);
                continue;
            }

            string typeName = Enum.GetName(typeof(FootballType), type);
            if (!string.IsNullOrEmpty(typeName) && typeName.StartsWith("Clothes", StringComparison.Ordinal))
            {
                AddUniqueFootballType(specialTypes, type);
            }
            else
            {
                AddUniqueFootballType(normalTypes, type);
            }
        }
    }

    private static void AddUniqueFootballType(List<FootballType> target, FootballType type)
    {
        if (!target.Contains(type))
        {
            target.Add(type);
        }
    }

    private static bool TryParseFootballType(string value, out FootballType type)
    {
        type = FootballType.None;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string safeValue = value.Trim().TrimEnd(',');
        return Enum.TryParse(safeValue, true, out type) && type != FootballType.None;
    }

    private static void AddGroupedBalls(List<FootballType> target, List<FootballType> selectedTypes, int requestedCount,
        string logName)
    {
        if (selectedTypes.Count == 0)
        {
            return;
        }

        int effectiveCount = Mathf.Max(0, requestedCount);
        int remainder = effectiveCount % MatchGroupSize;
        if (remainder != 0)
        {
            effectiveCount -= remainder;
            Debug.LogError($"{logName}数量:{requestedCount}不是{MatchGroupSize}的倍数. 使用{effectiveCount}.");
        }

        int minimumCount = selectedTypes.Count * MatchGroupSize;
        if (effectiveCount < minimumCount)
        {
            Debug.LogError($"{logName}数量:{requestedCount}不足以保证每种类型至少一组. 使用{minimumCount}.");
            effectiveCount = minimumCount;
        }

        List<FootballType> balls = new List<FootballType>(effectiveCount);
        for (int i = 0; i < selectedTypes.Count; i++)
        {
            AddFootballGroup(balls, selectedTypes[i]);
        }

        for (int i = balls.Count; i < effectiveCount; i += MatchGroupSize)
        {
            FootballType type = selectedTypes[UnityEngine.Random.Range(0, selectedTypes.Count)];
            AddFootballGroup(balls, type);
        }

        ShuffleBalls(balls);
        target.AddRange(balls);
    }

    private static void AddEvenlyGroupedBalls(List<FootballType> target, List<FootballType> selectedTypes,
        int requestedCount, string logName)
    {
        if (selectedTypes.Count == 0)
        {
            return;
        }

        int effectiveCount = Mathf.Max(0, requestedCount);
        int remainder = effectiveCount % MatchGroupSize;
        if (remainder != 0)
        {
            effectiveCount -= remainder;
            Debug.LogError($"{logName}数量:{requestedCount}不是{MatchGroupSize}的倍数. 使用{effectiveCount}.");
        }

        int minimumCount = selectedTypes.Count * MatchGroupSize;
        if (effectiveCount < minimumCount)
        {
            Debug.LogError($"{logName}数量:{requestedCount}不足以保证每种类型至少一组. 使用{minimumCount}.");
            effectiveCount = minimumCount;
        }

        int groupCount = effectiveCount / MatchGroupSize;
        int baseGroupCount = groupCount / selectedTypes.Count;
        int extraGroupCount = groupCount % selectedTypes.Count;

        List<FootballType> balls = new List<FootballType>(effectiveCount);
        for (int i = 0; i < selectedTypes.Count; i++)
        {
            int typeGroupCount = baseGroupCount + (i < extraGroupCount ? 1 : 0);
            for (int groupIndex = 0; groupIndex < typeGroupCount; groupIndex++)
            {
                AddFootballGroup(balls, selectedTypes[i]);
            }
        }

        ShuffleBalls(balls);
        target.AddRange(balls);
    }

    private static void AddFootballGroup(List<FootballType> target, FootballType type)
    {
        for (int i = 0; i < MatchGroupSize; i++)
        {
            target.Add(type);
        }
    }

    private static void AddCoinBalls(List<FootballType> target, List<FootballType> coinTypes, int coinCount)
    {
        int safeCoinCount = Mathf.Max(0, coinCount);
        if (safeCoinCount <= 0)
        {
            return;
        }

        FootballType coinType = coinTypes.Count > 0 ? coinTypes[0] : FootballType.Glod;
        for (int i = 0; i < safeCoinCount; i++)
        {
            target.Add(coinType);
        }
    }

    private void AddPrioritySpawnTypes(IReadOnlyList<FootballType> types)
    {
        for (int i = 0; i < types.Count; i++)
        {
            pendingPrioritySpawnTypes.Add(types[i]);
        }
    }

    /// <summary>
    /// 获取随机足球类型
    /// </summary>
    /// <param name="maxTypeCount">最大可选择的足球类型数量</param>
    /// <returns>返回一个随机的足球类型</returns>
    private FootballType GetRandomRoundFootballType(int maxTypeCount)
    {
        // 如果当前回合的足球类型列表为空
        if (roundFootballTypes.Count == 0)
        {
            // 获取普通的足球类型列表
            List<FootballType> normalTypes = GetNormalFootballTypes();
            // 如果普通类型列表不为空，则随机返回一个普通类型
            // 否则返回默认的Ball1类型
            return normalTypes.Count > 0
                ? normalTypes[UnityEngine.Random.Range(0, normalTypes.Count)]
                : FootballType.Ball1;
        }

        // 计算可用的足球类型数量，确保在1到列表最大长度之间
        int availableTypeCount = Mathf.Clamp(maxTypeCount, 1, roundFootballTypes.Count);
        // 从可用类型中随机返回一个足球类型
        return roundFootballTypes[UnityEngine.Random.Range(0, availableTypeCount)];
    }

    /// <summary>
    /// 获取常规的足球类型列表
    /// </summary>
    /// <returns>返回一个包含所有常规足球类型的列表，排除特殊类型Glod</returns>
    private static List<FootballType> GetNormalFootballTypes()
    {
        List<FootballType> normalTypes = new List<FootballType>();
        List<FootballType> specialTypes = new List<FootballType>();
        List<FootballType> coinTypes = new List<FootballType>();
        AddFallbackFootballTypes(normalTypes, specialTypes, coinTypes);
        return normalTypes;
    }

    private void AddPendingSpawnBallColors(IReadOnlyList<FootballType> balls)
    {
        for (int i = 0; i < balls.Count; i++)
        {
            pendingSpawnBalls.Add(ClampFootballColor(balls[i]));
        }
    }

    private void ReturnBallToPendingPool(FootballType type)
    {
        pendingSpawnBalls.Add(ClampFootballColor(type));
    }

    private void TryFillRowsAfterAddingPendingBalls()
    {
        if (!initialized || level == null || inputLocked)
        {
            return;
        }

        StartBoardFill(false, () => SetStatus("Tap a football"));
    }

    /// <summary>
    /// 检查是否有活动的等待区域操作
    /// </summary>
    public bool HasActiveWaitingAreaOperation()
    {
        // 检查是否正在处理等待区域或者有球正在移动到等待区域
        return waitingAreaProcessing || movingToWaitingBalls.Count > 0;
    }

    /// <summary>
    /// 将足球颜色类型限制在有效范围内
    /// </summary>
    /// <param name="type">要限制的足球颜色类型</param>
    /// <returns>限制在有效范围内的足球颜色类型</returns>
    private static FootballType ClampFootballColor(FootballType type)
    {
        // 获取FootballType枚举中值的数量
        int colorCount = Enum.GetValues(typeof(FootballType)).Length;
        // 将类型值限制在0到枚举最大值之间，确保索引有效
        int colorIndex = Mathf.Clamp((int) type, 0, colorCount - 1);
        // 将处理后的索引转换回FootballType并返回
        return (FootballType) colorIndex;
    }

    /// <summary>
    /// 将洗牌后的足球段添加到目标列表中
    /// </summary>
    /// <param name="target">目标列表，用于接收洗牌后的足球段</param>
    /// <param name="segment">要添加的足球段，在添加前会进行洗牌</param>
    private static void AppendShuffledSegment(List<FootballType> target, List<FootballType> segment)
    {
        if (segment.Count == 0)
        {
            return;
        }

        ShuffleBalls(segment);
        target.AddRange(segment);
        segment.Clear();
    }

    /// <summary>
    /// 使用Fisher-Yates洗牌算法随机打乱足球列表的顺序
    /// </summary>
    /// <param name="balls">要打乱的足球类型列表</param>
    private static void ShuffleBalls(List<FootballType> balls)
    {
        // 从列表的第一个元素开始，到最后一个元素结束
        for (int i = 0; i < balls.Count; i++)
        {
            // 随机选择一个索引位置，范围从当前位置i到列表末尾
            int swapIndex = UnityEngine.Random.Range(i, balls.Count);
            // 保存当前位置的值，用于后续交换
            FootballType temp = balls[i];
            // 将随机位置的值赋给当前位置
            balls[i] = balls[swapIndex];
            // 将之前保存的当前位置值赋给随机位置，完成交换
            balls[swapIndex] = temp;
        }
    }

    private void ReturnBoardBallsToPendingPoolAndRefill(List<FootballData> boardBalls)
    {
        if (boardBalls == null || boardBalls.Count == 0)
        {
            ShuffleBalls(pendingSpawnBalls);
            return;
        }

        for (int i = 0; i < boardBalls.Count; i++)
        {
            pendingSpawnBalls.Add(ClampFootballColor(boardBalls[i].Type));
        }

        ShuffleBalls(pendingSpawnBalls);

        for (int i = 0; i < boardBalls.Count; i++)
        {
            FootballData ball = boardBalls[i];
            ball.Type = TakeNextSpawnBall();

            if (ball.View == null)
            {
                continue;
            }

            ball.View.RefreshColor();
        }
    }

    private void AnimateShuffleBoardBalls(List<FootballData> boardBalls, Vector3 centerWorldPosition,
        Action onComplete)
    {
        List<ShuffleBallAnimationState> states = new List<ShuffleBallAnimationState>(boardBalls.Count);
        for (int i = 0; i < boardBalls.Count; i++)
        {
            FootballView view = boardBalls[i].View;
            if (view == null || view.RectTransform == null)
            {
                continue;
            }

            RectTransform rect = view.RectTransform;
            states.Add(new ShuffleBallAnimationState(rect.parent, rect.GetSiblingIndex(),
                rect.anchoredPosition, rect.localScale, rect.position, view));
            view.SetInteractable(false);
            if (flyingRoot != null)
            {
                rect.SetParent(flyingRoot, true);
            }
        }

        if (states.Count == 0)
        {
            ReturnBoardBallsToPendingPoolAndRefill(boardBalls);
            DOVirtual.DelayedCall(0.2f, () => onComplete?.Invoke()).SetTarget(this);
            return;
        }

        Sequence sequence = DOTween.Sequence().SetTarget(this);
        for (int i = 0; i < states.Count; i++)
        {
            sequence.Join(states[i].View.transform.DOMove(centerWorldPosition, 0.28f)
                .SetEase(Ease.InCubic));
            sequence.Join(states[i].View.transform.DOScale(states[i].LocalScale * 0.82f, 0.28f)
                .SetEase(Ease.InCubic));
        }

        sequence.AppendCallback(() => ReturnBoardBallsToPendingPoolAndRefill(boardBalls));

        for (int i = 0; i < states.Count; i++)
        {
            ShuffleBallAnimationState state = states[i];
            sequence.Join(state.View.transform.DOMove(state.WorldPosition, 0.32f)
                .SetEase(Ease.OutCubic));
            sequence.Join(state.View.transform.DOScale(state.LocalScale, 0.32f)
                .SetEase(Ease.OutCubic));
        }

        sequence.OnComplete(() =>
        {
            for (int i = 0; i < states.Count; i++)
            {
                RestoreShuffleBall(states[i]);
            }

            onComplete?.Invoke();
        });
    }

    private void RestoreShuffleBall(ShuffleBallAnimationState state)
    {
        if (state.View == null || state.View.RectTransform == null)
        {
            return;
        }

        RectTransform rect = state.View.RectTransform;
        if (state.Parent != null)
        {
            rect.SetParent(state.Parent, true);
            rect.SetSiblingIndex(state.SiblingIndex);
        }

        rect.anchoredPosition = state.AnchoredPosition;
        rect.localScale = state.LocalScale;
        state.View.SetInteractable(true);
    }

    /// <summary>
    /// 获取游戏板上所有的球
    /// </summary>
    /// <returns>返回包含游戏板上所有球的列表</returns>
    private List<FootballData> GetBoardBalls()
    {
        // 创建一个列表，用于存储所有的球
        List<FootballData> boardBalls = new List<FootballData>();
        // 遍历所有的块，将每个块中的球添加到boardBalls列表中
        for (int i = 0; i < allBlocks.Count; i++)
        {
            boardBalls.AddRange(allBlocks[i].Footballs);
        }

        return boardBalls;
    }

    private int GetBoardBallCount()
    {
        int count = 0;
        for (int i = 0; i < allBlocks.Count; i++)
        {
            count += allBlocks[i].Footballs.Count;
        }

        return count;
    }

    /// <summary>
    /// 替换待生成的足球颜色列表
    /// </summary>
    /// <param name="colors">新的足球颜色列表</param>
    private void ReplacePendingSpawnBalls(IReadOnlyList<FootballType> colors)
    {
        // 清空当前待生成的足球列表
        pendingSpawnBalls.Clear();
        // 添加新的足球颜色到待生成列表
        AddPendingSpawnBallColors(colors);
    }

    /// <summary>
    /// 创建一个可随机生成的行
    /// </summary>
    /// <param name="sourceRow">源行号</param>
    /// <param name="boardY">棋盘Y坐标</param>
    /// <returns>返回一个BlockDefinition列表，表示生成的行</returns>
    private List<BlockDefinition> CreateRandomSpawnableRow(int sourceRow, int boardY)
    {
        // 尝试最多20次创建一个有效的行
        for (int i = 0; i < 20; i++)
        {
            // 创建随机行宽列表
            List<int> widths = level.CreateRandomRowWidths(pendingSpawnBalls.Count);
            // 创建足球类型的行--里面冲待生成球池里取球
            List<FootballType> selectedBalls;
            List<FootballType[]> rowFootballs = CreateRowFootballs(widths, out selectedBalls);
            // 创建随机行
            List<BlockDefinition> row = level.CreateRandomRow(sourceRow, boardY, rowFootballs);
            // 检查是否可以放置所有方块定义
            if (row.All(CanPlaceBlockDefinition))
            {
                //移除待生成的球
                RemovePendingSpawnBalls(selectedBalls);
                return row;
            }
        }

        // 如果无法创建有效行，则创建一个备选方案
        List<BlockDefinition> fallback = new List<BlockDefinition>();
        // 查找空闲单元格
        List<int> freeCells = new List<int>();
        for (int x = 0; x < level.Width; x++)
        {
            if (!IsCellOccupied(x, boardY))
            {
                freeCells.Add(x);
            }
        }

        // 如果没有空闲单元格，返回空列表
        if (freeCells.Count == 0)
        {
            return fallback;
        }

        // 在随机空闲单元格放置一个方块
        int fallbackX = freeCells[UnityEngine.Random.Range(0, freeCells.Count)];
        fallback.Add(new BlockDefinition(fallbackX, boardY, 1, sourceRow, TakeNextSpawnBall()));
        return fallback;
    }

    /// <summary>
    /// 根据给定的宽度列表创建足球行
    /// </summary>
    /// <param name="widths">每行的宽度（每行足球数量）列表</param>
    /// <returns>返回一个FootballType二维数组列表，每个子数组代表一行足球</returns>
    private List<FootballType[]> CreateRowFootballs(List<int> widths, out List<FootballType> selectedBalls)
    {
        selectedBalls = SelectPendingSpawnBallsPreview(GetWidthTotal(widths));
        // 回结果列表，容量为宽度列表的长度
        List<FootballType[]> rowFootballs = new List<FootballType[]>(widths.Count);
        // 当前处理的足球索引
        int ballIndex = 0;

        // 遍历每一行的宽度
        for (int i = 0; i < widths.Count; i++)
        {
            // 计算当前行可以放置的足球数量，取最小值（当前行宽度 或 剩余足球数量）
            int count = Mathf.Min(widths[i], selectedBalls.Count - ballIndex);
            // 如果没有足球可放置，则退出循环
            if (count <= 0)
            {
                break;
            }

            // 创建当前行的足球数组
            FootballType[] footballs = new FootballType[count];
            // 从待生成足球数组中复制指定数量的足球到当前行
            selectedBalls.CopyTo(ballIndex, footballs, 0, count);
            // 将当前行添加到结果列表中
            rowFootballs.Add(footballs);
            // 更新足球索引
            ballIndex += count;
        }

        // 返回所有足球行的列表
        return rowFootballs;
    }

    /// <summary>
    /// 从待生成球列表中移除指定数量的球
    /// </summary>
    /// <param name="count">要移除的球的数量</param>
    private void RemovePendingSpawnBalls(IReadOnlyList<FootballType> balls)
    {
        for (int i = 0; i < balls.Count; i++)
        {
            RemovePendingSpawnBall(balls[i]);
        }
    }

    private List<FootballType> SelectPendingSpawnBallsPreview(int count)
    {
        int selectCount = Mathf.Min(Mathf.Max(0, count), pendingSpawnBalls.Count);
        List<FootballType> previewPool = new List<FootballType>(pendingSpawnBalls);
        HashSet<FootballType> previewPriorityTypes = new HashSet<FootballType>(pendingPrioritySpawnTypes);
        HashSet<FootballType> allowedTypes = CreateAllowedSpawnTypes();
        List<FootballType> selectedBalls = new List<FootballType>(selectCount);

        for (int i = 0; i < selectCount; i++)
        {
            int index = GetNextPendingSpawnBallIndex(previewPool, previewPriorityTypes, allowedTypes);
            FootballType type = previewPool[index];
            selectedBalls.Add(type);
            previewPool.RemoveAt(index);
            previewPriorityTypes.Remove(type);
        }

        return selectedBalls;
    }

    private void RemovePendingSpawnBall(FootballType type)
    {
        int index = pendingSpawnBalls.IndexOf(type);
        if (index < 0)
        {
            return;
        }

        pendingSpawnBalls.RemoveAt(index);
        pendingPrioritySpawnTypes.Remove(type);
    }

    /// <summary>
    /// 获取下一个要生成的足球类型
    /// </summary>
    /// <returns>返回下一个足球类型</returns>
    private FootballType TakeNextSpawnBall()
    {
        return TakePendingSpawnBallAt(
            GetNextPendingSpawnBallIndex(pendingSpawnBalls, pendingPrioritySpawnTypes, CreateAllowedSpawnTypes()));
    }

    /// <summary>
    /// 从待生成足球列表中指定位置获取足球类型
    /// </summary>
    /// <param name="index">要获取的足球在列表中的索引位置</param>
    /// <returns>返回指定位置的足球类型，如果列表为空则返回默认的Ball1</returns>
    private FootballType TakePendingSpawnBallAt(int index)
    {
        // 检查待生成足球列表是否为空
        if (pendingSpawnBalls.Count == 0)
        {
            return FootballType.Ball1;
        }

        // 确保索引在有效范围内，防止数组越界
        int safeIndex = Mathf.Clamp(index, 0, pendingSpawnBalls.Count - 1);
        // 获取指定索引位置的足球类型
        FootballType type = pendingSpawnBalls[safeIndex];
        // 从列表中移除已获取的足球类型
        pendingSpawnBalls.RemoveAt(safeIndex);
        pendingPrioritySpawnTypes.Remove(type);
        return type;
    }

    private int GetUnlockedSpecialTypeCount()
    {
        if (levelConfig == null || levelConfig.AddBallTypeProgresses == null)
        {
            return 0;
        }

        int unlockedCount = 0;
        float progress = Progress;
        for (int i = 0; i < levelConfig.AddBallTypeProgresses.Length; i++)
        {
            if (progress >= levelConfig.AddBallTypeProgresses[i])
            {
                unlockedCount++;
            }
        }

        return Mathf.Clamp(unlockedCount, 0, roundSpecialFootballTypes.Count);
    }

    private HashSet<FootballType> CreateAllowedSpawnTypes()
    {
        HashSet<FootballType> allowedTypes = new HashSet<FootballType>();
        for (int i = 0; i < roundFootballTypes.Count; i++)
        {
            allowedTypes.Add(roundFootballTypes[i]);
        }

        int unlockedSpecialTypeCount = GetUnlockedSpecialTypeCount();
        for (int i = 0; i < unlockedSpecialTypeCount; i++)
        {
            allowedTypes.Add(roundSpecialFootballTypes[i]);
        }

        allowedTypes.Add(FootballType.Glod);
        return allowedTypes;
    }

    private static int GetNextPendingSpawnBallIndex(IReadOnlyList<FootballType> balls,
        HashSet<FootballType> priorityTypes, HashSet<FootballType> allowedTypes)
    {
        if (balls.Count == 0)
        {
            return -1;
        }

        List<int> allowedIndices = new List<int>();
        for (int i = 0; i < balls.Count; i++)
        {
            if (allowedTypes.Contains(balls[i]))
            {
                allowedIndices.Add(i);
            }
        }

        if (allowedIndices.Count == 0)
        {
            return UnityEngine.Random.Range(0, balls.Count);
        }

        if (priorityTypes.Count > 0)
        {
            List<int> priorityIndices = new List<int>();
            for (int i = 0; i < allowedIndices.Count; i++)
            {
                int ballIndex = allowedIndices[i];
                if (priorityTypes.Contains(balls[ballIndex]))
                {
                    priorityIndices.Add(ballIndex);
                }
            }

            if (priorityIndices.Count > 0)
            {
                return priorityIndices[UnityEngine.Random.Range(0, priorityIndices.Count)];
            }
        }

        return allowedIndices[UnityEngine.Random.Range(0, allowedIndices.Count)];
    }

    private static int GetWidthTotal(IReadOnlyList<int> widths)
    {
        int total = 0;
        for (int i = 0; i < widths.Count; i++)
        {
            total += widths[i];
        }

        return total;
    }

    /// <summary>
    /// 计算所有方块定义中足球的总数量
    /// </summary>
    /// <param name="definitions">方块定义列表</param>
    /// <returns>所有方块定义中足球的总数量</returns>
    private static int GetDefinitionBallCount(List<BlockDefinition> definitions)
    {
        int count = 0; // 初始化足球数量计数器
        // 遍历所有方块定义
        for (int i = 0; i < definitions.Count; i++)
        {
            // 累加当前方块定义中的足球数量
            count += definitions[i].Footballs.Length;
        }

        return count; // 返回计算得到的总足球数量
    }

    /// <summary>
    /// 检查是否可以放置方块定义
    /// </summary>
    /// <param name="definition">要检查的方块定义</param>
    /// <returns>如果可以放置返回true，否则返回false</returns>
    private bool CanPlaceBlockDefinition(BlockDefinition definition)
    {
        // 遍历方块定义的宽度范围
        for (int x = definition.X; x < definition.X + definition.Width; x++)
        {
            // 检查当前列是否已被占用
            if (IsCellOccupied(x, definition.Y))
            {
                return false; // 如果有列被占用，则不能放置方块
            }
        }

        return true; // 所有列都未被占用，可以放置方块
    }

    /// <summary>
    /// 检查指定坐标的单元格是否被占用
    /// </summary>
    /// <param name="x">要检查的x坐标（列）</param>
    /// <param name="y">要检查的y坐标（行）</param>
    /// <returns>如果单元格被占用返回true，否则返回false</returns>
    private bool IsCellOccupied(int x, int y)
    {
        // 检查是否有任何块的y坐标与给定的y坐标相同
        // 并且x坐标在块的x坐标范围内（从block.X到block.X + block.Width）
        return allBlocks.Any(block => block.Y == y && x >= block.X && x < block.X + block.Width);
    }

    private bool CanMoveDown(BlockData block)
    {
        if (block.Y <= 0)
        {
            return false;
        }

        for (int x = block.X; x < block.X + block.Width; x++)
        {
            if (occupancy[x, block.Y - 1] != null)
            {
                return false;
            }
        }

        return true;
    }

    private void MarkOccupied(BlockData block)
    {
        for (int x = block.X; x < block.X + block.Width; x++)
        {
            occupancy[x, block.Y] = block;
        }
    }

    private void ProcessWaitingArea()
    {
        if (waitingAreaProcessing)
        {
            waitingAreaProcessRequested = true;
            return;
        }

        waitingAreaProcessing = true;
        ProcessWaitingAreaStep();
    }

    private void ProcessWaitingAreaStep()
    {
        List<FootballData> match = FindWaitingAreaMatch();

        if (match == null)
        {
            CompleteWaitingAreaProcessing();
            return;
        }

        if (match.Any(ball => autoBackupBalls.Contains(ball)))
        {
            AnimateAutoBackupRemoveAndReflow(match, ProcessWaitingAreaStep);
            return;
        }

        Sequence sequence = DOTween.Sequence().SetTarget(this);
        foreach (FootballData ball in match)
        {
            sequence.Join(ball.View.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
        }

        sequence.OnComplete(() =>
        {
            RemoveMatchedBalls(match);
            RefreshWaitSlots();
            ReflowWaitingBalls(ProcessWaitingAreaStep);
        });
    }

    private List<FootballData> FindWaitingAreaMatch()
    {
        return waitingBalls
            .Where(ball => !movingToWaitingBalls.Contains(ball))
            .Concat(autoBackupBalls)
            .GroupBy(ball => ball.Type)
            .Select(group => group.Take(MatchGroupSize).ToList())
            .FirstOrDefault(group => group.Count == MatchGroupSize);
    }

    private bool HasWaitingAreaMatch()
    {
        return FindWaitingAreaMatch() != null;
    }

    private void HandleFullWaitingArea()
    {
        if (HasWaitingAreaMatch())
        {
            ProcessWaitingArea();
            return;
        }

        SetStatus("Wait area full. Tap Restart");
        Debug.Log("Football match prototype: wait area full.");
        inputLocked = true;
        if (reviveUseCount >= MaxReviveCount)
        {
            OpenResultWindow(ResultWindowType.LoseWindow);
        }
        else
        {
            OpenReviveWindow();
        }
    }

    private void CompleteWaitingAreaProcessing()
    {
        waitingAreaProcessing = false;
        if (waitingAreaProcessRequested)
        {
            waitingAreaProcessRequested = false;
            ProcessWaitingArea();
            return;
        }

        StartBoardFill(true, FinishTurn, false);
    }

    /// <summary>
    /// 移除匹配的足球
    /// </summary>
    /// <param name="match">匹配到的足球列表</param>
    private void RemoveMatchedBalls(List<FootballData> match)
    {
        YangAudioManager.Instance.PlaySoundAudio("merge sound");
        // 注册连击
        RegisterCombo();
        // 增加已移除的匹配足球计数
        matchedRemovedBallCount += match.Count;
        // 检查是否为金钱奖励匹配
        bool isMoneyRewardMatch = IsMoneyRewardMatch(match);
        Vector3 matchWorldPosition = GetMatchWorldPosition(match);

        // 遍历所有匹配的足球
        foreach (FootballData ball in match)
        {
            // 从等待列表中移除足球
            waitingBalls.Remove(ball);

            // 检查并处理自动备份列表中的足球
            int backupIndex = autoBackupBalls.IndexOf(ball);
            if (backupIndex >= 0)
            {
                // 从自动备份列表中移除足球
                autoBackupBalls.RemoveAt(backupIndex);
                // 销毁对应位置的自动备份槽位
                DestroyAutoBackupSlotAt(backupIndex);
            }

            // 销毁足球的视图游戏对象
            Destroy(ball.View.gameObject);
        }

        // 如果是金钱奖励匹配
        if (isMoneyRewardMatch)
        {
            // 增加金钱奖励匹配计数
            moneyRewardMatchCount++;
            // 根据匹配次数决定是否打开金钱奖励窗口（每3次匹配打开一次）
            if (!IsLevelCleared())
            {
                OpenMoneyRewardWindow(moneyRewardMatchCount % 3 == 0);
            }
        }
        else
        {
            normalMatchCoinRewardHandler?.Invoke(matchWorldPosition);
            BagMgr.Instance.AddBagProp(CoinPropId, 1);
        }
        PlatformMgr.Instance.Shake();
    }

    private Vector3 GetMatchWorldPosition(List<FootballData> match)
    {
        if (match == null || match.Count == 0)
        {
            return transform.position;
        }

        Vector3 position = Vector3.zero;
        int count = 0;
        for (int i = 0; i < match.Count; i++)
        {
            if (match[i]?.View == null)
            {
                continue;
            }

            position += match[i].View.transform.position;
            count++;
        }

        return count > 0 ? position / count : transform.position;
    }

    private bool IsMoneyRewardMatch(List<FootballData> match)
    {
        if (match == null || match.Count != MatchGroupSize)
        {
            return false;
        }

        for (int i = 0; i < match.Count; i++)
        {
            if (match[i].Type != FootballType.Glod)
            {
                return false;
            }
        }

        return true;
    }

    private void OpenMoneyRewardWindow(bool canDoubleGet)
    {
        float currentMoney = BagMgr.Instance.GetBagPropCount(1);
        if (!MoneyRewardCalculator.TryCalculate(currentMoney, out float reward, out int decimalPlaces))
        {
            return;
        }

        MoneyRewardWindowData data = new MoneyRewardWindowData(reward, decimalPlaces, canDoubleGet);
        UIMonoInstance.OpenPanel<MoneyRewardWindow>(GroupType.弹窗1, data);
    }

    private int InsertBallIntoWaitingArea(FootballData ball)
    {
        int insertIndex = GetWaitingInsertIndex(ball);
        waitingBalls.Insert(insertIndex, ball);
        return insertIndex;
    }

    private int GetWaitingInsertIndex(FootballData ball)
    {
        int insertIndex = waitingBalls.Count;
        for (int i = 0; i < waitingBalls.Count; i++)
        {
            if (waitingBalls[i].Type == ball.Type)
            {
                insertIndex = i + 1;
            }
        }

        return insertIndex;
    }

    private void CompleteMovingToWaitingBall(FootballData ball, Action afterComplete)
    {
        MoveWaitingBallToCurrentSlot(ball, 0.08f, () =>
        {
            ball.View?.StopMoveTrail();
            movingToWaitingBalls.Remove(ball);
            afterComplete?.Invoke();
        });
    }

    private void MoveWaitingBallToCurrentSlot(FootballData ball, float duration, Action onComplete = null)
    {
        int index = waitingBalls.IndexOf(ball);
        if (index < 0 || index >= waitSlots.Count || ball.View == null)
        {
            onComplete?.Invoke();
            return;
        }

        Vector3 target = waitSlots[index].transform.position;
        if (duration <= 0f)
        {
            ball.View.transform.position = target;
            onComplete?.Invoke();
            return;
        }

        ball.View.transform.DOMove(target, duration)
            .SetEase(Ease.OutCubic)
            .SetTarget(this)
            .OnComplete(() => onComplete?.Invoke());
    }

    private void JoinWaitingBallReflow(Sequence sequence, FootballData excludedBall, float duration)
    {
        for (int i = 0; i < waitingBalls.Count; i++)
        {
            FootballData ball = waitingBalls[i];
            if (ball == excludedBall || ball.View == null || movingToWaitingBalls.Contains(ball))
            {
                continue;
            }

            sequence.Join(ball.View.transform.DOMove(waitSlots[i].transform.position, duration)
                .SetEase(Ease.OutCubic));
        }
    }

    private void ReflowWaitingBalls(Action onComplete)
    {
        Sequence sequence = DOTween.Sequence().SetTarget(this);
        JoinWaitingBallReflow(sequence, null, 0.18f);

        sequence.OnComplete(() => onComplete?.Invoke());
    }

    private void ReflowAutoBackupBalls(Action onComplete)
    {
        Sequence sequence = DOTween.Sequence().SetTarget(this);
        List<AutoBackupSlotLayout> layouts = CalculateAutoBackupSlotLayout(autoBackupSlots.Count);

        for (int i = 0; i < autoBackupSlots.Count; i++)
        {
            WaitSlotView slot = autoBackupSlots[i];
            slot.SetIndex(i);
            slot.SetOccupied(true);
            sequence.Join(AnimateAutoBackupSlotTo(slot, layouts[i], AutoBackupReflowDuration));
        }

        for (int i = 0; i < autoBackupBalls.Count; i++)
        {
            if (autoBackupBalls[i].View == null)
            {
                continue;
            }

            sequence.Join(autoBackupBalls[i].View.transform
                .DOMove(GetAutoBackupSlotWorldPosition(layouts[i]), AutoBackupReflowDuration)
                .SetEase(Ease.OutCubic));
        }

        sequence.OnComplete(() =>
        {
            ApplyAutoBackupLayoutImmediate();
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 完成回合
    /// </summary>
    private void FinishTurn()
    {
        if (gameEnded)
        {
            return;
        }

        if (IsLevelCleared())
        {
            SetStatus("Victory! Tap Restart");
            Debug.Log("Football match prototype: victory.");
            inputLocked = true;
            AdvanceSavedLevel();
            ApplyCurrentLevelConfig();
            OpenResultWindow(ResultWindowType.WinWindow);
            return;
        }

        if (waitingBalls.Count >= ActiveWaitSlotCount)
        {
            HandleFullWaitingArea();
            return;
        }

        inputLocked = false;
        SetStatus("Tap a football");
    }

    private bool IsLevelCleared()
    {
        return pendingSpawnBalls.Count == 0 && allBlocks.Count == 0 && waitingBalls.Count == 0 &&
               autoBackupBalls.Count == 0;
    }

    private async void OpenResultWindow(ResultWindowType windowType)
    {
        if (gameEnded)
        {
            return;
        }

        ResetCombo();
        gameEnded = true;
        //GameResultWindowData data = new GameResultWindowData(RestartGame);
        switch (windowType)
        {
            case ResultWindowType.WinWindow:
                WinWindowData temp = new WinWindowData();
                temp.RestartAction = () => RestartGame();
                await UIMonoInstance.OpenPanel<WinWindow>(GroupType.弹窗1, userData: temp);
                break;
            case ResultWindowType.LoseWindow:
                LoseWindowData temp2 = new LoseWindowData();
                temp2.RestartAction = () => RestartGame();
                await UIMonoInstance.OpenPanel<LoseWindow>(GroupType.弹窗1, userData: temp2);
                break;
            case ResultWindowType.ReviveWindow:
                ReviveWindowData data = new ReviveWindowData(ConsumeReviveAndApply, GiveUpRevive,
                    MaxReviveCount - reviveUseCount, MaxReviveCount);
                await UIMonoInstance.OpenPanel<ReviveWindow>(GroupType.弹窗1, userData: data);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(windowType), windowType, null);
        }
    }

    private async void OpenReviveWindow()
    {
        if (gameEnded || reviveUseCount >= MaxReviveCount || reviveWindowOpening || currentReviveWindow != null)
        {
            return;
        }

        reviveWindowOpening = true;
        ReviveWindowData data = new ReviveWindowData(ConsumeReviveAndApply, GiveUpRevive,
            MaxReviveCount - reviveUseCount, MaxReviveCount);
        try
        {
            (int id, IUGUIPanel panel) result =
                await UIMonoInstance.OpenPanel<ReviveWindow>(GroupType.弹窗1, userData: data);
            currentReviveWindow = result.panel as ReviveWindow;
        }
        catch
        {
            reviveWindowOpening = false;
            currentReviveWindow = null;
            throw;
        }
    }

    private void ConsumeReviveAndApply()
    {
        if (gameEnded)
        {
            return;
        }

        if (reviveUseCount >= MaxReviveCount)
        {
            currentReviveWindow = null;
            reviveWindowOpening = false;
            OpenResultWindow(ResultWindowType.LoseWindow);
            return;
        }

        reviveUseCount++;
        currentReviveWindow = null;
        reviveWindowOpening = false;
        ApplyRevive();
    }

    private void GiveUpRevive()
    {
        currentReviveWindow = null;
        reviveWindowOpening = false;
        ResetCombo();
        inputLocked = true;
        OpenResultWindow(ResultWindowType.LoseWindow);
    }

    private void RegisterCombo()
    {
        float now = Time.time;
        comboCount = now - lastComboMatchTime <= ComboTimeoutSeconds ? comboCount + 1 : 1;
        lastComboMatchTime = now;

        if (comboCount >= ComboDisplayStartCount)
        {
            ComboWindow.ShowCombo(comboCount);
        }
    }

    private void ResetCombo()
    {
        comboCount = 0;
        lastComboMatchTime = float.NegativeInfinity;
        ComboWindow.Hide();
    }

    private void ApplyRevive()
    {
        if (waitingBalls.Count == 0)
        {
            CompleteRevive();
            return;
        }

        List<FootballData> revivedBalls = new List<FootballData>(waitingBalls);
        waitingBalls.Clear();
        RefreshWaitSlots();

        for (int i = 0; i < revivedBalls.Count; i++)
        {
            ReturnBallToPendingPool(revivedBalls[i].Type);
        }

        ShuffleBalls(pendingSpawnBalls);

        Sequence sequence = DOTween.Sequence().SetTarget(this);
        bool hasAnimation = false;
        for (int i = 0; i < revivedBalls.Count; i++)
        {
            FootballData ball = revivedBalls[i];
            if (ball.View == null)
            {
                continue;
            }

            ball.View.SetInteractable(false);
            sequence.Join(ball.View.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack));
            hasAnimation = true;
        }

        if (!hasAnimation)
        {
            sequence.Kill();
            CompleteRevive();
            return;
        }

        sequence.OnComplete(() =>
        {
            for (int i = 0; i < revivedBalls.Count; i++)
            {
                if (revivedBalls[i].View != null)
                {
                    Destroy(revivedBalls[i].View.gameObject);
                }
            }

            CompleteRevive();
        });
    }

    private void CompleteRevive()
    {
        inputLocked = false;
        SetStatus("Tap a football");
    }

    /// <summary>
    /// 刷新等待区布局
    /// </summary>
    private void RefreshWaitSlots()
    {
        LayoutWaitSlots();
        for (int i = 0; i < waitSlots.Count; i++)
        {
            bool locked = IsWaitSlotLocked(i);
            waitSlots[i].SetLocked(locked);
            waitSlots[i].SetOccupied(!locked && i < waitingBalls.Count);
        }
    }

    private bool IsWaitSlotLocked(int index)
    {
        return index == LastWaitSlotIndex && !isLastWaitSlotUnlocked;
    }

    private void OnWaitSlotLockedClicked(WaitSlotView slot)
    {
        if (slot == null || !IsWaitSlotLocked(slot.Index) || unlockWaitSlotAdShowing)
        {
            return;
        }

        var data = new UnLockWaitAreaWindowData();
        data.closeCallBack = () => { };
        data.getCallBack = () =>
        {
            unlockWaitSlotAdShowing = true;
            SetStatus("Unlocking wait slot");
            if (PlatformMgr.Instance == null)
            {
                unlockWaitSlotAdShowing = false;
                Debug.LogError("PlatformMgr is null, cannot unlock wait slot by ad.");
                return;
            }

            PlatformMgr.Instance.LookAd(OnUnlockWaitSlotAdResult, UnlockWaitSlotAdType);
        };

        UIMonoInstance.OpenPanel<UnLockWaitAreaWindow>(GroupType.弹窗1, data);
    }

    private void OnUnlockWaitSlotAdResult(bool success)
    {
        unlockWaitSlotAdShowing = false;
        if (!success)
        {
            RefreshWaitSlots();
            return;
        }

        isLastWaitSlotUnlocked = true;
        RefreshWaitSlots();
        FinishTurn();
    }

    /// <summary>
    /// 布局等待区槽位的方法
    /// </summary>
    private void LayoutWaitSlots()
    {
        float availableWidth = waitRoot.rect.width > 0f ? waitRoot.rect.width : 900f;
        float availableHeight = waitRoot.rect.height > 0f ? waitRoot.rect.height : 110f;
        float slotSize = availableWidth / WaitSlotCount;
        // 计算起始x坐标：从左侧固定填充
        float startX = -availableWidth * 0.5f + slotSize * 0.5f;

        for (int i = 0; i < waitSlots.Count; i++)
        {
            RectTransform rect = waitSlots[i].RectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.one * slotSize;
            rect.anchoredPosition = new Vector2(startX + slotSize * i, 0f);
        }
    }

    /// <summary>
    /// 创建自动备份槽的布局信息
    /// </summary>
    private WaitSlotView CreateAutoBackupSlot(int index)
    {
        WaitSlotView slot = Instantiate(prefabWaitAre, autoBackupRoot);
        slot.Init(index);
        slot.SetIsAutoBackup(true);
        slot.SetOccupied(true);
        slot.transform.localScale = Vector3.one;
        slot.SetAlpha(0f);
        return slot;
    }

    private void DestroyAutoBackupSlotAt(int index)
    {
        if (index < 0 || index >= autoBackupSlots.Count)
        {
            return;
        }

        WaitSlotView slot = autoBackupSlots[index];
        autoBackupSlots.RemoveAt(index);
        if (slot != null)
        {
            Destroy(slot.gameObject);
        }
    }

    /// <summary>
    /// 自动备份添加动画方法，用于处理自动备份槽和球的动画效果
    /// </summary>
    private void AnimateAutoBackupAdd(Action onComplete)
    {
        // 获取当前自动备份槽的数量
        int oldSlotCount = autoBackupSlots.Count;
        // 计算需要的布局数量，取自动备份球数量和最大槽数量的较小值
        int layoutCount = Mathf.Min(autoBackupBalls.Count, AutoBackupSlotCount);
        // 计算自动备份槽的布局信息
        List<AutoBackupSlotLayout> layouts = CalculateAutoBackupSlotLayout(layoutCount);

        // 如果当前槽的数量大于需要的布局数量，则移除多余的槽
        while (autoBackupSlots.Count > layoutCount)
        {
            DestroyAutoBackupSlotAt(autoBackupSlots.Count - 1);
        }

        // 确保oldSlotCount不超过autoBackupSlots的实际数量，防止数组越界
        oldSlotCount = Mathf.Min(oldSlotCount, autoBackupSlots.Count);

        // 如果当前自动备份槽位数量少于需要的布局数量，则创建新的槽位
        while (autoBackupSlots.Count < layoutCount)
        {
            int index = autoBackupSlots.Count; // 获取当前槽位的索引
            WaitSlotView slot = CreateAutoBackupSlot(index);
            ApplyAutoBackupSlotLayout(slot, layouts[index]);
            slot.transform.localScale = Vector3.zero;
            slot.SetAlpha(0f);
            autoBackupSlots.Add(slot);
        }

        Sequence sequence = DOTween.Sequence().SetTarget(this);
        for (int i = 0; i < autoBackupSlots.Count; i++)
        {
            WaitSlotView slot = autoBackupSlots[i];
            slot.SetIndex(i);
            slot.SetOccupied(true);

            if (i < oldSlotCount)
            {
                sequence.Join(AnimateAutoBackupSlotTo(slot, layouts[i], AutoBackupAddDuration));
            }
            else
            {
                sequence.Join(slot.transform.DOScale(Vector3.one, AutoBackupSlotAppearDuration)
                    .SetEase(Ease.OutBack));
            }
        }

        int ballLayoutCount = Mathf.Min(autoBackupBalls.Count, layouts.Count);
        for (int i = 0; i < ballLayoutCount; i++)
        {
            FootballView view = autoBackupBalls[i].View;
            if (view == null)
            {
                continue;
            }

            sequence.Join(view.transform.DOMove(GetAutoBackupSlotWorldPosition(layouts[i]), AutoBackupAddDuration)
                .SetEase(Ease.OutCubic));
            sequence.Join(view.transform.DOScale(Vector3.one, AutoBackupAddDuration)
                .SetEase(Ease.OutCubic));
        }

        sequence.OnComplete(() =>
        {
            ApplyAutoBackupLayoutImmediate();
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 执行自动备份移除和重新流动动画的方法
    /// 该方法接收足球比赛数据列表和完成后的回调动作
    /// </summary>
    /// <param name="match">包含足球比赛数据的列表，用于动画处理</param>
    /// <param name="onComplete">动画完成后执行的回调动作，用于后续处理</param>
    private void AnimateAutoBackupRemoveAndReflow(List<FootballData> match, Action onComplete)
    {
        List<WaitSlotView> removingSlots = match
            .Select(ball => autoBackupBalls.IndexOf(ball))
            .Where(index => index >= 0 && index < autoBackupSlots.Count)
            .Distinct()
            .OrderByDescending(index => index)
            .Select(index => autoBackupSlots[index])
            .ToList();

        Sequence removeSequence = DOTween.Sequence().SetTarget(this);
        foreach (FootballData ball in match)
        {
            if (ball.View != null)
            {
                removeSequence.Join(ball.View.transform.DOScale(Vector3.zero, AutoBackupRemoveDuration)
                    .SetEase(Ease.InBack));
            }
        }

        foreach (WaitSlotView slot in removingSlots)
        {
            if (slot == null)
            {
                continue;
            }

            removeSequence.Join(slot.transform.DOScale(Vector3.zero, AutoBackupRemoveDuration)
                .SetEase(Ease.InBack));
        }

        removeSequence.OnComplete(() =>
        {
            RemoveMatchedBalls(match);
            RefreshWaitSlots();
            ReflowWaitingBalls(() => ReflowAutoBackupBalls(onComplete));
        });
    }

    /// <summary>
    /// 立即应用自动备份布局
    /// </summary>
    private void ApplyAutoBackupLayoutImmediate()
    {
        int layoutCount = Mathf.Min(autoBackupBalls.Count, AutoBackupSlotCount);
        while (autoBackupSlots.Count < layoutCount)
        {
            autoBackupSlots.Add(CreateAutoBackupSlot(autoBackupSlots.Count));
        }

        while (autoBackupSlots.Count > layoutCount)
        {
            DestroyAutoBackupSlotAt(autoBackupSlots.Count - 1);
        }

        List<AutoBackupSlotLayout> layouts = CalculateAutoBackupSlotLayout(autoBackupSlots.Count);
        for (int i = 0; i < autoBackupSlots.Count; i++)
        {
            WaitSlotView slot = autoBackupSlots[i];
            slot.SetIndex(i);
            slot.SetOccupied(true);
            slot.transform.localScale = Vector3.one;
            slot.SetAlpha(0f);
            ApplyAutoBackupSlotLayout(slot, layouts[i]);

            if (i < autoBackupBalls.Count && autoBackupBalls[i].View != null)
            {
                autoBackupBalls[i].View.transform.position = GetAutoBackupSlotWorldPosition(layouts[i]);
                autoBackupBalls[i].View.transform.localScale = Vector3.one;
                autoBackupBalls[i].View.SetInteractable(true);
            }
        }
    }

    /// <summary>
    /// 自动备份槽位布局方法，用于自动排列和定位备份槽位
    /// </summary>
    private void LayoutAutoBackupSlots()
    {
        // 检查自动备份根节点是否存在，以及备份槽位列表是否为空
        if (autoBackupRoot == null || autoBackupSlots.Count == 0)
        {
            // 如果条件不满足，直接返回，不执行后续布局
            return;
        }

        // 计算所有备份槽位的布局信息
        List<AutoBackupSlotLayout> layouts = CalculateAutoBackupSlotLayout(autoBackupSlots.Count);
        // 遍历所有备份槽位，应用计算好的布局
        for (int i = 0; i < autoBackupSlots.Count; i++)
        {
            ApplyAutoBackupSlotLayout(autoBackupSlots[i], layouts[i]);
        }
    }

    /// <summary>
    /// 计算自动备份槽位布局
    /// 根据给定的数量计算并返回一组自动备份槽位的布局信息
    /// </summary>
    /// <param name="count">需要计算的槽位数量</param>
    /// <returns>返回包含所有槽位布局信息的列表</returns>
    private List<AutoBackupSlotLayout> CalculateAutoBackupSlotLayout(int count)
    {
        List<AutoBackupSlotLayout> layouts = new List<AutoBackupSlotLayout>(); // 用于存储计算得到的布局信息列表
        if (autoBackupRoot == null || count <= 0) // 检查根节点是否有效且数量是否大于0
        {
            return layouts; // 如果无效或数量不合法，返回空列表
        }

        // 计算可用宽度和高度，如果获取到的值无效则使用默认值
        float availableWidth = autoBackupRoot.rect.width > 0f ? autoBackupRoot.rect.width : 900f;
        float availableHeight = autoBackupRoot.rect.height > 0f ? autoBackupRoot.rect.height : 80f;
        float slotSize = availableWidth / WaitSlotCount;
        // 计算起始x坐标：从左侧固定填充
        float startX = -availableWidth * 0.5f + slotSize * 0.5f;

        // 循环计算每个槽位的位置和大小
        for (int i = 0; i < count; i++)
        {
            // 创建新的槽位布局，位置根据索引计算，大小统一为slotSize
            layouts.Add(new AutoBackupSlotLayout(new Vector2(startX + slotSize * i, 0f), Vector2.one * slotSize));
        }

        return layouts; // 返回计算完成的布局列表
    }

    /// <summary>
    /// 自动备份槽位动画方法，将槽位视图平滑移动到指定布局位置
    /// </summary>
    /// <param name="slot">等待槽位的视图对象</param>
    /// <param name="layout">目标布局信息，包含位置和尺寸</param>
    /// <param name="duration">动画持续时间</param>
    /// <returns>返回创建的动画序列对象，可用于后续控制</returns>
    private Tween AnimateAutoBackupSlotTo(WaitSlotView slot, AutoBackupSlotLayout layout, float duration)
    {
        // 创建一个新的DOTween序列动画，并将目标设置为当前对象
        Sequence sequence = DOTween.Sequence().SetTarget(this);
        // 获取槽位的RectTransform组件
        RectTransform rect = slot.RectTransform;
        // 设置槽位的锚点为中心点
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        // 将槽位移动到目标位置，使用立方缓出效果
        sequence.Join(rect.DOAnchorPos(layout.AnchoredPosition, duration).SetEase(Ease.OutCubic));
        // 同时调整槽位尺寸到目标大小，同样使用立方缓出效果
        sequence.Join(rect.DOSizeDelta(layout.Size, duration).SetEase(Ease.OutCubic));
        // 返回创建的动画序列
        return sequence;
    }

    /// <summary>
    /// 应用自动备份槽位布局
    /// </summary>
    private void ApplyAutoBackupSlotLayout(WaitSlotView slot, AutoBackupSlotLayout layout)
    {
        // 获取槽位的RectTransform组件
        RectTransform rect = slot.RectTransform;
        // 设置锚点为中心点（0.5, 0.5）
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        // 设置槽位的大小为布局中指定的大小
        rect.sizeDelta = layout.Size;
        // 设置槽位的锚点位置为布局中指定的位置
        rect.anchoredPosition = layout.AnchoredPosition;
    }

    /// <summary>
    /// 根据自动备份槽位布局获取世界坐标位置
    /// </summary>
    /// <param name="layout">自动备份槽位布局对象，包含锚点位置信息</param>
    /// <returns>返回在世界坐标系中的位置，如果autoBackupRoot为null则返回零向量</returns>
    private Vector3 GetAutoBackupSlotWorldPosition(AutoBackupSlotLayout layout)
    {
        // 检查autoBackupRoot是否为空
        if (autoBackupRoot == null)
        {
            // 如果为空，返回零向量
            return Vector3.zero;
        }

        // 将锚点位置转换为世界坐标位置并返回
        return autoBackupRoot.TransformPoint(layout.AnchoredPosition);
    }

    private Vector3 GetDefaultEffectWorldPosition()
    {
        RectTransform rect = flyingRoot != null ? flyingRoot : transform as RectTransform;
        if (rect != null)
        {
            return rect.TransformPoint(rect.rect.center);
        }

        return transform.position;
    }

    private void SetStatus(string message)
    {
        //Debug.LogError($"状态更新:{message}");
        //FloatTipWindow.Show($"状态更新:{message}");
    }

    private readonly struct AutoBackupSlotLayout
    {
        public AutoBackupSlotLayout(Vector2 anchoredPosition, Vector2 size)
        {
            AnchoredPosition = anchoredPosition;
            Size = size;
        }

        public Vector2 AnchoredPosition { get; }
        public Vector2 Size { get; }
    }

    private readonly struct ShuffleBallAnimationState
    {
        public ShuffleBallAnimationState(Transform parent, int siblingIndex, Vector2 anchoredPosition,
            Vector3 localScale, Vector3 worldPosition, FootballView view)
        {
            Parent = parent;
            SiblingIndex = siblingIndex;
            AnchoredPosition = anchoredPosition;
            LocalScale = localScale;
            WorldPosition = worldPosition;
            View = view;
        }

        public Transform Parent { get; }
        public int SiblingIndex { get; }
        public Vector2 AnchoredPosition { get; }
        public Vector3 LocalScale { get; }
        public Vector3 WorldPosition { get; }
        public FootballView View { get; }
    }
}

public enum ResultWindowType
{
    None,
    WinWindow,
    LoseWindow,
    ReviveWindow
}
