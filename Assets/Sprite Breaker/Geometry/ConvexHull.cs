using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

// utility namespace
namespace SpriteBreakerUtil {

    // utility class used by SpriteShatter to compute convex hull around polygons, to generate convex collision shape for Collider3D
    public class ConvexHullCalculator {

        // main method
        public void GenerateHull( Vector3[] points, ref List<Vector3> verts, ref List<int> tris ) {
            if ( points.Length < 4 ) return;
            Initialize( points );
            GenerateInitialHull( points );
            while ( openSetTail >= 0 ) { GrowHull( points ); }
            foreach ( var face in faces.Values ) {
                int vi0, vi1, vi2;
                vi0 = verts.Count; verts.Add( points[ face.Vertex0 ] );
                vi1 = verts.Count; verts.Add( points[ face.Vertex1 ] );
                vi2 = verts.Count; verts.Add( points[ face.Vertex2 ] );
                tris.Add( vi0 );
                tris.Add( vi1 );
                tris.Add( vi2 );
            }
        }

        void Initialize( Vector3[] points ) {
            faceCount = 0;
            openSetTail = -1;
            if ( faces == null ) {
                faces = new Dictionary<int, Face>();
                litFaces = new HashSet<int>();
                horizon = new List<HorizonEdge>();
                openSet = new List<PointFace>( points.Length );
            } else {
                faces.Clear();
                litFaces.Clear();
                horizon.Clear();
                openSet.Clear();
                if ( openSet.Capacity < points.Length ) openSet.Capacity = points.Length;
            }
        }

        void GenerateInitialHull( Vector3[] points ) {
            int b0, b1, b2, b3;
            FindInitialHullIndices( points, out b0, out b1, out b2, out b3 );

            Vector3
            v0 = points[ b0 ],
            v1 = points[ b1 ],
            v2 = points[ b2 ],
            v3 = points[ b3 ];

            faceCount = 0;
            if ( Vector3.Dot( v3 - v1, Vector3.Cross( v1 - v0, v2 - v0 ) ) > 0.0f ) {
                faces[ faceCount++ ] = new Face( b0, b2, b1, 3, 1, 2, Normal( points[ b0 ], points[ b2 ], points[ b1 ] ) );
                faces[ faceCount++ ] = new Face( b0, b1, b3, 3, 2, 0, Normal( points[ b0 ], points[ b1 ], points[ b3 ] ) );
                faces[ faceCount++ ] = new Face( b0, b3, b2, 3, 0, 1, Normal( points[ b0 ], points[ b3 ], points[ b2 ] ) );
                faces[ faceCount++ ] = new Face( b1, b2, b3, 2, 1, 0, Normal( points[ b1 ], points[ b2 ], points[ b3 ] ) );
            } else {
                faces[ faceCount++ ] = new Face( b0, b1, b2, 3, 2, 1, Normal( points[ b0 ], points[ b1 ], points[ b2 ] ) );
                faces[ faceCount++ ] = new Face( b0, b3, b1, 3, 0, 2, Normal( points[ b0 ], points[ b3 ], points[ b1 ] ) );
                faces[ faceCount++ ] = new Face( b0, b2, b3, 3, 1, 0, Normal( points[ b0 ], points[ b2 ], points[ b3 ] ) );
                faces[ faceCount++ ] = new Face( b1, b3, b2, 2, 0, 1, Normal( points[ b1 ], points[ b3 ], points[ b2 ] ) );
            }

            // Create the openSet. Add all points except the points of the seed hull.
            for ( int i = 0; i < points.Length; i++ ) {
                if ( i == b0 || i == b1 || i == b2 || i == b3 ) continue;
                openSet.Add( new PointFace( i, UNASSIGNED, 0.0f ) );
            }

            // Add the seed hull verts to the tail of the list.
            openSet.Add( new PointFace( b0, INSIDE, float.NaN ) );
            openSet.Add( new PointFace( b1, INSIDE, float.NaN ) );
            openSet.Add( new PointFace( b2, INSIDE, float.NaN ) );
            openSet.Add( new PointFace( b3, INSIDE, float.NaN ) );

            // Set the openSetTail value
            openSetTail = openSet.Count - 5;

            // Assign all points of the open set. 
            for ( int i = 0; i <= openSetTail; i++ ) {
                var assigned = false;
                var fp = openSet[ i ];

                for ( int j = 0; j < 4; j++ ) {
                    var face = faces[ j ];
                    var dist = PointFaceDistance( points[ fp.Point ], points[ face.Vertex0 ], face );

                    if ( dist > 0 ) {
                        fp.Face = j;
                        fp.Distance = dist;
                        openSet[ i ] = fp;

                        assigned = true;
                        break;
                    }
                }

                if ( !assigned ) {
                    fp.Face = INSIDE;
                    fp.Distance = float.NaN;
                    openSet[ i ] = openSet[ openSetTail ];
                    openSet[ openSetTail ] = fp;
                    openSetTail -= 1;
                    i -= 1;
                }
            }

        }

        // Finds four points in the point cloud that are not coplanar for the seed hull
        void FindInitialHullIndices( Vector3[] points, out int b0, out int b1, out int b2, out int b3 ) {
            var count = points.Length;

            for ( int i0 = 0; i0 < count - 3; i0++ ) {
                for ( int i1 = i0 + 1; i1 < count - 2; i1++ ) {
                    var p0 = points[ i0 ];
                    var p1 = points[ i1 ];
                    if ( AreCoincident( p0, p1 ) ) continue;

                    for ( int i2 = i1 + 1; i2 < count - 1; i2++ ) {
                        var p2 = points[ i2 ];

                        if ( AreCollinear( p0, p1, p2 ) ) continue;

                        for ( int i3 = i2 + 1; i3 < count - 0; i3++ ) {
                            var p3 = points[ i3 ];

                            if ( AreCoplanar( p0, p1, p2, p3 ) ) continue;

                            b0 = i0;
                            b1 = i1;
                            b2 = i2;
                            b3 = i3;
                            return;
                        }
                    }
                }
            }

            b0 = b1 = b2 = b3 = -1;
            Debug.LogError( "Hull generation failed" );
        }

        void GrowHull( Vector3[] points ) {

            // Find farthest point and first lit face.
            var farthestPoint = 0;
            var dist = openSet[ 0 ].Distance;

            for ( int i = 1; i <= openSetTail; i++ ) {
                if ( openSet[ i ].Distance > dist ) {
                    farthestPoint = i;
                    dist = openSet[ i ].Distance;
                }
            }

            // Use lit face to find horizon and the rest of the lit faces.
            FindHorizon(
                points,
                points[ openSet[ farthestPoint ].Point ],
                openSet[ farthestPoint ].Face,
                faces[ openSet[ farthestPoint ].Face ] );

            // Construct new cone from horizon
            ConstructCone( points, openSet[ farthestPoint ].Point );

            // Reassign points
            ReassignPoints( points );
        }

        void FindHorizon( Vector3[] points, Vector3 point, int fi, Face face ) {
            litFaces.Clear();
            horizon.Clear();
            litFaces.Add( fi );
            var oppositeFace = faces[ face.Opposite0 ];
            var dist = PointFaceDistance( point, points[ oppositeFace.Vertex0 ], oppositeFace );

            if ( dist <= 0.0f ) {
                horizon.Add( new HorizonEdge { Face = face.Opposite0, Edge0 = face.Vertex1, Edge1 = face.Vertex2 } );
            } else {
                SearchHorizon( points, point, fi, face.Opposite0, oppositeFace );
            }

            if ( !litFaces.Contains( face.Opposite1 ) ) {
                oppositeFace = faces[ face.Opposite1 ];
                dist = PointFaceDistance( point, points[ oppositeFace.Vertex0 ], oppositeFace );

                if ( dist <= 0.0f ) {
                    horizon.Add( new HorizonEdge {
                        Face = face.Opposite1,
                        Edge0 = face.Vertex2,
                        Edge1 = face.Vertex0,
                    } );
                } else {
                    SearchHorizon( points, point, fi, face.Opposite1, oppositeFace );
                }
            }

            if ( !litFaces.Contains( face.Opposite2 ) ) {
                oppositeFace = faces[ face.Opposite2 ];
                dist = PointFaceDistance(
                    point,
                    points[ oppositeFace.Vertex0 ],
                    oppositeFace );

                if ( dist <= 0.0f ) {
                    horizon.Add( new HorizonEdge {
                        Face = face.Opposite2,
                        Edge0 = face.Vertex0,
                        Edge1 = face.Vertex1,
                    } );
                } else {
                    SearchHorizon( points, point, fi, face.Opposite2, oppositeFace );
                }
            }
        }

        // Recursively search to find the horizon or lit set.
        void SearchHorizon( Vector3[] points, Vector3 point, int prevFaceIndex, int numFaces, Face face ) {

            litFaces.Add( numFaces );
            int nextFaceIndex0;
            int nextFaceIndex1;
            int edge0;
            int edge1;
            int edge2;

            if ( prevFaceIndex == face.Opposite0 ) {
                nextFaceIndex0 = face.Opposite1;
                nextFaceIndex1 = face.Opposite2;

                edge0 = face.Vertex2;
                edge1 = face.Vertex0;
                edge2 = face.Vertex1;
            } else if ( prevFaceIndex == face.Opposite1 ) {
                nextFaceIndex0 = face.Opposite2;
                nextFaceIndex1 = face.Opposite0;

                edge0 = face.Vertex0;
                edge1 = face.Vertex1;
                edge2 = face.Vertex2;
            } else {
                nextFaceIndex0 = face.Opposite0;
                nextFaceIndex1 = face.Opposite1;

                edge0 = face.Vertex1;
                edge1 = face.Vertex2;
                edge2 = face.Vertex0;
            }

            if ( !litFaces.Contains( nextFaceIndex0 ) ) {
                var oppositeFace = faces[ nextFaceIndex0 ];

                var dist = PointFaceDistance(
                    point,
                    points[ oppositeFace.Vertex0 ],
                    oppositeFace );

                if ( dist <= 0.0f ) {
                    horizon.Add( new HorizonEdge {
                        Face = nextFaceIndex0,
                        Edge0 = edge0,
                        Edge1 = edge1,
                    } );
                } else {
                    SearchHorizon( points, point, numFaces, nextFaceIndex0, oppositeFace );
                }
            }

            if ( !litFaces.Contains( nextFaceIndex1 ) ) {
                var oppositeFace = faces[ nextFaceIndex1 ];

                var dist = PointFaceDistance(
                    point,
                    points[ oppositeFace.Vertex0 ],
                    oppositeFace );

                if ( dist <= 0.0f ) {
                    horizon.Add( new HorizonEdge {
                        Face = nextFaceIndex1,
                        Edge0 = edge1,
                        Edge1 = edge2,
                    } );
                } else {
                    SearchHorizon( points, point, numFaces, nextFaceIndex1, oppositeFace );
                }
            }
        }

        void ConstructCone( Vector3[] points, int farthestPoint ) {
            foreach ( var fi in litFaces ) faces.Remove( fi );
            var firstNewFace = faceCount;

            for ( int i = 0; i < horizon.Count; i++ ) {
                var v0 = farthestPoint;
                var v1 = horizon[ i ].Edge0;
                var v2 = horizon[ i ].Edge1;
                var o0 = horizon[ i ].Face;
                var o1 = ( i == horizon.Count - 1 ) ? firstNewFace : firstNewFace + i + 1;
                var o2 = ( i == 0 ) ? ( firstNewFace + horizon.Count - 1 ) : firstNewFace + i - 1;

                var fi = faceCount++;

                faces[ fi ] = new Face( v0, v1, v2, o0, o1, o2, Normal( points[ v0 ], points[ v1 ], points[ v2 ] ) );
                var horizonFace = faces[ horizon[ i ].Face ];

                if ( horizonFace.Vertex0 == v1 ) {
                    horizonFace.Opposite1 = fi;
                } else if ( horizonFace.Vertex1 == v1 ) {
                    horizonFace.Opposite2 = fi;
                } else {
                    horizonFace.Opposite0 = fi;
                }

                faces[ horizon[ i ].Face ] = horizonFace;
            }
        }

        void ReassignPoints( Vector3[] points ) {
            for ( int i = 0; i <= openSetTail; i++ ) {
                var fp = openSet[ i ];

                if ( litFaces.Contains( fp.Face ) ) {
                    var assigned = false;
                    var point = points[ fp.Point ];

                    foreach ( var kvp in faces ) {
                        var fi = kvp.Key;
                        var face = kvp.Value;

                        var dist = PointFaceDistance( point, points[ face.Vertex0 ], face );

                        if ( dist > Mathf.Epsilon ) {
                            assigned = true;
                            fp.Face = fi;
                            fp.Distance = dist;
                            openSet[ i ] = fp;
                            break;
                        }
                    }

                    if ( !assigned ) {
                        fp.Face = INSIDE;
                        fp.Distance = float.NaN;
                        openSet[ i ] = openSet[ openSetTail ];
                        openSet[ openSetTail ] = fp;
                        i--; openSetTail--;
                    }
                }
            }
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        float PointFaceDistance( Vector3 point, Vector3 pointOnFace, Face face ) {
            return Vector3.Dot( face.Normal, point - pointOnFace );
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        Vector3 Normal( Vector3 v0, Vector3 v1, Vector3 v2 ) {
            return Vector3.Cross( v1 - v0, v2 - v0 ).normalized;
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        bool AreCoincident( Vector3 a, Vector3 b ) {
            return ( a - b ).magnitude <= Mathf.Epsilon;
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        bool AreCollinear( Vector3 a, Vector3 b, Vector3 c ) {
            return Vector3.Cross( c - a, c - b ).magnitude <= Mathf.Epsilon;
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        bool AreCoplanar( Vector3 a, Vector3 b, Vector3 c, Vector3 d ) {
            var n1 = Vector3.Cross( c - a, c - b );
            var n2 = Vector3.Cross( d - a, d - b );
            return AreCollinear( Vector3.zero, n1, n2 );
        }

        struct Face {
            public int Vertex0;
            public int Vertex1;
            public int Vertex2;
            public int Opposite0;
            public int Opposite1;
            public int Opposite2;
            public Vector3 Normal;

            public Face( int v0, int v1, int v2, int o0, int o1, int o2, Vector3 normal ) {
                Vertex0 = v0;
                Vertex1 = v1;
                Vertex2 = v2;
                Opposite0 = o0;
                Opposite1 = o1;
                Opposite2 = o2;
                Normal = normal;
            }

            public bool Equals( Face other ) {
                return ( this.Vertex0 == other.Vertex0 )
                    && ( this.Vertex1 == other.Vertex1 )
                    && ( this.Vertex2 == other.Vertex2 )
                    && ( this.Opposite0 == other.Opposite0 )
                    && ( this.Opposite1 == other.Opposite1 )
                    && ( this.Opposite2 == other.Opposite2 )
                    && ( this.Normal == other.Normal );
            }
        }

        struct PointFace {
            public int Point;
            public int Face;
            public float Distance;

            public PointFace( int p, int f, float d ) { Point = p; Face = f; Distance = d; }
        }

        struct HorizonEdge {
            public int Face;
            public int Edge0;
            public int Edge1;
        }

        Dictionary<int, Face> faces;
        List<PointFace> openSet;
        HashSet<int> litFaces;
        List<HorizonEdge> horizon;
        int openSetTail = -1;
        int faceCount = 0;

        const int UNASSIGNED = -2; // point not assigned to a face yet
        const int INSIDE = -1; // points is inside convex hull

        public static readonly ConvexHullCalculator instance = new ConvexHullCalculator();

    }
}