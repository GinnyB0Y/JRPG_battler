using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private string battleSceneName;

    [Header("Player")]
    [SerializeField] private Button decreasePlayerButton;
    [SerializeField] private Button increasePlayerButton;
    [SerializeField] private TMP_Text playerTeamSizeText;

    [Header("Enemy")]
    [SerializeField] private Button decreaseEnemyButton;
    [SerializeField] private Button increaseEnemyButton;
    [SerializeField] private TMP_Text enemyTeamSizeText;

    private void OnEnable()
    {
        decreasePlayerButton.onClick.AddListener(DecreasePlayerTeam);
        increasePlayerButton.onClick.AddListener(IncreasePlayerTeam);
        decreaseEnemyButton.onClick.AddListener(DecreaseEnemyTeam);
        increaseEnemyButton.onClick.AddListener(IncreaseEnemyTeam);
        RefreshTeamSizes();
    }

    private void OnDisable()
    {
        decreasePlayerButton.onClick.RemoveListener(DecreasePlayerTeam);
        increasePlayerButton.onClick.RemoveListener(IncreasePlayerTeam);
        decreaseEnemyButton.onClick.RemoveListener(DecreaseEnemyTeam);
        increaseEnemyButton.onClick.RemoveListener(IncreaseEnemyTeam);
    }

    public void StartBattle()
    {
        LoadBattleAsync().Forget();
    }

    private async UniTaskVoid LoadBattleAsync()
    {
        canvas.gameObject.SetActive(false);
        await SceneManager.LoadSceneAsync(battleSceneName);
    }

    private void DecreasePlayerTeam()
    {
        BattleSettings.PlayerTeamSize--;
        RefreshTeamSizes();
    }

    private void IncreasePlayerTeam()
    {
        BattleSettings.PlayerTeamSize++;
        RefreshTeamSizes();
    }

    private void DecreaseEnemyTeam()
    {
        BattleSettings.EnemyTeamSize--;
        RefreshTeamSizes();
    }

    private void IncreaseEnemyTeam()
    {
        BattleSettings.EnemyTeamSize++;
        RefreshTeamSizes();
    }

    private void RefreshTeamSizes()
    {
        RefreshTeamSize(BattleSettings.PlayerTeamSize, playerTeamSizeText, decreasePlayerButton, increasePlayerButton);
        RefreshTeamSize(BattleSettings.EnemyTeamSize, enemyTeamSizeText, decreaseEnemyButton, increaseEnemyButton);
    }

    private static void RefreshTeamSize(int size, TMP_Text text, Button decreaseButton, Button increaseButton)
    {
        text.text = size.ToString();
        decreaseButton.interactable = size > BattleSettings.MinTeamSize;
        increaseButton.interactable = size < BattleSettings.MaxTeamSize;
    }
}
