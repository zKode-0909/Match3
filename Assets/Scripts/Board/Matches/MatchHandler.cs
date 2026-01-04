using System.Collections.Generic;
using UnityEngine;

public static class MatchHandler
{

    public static HashSet<Vector2Int> GetMatchGroupFromSeed(int x, int y,GameBoard gameBoard)
    {
        var result = new HashSet<Vector2Int>();

        var h = GetHorizontalRun(x, y,gameBoard);
        if (h.Count >= 3) foreach (var p in h) result.Add(p);

        var v = GetVerticalRun(x, y, gameBoard);
        if (v.Count >= 3) foreach (var p in v) result.Add(p);

        return result; // union handles crosses automatically
    }

    

    public static List<Vector2Int> GetHorizontalRun(int x, int y,GameBoard gameBoard)
    {
        var slot = gameBoard.gridSlots[x, y];
        if (slot == null || slot.type == null) return new List<Vector2Int>();

        var tar = slot.type;

        int left = x;
        while (left - 1 >= 0 && gameBoard.gridSlots[left - 1, y] != null && gameBoard.gridSlots[left - 1, y].type == tar)
            left--;

        int right = x;
        while (right + 1 < gameBoard.cols && gameBoard.gridSlots[right + 1, y] != null && gameBoard.gridSlots[right + 1, y].type == tar)
            right++;

        var run = new List<Vector2Int>();
        for (int i = left; i <= right; i++)
            run.Add(new Vector2Int(i, y));

        return run;
    }

    public static List<Vector2Int> GetVerticalRun(int x, int y,GameBoard gameBoard)
    {
        var slot = gameBoard.gridSlots[x, y];
        if (slot == null || slot.type == null) return new List<Vector2Int>();

        var tar = slot.type;

        int down = y;
        while (down - 1 >= 0 && gameBoard.gridSlots[x, down - 1] != null && gameBoard.gridSlots[x, down - 1].type == tar)
            down--;

        int up = y;
        while (up + 1 < gameBoard.rows && gameBoard.gridSlots[x, up + 1] != null && gameBoard.gridSlots[x, up + 1].type == tar)
            up++;

        var run = new List<Vector2Int>();
        for (int j = down; j <= up; j++)
            run.Add(new Vector2Int(x, j));

        return run;
    }


    public static HashSet<Vector2Int> CheckMatch(GameBoard gameBoard)
    {
        Debug.Log("checking matches");
        HashSet<Vector2Int> matches = new HashSet<Vector2Int>();
        for (int x = 0; x < gameBoard.cols; x++)
        {

            for (int y = 0; y < gameBoard.rows; y++)
            {
                if (y > 1)
                {
                    if (gameBoard.gridSlots[x, y - 1].type == gameBoard.gridSlots[x, y - 2].type && gameBoard.gridSlots[x, y - 1].type == gameBoard.gridSlots[x, y].type)
                    {
                        Debug.Log("MATCH FOUND");
                        var newMatches = GetMatchGroupFromSeed(x, y,gameBoard);
                        foreach (var match in newMatches)
                        {
                            matches.Add(match);
                        }

                    }
                }
                if (x > 1)
                {
                    if (gameBoard.gridSlots[x - 1, y].type == gameBoard.gridSlots[x - 2, y].type && gameBoard.gridSlots[x - 1, y].type == gameBoard.gridSlots[x, y].type && gameBoard.gridSlots[x, y])
                    {
                        Debug.Log("MATCH FOUND");
                        var newMatches = GetMatchGroupFromSeed(x, y, gameBoard);
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
}
