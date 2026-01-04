using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Drawing;
using DG.Tweening;
using System.Collections;



public class GameBoard : MonoBehaviour
{
    public int rows;
    public int cols;
    public List<EltType> types = new List<EltType>();
    [SerializeField] GridSlot prefab;
    public GameObject panel;
    [SerializeField] InputReader inputReader;
    [SerializeField] GraphicCaster caster;
    //[SerializeField] GameManager gameManager;
   // GameManager gameManager;
    [SerializeField] Score score;
    [SerializeField] Lives lives;
    [SerializeField] HighScores highScores;
    public GridSlot[,] gridSlots;
    private GridSlot prevSelection;
    private GridSlot currSelection;
    public Vector2 cellSize;
    public Vector2 cellSpacing;
    public RectOffset padding;
    StateMachine stateMachine;
    public float boardWidth;
    public float boardHeight;
    public Vector2 boardDimensions;
    bool performAction = false;
    bool gameOver = false;
    bool lockInput = false;
   

    Vector2 boardOffset;


    private void OnEnable()
    {

        gameOver = false;
        stateMachine = new StateMachine();
        //boardDimensions = new Vector2(boardWidth, boardHeight);

        //prevSelection = new GridSlot().Init();
        gridSlots = new GridSlot[cols, rows];
        inputReader.ClickEvent += HandleClick;

        BoardManager.InitializeBoard(this);
        

    }

    private void OnDisable()
    {
        inputReader.ClickEvent -= HandleClick;
    }

    public void HandleClick()
    {
        if (lives.livesLeft <= 0) {
            Debug.Log($"This game is over pal, game over- lives left is {lives.livesLeft}");
            return;
        }

        if (caster == null) return;
        List<RaycastResult> results = caster.graphicCast(LayerMask.NameToLayer("Match3Slot"));

        if (lockInput == true) return;

        if (results.Count == 0) {
            //prevSelection = null;
            if (currSelection != null) {
                currSelection.HideHighlight();
                currSelection = null;
                prevSelection = null;
                
            }
            
            Debug.Log("None selected");
            return;
        }

        for (int i = 0; i < results.Count; i++)
        {
            prevSelection = currSelection;
            //Debug.Log($"results i: {results[i]}");
            currSelection = results[i].gameObject.GetComponent<GridSlot>();

            currSelection.ShowHighlightAt(new Vector2(currSelection.x, currSelection.y), padding.right, cellSize);
            
            
           // currSelection.ShowHighlightAt(PositionFromXY_TopLeft(currSelection.x, currSelection.y), 0, cellSize);
            if (currSelection != null) {
                currSelection.HandleLeftClick();
                //Debug.Log($"currSelected: {currSelection.x},{currSelection.y}");
            }

            if (prevSelection != null && currSelection == null) {
               
                //Debug.Log($"prev selected: {prevSelection.x},{prevSelection.y}");
                
            }
            
            if (prevSelection == null && currSelection == null) {
                //Debug.Log("NONE SELECTED");
            }


           
        }

        if (prevSelection != null && currSelection != null && prevSelection != currSelection && IsAdjacent(prevSelection.x, prevSelection.y, currSelection.x, currSelection.y))
        {
            currSelection.HideHighlight();
            prevSelection.HideHighlight();
            bool found = true;
            BoardManager.SwapElts(prevSelection, currSelection,this);
            
            StartCoroutine(ResolveBoardRoutine());

            Debug.Log("swapped elts");
            currSelection = null;
            prevSelection = null;
            lives.RemoveLife();

        }
        else if (prevSelection != null && currSelection != null && prevSelection != currSelection && !IsAdjacent(prevSelection.x, prevSelection.y, currSelection.x, currSelection.y))
        {
            // Debug.Log($"Not adjacent! keeping {prevSelection.x},{prevSelection.y} selected");
            prevSelection.HideHighlight();
            //currSelection = prevSelection;
            prevSelection = null;
        }
        else if (prevSelection != null && currSelection == null) {
            Debug.Log($"herheehrerehr0");
            prevSelection.HideHighlight();
            prevSelection = null;
        }

        
    }

    IEnumerator ResolveBoardRoutine()
    {
        lockInput = true;


        try
        {
            while (true)
            {
                var matchesFound = MatchHandler.CheckMatch(this);
                if (matchesFound.Count == 0)
                    break;

                // clear
                foreach (var p in matchesFound)
                {
                    var slot = gridSlots[p.x, p.y];
                    if (slot == null || slot.type == null) continue;

                    score.IncreaseScore(slot.type.score);
           

                    //Destroy(slot.gameObject);
                    GridSlotFactory.ReturnToPool(slot);
                    gridSlots[p.x, p.y] = null;
                }

                // fall and WAIT
                var fallSeq = BoardManager.MakeEltsFallTween(this, 0.5f);
                yield return fallSeq.WaitForCompletion();

                // repopulate
                BoardManager.RepopulateBoard(this);

                // optional: wait a frame so UI/layout updates before next match scan
                yield return null;
            }
        }
        finally
        {
            lockInput = false;

            if (lives.livesLeft <= 0)
            {
                gameOver = true;
                highScores.TryInsertScore(score.currentScore);
            }
        }
    }




    bool IsAdjacent(int x1, int y1, int x2, int y2)
    {
        return Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2) == 1;
    }


    public GridSlot InstantiateSlot() {
        return Instantiate(prefab);
    }


   

    private void Update()
    {
       // Debug.Log($"lives left = {lives.livesLeft} gameOver status: {gameOver} instanceID: {GetInstanceID()}");
        
        //Debug.Log($"curr {currSelection}  prev {prevSelection}");
       // stateMachine.Update();
    }

    private void FixedUpdate()
    {
        //stateMachine.FixedUpdate();
    }


   




   


   

}

