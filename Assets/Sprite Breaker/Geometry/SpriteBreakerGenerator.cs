using System;
using System.Collections.Generic;
using UnityEngine;


// utility namespace
namespace SpriteBreakerUtil {

    // utility class for generating shards
    public class SpriteBreakerGenerator {

        // main method
        public void Generate( SpriteBreakerData data, SpriteBreaker breaker, bool triangulate=false ) {

            Epsilon.eps = Mathf.Pow( 10, -breaker.csgEpsilon );
            switch ( breaker.generateType ) {
            case SpriteBreaker.BreakerGenerateType.Radial:
                GenerateRadial( data, breaker, triangulate ); break;


            }
        }

        void GenerateRadial( SpriteBreakerData data, SpriteBreaker breaker, bool triangulate ) {

            // set up scaling
            Vector2 scale = new Vector2( 1, 1 );
            Sprite sprite = breaker.currentSprite;
            if ( sprite != null ) {
                if ( sprite.rect.width > sprite.rect.height ) {
                    scale.x = sprite.rect.height / sprite.rect.width;
                } else {
                    scale.y = sprite.rect.width / sprite.rect.height;
                }
            }

            // subject polygons set up
            PolyBool pb = PolyBool.instance;
            List<SegmentList> intermediate = new List<SegmentList>(), altIntermediate = new List<SegmentList>(), swapSegList,
                            subject = new List<SegmentList>();
            for ( int i = 0, np = data.polygons.Count; i < np; i++ ) {
                if ( data.polygons[ i ] == null || data.polygons[ i ].edges.Count == 0 ) continue;
                Polygon p = new Polygon() { regions = new List<List<Vector2>>() };
                p.regions.Add( data.polygons[ i ].edges );
                SegmentList ps = pb.segments( p, breaker.csgUpscale );
                intermediate.Add( ps );
                subject.Add( ps.Clone() );
            }

            // origin (clamp)
            Vector2 center = new Vector2( Mathf.Clamp( breaker.generateOrigin.x, -1, 2 ), Mathf.Clamp( breaker.generateOrigin.y, -1, 2 ) );

            // randomness
            if ( breaker.generateSeed != 0 ) UnityEngine.Random.InitState( breaker.generateSeed );
            float generateRandomness = breaker.generateRandomness;

            // start angle
            float curAngle = Mathf.PI * breaker.generateAngle / 180;

            // spacing between cross cuts ( 2 - 24 )
            int numCuts = Mathf.RoundToInt( 2 + 20 * Mathf.Clamp01( 0.01f * _RandomizeParameterDelta( 100 - breaker.generateSpacing, 10, 10, generateRandomness ) ) );
            float cutStep = Mathf.PI / numCuts;

            // cutting poly setup
            Vector2[] cutLine = new Vector2[ 4 + Mathf.CeilToInt( 2 * ( 2 + 0.25f * breaker.generateFrequency ) ) ];
            Polygon cutter = new Polygon();
            cutter.regions = new List<List<Vector2>>();
            List<Vector2> cutterRegion = new List<Vector2>();
            SegmentList cutterSegments;
            cutter.regions.Add( cutterRegion );           

            // radius of radial cuts from center
            float maxDistance = Mathf.Max( center.magnitude, ( center - Vector2.left ).magnitude, ( center - Vector2.up ).magnitude, ( center - Vector2.one ).magnitude ) / Mathf.Min( scale.x, scale.y );

            // make radial cuts across circle center
            for ( int cut = 0; cut < numCuts; cut++ ) {
                try {

                    // zigs are number of times line changes direction from center to outer edge
                    int numZigs = Math.Min( 2 + Mathf.FloorToInt( 20 * Mathf.Clamp01( _RandomizeParameterDelta( 0.01f * breaker.generateFrequency, 0.002f * breaker.generateFrequency, generateRandomness ) ) ),
                        ( cutLine.Length - 3 ) / 2 ); // clip to max points in cutLine
                    int numPoints = 3 + numZigs * 2;

                    // zig step distance each time
                    float zigStep = maxDistance / numZigs, zigDistance = (float) zigStep / numZigs;
                    float cos = Mathf.Cos( curAngle ), sin = Mathf.Sin( curAngle );

                    // direction 
                    Vector2 dir = new Vector2( cos, sin ), perp = Vector2.Perpendicular( dir ) * scale, wobble;
                    float dirDeviation = cutStep * 0.5f;
                    dir *= scale;

                    // create a cut
                    Vector2 leftPoint = center, rightPoint = center;
                    cutLine[ numZigs + 1 ] = center;
                    float positionFromCenter = 0;
                    for ( int zig = 0, lastZig = numZigs - 1; zig <= lastZig; zig++ ) {
                        positionFromCenter = (float) zig / numZigs; // zig->lastSZig, positionFormCenter->1

                        // if last zig
                        if ( zig == lastZig ) {
                            // snap to outer edge
                            leftPoint = center + dir * maxDistance;
                            rightPoint = center - dir * maxDistance;
                        } else {
                            // wobble prevents CSG from merging points one a straight line
                            wobble = perp * ( zig % 2 - 0.5f ) * 0.1f / numCuts;

                            // points radiate away from center
                            leftPoint = _RandomizeMoveInDirection( leftPoint + wobble, center, zigStep * ( 0.7f + 1f * positionFromCenter ), dir, 0.3f, Mathf.PI * 0.2f, cutStep * 0.3f, breaker.generateRandomness );
                            rightPoint = _RandomizeMoveInDirection( rightPoint + wobble, center, zigStep * ( 0.7f + 1f * positionFromCenter ), -dir, 0.3f, Mathf.PI * 0.2f, cutStep * 0.3f, breaker.generateRandomness );
                        }
                        cutLine[ numZigs - zig ] = leftPoint ;
                        cutLine[ numZigs + zig + 2 ] = rightPoint;
                    }

                    // cap the end
                    cutLine[ 0 ] = leftPoint + perp * maxDistance;
                    cutLine[ numPoints - 1 ] = rightPoint + perp * maxDistance;

                    // copy points to region
                    cutterRegion.Clear();
                    for ( int i = 0; i < numPoints; i++ ) cutterRegion.Add( cutLine[ i ] );
                    cutterSegments = pb.segments( cutter, breaker.csgUpscale );

                    // cut each intermediate segment with cutter, place result to alt
                    altIntermediate.Clear();
                    for ( int i = 0; i < intermediate.Count; i++ ) {
                        CombinedSegmentLists comb = pb.combine( intermediate[ i ].Clone(), cutterSegments.Clone() );
                        SegmentList intersect = pb.selectIntersect( comb );
                        SegmentList difference = pb.selectDifference( comb );
                        if ( intersect.Count > 0 ) altIntermediate.Add( intersect );
                        if ( difference.Count > 0 ) altIntermediate.Add( difference );
                    }

                    // swap alt/intermediate
                    swapSegList = altIntermediate; altIntermediate = intermediate; intermediate = swapSegList;

                // increment angle
                } catch ( Exception e ) {
                    Debug.Log( e );
                } // skip errors
                curAngle += _RandomizeParameterDelta( cutStep, -cutStep * 0.2f, cutStep * 0.2f, generateRandomness );
            }

            // splinters and cross cuts
            altIntermediate.Clear();
            Vector2 ctr = center * breaker.csgUpscale;
            int maxSplinters = 1 + numCuts / 3;
            for ( int s = 0, nsegs = intermediate.Count; s < nsegs; s++ ) {
                List<SegmentList> cuts = (s % maxSplinters) == 1 ? _MakeSplinter( intermediate[ s ], scale, maxDistance, cutStep * 0.2f, breaker ) : null;
                if ( cuts != null && cuts.Count > 0 ) altIntermediate.AddRange( cuts );
                else altIntermediate.Add( intermediate[ s ] );
            }
            intermediate.Clear();
            for ( int s = 0, nsegs = altIntermediate.Count; s < nsegs; s++ ) {
                List<SegmentList> cuts = _MakeCrossCuts( altIntermediate[ s ], scale, breaker );
                if ( cuts != null && cuts.Count > 0 ) intermediate.AddRange( cuts );
                else intermediate.Add( altIntermediate[ s ] );
            }

            // convert intermediate to polys
            data.polygons.Clear();
            float downscale = 1.0f / breaker.csgUpscale;
            // bool emptyPolysDetected = false;
            List<Polygon> polygons = new List<Polygon>();
            for ( int s = 0, nsegs = intermediate.Count; s < nsegs; s++ ) {
                if ( intermediate[ s ] == null ) continue;
                Polygon polys = pb.polygon( intermediate[ s ], downscale );
                // if ( polys.regions.Count == 0 ) emptyPolysDetected = true;
                polygons.Add( polys );
            }

            // finally
            Rect bounds = new Rect( -0.1f, -0.1f, 1.2f, 1.2f );
            for ( int k = 0, np = polygons.Count; k < np; k++ ) {
                Polygon polys = polygons[ k ];
                for ( int i = 0, nr = polys.regions.Count; i < nr; i++ ) {
                    SpriteBreakerPolygon poly = new SpriteBreakerPolygon();
                    poly.edges = polys.regions[ i ];
                    for ( int j = 0, jj = poly.edges.Count; j < jj; j++ ) {
                        if ( !bounds.Contains( poly.edges[ j ] ) ) { poly = null; break; }
                        poly.pivot += poly.edges[ j ];
                    }
                    if ( poly == null ) continue;
                    poly.pivot /= poly.edges.Count;
                    if ( triangulate ) poly.Triangulate( true, false );
                    data.polygons.Add( poly );
                }
            }

        }

        // makes splinters in shards
        List<SegmentList> _MakeSplinter( SegmentList subject, Vector2 scale, float maxDistance, float angleDeviation, SpriteBreaker breaker ) {                
            // sanity check
            if ( subject.Count <= 8 ) return null;

            // sort segments by distance to origin into left/right
            Vector2 origin = breaker.generateOrigin * breaker.csgUpscale;
            maxDistance *= breaker.csgUpscale;
            angleDeviation *= Mathf.Rad2Deg;
            Vector2 rayDir;
            List<Vector2> leftPoints, rightPoints;
            float avgLeftLength, avgRightLength;
            (leftPoints, rightPoints, rayDir, avgLeftLength, avgRightLength) = _SplitIntoLeftAndRight( subject, origin );            

            // left should have more points than right
            if ( rightPoints.Count > leftPoints.Count ) {
                List<Vector2> temp = rightPoints;
                rightPoints = leftPoints;
                leftPoints = temp;
                float t = avgRightLength; avgRightLength = avgLeftLength; avgLeftLength = t;
            }

            // find suitable start location
            int curLeft = 2 + (int) _RandomizeParameter( 0, 0, 4, breaker.generateRandomness );
            int splintDir = 0;
            for ( int i = curLeft; i < leftPoints.Count - 1; i++ ) {
                Vector2 pt2 = leftPoints[ i ], pt1 = leftPoints[ i - 1 ], pt0 = leftPoints[ i - 2 ];
                float ang0 = Vector2.SignedAngle( ( pt1 - pt0 ).normalized, rayDir ),
                      ang1 = Vector2.SignedAngle( ( pt2 - pt1 ).normalized, rayDir );

                if ( Mathf.Abs( ang0 - ang1 ) > angleDeviation ) {
                    splintDir = (int) Mathf.Sign( ang0 - ang1 );
                    curLeft = i - 1;
                    break;
                }
            }

            // not found
            if ( splintDir == 0 || curLeft >= rightPoints.Count - 4 ) return null;

            // create poly
            Polygon cutPoly = new Polygon() { regions = new List<List<Vector2>>() };
            List<Vector2> points = new List<Vector2>( rightPoints.Count );
            cutPoly.regions.Add( points );

            // first cap and point
            Vector2 lp = leftPoints[ curLeft ], rp;
            points.Add( lp ); // will be updated at the end
            points.Add( lp );

            // middle
            float transitionToCenter = 0.5f, transitionStep = _RandomizeParameterDelta( 0.15f, 0.1f, breaker.generateRandomness );
            float leftRightWidth = 0;
            for ( int i = curLeft + 1; i < rightPoints.Count - 1; i++ ) {
                transitionToCenter = Mathf.Clamp01( transitionToCenter - transitionStep );
                lp = leftPoints[ i ];  rp = rightPoints[ i ];
                leftRightWidth = ( rp - lp ).magnitude;
                points.Add( lp + ( 0.7f - 0.6f * transitionToCenter ) * ( rp - lp ) );
            }

            // end cap
            lp = origin + rayDir * maxDistance;
            points.Add( lp );
            Vector2 rayDirPerpendicular = Vector2.Perpendicular( rayDir ).normalized * scale;
            if ( Vector2.Dot( ( rightPoints[ curLeft ] - leftPoints[ curLeft ] ).normalized, rayDirPerpendicular ) < 0 ) rayDirPerpendicular *= -1;
            points.Add( lp - rayDirPerpendicular * leftRightWidth );
            points[ 0 ] -= rayDirPerpendicular * leftRightWidth;

            // DEBUG - return cut regions
            /* List <SegmentList> res = new List<SegmentList>();
            res.Add( subject );
            SegmentList cutPolySegs = PolyBool.instance.segments( cutPoly );
            res.Add( cutPolySegs );
            return res;
            */
            
            // cut
            try {
                SegmentList cutter = PolyBool.instance.segments( cutPoly );
                CombinedSegmentLists comb = PolyBool.instance.combine( subject.Clone(), cutter );
                SegmentList intersect = PolyBool.instance.selectIntersect( comb );
                SegmentList difference = PolyBool.instance.selectDifference( comb );
                List<SegmentList> result = new List<SegmentList>();
                if ( intersect.Count > 0 ) result.Add( intersect );
                if ( difference.Count > 0 ) result.Add( difference );
                if ( result.Count == 0 ) Debug.Log( "Empty result while cross cutting" );                
                return result;
            } catch ( Exception e ) {
                Debug.Log( "Exception while cross cutting: " + e.ToString() );
                return null;
            }
        }

        // makes cut across shards
        List<SegmentList> _MakeCrossCuts( SegmentList subject, Vector2 scale, SpriteBreaker breaker ) {
            // sanity check
            if ( subject.Count <= 5 ) return null;

            // sort segments by distance to origin into left/right
            Vector2 origin = breaker.generateOrigin * breaker.csgUpscale;
            Vector2 rayDir;
            List<Vector2> leftPoints, rightPoints;
            float avgLeftLength, avgRightLength;
            (leftPoints, rightPoints, rayDir, avgLeftLength, avgRightLength) = _SplitIntoLeftAndRight( subject, origin );
            Vector2 rayDirPerpendicular = Vector2.Perpendicular( rayDir ).normalized * scale;

            // left should have more points than right
            if ( rightPoints.Count > leftPoints.Count ) {
                List<Vector2> temp = rightPoints;
                rightPoints = leftPoints;
                leftPoints = temp;
                float t = avgRightLength; avgRightLength = avgLeftLength; avgLeftLength = t;
            }

            // find suitable places to cross cut
            Polygon cutPoly = new Polygon() { regions = new List<List<Vector2>>() };
            int numCuts = 0, maxCuts = 10;
            int curRight = 0, curLeft = 1 + (int) _RandomizeParameter( 0, 0, 4, breaker.generateRandomness );
            float widestLeftRightSpan = 0;

            // while cuts can be made
            while ( numCuts < maxCuts && curLeft < leftPoints.Count ) {

                // take next left point
                Vector2 rightPoint, leftPoint = leftPoints[ curLeft ];
                float rightDist, leftDist = ( origin - leftPoint ).magnitude;

                // X = random 1-4
                int cutWidth = Math.Min( (int) _RandomizeParameter( 1 + numCuts % 2, 0, 3, breaker.generateRandomness ), leftPoints.Count - curLeft - 1 );
                if ( cutWidth + curLeft >= leftPoints.Count - 1 ) break;

                // on the right side, walk forward from last point until distance = center->left is achieved
                // and there are enough points left to make a cut
                bool perpendicularCut = true;
                while ( curRight < rightPoints.Count ) {
                    rightPoint = rightPoints[ curRight ];
                    rightDist = ( origin - rightPoint ).magnitude;
                    widestLeftRightSpan = Mathf.Max( widestLeftRightSpan, ( leftPoint - rightPoint ).magnitude );
                    if ( ( rightDist > leftDist || Mathf.Abs( rightDist - leftDist ) < avgRightLength * 0.25f ) && curRight + cutWidth < rightPoints.Count ) {
                        float dot = Math.Abs( Vector2.Dot( ( leftPoint - rightPoint ).normalized, rayDir ) );
                        perpendicularCut = ( dot > 0.2f );
                        break;
                    }
                    curRight++;
                    // if out of points on right, or less than X left, this will be a perpendicular cut
                }

                // allocate points
                Vector2[] region =
                    perpendicularCut ? new Vector2[ cutWidth + 3 ]
                                     : new Vector2[ ( cutWidth + 1 ) * 2 ];

                // add points
                for ( int i = 0; i <= cutWidth; i++ ) {
                    region[ i ] = leftPoints[ curLeft + i ];
                    if ( !perpendicularCut ) {
                        region[ cutWidth + i + 1 ] = rightPoints[ curRight + ( cutWidth - i ) ];
                    }
                }

                // if perpendicular, add perp points
                if ( perpendicularCut ) {
                    float flipPerp = Mathf.Sign( Vector2.SignedAngle( ( leftPoint - origin ).normalized, rayDir ) );
                    region[ cutWidth + 1 ] = leftPoints[ curLeft + cutWidth ] + flipPerp * rayDirPerpendicular * widestLeftRightSpan * 2;
                    region[ cutWidth + 2 ] = leftPoints[ curLeft ] + flipPerp * rayDirPerpendicular * widestLeftRightSpan * 2;
                } else curRight += cutWidth;

                // add
                cutPoly.regions.Add( new List<Vector2>( region ) );

                // advance
                curLeft += (int) _RandomizeParameter( 1 + cutWidth + numCuts % 3, 0, 4, breaker.generateRandomness );
                numCuts++;

            }

            // cut
            try {
                SegmentList cutter = PolyBool.instance.segments( cutPoly );
                CombinedSegmentLists comb = PolyBool.instance.combine( subject.Clone(), cutter );
                SegmentList intersect = PolyBool.instance.selectIntersect( comb );
                SegmentList difference = PolyBool.instance.selectDifference( comb );
                List<SegmentList> result = new List<SegmentList>();
                if ( intersect.Count > 0 ) result.Add( intersect );
                if ( difference.Count > 0 ) result.Add( difference );
                if ( result.Count == 0 ) Debug.Log( "Empty result while cross cutting" );
                return result;
            } catch ( Exception e ) {
                Debug.Log( "Exception while cross cutting: " + e.ToString() );
                return null;
            }
        }

        // returns leftPoints, rightPoints, dirRay, avgLeftEdgeLength, avgRightEdgeLength
        (List<Vector2>, List<Vector2>, Vector2, float, float) _SplitIntoLeftAndRight( SegmentList subject, Vector2 origin ) {
            subject.Sort( ( Segment a, Segment b ) => {
                float a0 = ( a.start - origin ).magnitude, a1 = ( a.end - origin ).magnitude;
                float aa = Mathf.Min( a0, a1 );
                float b0 = ( b.start - origin ).magnitude, b1 = ( b.end - origin ).magnitude;
                float bb = Mathf.Min( b0, b1 );
                return aa.CompareTo( bb );
            } );

            // sort segments into left and right and set up ray direction
            Vector2 rayDir = new Vector2();
            List<Vector2> leftPoints = new List<Vector2>( 4 + subject.Count / 2 ),
                         rightPoints = new List<Vector2>( leftPoints.Capacity );
            bool[] matched = new bool[ subject.Count ];
            Segment seg = subject[ 0 ];
            Vector2 leftEdge = seg.start, rightEdge = seg.end;
            float avgLeftLength = 0, avgRightLength = 0;
            leftPoints.Add( leftEdge );
            rightPoints.Add( rightEdge );
            matched[ 0 ] = true;
            int matchedCount = 1, firstUnmatched = 1, totalSegs = subject.Count;
            while ( matchedCount < totalSegs ) {
                // find match
                bool found = false;
                for ( int i = firstUnmatched; i < totalSegs; i++ ) {
                    seg = subject[ i ];
                    if ( matched[ i ] ) continue; // skip if already matched
                    if ( Epsilon.pointsSame( seg.start, leftEdge ) ) {
                        avgLeftLength += ( leftEdge - seg.end ).magnitude; leftEdge = seg.end; leftPoints.Add( leftEdge ); found = true;
                    } else if ( Epsilon.pointsSame( seg.end, leftEdge ) ) {
                        avgLeftLength += ( leftEdge - seg.start ).magnitude; leftEdge = seg.start; leftPoints.Add( leftEdge ); found = true;
                    } else if ( Epsilon.pointsSame( seg.start, rightEdge ) ) {
                        avgRightLength += ( rightEdge - seg.end ).magnitude; rightEdge = seg.end; rightPoints.Add( rightEdge ); found = true;
                    } else if ( Epsilon.pointsSame( seg.end, rightEdge ) ) {
                        avgRightLength += ( rightEdge - seg.start ).magnitude; rightEdge = seg.start; rightPoints.Add( rightEdge ); found = true;
                    }
                    // have match
                    if ( found ) {
                        // increment unmatched
                        if ( i - firstUnmatched == 1 ) firstUnmatched++;
                        matched[ i ] = true; // mark as found
                        matchedCount++;
                        rayDir += ( seg.start - origin ).normalized;
                        break;
                    }
                }
                if ( !found ) break;
            }
            rayDir = ( rayDir / ( subject.Count - 1 ) ).normalized;
            avgRightLength /= rightPoints.Count; avgLeftLength /= leftPoints.Count;
            return (leftPoints, rightPoints, rayDir, avgLeftLength, avgRightLength);
        }

        float _RandomizeParameterDelta( float input, float deltaMinus, float deltaPlus, float randomness ) {            
            float rand = UnityEngine.Random.Range( -randomness, randomness );
            if ( rand < 0 ) return input - rand * UnityEngine.Random.Range( 0, deltaMinus );
            return input + rand * UnityEngine.Random.Range( 0, deltaPlus );
        }

        float _RandomizeParameterDelta( float input, float delta, float randomness ) {
            float rand = UnityEngine.Random.Range( -randomness, randomness );
            return input + rand * UnityEngine.Random.Range( -delta, delta );
        }

        float _RandomizeParameter( float input, float minValue, float maxValue, float randomness ) {
            float rand = UnityEngine.Random.Range( -randomness, randomness );
            if ( rand < 0 ) return input - rand * UnityEngine.Random.Range( 0, Mathf.Abs( input - minValue ) );
            return input + rand * UnityEngine.Random.Range( 0, Mathf.Abs( input - maxValue ) );
        }

        Vector2 _RandomizeMoveInDirection( Vector2 coord, Vector2 origin, float distance, Vector2 direction, float deltaDistMultiplier, float deltaRad, float rail, float randomness ) {
            float rand = UnityEngine.Random.Range( -randomness, randomness );
            float rotateDir = rand * UnityEngine.Random.Range( -deltaRad, deltaRad );
            float sin = Mathf.Sin( rotateDir ), cos = Mathf.Cos( rotateDir );
            Vector2 newDir = new Vector2( ( cos * direction.x ) - ( sin * direction.y ), ( sin * direction.x ) + ( cos * direction.y ) );
            float newDistance = distance + rand * UnityEngine.Random.Range( -deltaDistMultiplier, deltaDistMultiplier ) * distance;
            Vector2 newCoord = coord + newDir * newDistance;

            // clip to rails
            float newCoordAngle = Vector2.SignedAngle( direction, ( newCoord - origin ).normalized ) * Mathf.Deg2Rad;
            if ( newCoordAngle > rail || newCoordAngle < -rail ) {
                rail *= 0.9f * Mathf.Sign( newCoordAngle );
                sin = Mathf.Sin( rail ); cos = Mathf.Cos( rail );
                Vector2 railDir = new Vector2( ( cos * direction.x ) - ( sin * direction.y ), ( sin * direction.x ) + ( cos * direction.y ) );
                float newCoordDist = ( newCoord - origin ).magnitude;
                newCoord = origin + newCoordDist * railDir.normalized;
            }

            return newCoord;
            
        }

        public static readonly SpriteBreakerGenerator instance = new SpriteBreakerGenerator();

    }

}
