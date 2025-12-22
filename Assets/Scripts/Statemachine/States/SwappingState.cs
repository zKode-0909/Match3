using UnityEngine;

public class SwappingState : BaseBoardState
{
    public SwappingState(GameBoard board) : base(board)
    {
    }

    public override void OnEnter()
    {
        Debug.Log($"entered swapping state");
    }

    public override void Update()
    {
        Debug.Log($"in update state");
    }

}
