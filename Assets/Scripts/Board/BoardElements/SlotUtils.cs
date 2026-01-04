using System.Collections.Generic;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

public static class SlotUtils
{
    

    public static Vector2 PositionFromXY_TopLeft(int x, int y,GameBoard gameBoard)
    {
        var step = new Vector2(gameBoard.cellSize.x + gameBoard.cellSpacing.x,gameBoard.cellSize.y + gameBoard.cellSpacing.y);
        float startX = (-gameBoard.boardDimensions.x * 0.5f + gameBoard.padding.left) + (gameBoard.cellSize.x / 2);
        float startY = (gameBoard.boardDimensions.y * 0.5f - gameBoard.padding.top) - (gameBoard.cellSize.y / 2); //+ 15.5f;

        return new Vector2(
            startX + x * step.x,
            startY - y * step.y
        );
    }

    public static GridSlot BuildSlot(int x, int y,GameBoard gameBoard)
    {
        var filteredOptions = PickValidTypes(x, y, gameBoard);
        var option = Pick(filteredOptions);
        var slot = GridSlotFactory.Spawn(option);

        //GridSlot slot = gameBoard.InstantiateSlot();
        slot.transform.SetParent(gameBoard.panel.transform, false);
        slot.Init(x, y);
        var rt = slot.GetComponent<RectTransform>();
        rt.anchoredPosition = PositionFromXY_TopLeft(x, y,gameBoard);


        rt.sizeDelta = new Vector2(gameBoard.cellSize.x, gameBoard.cellSize.y);


        return slot;
    }


    public static EltType Pick(List<EltType> options)
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


    public static List<EltType> PickValidTypes(int x, int y, GameBoard gameBoard)
    {
        var possible = new List<EltType>(gameBoard.types);

        // Horizontal check: prevent making xxx
        if (x >= 2)
        {
            var left1 = gameBoard.gridSlots[x - 1, y];
            var left2 = gameBoard.gridSlots[x - 2, y];

            if (left1 != null && left2 != null && left1.type == left2.type)
            {
                possible.Remove(left1.type);
            }
        }

        // Vertical check: prevent making xxx
        if (y >= 2)
        {
            var down1 = gameBoard.gridSlots[x, y - 1];
            var down2 = gameBoard.gridSlots[x, y - 2];

            if (down1 != null && down2 != null && down1.type == down2.type)
            {
                possible.Remove(down1.type);
            }
        }

        return possible;
    }
}
