using My2048;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum Direction
{
    TOP_TO_BOTTOM, LEFT_TO_RIGHT
}

public enum Process
{
    GETTING_INPUT,
    LERPING_TILES,
    UNDOING
}
[System.Serializable]
public struct SwipeData
{
    public Direction direction;
    public bool invert;
}

[System.Serializable]
public struct tileData
{
    public uint value;
    public Index index;
    public Index oldIndex;
    public Index otherTileIndex;
    public bool merged;
    public bool spawned;

    public tileData(uint Value, Index index, Index oldIndex, Index otherIndex, bool Merged, bool Spawned)
    {
        value = Value;
        this.index = index;
        this.oldIndex = oldIndex;
        otherTileIndex = otherIndex;
        merged = Merged;
        spawned = Spawned;
    }
}



public class TwentyFortyEight : MonoBehaviour
{
    public int TouchDeadZone = 60;
    public float TouchMaxHeightPercent = 90;
    public int winningNumber = 2048;
    public Sprite[] spriteTiles;
    public Text scoreText;
    public Text bestText;
    public Button undoButton;
    public GameData gameData;
    //uint score = 0;
    //uint previousScore = 0;
    uint bestScore = 0;
    uint deltaScore = 0;
    LerpData<uint> scoreLerp;
    Vector2 startTouch = Vector2.zero;
    Vector2 swipeDelta = Vector2.zero;
    bool undoing = false;
    bool moving = false;
    //bool canUndo = false;
    public GameBoard board;
    MoveData move;
    Stack<Tile> stack;
    Process state = Process.GETTING_INPUT;
    //SwipeData previousSwipe;
    SwipeData currentSwipe;
    SaveLoad saveLoad;

    //static System.DateTime currentTime = new System.DateTime();
    //static int minutesClock = System.DateTime.Now.Minute;
    private void Awake()
    {
        //spriteTiles = Resources.LoadAll<Sprite>("");
        saveLoad = GetComponent<SaveLoad>();
    }
    private void Start()
    {
        //savePath = Application.persistentDataPath + "/save.dat";
        //data = new GameData();
        gameData = saveLoad.Load(board, false);
        stack = new Stack<Tile>(board.size);
        bestScore = (uint)PlayerPrefs.GetInt("best");
        bestText.text = bestScore.ToString();
        scoreText.text = gameData.score.ToString();
        undoButton.interactable = gameData.canUndo;

        if (gameData.activeTileData == null)
        {
            // there was no save file...

            GameBoard.create(board, null, false);

            board.spawnRandomTile(false);
            board.spawnRandomTile(false);

        }

        //Debug.Log(System.DateTime.Now.ToString("HH:mm"));
    }

    private void Update()
    {
        switch (state)
        {
            case Process.GETTING_INPUT:
                getInput();
                break;
            case Process.LERPING_TILES:
                lerpTiles();
                break;
            case Process.UNDOING:
                break;
            default:
                break;
        }
        // System.DateTime currentTile = new System.DateTime();
        //if (System.DateTime.Now.Minute != minutesClock)
        //{
        //    minutesClock = System.DateTime.Now.Minute;
        //    Debug.Log(System.DateTime.Now.ToString("HH:mm"));
        //}
    }

    public Sprite valueToTile(uint value)
    {
        if (spriteTiles == null)
            return null;
        if (spriteTiles.Length < 1)
            return null;
        int i = 0;
        while (value > 1)
        {
            value /= 2;
            i++;
        }
        return spriteTiles[i];
    }

    void initializeLerp()
    {
        foreach (var t in board.tiles)
        {
            if (!t)
                continue;
            t.lerpData = new LerpData<Vector2>()
            {
                timeStarted = Time.time,
                start = t.transform.position,
                end = t.nextPosition,
                lerpDuration = 0.25f,
                t = 0
            };
        }

        foreach (var t in board.removedTiles.list)
        { // i need this because i remove the tiles and put them into another list...
            if (!t)
                continue;
            t.lerpData = new LerpData<Vector2>()
            {
                timeStarted = Time.time,
                start = t.transform.position,
                end = t.nextPosition,
                lerpDuration = 0.25f,
                t = 0
            };
        }
    }

    void lerpTiles()
    {
        bool lerping = false;
        foreach (var t in board.tiles)
        {
            if (!t)
                continue;
            if (t.lerpData.t < 1)
            {
                t.lerp();
                lerping = true;
            }
        }
        foreach (var t in board.removedTiles.list)
        { // i need this because i remove the tiles and put them into another list...
            if (!t)
                continue;
            if (t.lerpData.t < 1)
            {
                t.lerp();
                lerping = true;
            }
        }
        if (!lerping)
        {
            foreach (var t in board.tiles)
            {
                if (!t)
                    continue;
                t.transform.position = t.nextPosition;
                if (t.currentMove.merged)
                {
                    t.animator.SetTrigger("merge");
                }
                t.setSprite();
            }
            foreach (var t in board.removedTiles.list)
            { // i need this because i remove the tiles and put them into another list...
                if (!t)
                    continue;
                t.transform.position = t.nextPosition;
                t.gameObject.SetActive(false);
                t.setSprite();
            }
            onTilesFinishedMoving();
            state = Process.GETTING_INPUT;
        }
    }

    Touch touch;
    void getInput()
    {
        if (moving || undoing)
            return;
        #region Swipe Input
        if (Input.GetMouseButtonDown(0))
        {
            //UnityEngine.Debug.Log("tap");
            // tap = true;
            if (isTouchOverUIButton())
            {
                Debug.Log("is over button!");
                return;
            }
            else
            {
                Debug.Log("is not over button!");
                //Debug.LogFormat("Starttouch: {0}, Pointerpos: {1}", startTouch, Input.mousePosition);
            }
            startTouch = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            swipeDelta = (Vector2)Input.mousePosition - startTouch;
            TrySwipe();
        }

        if (Input.touchCount != 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                //tap = true;
                if (isTouchOverUIButton())
                {
                    return;
                }
                startTouch = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                swipeDelta = Vector2.zero;
                if (startTouch != Vector2.zero)
                {
                    swipeDelta = touch.position - startTouch;
                }
                TrySwipe();
                startTouch = swipeDelta = Vector2.zero;
            }
        }
        #endregion

#if UNITY_EDITOR
        if (Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow))
        {
            OnSwipeUp();
        }

        if (Input.GetKeyUp(KeyCode.S) || Input.GetKeyUp(KeyCode.DownArrow))
        {
            OnSwipeDown();
        }

        if (Input.GetKeyUp(KeyCode.D) || Input.GetKeyUp(KeyCode.RightArrow))
        {
            OnSwipeRight();
        }

        if (Input.GetKeyUp(KeyCode.A) || Input.GetKeyUp(KeyCode.LeftArrow))
        {
            OnSwipeLeft();
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            SceneManager.LoadScene(0);
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            board.spawnRandomTile(false);
        }

        if (Input.GetKeyUp(KeyCode.Z))
        {
            if (board.spawnedTile)
                board.spawnedTile.shrink();
        }

#endif
    }

    public void TrySwipe()
    {
        if (startTouch == Vector2.zero)
            return;

        if (swipeDelta.magnitude > TouchDeadZone && (startTouch.y / Screen.height) < TouchMaxHeightPercent)
        {
            if (isTouchOverUIButton())
            {
                return;
            }
            float x = swipeDelta.x;
            float y = swipeDelta.y;
            if (Mathf.Abs(x) > Mathf.Abs(y))
            {
                // l or r
                if (x < 0)
                {
                    OnSwipeLeft();
                }
                else
                {
                    OnSwipeRight();
                }
            }
            else if (y > 0)
            {
                // u or d
                OnSwipeUp();
            }
            else
            {
                OnSwipeDown();
            }
            startTouch = swipeDelta = Vector2.zero;
        }
    }

    List<RaycastResult> results = new List<RaycastResult>(2);
    public bool isTouchOverUIButton()
    {
        results.Clear();
        PointerEventData p = new PointerEventData(EventSystem.current);
        p.position = Input.mousePosition;
        EventSystem.current.RaycastAll(p, results);
        return results.Count > 0;
#if !UNITY_EDITOR && !UNITY_STANDALONE
        return EventSystem.current.IsPointerOverGameObject(touch.fingerId);
#else
        return EventSystem.current.IsPointerOverGameObject();
#endif

    }

    public void onSpawnedTileShrunk()
    {
        board[board.spawnedTile.index.x, board.spawnedTile.index.y] = null;
        board.AddTileToPool(board.spawnedTile);
        board.spawnedTile = null;
        undo();
    }

    void undo()
    {
        if (undoing) return;
        if (!gameData.canUndo) return;
        undoing = true;
        gameData.canUndo = false;
        //score = previousScore;
        //score -= deltaScore;
        MoveData move = new MoveData();
        switch (gameData.previousSwipe.direction)
        {
            case Direction.TOP_TO_BOTTOM:
                move = gameData.previousSwipe.invert ? board.upData : board.downData;
                break;
            case Direction.LEFT_TO_RIGHT:
                move = gameData.previousSwipe.invert ? board.leftData : board.rightData;
                break;
        }
        for (int i = 0; i < board.size; i++)
        {
            int x = move.startX + i * move.xRowShift;
            int y = move.startY + i * move.yRowShift;
            for (int j = 0; j < board.size; j++)
            {
                Tile t = board[x, y];
                if (t)
                {
                    t.undo();
                    board[x, y] = null;
                    t.index = t.currentMove.index;
                    board[t.currentMove.index.x, t.currentMove.index.y] = t;
                    t.nextPosition = board.getWorldPos(t.currentMove.index.x, t.currentMove.index.y);
                    if (!t.currentMove.merged)
                    {
                        x += move.xDir;
                        y += move.yDir;
                        continue;
                    }
                    Tile r = board.removedTiles[t.otherTileIndex.x, t.otherTileIndex.y];
                    board[r.currentMove.index.x, r.currentMove.index.y] = r;
                    r.nextPosition = board.getWorldPos(r.currentMove.index.x, r.currentMove.index.y);
                    board.removedTiles[r.currentMove.index.x, r.currentMove.index.y] = null;
                    r.undo();
                    t.clean();
                    r.clean();
                    r.index = r.currentMove.index;
                }
                x += move.xDir;
                y += move.yDir;
            }
        }
        initializeLerp();
        state = Process.LERPING_TILES;
    }

    public void onTilesFinishedMoving()
    {
        if (undoing)
        {
            undoing = false;
            gameData.score = gameData.previousScore;
            //score -= deltaScore;
            //deltaScore = 0;
        }
        else if (!undoing)
        {
            board.spawnedTile = board.spawnRandomTile(true);
            gameData.score += deltaScore;
            deltaScore = 0;
            if (gameData.score > bestScore)
            {
                bestScore = gameData.score;
            }
            checkGameStatus();
        }
        moving = false;
        if (!undoing)
        {
            scoreText.text = gameData.score.ToString();
            bestText.text = bestScore.ToString();
        }
        else
        {
            scoreText.text = gameData.previousScore.ToString();
        }
        undoButton.interactable = gameData.canUndo;
        //saveGame();
        SaveLoad.Save(this);
    }

    void checkGameStatus()
    {
        saveScores();
        bool isBoardFull = true;
        foreach (var tile in board.tiles)
        {
            if (!tile)
            {
                isBoardFull = false;
                continue;
            }
            if (tile.value >= winningNumber)
            {
                onGameWon();
                return;
            }
        }

        if (isBoardFull)
        {
            if (!canAMoveBeMade())
                onGameOver();
        }
    }

    void saveScores()
    {
        PlayerPrefs.SetInt("best", (int)bestScore);
    }

    void onGameWon()
    {
    }

    void onGameOver()
    {
    }

    void moveTiles()
    {
        if (moving)
            return;
        if (!canAMoveBeMade())
        {
            // UnityEngine.Debug.Log("a move cannot be made");
            return;
        }
        gameData.previousSwipe = currentSwipe;
        gameData.previousScore = gameData.score;
        moving = true;
        gameData.canUndo = true;
        board.ClearRemovedTiles(); // removing any tiles that were not restored. they should be moved to a pool of fresh tiles to be used
        for (int i = 0; i < board.size; i++)
        {
            int x = move.startX + i * move.xRowShift;
            int y = move.startY + i * move.yRowShift;
            for (int j = 0; j < board.size; j++)
            {
                Tile t = board[x, y];
                if (t)
                {
                    stack.Push(t);
                    t.clean();
                    board[x, y] = null;
                }
                x += move.xDir;
                y += move.yDir;
            }

            int tileCount = stack.Count;
            x = move.endX + i * move.xRowShift;
            y = move.endY + i * move.yRowShift;
            Tile prevTile = null;
            for (int j = 0; j < tileCount; j++)
            {
                Tile tile = stack.Pop();
                if (prevTile)
                {
                    if (prevTile.value == tile.value && !prevTile.currentMove.merged && !prevTile.currentMove.removed && !tile.currentMove.removed && !tile.currentMove.merged)
                    {
                        prevTile = prevTile + tile;
                        deltaScore += prevTile.value;

                        tile.currentMove.index = tile.index;
                        board[tile.currentMove.index.x, tile.currentMove.index.y] = null; // should already be null
                        board.removedTiles[tile.currentMove.index.x, tile.currentMove.index.y] = tile;
                        Tile.DebugSetGameObjectName(prevTile);
                        tile.nextPosition = prevTile.nextPosition;
                        continue;
                    }
                }
                tile.currentMove.index = tile.index;
                tile.index = new Index(x, y);
                board[x, y] = tile;
                tile.nextPosition = board.getWorldPos(x, y);
                tile.memory.Add(tile.currentMove);
                Tile.DebugSetGameObjectName(tile);
                prevTile = tile;
                x -= move.xDir; // flipping add to minus to go back....
                y -= move.yDir;
            }
        }
        initializeLerp();
        state = Process.LERPING_TILES;
    }

    private bool canAMoveBeMade()
    {
        int count = board.tiles.Count;
        for (int i = 0; i < count; i++)
        {
            Tile t = board[i];
            if (canMoveOrMergeTile(t))
            {
                return true;
            }
        }
        return false;
    }

    bool canMoveOrMergeTile(Tile tile)
    {
        if (!tile)
        {
            return false;
        }

        if (tile.currentMove.removed)
        {
            return false;
        }

        Tile query = board.getNextTile(tile, currentSwipe);
        if (query)
        {
            if (tile.value == query.value) return true;
        }
        Index i = board.getNextEmptyIndex(tile, currentSwipe);
        if (i.x != -1 && i.y != -1)
        {
            return true;
        }
        return false;
    }

    public void OnSwipeUp()
    {
        currentSwipe.direction = Direction.TOP_TO_BOTTOM;
        currentSwipe.invert = true;
        move = board.upData;
        moveTiles();
    }

    public void OnSwipeDown()
    {
        currentSwipe.direction = Direction.TOP_TO_BOTTOM;
        currentSwipe.invert = false;
        move = board.downData;
        moveTiles();
    }

    public void OnSwipeLeft()
    {
        currentSwipe.direction = Direction.LEFT_TO_RIGHT;
        currentSwipe.invert = false;
        move = board.rightData;
        moveTiles();
    }

    public void OnSwipeRight()
    {
        currentSwipe.direction = Direction.LEFT_TO_RIGHT;
        currentSwipe.invert = true;
        move = board.leftData;
        moveTiles();
    }

    public void newGame()
    {
        //SceneManager.LoadScene(0);
        gameData.score = 0;
        gameData.previousScore = 0;
        gameData.canUndo = false;
        moving = false;
        undoing = false;
        for (int i = 0; i < board.length; i++)
        {
            Tile t = board[i];
            if (!t)
                continue;
            t.reset();
            t.gameObject.SetActive(false);
            board[i] = null;

            t = board.removedTiles.list[i];
            if (!t)
                continue;
            t.reset();
            t.gameObject.SetActive(false);
            board.removedTiles.list[i] = null;
        }
        saveLoad.initGame(board, true);
    }

    public void Undo()
    {
        if (undoing) return;
        if (board.spawnedTile)
            board.spawnedTile.shrink();
    }
}
