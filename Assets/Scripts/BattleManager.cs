using System;
using System.Collections.Generic;
using System.Linq;
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

    [Header("Visuals")]
    [SerializeField] private GameObject activeMarkerPrefab;
    [SerializeField] private Transform hoverMarker;
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

        selector.Init(views, activeMarkerPrefab, hoverMarker, targetMarker);
    }

    private void Start()
    {
        Battlefield field = new Battlefield(units);
        CombatRules rules = new CombatRules(field, new System.Random());
        AiCommandSource ai = new AiCommandSource(rules, TimeSpan.FromSeconds(enemyThinkDelay));
        BattleLoop loop = new BattleLoop(field, rules, battleUI, ai, presenter);

        battleUI.Bind(loop, rules, field, selector);
        battleUI.ExitRequested += ExitToMenu;
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
            battleUI.ExitRequested -= ExitToMenu;

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
        Transform[] line = BuildLine(npcMeleePositions, npcRangePositions);

        List<UnitData> team = new List<UnitData> { playerData };
        team.AddRange(BattleSettings.NpcRoles.Select(role => PickByRole(npcPool, role)));

        SpawnTeam(team, Team.Player, line, npcMeleePositions.Length);
    }

    private void SpawnEnemyTeam()
    {
        Transform[] line = BuildLine(enemyMeleePositions, enemyRangePositions);
        List<UnitData> team = BattleSettings.EnemyRoles.Select(role => PickByRole(enemyPool, role)).ToList<UnitData>();

        SpawnTeam(team, Team.Enemy, line, enemyMeleePositions.Length);
    }

    private static Transform[] BuildLine(Transform[] meleePositions, Transform[] rangePositions)
    {
        return meleePositions.Concat(rangePositions).ToArray();
    }

    private T PickByRole<T>(T[] pool, UnitRole role) where T : UnitData
    {
        T[] matching = pool.Where(data => data.Role == role).ToArray();

        if (matching.Length == 0)
        {
            Debug.LogWarning($"No {typeof(T).Name} with role {role} in pool, picking any");
            matching = pool;
        }

        return matching[teamRandom.Next(matching.Length)];
    }

    private void SpawnTeam(IEnumerable<UnitData> team, Team side, Transform[] line, int vanguardSize)
    {
        int front = 0;
        int back = line.Length - 1;

        foreach (UnitData data in team.Take(line.Length))
        {
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
