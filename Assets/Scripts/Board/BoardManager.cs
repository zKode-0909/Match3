using DG.Tweening;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public static class BoardManager
{
    

    public static void RepopulateBoard(GameBoard gameBoard)
    {
        for (int x = 0; x < gameBoard.cols; x++)
        {
            for (int y = 0; y < gameBoard.rows; y++)
            {
                if (gameBoard.gridSlots[x, y] == null)
                {
                    gameBoard.gridSlots[x, y] = SlotUtils.BuildSlot(x, y,gameBoard);

                }
            }
        }
    }


    public static void ResizeBoard(GameBoard gameBoard)
    {


        float width =
            gameBoard.padding.left +
        gameBoard.padding.right +
        (gameBoard.cols * gameBoard.cellSize.x) +
            ((gameBoard.cols - 1) * gameBoard.cellSpacing.x);

        float height =
            gameBoard.padding.top +
            gameBoard.padding.bottom +
            (gameBoard.rows * gameBoard.cellSize.y) +
            ((gameBoard.rows - 1) * gameBoard.cellSpacing.y);



        gameBoard.panel.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);


        Debug.Log($"resized grid with width {width} and height {height}");
    }


    public static void InitializeBoard(GameBoard gameBoard)
    {

        gameBoard.boardWidth = gameBoard.padding.left + gameBoard.padding.right +
             gameBoard.cols * gameBoard.cellSize.x + (gameBoard.cols - 1) * gameBoard.cellSpacing.x;

        gameBoard.boardHeight = gameBoard.padding.top + gameBoard.padding.bottom +
                       gameBoard.rows * gameBoard.cellSize.y + (gameBoard.rows - 1) * gameBoard.cellSpacing.y;

        gameBoard.boardDimensions = new Vector2(gameBoard.boardWidth, gameBoard.boardHeight);

        ResizeBoard(gameBoard);

        for (int y = 0; y < gameBoard.rows; y++)
        {
            for (int x = 0; x < gameBoard.cols; x++)
            {

                gameBoard.gridSlots[x, y] = SlotUtils.BuildSlot(x, y,gameBoard);
            }
        }


    }



    public static void SwapElts(GridSlot a, GridSlot b,GameBoard gameBoard)
    {
        // cache coords first
        int ax = a.x, ay = a.y;
        int bx = b.x, by = b.y;

        // swap in your logical array
        gameBoard.gridSlots[ax, ay] = b;
        gameBoard.gridSlots[bx, by] = a;

        // update the slot coords
        a.x = bx; a.y = by;
        b.x = ax; b.y = ay;

        var art = a.GetComponent<RectTransform>();
        var brt = b.GetComponent<RectTransform>();

        art.anchoredPosition = SlotUtils.PositionFromXY_TopLeft(a.x, a.y, gameBoard);//new Vector2(a.x, a.y);
        brt.anchoredPosition = SlotUtils.PositionFromXY_TopLeft(b.x, b.y, gameBoard);// new Vector2 (b.x, b.y);

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



    public static Sequence MakeEltsFallTween(GameBoard gameBoard, float duration = 0.2f)
    {
        Sequence seq = DOTween.Sequence();

        for (int x = gameBoard.cols - 1; x >= 0; x--)
        {
            for (int y = gameBoard.rows - 1; y >= 0; y--)
            {
                if (gameBoard.gridSlots[x, y] != null) continue;

                for (int i = y - 1; i >= 0; i--)
                {
                    var moved = gameBoard.gridSlots[x, i];
                    if (moved == null) continue;

                    gameBoard.gridSlots[x, y] = moved;
                    gameBoard.gridSlots[x, i] = null;

                    moved.x = x;
                    moved.y = y;

                    RectTransform rt = moved.GetComponent<RectTransform>();
                    rt.DOKill();

                    seq.Join(rt.DOAnchorPos(SlotUtils.PositionFromXY_TopLeft(x, y, gameBoard), duration)
                              .SetEase(Ease.OutQuad));

                    break;
                }
            }
        }

        return seq;
    }








}
