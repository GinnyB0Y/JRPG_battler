using System.Collections.Generic;

public class InitiativeQueue
{
    private readonly List<BattleUnit> order = new List<BattleUnit>();
    private int nextIndex;

    public int Round { get; private set; }
    public BattleUnit Current { get; private set; }

    public List<BattleUnit> GetUpcoming()
    {
        List<BattleUnit> result = new List<BattleUnit>();

        for (int i = nextIndex; i < order.Count; i++)
        {
            if (order[i].IsAlive)
                result.Add(order[i]);
        }

        return result;
    }

    public void StartRound(IReadOnlyList<BattleUnit> units)
    {
        Round++;
        Current = null;
        nextIndex = 0;

        order.Clear();

        foreach (BattleUnit unit in units)
        {
            if (unit.IsAlive)
                order.Add(unit);
        }

        order.Sort(CompareInitiative);
    }

    public bool TryGetNext(out BattleUnit unit)
    {
        while (nextIndex < order.Count && !order[nextIndex].IsAlive)
            nextIndex++;

        if (nextIndex < order.Count)
        {
            unit = order[nextIndex];
            nextIndex++;
        }
        else
        {
            unit = null;
        }

        Current = unit;
        return unit != null;
    }

    private static int CompareInitiative(BattleUnit a, BattleUnit b)
    {
        if (a.Speed != b.Speed)
            return b.Speed.CompareTo(a.Speed);

        if (a.Team != b.Team)
            return a.Team.CompareTo(b.Team);

        return a.Slot.CompareTo(b.Slot);
    }
}
