using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private PlayerData playerData;
    [SerializeField] private NpcData[] npcPool;
    [SerializeField] private EnemyData[] enemyPool;

    [SerializeField] private Transform[] npcMeleePositions;
    [SerializeField] private Transform[] npcRangePositions;
    [SerializeField] private Transform[] enemyMeleePositions;
    [SerializeField] private Transform[] enemyRangePositions;

    [SerializeField] private string menuSceneName;
    [SerializeField, Min(0f)] private float enemyThinkDelay;
    [SerializeField, Range(0, 100)] private int enemyRandomActionChance = 50;

    [Header("Visuals")]
    [SerializeField] private GameObject activeMarkerPrefab;
    [SerializeField] private Transform targetMarker;
    [SerializeField] private BattleUI battleUI;
    [SerializeField] private BattleCamera battleCamera;
    [SerializeField] private PauseMenu pauseMenu;

    private readonly List<BattleUnit> units = new List<BattleUnit>();
    private readonly List<UnitView> views = new List<UnitView>();
    private BattlePresenter presenter;
    private UnitSelector selector;
    private System.Random teamRandom;
    private CancellationTokenSource battleCancellation;
    private bool isLeaving;

    private void Awake()
    {
        teamRandom = new System.Random(BattleSettings.Seed);
        presenter = gameObject.AddComponent<BattlePresenter>();
        selector = gameObject.AddComponent<UnitSelector>();

        SpawnPlayerTeam();
        SpawnEnemyTeam();

        selector.Init(views, activeMarkerPrefab, targetMarker);
    }

    private void Start()
    {
        Battlefield field = new Battlefield(units);
        CombatRules rules = new CombatRules(field, new System.Random());
        AiCommandSource ai = new AiCommandSource(rules, TimeSpan.FromSeconds(enemyThinkDelay), enemyRandomActionChance, new System.Random());
        BattleLoop loop = new BattleLoop(field, rules, battleUI, ai, presenter);

        battleUI.Bind(loop, rules, field, selector);
        battleUI.ExitRequested += ExitToMenu;
        battleUI.RestartRequested += Restart;
        selector.Bind(loop);

        if (battleCamera != null)
            battleCamera.Bind(loop, battleUI, views);

        if (pauseMenu != null)
        {
            pauseMenu.Bind(loop);
            pauseMenu.RestartRequested += Restart;
            pauseMenu.ExitRequested += ExitToMenu;
        }

        battleCancellation = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        loop.RunAsync(battleCancellation.Token).Forget();
    }

    private void OnDestroy()
    {
        if (battleUI != null)
        {
            battleUI.ExitRequested -= ExitToMenu;
            battleUI.RestartRequested -= Restart;
        }

        if (pauseMenu != null)
        {
            pauseMenu.RestartRequested -= Restart;
            pauseMenu.ExitRequested -= ExitToMenu;
        }

        battleCancellation?.Cancel();
        battleCancellation?.Dispose();
    }

    private void Restart()
    {
        LeaveAsync(SceneManager.GetActiveScene().name).Forget();
    }

    private void ExitToMenu()
    {
        LeaveAsync(menuSceneName).Forget();
    }

    private async UniTaskVoid LeaveAsync(string sceneName)
    {
        if (isLeaving)
            return;

        isLeaving = true;
        battleCancellation.Cancel();
        Time.timeScale = 1f;
        await SceneManager.LoadSceneAsync(sceneName);
    }

    private void SpawnPlayerTeam()
    {
        Transform[] line = BuildTeams(npcMeleePositions, npcRangePositions);

        List<UnitData> team = new List<UnitData> { playerData };

        foreach (UnitRole role in BattleSettings.NpcRoles)
            team.Add(PickByRole(npcPool, role));

        SpawnTeam(team, Team.Player, line, npcMeleePositions.Length);
    }

    private void SpawnEnemyTeam()
    {
        Transform[] line = BuildTeams(enemyMeleePositions, enemyRangePositions);
        List<UnitData> team = new List<UnitData>();

        foreach (UnitRole role in BattleSettings.EnemyRoles)
            team.Add(PickByRole(enemyPool, role));

        SpawnTeam(team, Team.Enemy, line, enemyMeleePositions.Length);
    }

    private static Transform[] BuildTeams(Transform[] meleePositions, Transform[] rangePositions)
    {
        List<Transform> line = new List<Transform>();
        line.AddRange(meleePositions);
        line.AddRange(rangePositions);
        return line.ToArray();
    }

    private UnitData PickByRole(UnitData[] pool, UnitRole role)
    {
        List<UnitData> matching = new List<UnitData>();

        foreach (UnitData data in pool)
        {
            if (data.Role == role)
                matching.Add(data);
        }

        if (matching.Count == 0)
        {
            Debug.LogWarning($"нет {pool[0].GetType().Name} с ролью {role}");
            matching.AddRange(pool);
        }

        return matching[teamRandom.Next(matching.Count)];
    }

    private void SpawnTeam(List<UnitData> team, Team side, Transform[] line, int vanguardSize)
    {
        int front = 0;
        int back = line.Length - 1;

        int count = Mathf.Min(team.Count, line.Length);

        for (int i = 0; i < count; i++)
        {
            UnitData data = team[i];
            int slot;

            if (data.Role == UnitRole.Melee)
                slot = front++;
            else
                slot = back--;

            BattleRow row;

            if (slot < vanguardSize)
                row = BattleRow.Vanguard;
            else
                row = BattleRow.Rearguard;

            Spawn(data, side, slot, row, line[slot]);
        }
    }

    private void Spawn(UnitData data, Team side, int slot, BattleRow row, Transform position)
    {
        GameObject instance = Instantiate(data.Prefab, position.position, position.rotation, transform);
        BattleUnit unit = new BattleUnit(data, data.RollStats(teamRandom), data.RollAbilities(teamRandom), side, slot, row);
        units.Add(unit);

        UnitView view = instance.AddComponent<UnitView>();
        view.Bind(unit);
        views.Add(view);
        presenter.Register(view);
    }
}
