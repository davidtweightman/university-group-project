using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    //Board Vars
    private const int boardWidth = 8;
    private const int boardHeight = 8;
    private BoardSpace[,] board = new BoardSpace[boardHeight, boardWidth];

    //Offsets in format of; Left, UpLeft, Up, UpRight, Right, DownRight, Down, DownLeft
    Vector2[] offsets = { new Vector2(-1, 0), new Vector2(-1, 1), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0), new Vector2(1, -1), new Vector2(0, -1), new Vector2(-1, -1) };

    //Player Vars
    int aiDifficulty;
    bool playerTurn = true; // Player is playerstate 1 which means they are black counters
    bool gameOver = false;
    bool lastPlayerUnableToMove = false;
    bool highlightsActive = true;

    //Prefabs
    [SerializeField] private GameObject piecePrefab;
    [SerializeField] private Sprite whitePiece;
    [SerializeField] private Sprite blackPiece;
    [SerializeField] private GameObject highlightPrefab;

    //Moves Intelligence
    List<PossibleMove> possibleMoves = new List<PossibleMove>();
    List<GameObject> highlights = new List<GameObject>();

    //Scripts References
    UIManager uiManager;

    private void Awake()
    {
        uiManager = GameObject.Find("UIManager").GetComponent<UIManager>();
    }

    // Start is called before the first frame update
    private void Start()
    {
        aiDifficulty = Persistance.aiDifficulty;

        ResetBoard();
        CheckPossibleMoves();
        DisplayPossibleMoves();
    }

    // Update is called once per frame
    private void Update()
    {
        //Player left clicks
        if (Input.GetMouseButtonDown(0) && !gameOver)
        {
            //If it is the players turn
            if ((aiDifficulty == 0 && (playerTurn || !playerTurn)) || (aiDifficulty != 0 && playerTurn))
            {
                //Find which square they clicked in
                Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                mousePosition = new Vector3(Mathf.Round(mousePosition.x), Mathf.Round(mousePosition.y), 0);

                //If they clicked within the board
                if (mousePosition.x > -1 && mousePosition.x < boardWidth && mousePosition.y > -1 && mousePosition.y < boardHeight)
                {
                    PossibleMove moveToMake = new PossibleMove();

                    //Check if the position is within the ruleset -- Is it next to a piece and does it flip other pieces
                    if (WithinPossibleMoves((int)mousePosition.x, (int)mousePosition.y, ref moveToMake))
                    {
                        //Now there is confirmation, make the change
                        PlacePiece((int)mousePosition.x, (int)mousePosition.y, (playerTurn) ? 1 : 2, moveToMake);

                        //Change Turn
                        MoveCompleted();
                    }
                }
            }
        }
    }



    private void DisplayPossibleMoves()
    {
        if (highlightsActive)
        {
            //Remove Old Hightlights
            for (int i = 0; i < highlights.Count; i++)
            {
                Destroy(highlights[i]);
            }
            highlights.Clear();

            //Add New Highlights
            for (int i = 0; i < possibleMoves.Count; i++)
            {
                GameObject highlight = Instantiate(highlightPrefab, new Vector3(possibleMoves[i].x, possibleMoves[i].y, 0), Quaternion.identity);
                highlights.Add(highlight);
            }
        }
        else
        {
            if (highlights.Count > 0)
            {
                //Remove Old Hightlights
                for (int i = 0; i < highlights.Count; i++)
                {
                    Destroy(highlights[i]);
                }
                highlights.Clear();
            }
        }
    }

    private bool WithinPossibleMoves(int x, int y, ref PossibleMove pm)
    {
        for (int i = 0; i < possibleMoves.Count; i++)
        {
            if (possibleMoves[i].x == x && possibleMoves[i].y == y)
            {
                pm = possibleMoves[i];
                return true;
            }
        }

        return false;
    }

    private void CheckPossibleMoves()
    {
        //Loop through entire board
        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                //Find a position where there isnt a counter
                if (board[y, x].state == 0)
                {
                    //Check if there is a counter within the 3x3 area
                    if (Check3X3(x, y))
                    {
                        //Check if putting a counter in the position will flip pieces
                        CheckFlipsPieces(x, y);
                    }
                }
            }
        }
    }

    private bool Check3X3(int x, int y)
    {
        //Check the 3x3 grid, with x and y being the centre
        for (int i = 0; i < offsets.Length; i++)
        {
            int checkX = x + (int)offsets[i].x;
            int checkY = y + (int)offsets[i].y;

            //Check the position is valid on the board
            if (checkX > 0 && checkX < boardWidth - 1 && checkY > 0 && checkY < boardHeight - 1)
            {
                //Check if the state is not empty
                if (board[checkY, checkX].state != 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void CheckFlipsPieces(int x, int y)
    {
        //Set which piece we're checking for depending on whose turn it is
        int pieceState = (playerTurn) ? 1 : 2;

        //All the positions that would be flipped if this piece was placed
        List<Vector2> totalPositions = new List<Vector2>();
        List<Vector2> checkedPositions = new List<Vector2>();

        //Loop through all the offsets to check all 8 directions
        for (int i = 0; i < offsets.Length; i++)
        {
            //Reset the not at end of search bool for all directions
            bool notAtEnd = true;

            //Reset the positions being checked list
            checkedPositions.Clear();

            //Reset the position to check
            int checkY = y;
            int checkX = x;

            //Loop through all positions of the direction
            while (notAtEnd)
            {
                //Modify the position by the offset
                checkY += (int)offsets[i].y;
                checkX += (int)offsets[i].x;

                //Check if the position has reached the end of the board
                if (checkY < 0 || checkY > boardHeight - 1 || checkX < 0 || checkX > boardWidth - 1)
                {
                    notAtEnd = false;
                }
                else
                {
                    //Check if the position has reached a piece of the same colour
                    if (board[checkY, checkX].state == pieceState)
                    {
                        //If it is not directly next to the piece placed
                        if (checkedPositions.Count > 0)
                        {
                            //Add all the positions that have been checked along the way to the total list to be flipped
                            for (int j = 0; j < checkedPositions.Count; j++)
                            {
                                totalPositions.Add(checkedPositions[j]);
                            }
                        }
                        notAtEnd = false;
                    }
                    //Check if the position has reached an empty space
                    else if (board[checkY, checkX].state == 0)
                    {
                        notAtEnd = false;
                    }
                }

                //Add the current position that has been determined to still be on the board and not the same piece and not a blank to the list
                checkedPositions.Add(new Vector2(checkX, checkY));
            }
        }

        if (totalPositions.Count > 0)
        {
            PossibleMove move = new PossibleMove(x, y, totalPositions.Count, totalPositions.ToArray());
            possibleMoves.Add(move);
        }
    }

    private void PlacePiece(int x, int y, int playerState, PossibleMove pm)
    {
        SetPositionToPieceColour(x, y, playerState);

        for (int i = 0; i < pm.positionsFlipped.Length; i++)
        {
            SetPositionToPieceColour((int)pm.positionsFlipped[i].x, (int)pm.positionsFlipped[i].y, playerState);
        }
    }

    private void MoveCompleted()
    {
        //Calculate and change the scores
        int w = 0, b = 0;
        CountPieces(ref w, ref b);
        uiManager.SetScores(w, b);

        //If not finished, then change the players turn
        playerTurn = !playerTurn;
        uiManager.SetPlayerTurn(playerTurn);

        //Setup moves for next turn
        possibleMoves.Clear();
        CheckPossibleMoves();
        DisplayPossibleMoves();






        //Now that moves have been calculated, check if the player can make one
        if (possibleMoves.Count == 0)
        {
            if (lastPlayerUnableToMove) // If not check if the previous turn the player was able to make one
            {
                gameOver = true; // If not then the game has reached a standstill and is over
                uiManager.ErrorBoxShow(1f, "Game Over!");
                string text = (b > w) ? "Black Wins!" : "White Wins!";
                uiManager.DisplayWinner(text);
            }
            else
            {
                lastPlayerUnableToMove = true; // If the player was able to move next turn, skip this players turn and set bool to true
                uiManager.ErrorBoxShow(1f, (playerTurn) ? "Black Has No Moves. Turn Skipped." : "White Has No Moves. Turn Skipped.");
                MoveCompleted();
            }
        }




        else
        {
            if (aiDifficulty != 0 && !playerTurn)
            {
                switch (aiDifficulty)
                {
                    case 1:
                        Invoke("AILevel1Move", 1f);
                        break;
                    case 2:
                        Invoke("AILevel2Move", 1f);
                        break;
                    case 3:
                        Invoke("AILevel3Move", 1f);
                        break;
                }
            }
        }
    }

    private void ResetBoard()
    {
        //Set Whole Board to blank
        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                board[y, x] = new BoardSpace(Instantiate(piecePrefab, new Vector3(x, y, 0), Quaternion.identity), 0);
            }
        }

        //Assign middle 4 pieces
        SetPositionToPieceColour(3, 3, 2);
        SetPositionToPieceColour(4, 3, 1);
        SetPositionToPieceColour(3, 4, 1);
        SetPositionToPieceColour(4, 4, 2);
    }

    private void SetPositionToPieceColour(int x, int y, int state)
    {
        //1 = Black Piece, 2 = White Piece, 0 = Empty
        if (state == 1)
        {
            board[y, x].spriteRenderer.sprite = blackPiece;
        }
        else if (state == 2)
        {
            board[y, x].spriteRenderer.sprite = whitePiece;
        }
        else
        {
            board[y, x].spriteRenderer.sprite = null;
        }

        board[y, x].state = state;
    }
    
    private void CountPieces(ref int w, ref int b)
    {
        int whites = 0;
        int blacks = 0;

        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                if (board[y, x].state == 1)
                {
                    blacks++;
                }
                else if (board[y, x].state == 2)
                {
                    whites++;
                }
            }
        }

        w = whites;
        b = blacks;
    }

    private void AILevel1Move() //Temporary
    {
        int randomPos = Random.Range(0, possibleMoves.Count);
        PlacePiece(possibleMoves[randomPos].x, possibleMoves[randomPos].y, 2, possibleMoves[randomPos]);

        MoveCompleted();
    }

    private void AILevel2Move() //Temporary
    {
        int posOfMostValue = 0;
        int currentMostValue = 0;

        for (int i = 0; i < possibleMoves.Count; i++)
        {
            if (possibleMoves[i].value > currentMostValue)
            {
                currentMostValue = possibleMoves[i].value;
                posOfMostValue = i;
            }
        }

        PlacePiece(possibleMoves[posOfMostValue].x, possibleMoves[posOfMostValue].y, 2, possibleMoves[posOfMostValue]);

        MoveCompleted();
    }

    private void AILevel3Move()
    {
        int ai1_posOfMostValue = 0; //hold the position within the possible moves that flips the most peices
        int ai1_currentMostValue = 0; //holds the score of how many are flipped for the position of most value
        int whites = 0; //how many white peices there are
        int blacks = 0; //how many black peices there are

        BoardSpace[,] board2 = new BoardSpace[boardHeight, boardWidth]; //remembers the state of the board before any peices are placed

        CountPieces(ref whites, ref blacks);//counts how many peices there are in total

        //saves the board state to board 2
        for (int y = 0; y < boardHeight; y++)
        {
            for (int x = 0; x < boardWidth; x++)
            {
                board2[y, x].state = board[y, x].state;
            }
        }
        
        //1st itteration: goes through the AI's first possible moves
        for (int i = 0; i < possibleMoves.Count; i++)
        {
            int score = 0; //holds the score 
            int plr1_posOfMostValue = 0; //hold the position within the possible moves that flips the most peices
            int plr1_currentMostValue = 0; //holds the score of how many are flipped for the position of most value

            score = possibleMoves[i].value;

            PlacePiece(possibleMoves[i].x, possibleMoves[i].y, 2, possibleMoves[i]);
            playerTurn = !playerTurn;
            possibleMoves.Clear();
            CheckPossibleMoves();

            //2nd itteration: goes through the players first possible moves
            if (whites + blacks < 64 && possibleMoves.Count != 0)
            {

                for (int j = 0; j < possibleMoves.Count; j++)
                {
                    if (possibleMoves[j].value > plr1_currentMostValue)
                    {
                        plr1_currentMostValue = possibleMoves[j].value;
                        plr1_posOfMostValue = j;
                    }
                }
                PlacePiece(possibleMoves[plr1_posOfMostValue].x, possibleMoves[plr1_posOfMostValue].y, 1, possibleMoves[plr1_posOfMostValue]);
                playerTurn = !playerTurn;
                possibleMoves.Clear();
                CheckPossibleMoves();

            }

            //3rd itteration: goes through the AI's second possible moves
            if (whites + blacks < 63 && possibleMoves.Count != 0)
            {
                int ai2_posOfMostValue = 0;
                int ai2_currentMostValue = 0;
                

                BoardSpace[,] board3 = new BoardSpace[boardHeight, boardWidth];

                CountPieces(ref whites, ref blacks);

                //saves the board state to board3
                for (int y = 0; y < boardHeight; y++)
                {
                    for (int x = 0; x < boardWidth; x++)
                    {
                        board3[y, x].state = board[y, x].state;
                    }
                } 

                
                for (int j = 0; j < possibleMoves.Count; j++)
                {
                    int score2 = 0;
                    int plr2_posOfMostValue = 0;
                    int plr2_currentMostValue = 0;
                    int ai3_posOfMostValue = 0;
                    int ai3_currentMostValue = 0;
                    score2 = possibleMoves[j].value;

                    PlacePiece(possibleMoves[j].x, possibleMoves[j].y, 2, possibleMoves[j]);
                    playerTurn = !playerTurn;
                    possibleMoves.Clear();
                    CheckPossibleMoves();

                    //4th itteration: goes through the players second possible moves
                    if (whites + blacks < 62 && possibleMoves.Count != 0)
                    {
                        for (int l = 0; l < possibleMoves.Count; l++)
                        {
                            if (possibleMoves[l].value > plr2_currentMostValue)
                            {
                                plr2_currentMostValue = possibleMoves[l].value;
                                plr2_posOfMostValue = l;
                            }
                        }
                        PlacePiece(possibleMoves[plr2_posOfMostValue].x, possibleMoves[plr2_posOfMostValue].y, 1, possibleMoves[plr2_posOfMostValue]);
                        playerTurn = !playerTurn;
                        possibleMoves.Clear();
                        CheckPossibleMoves();

                        //5rd itteration: goes through the AI's third possible moves
                        for (int l = 0; l < possibleMoves.Count; l++)
                        {
                            if (possibleMoves[l].value > ai3_currentMostValue)
                            {
                                ai3_currentMostValue = possibleMoves[l].value;
                                ai3_posOfMostValue = l;
                            }
                        }
                        score2 += ai3_currentMostValue;

                    }
                    
                    //resets the board to the second board state
                    for (int y = 0; y < boardHeight; y++)
                    {
                        for (int x = 0; x < boardWidth; x++)
                        {
                            board[y, x].state = board3[y, x].state;
                            SetPositionToPieceColour(x, y, board3[y, x].state);
                        }
                    }
                    possibleMoves.Clear();
                    CheckPossibleMoves();
                    DisplayPossibleMoves();

                    //checks if the new path is better than the previous path
                    if (score2 > ai2_currentMostValue)
                    {
                        ai2_currentMostValue = score;
                        ai2_posOfMostValue = i;
                    }


                }
                score += ai2_currentMostValue;

            }





            //re-sets board to board2
            for (int y = 0; y < boardHeight; y++)
            {
                for (int x = 0; x < boardWidth; x++)
                {
                    board[y, x].state = board2[y, x].state;
                    SetPositionToPieceColour(x, y, board2[y, x].state);
                }
            }
            possibleMoves.Clear();
            CheckPossibleMoves();
            DisplayPossibleMoves();

            //checks if the new path is better than the previous path
            if (score > ai1_currentMostValue)
            {
                ai1_currentMostValue = score;
                ai1_posOfMostValue = i;
            }
        }
        //places the peice at the optimum position
        if (possibleMoves.Count != 0)
        PlacePiece(possibleMoves[ai1_posOfMostValue].x, possibleMoves[ai1_posOfMostValue].y, 2, possibleMoves[ai1_posOfMostValue]);

        playerTurn = false;
        
        MoveCompleted();
    }

    public void ToggleHighlights()
    {
        highlightsActive = !highlightsActive;
        DisplayPossibleMoves();
    }
}

struct BoardSpace
{
    public GameObject pieceAtLocation;
    public SpriteRenderer spriteRenderer;
    public int state;

    public BoardSpace(GameObject inputObj, int inputState)
    {
        pieceAtLocation = inputObj;
        spriteRenderer = pieceAtLocation.GetComponent<SpriteRenderer>();
        state = inputState;
    }
}

struct PossibleMove
{
    public int x;
    public int y;
    public int value;
    public Vector2[] positionsFlipped;

    public PossibleMove(int inputX, int inputY, int inputValue, Vector2[] inputPositionsFlipped)
    {
        x = inputX;
        y = inputY;
        value = inputValue;
        positionsFlipped = inputPositionsFlipped;
    }
}
