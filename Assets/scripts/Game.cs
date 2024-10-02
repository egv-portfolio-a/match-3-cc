using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class Game : MonoBehaviour
{
    private enum GAMESTATE
    {
        UNKNOWN = -1,
        INITIALIZE,
        SELECT_FIRST_TILE,
        SELECT_SECOND_TILE,
        SWAP_FIRST_SECOND_TILE,
        RESOLVE_GRID,
        DESTROY_TILES,
        UNDO_SWAP,
        WAIT
    }

    public CC_GameGrid gameGrid;
    public Camera worldCamera;
    public TileDestroyer tileDestroyer;

    public GameObject debugObj;

    private Vector3Int mFirstTilePos;
    private Vector3Int mSecondTilePos;
    private List<Cube> mTilesToDestroy;

    private GAMESTATE gameState = GAMESTATE.UNKNOWN;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0; 
        Application.targetFrameRate = 60;
    }

    void Start()
    {
        gameGrid.Initialize();
        gameState = GAMESTATE.INITIALIZE;
    }

    void Update()
    {
#if UNITY_EDITOR
        Vector3 debugPos = worldCamera.ScreenToWorldPoint(Input.mousePosition);
        debugObj.transform.position = debugPos;
#endif

        switch (gameState)
        {
            case GAMESTATE.INITIALIZE:
            {
                mFirstTilePos = GameGrid.VECTOR3INT_INVALID;
                mSecondTilePos = GameGrid.VECTOR3INT_INVALID;
                mTilesToDestroy = new List<Cube>();

                gameGrid.FillMissingTiles();
                gameState = GAMESTATE.SELECT_FIRST_TILE;
                break;
            }
            case GAMESTATE.SELECT_FIRST_TILE:
            {
                SelectFirstTile();
                break;
            }
            case GAMESTATE.SELECT_SECOND_TILE:
            {
                SelectSecondTile();
                break;
            }
            case GAMESTATE.SWAP_FIRST_SECOND_TILE:
            {
                SwapFirstAndSecondTile();
                break;
            }
            case GAMESTATE.RESOLVE_GRID:
            {
                ResolveGrid();
                break;
            }
            case GAMESTATE.DESTROY_TILES:
            {
                DestroyTiles();
                break;
            }
            case GAMESTATE.UNDO_SWAP:
            {
                UndoSwap();
                break;
            }
        }
    }

    void SelectFirstTile()
    {
        Vector2 touchPos = Vector2.zero;

#if UNITY_EDITOR
        if (Input.GetButton("Fire1"))
        {
            touchPos = Input.mousePosition;
        }
#else
        if (Input.touches.Length > 0)
        {
            touchPos = Input.touches[0].position;
        }
#endif

        if (touchPos != Vector2.zero)
        {
            Vector3 touch3D = worldCamera.ScreenToWorldPoint(touchPos);
            Vector3Int touch2D = gameGrid.WorldToCell(touch3D);

#if UNITY_EDITOR
            Debug.Log("touched pos = " + touch3D + " => " + touch2D);
#endif
            if (gameGrid.IsValidPosition(touch2D))
            {
                mFirstTilePos = touch2D;
                gameState = GAMESTATE.SELECT_SECOND_TILE;
            }
        }
    }

    void SelectSecondTile()
    {
        Vector2 touchPos = Vector2.zero;

#if UNITY_EDITOR
        if (Input.GetButton("Fire1"))
        {
            touchPos = Input.mousePosition;
        }
#else
        if (Input.touches.Length > 0)
        {
            touchPos = Input.touches[0].position;
        }
#endif

        if (touchPos != Vector2.zero)
        {
            Vector3 touch3D = worldCamera.ScreenToWorldPoint(touchPos);
            Vector3Int touch2D = gameGrid.WorldToCell(touch3D);

            Vector3Int direction = (touch2D - mFirstTilePos);
            Vector3Int newSecondTilePos = new Vector3Int(mFirstTilePos.x, mFirstTilePos.y, mFirstTilePos.z);
            if (direction.x < 0)
            {
                newSecondTilePos.x = mFirstTilePos.x - 1;
            }
            else if (direction.x > 0)
            {
                newSecondTilePos.x = mFirstTilePos.x + 1;
            }
            else if (direction.y < 0)
            {
                newSecondTilePos.y = mFirstTilePos.y - 1;
            }
            else if (direction.y > 0)
            {
                newSecondTilePos.y = mFirstTilePos.y + 1;
            }

            if (gameGrid.IsValidPosition(newSecondTilePos)
                && newSecondTilePos != mFirstTilePos)
            {
                mSecondTilePos = newSecondTilePos;
                gameState = GAMESTATE.SWAP_FIRST_SECOND_TILE;
            }
        }
    }

    void SwapFirstAndSecondTile()
    {
        gameGrid.MoveObject(gameGrid.GetObjectAtPos(mFirstTilePos), mSecondTilePos, -1);
        gameGrid.MoveObject(gameGrid.GetObjectAtPos(mSecondTilePos), mFirstTilePos);

        Task.Run(async () =>
        {
            while(gameGrid.IsAnyObjectMoving())
            {
                await Task.Delay(100);
            }

            gameState = GAMESTATE.RESOLVE_GRID;
        });
        gameState = GAMESTATE.WAIT;
    }

    void ResolveGrid()
    {
        for (int y = 0; y < gameGrid.Height; y++)
        {
            for (int x = 0; x < gameGrid.Width; x++)
            {
                Cube refTile = gameGrid.GetObjectAtPos(x, y);
                if (refTile == null)
                {
                    continue;
                }

                List<Cube> tileSet = GetConnectedTiles(refTile);
                if (tileSet.Count > 0)
                {
                    mTilesToDestroy = tileSet;
                    gameState = GAMESTATE.DESTROY_TILES;
                    return;
                }
            }
        }

        if (mFirstTilePos == GameGrid.VECTOR3INT_INVALID && mSecondTilePos  == GameGrid.VECTOR3INT_INVALID)
        {
            gameState = GAMESTATE.INITIALIZE;
        }
        else
        {
            gameState = GAMESTATE.UNDO_SWAP;
        }
    }

    void DestroyTiles()
    {
        Dictionary<int, int> columnsToAdjust = new Dictionary<int, int>();

        foreach(Cube tile in mTilesToDestroy)
        {
            if (!columnsToAdjust.ContainsKey(tile.gridX))
            {
                columnsToAdjust.Add(tile.gridX, tile.gridY);
            }
            else if (columnsToAdjust[tile.gridX] > tile.gridY)
            {
                columnsToAdjust[tile.gridX] = tile.gridY;
            }

            gameGrid.DestroyObject(tile, OnDestroyTile);
        }

        foreach(KeyValuePair<int, int> column in columnsToAdjust)
        {
            int x = column.Key;
            int numTilesToDrop = 0;
            for(int y = column.Value; y < gameGrid.Height; y++)
            {
                if (gameGrid.GetObjectAtPos(x, y) == null)
                {
                    numTilesToDrop++;
                }
                else
                {
                    gameGrid.MoveObject(
                        gameGrid.GetObjectAtPos(x, y), 
                        new Vector3Int(x, y - numTilesToDrop)
                    );
                }
            }
        }

        Task.Run(async () =>
        {
            while (gameGrid.IsAnyObjectMoving())
            {
                await Task.Delay(100);
            }

            mFirstTilePos = GameGrid.VECTOR3INT_INVALID;
            mSecondTilePos = GameGrid.VECTOR3INT_INVALID;
            gameState = GAMESTATE.RESOLVE_GRID;
        });
        gameState = GAMESTATE.WAIT;
    }

    void UndoSwap()
    {
        gameGrid.MoveObject(gameGrid.GetObjectAtPos(mFirstTilePos), mSecondTilePos, -1);
        gameGrid.MoveObject(gameGrid.GetObjectAtPos(mSecondTilePos), mFirstTilePos);

        Task.Run(async () =>
        {
            while (gameGrid.IsAnyObjectMoving())
            {
                await Task.Delay(100);
            }
            gameState = GAMESTATE.INITIALIZE;
        });
        gameState = GAMESTATE.WAIT;
    }

    List<Cube> GetConnectedTiles(Cube pRefTile)
    {
        List<Cube> retVal = new List<Cube>();

        Vector3Int tilePos = new Vector3Int(pRefTile.gridX, pRefTile.gridY);

        gameGrid.GetAdjacentSimilarCells(tilePos, Vector3Int.up, retVal, pRefTile);
        gameGrid.GetAdjacentSimilarCells(tilePos, Vector3Int.down, retVal, pRefTile);

        if (retVal.Count < 3)
        {
            retVal.Clear();

            gameGrid.GetAdjacentSimilarCells(tilePos, Vector3Int.left, retVal, pRefTile);
            gameGrid.GetAdjacentSimilarCells(tilePos, Vector3Int.right, retVal, pRefTile);
        }

        if (retVal.Count < 3)
        {
            retVal.Clear();
        }

        return retVal;
    }

    void OnDestroyTile(GameObject thisTile)
    {
        tileDestroyer.Destroy(thisTile);
    }
}
