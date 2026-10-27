using System.Collections.Generic;
using UnityEngine;

public abstract class UnitData : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private GameObject prefab;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 20;
    [SerializeField] private int speed = 5;
    [SerializeField] private int attack = 5;
    [SerializeField] private int defense = 5;

    [Header("Random Stats")]
    [SerializeField] private bool randomHP;
    [SerializeField] private Vector2Int maxHealthRange;
    [SerializeField] private bool randomSpeed;
    [SerializeField] private Vector2Int maxSpeedRange;
    [SerializeField] private bool randomAttack;
    [SerializeField] private Vector2Int maxAttackRange;
    [SerializeField] private bool randomDefense;
    [SerializeField] private Vector2Int maxDefenseRange;

    [Header("Abilities")]
    [SerializeField] private AbilityData[] abilities;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public GameObject Prefab => prefab;
    public int MaxHealth => maxHealth;
    public int Speed => speed;
    public int Attack => attack;
    public int Defense => defense;
    public IReadOnlyList<AbilityData> Abilities => abilities;

    public abstract UnitRole Role { get; }

    public UnitStats RollStats(System.Random random)
    {
        return new UnitStats(
            Roll(maxHealth, randomHP, maxHealthRange, 1, random),
            Roll(speed, randomSpeed, maxSpeedRange, 0, random),
            Roll(attack, randomAttack, maxAttackRange, 0, random),
            Roll(defense, randomDefense, maxDefenseRange, 0, random));
    }

    private static int Roll(int value, bool isRandom, Vector2Int range, int minValue, System.Random random)
    {
        if (!isRandom)
            return Mathf.Max(minValue, value);

        int min = Mathf.Max(minValue, Mathf.Min(range.x, range.y));
        int max = Mathf.Max(min, Mathf.Max(range.x, range.y));
        return random.Next(min, max + 1);
    }
}
