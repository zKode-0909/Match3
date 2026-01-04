using UnityEngine;
using System.Linq;
using System.Collections.Generic;

// utility namespace
namespace SpriteBreakerUtil {

    // utility class used by SpriteShatterData to triangulate polygons
    public class Triangulation2D {

        // main method
        public Triangulation2D( Polygon2D polygon ) {
            PSLG = polygon;
            V = PSLG.Vertices.ToList();
            S = PSLG.Segments.ToList();
            Triangulate( polygon.Vertices.Select( v => v.Coordinate ).ToArray() );
            for ( int i = 0; i < P.Count; i++ ) P[ i ].index = i;
        }

        int FindVertex( Vector2 p, List<Vertex2D> Vertices ) { return Vertices.FindIndex( v => v.Coordinate == p ); }

        int FindSegment( Vertex2D a, Vertex2D b, List<Segment2D> Segments ) { return Segments.FindIndex( s => ( s.a == a && s.b == b ) || ( s.a == b && s.b == a ) ); }

        public Vertex2D CheckAndAddVertex( Vector2 coord ) {
            int idx = FindVertex( coord, P );
            if ( idx < 0 ) {
                var v = new Vertex2D( coord );
                P.Add( v );
                return v;
            }
            return P[ idx ];
        }

        public Segment2D CheckAndAddSegment( Vertex2D a, Vertex2D b ) {
            int idx = FindSegment( a, b, E );
            Segment2D s;
            if ( idx < 0 ) {
                s = new Segment2D( a, b );
                E.Add( s );
            } else {
                s = E[ idx ];
            }
            s.Increment();
            return s;
        }

        public Triangle2D AddTriangle( Vertex2D a, Vertex2D b, Vertex2D c ) {
            Segment2D s0 = CheckAndAddSegment( a, b );
            Segment2D s1 = CheckAndAddSegment( b, c );
            Segment2D s2 = CheckAndAddSegment( c, a );
            var t = new Triangle2D( s0, s1, s2 );
            t = new Triangle2D( s0, s1, s2 );
            T.Add( t );
            return t;
        }

        public void RemoveTriangle( Triangle2D t ) {
            int idx = T.IndexOf( t );
            if ( idx < 0 ) return;

            T.RemoveAt( idx );
            if ( t.s0.Decrement() <= 0 ) RemoveSegment( t.s0 );
            if ( t.s1.Decrement() <= 0 ) RemoveSegment( t.s1 );
            if ( t.s2.Decrement() <= 0 ) RemoveSegment( t.s2 );
        }

        public void RemoveTriangle( Segment2D s ) { T.FindAll( t => t.HasSegment( s ) ).ForEach( t => RemoveTriangle( t ) ); }

        public void RemoveSegment( Segment2D s ) {
            E.Remove( s );
            if ( s.a.ReferenceCount <= 0 ) P.Remove( s.a );
            if ( s.b.ReferenceCount <= 0 ) P.Remove( s.b );
        }

        void Bound( Vector2[] points, out Vector2 min, out Vector2 max ) {
            min = Vector2.one * float.MaxValue;
            max = Vector2.one * float.MinValue;
            for ( int i = 0, n = points.Length; i < n; i++ ) {
                Vector2 p = points[ i ];
                min.x = Mathf.Min( min.x, p.x );
                min.y = Mathf.Min( min.y, p.y );
                max.x = Mathf.Max( max.x, p.x );
                max.y = Mathf.Max( max.y, p.y );
            }
        }

        public Triangle2D AddExternalTriangle( Vector2 min, Vector2 max ) {
            Vector2 center = ( max + min ) * 0.5f;
            float diagonal = ( max - min ).magnitude;
            float dh = diagonal * 0.5f;
            float rdh = Mathf.Sqrt( 3f ) * dh;
            return AddTriangle(
                CheckAndAddVertex( center + new Vector2( -rdh, -dh ) * 3f ),
                CheckAndAddVertex( center + new Vector2( rdh, -dh ) * 3f ),
                CheckAndAddVertex( center + new Vector2( 0f, diagonal ) * 3f )
            );
        }

        void Triangulate( Vector2[] points ) {
            Vector2 max;
            Bound( points, out Vector2 min, out max );
            AddExternalTriangle( min, max );

            for ( int i = 0, n = points.Length; i < n; i++ ) {
                var v = points[ i ];
                UpdateTriangulation( v );
            }

            RemoveExternalPSLG();
        }

        void RemoveExternalPSLG() {
            for ( int i = 0, n = T.Count; i < n; i++ ) {
                Triangle2D t = T[ i ];
                if ( ExternalPSLG( t ) || HasOuterSegments( t ) ) {
                    RemoveTriangle( t );
                    i--; n--;
                }
            }
        }

        bool ContainsSegments( Segment2D s, List<Segment2D> segments ) {
            return segments.FindIndex( s2 =>
                ( s2.a.Coordinate == s.a.Coordinate && s2.b.Coordinate == s.b.Coordinate ) ||
                ( s2.a.Coordinate == s.b.Coordinate && s2.b.Coordinate == s.a.Coordinate )
            ) >= 0;
        }

        bool HasOuterSegments( Triangle2D t ) {
            if ( !ContainsSegments( t.s0, S ) ) return ExternalPSLG( t.s0 );
            if ( !ContainsSegments( t.s1, S ) ) return ExternalPSLG( t.s1 );
            if ( !ContainsSegments( t.s2, S ) ) return ExternalPSLG( t.s2 );
            return false;
        }

        void UpdateTriangulation( Vector2 p ) {
            List<Triangle2D> tmpT = new List<Triangle2D>();
            List<Segment2D> tmpS = new List<Segment2D>();

            Vertex2D v = CheckAndAddVertex( p );
            tmpT = T.FindAll( t => t.ContainsInExternalCircle( v ) );
            tmpT.ForEach( t => {
                tmpS.Add( t.s0 );
                tmpS.Add( t.s1 );
                tmpS.Add( t.s2 );

                AddTriangle( t.a, t.b, v );
                AddTriangle( t.b, t.c, v );
                AddTriangle( t.c, t.a, v );
                RemoveTriangle( t );
            } );

            while ( tmpS.Count != 0 ) {
                Segment2D s = tmpS.Last();
                tmpS.RemoveAt( tmpS.Count - 1 );

                List<Triangle2D> commonT = T.FindAll( t => t.HasSegment( s ) );
                if ( commonT.Count <= 1 ) continue;

                Triangle2D
                abc = commonT[ 0 ],
                abd = commonT[ 1 ];

                if ( abc.Equals( abd ) ) {
                    RemoveTriangle( abc );
                    RemoveTriangle( abd );
                    continue;
                }

                Vertex2D
                a = s.a,
                b = s.b,
                c = abc.ExcludePoint( s ),
                d = abd.ExcludePoint( s );

                Circle2D ec = Circle2D.GetCircumscribedCircle( abc );
                if ( ec.Contains( d.Coordinate ) ) {
                    RemoveTriangle( abc );
                    RemoveTriangle( abd );

                    AddTriangle( a, c, d ); // add acd
                    AddTriangle( b, c, d ); // add bcd

                    Segment2D[] segments0 = abc.ExcludeSegment( s );
                    tmpS.Add( segments0[ 0 ] );
                    tmpS.Add( segments0[ 1 ] );

                    Segment2D[] segments1 = abd.ExcludeSegment( s );
                    tmpS.Add( segments1[ 0 ] );
                    tmpS.Add( segments1[ 1 ] );
                }
            }

        }

        bool ExternalPSLG( Vector2 p ) { return !Utils2D.Contains( p, V ); }

        bool ExternalPSLG( Segment2D s ) { return ExternalPSLG( s.Midpoint() ); }

        bool ExternalPSLG( Triangle2D t ) { return ExternalPSLG( t.a.Coordinate ) || ExternalPSLG( t.b.Coordinate ) || ExternalPSLG( t.c.Coordinate ); }

        // properties
        public Polygon2D Polygon => PSLG;
        public Triangle2D[] Triangles => T.ToArray();
        public List<Segment2D> Edges => E;
        public List<Vertex2D> Points => P;

        protected Polygon2D PSLG;
        protected List<Vertex2D> V = new List<Vertex2D>();
        protected List<Segment2D> S = new List<Segment2D>();
        protected List<Vertex2D> P = new List<Vertex2D>();
        protected List<Segment2D> E = new List<Segment2D>();
        protected List<Triangle2D> T = new List<Triangle2D>();

    }

    public class Vertex2D {

        Vector2 coordinate;
        public int index;
        public int ReferenceCount { get; private set; }
        public Vector2 Coordinate => coordinate;

        public Vertex2D( Vector2 coord ) { coordinate = coord; }

        public int Increment() { return ++ReferenceCount; }

        public int Decrement() { return --ReferenceCount; }
    }

    // used to pass into poly shape into triangulator
    public class Polygon2D {

        List<Vertex2D> vertices;
        List<Segment2D> segments;
        public Vertex2D[] Vertices => vertices.ToArray();
        public Segment2D[] Segments => segments.ToArray();

        // returns polygon hull defined by points (n * log n)
        public static Polygon2D ConvexHull( Vector2[] points ) {
            List<Vector2> ordered = points.ToList().OrderBy( p => p.x ).ToList();
            List<Vector2> upper = new List<Vector2>();
            upper.Add( ordered[ 0 ] ); upper.Add( ordered[ 1 ] );
            for ( int i = 2, n = ordered.Count; i < n; i++ ) {
                upper.Add( ordered[ i ] );
                int l = upper.Count;
                if ( l > 2 ) {
                    Vector2
                    p = upper[ l - 3 ],
                    r = upper[ l - 2 ],
                    q = upper[ l - 1 ];
                    if ( Utils2D.LeftSide( p, q, r ) ) upper.RemoveAt( l - 2 );
                }
            }

            List<Vector2> lower = new List<Vector2>();
            lower.Add( ordered[ ordered.Count - 1 ] );
            lower.Add( ordered[ ordered.Count - 2 ] );
            for ( int i = ordered.Count - 3; i >= 0; i-- ) {
                lower.Add( ordered[ i ] );
                int l = lower.Count;
                if ( l > 2 ) {
                    Vector2
                    p = lower[ l - 3 ],
                    r = lower[ l - 2 ],
                    q = lower[ l - 1 ];
                    if ( Utils2D.LeftSide( p, q, r ) ) lower.RemoveAt( l - 2 );
                }
            }

            lower.RemoveAt( lower.Count - 1 );
            lower.RemoveAt( 0 );
            upper.AddRange( lower );
            return new Polygon2D( upper.ToArray() );
        }

        public static Polygon2D Contour( Vector2[] points ) {
            var n = points.Length;
            var edges = new List<HalfEdge2D>();
            for ( int i = 0; i < n; i++ ) edges.Add( new HalfEdge2D( points[ i ] ) );
            for ( int i = 0; i < n; i++ ) {
                var e = edges[ i ];
                e.from = edges[ ( i == 0 ) ? ( n - 1 ) : ( i - 1 ) ];
                e.to = edges[ ( i + 1 ) % n ];
            }
            edges = SplitEdges( edges );

            List<Vector2> result = new List<Vector2>();
            HalfEdge2D start = edges[ 0 ];
            result.Add( start.p );

            HalfEdge2D current = start;
            while ( true ) {
                HalfEdge2D from = current, to = current.to;
                HalfEdge2D from2 = to.to, to2 = from2.to;

                bool flag = false;

                while ( from2 != start && to2 != from ) {
                    if ( flag = Utils2D.Intersect( from.p, to.p, from2.p, to2.p ) ) break;
                    from2 = to2;
                    to2 = to2.to;
                }

                if ( !flag ) {
                    result.Add( to.p );
                    current = to;

                } else {
                    result.Add( from2.p );

                    // reconnect
                    from.to = from2;
                    from2.to = from;
                    to.from = to2;
                    to.Invert();
                    to2.from = to;

                    HalfEdge2D e = from2;
                    while ( e != to ) {
                        e.Invert();
                        e = e.to;
                    }

                    current = from2;
                }

                if ( current == start ) break;
            }

            result.RemoveAt( result.Count - 1 ); // remove last
            return new Polygon2D( result.ToArray() );
        }

        // Disable to intersect more than two edges
        static List<HalfEdge2D> SplitEdges( List<HalfEdge2D> edges ) {
            HalfEdge2D start = edges[ 0 ];
            HalfEdge2D cur = start;

            while ( true ) {
                HalfEdge2D from = cur, to = from.to;
                HalfEdge2D from2 = to.to, to2 = from2.to;
                int intersections = 0;
                while ( to2 != from.from ) {
                    if ( Utils2D.Intersect( from.p, to.p, from2.p, to2.p ) ) {
                        intersections++;
                        if ( intersections >= 2 ) break;
                    }
                    // next
                    from2 = from2.to;
                    to2 = to2.to;
                }

                if ( intersections >= 2 ) {
                    edges.Add( cur.Split() );
                } else {
                    // next
                    cur = cur.to;
                    if ( cur == start ) break;
                }
            }

            return edges;
        }

        // contour must be counter clockwise
        public Polygon2D( Vector2[] contour ) {
            vertices = contour.Select( p => new Vertex2D( p ) ).ToList();
            segments = new List<Segment2D>();
            for ( int i = 0, n = vertices.Count; i < n; i++ ) {
                Vertex2D v0 = vertices[ i ];
                Vertex2D v1 = vertices[ ( i + 1 ) % n ];
                segments.Add( new Segment2D( v0, v1 ) );
            }
        }

        public Polygon2D( List<Vector2> contour ) {
            vertices = contour.Select( p => new Vertex2D( p ) ).ToList();
            segments = new List<Segment2D>();
            for ( int i = 0, n = vertices.Count; i < n; i++ ) {
                Vertex2D v0 = vertices[ i ];
                Vertex2D v1 = vertices[ ( i + 1 ) % n ];
                segments.Add( new Segment2D( v0, v1 ) );
            }
        }

        // returns true if poly contains point
        public bool Contains( Vector2 p ) { return Utils2D.Contains( p, vertices ); }

    }

    public class Segment2D {
        float length;
        public int ReferenceCount { get; private set; }
        public Vertex2D a, b;

        public Segment2D( Vertex2D a, Vertex2D b ) { this.a = a; this.b = b; }

        public Vector2 Midpoint() { return ( a.Coordinate + b.Coordinate ) * 0.5f; }

        // segment length
        public float Length() {
            if ( length <= 0f ) length = ( a.Coordinate - b.Coordinate ).magnitude;
            return length;
        }

        // returns true if point is in within diametral circle of segment 
        public bool EncroachedUpon( Vector2 p ) {
            if ( p == a.Coordinate || p == b.Coordinate ) return false;
            float radius = ( a.Coordinate - b.Coordinate ).magnitude * 0.5f;
            return ( Midpoint() - p ).magnitude < radius;
        }

        // returns true if point is on this segment
        public bool On( Vector2 p ) {
            if ( HasPoint( p ) ) return true;
            if ( Distance( p ) > Mathf.Epsilon ) return false;
            Vector2 p0 = a.Coordinate, p1 = b.Coordinate;
            bool
            bx = ( p0.x < p1.x ) ? ( p0.x <= p.x && p.x <= p1.x ) : ( p1.x <= p.x && p.x <= p0.x ),
            by = ( p0.y < p1.y ) ? ( p0.y <= p.y && p.y <= p1.y ) : ( p1.y <= p.y && p.y <= p0.y );
            return bx && by;
        }

        // distance from point to a line
        public float Distance( Vector2 p ) {
            Vector2 p0 = a.Coordinate, p1 = b.Coordinate;
            float dx = ( p1.x - p0.x ), dy = ( p1.y - p0.y );
            return Mathf.Abs( ( dy * p.x ) - ( dx * p.y ) + ( p1.x * p0.y ) - ( p1.y * p0.x ) ) / Mathf.Sqrt( dy * dy + dx * dx );
        }

        public bool HasPoint( Vertex2D v ) { return ( a == v ) || ( b == v ); }

        public bool HasPoint( Vector2 p ) { return ( a.Coordinate == p ) || ( b.Coordinate == p ); }

        public int Increment() { a.Increment(); b.Increment(); return ++ReferenceCount; }

        public int Decrement() { a.Decrement(); b.Decrement(); return --ReferenceCount; }

    }

    public class Triangle2D {

        public Vertex2D a, b, c;
        public Segment2D s0, s1, s2;
        private Circle2D circum;

        public Triangle2D( Segment2D seg0, Segment2D seg1, Segment2D seg2 ) {
            s0 = seg0;
            s1 = seg1;
            s2 = seg2;
            a = seg0.a;
            b = seg0.b;
            c = ( seg2.b == a || seg2.b == b ) ? seg2.a : seg2.b;
        }

        public bool HasPoint( Vertex2D p ) { return ( a == p ) || ( b == p ) || ( c == p ); }

        public bool HasCommonPoint( Triangle2D t ) { return HasPoint( t.a ) || HasPoint( t.b ) || HasPoint( t.c ); }

        public bool HasSegment( Segment2D s ) { return ( s0 == s ) || ( s1 == s ) || ( s2 == s ); }

        public bool HasSegment( Vector2 a, Vector2 b ) { return ( s0.HasPoint( a ) && s0.HasPoint( b ) ) || ( s1.HasPoint( a ) && s1.HasPoint( b ) ) || ( s2.HasPoint( a ) && s2.HasPoint( b ) ); }

        public Vertex2D ExcludePoint( Segment2D s ) {
            if ( !s.HasPoint( a ) ) return a;
            if ( !s.HasPoint( b ) ) return b;
            return c;
        }

        public Vertex2D ExcludePoint( Vector2 p0, Vector2 p1 ) {
            if ( p0 != a.Coordinate && p1 != a.Coordinate ) return a;
            if ( p0 != b.Coordinate && p1 != b.Coordinate ) return b;
            return c;
        }

        public Vertex2D[] ExcludePoint( Vector2 p ) {
            if ( p == a.Coordinate ) return new Vertex2D[] { b, c };
            if ( p == b.Coordinate ) return new Vertex2D[] { a, c };
            return new Vertex2D[] { a, b };
        }

        public Segment2D[] ExcludeSegment( Segment2D s ) {
            if ( s0.Equals( s ) ) {
                return new Segment2D[] { s1, s2 };
            } else if ( s1.Equals( s ) ) {
                return new Segment2D[] { s0, s2 };
            }
            return new Segment2D[] { s0, s1 };
        }

        public Segment2D CommonSegment( Vertex2D v0, Vertex2D v1 ) {
            if ( s0.HasPoint( v0 ) && s0.HasPoint( v1 ) ) {
                return s0;
            } else if ( s1.HasPoint( v0 ) && s1.HasPoint( v1 ) ) {
                return s1;
            }
            return s2;
        }

        public Segment2D[] CommonSegments( Vertex2D v ) {
            if ( s0.HasPoint( v ) && s1.HasPoint( v ) ) {
                return new[] { s0, s1 };
            } else if ( s1.HasPoint( v ) && s2.HasPoint( v ) ) {
                return new[] { s1, s2 };
            }
            return new[] { s0, s2 };
        }

        public Vector2 Circumcenter() {
            if ( circum == null ) {
                circum = Circle2D.GetCircumscribedCircle( this );
            }
            return circum.center;
        }

        public bool ContainsInExternalCircle( Vertex2D v ) {
            if ( circum == null ) {
                circum = Circle2D.GetCircumscribedCircle( this );
            }
            return circum.Contains( v.Coordinate );
        }

        float Angle( Vector2 from, Vector2 to0, Vector2 to1 ) {
            var v0 = ( to0 - from );
            var v1 = ( to1 - from );
            float acos = Mathf.Acos( Vector2.Dot( v0, v1 ) / Mathf.Sqrt( v0.sqrMagnitude * v1.sqrMagnitude ) );
            return acos;
        }

        // angle is in radians
        public bool Skinny( float angle, float threshold ) {
            if ( s0.Length() <= threshold && s1.Length() <= threshold && s2.Length() <= threshold ) return false;
            if ( Angle( a.Coordinate, b.Coordinate, c.Coordinate ) < angle ) return true;
            if ( Angle( b.Coordinate, a.Coordinate, c.Coordinate ) < angle ) return true;
            if ( Angle( c.Coordinate, a.Coordinate, b.Coordinate ) < angle ) return true;
            return false;
        }

        public bool Equals( Triangle2D t ) { return HasPoint( t.a ) && HasPoint( t.b ) && HasPoint( t.c ); }

    }

    public class HalfEdge2D {
        public Vector2 p;
        public HalfEdge2D from, to;
        public HalfEdge2D( Vector2 pp ) { p = pp; }

        public void Invert() { var tmp = from; from = to; to = tmp; }

        public HalfEdge2D Split() {
            Vector2 m = ( to.p + p ) * 0.5f;
            HalfEdge2D e = new HalfEdge2D( m );
            to.from = e; e.to = to;
            to = e; e.from = this;
            return e;
        }
    }

    public class Circle2D {
        public Vector2 center;
        public float radius;

        public Circle2D( Vector2 c, float r ) { center = c; radius = r; }

        public bool Contains( Vector2 p ) { return ( p - center ).magnitude < radius; }

        public static Circle2D GetCircumscribedCircle( Triangle2D triangle ) {
            float
            x1 = triangle.a.Coordinate.x,
            y1 = triangle.a.Coordinate.y,
            x2 = triangle.b.Coordinate.x,
            y2 = triangle.b.Coordinate.y,
            x3 = triangle.c.Coordinate.x,
            y3 = triangle.c.Coordinate.y,

            x1_2 = x1 * x1,
            x2_2 = x2 * x2,
            x3_2 = x3 * x3,
            y1_2 = y1 * y1,
            y2_2 = y2 * y2,
            y3_2 = y3 * y3,

            c = 2f * ( ( x2 - x1 ) * ( y3 - y1 ) - ( y2 - y1 ) * ( x3 - x1 ) ),
            x = ( ( y3 - y1 ) * ( x2_2 - x1_2 + y2_2 - y1_2 ) + ( y1 - y2 ) * ( x3_2 - x1_2 + y3_2 - y1_2 ) ) / c,
            y = ( ( x1 - x3 ) * ( x2_2 - x1_2 + y2_2 - y1_2 ) + ( x2 - x1 ) * ( x3_2 - x1_2 + y3_2 - y1_2 ) ) / c,
            _x = ( x1 - x ),
            _y = ( y1 - y ),

            r = Mathf.Sqrt( ( _x * _x ) + ( _y * _y ) );
            return new Circle2D( new Vector2( x, y ), r );
        }

    }

    public class Utils2D {

        // constrain a distance between two points to "threshold" length
        public static List<Vector2> Constrain( List<Vector2> points, float threshold = 1f ) {
            List<Vector2> result = new List<Vector2>();
            int n = points.Count;
            for ( int i = 0, j = 1; i < n && j < n; j++ ) {
                Vector2 from = points[ i ], to = points[ j ];
                if ( Vector2.Distance( from, to ) > threshold ) {
                    result.Add( from );
                    i = j;
                }
            }

            Vector2 p0 = result.Last(), p1 = result.First();
            if ( Vector2.Distance( p0, p1 ) > threshold ) result.Add( ( p0 + p1 ) * 0.5f );
            return result;
        }

        // check intersection segment (p0, p1) to segment (p2, p3)
        public static bool Intersect( Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3 ) {
            Vector2
            s1 = p1 - p0,
            s2 = p3 - p2;
            float
            s = ( -s1.y * ( p0.x - p2.x ) + s1.x * ( p0.y - p2.y ) ) / ( -s2.x * s1.y + s1.x * s2.y ),
            t = ( s2.x * ( p0.y - p2.y ) - s2.y * ( p0.x - p2.x ) ) / ( -s2.x * s1.y + s1.x * s2.y );
            return ( s >= 0 && s <= 1 && t >= 0 && t <= 1 );
        }

        // returns true if p is in polygon defined by vertices
        public static bool Contains( Vector2 p, List<Vertex2D> vertices ) {
            int n = vertices.Count;
            bool c = false;
            for ( int i = 0, j = n - 1; i < n; j = i++ ) {
                if ( vertices[ i ].Coordinate == p ) return true;
                if ( ( ( vertices[ i ].Coordinate.y > p.y ) != ( vertices[ j ].Coordinate.y > p.y ) ) &&
                    ( p.x < ( vertices[ j ].Coordinate.x - vertices[ i ].Coordinate.x ) * ( p.y - vertices[ i ].Coordinate.y ) / ( vertices[ j ].Coordinate.y - vertices[ i ].Coordinate.y ) + vertices[ i ].Coordinate.x ) ) {
                    c = !c;
                }
            }
            return c;
        }

        public static bool LeftSide( Vector2 from, Vector2 to, Vector2 p ) { return ( ( ( to.x - from.x ) * ( p.y - from.y ) - ( to.y - from.y ) * ( p.x - from.x ) ) > 0f ); }

        public static bool CheckEqual( Vertex2D v0, Vertex2D v1 ) { return ( v0.Coordinate == v1.Coordinate ); }

        public static bool CheckEqual( Segment2D s0, Segment2D s1 ) { return ( CheckEqual( s0.a, s1.a ) && CheckEqual( s0.b, s1.b ) ) || ( CheckEqual( s0.a, s1.b ) && CheckEqual( s0.b, s1.a ) ); }

        public static bool CheckEqual( Triangle2D t0, Triangle2D t1 ) {
            return
                ( CheckEqual( t0.s0, t1.s0 ) && CheckEqual( t0.s1, t1.s1 ) && CheckEqual( t0.s2, t1.s2 ) ) ||
                ( CheckEqual( t0.s0, t1.s0 ) && CheckEqual( t0.s1, t1.s2 ) && CheckEqual( t0.s2, t1.s1 ) ) ||
                ( CheckEqual( t0.s0, t1.s1 ) && CheckEqual( t0.s1, t1.s0 ) && CheckEqual( t0.s2, t1.s2 ) ) ||
                ( CheckEqual( t0.s0, t1.s1 ) && CheckEqual( t0.s1, t1.s2 ) && CheckEqual( t0.s2, t1.s0 ) ) ||
                ( CheckEqual( t0.s0, t1.s2 ) && CheckEqual( t0.s1, t1.s0 ) && CheckEqual( t0.s2, t1.s1 ) ) ||
                ( CheckEqual( t0.s0, t1.s2 ) && CheckEqual( t0.s1, t1.s1 ) && CheckEqual( t0.s2, t1.s0 ) );
        }
    }
}
