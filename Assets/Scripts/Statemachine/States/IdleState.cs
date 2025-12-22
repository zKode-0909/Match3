using Unity.VisualScripting;
using UnityEngine;

public class IdleState : BaseBoardState
{
    public IdleState(GameBoard board) : base(board)
    {
    }

    public override void OnEnter() {
        Debug.Log("Entering idle state");
    }

    public override void Update()
    {
        Debug.Log("In idle state");
    }
}
