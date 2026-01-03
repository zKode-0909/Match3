using TMPro;
using UnityEngine;

public class Lives : MonoBehaviour
{
    public TextMeshProUGUI livesText;
    public int livesLeft;

    private void Awake()
    {
       // livesLeft = 20;


    }
    /*
    private void Start()
    {
        livesLeft = 5;
    }*/

    public void RemoveLife() { 
        livesLeft--;
    }

    public int GetLives() {
        return livesLeft;
    }

    public void SetLives(int lives)
    {
        livesLeft = lives;
    }

    private void Update()
    {
        livesText.text = $"Lives left: {livesLeft}";
    }
}
