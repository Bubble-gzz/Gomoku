using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI : MonoBehaviour
{
    const float inf = (float)1e7;
    [SerializeField]
    public int depthLimit;
    [SerializeField]
    public int computationLimit;
    int computation;
    [SerializeField]
    int distLimit;
    [SerializeField]
    int widthLimit;
    [SerializeField]
    float gapThreshold;
    [SerializeField]
    float criticalThreshold;
    [SerializeField]
    List<float> score_OOOOO, score_XOOOOX, score_XOOOXX, score_XOOOX, score_XOOX, score_XOX, score_OOXOO, score_OXOOO, score_XOOXOX;
    [SerializeField]
    List<float> score_XOOOOB, score_XOOOB, score_XOOB, score_XOB;
    [SerializeField]
    List<float> score_double_trap;
    class Pattern{
        public string name;
        public int dk, len;
        public int[] mask;
        public List<float> score;
        public Pattern(int len, int[] mask, List<float> score, int dk = 0, string name = "")
        {
            this.len = len; this.mask = mask; this.score = score; this.dk = dk;
            this.name = name;
        }
    }
    List<Pattern> patterns = new List<Pattern>();
    public int n, myTurn;
    enum Strategy{
        Random,
        Clever
    }
    [SerializeField]
    Strategy strategy;
    
    List<Vector2> offsets = new List<Vector2>(); 
    public ChessBoard chessBoard;
    void Awake()
    {
        offsets.Add(new Vector2(0, 1));
        offsets.Add(new Vector2(1, 0));
        offsets.Add(new Vector2(1, 1));
        offsets.Add(new Vector2(-1, 1));
        patterns.Add(new Pattern(5, new int[5]{0,0,0,0,0}, score_OOOOO, -1, "OOOOO"));

        patterns.Add(new Pattern(6, new int[6]{-1,0,0,0,0,-1}, score_XOOOOX, -1, "XOOOOX"));
        
        patterns.Add(new Pattern(6, new int[6]{-1,0,0,0,-1,-1}, score_XOOOXX, -1, "XOOOXX"));
        patterns.Add(new Pattern(6, new int[6]{-1,-1,0,0,0,-1}, score_XOOOXX, -2, "XOOOXX"));
        patterns.Add(new Pattern(5, new int[5]{-1,0,0,0,-1}, score_XOOOX, -1, "XOOOX"));
    
        patterns.Add(new Pattern(4, new int[4]{-1,0,0,-1}, score_XOOX, -1));
        patterns.Add(new Pattern(3, new int[3]{-1,0,-1}, score_XOX, -1));
        patterns.Add(new Pattern(5, new int[5]{0,0,-1,0,0}, score_OOXOO, 0));
        patterns.Add(new Pattern(5, new int[5]{0,-1,0,0,0}, score_OXOOO, 0, "OXOOO"));
        patterns.Add(new Pattern(5, new int[5]{0,0,0,-1,0}, score_OXOOO, 0, "OXOOO"));

        patterns.Add(new Pattern(6, new int[6]{-1,0,0,-1,0,-1}, score_XOOXOX, -1, "XOOXOX"));
        patterns.Add(new Pattern(6, new int[6]{-1,0,-1,0,0,-1}, score_XOOXOX, -1, "XOOXOX"));

        patterns.Add(new Pattern(6, new int[6]{-1,0,0,0,0,1}, score_XOOOOB, -1));
        patterns.Add(new Pattern(6, new int[6]{1,0,0,0,0,-1}, score_XOOOOB, -1));
        patterns.Add(new Pattern(5, new int[5]{-1,0,0,0,1}, score_XOOOB, -1));
        patterns.Add(new Pattern(5, new int[5]{1,0,0,0,-1}, score_XOOOB, -1));
        patterns.Add(new Pattern(4, new int[4]{-1,0,0,1}, score_XOOB, -1));
        patterns.Add(new Pattern(4, new int[4]{1,0,0,-1}, score_XOOB, -1));
        patterns.Add(new Pattern(3, new int[3]{-1,0,1}, score_XOB, -1));
        patterns.Add(new Pattern(3, new int[3]{1,0,-1}, score_XOB, -1));
        myTurn = 0;
    }
    class ChessPos{
        public int x, y, dist;
        public ChessPos(int x, int y, int dist)
        {
            this.x = x;
            this.y = y;
            this.dist = dist;
        }
    }
    class Choice{
        public int x, y;
        public float value;
        public Choice(int x, int y, float value)
        {
            this.x = x;
            this.y = y;
            this.value = value;
        }
    }
    int AscendChoice(Choice A, Choice B)
    {
        if (A.value < B.value - 0.001f) return -1;
        if (A.value > B.value + 0.001f) return 1;
        return 0;
    }
    int DescendChoice(Choice A, Choice B)
    {
        if (A.value < B.value - 0.001f) return 1;
        if (A.value > B.value + 0.001f) return -1;
        return 0;
    }
    int[] dx = new int[4]{1,0,-1,0};
    int[] dy = new int[4]{0,1,0,-1};
    Queue<ChessPos> que = new Queue<ChessPos>();
    bool[,] vis;
    void CalcDist(State state, ref int[,] dist)
    {
        vis = new bool[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                if (state.board[i, j] != -1)
                    que.Enqueue(new ChessPos(i, j, 0));
                vis[i, j] = false;
                dist[i, j] = 0;
            }
        while (que.Count > 0)
        {
            ChessPos curPos = que.Dequeue();
            vis[curPos.x, curPos.y] = true;
            dist[curPos.x, curPos.y] = curPos.dist;
            for (int k = 0; k < 4; k++)
            {
                ChessPos newPos = new ChessPos(curPos.x + dx[k], curPos.y + dy[k], curPos.dist + 1);
                if (!PosOnBoard(new Vector2(newPos.x, newPos.y))) continue;
                if (vis[newPos.x, newPos.y]) continue;
                vis[newPos.x, newPos.y] = true;
                que.Enqueue(newPos);
            }
        }
    }
    float Evaluate(State state, int turn, bool debug = false)
    {
        float totalScore = 0;
        int[] count_double_trap = new int[2]{0, 0};
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            if (state.board[i, j] != -1)                        //prune
                foreach (var offset in offsets)
                {
                    bool double_trap_flag = false;
                    foreach (var pattern in patterns)
                    {
                        int matchResult = MatchResult(state, turn, i, j, offset, pattern);
                        if (matchResult == -1) continue;
                        float score = pattern.score[matchResult];
                        if ((turn^matchResult) != myTurn) score *= -1;
                        totalScore += score;

                        if (pattern.name == "XOOOXX" || pattern.name == "XOOXOX")
                        {
                            if (!double_trap_flag) count_double_trap[turn^matchResult]++;
                            double_trap_flag = true;
                        }
                    }
                }

        float extraScore;
        for (int isEnemy = 0; isEnemy < 2; isEnemy++)
            if (count_double_trap[isEnemy] >= 2) {
                //if (debug) Debug.Log("Find double_XOOOXX");
                extraScore = score_double_trap[isEnemy];
                if ((turn^isEnemy) != myTurn) extraScore *= -1;
                totalScore += extraScore;
            }
        return totalScore;
    }
    bool PosOnBoard(Vector2 pos)
    {
        int x = (int)pos.x, y = (int)pos.y;
        if (x < 0 || x >= n) return false;
        if (y < 0 || y >= n) return false;
        return true;
    }
    bool Match(int mask, int f, bool onBoard)
    {
        if (mask == 1) {
            return !onBoard || (mask == f);
        }
        return onBoard && (mask == f);
    }
    int MatchResult(State state, int turn, int i, int j, Vector2 offset, Pattern pattern)
    {
        /*
            mask
            -1: blank
            0: friend
            1: enemy or outOfBoundary
        */
        bool flag;

        int dx = (int)offset.x, dy = (int)offset.y, dk = pattern.dk, len = pattern.len;
        int[] mask = pattern.mask;

        for (int isEnemy = 0; isEnemy < 2; isEnemy++)
        {
            flag = true;
            for (int k = 0; k < len; k++)
            {
                int x = i + (k + dk) * dx, y = j + (k + dk) * dy, f = -2;
                bool onBoard = PosOnBoard(new Vector2(x, y));
                if (onBoard) {
                    f = state.board[x, y];
                    if (f == 0 || f == 1) f ^= turn ^ isEnemy;
                }
                if (!Match(mask[k], f, onBoard)) {
                    flag = false; break;
                }
            }
            if (flag) {
                if (pattern.name == "XOOOOX") {
                    //Debug.Log("turn : " + turn + " Find pattern : " + pattern.name + 
                   //" matchResult :" + isEnemy + "pos:(" + i + ", " + j + ")");
                }
                return isEnemy;
            }
        }
        return -1;
    }

    public Vector2 NextStep(State state)
    {
        Vector2 choice = new Vector2(0, 0);
        if (strategy == Strategy.Random)
        {
            int i, j;
            while (true)
            {
                i = Random.Range(0, n);
                j = Random.Range(0, n);
                if (state.board[i, j] == -1) break;
            }
            return new Vector2(i, j);
        }

        List<Vector2> bestChoices = new List<Vector2>();
        computation = 0;
        float bestValue = -inf;
        int[,] dist = new int[n, n];
        CalcDist(state, ref dist);
        List<Choice> candidatePos = new List<Choice>();
        bool emptyBoard = true;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                chessBoard.SetText(i, j, "");
                if (state.board[i, j] != -1) emptyBoard = false;
                if (state.board[i, j] == -1 && dist[i, j] <= distLimit)
                {
                    State newState = state.PlaceChess(i, j, myTurn);
                    if (newState.GameOver()) return new Vector2(i, j);   //Checkmate
                    float value = Evaluate(newState, myTurn^1);
                    candidatePos.Add(new Choice(i, j, value));
                }
            }
        if (emptyBoard) return new Vector2(n / 2, n / 2);
        candidatePos.Sort(DescendChoice);
        if (candidatePos.Count > 1)
        for (int k = 0; k < candidatePos.Count; k++)
        {
            if (k >= widthLimit) break;
            if (k > 0 && Gap(candidatePos[k-1].value, candidatePos[k].value)) break;
            int i = candidatePos[k].x, j = candidatePos[k].y;
            State newState = state.PlaceChess(i, j, myTurn);
            float value = MiniMax(newState, 1, myTurn ^ 1, bestValue, inf);
            chessBoard.SetText(i, j, (Mathf.Sign(value) * Mathf.Log10(Mathf.Abs(value))).ToString("f2"));
            if (value > bestValue)
            {
                bestValue = value;
                bestChoices.Clear();
                bestChoices.Add(new Vector2(i, j));
            }
            else if (Mathf.Abs(value - bestValue) < 0.001f)
            {
                bestChoices.Add(new Vector2(i, j));
            }
        }
        choice = bestChoices[Random.Range(0, bestChoices.Count)];
        //Debug.Log("bestValue : " + bestValue);
        return choice;
    }
    bool Gap(float a, float b)
    {
        if (Mathf.Abs(a) < criticalThreshold && Mathf.Abs(b) < criticalThreshold) return false;
        if (a < 0 && b > 0) return true;
        if (a > 0 && b < 0) return true;
        float lv_a = Mathf.Log10(Mathf.Abs(a)), lv_b = Mathf.Log10(Mathf.Abs(b));
        if (Mathf.Abs(lv_a - lv_b) > gapThreshold) return true;
        return false;
    }
    float MiniMax(State state, int depth, int turn, float alpha, float beta)
    {
        computation++;
        //Debug.Log("computation : " + computation);
        if (state.GameOver())
        {
           // Debug.Log("gameover depth:" + depth);
            if (turn != myTurn) return inf - depth;
            else return - inf + depth;
        }
        if (depth == depthLimit || computation >= computationLimit)
            return Evaluate(state, turn);
        float bestValue;
        int[,] dist = new int[n, n];
        CalcDist(state, ref dist);
        
        List<Choice> candidatePos = new List<Choice>();

        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            if (state.board[i, j] == -1 && dist[i, j] <= distLimit)
            {
                State newState = state.PlaceChess(i, j, turn);
                if (newState.GameOver())
                {
                    if (turn == myTurn) return inf - depth;
                    else return - inf + depth;
                }
                float value = Evaluate(newState, turn^1);
                candidatePos.Add(new Choice(i, j, value));
            }

        if (turn == myTurn)
        {
            bestValue = -inf;
            candidatePos.Sort(DescendChoice);
            for (int k = 0; k < candidatePos.Count; k++)
            {
                if (k >= widthLimit) break;
                if (k > 0 && Gap(candidatePos[k-1].value, candidatePos[k].value)) break; //prune
                int i = candidatePos[k].x, j = candidatePos[k].y;

                State newState = state.PlaceChess(i, j, turn);
                
                int newDepth;
                if (candidatePos.Count <= 2) newDepth = depth;
                else newDepth = depth + 1;

                float value = MiniMax(newState, newDepth, turn ^ 1, alpha, beta);
                bestValue = Mathf.Max(bestValue, value);
                alpha = Mathf.Max(alpha, value);
                if (beta <= alpha || computation >= computationLimit) break;
            }
        }
        else
        {
            bestValue = inf;
            candidatePos.Sort(AscendChoice);
            for (int k = 0; k < candidatePos.Count; k++)
            {
                if (k >= widthLimit) break;
                if (k > 0 && Gap(candidatePos[k-1].value, candidatePos[k].value)) break; //prune
                int i = candidatePos[k].x, j = candidatePos[k].y;

                State newState = state.PlaceChess(i, j, turn);
                //if (newState.CheckStatus())
                //{
                //    Debug.Log("turn : "+ turn + "  GameOver at pos(" + i + ", " + j + ")");
                //}
                //else Debug.Log("turn : "+ turn + "  try pos(" + i + ", " + j + ")");

                int newDepth;
                if (candidatePos.Count <= 2) newDepth = depth;
                else newDepth = depth + 1;

                float value = MiniMax(newState, newDepth, turn ^ 1, alpha, beta);
                bestValue = Mathf.Min(bestValue, value);
                beta = Mathf.Min(beta, value);
                if (beta <= alpha || computation >= computationLimit) break;
            }
        }
        return bestValue;
    }
}
