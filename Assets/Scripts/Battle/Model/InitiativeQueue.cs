using System.Collections.Generic;
using System.Linq;

public class InitiativeQueue
{
    private readonly List<BattleUnit> order = new List<BattleUnit>();
    private int nextIndex;

    public int Round { get; private set; }
    public BattleUnit Current { get; private set; }

    public IEnumerable<BattleUnit> Upcoming => order.Skip(nextIndex).Where(unit => unit.IsAlive);

    public void StartRound(IEnumerable<BattleUnit> units)
    {
        Round++;
        Current = null;
        nextIndex = 0;

        order.Clear();
        order.AddRange(units
            .Where(unit => unit.IsAlive)
            .OrderByDescending(unit => unit.Speed)
            .ThenBy(unit => unit.Team)
            .ThenBy(unit => unit.Slot));
    }

    public bool TryAdvance(out BattleUnit unit)
    {
        while (nextIndex < order.Count)
        {
            BattleUnit candidate = order[nextIndex++];

            if (candidate.IsAlive)
            {
                Current = candidate;
                unit = candidate;
                return true;
            }
        }

        Current = null;
        unit = null;
        return false;
    }
}
