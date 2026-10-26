using UnityEngine;

public class BattleManager : MonoBehaviour
{
    private const int MaxTeamSize = 4;

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject npcPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] playerPositions;
    [SerializeField] private Transform[] enemyPositions;

    private void Awake()
    {
        SpawnPlayerTeam();
        SpawnEnemyTeam();
    }

    private void SpawnPlayerTeam()
    {
        int count = GetTeamSize(playerPositions);

        for (int i = 0; i < count; i++)
            Spawn(i == 0 ? playerPrefab : npcPrefab, playerPositions[i]);
    }

    private void SpawnEnemyTeam()
    {
        int count = GetTeamSize(enemyPositions);

        for (int i = 0; i < count; i++)
            Spawn(enemyPrefab, enemyPositions[i]);
    }

    private static int GetTeamSize(Transform[] positions)
    {
        return Mathf.Min(positions.Length, MaxTeamSize);
    }

    private void Spawn(GameObject prefab, Transform position)
    {
        Instantiate(prefab, position.position, position.rotation, transform);
    }
}
