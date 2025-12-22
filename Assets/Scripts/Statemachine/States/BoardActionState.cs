using UnityEngine;

public class BoardActionState : BaseBoardState
{
    public BoardActionState(GameBoard board) : base(board)
    {
    }

    public override void OnEnter()
    {
        Debug.Log("entered action state");
    }

    public override void Update()
    {
        Debug.Log("in action state");
    }
}
