using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cube : MonoBehaviour
{
    Color[] ValidColors = new Color[]
    {
        Color.red,
        Color.yellow,
        Color.blue,
        Color.green,
        Color.magenta,
        Color.cyan,
    };

    public float movementSpeed = 0;
    public Color color;

    public int gridX { get; set; }
    public int gridY { get; set; }

    [ContextMenu("Fill with Random Color")]
    public void FillWithRandomColor()
    {
        color = ValidColors[UnityEngine.Random.Range(0, ValidColors.Length)];

        SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>();
        renderer.color = color;
    }
}
