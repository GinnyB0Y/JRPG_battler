using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private PlayerData playerData;
    [SerializeField] private NpcData[] npcPool;
    [SerializeField] private EnemyData[] enemyPool;

    [SerializeField] private Transform[] npcMeleePositions;
    [SerializeField] private Transform[] npcRangePositions;
    [SerializeField] private Transform[] enemyMeleePositions;
    [SerializeField] private Transform[] enemyRangePositions;

    private void Awake()
    {
        SpawnPlayerTeam();
        SpawnEnemyTeam();
    }

    private void SpawnPlayerTeam()
    {
        Transform[] line = BuildLine(npcMeleePositions, npcRangePositions);
        int size = Mathf.Min(BattleSettings.PlayerTeamSize, line.Length);

        List<UnitData> team = new List<UnitData> { playerData };
        team.AddRange(PickRandom(npcPool, size - 1));

        SpawnTeam(team, line);
    }

    private void SpawnEnemyTeam()
    {
        Transform[] line = BuildLine(enemyMeleePositions, enemyRangePositions);
        int size = Mathf.Min(BattleSettings.EnemyTeamSize, line.Length);

        SpawnTeam(PickRandom(enemyPool, size), line);
    }

    private static Transform[] BuildLine(Transform[] meleePositions, Transform[] rangePositions)
    {
        return meleePositions.Concat(rangePositions).ToArray();
    }

    private static IEnumerable<T> PickRandom<T>(T[] pool, int count)
    {
        for (int i = 0; i < count; i++)
            yield return pool[Random.Range(0, pool.Length)];
    }

    private void SpawnTeam(IEnumerable<UnitData> team, Transform[] line)
    {
        int front = 0;
        int back = line.Length - 1;

        foreach (UnitData unit in team)
        {
            Transform position = unit.Role == UnitRole.Melee ? line[front++] : line[back--];
            Spawn(unit, position);
        }
    }

    private void Spawn(UnitData unit, Transform position)
    {
        Instantiate(unit.Prefab, position.position, position.rotation, transform);
    }
}
