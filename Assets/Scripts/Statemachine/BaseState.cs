using UnityEngine;

public abstract class BaseBoardState : IState
{
    //protected readonly PlayerController player;
    //protected readonly Animator animator;
    protected readonly GameBoard board;
    
    protected const float crossFadeDuration = 0.1f;

    protected BaseBoardState(GameBoard board)
    {
        this.board = board;
    }

    public virtual void FixedUpdate()
    {
        //throw new System.NotImplementedException();
    }

    public virtual void OnExit()
    {
        Debug.Log("exited state");
    }

    public virtual void Update()
    {
        //throw new System.NotImplementedException();
    }

    public virtual void OnEnter()
    {
        //throw new System.NotImplementedException();
    }

   
}