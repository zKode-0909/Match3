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
    [SerializeField] int rows;
    [SerializeField] int cols;
    [SerializeField] List<EltType> types = new List<EltType>();
    [SerializeField] EltType emptyElt;
    [SerializeField] GridSlot prefab;
    [SerializeField] GameObject panel;
    [SerializeField] InputReader inputReader;
    [SerializeField] GraphicCaster caster;
    //[SerializeField] GameManager gameManager;
   // GameManager gameManager;
    [SerializeField] Score score;
    [SerializeField] Lives lives;
    [SerializeField] HighScores highScores;
    private GridLayoutGroup grid;
    GridSlot[,] gridSlots;
    private GridSlot prevSelection;
    private GridSlot currSelection;
    private HashSet<Vector2Int> markedToDestroy;
    [SerializeField] Vector2 cellSize;
    [SerializeField] Vector2 cellSpacing;
    [SerializeField] RectOffset padding;
    StateMachine stateMachine;
    float boardWidth;
    float boardHeight;
    Vector2 boardDimensions;
    bool performAction = false;
    bool gameOver = false;
   

    Vector2 boardOffset;


    private void OnEnable()
    {
        gameOver = false;
        inputReader.ClickEvent += HandleClick;

    }

    private void OnDisable()
    {
        inputReader.ClickEvent -= HandleClick;
    }

    private void OnDestroy()
    {
        inputReader.ClickEvent -= HandleClick;
    }

    private void Start()
    {

        //gameManager = new GameManager()
        gameOver = false;
        stateMachine = new StateMachine();
        boardWidth = padding.left + padding.right +
             cols * cellSize.x + (cols - 1) * cellSpacing.x;
        
        boardHeight = padding.top + padding.bottom +
                       rows * cellSize.y + (rows - 1) * cellSpacing.y;

        boardDimensions = new Vector2(boardWidth, boardHeight);
        markedToDestroy = new HashSet<Vector2Int>();
        //prevSelection = new GridSlot().Init();
        gridSlots = new GridSlot[cols, rows];
        grid = panel.GetComponent<GridLayoutGroup>();
        

        /*
        var idleState = new IdleState(this);
        var swappingState = new SwappingState(this);
        var actionState = new BoardActionState(this);

        At(idleState,swappingState,new FuncPredicate(()=> currSelection != null && prevSelection == null));
        At(swappingState, idleState, new FuncPredicate(() => currSelection == null && prevSelection == null));
        At(swappingState, actionState, new FuncPredicate(() => currSelection != null && prevSelection != null));
        At(actionState, idleState, new FuncPredicate(() => currSelection == null && prevSelection == null));

        stateMachine.SetState(idleState);
        */
        ResizeBoard();
        InitializeBoard(types);
    }



    private void ResizeBoard()
    {


        float width =
            padding.left +
        padding.right +
        (cols * cellSize.x) +
            ((cols - 1) * cellSpacing.x);

        float height =
            padding.top +
            padding.bottom +
            (rows * cellSize.y) +
            ((rows - 1) * cellSpacing.y);



        panel.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);


        Debug.Log($"resized grid with width {width} and height {height}");
    }


    public void HandleClick()
    {
        if (lives.livesLeft <= 0) {
            Debug.Log($"This game is over pal, game over- lives left is {lives.livesLeft}");
            return;
        }

        if (caster == null) return;
        List<RaycastResult> results = caster.graphicCast(LayerMask.NameToLayer("Match3Slot"));


  

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
                currSelection.type.OnLeftClick(currSelection.x, currSelection.y);
                //Debug.Log($"currSelected: {currSelection.x},{currSelection.y}");
            }

            if (prevSelection != null && currSelection == null) {
               
                //Debug.Log($"prev selected: {prevSelection.x},{prevSelection.y}");
                
            }
            
            if (prevSelection == null && currSelection == null) {
                //Debug.Log("NONE SELECTED");
            }


           
        }



        //
        if (prevSelection != null && currSelection != null && prevSelection != currSelection && IsAdjacent(prevSelection.x, prevSelection.y, currSelection.x, currSelection.y))
        {
            //Debug.Log($"switch with previous selection {prevSelection.x},{prevSelection.y}    currentSelection {currSelection.x},{currSelection.y}");
            //performAction = true;
            //stateMachine.SetState(new BoardActionState(this));
            currSelection.HideHighlight();
            prevSelection.HideHighlight();
            bool found = true;
            SwapElts(prevSelection, currSelection);
            StartCoroutine(ResolveBoardRoutine());

            /*
            while (found == true) {
                
                

                HashSet<Vector2Int> matchesFound = CheckMatch();
                if (matchesFound.Count > 0)
                {
                    found = true;
                }
                else { 
                    found = false;
                }
                var toClear = matchesFound.ToList(); // stable copy
                matchesFound.Clear();
                //Debug.Log($"elts being destroyed: {toClear.Count}");
                foreach (var destroyed in toClear)
                {*/
            /*
            GridSlot testEmptySlot = Instantiate(prefab);
            testEmptySlot.transform.SetParent(panel.transform, false);
            testEmptySlot.Init(emptyElt, destroyed.x, destroyed.y);
            var rt = testEmptySlot.GetComponent<RectTransform>();
            rt.anchoredPosition = PositionFromXY_TopLeft(testEmptySlot.x, testEmptySlot.y);
            rt.sizeDelta = new Vector2(cellSize.x, cellSize.y);*/
            /* Destroy(gridSlots[destroyed.x, destroyed.y].gameObject);
             gridSlots[destroyed.x, destroyed.y] = null;//testEmptySlot;

         }
         MakeEltsFall();
         toClear.Clear();

         RepopulateBoard();
     }*/

            Debug.Log("swapped elts");
            currSelection = null;
            prevSelection = null;
            //performAction = false;
            // gameManager.SpendLife();
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
        // you probably do SwapElts before starting this
        bool found = true;

        while (found)
        {
            var matchesFound = CheckMatch(); // returns HashSet<Vector2Int>

            found = matchesFound.Count > 0;
            if (!found) break;

            // clear
            foreach (var p in matchesFound)
            {
                if (gridSlots[p.x, p.y] == null) continue;
                //gameManager.AddScore(gridSlots[p.x, p.y].type.score);
                score.IncreaseScore(gridSlots[p.x, p.y].type.score);
                gridSlots[p.x, p.y].type.OnDestroy();
                Destroy(gridSlots[p.x, p.y].gameObject);
                gridSlots[p.x, p.y] = null;
            }

            // fall (animate) and WAIT
            var fallSeq = MakeEltsFallTween(0.5f);
            yield return fallSeq.WaitForCompletion();

            // repopulate (you can animate these too if you want)
            RepopulateBoard();

            // OPTIONAL: if repopulate spawns with tweens, wait for that too
            // yield return repopSeq.WaitForCompletion();

            // loop again for cascades
        }
    }


    System.Collections.IEnumerator MakeSelection() {
        
        yield return new WaitForSeconds(10f);
    }

    Vector2 PositionFromXY(int x, int y)
    {
        return new Vector2(
            padding.left + x * (cellSize.x + cellSpacing.x),
            -(padding.top + y * (cellSize.y + cellSpacing.y))
        );
    }

    Vector2 Step => new Vector2(cellSize.x + cellSpacing.x, cellSize.y + cellSpacing.y);

    Vector2 PositionFromXY_TopLeft(int x, int y)
    {
        var step = Step;
        float startX = (-boardDimensions.x * 0.5f + padding.left) + (cellSize.x / 2);
        float startY = (boardDimensions.y * 0.5f - padding.top) - (cellSize.y / 2); //+ 15.5f;

        return new Vector2(
            startX + x * step.x,
            startY - y * step.y
        );
    }

    HashSet<Vector2Int> GetMatchGroupFromSeed(int x, int y)
    {
        var result = new HashSet<Vector2Int>();

        var h = GetHorizontalRun(x, y);
        if (h.Count >= 3) foreach (var p in h) result.Add(p);

        var v = GetVerticalRun(x, y);
        if (v.Count >= 3) foreach (var p in v) result.Add(p);

        return result; // union handles crosses automatically
    }

    bool InBounds(int x, int y) => x >= 0 && x < cols && y >= 0 && y < rows;

    List<Vector2Int> GetHorizontalRun(int x, int y)
    {
        var slot = gridSlots[x, y];
        if (slot == null || slot.type == null) return new List<Vector2Int>();

        var tar = slot.type;

        int left = x;
        while (left - 1 >= 0 && gridSlots[left - 1, y] != null && gridSlots[left - 1, y].type == tar)
            left--;

        int right = x;
        while (right + 1 < cols && gridSlots[right + 1, y] != null && gridSlots[right + 1, y].type == tar)
            right++;

        var run = new List<Vector2Int>();
        for (int i = left; i <= right; i++)
            run.Add(new Vector2Int(i, y));

        return run;
    }

    List<Vector2Int> GetVerticalRun(int x, int y)
    {
        var slot = gridSlots[x, y];
        if (slot == null || slot.type == null) return new List<Vector2Int>();

        var tar = slot.type;

        int down = y;
        while (down - 1 >= 0 && gridSlots[x, down - 1] != null && gridSlots[x, down - 1].type == tar)
            down--;

        int up = y;
        while (up + 1 < rows && gridSlots[x, up + 1] != null && gridSlots[x, up + 1].type == tar)
            up++;

        var run = new List<Vector2Int>();
        for (int j = down; j <= up; j++)
            run.Add(new Vector2Int(x, j));

        return run;
    }

    void RepopulateBoard() {
        for (int x = 0; x < cols; x++) {
            for (int y = 0; y < rows; y++) {
                if (gridSlots[x, y] == null) {
                    gridSlots[x,y] = BuildSlot(x, y);

                }
            }
        }
    }

    HashSet<Vector2Int> CheckMatch() {
        Debug.Log("checking matches");
        HashSet<Vector2Int> matches = new HashSet<Vector2Int>();
        for (int x = 0; x < cols; x++) {
            
            for (int y = 0; y < rows; y++) {
                if (y > 1) {
                    if (gridSlots[x, y - 1].type == gridSlots[x, y - 2].type && gridSlots[x,y-1].type == gridSlots[x,y].type) {
                        Debug.Log("MATCH FOUND");
                        var newMatches = GetMatchGroupFromSeed(x, y);
                        foreach (var match in newMatches) { 
                            matches.Add(match);
                        }
                        
                    }
                }
                if (x > 1)
                {
                    if (gridSlots[x - 1,y].type == gridSlots[x -2, y].type && gridSlots[x -1, y].type == gridSlots[x, y].type && gridSlots[x,y])
                    {
                        Debug.Log("MATCH FOUND");
                        var newMatches = GetMatchGroupFromSeed(x, y);
                        foreach (var match in newMatches)
                        {
                            matches.Add(match);
                        }
                    }
                }
            }
        }

        return matches;

      
    }

    bool IsAdjacent(int x1, int y1, int x2, int y2)
    {
        return Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2) == 1;
    }


    List<EltType> PickValidTypes(int x, int y,List<EltType> types)
    {
        var possible = new List<EltType>(types);

        // Horizontal check: prevent making xxx
        if (x >= 2)
        {
            var left1 = gridSlots[x - 1, y];
            var left2 = gridSlots[x - 2, y];

            if (left1 != null && left2 != null && left1.type == left2.type)
            {
                possible.Remove(left1.type);
            }
        }

        // Vertical check: prevent making xxx
        if (y >= 2)
        {
            var down1 = gridSlots[x, y - 1];
            var down2 = gridSlots[x, y - 2];

            if (down1 != null && down2 != null && down1.type == down2.type)
            {
                possible.Remove(down1.type);
            }
        }

        return possible;
    }


    public void SwapElts(GridSlot a,GridSlot b) {
        // cache coords first
        int ax = a.x, ay = a.y;
        int bx = b.x, by = b.y;

        // swap in your logical array
        gridSlots[ax, ay] = b;
        gridSlots[bx, by] = a;

        // update the slot coords
        a.x = bx; a.y = by;
        b.x = ax; b.y = ay;
        
        var art = a.GetComponent<RectTransform>();
        var brt = b.GetComponent<RectTransform>();

        art.anchoredPosition = PositionFromXY_TopLeft(a.x, a.y);//new Vector2(a.x, a.y);
        brt.anchoredPosition = PositionFromXY_TopLeft(b.x, b.y);// new Vector2 (b.x, b.y);

        // swap UI order (GridLayoutGroup uses sibling order)
        /*
        int aIdx = a.transform.GetSiblingIndex();
        int bIdx = b.transform.GetSiblingIndex();
        a.transform.SetSiblingIndex(bIdx);
        b.transform.SetSiblingIndex(aIdx);
        */
        // IMPORTANT: do NOT Destroy(a) or Destroy(b)
      
        
        //CollapseColumns();




    }

    private void Update()
    {
       // Debug.Log($"lives left = {lives.livesLeft} gameOver status: {gameOver} instanceID: {GetInstanceID()}");
        if (lives.livesLeft <= 0) { 
            gameOver = true;
            highScores.TryInsertScore(score.currentScore);
        }
        //Debug.Log($"curr {currSelection}  prev {prevSelection}");
       // stateMachine.Update();
    }

    private void FixedUpdate()
    {
        //stateMachine.FixedUpdate();
    }


    public void AnimateSwap(GridSlot a, GridSlot b, float duration = 0.15f)
    {
        RectTransform art = a.GetComponent<RectTransform>();
        RectTransform brt = b.GetComponent<RectTransform>();

        // cache positions BEFORE changing logic
        Vector2 posA = art.anchoredPosition;
        Vector2 posB = brt.anchoredPosition;

        // animate
        Sequence seq = DOTween.Sequence();

        seq.Join(art.DOAnchorPos(posB, duration).SetEase(Ease.OutQuad));
        seq.Join(brt.DOAnchorPos(posA, duration).SetEase(Ease.OutQuad));

        seq.OnComplete(() =>
        {
            // NOW swap logical data
            SwapElts(a, b);
            HashSet<Vector2Int> matchesFound = CheckMatch();
            var toClear = matchesFound.ToList(); // stable copy
            matchesFound.Clear();

            foreach (var destroyed in toClear)
            {
                //GridSlot testEmptySlot = Instantiate(prefab);
                //testEmptySlot.transform.SetParent(panel.transform, false);
                //testEmptySlot.Init(emptyElt, destroyed.x, destroyed.y);
                //var rt = testEmptySlot.GetComponent<RectTransform>();
                //rt.anchoredPosition = PositionFromXY_TopLeft(testEmptySlot.x, testEmptySlot.y);
                //rt.sizeDelta = new Vector2(cellSize.x, cellSize.y);
                Destroy(gridSlots[destroyed.x, destroyed.y].gameObject);
                gridSlots[destroyed.x,destroyed.y] = null;
                
            }
            
            MakeEltsFall();
            toClear.Clear();
        });
    }




    Sequence MakeEltsFallTween(float duration = 0.2f)
    {
        Sequence seq = DOTween.Sequence();

        for (int x = cols - 1; x >= 0; x--)
        {
            for (int y = rows - 1; y >= 0; y--)
            {
                if (gridSlots[x, y] != null) continue;

                for (int i = y - 1; i >= 0; i--)
                {
                    var moved = gridSlots[x, i];
                    if (moved == null) continue;

                    gridSlots[x, y] = moved;
                    gridSlots[x, i] = null;

                    moved.x = x;
                    moved.y = y;

                    RectTransform rt = moved.GetComponent<RectTransform>();
                    rt.DOKill();

                    seq.Join(rt.DOAnchorPos(PositionFromXY_TopLeft(x, y), duration)
                              .SetEase(Ease.OutQuad));

                    break;
                }
            }
        }

        return seq;
    }


    void MakeEltsFall()
    {
        

        for (int x = cols - 1; x >= 0; x--)
        {
            for (int y = rows-1; y >= 0; y--)
            {
                if (gridSlots[x, y] == null) {
                    //Debug.Log($"found null at {x},{y}");
                    for (int i = y - 1; i >=0; i--)
                    {
                        if (gridSlots[x, i] != null) {
                            gridSlots[x,y] = gridSlots[x, i];

                            gridSlots[x, i] = null;

                            gridSlots[x, y].x = x;
                            gridSlots[x, y].y = y;

                            RectTransform rt = gridSlots[x,y].GetComponent<RectTransform>();
                            rt.anchoredPosition = PositionFromXY_TopLeft(x, y);
                           
                            break;
                        }
                    }
                }

               
            }
        }

    }

    public void InitializeBoard(List<EltType> options) {
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                
                gridSlots[x, y] = BuildSlot(x,y);                             
            }
        }


    }


    public GridSlot BuildSlot(int x,int y) {
        var filteredOptions = PickValidTypes(x, y, types);
        var option = Pick(filteredOptions);

        GridSlot slot = Instantiate(prefab);
        slot.transform.SetParent(panel.transform, false);
        slot.Init(option, x, y);
        var rt = slot.GetComponent<RectTransform>();
        rt.anchoredPosition = PositionFromXY_TopLeft(x, y);

        
        rt.sizeDelta = new Vector2(cellSize.x, cellSize.y);


        return slot;
    }





    public EltType Pick(List<EltType> options)
    {
        if (options == null || options.Count == 0)
            throw new ArgumentException("No options provided.");

        float total = 0f;
        for (int i = 0; i < options.Count; i++)
        {
            float w = options[i].weight;
            if (w > 0f) total += w;
        }

        if (total <= 0f)
            throw new ArgumentException("All weights are zero.");

        float roll = UnityEngine.Random.value * total; // [0, total)

        for (int i = 0; i < options.Count; i++)
        {
            float w = options[i].weight;
            if (w <= 0f) continue;

            roll -= w;
            if (roll < 0f)
                return options[i];
        }

        // Fallback (shouldn't happen due to float precision, but safe)
        return options[options.Count - 1];
    }
}

