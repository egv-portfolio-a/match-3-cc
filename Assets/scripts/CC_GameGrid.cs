using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CC_GameGrid : GameGrid
{
    public float animSpeed;
    public int maxVisibleRow;

    public override void OnCreateNewTile(Cube pTile)
    {
        pTile.movementSpeed = animSpeed;
        pTile.FillWithRandomColor();
    }

    public override bool OnValidatePosition(Vector3Int pTilePos)
    {
        return pTilePos.x > -1 && pTilePos.x < Width && pTilePos.y > -1 && pTilePos.y < maxVisibleRow;
    }

    public void FillMissingTiles()
    {
        for(int y = 0; y < Height; y++)
        {
            for(int x = 0; x < Width; x++)
            {
                Cube thisTile = GetObjectAtPos(x, y);
                if (thisTile == null)
                {
                    CreateNewTileAtPos(x, y);
                }
            }
        }
    }
}
