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
        int size = Mathf.Min(BattleSettings.PlayerTeamSize, line.Length);

        List<UnitData> team = new List<UnitData> { playerData };
        team.AddRange(PickRandom(npcPool, size - 1));

        SpawnTeam(team, Team.Player, line, npcMeleePositions.Length);
    }

    private void SpawnEnemyTeam()
    {
        Transform[] line = BuildLine(enemyMeleePositions, enemyRangePositions);
        int size = Mathf.Min(BattleSettings.EnemyTeamSize, line.Length);

        SpawnTeam(PickRandom(enemyPool, size), Team.Enemy, line, enemyMeleePositions.Length);
    }

    private static Transform[] BuildLine(Transform[] meleePositions, Transform[] rangePositions)
    {
        return meleePositions.Concat(rangePositions).ToArray();
    }

    private IEnumerable<T> PickRandom<T>(T[] pool, int count)
    {
        for (int i = 0; i < count; i++)
            yield return pool[teamRandom.Next(pool.Length)];
    }

    private void SpawnTeam(IEnumerable<UnitData> team, Team side, Transform[] line, int vanguardSize)
    {
        int front = 0;
        int back = line.Length - 1;

        foreach (UnitData data in team)
        {
            int slot = data.Role == UnitRole.Melee ? front++ : back--;
            BattleRow row = slot < vanguardSize ? BattleRow.Vanguard : BattleRow.Rearguard;
            Spawn(data, side, slot, row, line[slot]);
        }
    }

    private void Spawn(UnitData data, Team side, int slot, BattleRow row, Transform position)
    {
        GameObject instance = Instantiate(data.Prefab, position.position, position.rotation, transform);
        BattleUnit unit = new BattleUnit(data, side, slot, row);
        units.Add(unit);

        UnitView view = instance.AddComponent<UnitView>();
        view.Bind(unit);
        views.Add(view);
        presenter.Register(view);
    }
}
