using JetBrains.Annotations;
using UnityEngine;

public class Path : MonoBehaviour
{
    public float CurrentRadius = PathNode.DefaultRadius;
    public Color CurrentColor = Color.white;

    private void AddNode( Vector3 pos, bool startPoint )
    {
        var node = new GameObject( "Node " + transform.childCount, typeof(PathNode) )
            .GetComponent<PathNode>();

        node.StartPoint = startPoint;
        node.Radius = CurrentRadius;
        node.Color = CurrentColor;
        node.transform.SetParent( transform, false );
        node.transform.localPosition = pos;
    }

    public void Clear()
    {
        foreach ( var node in transform.GetComponentsInChildren<PathNode>() )
        {
            DestroyImmediate( node.gameObject );
        }
    }

    public void MoveTo( float x, float y, float z )
    {
        AddNode( new Vector3( x, y, z ), true );
    }

    public void LineTo( float x, float y, float z )
    {
        AddNode( new Vector3( x, y, z ), false );
    }

    [UsedImplicitly]
    private void OnDrawGizmos()
    {
        var nodes = transform.GetComponentsInChildren<PathNode>();
        if ( nodes.Length < 2 ) return;

        var prev = nodes[nodes.Length - 1];
        foreach ( var next in nodes )
        {
            if ( !next.StartPoint )
            {
                Gizmos.color = Color.Lerp( prev.Color, next.Color, 0.5f );
                Gizmos.DrawLine( prev.transform.position, next.transform.position );
            }

            prev = next;
        }
    }
}
