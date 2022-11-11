using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class State{
    int n;
    public int[,] board;
    List<Vector2> offsets = new List<Vector2>(); 
    public State(int n) {
        this.n = n;
        board = new int[n, n];
        offsets.Add(new Vector2(0, 1));
        offsets.Add(new Vector2(1, 0));
        offsets.Add(new Vector2(1, 1));
        offsets.Add(new Vector2(-1, 1));
    }
    public State PlaceChess(int x, int y, int color)
    {
        State newState = new State(n);
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                newState.board[i, j] = this.board[i, j];
        newState.board[x, y] = color;
        return newState;
    }
    bool PosOnBoard(Vector2 pos)
    {
        int x = (int)pos.x, y = (int)pos.y;
        if (x < 0 || x >= n) return false;
        if (y < 0 || y >= n) return false;
        return true;
    }
    public bool GameOver()
    {
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                foreach(var offset in offsets)
                {
                    bool flag = true;
                    int colorSum = 0;
                    for (int k = 0; k < 5; k++)
                    {
                        int x = i + (int)offset.x * k;
                        int y = j + (int)offset.y * k;
                        if (!PosOnBoard(new Vector2(x, y)) || board[x, y] == -1)
                        {
                            flag = false;
                            break;
                        }
                        colorSum += board[x, y];
                    }
                    if (!flag) continue;
                    if (colorSum == 0 || colorSum == 5) return true;
                }
        return false;
    }
}