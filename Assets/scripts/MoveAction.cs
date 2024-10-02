using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class MoveAction : MonoBehaviour
{
    public float Speed = 1;
    public Vector3 Destination;
    public float threshold = 0.1f;
    public UnityAction<MoveAction> OnArrive;

    private float mLastDistance = 99999;

    public void Move(float pX, float pY, float pZ, float pSpeed, UnityAction<MoveAction> pOnDestinationReached)
    {
        Move(new Vector3(pX, pY, pZ), pSpeed, pOnDestinationReached);
    }

    public void Move(Vector3 pPos, float pSpeed, UnityAction<MoveAction> pOnDestinationReached)
    {
        Destination = pPos;
        Speed = pSpeed;
        OnArrive = pOnDestinationReached;
    }

    void Update()
    {
        Vector3 myPosition = transform.position;
        Vector3 offset = Destination - myPosition;
        Vector3 direction = offset.normalized;
        float distance = offset.magnitude;

        if (distance < mLastDistance)
        {
            mLastDistance = distance;
            transform.position += (direction * Time.deltaTime * Speed);
        }
        else
        {
            OnArrive(this);
        }
    }
}
