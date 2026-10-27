using System;
using System.Collections.Generic;
using System.Linq;

public class InitiativeQueue
{
    private readonly List<BattleUnit> order = new List<BattleUnit>();
    private int nextIndex;
    private int begunIndex;

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

    public bool TryPeekNext(out BattleUnit unit)
    {
        Current = null;

        while (nextIndex < order.Count && !order[nextIndex].IsAlive)
            nextIndex++;

        if (nextIndex < order.Count)
            unit = order[nextIndex];
        else
            unit = null;

        return unit != null;
    }

    public IReadOnlyList<BattleUnit> GetCandidates(Team team)
    {
        return Upcoming.Where(unit => unit.Team == team).ToList();
    }

    public void Begin(BattleUnit unit)
    {
        int index = order.IndexOf(unit, nextIndex);

        if (index < 0)
            throw new ArgumentException($"{unit.Name} has already acted this round");

        (order[nextIndex], order[index]) = (order[index], order[nextIndex]);
        Current = unit;
        begunIndex = index;
        nextIndex++;
    }

    public void CancelBegin()
    {
        if (Current == null)
            throw new InvalidOperationException("No turn to cancel");

        nextIndex--;
        (order[nextIndex], order[begunIndex]) = (order[begunIndex], order[nextIndex]);
        Current = null;
    }
}
