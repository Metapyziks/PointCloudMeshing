using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

[ExecuteInEditMode, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MarchingCubesTest : MonoBehaviour
{
    public Path Path;

    [Range(0f, 1f)]
    public float Threshold = 0.5f;
    
    public Vector3 CaptureVolume = Vector3.one;

    public int DistanceFieldResolution = 32;
    public int VerticalResolution = 16;
    
    public bool UpdateMesh;

    [UsedImplicitly]
    private void Update()
    {
        if ( UpdateMesh )
        {
            RebuildMesh();
        }
    }

    [ThreadStatic] private static MarchingCubes _sCubes;
    [ThreadStatic] private static float[] _sBufferA;
    [ThreadStatic] private static float[] _sBufferB;
    [ThreadStatic] private static List<DistanceFieldSampler.Vertex> _sVertices;

    private void RebuildMesh()
    {
        if ( Path == null ) return;

        var bufferSize = DistanceFieldResolution * DistanceFieldResolution;

        if ( _sBufferA == null || _sBufferA.Length < bufferSize )
        {
            _sBufferA = new float[bufferSize];
            _sBufferB = new float[bufferSize];
        }

        if ( _sCubes == null ) _sCubes = new MarchingCubes();
        else _sCubes.Clear();

        if ( _sVertices == null ) _sVertices = new List<DistanceFieldSampler.Vertex>();
        else _sVertices.Clear();

        Path.GetVertices( _sVertices );

        _sCubes.Threshold = Threshold;
        _sCubes.CubeSize = new Vector3(
            CaptureVolume.x / DistanceFieldResolution,
            CaptureVolume.y / VerticalResolution,
            CaptureVolume.z / DistanceFieldResolution );

        var origin = transform.position;
        var size = new Vector2( CaptureVolume.x, CaptureVolume.z );

        var last = _sBufferA;
        var next = _sBufferB;

        var values = new float[8];

        DistanceFieldSampler.SampleDistanceField( _sVertices, origin, size, DistanceFieldResolution, last );
        for ( var yb = 1; yb < VerticalResolution; ++yb )
        {
            var ya = yb - 1;

            var offset = new Vector3( 0f, (float) yb / VerticalResolution, 0f );
            DistanceFieldSampler.SampleDistanceField( _sVertices, origin + offset, size, DistanceFieldResolution, next );

            for ( var zb = 1; zb < DistanceFieldResolution; ++zb )
            {
                var za = zb - 1;
                for ( var xb = 1; xb < DistanceFieldResolution; ++xb )
                {
                    var xa = xb - 1;

                    values[0] = last[xa + za * DistanceFieldResolution];
                    values[1] = last[xb + za * DistanceFieldResolution];
                    values[2] = next[xa + za * DistanceFieldResolution];
                    values[3] = next[xb + za * DistanceFieldResolution];
                    values[4] = last[xa + zb * DistanceFieldResolution];
                    values[5] = last[xb + zb * DistanceFieldResolution];
                    values[6] = next[xa + zb * DistanceFieldResolution];
                    values[7] = next[xb + zb * DistanceFieldResolution];

                    _sCubes.MoveToCube( xa, ya, za );
                    _sCubes.Write( values );
                }
            }

            var temp = last;
            last = next;
            next = temp;
        }

        var meshFilter = this.GetComponent<MeshFilter>();

        if ( meshFilter.sharedMesh == null )
        {
            meshFilter.sharedMesh = new Mesh();
        }

        _sCubes.CopyToMesh( meshFilter.sharedMesh );
    }

    [UsedImplicitly]
    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;

        var xs = CaptureVolume.x;
        var ys = CaptureVolume.y;
        var zs = CaptureVolume.z;

        Gizmos.color = Color.green;
        Gizmos.DrawLine( new Vector3(0f, 0f, 0f), new Vector3(xs, 0f, 0f));
        Gizmos.DrawLine( new Vector3(0f, 0f, 0f), new Vector3(0f, ys, 0f));
        Gizmos.DrawLine( new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, zs));
        Gizmos.DrawLine( new Vector3(xs, 0f, 0f), new Vector3(xs, ys, 0f));
        Gizmos.DrawLine( new Vector3(xs, 0f, 0f), new Vector3(xs, 0f, zs));
        Gizmos.DrawLine( new Vector3(0f, ys, 0f), new Vector3(xs, ys, 0f));
        Gizmos.DrawLine( new Vector3(0f, ys, 0f), new Vector3(0f, ys, zs));
        Gizmos.DrawLine( new Vector3(0f, 0f, zs), new Vector3(xs, 0f, zs));
        Gizmos.DrawLine( new Vector3(0f, 0f, zs), new Vector3(0f, ys, zs));
        Gizmos.DrawLine( new Vector3(xs, ys, 0f), new Vector3(xs, ys, zs));
        Gizmos.DrawLine( new Vector3(xs, 0f, zs), new Vector3(xs, ys, zs));
        Gizmos.DrawLine( new Vector3(0f, ys, zs), new Vector3(xs, ys, zs));
    }
}
