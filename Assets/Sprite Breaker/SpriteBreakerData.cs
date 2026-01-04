using System.Collections.Generic;
using UnityEngine;
using SpriteBreakerUtil;

[System.Serializable]
public class SpriteBreakerPolygon {

    // vertices, uv, edges, and pivot are all in the coordinate system (0,0 bottom left -> 1,1 upper right), representing sprite rect
    public Vector3[] vertices;
    public Vector2[] uv;
    public Vector3[] normals;
    public int[] triangles;
    public List<Vector2> edges;
    public Vector2 pivot;

    public string name = ""; // spawned game object will have this name assigned to it
    public float zOffset = 0; // offset shard in z-axis by this amount
    public int sortingOrderOffset = 0; // offset shard's renderer layer sorting order this number (from sprite renderer's sorting order)
    public bool active = true; // shard will be spawned active
    public bool enabled = true; // controls if shard will be spawned
    public bool includeInPhysics = true; // shard will be updated by physics
    public string tag = ""; // spawned game object will get this tag
    public float floatValue = 0; // reserved, can be used to pass data
    public string stringValue = ""; // reserved, can be used to pass data
    public Object extra = null; // reserved, can be used to hold arbitrary data

    public Color shardColor = Color.white; // shard starting color
    public Color currentColor = Color.white; // last set shard color - SpriteBreaker.shardColor multiplied by polygon's own color 
    public bool skipColorUpdate = false; // don't update this shard's color
    [System.NonSerialized] public Renderer cachedRenderer; // used to update color
    [System.NonSerialized] public Component cachedBody; // used to init physics impulse

    public GameObject gameObject; // spawned game object reference
    public Vector3 startPosition; // starting position

    // fake physics - used in SpriteBreaker simulation
    [System.NonSerialized] public Vector3 fakePhysicsVelocity = new Vector3();
    [System.NonSerialized] public Vector3 fakePhysicsRotationalVelocity = new Vector3();
    [System.NonSerialized] public float fakePhysicsMass = 1;
    [System.NonSerialized] public float fakePhysicsVelocityDamping = 0.1f, fakePhysicsRotationalVelocityDamping = 0.1f;
    private static readonly int _Color = Shader.PropertyToID( "_Color" );

    /// <summary>
    /// Retriangulates this polygon.
    /// </summary>
    /// <param name="checkWinding">ensure polygon isn't inverted</param>
    /// <param name="resetPivot">average pivot among the vertices</param>
    /// <param name="breakerData">optional data, used to correct selection in edit mode if winding has changed</param>
    /// <returns>True if successful</returns>
    public bool Triangulate( bool checkWinding = true, bool resetPivot = false, SpriteBreakerData breakerData=null ) {
        
        // ensure clockwise
        if ( checkWinding ) {
            float signedArea = 0;
            for ( int i = 0, ne = edges.Count; i < ne; i++ ) {
                Vector2 v0 = edges[ i ], v1 = edges[ ( i + 1 ) % ne ];
                signedArea += v0.x * v1.y - v1.x * v0.y;
            }
            if ( signedArea < 0 ) {
                edges.Reverse();
                if ( breakerData != null ) {
                    int ip = breakerData.polygons.IndexOf( this );
                    if ( breakerData.selectedPoints.ContainsKey( ip ) ) {
                        List<int> sps = breakerData.selectedPoints[ ip ];
                        for ( int i = sps.Count - 1, ne = edges.Count - 1; i >= 0; i-- ) {
                            sps[ i ] = ne - sps[ i ];
                        }
                    }
                }
            }
        }

        // average pivot
        if ( resetPivot ) {
            pivot.Set( 0, 0 );
            foreach ( Vector2 t in edges ) {
                pivot += t;
            }
            pivot /= edges.Count;
        }

        // triangulate
        Polygon2D py = new Polygon2D( edges );
        Triangulation2D triangulation = new Triangulation2D( py );

        // successful?
        if ( triangulation.Triangles.Length > 0 ) {

            // vertices, uvs, and normals
            List<Vertex2D> verts = triangulation.Points;
            vertices = new Vector3[ verts.Count ];
            normals = new Vector3[ verts.Count ];
            uv = new Vector2[ verts.Count ];
            Vector3 normal = Vector3.back;
            for ( int i = 0, nv = verts.Count; i < nv; i++ ) {
                Vector2 v = verts[ i ].Coordinate;
                Vector3 v3 = new Vector3( v.x, v.y, 0 );
                vertices[ i ] = v3;
                normals[ i ] = normal;
                uv[ i ] = v;
            }

            // triangles
            triangles = new int[ triangulation.Triangles.Length * 3 ];
            for ( int i = 0, nt = triangulation.Triangles.Length; i < nt; i++ ) {
                Triangle2D t = triangulation.Triangles[ i ];
                triangles[ i * 3 ] = t.a.index;
                if ( Utils2D.LeftSide( t.a.Coordinate, t.b.Coordinate, t.c.Coordinate ) ) {
                    triangles[ i * 3 + 1 ] = t.c.index; triangles[ i * 3 + 2 ] = t.b.index;
                } else {
                    triangles[ i * 3 + 1 ] = t.b.index; triangles[ i * 3 + 2 ] = t.c.index;
                }
            }

            // success
            return true;

        } else {

            // bad result
            triangles = null;
            vertices = null;
            uv = null;
            normals = null;
            return false;
        }

    }

    /// <summary>
    /// Clones SpriteBreakerPolygon
    /// </summary>
    /// <param name="offsetX">optional offset X</param>
    /// <param name="offsetY">optional offset Y</param>
    /// <returns>new copy</returns>
    public SpriteBreakerPolygon Clone( float offsetX = 0, float offsetY = 0 ) {
        SpriteBreakerPolygon copy = new SpriteBreakerPolygon();
        copy.edges = edges != null ? edges.GetRange( 0, edges.Count ) : null;
        copy.triangles = triangles != null ? (int[]) triangles.Clone() : null;
        copy.vertices = vertices != null ? (Vector3[]) vertices.Clone() : null;
        copy.normals = vertices != null ? (Vector3[]) normals.Clone() : null;
        copy.uv = vertices != null ? (Vector2[]) uv.Clone() : null;
        copy.name = name;
        copy.enabled = enabled;
        copy.tag = tag;
        copy.floatValue = floatValue;
        copy.stringValue = stringValue;
        copy.pivot = pivot;
        Vector2 offset = new Vector2( offsetX, offsetY );
        Vector2 offsetUV = new Vector2( offsetX, -offsetY );
        Vector3 offset3 = new Vector3( offsetX, offsetY, 0 );
        if ( offset.sqrMagnitude > Mathf.Epsilon ) {
            copy.pivot += offset;
            for ( int i = 0, n = copy.edges.Count; i < n; i++ ) copy.edges[ i ] += offset;
            for ( int i = 0, n = copy.vertices.Length; i < n; i++ ) copy.vertices[ i ] += offset3;
            for ( int i = 0, n = copy.uv.Length; i < n; i++ ) copy.uv[ i ] += offsetUV;
        }
        return copy;
    }

    // updates MeshRenderer on attached shard with current color and sprite texture
    public void UpdateRenderer( SpriteBreaker spriteBreaker, MaterialPropertyBlock block ) {
        if ( gameObject == null ) return;
        if ( cachedRenderer == null ) cachedRenderer = gameObject.GetComponent<Renderer>();
        currentColor = shardColor * spriteBreaker.shardsColor;
        if ( cachedRenderer != null ) {
            if ( !skipColorUpdate ) block.SetColor( _Color, currentColor );
            cachedRenderer.SetPropertyBlock( block );
        }
    }

    // returns true if point is inside this polygon
    public bool PointInPolygon( Vector2 point ) {
        int i, j = edges.Count - 1;
        bool oddNodes = false;
        for ( i = 0; i < edges.Count; i++ ) {
            if ( ( edges[ i ].y < point.y && edges[ j ].y >= point.y
            || edges[ j ].y < point.y && edges[ i ].y >= point.y )
            && ( edges[ i ].x <= point.x || edges[ j ].x <= point.x ) ) {
                oddNodes ^= ( edges[ i ].x + ( point.y - edges[ i ].y ) / ( edges[ j ].y - edges[ i ].y ) * ( edges[ j ].x - edges[ i ].x ) < point.x );
            }
            j = i;
        }
        return oddNodes;
    }

    public float Area() {
        float area = 0;
        for ( int i = 0, n = edges.Count; i < n; i++ ) {
            int i1 = ( i + 1 ) % n;
            area += ( edges[ i ].y + edges[ i1 ].y ) * ( edges[ i1 ].x - edges[ i ].x );
        }
        return Mathf.Abs( area * 0.5f );
    }

    // simulates fake physics
    public void UpdateFakePhysics( Vector3 gravity ) {
        if ( gameObject == null ) return;
        float dt = Time.deltaTime;
        Transform transform = gameObject.transform;
        transform.localRotation *= Quaternion.Euler( fakePhysicsRotationalVelocity * dt ); // update rotation
        fakePhysicsVelocity += gravity * dt; // update velocity
        transform.localPosition += fakePhysicsVelocity * dt; // update position

        // damping
        if ( fakePhysicsVelocityDamping > 0 ) fakePhysicsVelocity *= ( 1 - dt * fakePhysicsVelocityDamping );
        if ( fakePhysicsRotationalVelocityDamping > 0 ) fakePhysicsRotationalVelocity *= ( 1 - dt * fakePhysicsRotationalVelocityDamping );        
    }
}

// data class containing polygons for shattering a sprite
[System.Serializable]
public class SpriteBreakerData {

    // polygons
    public List<SpriteBreakerPolygon> polygons = new List<SpriteBreakerPolygon>();

    // stores selection for SpriteBreakerEditor
    public List<int> selectedPolygons = new List<int>();
    public Dictionary<int, List<int>> selectedPoints = new Dictionary<int, List<int>>();

    /// <summary>
    /// Populates and triangulates data using an existing sprite. Sprite's skinning mesh is used.
    /// </summary>
    /// <param name="sprite"></param>
    public void PopulateFromSprite( Sprite sprite ) {

        // reset        
        selectedPoints.Clear();
        selectedPolygons.Clear();

        // if empty, just make a quad
        if ( sprite == null ) {
            InitAsQuad();
            return;
        } else polygons.Clear();

        Vector2 spriteSize = sprite.rect.size;
        Vector2 spritePivot = sprite.pivot;
        Vector2[] verts = sprite.vertices;
        ushort[] tris = sprite.triangles;
        float spritePixelsPerUnit = sprite.pixelsPerUnit;

        // all edges as v0, v1, useCount
        List<Vector3Int> allEdges = new List<Vector3Int>();

        // for each triangle
        for ( int i = 0; i < tris.Length; i += 3 ) {
            int v0 = tris[ i ], v1 = tris[ i + 1 ], v2 = tris[ i + 2 ];
            int e00 = ( v0 < v1 ? v0 : v1 ), e01 = ( e00 == v0 ? v1 : v0 ); // v0,v1 ordered
            int e10 = ( v1 < v2 ? v1 : v2 ), e11 = ( e10 == v1 ? v2 : v1 ); // v1,v2 ordered
            int e20 = ( v2 < v0 ? v2 : v0 ), e21 = ( e20 == v2 ? v0 : v2 ); // v2,v0 ordered
            // find existing edges
            int existing0 = -1, existing1 = -1, existing2 = -1;
            for ( int j = 0; j < allEdges.Count; j++ ) {
                Vector3Int edge = allEdges[ j ];
                if ( edge.x == e00 && edge.y == e01 ) {
                    existing0 = j;
                } else if ( edge.x == e10 && edge.y == e11 ) {
                    existing1 = j;
                } else if ( edge.x == e20 && edge.y == e21 ) {
                    existing2 = j;
                } else continue;
                edge.z++; allEdges[ j ] = edge;
            }
            if ( existing0 < 0 ) allEdges.Add( new Vector3Int( e00, e01, 0 ) );
            if ( existing1 < 0 ) allEdges.Add( new Vector3Int( e10, e11, 0 ) );
            if ( existing2 < 0 ) allEdges.Add( new Vector3Int( e20, e21, 0 ) );
        }

        // extract only edges with z == 0
        List<Vector2Int> borderEdges = new List<Vector2Int>();
        for ( int i = 0; i < allEdges.Count; i++ ) {
            Vector3Int edge = allEdges[ i ];
            if ( edge.z == 0 ) borderEdges.Add( new Vector2Int( edge.x, edge.y ) );
        }

        // multiple polygons
        int borderStart = 0;
        float signedArea = 0;
        SpriteBreakerPolygon currentPoly = new SpriteBreakerPolygon();
        polygons.Add( currentPoly );

        // order edges v0->v1 , v1->v2 ... 
        for ( int i = 0, ne = borderEdges.Count; i < ne; i++ ) {
            Vector2Int edge = borderEdges[ i ];
            Vector2 v0 = verts[ edge.x ], v1 = verts[ edge.y ];
            signedArea += v0.x * v1.y - v1.x * v0.y;

            // find next
            bool lastInChain = true;
            for ( int j = i + 1; j < borderEdges.Count; j++ ) {
                Vector2Int nextEdge = borderEdges[ j ];
                if ( nextEdge.y == edge.y ) { // flip v0, v1
                    int temp = nextEdge.x; nextEdge.x = edge.y; nextEdge.y = temp;
                } else if ( nextEdge.x != edge.y ) continue;
                // found next, swap j, i+1
                if ( j > i + 1 ) borderEdges[ j ] = borderEdges[ i + 1 ];
                borderEdges[ i + 1 ] = nextEdge;
                lastInChain = false;
                break;
            }

            // if end of chain
            if ( lastInChain ) {

                // add edge vertices (borderStart to i)
                currentPoly.edges = new List<Vector2>( i - borderStart + 1 );
                for ( int j = borderStart; j <= i; j++ ) {
                    Vector2 p = ( verts[ borderEdges[ j ].x ] * spritePixelsPerUnit + spritePivot ) / spriteSize;
                    currentPoly.edges.Add( p );
                    currentPoly.pivot += p;
                }

                // average pivot
                currentPoly.pivot /= currentPoly.edges.Count;

                // triangulate
                currentPoly.Triangulate( false );

                // next chain
                if ( i < ne - 1 ) {
                    borderStart = i + 1;
                    signedArea = 0;
                    currentPoly = new SpriteBreakerPolygon();
                    polygons.Add( currentPoly );
                }
            }
        }
    }

    /// <summary>
    /// Resets data to a quad covering entire sprite.
    /// </summary>
    public void InitAsQuad() {
        SpriteBreakerPolygon poly = new SpriteBreakerPolygon();
        polygons.Clear();
        polygons.Add( poly );
        poly.edges = new List<Vector2>( 4 );
        poly.edges.Add( new Vector2( 0, 0 ) );
        poly.edges.Add( new Vector2( 1, 0 ) );
        poly.edges.Add( new Vector2( 1, 1 ) );
        poly.edges.Add( new Vector2( 0, 1 ) );
        poly.pivot.Set( 0.5f, 0.5f );
        poly.normals = new Vector3[ 2 ] { Vector3.back, Vector3.back };
        poly.uv = poly.edges.ToArray();
        poly.triangles = new int[ 6 ] { 0, 2, 1, 0, 3, 2 };
    }

    // prints out verbose description of data
    public override string ToString() {
        string ret = "SpriteBreakerData: " + polygons.Count + " poly(s)";
        for ( int i = 0; i < polygons.Count; i++ ) {
            SpriteBreakerPolygon poly = polygons[ i ];
            if ( poly == null )
                ret += "\nPoly " + ( i + 1 ).ToString() + " is NULL! ";
            else {
                ret += "\nPoly " + ( i + 1 ).ToString();
                ret += ( poly.name.Length > 0 ? ( " \"" + poly.name + "\"" ) : "" );
                ret += ( poly.tag.Length > 0 ? ( " [" + poly.name + "]" ) : "" );
                ret += ( Mathf.Abs( poly.floatValue ) > Mathf.Epsilon ? ( " value:" + poly.floatValue ) : "" ) + "\n";

                ret += "\tEdges: " + poly.edges.Count + "\n";
                for ( int j = 0, cnt = poly.edges.Count - 1; j <= cnt; j++ ) {
                    ret += "\t\t" + j + ": " + poly.edges[ j ].ToString( "F1" ) + ( j < cnt ? ", \n" : "\n" );
                }

                if ( poly.vertices != null ) {
                    ret += "\tVertices: " + poly.vertices.Length + "\n";
                    for ( int j = 0, cnt = poly.vertices.Length - 1; j <= cnt; j++ ) {
                        ret += "\t\t" + j + ": " + poly.vertices[ j ].ToString( "F1" );
                        if ( poly.uv != null && poly.uv.Length == poly.vertices.Length ) {
                            ret += " uv:" + poly.uv[ j ].ToString( "F2" );
                        }
                        ret += ( j < cnt ? ", \n" : "\n" );
                    }
                }

                if ( poly.triangles != null ) {
                    ret += "\tTriangles: " + poly.triangles.Length / 3 + "\n";
                    for ( int j = 0, jj = 0, cnt = poly.triangles.Length / 3 - 1; j <= cnt; j++, jj = j * 3 ) {
                        ret += "\t\t" + j + ": " +
                            poly.triangles[ jj ] + " -> " + poly.triangles[ jj + 1 ] + " -> " + poly.triangles[ jj + 2 ]
                            + ( j < cnt ? ", \n" : "\n" );
                    }
                }

            }
        }
        return ret;
    }

    /// <summary>
    /// Clones SpriteBreakerData
    /// </summary>
    /// <returns>new copy</returns>
    public SpriteBreakerData Clone() {
        SpriteBreakerData copy = new SpriteBreakerData();
        for ( int i = 0; i < polygons.Count; i++ ) {
            copy.polygons.Add( polygons[ i ].Clone() );
        }
        return copy;
    }

}