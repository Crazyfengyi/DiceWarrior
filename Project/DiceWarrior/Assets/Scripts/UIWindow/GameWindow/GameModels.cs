using System;
using System.Collections.Generic;
using cfg;

public enum FootballType
{
    None = 0,
    Glod = 1,
    Ball1,
    Ball2,
    Ball3,
    Ball4,
    Ball5,
    Ball6,
    Ball7,
    Ball8,
    Ball9,
    Ball10,
    Ball11,
    Ball12,
    Ball13,
    Ball14,
    Ball15,
    Clothes1,
    Clothes2,
    Clothes3,
    Clothes4,
    Clothes5,
    Clothes6,
    Clothes7,
    Clothes8,
    Clothes9,
    Clothes10,
    Clothes11,
}

[Serializable]
public sealed class FootballData
{
    public FootballData(FootballType type, BlockData owner)
    {
        Type = type;
        Owner = owner;
    }

    public FootballType Type { get; set; }
    public BlockData Owner { get; set; }
    public FootballView View { get; set; }
}

public sealed class BlockData
{
    public BlockData(int x, int y, int width, int sourceRow)
    {
        X = x;
        Y = y;
        Width = width;
        SourceRow = sourceRow;
    }

    public int X { get; }
    public int Y { get; set; }
    public int Width { get; }
    public int SourceRow { get; }
    public List<FootballData> Footballs { get; } = new List<FootballData>();
    public BlockView View { get; set; }
}

public sealed class BlockDefinition
{
    public BlockDefinition(int x, int y, int width, int sourceRow, params FootballType[] footballs)
    {
        X = x;
        Y = y;
        Width = width;
        SourceRow = sourceRow;
        Footballs = footballs;
    }

    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int SourceRow { get; }
    public FootballType[] Footballs { get; }
}

public sealed class LevelConfig
{
    private const int MaxBlockWidth = 6;
    private static readonly float[] DefaultAddBallTypeProgresses = {0.3f, 0.6f, 0.8f};

    public LevelConfig(int boardWidth, int visibleRows, int totalBallCount, int initialBallTypeCount, int specialBallCount,
        int initialSpecialBallTypeCount, int coinCount, int maxRowBlockWidthSum, int[] blockWidthWeights = null,
        IReadOnlyList<float> addBallTypeProgresses = null)
    {
        BoardWidth = System.Math.Max(1, boardWidth);
        VisibleRows = System.Math.Max(1, visibleRows);
        TotalBallCount = System.Math.Max(0, totalBallCount);
        InitialBallTypeCount = System.Math.Max(1, initialBallTypeCount);
        SpecialBallCount = System.Math.Max(0, specialBallCount);
        InitialSpecialBallTypeCount = System.Math.Max(0, initialSpecialBallTypeCount);
        CoinCount = System.Math.Max(0, coinCount);
        MaxRowBlockWidthSum = System.Math.Max(1, System.Math.Min(maxRowBlockWidthSum, BoardWidth));
        BlockWidthWeights = CreateBlockWidthWeights(blockWidthWeights);
        AddBallTypeProgresses = CreateAddBallTypeProgresses(addBallTypeProgresses);
    }

    /// <summary>
    /// 游戏棋盘配置属性类
    /// 包含游戏棋盘的各种参数配置
    /// </summary>
    public int BoardWidth { get; } // 棋盘的宽度（单位：格子数）

    public int VisibleRows { get; }
    public int TotalBallCount { get; }
    public int InitialBallTypeCount { get; }
    public int SpecialBallCount { get; }
    public int InitialSpecialBallTypeCount { get; }
    public int CoinCount { get; }
    public int MaxRowBlockWidthSum { get; }
    public int[] BlockWidthWeights { get; }
    public float[] AddBallTypeProgresses { get; }

    public static LevelConfig CreateDefault()
    {
        return new LevelConfig(6, 8, 60, 3, 0, 0, 30, 6, new[] {1, 30, 30, 2, 2, 2},
            DefaultAddBallTypeProgresses);
    }

    public static LevelConfig CreateDefault(int[] blockWidthWeights)
    {
        return new LevelConfig(6, 8, 60, 3, 0, 0, 30, 6, blockWidthWeights, DefaultAddBallTypeProgresses);
    }

    public static LevelConfig CreateFromTable(TbLevelData tableData)
    {
        if (tableData == null)
        {
            return CreateDefault();
        }

        return new LevelConfig(6, 8, tableData.AllballNum, tableData.InitBallType, tableData.SpecialBallNum,
            tableData.InitSpecialBallType, tableData.GlodNum, 6, new[] {1, 30, 30, 2, 2, 2}, tableData.AddBallType);
    }

    private static int[] CreateBlockWidthWeights(int[] blockWidthWeights)
    {
        int[] weights = new int[MaxBlockWidth];
        bool hasPositiveWeight = false;

        for (int i = 0; i < weights.Length; i++)
        {
            int weight = blockWidthWeights != null && i < blockWidthWeights.Length ? blockWidthWeights[i] : 1;
            weights[i] = System.Math.Max(0, weight);
            hasPositiveWeight |= weights[i] > 0;
        }

        if (hasPositiveWeight)
        {
            return weights;
        }

        for (int i = 0; i < weights.Length; i++)
        {
            weights[i] = 1;
        }

        return weights;
    }

    private static float[] CreateAddBallTypeProgresses(IReadOnlyList<float> addBallTypeProgresses)
    {
        List<float> progresses = new List<float>();
        if (addBallTypeProgresses != null)
        {
            for (int i = 0; i < addBallTypeProgresses.Count; i++)
            {
                float progress = addBallTypeProgresses[i];
                if (progress > 0f && progress < 1f)
                {
                    progresses.Add(progress);
                }
            }
        }

        progresses.Sort();
        return progresses.ToArray();
    }
}

public sealed class LevelData
{
    private const int MaxBlockWidth = 6;

    public int Width { get; private set; }
    public int Height { get; private set; }
    public int InitialVisibleRows { get; private set; }
    public int TotalBallCount { get; private set; }
    public int InitialBallTypeCount { get; private set; }
    public int SpecialBallCount { get; private set; }
    public int InitialSpecialBallTypeCount { get; private set; }
    public int MatchableBallCount => TotalBallCount + SpecialBallCount + CoinCount;
    public int CoinCount { get; private set; }
    public int MaxRowBlockWidthSum { get; private set; }
    public int[] BlockWidthWeights { get; private set; }

    public static LevelData CreatePrototype()
    {
        return CreateFromConfig(LevelConfig.CreateDefault());
    }

    public static LevelData CreateFromConfig(LevelConfig config)
    {
        LevelData level = new LevelData
        {
            Width = config.BoardWidth,
            Height = config.VisibleRows,
            InitialVisibleRows = config.VisibleRows,
            TotalBallCount = config.TotalBallCount,
            InitialBallTypeCount = config.InitialBallTypeCount,
            SpecialBallCount = config.SpecialBallCount,
            InitialSpecialBallTypeCount = config.InitialSpecialBallTypeCount,
            CoinCount = config.CoinCount,
            MaxRowBlockWidthSum = config.MaxRowBlockWidthSum,
            BlockWidthWeights = CopyBlockWidthWeights(config.BlockWidthWeights)
        };

        return level;
    }

    /// <summary>
    /// 创建随机行宽列表
    /// </summary>
    /// <param name="maxBallCount">最大球数量限制</param>
    /// <returns>返回一个整数列表，表示每行的宽度</returns>
    public List<int> CreateRandomRowWidths(int maxBallCount)
    {
        // 如果最大球数小于等于0，返回空列表
        if (maxBallCount <= 0)
        {
            return new List<int>();
        }

        // 创建随机宽度列表，使用最大行块宽度和块权重
        List<int> widths = CreateRandomWidths(MaxRowBlockWidthSum, BlockWidthWeights);
        // 计算允许的最大宽度，取最大球数和最大行块宽度的较小值
        int allowedWidth = System.Math.Min(maxBallCount, MaxRowBlockWidthSum);
        // 当总宽度超过允许宽度且列表不为空时，移除最后一个元素
        while (GetTotalWidth(widths) > allowedWidth && widths.Count > 0)
        {
            widths.RemoveAt(widths.Count - 1);
        }

        // 如果列表为空，添加一个允许宽度或最大块宽度中的较小值
        if (widths.Count == 0)
        {
            widths.Add(System.Math.Min(allowedWidth, MaxBlockWidth));
        }

        return widths;
    }

    /// <summary>
    /// 创建一个随机的方块行
    /// </summary>
    /// <param name="sourceRow">源行的索引</param>
    /// <param name="rowFootballs">足球类型的二维数组列表，表示每行的足球排列</param>
    /// <returns>返回一个包含方块定义的列表</returns>
    public List<BlockDefinition> CreateRandomRow(int sourceRow, int boardY, IReadOnlyList<FootballType[]> rowFootballs)
    {
        // 计算已使用的宽度总和
        int usedWidth = 0;
        for (int i = 0; i < rowFootballs.Count; i++)
        {
            usedWidth += rowFootballs[i].Length;
        }

        // 如果没有使用任何宽度，返回空列表
        if (usedWidth <= 0)
        {
            return new List<BlockDefinition>();
        }

        int startX = UnityEngine.Random.Range(0, Width - usedWidth + 1);
        List<BlockDefinition> rowBlocks = new List<BlockDefinition>(rowFootballs.Count);
        int x = startX;
        for (int i = 0; i < rowFootballs.Count; i++)
        {
            FootballType[] footballs = rowFootballs[i];
            int width = footballs.Length;
            rowBlocks.Add(new BlockDefinition(x, boardY, width, sourceRow, footballs));
            x += width;
        }

        return rowBlocks;
    }

    /// <summary>
    /// 创建一个随机宽度的列表，总宽度不超过指定的最大值
    /// </summary>
    /// <param name="maxTotalWidth">允许的最大总宽度</param>
    /// <param name="blockWidthWeights">不同宽度块的权重数组</param>
    /// <returns>返回一个包含随机宽度值的列表</returns>
    private static List<int> CreateRandomWidths(int maxTotalWidth, int[] blockWidthWeights)
    {
        // 随机生成一个目标宽度，范围在3到maxTotalWidth之间
        int targetWidth = UnityEngine.Random.Range(1, maxTotalWidth + 1);
        // 用于存储生成的宽度列表
        List<int> widths = new List<int>();
        // 计算剩余需要分配的宽度
        int remaining = targetWidth;
        // 当还有剩余宽度需要分配时，继续循环
        while (remaining > 0)
        {
            // 根据权重选择一个块宽度，确保不超过最大块宽度和剩余宽度
            int width = ChooseWeightedBlockWidth(System.Math.Min(MaxBlockWidth, remaining), blockWidthWeights);
            // 将选择的宽度添加到列表中
            widths.Add(width);
            // 从剩余宽度中减去已分配的宽度
            remaining -= width;
        }

        return widths;
    }

    /// <summary>
    /// 根据权重随机选择方块宽度
    /// </summary>
    /// <param name="maxWidth">方块的最大宽度</param>
    /// <param name="blockWidthWeights">不同宽度的权重数组</param>
    /// <returns>返回根据权重随机选择的方块宽度</returns>
    private static int ChooseWeightedBlockWidth(int maxWidth, int[] blockWidthWeights)
    {
        // 计算所有宽度权重的总和
        int totalWeight = 0;
        for (int width = 1; width <= maxWidth; width++)
        {
            totalWeight += GetBlockWidthWeight(blockWidthWeights, width);
        }

        // 如果总权重小于等于0，则返回一个随机宽度
        if (totalWeight <= 0)
        {
            return UnityEngine.Random.Range(1, maxWidth + 1);
        }

        // 生成一个0到总权重之间的随机数
        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulativeWeight = 0;
        // 遍历所有可能的宽度，累加权重直到超过随机数
        for (int width = 1; width <= maxWidth; width++)
        {
            cumulativeWeight += GetBlockWidthWeight(blockWidthWeights, width);
            if (roll < cumulativeWeight)
            {
                return width;
            }
        }

        // 如果所有权重累加后仍未超过随机数，则返回最大宽度
        return maxWidth;
    }

    private static int GetBlockWidthWeight(int[] blockWidthWeights, int width)
    {
        int index = width - 1;
        if (blockWidthWeights == null || index < 0 || index >= blockWidthWeights.Length)
        {
            return 1;
        }

        return System.Math.Max(0, blockWidthWeights[index]);
    }

    /// <summary>
    /// 计算整数列表中所有元素的总和
    /// </summary>
    /// <param name="widths">整数列表，包含需要累加的数值</param>
    /// <returns>返回列表中所有元素的总和</returns>
    private static int GetTotalWidth(List<int> widths)
    {
        int totalWidth = 0; // 用于存储总和的变量，初始值为0
        // 遍历列表中的每个元素
        for (int i = 0; i < widths.Count; i++)
        {
            totalWidth += widths[i]; // 将当前元素的值累加到总和中
        }

        return totalWidth; // 返回计算得到的总和
    }

    private static int[] CopyBlockWidthWeights(int[] blockWidthWeights)
    {
        int[] copy = new int[MaxBlockWidth];
        for (int i = 0; i < copy.Length; i++)
        {
            copy[i] = GetBlockWidthWeight(blockWidthWeights, i + 1);
        }

        return copy;
    }
}
