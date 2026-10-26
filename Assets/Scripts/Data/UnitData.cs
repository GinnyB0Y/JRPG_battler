using UnityEngine;

public abstract class UnitData : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private GameObject prefab;

    [Header("Stats")]
    [SerializeField, Min(1)] private int maxHealth = 20;
    [SerializeField, Min(0)] private int speed = 5;
    [SerializeField, Min(0)] private int attack = 5;
    [SerializeField, Min(0)] private int defense = 5;

    public string DisplayName => displayName;
    public GameObject Prefab => prefab;
    public int MaxHealth => maxHealth;
    public int Speed => speed;
    public int Attack => attack;
    public int Defense => defense;

    public abstract UnitRole Role { get; }
}
