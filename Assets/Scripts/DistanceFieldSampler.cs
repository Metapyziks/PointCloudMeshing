using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

[ExecuteInEditMode]
public class DistanceFieldSampler : MonoBehaviour
{
    #region Unity Junk
    public Path Path;
    public Texture2D Result;
    public int Resolution = 128;

    private Vector2 _lastSampleSize;
    private Vector3 _lastSampledPosition;

    private Vector2 SampleSize
    {
        get
        {
            var scale = transform.lossyScale;
            return new Vector2( scale.x, scale.y );
        }
    }

    private bool NeedsUpdate
    {
        get
        {
            if ( Path == null ) return false;
            return Result == null || Result.width != Resolution || _lastSampledPosition != transform.position
                || Math.Abs( _lastSampleSize.x - SampleSize.x ) > float.Epsilon
                || Math.Abs( _lastSampleSize.y - SampleSize.y ) > float.Epsilon;
        }
    }
    #endregion

    /// <summary>
    /// Represents a point along a path.
    /// </summary>
    public struct Vertex
    {
        /// <summary>
        /// Position in 3D space of the vertex.
        /// </summary>
        public readonly Vector3 Pos;

        /// <summary>
        /// Radius of the line segment starting at this vertex.
        /// </summary>
        public readonly float Radius;

        /// <summary>
        /// If true, this vertex has no edge preceding it.
        /// </summary>
        public readonly bool StartPoint;

        public Vertex( Vector3 pos, float radius, bool start )
        {
            Pos = pos;
            Radius = radius;
            StartPoint = start;
        }

        public Vertex( PathNode node )
        {
            Pos = node.transform.position;
            Radius = node.Radius;
            StartPoint = node.StartPoint;
        }
    }

    /// <summary>
    /// A pair of vertices that define a line segment.
    /// </summary>
    private struct Edge
    {
        /// <summary>
        /// Start vertex of the line segment.
        /// </summary>
        public readonly Vertex First;

        /// <summary>
        /// End vertex of the line segment.
        /// </summary>
        public readonly Vertex Second;

        public Edge( Vertex first, Vertex second )
        {
            First = first;
            Second = second;
        }
    }

    [ThreadStatic] private static List<Edge> _sNearbyEdges;

    /// <summary>
    /// Find a list of edgest from the given vertex list that will possibly intesect with the
    /// given rectangle, defined by it's vertical position (<paramref name="layerPos"/>), and
    /// minimum and maximum X and Z bounds (<paramref name="min"/> and <paramref name="max"/>).
    /// </summary>
    /// <param name="path">List of vertices to search through/</param>
    /// <param name="layerPos">Vertical position of the rectangle to find edges near to.</param>
    /// <param name="min">Minimum X and Z coordinates of the rectangle.</param>
    /// <param name="max">Maximum X and Z coordinates of the rectangle.</param>
    /// <param name="outEdges">List to append the results to.</param>
    private static void FindNearbyEdges( List<Vertex> path, float layerPos, Vector2 min, Vector2 max, List<Edge> outEdges )
    {
        var prev = path[path.Count - 1];
        foreach ( var next in path )
        {
            if ( !next.StartPoint )
            {
                var edge = new Edge( prev, next );
                if ( IsEdgeNearSampleRange( ref edge, layerPos, min, max ) )
                {
                    outEdges.Add( edge );
                }
            }

            prev = next;
        }
    }

    private static bool IsEdgeNearSampleRange( ref Edge edge, float layerPos, Vector2 min, Vector2 max )
    {
        // Check if completely above
        if ( edge.First.Pos.y - edge.First.Radius > layerPos && edge.Second.Pos.y - edge.First.Radius > layerPos ) return false;

        // Check if completely below
        if ( edge.First.Pos.y + edge.First.Radius < layerPos && edge.Second.Pos.y + edge.First.Radius < layerPos ) return false;

        // TODO: Check if edge is outside of the square.
        return true;
    }

    private static float GetDistance( Vector3 samplePos, Edge edge )
    {
        var ap = samplePos - edge.First.Pos;
        var bp = samplePos - edge.Second.Pos;
        var ab = edge.Second.Pos - edge.First.Pos;

        if ( Vector3.Dot( ap, ab ) < 0f ) return ap.magnitude;
        if ( Vector3.Dot( bp, ab ) > 0f ) return bp.magnitude;

        return Mathf.Sqrt( Vector3.Cross( ap, bp ).sqrMagnitude / ab.sqrMagnitude );
    }

    private static float DistanceToDistanceScore( float distance, float radius )
    {
        return Mathf.Clamp01( 1f - distance * 0.5f / radius );
    }

    private static float GetDistanceScore( Vector3 samplePos, List<Edge> edges )
    {
        var score = 0f;

        foreach ( var edge in edges )
        {
            score = Math.Max( score, DistanceToDistanceScore( GetDistance( samplePos, edge ), edge.First.Radius ) );
        }

        return score;
    }

    public static void SampleDistanceField( List<Vertex> path, Vector3 origin, Vector2 size, int resolution, float[] outSamples )
    {
        if ( outSamples.Length < resolution * resolution )
        {
            throw new ArgumentException("Expected outSamples to be at least " + resolution * resolution + " in length.");
        }

        Array.Clear( outSamples, 0, resolution * resolution );

        if ( path.Count < 2 ) return;

        var layerPos = origin.y;
        var min = new Vector2( origin.x, origin.z );
        var max = min + size;

        if ( _sNearbyEdges == null ) _sNearbyEdges = new List<Edge>();
        else _sNearbyEdges.Clear();

        FindNearbyEdges( path, layerPos, min, max, _sNearbyEdges );

        for ( var row = 0; row < resolution; ++row )
        {
            var z = row * (max.y - min.y) / resolution + min.y;
            for ( var col = 0; col < resolution; ++col )
            {
                var x = col * (max.x - min.x) / resolution + min.x;
                var pos = new Vector3( x, layerPos, z );

                outSamples[col + row * resolution] = GetDistanceScore( pos, _sNearbyEdges );
            }
        }
    }

    [ThreadStatic] private static float[] _sBuffer;
    [ThreadStatic] private static Color[] _sColors;
    [ThreadStatic] private static List<Vertex> _sPath;

    [UsedImplicitly]
    private void Update()
    {
        if ( !NeedsUpdate ) return;

        _lastSampledPosition = transform.position;
        _lastSampleSize = SampleSize;

        if ( Result == null )
        {
            Result = new Texture2D( Resolution, Resolution, TextureFormat.RGB24, false );
        }
        else if ( Resolution != Result.width )
        {
            Result.Resize( Resolution, Resolution, TextureFormat.RGB24, false );
        }

        if ( _sPath == null ) _sPath = new List<Vertex>();
        else _sPath.Clear();

        Path.GetVertices( _sPath );

        if ( _sBuffer == null || _sBuffer.Length < Resolution * Resolution )
        {
            _sBuffer = new float[Resolution * Resolution];
            _sColors = new Color[Resolution * Resolution];
        }

        var origin = _lastSampledPosition - new Vector3( _lastSampleSize.x * 0.5f, 0f, _lastSampleSize.y * 0.5f );
        SampleDistanceField( _sPath, origin, _lastSampleSize, Resolution, _sBuffer );

        for ( var i = 0; i < Resolution * Resolution; ++i )
        {
            var value = _sBuffer[i];
            _sColors[i] = new Color( value, value, value, 1f );
        }

        Result.SetPixels( _sColors );
        Result.Apply( false );

        var meshRenderer = GetComponent<MeshRenderer>();
        if ( meshRenderer != null )
        {
            meshRenderer.sharedMaterial.mainTexture = Result;
        }
    }
}
