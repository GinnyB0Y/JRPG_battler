public readonly struct UnitStats
{
    public UnitStats(int maxHealth, int speed, int attack, int defense)
    {
        MaxHealth = maxHealth;
        Speed = speed;
        Attack = attack;
        Defense = defense;
    }

    public int MaxHealth { get; }
    public int Speed { get; }
    public int Attack { get; }
    public int Defense { get; }
}
