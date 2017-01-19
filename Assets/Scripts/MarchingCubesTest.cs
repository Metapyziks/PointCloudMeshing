using JetBrains.Annotations;
using UnityEngine;

[ExecuteInEditMode, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MarchingCubesTest : MonoBehaviour
{
    [Range(0f, 1f)]
    public float Threshold = 0.5f;
    
    [Range(0f, 1f)] public float X0Y0Z0 = 0f;
    [Range(0f, 1f)] public float X1Y0Z0 = 0f;
    [Range(0f, 1f)] public float X0Y1Z0 = 0f;
    [Range(0f, 1f)] public float X1Y1Z0 = 0f;
    [Range(0f, 1f)] public float X0Y0Z1 = 0f;
    [Range(0f, 1f)] public float X1Y0Z1 = 0f;
    [Range(0f, 1f)] public float X0Y1Z1 = 0f;
    [Range(0f, 1f)] public float X1Y1Z1 = 0f;

    private readonly float[] _values = new float[8];
    private readonly MarchingCubes _cubes = new MarchingCubes( Vector3.one );

    private void CopyValuesToArray()
    {
        _values[0] = X0Y0Z0;
        _values[1] = X1Y0Z0;
        _values[2] = X0Y1Z0;
        _values[3] = X1Y1Z0;
        _values[4] = X0Y0Z1;
        _values[5] = X1Y0Z1;
        _values[6] = X0Y1Z1;
        _values[7] = X1Y1Z1;
    }

    [UsedImplicitly]
    private void Update()
    {
        var filter = GetComponent<MeshFilter>();

        if ( filter.sharedMesh == null ) filter.sharedMesh = new Mesh();

        CopyValuesToArray();

        new MarchingCubes( Vector3.one );

        _cubes.Threshold = Threshold;
        _cubes.Clear();
        _cubes.Write( _values );

        _cubes.CopyToMesh( filter.sharedMesh );
    }

    [UsedImplicitly]
    private void OnDrawGizmos()
    {
        CopyValuesToArray();

        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = Color.green;
        Gizmos.DrawLine( new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f));
        Gizmos.DrawLine( new Vector3(0f, 0f, 0f), new Vector3(0f, 1f, 0f));
        Gizmos.DrawLine( new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 1f));
        Gizmos.DrawLine( new Vector3(1f, 0f, 0f), new Vector3(1f, 1f, 0f));
        Gizmos.DrawLine( new Vector3(1f, 0f, 0f), new Vector3(1f, 0f, 1f));
        Gizmos.DrawLine( new Vector3(0f, 1f, 0f), new Vector3(1f, 1f, 0f));
        Gizmos.DrawLine( new Vector3(0f, 1f, 0f), new Vector3(0f, 1f, 1f));
        Gizmos.DrawLine( new Vector3(0f, 0f, 1f), new Vector3(1f, 0f, 1f));
        Gizmos.DrawLine( new Vector3(0f, 0f, 1f), new Vector3(0f, 1f, 1f));
        Gizmos.DrawLine( new Vector3(1f, 1f, 0f), new Vector3(1f, 1f, 1f));
        Gizmos.DrawLine( new Vector3(1f, 0f, 1f), new Vector3(1f, 1f, 1f));
        Gizmos.DrawLine( new Vector3(0f, 1f, 1f), new Vector3(1f, 1f, 1f));
        
        Gizmos.color = Color.white;
        _cubes.Threshold = Threshold;
        _cubes.DrawGizmos( _values );
    }
}
