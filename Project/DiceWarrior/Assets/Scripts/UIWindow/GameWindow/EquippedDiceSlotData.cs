using System.Collections.Generic;

public sealed class EquippedDiceSlotData
{
    public EquippedDiceSlotData(string name, int diceSides, IReadOnlyList<int> faces)
    {
        Name = name;
        DiceSides = diceSides;
        Faces = faces;
    }

    public string Name { get; }
    public int DiceSides { get; }
    public IReadOnlyList<int> Faces { get; }
    public bool IsEmpty => DiceSides <= 0;
}
