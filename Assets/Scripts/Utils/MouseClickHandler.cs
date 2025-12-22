using UnityEngine;

public class MouseClickHandler : MonoBehaviour
{

    [SerializeField] InputReader input;


    private void Awake()
    {
        //input.mouseEvent += HandleMousePositionChange;
        //input.mouseEvent += HandleMousePositionChange;
    }


    Vector2 mousePosition = new Vector2(0, 0);

    void HandleMousePositionChange(Vector2 newPos)
    {
        mousePosition = newPos;
        Debug.Log(mousePosition);
    }

    public Vector2 GetMousePosition()
    {
        return mousePosition;
    }
}