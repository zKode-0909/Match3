using TMPro;
using UnityEngine;

public class HighScores : MonoBehaviour
{
    [SerializeField] TMP_Text text;
    const int Count = 10;
    int[] highScores = new int[Count];

    void OnEnable()
    {
        LoadScores();
        RefreshText();
    }

    void LoadScores()
    {
        for (int i = 0; i < Count; i++)
            highScores[i] = PlayerPrefs.GetInt($"highScore{i}", 0); // use 0..9 keys
    }

    void SaveScores()
    {
        for (int i = 0; i < Count; i++)
            PlayerPrefs.SetInt($"highScore{i}", highScores[i]);

        PlayerPrefs.Save();
    }

    public void TryInsertScore(int newScore)
    {
        // Find where it belongs
        int insertIndex = -1;
        for (int i = 0; i < Count; i++)
        {
            if (newScore > highScores[i])
            {
                insertIndex = i;
                break;
            }
        }

        if (insertIndex == -1) return;

        // Shift down to make space
        for (int i = Count - 1; i > insertIndex; i--)
            highScores[i] = highScores[i - 1];

        // Insert new score
        highScores[insertIndex] = newScore;

        SaveScores();
        RefreshText();
    }

    void RefreshText()
    {
        text.text = "";
        for (int i = 0; i < Count; i++)
            text.text += $"{i + 1,2}. {highScores[i]}\n";
    }
}