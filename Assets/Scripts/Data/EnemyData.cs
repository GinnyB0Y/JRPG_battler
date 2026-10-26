using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Battle/Enemy Data")]
public class EnemyData : UnitData
{
    [SerializeField] private UnitRole role;

    public override UnitRole Role => role;
}
