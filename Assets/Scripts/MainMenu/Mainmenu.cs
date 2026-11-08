using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Widoki menu")]
    [SerializeField] private GameObject mainView;
    [SerializeField] private GameObject newGameView;
    [SerializeField] private GameObject continueView;
    [SerializeField] private GameObject highScoreView;
    [SerializeField] private GameObject settingsView;

    [Header("Scena z gra")]
    [Tooltip("Nazwa sceny")]
    [SerializeField] private string gameSceneName = "SampleScene";

    private void Start()
    {
    
        Time.timeScale = 1f;
        ShowMain();
    }


    public void ShowMain() => ShowOnly(mainView);
    public void ShowNewGame() => ShowOnly(newGameView);
    public void ShowContinue() => ShowOnly(continueView);
    public void ShowHighScore() => ShowOnly(highScoreView);
    public void ShowSettings() => ShowOnly(settingsView);

    private void ShowOnly(GameObject view)
    {
        SetActiveSafe(mainView, view == mainView);
        SetActiveSafe(newGameView, view == newGameView);
        SetActiveSafe(continueView, view == continueView);
        SetActiveSafe(highScoreView, view == highScoreView);
        SetActiveSafe(settingsView, view == settingsView);
    }

    private static void SetActiveSafe(GameObject go, bool active)
    {
        if (go != null) go.SetActive(active);
    }

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

   
    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}