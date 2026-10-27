using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private PlayerData playerData;
    [SerializeField] private NpcData[] npcPool;
    [SerializeField] private EnemyData[] enemyPool;

    [SerializeField] private Transform[] npcMeleePositions;
    [SerializeField] private Transform[] npcRangePositions;
    [SerializeField] private Transform[] enemyMeleePositions;
    [SerializeField] private Transform[] enemyRangePositions;

    [SerializeField] private string menuSceneName = "menu_scene";
    [SerializeField, Min(0f)] private float enemyThinkDelay;

    [Header("Visuals")]
    [SerializeField] private GameObject activeMarkerPrefab;
    [SerializeField] private BattleUI battleUI;
    [SerializeField] private BattleCamera battleCamera;

    private readonly List<BattleUnit> units = new List<BattleUnit>();
    private readonly List<UnitView> views = new List<UnitView>();
    private BattlePresenter presenter;

    private void Awake()
    {
        presenter = gameObject.AddComponent<BattlePresenter>();

        if (activeMarkerPrefab != null)
            presenter.CreateActiveMarker(activeMarkerPrefab);

        SpawnPlayerTeam();
        SpawnEnemyTeam();
    }

    private void Start()
    {
        Battlefield field = new Battlefield(units);
        CombatRules rules = new CombatRules(field, new System.Random());
        AiCommandSource ai = new AiCommandSource(rules, TimeSpan.FromSeconds(enemyThinkDelay));
        BattleLoop loop = new BattleLoop(field, rules, battleUI, ai, presenter);

        battleUI.Bind(loop, rules, field, menuSceneName);
        presenter.Bind(loop);

        if (battleCamera != null)
            battleCamera.Bind(loop, views);

        loop.RunAsync(destroyCancellationToken).Forget();
    }

    private void SpawnPlayerTeam()
    {
        Transform[] line = BuildLine(npcMeleePositions, npcRangePositions);
        int size = Mathf.Min(BattleSettings.PlayerTeamSize, line.Length);

        List<UnitData> team = new List<UnitData> { playerData };
        team.AddRange(PickRandom(npcPool, size - 1));

        SpawnTeam(team, Team.Player, line);
    }

    private void SpawnEnemyTeam()
    {
        Transform[] line = BuildLine(enemyMeleePositions, enemyRangePositions);
        int size = Mathf.Min(BattleSettings.EnemyTeamSize, line.Length);

        SpawnTeam(PickRandom(enemyPool, size), Team.Enemy, line);
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

    private void SpawnTeam(IEnumerable<UnitData> team, Team side, Transform[] line)
    {
        int front = 0;
        int back = line.Length - 1;

        foreach (UnitData data in team)
        {
            int slot = data.Role == UnitRole.Melee ? front++ : back--;
            Spawn(data, side, slot, line[slot]);
        }
    }

    private void Spawn(UnitData data, Team side, int slot, Transform position)
    {
        GameObject instance = Instantiate(data.Prefab, position.position, position.rotation, transform);
        BattleUnit unit = new BattleUnit(data, side, slot);
        units.Add(unit);

        UnitView view = instance.AddComponent<UnitView>();
        view.Bind(unit);
        views.Add(view);
        presenter.Register(view);
    }
}
