using System;
using System.Collections.Generic;

public class BattleUnit
{
    public BattleUnit(UnitData data, Team team, int slot, BattleRow row)
    {
        Data = data;
        Team = team;
        Slot = slot;
        Row = row;
        Name = $"{data.DisplayName} {slot + 1}";
        Health = data.MaxHealth;
    }

    public event Action<BattleUnit> HealthChanged;

    public UnitData Data { get; }
    public Team Team { get; }
    public int Slot { get; }
    public BattleRow Row { get; }
    public string Name { get; }
    public int Health { get; private set; }

    public int MaxHealth => Data.MaxHealth;
    public int Speed => Data.Speed;
    public int Attack => Data.Attack;
    public int Defense => Data.Defense;
    public IReadOnlyList<AbilityData> Abilities => Data.Abilities;
    public bool IsAlive => Health > 0;
    public int MissingHealth => MaxHealth - Health;
    public float HealthRatio => (float)Health / MaxHealth;

    public void TakeDamage(int amount)
    {
        if (!IsAlive || amount <= 0)
            return;

        Health = Math.Max(0, Health - amount);
        HealthChanged?.Invoke(this);
    }

    public void Heal(int amount)
    {
        if (!IsAlive || amount <= 0)
            return;

        Health = Math.Min(MaxHealth, Health + amount);
        HealthChanged?.Invoke(this);
    }
}
