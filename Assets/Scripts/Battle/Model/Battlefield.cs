using System.Collections.Generic;
using System.Linq;

public class Battlefield
{
    private readonly List<BattleUnit> units;

    public Battlefield(IEnumerable<BattleUnit> units)
    {
        this.units = units.ToList();
    }

    public IReadOnlyList<BattleUnit> Units => units;

    public IEnumerable<BattleUnit> GetTeam(Team team)
    {
        return units.Where(unit => unit.Team == team);
    }

    public IEnumerable<BattleUnit> GetAlive(Team team)
    {
        return GetTeam(team).Where(unit => unit.IsAlive);
    }

    public BattleUnit GetFront(Team team)
    {
        return GetAlive(team).OrderBy(unit => unit.Slot).FirstOrDefault();
    }

    public bool IsFront(BattleUnit unit)
    {
        return unit.IsAlive && GetFront(unit.Team) == unit;
    }

    public bool TryGetWinner(out Team winner)
    {
        if (!GetAlive(Team.Enemy).Any())
        {
            winner = Team.Player;
            return true;
        }

        if (!GetAlive(Team.Player).Any())
        {
            winner = Team.Enemy;
            return true;
        }

        winner = default;
        return false;
    }
}
