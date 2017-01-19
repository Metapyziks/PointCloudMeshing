using JetBrains.Annotations;
using UnityEngine;

[ExecuteInEditMode]
public class TestPathGenerator : MonoBehaviour
{
    public bool Regenerate = false;

    [UsedImplicitly]
    private void Update()
    {
        if ( !Regenerate ) return;
        Regenerate = false;

        var path = GetComponent<Path>();
        path.Clear();

        const float thickness = 0.125f;

        path.CurrentRadius = PathNode.DefaultRadius;

        for ( var j = 0; j < 8; ++j )
        {
            var y = j * path.CurrentRadius;

            path.CurrentColor = Color.white;

            path.MoveTo( 0f, y, 0f );
            path.LineTo( 1f, y, 0f );
            path.LineTo( 1f, y, 1f );
            path.LineTo( 0f, y, 1f );
            path.LineTo( 0f, y, PathNode.DefaultRadius );

            path.MoveTo( thickness, y, thickness );
            path.LineTo( 1f - thickness, y, thickness );
            path.LineTo( 1f - thickness, y, 0.5f );
            path.LineTo( 0.5f, y, 0.5f );
            path.LineTo( thickness, y, 1f - thickness );
            path.LineTo( thickness, y, thickness + PathNode.DefaultRadius );
            
            path.CurrentColor = Color.red;

            path.MoveTo( 1f - thickness * 0.5f, y, 0.5f );
            path.LineTo( 1f - thickness * 0.5f, y, thickness * 0.5f );
            path.LineTo( thickness * 0.5f, y, thickness * 0.5f );
            path.LineTo( thickness * 0.5f, y, 1f - thickness * 0.5f );
            path.LineTo( 1f - thickness * 0.5f, y, 1f - thickness * 0.5f );

            for ( var i = 1; i < 4; ++i )
            {
                path.LineTo( 1f - thickness * 0.5f, y, 1f - i * thickness );
                path.LineTo( i * thickness + thickness * 0.707f, y, 1f - i * thickness );
                path.LineTo( (i + 0.5f) * thickness + thickness * 0.707f, y, 1f - (i + 0.5f) * thickness );
                path.LineTo( 1 - thickness * 0.5f, y, 1f - (i + 0.5f) * thickness );
            }
        }
    }
}
