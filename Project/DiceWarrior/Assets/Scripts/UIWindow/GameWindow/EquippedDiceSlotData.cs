using System.Collections.Generic;
using UnityEngine;

public sealed class EquippedDiceSlotData
{
    private readonly List<int> faces = new List<int>();

    /// <summary>
    /// 创建一颗运行时骰子实例。
    /// </summary>
    public EquippedDiceSlotData(string name, int diceSides, IEnumerable<int> faces)
    {
        Name = name;
        DiceSides = diceSides;
        SetFaces(faces);
    }

    public string Name { get; }
    public int DiceSides { get; }
    public IReadOnlyList<int> Faces => faces;
    public bool IsEmpty => DiceSides <= 0;

    /// <summary>
    /// 克隆当前骰子数据。
    /// </summary>
    public EquippedDiceSlotData Clone()
    {
        return new EquippedDiceSlotData(Name, DiceSides, faces);
    }

    /// <summary>
    /// 对整颗骰子的所有面应用数值强化。
    /// </summary>
    public void ApplyWholeDiceDelta(int delta)
    {
        if (IsEmpty)
        {
            return;
        }

        for (int i = 0; i < faces.Count; i++)
        {
            faces[i] = Mathf.Max(0, faces[i] + delta);
        }
    }

    /// <summary>
    /// 对指定骰面应用数值强化。
    /// </summary>
    public void ApplySingleFaceDelta(int faceIndex, int delta)
    {
        if (IsEmpty || faceIndex < 0 || faceIndex >= faces.Count)
        {
            return;
        }

        faces[faceIndex] = Mathf.Max(0, faces[faceIndex] + delta);
    }

    /// <summary>
    /// 构建整骰强化预览结果。
    /// </summary>
    public List<int> BuildWholeDicePreview(int delta)
    {
        return BuildPreview(null, delta);
    }

    /// <summary>
    /// 构建单面强化预览结果。
    /// </summary>
    public List<int> BuildSingleFacePreview(int faceIndex, int delta)
    {
        return BuildPreview(faceIndex, delta);
    }

    /// <summary>
    /// 按目标面生成预览列表。
    /// </summary>
    private List<int> BuildPreview(int? faceIndex, int delta)
    {
        List<int> preview = new List<int>(faces.Count);
        for (int i = 0; i < faces.Count; i++)
        {
            int value = faces[i];
            if (!faceIndex.HasValue || faceIndex.Value == i)
            {
                value = Mathf.Max(0, value + delta);
            }

            preview.Add(value);
        }

        return preview;
    }

    /// <summary>
    /// 写入骰子的基础面值列表。
    /// </summary>
    private void SetFaces(IEnumerable<int> sourceFaces)
    {
        faces.Clear();
        if (sourceFaces == null)
        {
            return;
        }

        foreach (int face in sourceFaces)
        {
            faces.Add(Mathf.Max(0, face));
        }
    }
}
