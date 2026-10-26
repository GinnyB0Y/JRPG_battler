using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Battle/Player Data")]
public class PlayerData : UnitData
{
    public override UnitRole Role => UnitRole.Melee;
}
