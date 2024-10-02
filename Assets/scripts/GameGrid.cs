using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameGrid : MonoBehaviour
{
    public static readonly Vector3Int VECTOR3INT_INVALID = new Vector3Int(-1, -1);

    private class TileMover
    {
        public Cube target;
        public Vector3Int endPos = VECTOR3INT_INVALID;
        public Grid refGrid;
        public float movementSpeed = 0;
        public float moveLayerIndex = -1;

        public bool isMoving { get; private set; }

        private Vector3 mCurrentPosition = VECTOR3INT_INVALID;
        private Vector3 mDestPos3D = VECTOR3INT_INVALID;
        private float mOriginalZ = 0;

        private Vector3 mMoveDirection = VECTOR3INT_INVALID;
        private float mLastDistance = 0;

        private UnityAction mOnMovementComplete;

        public void Start(UnityAction pOnMovementComplete)
        {
            mOnMovementComplete = pOnMovementComplete;

            Vector3 destPos3D = refGrid.CellToWorld(endPos);

            mCurrentPosition = new Vector3(target.transform.position.x, target.transform.position.y, moveLayerIndex);
            mDestPos3D = new Vector3(destPos3D.x, destPos3D.y, moveLayerIndex);
            mOriginalZ = target.transform.position.z;

            mMoveDirection = (mDestPos3D - mCurrentPosition).normalized * movementSpeed;
            mLastDistance = (mDestPos3D - mCurrentPosition).magnitude;

            isMoving = true;
        }

        public void Update()
        {
            if (!isMoving) return;

            mCurrentPosition += new Vector3(mMoveDirection.x * Time.deltaTime, mMoveDirection.y * Time.deltaTime, 0);

            float currentDistance = (mDestPos3D - mCurrentPosition).magnitude;
            if (currentDistance >= mLastDistance)
            {
                mCurrentPosition = new Vector3(mDestPos3D.x, mDestPos3D.y, mOriginalZ);
                isMoving = false;

                mOnMovementComplete.Invoke();
            }

            target.transform.position = mCurrentPosition;
            mLastDistance = currentDistance;
        }
    }

    public int Width;
    public int Height;

    public GameObject SpritePrefab;

    private Grid grid;
    private Cube[][] gridObjects;

    private List<TileMover> movingTiles;

    [ContextMenu("Build Grid")]
    public void Initialize()
    {
        grid = GetComponent<Grid>();
        movingTiles = new List<TileMover>();

        while (grid.transform.childCount > 0)
        {
            GameObject.DestroyImmediate(grid.transform.GetChild(0).gameObject);
        }

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (gridObjects == null) gridObjects = new Cube[Width][];
                if (gridObjects[x] == null) gridObjects[x] = new Cube[Height];

                CreateNewTileAtPos(x, y);
            }
        }
    }

    public Vector3Int WorldToCell(Vector3 pWorldPos)
    {
        return grid.WorldToCell(pWorldPos);
    }

    public void CreateNewTileAtPos(int pX, int pY)
    {
        GameObject newObject = GameObject.Instantiate(SpritePrefab, grid.transform, false);
        newObject.transform.localPosition = grid.CellToLocal(new Vector3Int(pX, pY));

        Cube newGridObject = newObject.GetComponentInChildren<Cube>();
        newGridObject.gridX = pX;
        newGridObject.gridY = pY;

        gridObjects[pX][pY] = newGridObject;

        OnCreateNewTile(newGridObject);
    }

    public Cube GetObjectAtPos(Vector3Int pPos)
    {
        return GetObjectAtPos(pPos.x, pPos.y);
    }

    public Cube GetObjectAtPos(int pX, int pY)
    {
        if (pX < 0 || pY < 0) return null;
        if (pX >= Width || pY >= Height) return null;

        return gridObjects[pX][pY];
    }

    public void DestroyObjectAtPos(Vector3Int pPos, UnityAction<GameObject> pOnDestroy)
    {
        DestroyObjectAtPos(pPos.x, pPos.y, pOnDestroy);
    }

    public void DestroyObjectAtPos(int pX, int pY, UnityAction<GameObject> pOnDestroy)
    {
        if (gridObjects[pX][pY] == null) return;

        Cube thisObject = gridObjects[pX][pY];

        gridObjects[pX][pY] = null;
        pOnDestroy(thisObject.gameObject);
    }

    public void DestroyObject(Cube pThisObject, UnityAction<GameObject> pOnDestroy)
    {
        if (gridObjects[pThisObject.gridX][pThisObject.gridY] == null) return;

        gridObjects[pThisObject.gridX][pThisObject.gridY] = null;
        pOnDestroy(pThisObject.gameObject);
    }

    public bool IsValidPosition(Vector3Int pPos)
    {
        return OnValidatePosition(pPos);
    }

    public void MoveObject(Cube pTarget, Vector3Int pEndPos, float pZ = 1)
    {
        Debug.Assert(pTarget != null, "pTarget is null.");

        TileMover newTileMover = new TileMover();
        newTileMover.target = pTarget;
        newTileMover.refGrid = grid;
        newTileMover.endPos = pEndPos;
        newTileMover.moveLayerIndex = pTarget.transform.position.z + pZ;
        newTileMover.movementSpeed = pTarget.movementSpeed;
        newTileMover.Start(() =>
        {
            gridObjects[pEndPos.x][pEndPos.y] = pTarget;
            pTarget.gridX = pEndPos.x;
            pTarget.gridY = pEndPos.y;
        });

        movingTiles.Add(newTileMover);
        gridObjects[pTarget.gridX][pTarget.gridY] = null;
    }

    public bool IsObjectMoving(Cube pObject)
    {
        return movingTiles.Find(item => item.target == pObject) != null;
    }

    public bool IsAnyObjectMoving()
    {
        return movingTiles.Count > 0;
    }

    public void GetAdjacentSimilarCells(Vector3Int pTilePos, Vector3Int pDir, List<Cube> pCollectionList, Cube pRefTile)
    {
        if (!IsValidPosition(pTilePos))
        {
            return;
        }

        if (gridObjects[pTilePos.x][pTilePos.y] == null)
        {
            GetAdjacentSimilarCells(pTilePos + pDir, pDir, pCollectionList, pRefTile);
            return;
        }

        Cube thisCubeObj = gridObjects[pTilePos.x][pTilePos.y].GetComponentInChildren<Cube>();
        if (thisCubeObj != null && thisCubeObj.color == pRefTile.color && !pCollectionList.Contains(thisCubeObj))
        {
            pCollectionList.Add(thisCubeObj);
            GetAdjacentSimilarCells(pTilePos + pDir, pDir, pCollectionList, pRefTile);
        }
    }

    public virtual void OnCreateNewTile(Cube pTile) { }

    public virtual bool OnValidatePosition(Vector3Int pTilePos)
    {
        return pTilePos.x > -1 && pTilePos.x < Width && pTilePos.y > -1 && pTilePos.y < Height;
    }

    void Update()
    {
        int i = 0;
        while(i < movingTiles.Count)
        {
            movingTiles[i].Update();
            if (movingTiles[i].isMoving)
            {
                i++;
            }
            else
            {
                movingTiles.Remove(movingTiles[i]);
            }
        }
    }
}
