using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class LeaderboardUI : MonoBehaviour
{
    public static LeaderboardUI Instance;
    
    [Header("UI References")]
    public GameObject endScreenPanel; // The semi-transparent background
    public Text leaderboardText; 
    public Button replayButton;

    private const string PREF_KEY = "MazeLeaderboard";

    private void Awake()
    {
        Instance = this;
        // Hide at start
        if (endScreenPanel) endScreenPanel.SetActive(false);
        
        if (replayButton)
        {
            replayButton.onClick.AddListener(OnReplayClicked);
        }
    }

    public void ShowWinScreen(float time)
    {
        AddScore(time);
        if (endScreenPanel) endScreenPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    // Call this if we just want to show "Game Over" without saving score
    public void ShowGameOverScreen()
    {
        if (endScreenPanel) endScreenPanel.SetActive(true);
        if (leaderboardText) leaderboardText.text = "GAME OVER\n\nCaught by Minotaur!";
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void AddScore(float time)
    {
        List<float> scores = GetScores();
        scores.Add(time);
        scores.Sort();
        
        // Keep top 5
        if (scores.Count > 5) scores.RemoveRange(5, scores.Count - 5);
        
        SaveScores(scores);
        UpdateUI(time); // Pass current time to highlight or display
    }

    private void OnReplayClicked()
    {
        if (GameManager.Instance) GameManager.Instance.RestartGame();
    }

    private void UpdateUI(float currentTime)
    {
        if (leaderboardText == null) return;

        List<float> scores = GetScores();
        string display = $"VICTORY!\nYOUR TIME: {currentTime:F2}s\n\nTOP TIMES:\n";
        for (int i = 0; i < scores.Count; i++)
        {
            display += $"{i+1}. {scores[i]:F2}s\n";
        }
        leaderboardText.text = display;
    }

    private List<float> GetScores()
    {
        string data = PlayerPrefs.GetString(PREF_KEY, "");
        if (string.IsNullOrEmpty(data)) return new List<float>();

        // Use Pipe | separator to avoid conflict with decimal commas in European locales
        return data.Split('|').Select(s => 
        {
            float val;
            // Try parsing with Invariant (dot) first, then Current (comma?)
            if (float.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out val))
                return val;
            return 0f;
        }).Where(f => f > 0).ToList();
    }

    private void SaveScores(List<float> scores)
    {
        // Save using InvariantCulture (dots) and Pipe separator
        string data = string.Join("|", scores.Select(s => s.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        PlayerPrefs.SetString(PREF_KEY, data);
        PlayerPrefs.Save();
    }

    [ContextMenu("Clear Leaderboard")]
    public void ClearLeaderboard()
    {
        PlayerPrefs.DeleteKey(PREF_KEY);
        PlayerPrefs.Save();
        Debug.Log("Leaderboard Cleared!");
        // Update UI if in play mode
        if (leaderboardText) leaderboardText.text = "LEADERBOARD CLEARED";
    }
}
