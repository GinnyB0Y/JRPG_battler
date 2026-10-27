using UnityEngine;

[CreateAssetMenu(fileName = "AbilityData", menuName = "Battle/Ability Data")]
public class AbilityData : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private AbilityEffect effect;
    [SerializeField] private AbilityVisual visual;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField, Min(0)] private int power = 5;

    [Header("Stun")]
    [SerializeField, Min(0)] private int stunTurns;

    [Header("Targeting")]
    [SerializeField] private TargetSide targetSide;
    [SerializeField] private bool requiresFrontUser;
    [SerializeField] private bool requiresFrontTarget;
    [SerializeField, Min(0)] private int frontAttackPenalty;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public AbilityEffect Effect => effect;
    public AbilityVisual Visual => visual;
    public Projectile ProjectilePrefab => projectilePrefab;
    public int Power => power;
    public int StunTurns => stunTurns;
    public bool Stuns => effect == AbilityEffect.Damage && stunTurns > 0;
    public TargetSide TargetSide => targetSide;
    public bool RequiresFrontUser => requiresFrontUser;
    public bool RequiresFrontTarget => requiresFrontTarget;
    public int FrontAttackPenalty => frontAttackPenalty;
}
