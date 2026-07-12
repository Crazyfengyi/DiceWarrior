using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using cfg;
using UnityEngine;

public sealed class DiceEnhancePreviewModel
{
    private readonly List<int> previewFaces = new List<int>();

    /// <summary>
    /// 根据当前骰子和强化配置生成预览数据。
    /// </summary>
    public DiceEnhancePreviewModel(EquippedDiceSlotData dice, EnhancementTypeData enhancementData,
        int? selectedFaceIndex)
    {
        SourceDice = dice;
        EnhancementData = enhancementData;
        SelectedFaceIndex = selectedFaceIndex;
        BuildPreview();
    }

    public EquippedDiceSlotData SourceDice { get; }
    public EnhancementTypeData EnhancementData { get; }
    public IReadOnlyList<int> PreviewFaces => previewFaces;
    public int? SelectedFaceIndex { get; }
    public bool HasValidTarget { get; private set; }
    public int MinValue { get; private set; }
    public int MaxValue { get; private set; }

    public string RangeText => $"极限区间  {MinValue}~{MaxValue}";

    public string ProbabilityText
    {
        get
        {
            if (previewFaces.Count == 0)
            {
                return "暂无概率数据";
            }

            SortedDictionary<int, int> countMap = new SortedDictionary<int, int>();
            for (int i = 0; i < previewFaces.Count; i++)
            {
                int faceValue = previewFaces[i];
                countMap.TryGetValue(faceValue, out int count);
                countMap[faceValue] = count + 1;
            }

            StringBuilder builder = new StringBuilder();
            int totalCount = previewFaces.Count;
            foreach (KeyValuePair<int, int> pair in countMap)
            {
                float percent = pair.Value * 100f / totalCount;
                builder.Append(pair.Key)
                    .Append(' ')
                    .Append(percent.ToString(percent % 1f == 0f ? "0" : "0.0", CultureInfo.InvariantCulture))
                    .Append('%')
                    .AppendLine();
            }

            return builder.ToString().TrimEnd();
        }
    }

    /// <summary>
    /// 生成强化后的骰面预览。
    /// </summary>
    private void BuildPreview()
    {
        previewFaces.Clear();
        HasValidTarget = false;
        MinValue = 0;
        MaxValue = 0;

        if (SourceDice == null || SourceDice.IsEmpty || EnhancementData == null)
        {
            return;
        }

        if (IsWholeDiceEnhancement(EnhancementData))
        {
            previewFaces.AddRange(SourceDice.BuildWholeDicePreview(GetValueDelta(EnhancementData)));
            HasValidTarget = true;
        }
        else if (SelectedFaceIndex.HasValue && SelectedFaceIndex.Value >= 0 &&
                 SelectedFaceIndex.Value < SourceDice.Faces.Count)
        {
            previewFaces.AddRange(SourceDice.BuildSingleFacePreview(SelectedFaceIndex.Value,
                GetValueDelta(EnhancementData)));
            HasValidTarget = true;
        }

        if (previewFaces.Count == 0)
        {
            return;
        }

        MinValue = int.MaxValue;
        MaxValue = int.MinValue;
        for (int i = 0; i < previewFaces.Count; i++)
        {
            MinValue = Mathf.Min(MinValue, previewFaces[i]);
            MaxValue = Mathf.Max(MaxValue, previewFaces[i]);
        }
    }

    /// <summary>
    /// 读取当前效果支持的数值增量。
    /// </summary>
    /// <summary>
    /// 读取强化类型表中的数值增量。
    /// </summary>
    public static int GetValueDelta(EnhancementTypeData enhancementData)
    {
        if (enhancementData == null ||
            (enhancementData.EnhancementType != "数值增加" && enhancementData.EnhancementType != "数值减少"))
        {
            return 0;
        }

        string valueText = enhancementData.EnhancementName?.Replace("全", string.Empty);
        return int.TryParse(valueText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            ? value
            : 0;
    }

    /// <summary>
    /// 判断强化类型是否作用于整颗骰子。
    /// </summary>
    public static bool IsWholeDiceEnhancement(EnhancementTypeData enhancementData)
    {
        return enhancementData != null && enhancementData.EnhancementName != null &&
               enhancementData.EnhancementName.StartsWith("全", StringComparison.Ordinal);
    }
}
