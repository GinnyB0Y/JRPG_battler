using UnityEngine;

public class BattleManager : MonoBehaviour
{
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
        int count = GetTeamSize(BattleSettings.PlayerTeamSize, playerPositions);

        for (int i = 0; i < count; i++)
            Spawn(i == 0 ? playerPrefab : npcPrefab, playerPositions[i]);
    }

    private void SpawnEnemyTeam()
    {
        int count = GetTeamSize(BattleSettings.EnemyTeamSize, enemyPositions);

        for (int i = 0; i < count; i++)
            Spawn(enemyPrefab, enemyPositions[i]);
    }

    private static int GetTeamSize(int requestedSize, Transform[] positions)
    {
        return Mathf.Min(requestedSize, positions.Length);
    }

    private void Spawn(GameObject prefab, Transform position)
    {
        Instantiate(prefab, position.position, position.rotation, transform);
    }
}
