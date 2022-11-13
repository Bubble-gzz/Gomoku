using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickArea : MonoBehaviour
{
    public ChessBoard chessBoard;
    public Vector2 pos;
    void Start()
    {
        
    }

    void Update()
    {
        
    }

    void OnMouseEnter()
    {
        if (!chessBoard.inGame) return;
        chessBoard.CreateGhost(pos);
    }
    void OnMouseExit()
    {
        if (!chessBoard.inGame) return;
        chessBoard.DeleteGhost(pos);
    }
    void OnMouseDown()
    {
        if (!chessBoard.inGame) return;
        StartCoroutine(chessBoard.PlaceChess(pos));
    }
}
