using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TileDestroyer : MonoBehaviour
{
    public float animSpeed = 10.0f;

    private List<MoveAction> tilesToDestroy = new List<MoveAction>();

    bool test = false;

    public void Destroy(GameObject pTile)
    {
        pTile.transform.parent = this.transform;

        pTile.layer = this.gameObject.layer;
        foreach(Transform child in pTile.transform)
        {
            child.gameObject.layer = this.gameObject.layer;
        }

        pTile.transform.position = new Vector3(pTile.transform.position.x, pTile.transform.position.y, this.transform.position.z);
        Vector3 destination = pTile.transform.position + new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), 0);

        MoveAction mover1 = pTile.AddComponent<MoveAction>();
        mover1.Move(destination, animSpeed, (obj) =>
        {
            mover1.enabled = false;

            MoveAction mover2 = pTile.AddComponent<MoveAction>();
            mover2.Move(this.transform.position, animSpeed, DestroyTile);
            tilesToDestroy.Add(mover2);
        });
    }

    void DestroyTile(MoveAction pObj)
    {
        GameObject.Destroy(pObj.gameObject);
        tilesToDestroy.Remove(pObj);
    }
}
