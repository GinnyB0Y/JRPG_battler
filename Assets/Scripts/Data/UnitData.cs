using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class UnitData : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private GameObject prefab;

    [Header("Stats")]
    [SerializeField] private int maxHealth;
    [SerializeField] private int speed;
    [SerializeField] private int attack;
    [SerializeField] private int defense;

    [Header("Random Stats")]
    [SerializeField] private bool randomHP;
    [SerializeField] private Vector2Int maxHealthRange;
    [SerializeField] private bool randomSpeed;
    [SerializeField] private Vector2Int maxSpeedRange;
    [SerializeField] private bool randomAttack;
    [SerializeField] private Vector2Int maxAttackRange;
    [SerializeField] private bool randomDefense;
    [SerializeField] private Vector2Int maxDefenseRange;

    [SerializeField] private AbilityData[] abilities;
    [SerializeField] private AbilityData[] abilitiesPlus;
    [SerializeField] private int abilityCounter;
    [SerializeField, Range(0, 100)] private int abilityChance;

    public string DisplayName
    {
        get
        {
            if (string.IsNullOrEmpty(displayName))
                return name;
            else
                return displayName;
        }
    }

    public GameObject Prefab => prefab;
    public int MaxHealth => maxHealth;
    public int Speed => speed;
    public int Attack => attack;
    public int Defense => defense;
    public IReadOnlyList<AbilityData> Abilities => abilities;

    public abstract UnitRole Role { get; }

    public IReadOnlyList<AbilityData> RollAbilities(System.Random random)
    {
        List<AbilityData> result = new List<AbilityData>();

        if (abilities != null)
            result.AddRange(abilities.Where(ability => ability != null).Distinct());

        if (abilitiesPlus == null)
            return result;

        List<AbilityData> extra = abilitiesPlus
            .Where(ability => ability != null && !result.Contains(ability))
            .Distinct()
            .ToList();

        for (int i = 0; i < abilityCounter && extra.Count > 0; i++)
        {
            if (random.Next(100) >= abilityChance)
                continue;

            int index = random.Next(extra.Count);
            result.Add(extra[index]);
            extra.RemoveAt(index);
        }

        return result;
    }

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
