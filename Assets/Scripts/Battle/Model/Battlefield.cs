using System.Collections.Generic;

public class Battlefield
{
    private readonly List<BattleUnit> units;

    public Battlefield(List<BattleUnit> units)
    {
        this.units = new List<BattleUnit>(units);
    }

    public IReadOnlyList<BattleUnit> Units => units;

    public List<BattleUnit> GetTeam(Team team)
    {
        List<BattleUnit> result = new List<BattleUnit>();

        foreach (BattleUnit unit in units)
        {
            if (unit.Team == team)
                result.Add(unit);
        }

        return result;
    }

    public List<BattleUnit> GetAlive(Team team)
    {
        List<BattleUnit> result = new List<BattleUnit>();

        foreach (BattleUnit unit in units)
        {
            if (unit.Team == team && unit.IsAlive)
                result.Add(unit);
        }

        return result;
    }

    public List<BattleUnit> GetFrontLine(Team team)
    {
        List<BattleUnit> alive = GetAlive(team);

        if (alive.Count == 0)
            return alive;

        BattleRow frontRow = alive[0].Row;

        foreach (BattleUnit unit in alive)
        {
            if (unit.Row < frontRow)
                frontRow = unit.Row;
        }

        List<BattleUnit> result = new List<BattleUnit>();

        foreach (BattleUnit unit in alive)
        {
            if (unit.Row == frontRow)
                result.Add(unit);
        }

        return result;
    }

    public bool IsFront(BattleUnit unit)
    {
        return unit.IsAlive && GetFrontLine(unit.Team).Contains(unit);
    }

    public bool TryGetWinner(out Team winner)
    {
        if (GetAlive(Team.Enemy).Count == 0)
        {
            winner = Team.Player;
            return true;
        }

        if (GetAlive(Team.Player).Count == 0)
        {
            winner = Team.Enemy;
            return true;
        }

        winner = default;
        return false;
    }
}
