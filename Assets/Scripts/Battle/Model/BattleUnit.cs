using System;
using System.Collections.Generic;

public class BattleUnit
{
    private readonly UnitStats stats;

    public BattleUnit(UnitData data, UnitStats stats, IReadOnlyList<AbilityData> abilities, Team team, int slot, BattleRow row)
    {
        Data = data;
        this.stats = stats;
        Abilities = abilities;
        Team = team;
        Slot = slot;
        Row = row;
        Name = $"{data.DisplayName} {slot + 1}";
        Health = stats.MaxHealth;
    }

    public event Action<BattleUnit> HealthChanged;
    public event Action<BattleUnit> StunChanged;

    public UnitData Data { get; }
    public Team Team { get; }
    public int Slot { get; }
    public BattleRow Row { get; }
    public string Name { get; }
    public int Health { get; private set; }
    public int StunnedTurns { get; private set; }

    public int MaxHealth => stats.MaxHealth;
    public int Speed => stats.Speed;
    public int Attack => stats.Attack;
    public int Defense => stats.Defense;
    public IReadOnlyList<AbilityData> Abilities { get; }
    public bool IsAlive => Health > 0;
    public bool IsStunned => IsAlive && StunnedTurns > 0;
    public int MissingHealth => MaxHealth - Health;
    public float HealthRatio => (float)Health / MaxHealth;

    public void TakeDamage(int amount)
    {
        if (!IsAlive || amount <= 0)
            return;

        Health = Math.Max(0, Health - amount);
        HealthChanged?.Invoke(this);

        if (!IsAlive && StunnedTurns > 0)
            SetStunnedTurns(0);
    }

    public void Heal(int amount)
    {
        if (!IsAlive || amount <= 0)
            return;

        Health = Math.Min(MaxHealth, Health + amount);
        HealthChanged?.Invoke(this);
    }

    public void Stun(int turns)
    {
        if (!IsAlive || turns <= StunnedTurns)
            return;

        SetStunnedTurns(turns);
    }

    public void SpendStunnedTurn()
    {
        if (StunnedTurns > 0)
            SetStunnedTurns(StunnedTurns - 1);
    }

    private void SetStunnedTurns(int turns)
    {
        StunnedTurns = turns;
        StunChanged?.Invoke(this);
    }
}
