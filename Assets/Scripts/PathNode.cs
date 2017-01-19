using UnityEngine;

public class PathNode : MonoBehaviour
{
    public const float DefaultRadius = 1f / 16f;

    public float Radius = DefaultRadius;
    public bool StartPoint;
    public Color Color = Color.white;
}