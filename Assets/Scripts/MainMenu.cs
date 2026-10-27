using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private string battleSceneName;

    public void StartBattle()
    {
        LoadBattleAsync().Forget();
    }

    private async UniTaskVoid LoadBattleAsync()
    {
        BattleSettings.RollSeed();
        canvas.gameObject.SetActive(false);
        await SceneManager.LoadSceneAsync(battleSceneName);
    }
}
