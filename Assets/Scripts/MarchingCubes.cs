using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class MarchingCubes
{
    private static string VertexIndexToString( int index )
    {
        return string.Format( "{0}:{1}:{2}", index & 1, (index >> 1) & 1, (index >> 2) & 1 );
    }

    private class MarchingCubesCase
    {
        private struct Vertex : IEquatable<Vertex>
        {
            public readonly int A;
            public readonly int B;

            public Vertex( int a, int b )
            {
                A = a;
                B = b;
            }

            public override string ToString()
            {
                return string.Format( "{{{0}, {1}}}", VertexIndexToString( A ), VertexIndexToString( B ) );
            }

            public override bool Equals( object obj )
            {
                if ( ReferenceEquals( null, obj ) ) return false;
                return obj is Vertex && Equals( (Vertex) obj );
            }

            public bool Equals( Vertex other )
            {
                return A == other.A && B == other.B;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (A * 397) ^ B;
                }
            }
        }

        private struct Edge : IEquatable<Edge>
        {
            public readonly Vertex A;
            public readonly Vertex B;

            public bool IsValid { get { return !A.Equals( B ); } }
            public Edge Reverse { get { return new Edge( B, A ); } }

            public Edge( Vertex a, Vertex b )
            {
                A = a;
                B = b;
            }

            public override string ToString()
            {
                return string.Format( "({0}, {1})", A, B );
            }

            public bool Equals( Edge other )
            {
                return A.Equals( other.A ) && B.Equals( other.B );
            }

            public override bool Equals( object obj )
            {
                if ( ReferenceEquals( null, obj ) ) return false;
                return obj is Edge && Equals( (Edge) obj );
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (A.GetHashCode() * 397) ^ B.GetHashCode();
                }
            }
        }

        private struct Triangle
        {
            public readonly int A;
            public readonly int B;
            public readonly int C;

            public Triangle( int a, int b, int c )
            {
                A = a;
                B = b;
                C = c;
            }

            public override string ToString()
            {
                return string.Format( "({0}, {1}, {2})", A, B, C );
            }
        }

        private static readonly Vector3[] _sVectorLookup =
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(1f, 1f, 0f),
            new Vector3(0f, 0f, 1f),
            new Vector3(1f, 0f, 1f),
            new Vector3(0f, 1f, 1f),
            new Vector3(1f, 1f, 1f) 
        };

        private readonly Vertex[] _vertices;
        private readonly Edge[] _edges;
        private readonly Triangle[] _triangles;

        public MarchingCubesCase( bool[] corners )
        {
            var verts = new List<Vertex>();
            var faces = new List<Vertex>[6];
            
            for ( var i = 0; i < 6; ++i ) faces[i] = new List<Vertex>();
            for ( var i = 0; i < 8; ++i )
            {
                var x = i ^ 0x1;
                var y = i ^ 0x2;
                var z = i ^ 0x4;

                Vertex vert;

                if ( corners[i] && !corners[x] )
                {
                    verts.Add( vert = new Vertex( i, x ) );
                    faces[(i & 0x2) == 0 ? 2 : 3].Add( vert );
                    faces[(i & 0x4) == 0 ? 4 : 5].Add( vert );
                }
                if ( corners[i] && !corners[y] )
                {
                    verts.Add( vert = new Vertex( i, y ) );
                    faces[(i & 0x1) == 0 ? 0 : 1].Add( vert );
                    faces[(i & 0x4) == 0 ? 4 : 5].Add( vert );
                }
                if ( corners[i] && !corners[z] )
                {
                    verts.Add( vert = new Vertex( i, z ) );
                    faces[(i & 0x1) == 0 ? 0 : 1].Add( vert );
                    faces[(i & 0x2) == 0 ? 2 : 3].Add( vert );
                }
            }

            _vertices = verts.ToArray();

            var edges = new List<Edge>();
            for ( var i = 0; i < 6; ++i )
            {
                var face = faces[i];
                if ( face.Count == 0 ) continue;
                if ( face.Count == 2 )
                {
                    edges.Add( new Edge( face[0], face[1] ) );
                    continue;
                }

                for ( var j = 0; j < face.Count - 1; ++j )
                for ( var k = j + 1; k < face.Count; ++k )
                {
                    var a = face[j];
                    var b = face[k];

                    switch ( a.A ^ b.A )
                    {
                        case 0x0:
                        case 0x1:
                        case 0x2:
                        case 0x4:
                            edges.Add( new Edge( a, b ) );
                            break;
                    }
                }
            }

            var sortedEdges = new List<Edge>();
            var triangles = new List<Triangle>();

            while ( edges.Count > 0 )
            {
                var last = edges[0];
                edges.RemoveAt( 0 );

                if ( ShouldFlip( last ) ) last = last.Reverse;

                sortedEdges.Add( last );

                var a = Array.IndexOf( _vertices, last.A );
                var b = Array.IndexOf( _vertices, last.B );

                while ( edges.Count > 0 )
                {
                    var next = edges.FirstOrDefault( x => x.A.Equals( last.B ) || x.B.Equals( last.B ) );
                    if ( !next.IsValid ) break;

                    edges.Remove( next );
                    
                    last = next.A.Equals( last.B ) ? next : next.Reverse;
                    sortedEdges.Add( last );

                    var c = Array.IndexOf( _vertices, last.B );

                    triangles.Add( new Triangle( a, b, c ) );

                    b = c;
                }
            }

            _edges = sortedEdges.ToArray();
            _triangles = triangles.ToArray();
        }

        private static bool ShouldFlip( Edge edge )
        {
            var aa = _sVectorLookup[edge.A.A];
            var ab = _sVectorLookup[edge.A.B];
            var ba = _sVectorLookup[edge.B.A];
            var bb = _sVectorLookup[edge.B.B];

            var a = (aa + ab) * 0.5f;
            var b = (ba + bb) * 0.5f;

            var solidMidPoint = (aa + ba) * 0.5f;
            var cross = Vector3.Cross( a - solidMidPoint, b - a );

            return Vector3.Dot( cross, (a + b) * 0.5f - new Vector3( 0.5f, 0.5f, 0.5f ) ) >= 0f;
        }

        [ThreadStatic] private static int[] _sVertexIndices;

        private static int[] GetVertexIndexBuffer()
        {
            return _sVertexIndices ?? (_sVertexIndices = new int[16]);
        }

        private Vector3 FindVertex( float[] values, float threshold, Vertex vertex )
        {
            var a = values[vertex.A];
            var b = values[vertex.B];
            var t = (threshold - a) / (b - a);

            var src = _sVectorLookup[vertex.A];
            var dst = _sVectorLookup[vertex.B];

            switch ( vertex.A ^ vertex.B )
            {
                case 0x1:
                    src.x += (dst.x - src.x) * t;
                    break;
                case 0x2:
                    src.y += (dst.y - src.y) * t;
                    break;
                case 0x4:
                    src.z += (dst.z - src.z) * t;
                    break;
            }

            return src;
        }

        public void Write( MarchingCubes cubes, float[] values, float threshold )
        {
            var vertIndices = GetVertexIndexBuffer();
            for ( var i = 0; i < _vertices.Length; ++i )
            {
                vertIndices[i] = cubes.WriteVertex( FindVertex( values, threshold, _vertices[i] ) );
            }

            for ( var i = 0; i < _triangles.Length; ++i )
            {
                var face = _triangles[i];
                cubes.WriteFace( vertIndices[face.A], vertIndices[face.B], vertIndices[face.C] );
            }
        }

        public void DrawGizmos( float[] values, float threshold )
        {
            for ( var i = 0; i < _edges.Length; ++i )
            {
                var edge = _edges[i];
                var a = FindVertex( values, threshold, edge.A );
                var b = FindVertex( values, threshold, edge.B );

                Gizmos.DrawLine( a, b );
            }
        }
    }

    private readonly MarchingCubesCase[] _lookupTable = new MarchingCubesCase[256];

    public Vector3 CubeSize { get; set; }
    public Vector3 CubePos { get; set; }
    public float Threshold { get; set; }
    
    private readonly List<int> _indices = new List<int>();
    private readonly List<Vector3> _vertices = new List<Vector3>();

    public MarchingCubes( Vector3 cubeSize )
    {
        PopulateLookupTable();

        CubeSize = cubeSize;
        Threshold = 0.5f;
    }

    private void PopulateLookupTable()
    {
        for ( var i = 0; i < 256; ++i )
        {
            _lookupTable[i] = new MarchingCubesCase( new[]
            {
                (i & 0x01) != 0,
                (i & 0x02) != 0,
                (i & 0x04) != 0,
                (i & 0x08) != 0,
                (i & 0x10) != 0,
                (i & 0x20) != 0,
                (i & 0x40) != 0,
                (i & 0x80) != 0
            } );
        }
    }

    public void Clear()
    {
        _indices.Clear();
        _vertices.Clear();
    }

    public void MoveToCube( int x, int y, int z )
    {
        CubePos = new Vector3( CubeSize.x * x, CubeSize.y * y, CubeSize.z * z );
    }

    private MarchingCubesCase LookupCase( float[] values )
    {
        if ( values.Length != 8 ) throw new Exception( "Expected 8 values." );

        var thresh = Threshold;
        var lookup = 0;

        for ( var i = 0; i < 8; ++i )
        {
            lookup |= values[i] >= thresh ? 1 << i : 0;
        }

        return _lookupTable[lookup];
    }

    public void Write( float[] values )
    {
        LookupCase( values ).Write( this, values, Threshold );
    }

    private void WriteFace( int a, int b, int c )
    {
        _indices.Add( a );
        _indices.Add( b );
        _indices.Add( c );
    } 

    private int WriteVertex( Vector3 vertex )
    {
        // TODO: Lookup existing vertices

        _vertices.Add( vertex );
        return _vertices.Count - 1;
    }

    public void CopyToMesh( Mesh mesh )
    {
        mesh.Clear();
        mesh.SetVertices( _vertices );
        mesh.SetTriangles( _indices, 0 );
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.UploadMeshData( false );
    }

    public void DrawGizmos( float[] values )
    {
        LookupCase( values ).DrawGizmos( values, Threshold );
    }
}
