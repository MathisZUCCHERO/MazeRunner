using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Settings")]
    public float gameTime = 0f;
    public bool isGameActive = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Optional: Keep logic across scenes if needed
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public UnityEngine.UI.Text timerText;

    void Start()
    {
        if (timerText == null)
        {
            GameObject tObj = GameObject.Find("TimerText");
            if (tObj) timerText = tObj.GetComponent<UnityEngine.UI.Text>();
        }
        StartGame();
    }

    void Update()
    {
        if (isGameActive)
        {
            gameTime += Time.deltaTime;
            if (timerText) timerText.text = $"TIME: {gameTime:F2}";
        }
    }

    public void StartGame()
    {
        isGameActive = true;
        gameTime = 0f;
        Time.timeScale = 1f;
    }

    public void GameOver()
    {
        isGameActive = false;
        Time.timeScale = 0f; // Pause game
        Debug.Log("Game Over! Caught by Minotaur.");
        
        if (LeaderboardUI.Instance != null)
        {
            LeaderboardUI.Instance.ShowGameOverScreen();
        }
    }

    public void WinGame()
    {
        isGameActive = false;
        Time.timeScale = 0f; // Pause game
        Debug.Log($"You Escaped! Time: {gameTime:F2}s");
        
        if (LeaderboardUI.Instance != null)
        {
            LeaderboardUI.Instance.ShowWinScreen(gameTime);
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
