using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private struct Vertex
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
        }

        private struct Edge
        {
            public readonly Vertex A;
            public readonly Vertex B;

            public Edge( Vertex a, Vertex b )
            {
                A = a;
                B = b;
            }

            public override string ToString()
            {
                return string.Format( "({0}, {1})", A, B );
            }
        }

        private struct Triangle
        {
            public readonly int A;
            public readonly int B;
            public readonly int C;
            
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

            _edges = edges.ToArray();
            _triangles = new Triangle[0];
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

            var result = _sVectorLookup[vertex.A];

            switch ( vertex.A ^ vertex.B )
            {
                case 0x1:
                    result.x = t;
                    break;
                case 0x2:
                    result.y = t;
                    break;
                case 0x4:
                    result.z = t;
                    break;
            }

            return result;
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
