using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class ChessBoard : MonoBehaviour
{
    [SerializeField]
    GameObject gridLinePrefab;
    [SerializeField]
    int n;
    [SerializeField]
    float boardSize;
    [SerializeField]
    List<GameObject> stones = new List<GameObject>();
    [SerializeField]
    GameObject clickAreaPrefab;
    GameObject[,] chess;
    [SerializeField]
    GameObject debugValuePrefab;
    GameObject[,] debugValues;
    Transform myCanvas;
    int[,] color;
    bool[,] hasChess;
    public int turn, playerTurn;
    List<Vector2> offsets = new List<Vector2>();
    public bool inGame;
    public bool calculating;
    AI computer;
    Vector2[] lastComputerPos = new Vector2[2];
    TMP_Text status;
    bool showDebugValue;
    [SerializeField]
    GameObject chooseColorPanel;
    void Awake()
    {
        showDebugValue = false;
        chess = new GameObject[n, n];
        debugValues = new GameObject[n, n];
        color = new int[n, n];
        hasChess = new bool[n, n];
        offsets.Add(new Vector2(0, 1));
        offsets.Add(new Vector2(1, 0));
        offsets.Add(new Vector2(1, 1));
        offsets.Add(new Vector2(-1, 1));
        myCanvas = transform.Find("Canvas");
        DrawLine();
        Spawn();
    }
    void Start()
    {
        status = GameObject.Find("Canvas/Status").GetComponent<TMP_Text>();
        computer = GameObject.Find("AI").GetComponent<AI>();
        computer.chessBoard = this;
        computer.n = this.n;
        myCanvas.gameObject.SetActive(false);
        SetDebugValue(showDebugValue);
        chooseColorPanel.SetActive(true);
    }
    public void ExitGame()
    {
        Application.Quit();
    }
    void Init()
    {
        lastComputerPos[0] = new Vector2(-1, -1);
        lastComputerPos[1] = new Vector2(-1, -1);
        turn = 0;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                if (chess[i, j] != null) {
                    Destroy(chess[i, j]);
                    chess[i, j] = null;
                }
                hasChess[i, j] = false;
                debugValues[i, j].GetComponentInChildren<TMP_Text>().text = "";
            }
        inGame = true;
    }
    public void SetDebugValue(bool flag)
    {
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                debugValues[i, j].gameObject.SetActive(flag);
    }
    public void StartGame(int computerTurn)
    {
        Init();
        computer.myTurn = computerTurn;
        status.text = "Your Turn";
        inGame = true;
        myCanvas.gameObject.SetActive(true);
        if (computer.myTurn == 0) StartCoroutine(PlaceChess(computer.NextStep(GetState())));
    }
    public void HideChessBoard()
    {
        myCanvas.gameObject.SetActive(false);
    }
    public void ShowChessBoard()
    {
        if (!inGame) return;
        myCanvas.gameObject.SetActive(true);
    }
    void DrawLine()
    {
        float interval = boardSize / (n - 1), anchor = - boardSize / 2;
        Vector2 lineScale = gridLinePrefab.transform.localScale;
        lineScale.x = boardSize;
        for (int i = 0; i < n; i++)
        {
            GameObject newLine = Instantiate(gridLinePrefab, myCanvas);
            newLine.transform.localScale = lineScale;
            newLine.transform.localPosition = new Vector2(0, anchor + interval * i);
        }
        for (int j = 0; j < n; j++)
        {
            GameObject newLine = Instantiate(gridLinePrefab, myCanvas);
            newLine.transform.localScale = lineScale;
            newLine.transform.rotation = Quaternion.Euler(0, 0, 90);
            newLine.transform.localPosition = new Vector2(anchor + interval * j, 0);
        }
    }
    
    void Spawn()
    {
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                GameObject clickArea = Instantiate(clickAreaPrefab, myCanvas);
                ClickArea script = clickArea.GetComponent<ClickArea>();
                clickArea.transform.localPosition = BoardPosition(new Vector2(i, j));
                script.chessBoard = this;
                script.pos = new Vector2(i, j);

                GameObject debugValue = Instantiate(debugValuePrefab, myCanvas);
                debugValues[i, j] = debugValue;
                debugValue.transform.localPosition = BoardPosition(new Vector2(i, j));
                debugValue.GetComponentInChildren<TMP_Text>().text = "";
            }
    }
    Vector2 BoardPosition(Vector2 pos)
    {
        Vector2 anchor = - Vector2.one * (boardSize / 2);
        float interval = boardSize / (n - 1);
        return anchor + new Vector2(pos.y, pos.x) * interval;
    }
    public void CreateGhost(Vector2 pos)
    {
        if (!inGame || calculating) return;
        if (hasChess[(int)pos.x, (int)pos.y]) return;
        GameObject ghost = Instantiate(stones[turn], myCanvas);
        ghost.transform.localPosition = BoardPosition(pos);
        ghost.GetComponentInChildren<TMP_Text>().enabled = false;
        SpriteRenderer sprite = ghost.GetComponent<SpriteRenderer>();
        Color newColor = sprite.color;
        newColor.a = 0.5f;
        sprite.color = newColor;
        chess[(int)pos.x, (int)pos.y] = ghost;
    }
    public void DeleteGhost(Vector2 pos)
    {
        if (!inGame || calculating) return;
        if (hasChess[(int)pos.x, (int)pos.y]) return;
        Destroy(chess[(int)pos.x, (int)pos.y]);
    }
    public IEnumerator PlaceChess(Vector2 pos)
    {
        if (!inGame || calculating) yield break;
        int x = (int)pos.x, y = (int)pos.y;
        if (hasChess[x, y]) yield break;
        hasChess[x, y] = true;
        color[x, y] = turn;
        GameObject newChess = Instantiate(stones[turn], myCanvas);
        newChess.transform.localPosition = BoardPosition(pos);
        if (chess[x, y] != null) Destroy(chess[x, y]);
        chess[x, y] = newChess;
        newChess.GetComponentInChildren<TMP_Text>().enabled = false;

        if (turn == computer.myTurn)
        {
            if (PosOnBoard(lastComputerPos[turn]))
            {
                int i = (int)lastComputerPos[turn].x, j = (int)lastComputerPos[turn].y;
                chess[i, j].GetComponentInChildren<TMP_Text>().enabled = false;
            }
            lastComputerPos[turn] = new Vector2(x, y);
            Debug.Log("turn : " + turn + "  current chess : " + chess[x, y]);
            chess[x, y].GetComponentInChildren<TMP_Text>().enabled = true;
        }
        
        if (CheckStatus())
        {
            GameOver(turn);
            yield break;
        }

        turn = (turn + 1) % 2;
        if (turn == computer.myTurn)
        {
            calculating = true;
            yield return null;
            status.text = "AI is thinking... Wait a minute...";
            yield return null;
            Vector2 computerChoice = computer.NextStep(GetState());
            calculating = false;
            status.text = "Your Turn";
            StartCoroutine(PlaceChess(computerChoice));
        }
    }
    bool PosOnBoard(Vector2 pos)
    {
        int x = (int)pos.x, y = (int)pos.y;
        if (x < 0 || x >= n) return false;
        if (y < 0 || y >= n) return false;
        return true;
    }
    bool CheckStatus()
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
                        if (!PosOnBoard(new Vector2(x, y)) || !hasChess[x, y])
                        {
                            flag = false;
                            break;
                        }
                        colorSum += color[x, y];
                    }
                    if (!flag) continue;
                    if (colorSum == 0 || colorSum == 5) return true;
                }
        return false;
    }
    void GameOver(int winner)
    {
        Debug.Log("winner : " + turn);
        if (turn == computer.myTurn)
        {
            status.text = "You Lose";
        }
        else 
        {
            status.text = "You Win";
        }
        inGame = false;
    }
    public State GetState()
    {
        State res = new State(n);
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                if (!hasChess[i, j]) res.board[i, j] = -1;
                else res.board[i, j] = color[i, j];
        return res;
    }
    public void SetText(int i, int j, string newText)
    {
        debugValues[i, j].GetComponentInChildren<TMP_Text>().text = newText;
    }
}
