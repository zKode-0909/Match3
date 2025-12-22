using TMPro;
using UnityEngine;

public class Lives : MonoBehaviour
{
    public TextMeshProUGUI livesText;
    public int livesLeft;

    private void Awake()
    {
        livesLeft = 20;
    }

    public void RemoveLife() { 
        livesLeft--;
    }

    public int GetLives() {
        return livesLeft;
    }

    private void Update()
    {
        livesText.text = livesLeft.ToString();
    }
}
