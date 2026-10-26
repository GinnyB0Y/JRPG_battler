using UnityEngine;

[CreateAssetMenu(fileName = "NpcData", menuName = "Battle/Npc Data")]
public class NpcData : UnitData
{
    [SerializeField] private UnitRole role;

    public override UnitRole Role => role;
}
