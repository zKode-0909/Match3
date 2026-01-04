using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEditor;
using SpriteBreakerUtil;
using System.ComponentModel;
using UnityEngine.Rendering;

[HelpURL( "http://gogoat.com/sprite-breaker/documentation" )]
[Serializable]
public class SpriteBreaker : MonoBehaviour {

    [Tooltip( "Object containing the shard shape data." )]
    public SpriteBreakerData data; // slice data

    [Tooltip("If assigned, SpriteBreaker without data will copy data from asset when broken.")]
    public SpriteBreakerDataAsset dataAsset; // if assigned, when object is created

    [Tooltip("Sprite used to generate fragments. If this field is not assigned, a sprite from attached SpriteRenderer will be used.")]
    public Sprite sprite; // overrides attached SpriteRenderer's sprite on this gameObject

    [Tooltip( "A gameObject used as a prefab to generate shards. (Optional)" )]
    public GameObject shardPrototype; // instantiated as base for each shard, if provided

    [Tooltip( "Transform used as a parent for generated fragments. (Optional)" )]
    public Transform shardsParent = null; // shards will be parented to this Transform (or this object if not set)

    [Tooltip( "Update spawned shards renderer color every frame." )]
    public bool updateShardsColor = true;
    protected Color _colorPreviousUpdate; // used to check if need to update the shards color

    [Tooltip( "Shadow casting mode." )]
    public ShadowCastingMode castShadows = ShadowCastingMode.TwoSided;
    
    [Tooltip( "Receive shadows." )]
    public bool receiveShadows = false;
    
    [Tooltip("Overall color/alpha of all shards.")]
    public Color shardsColor = Color.white; // color multiplied into each spawned fragment's renderer

    [Tooltip( "This event is called for each generated shard.")]
    public ShardEvent OnShardCreated = new ShardEvent(); // called for each generated shard

    [Tooltip( "Should editor draw shards preview." )]
    public bool drawEditorGizmos = true; // draw preview of slicing

    [Tooltip( "Draw shards preview with this color.")]
    public Color gizmoColor = new Color( 0.9f, 0.3f, 0.3f, 1 ); // preview unselected color

    [Tooltip( "Should attached SpriteRenderer be disabled/enabled on Break/Clear." )]
    public bool disableRendererOnBreak = true;

    [Tooltip("If set, a collider will be added/updated to each shard with polygon edges.")]
    public CreateColliders createColliders = CreateColliders.FakePhysics;
    public enum CreateColliders { None, Collider2D, Collider3D, FakePhysics };

    [Tooltip( "For 3D collider how thick should the generated collider mesh be." )]
    [Range(0.05f,1000)]
    public float colliderThickness = 1f;

    [Tooltip( "Gravity for fake physics" )][Description("Gravity")]
    public Vector3 fakeGravity = new Vector3(0, -10, 0);

    [Tooltip("Multiply rigid bodies mass by this value when creating shards.")]
    public float massMultiplier = 1.0f;

    [Tooltip( "Apply this impulse radially from origin to each shard on break. Origin is set in Generate foldout." )]
    public float initialRadialImpulse = 1f;
    [Description( "±" )][Tooltip("Plus or minus randomness % variation for radial impulse")][Range(0,100)]
    public float initialRadialImpulsePlusMinus = 10f;

    [Tooltip( "Apply this impulse linearly to each shard on break." )]
    public Vector3 initialLinearImpulse = new Vector3();
    [Description( "±" )][Tooltip( "Plus or minus randomness % multiplier for linear impulse" )][Range( 0, 200 )]
    public float initialLinearImpulsePlusMinus = 10f;

    [Tooltip( "Apply this impulse to shard rotation on break." )]
    public Vector3 initialRotationalImpulse = new Vector3(0,0,0);
    [Description( "±" )][Tooltip( "Plus or minus randomness % multiplier for rotational impulse" )][Range( 0, 100 )]
    public float initialRotationalImpulsePlusMinus = 10f;

    [Description( "Time to live (seconds)")][Tooltip( "If set to non-0, the shards will be destroyed after this time interval after Break() is called." )][Min(0)]
    public float timeToLive = 2f;

    [Tooltip( "If timeToLive is set, the shards will fade to alpha=0 after this time." )][Min( 0 )]
    public float fadeAfter = 1f;

    public enum EndAction {
        DestroyObject,
        DestroyParent,
        DeactivateShards,
        Reset,
        ClearDataAndReset,
        Restart,
        DoNothing
    };
    [Tooltip( "If timeToLive is set, what to do when time expires after Break()" )]
    public EndAction endAction;

    [Tooltip( "If timeToLive is set, called after time expires" )]
    public BreakEvent OnTimeToLiveExpired = new BreakEvent();

    // used to upscale coordinates when performing CSG operations, to avoid precision errors
    [Tooltip( "How much to upscale coordinates when performing CSG operations (1000 is default)" )]
    [Min( 1 )]
    public float csgUpscale = 1000;

    // epsilon used for CSG
    [Tooltip( "Epsilon precision value used during CSG. 10 to the -X power (or 10e-X), where X is this value (5 is default)" )]
    [Range( 2, 8 )]
    public int csgEpsilon = 5;

    [Tooltip( "Generate shards" )]
    public BreakerGenerateType generateType;
    public enum BreakerGenerateType {
        Radial,
        Directional,
        Bricks,
        Voronoi,
        Custom
    }

    [Tooltip( "Seed - if set to 0, the seed is random")]
    public int generateSeed = 0;

    [Tooltip( "Start data for generation" )]
    public BreakerGenerateFrom generateFrom;
    public enum BreakerGenerateFrom {
        Quad,
        Data,
        Asset
    }

    [Tooltip( "Randomness" )][Range(0, 2)] // multiplier
    public float generateRandomness = 1.5f;

    [Tooltip("Features angle")][Range(0,360)] // degrees
    public float generateAngle = 0;

    [Tooltip( "Features spacing" )][Range( 0, 100 )] // percent
    public float generateSpacing = 60;

    [Tooltip( "Features frequency" )] [Range( 0, 100 )] // percent
    public float generateFrequency = 25;

    [Tooltip( "Origin" )]
    public Vector2 generateOrigin = new Vector2( 0.5f, 0.5f );

    [Tooltip( "Automatically call Break() on Awake." )]
    public bool autoBreakOnAwake = false;
    
    public bool hasShards => _HasShards(); // true if has shards still attached to polygons
    public bool canBreak => _CanBreak(); // true if breaking is possible - i.e. there's a sprite, and breaker data

    public Sprite currentSprite => _GetSprite(); // returns current sprite - either current override, or from a renderer if present

    public GameObject[] shards => _GetShards(); // returns currently spawned shards as gameobjects

    // private
    private static Material _defaultMaterial; // used internally
    public MaterialPropertyBlock materialPropertyBlock; // used to update shards colors
    bool _simulating = false; // Break was called in play mode
    public float timeBroken { get; private set; } // time Break was called
    float _initialFadeAlpha = 1; // when starting to fade, this is the starting point for alpha
    private static readonly int _Color = Shader.PropertyToID( "_Color" );

    /// <summary>
    /// Creates pieces or "shards" of sprite according to stored data. Uses .shardPrototype, if assigned, as prefab for each.
    /// New pieces are parented under .shardsParent or this GameObject. Calls OnShardCreated event for each shard.
    /// </summary>
    public void Break () {
        
        // clean finish
        if ( _simulating ) FinishUp(true);
        
        // auto generate
        if ( data == null || data.polygons.Count == 0 ) Generate();
        
        // get sprite
        Sprite useSprite = sprite;
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if ( useSprite == null && spriteRenderer != null ) useSprite = spriteRenderer.sprite;
        if ( useSprite == null ) return;

        // copy material property block from sprite
        UpdateMaterialPropertyBlock( spriteRenderer );

        // if data is empty, use asset
        if ( ( data == null || data.polygons.Count == 0 ) && dataAsset != null ) {
            data = dataAsset.data.Clone();
        }

        // can spawn
        if ( data != null && data.polygons.Count > 0 && useSprite != null ) {

            // if simulating, reset alpha
            if ( _simulating ) shardsColor.a = _initialFadeAlpha;
            
            // prepare
            Transform parent = (shardsParent != null ? shardsParent : transform);
            if ( disableRendererOnBreak && spriteRenderer != null && spriteRenderer.enabled ) {
                #if UNITY_EDITOR
                if ( !Application.isPlaying ) Undo.RecordObject( spriteRenderer, "Disable renderer" );
                #endif
                spriteRenderer.enabled = false;
            }

            // prepare uv
            (Vector2 spriteUVOrigin, Vector2 spriteUVSize) = (useSprite.rect.position, useSprite.rect.size);
            Vector2 textureSize = new Vector2( useSprite.texture.width, useSprite.texture.height );
            
            // if atlas
            if ( useSprite.packed ) {
                // use texture rect when in atlas mode
                if ( useSprite.packingMode == SpritePackingMode.Rectangle && useSprite.textureRect.width > 0 ) {
                    spriteUVOrigin = useSprite.textureRect.position - useSprite.textureRectOffset;
                    spriteUVSize = useSprite.rect.size;
                // probably won't work correctly with tight packing
                } else if ( useSprite.packingMode == SpritePackingMode.Tight ) {
                    // attempt to locate the sprite origin on atlas based on sprite UVs
                    Vector2 [] suvs = useSprite.uv;
                    Vector2 min = Vector2.positiveInfinity;
                    foreach ( Vector2 suv in suvs ) {
                        min.x = Mathf.Min( min.x, suv.x );
                        min.y = Mathf.Min( min.y, suv.y );
                    }
                    // set new origin
                    spriteUVOrigin = min * textureSize;
                    // issue a warning
                    Debug.LogWarning( "Sprite " + useSprite.name + " may not render correctly when it's packed into a SpriteAtlas with Tight Packing.");
                }
            }

            // set uv offset
            spriteUVOrigin /= textureSize;
            spriteUVSize /= textureSize;
            Rect uvTransform = new Rect( spriteUVOrigin, spriteUVSize );
            
            // for each polygon
            for ( int i = 0, np = data.polygons.Count; i < np; i++ ) {
                SpriteBreakerPolygon poly = data.polygons[ i ];
                
                // make sure poly's enabled / valid
                if ( !poly.enabled || poly.edges.Count < 3 ) continue;

                // make sure poly's triangulated
                if ( poly.triangles == null || poly.triangles.Length == 0 ) {
                    poly.Triangulate();
                    // if failed to triangulate, skip
                    if ( poly.triangles == null || poly.triangles.Length == 0 ) {
                        // Debug.Log( "Poly " + i + " failed to triangulate." );                            
                        continue;
                    }
                }

                // re-spawn it
                if ( poly.gameObject != null ) DeleteShard( poly );
                poly.gameObject = SpawnShard( i, parent, useSprite, spriteRenderer, uvTransform );
                ApplyShardInitialImpulse( poly );
                if ( Application.isPlaying ) {
                    OnShardCreated.Invoke( poly.gameObject, poly, this );
                }
            }
            
            // start simulatin'
            if ( timeToLive > Mathf.Epsilon ) {
                _simulating = true;
                _initialFadeAlpha = shardsColor.a;
                timeBroken = Time.time;
            }
        }
    }

    /// <summary>
    /// Deletes all spawned pieces, resets renderer
    /// </summary>
    public void Clear () {
        _simulating = false;
        if ( timeToLive > Mathf.Epsilon ) shardsColor.a = _initialFadeAlpha;
        if ( data == null ) return;
        for ( int i = 0, np = data.polygons.Count; i < np; i++ ) {
            SpriteBreakerPolygon poly = data.polygons[ i ];
            if ( poly.gameObject != null ) DeleteShard( poly );
        }
        // toggle sprite renderer back on
        if ( disableRendererOnBreak ) {
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if ( spriteRenderer != null && !spriteRenderer.enabled ) {
                #if UNITY_EDITOR
                if ( !Application.isPlaying ) Undo.RecordObject( spriteRenderer, "Enable renderer" );
                #endif
                spriteRenderer.enabled = true;
            }
        }
    }

    /// <summary>
    /// Split polygons using given cut line.
    /// </summary>
    /// <param name="polygons">Polygons to split. Can be already belonging to this SpriteBreaker data, or just arbitrary new SpriteBreakerPolygons.</param>
    /// <param name="cutLine">The cutting line's points</param>
    /// <param name="triangulateResult">Call Triangulate on each resulting polygon</param>
    /// <param name="replaceData">If true, will add and replace polygons in this object's data</param>
    /// <returns>resulting polygons, or null if failed</returns>
    public List<SpriteBreakerPolygon> SplitPolygons( List<SpriteBreakerPolygon> polygons, List<Vector2> cutLine, bool triangulateResult=true, bool replaceData=true) {
        // make sure the cutter line is valid
        if ( cutLine == null || polygons == null || cutLine.Count < 2 ) return null;

        // prepare
        if ( replaceData && data == null ) data = new SpriteBreakerData();
        PolyBool polyBool = PolyBool.instance;
        Epsilon.eps = Mathf.Pow( 10, -csgEpsilon );
        try {

            // construct a polygon to cut with
            float downscale = 1.0f / csgUpscale;
            List<Vector2> cutterPoints;
            Vector2 ext0 = new Vector2(), ext1 = new Vector2();
            int numCutPts = cutLine.Count;

            // if poly is not closed, extend its endpoints
            if ( ( cutLine[ 0 ] - cutLine[ numCutPts - 1 ] ).magnitude > Mathf.Epsilon ) {
                cutterPoints = new List<Vector2>( numCutPts + 3 );
                ext0 = ( cutLine[ 0 ] - cutLine[ 1 ] ).normalized;
                ext1 = ( cutLine[ numCutPts - 1 ] - cutLine[ numCutPts - 2 ] ).normalized;
                cutterPoints.Add( cutLine[ 0 ] + ext0 * 1 * csgUpscale );
                cutterPoints.AddRange( cutLine );
                cutterPoints.Add( cutLine[ numCutPts - 1 ] + ext1 * 1 * csgUpscale );
                if ( Vector2.Dot( ext0, ext1 ) < 0 ) { cutterPoints.Add( Vector2.Perpendicular( ext0 ) * 1 * csgUpscale ); } // add a point between if end segments are more than 90 deg apart
            } else {
                cutterPoints = cutLine;
            }

            // cutter
            Polygon cutter = new Polygon();
            cutter.regions = new List<List<Vector2>>();
            cutter.regions.Add( cutterPoints );
            SegmentList cutterSeg = polyBool.segments( cutter, csgUpscale );

            // subject polygons
            List<Polygon> subjects = new List<Polygon>( polygons.Count );
            List<SpriteBreakerPolygon> resultList = new List<SpriteBreakerPolygon>();

            // for each polygon
            for ( int i = 0, numPolys = polygons.Count; i < numPolys; i++ ) {
                // create poly
                SpriteBreakerPolygon origPoly = polygons[ i ];
                Polygon subj = new Polygon();
                subj.regions = new List<List<Vector2>>();
                subj.regions.Add( new List<Vector2>( origPoly.edges ) );
                if ( replaceData ) data.polygons.Remove( origPoly );

                // perform cut
                SegmentList segments = polyBool.segments( subj, csgUpscale );
                CombinedSegmentLists comb = polyBool.combine( segments, cutterSeg.Clone() );
                SegmentList intersection = polyBool.selectIntersect( comb );
                SegmentList difference = polyBool.selectDifference( comb );
                Polygon result;

                // make sure there's data
                if ( replaceData && data == null ) data = new SpriteBreakerData();

                // convert intersection to one or more polygons
                if ( intersection.Count > 0 ) {
                    result = polyBool.polygon( intersection, downscale );
                    for ( int j = 0; j < result.regions.Count; j++ ) {
                        SpriteBreakerPolygon polygon = new SpriteBreakerPolygon();
                        polygon.edges = new List<Vector2>( result.regions[ j ] );
                        for ( int jj = 0, njp = polygon.edges.Count; jj < njp; jj++ ) {
                            polygon.pivot += polygon.edges[ jj ];
                        }
                        polygon.pivot /= polygon.edges.Count;
                        if ( replaceData ) data.polygons.Add( polygon );
                        if ( triangulateResult ) polygon.Triangulate();
                        resultList.Add( polygon );
                    }
                }
                // convert difference
                if ( difference.Count > 0 ) {
                    result = polyBool.polygon( difference, downscale );
                    for ( int j = 0; j < result.regions.Count; j++ ) {
                        SpriteBreakerPolygon polygon = new SpriteBreakerPolygon();
                        polygon.edges = new List<Vector2>( result.regions[ j ] );
                        for ( int jj = 0, njp = polygon.edges.Count; jj < njp; jj++ ) {
                            polygon.pivot += polygon.edges[ jj ];
                        }
                        polygon.pivot /= polygon.edges.Count;
                        if ( replaceData ) data.polygons.Add( polygon );
                        if ( triangulateResult ) polygon.Triangulate();
                        resultList.Add( polygon );
                    }
                }
            }

            return resultList;
        } catch ( Exception e ) {
            Debug.LogError( e );
            return null;
        }

    }

    /// <summary>
    /// Moves shards gameObjects to initial location and restarts clock for timeToLive and fadeAfter.
    /// </summary>
    /// <param name="resetShardPositions">Move shards to their spawn positions</param>
    public void RestartSimulation( bool resetShardPositions=true ) {

        SpriteRenderer baseRenderer = GetComponent<SpriteRenderer>();
        Sprite baseSprite = sprite != null ? sprite : ( baseRenderer != null ? baseRenderer.sprite : null );
        bool flipX = baseRenderer != null && baseRenderer.flipX;
        bool flipY = baseRenderer != null && baseRenderer.flipY;
        Vector3 spritePivot = new Vector3( flipX ? ( baseSprite.rect.width - baseSprite.pivot.x ) : baseSprite.pivot.x, flipY ? ( baseSprite.rect.height - baseSprite.pivot.y ) : baseSprite.pivot.y, 0 );

        // reset
        for ( int i = 0, ns = data.polygons.Count; i < ns; i++ ) {
            SpriteBreakerPolygon poly = data.polygons[ i ];
            if ( poly.gameObject == null ) continue;
            if ( resetShardPositions ) {
                Transform t = poly.gameObject.transform;
                Vector3 polyPivot = new Vector3( flipX ? ( 1 - poly.pivot.x ) : poly.pivot.x,
                    flipY ? ( 1 - poly.pivot.y ) : poly.pivot.y, 0 );
                t.localPosition =
                    ( new Vector3( baseSprite.rect.width * polyPivot.x, baseSprite.rect.height * polyPivot.y, 0 ) -
                      spritePivot ) / baseSprite.pixelsPerUnit;
                t.localRotation = Quaternion.identity;
                t.localScale = Vector3.one;
            }
            ApplyShardInitialImpulse( poly );
            OnShardCreated.Invoke( poly.gameObject, poly, this );
        }
    

        // start simulatin again
        if ( timeToLive > Mathf.Epsilon ) {
            _simulating = true;
            _initialFadeAlpha = shardsColor.a;
            timeBroken = Time.time;
        }
    }

    /// <summary>
    /// Joins provided polygons into one or more polygons, performing CSG Union.
    /// </summary>
    /// <param name="polygons">One or more operand polygons</param>
    /// <param name="triangulateResult">Call Triangulate on each resulting polygon</param>
    /// <param name="replaceData">If true, will add and replace polygons in this object's data</param>
    /// <returns>Resulting polygon(s)</returns>
    public List<SpriteBreakerPolygon> UnionPolygons( List<SpriteBreakerPolygon> polygons, bool triangulateResult=true, bool replaceData=true ) {
        SegmentList segments = null;
        PolyBool polyBool = PolyBool.instance;
        Epsilon.eps = Mathf.Pow( 10, -csgEpsilon );
        for ( int i = 0, np = polygons.Count; i < np; i++ ) {
            SpriteBreakerPolygon polygon = polygons[ i ];
            if ( replaceData && data != null ) data.polygons.Remove( polygon );
            // make polygon
            Polygon poly = new Polygon();
            poly.regions = new List<List<Vector2>>();            
            poly.regions.Add( polygon.edges );
            // first segment
            if ( segments == null ) {
                segments = polyBool.segments( poly, csgUpscale );
            } else {
                SegmentList seg = polyBool.segments( poly, csgUpscale );
                CombinedSegmentLists comb = polyBool.combine( segments, seg );
                segments = polyBool.selectUnion( comb );
            }
        }

        // ensure data
        if ( replaceData && data == null ) data = new SpriteBreakerData();

        // convert result to one or more polygons
        List<SpriteBreakerPolygon> resultList = new List<SpriteBreakerPolygon>();
        Polygon result = polyBool.polygon( segments, 1.0f / csgUpscale );
        for ( int i = 0; i < result.regions.Count; i++ ) {
            SpriteBreakerPolygon polygon = new SpriteBreakerPolygon();
            polygon.edges = new List<Vector2>( result.regions[ i ] );
            for ( int j = 0, np = polygon.edges.Count; j < np; j++ ) {
                polygon.pivot += polygon.edges[ j ];
            }
            polygon.pivot /= polygon.edges.Count;
            if ( triangulateResult ) polygon.Triangulate();
            if ( replaceData ) data.polygons.Add( polygon );            
            resultList.Add( polygon );
        }
        return resultList;
    }

    /// <summary>
    /// Subtracts polygons[1...end] from polygons[ 0 ] into one or more polygons, performing CSG Subtract.
    /// </summary>
    /// <param name="polygons">Two or more operand polygons</param>
    /// <param name="triangulateResult">Call Triangulate on each resulting polygon</param>
    /// <param name="replaceData">If true, will add and replace polygons in this object's data</param>
    /// <returns>Resulting polygon(s)</returns>
    public List<SpriteBreakerPolygon> SubtractPolygons( List<SpriteBreakerPolygon> polygons, bool triangulateResult = true, bool replaceData = true ) {
        // at least two operands
        if ( polygons.Count < 2 ) return null;
        PolyBool polyBool = PolyBool.instance;
        Epsilon.eps = Mathf.Pow( 10, -csgEpsilon );
        SegmentList segments = null;
        for ( int i = 0, np = polygons.Count; i < np; i++ ) {
            SpriteBreakerPolygon polygon = polygons[ i ];
            if ( replaceData && data != null ) data.polygons.Remove( polygon );
            // make polygon
            Polygon poly = new Polygon();
            poly.regions = new List<List<Vector2>>();
            poly.regions.Add( polygon.edges );
            // first segment
            if ( segments == null ) {
                segments = polyBool.segments( poly, csgUpscale );
            } else {
                SegmentList seg = polyBool.segments( poly, csgUpscale );
                CombinedSegmentLists comb = polyBool.combine( segments, seg );
                segments = polyBool.selectDifference( comb );
            }
        }

        // ensure data
        if ( replaceData && data == null ) data = new SpriteBreakerData();

        // convert result to one or more polygons
        List<SpriteBreakerPolygon> resultList = new List<SpriteBreakerPolygon>();
        Polygon result = polyBool.polygon( segments, 1.0f / csgUpscale );
        for ( int i = 0; i < result.regions.Count; i++ ) {
            SpriteBreakerPolygon polygon = new SpriteBreakerPolygon();
            polygon.edges = new List<Vector2>( result.regions[ i ] );
            for ( int j = 0, np = polygon.edges.Count; j < np; j++ ) {
                polygon.pivot += polygon.edges[ j ];
            }
            polygon.pivot /= polygon.edges.Count;
            if ( triangulateResult ) polygon.Triangulate();
            if ( replaceData ) data.polygons.Add( polygon );
            resultList.Add( polygon );
        }
        return resultList;
    }

    /// <summary>
    /// Intersects polygons into one or more polygons, performing CSG Intersect.
    /// </summary>
    /// <param name="polygons">Two or more operand polygons</param>
    /// <param name="triangulateResult">Call Triangulate on each resulting polygon</param>
    /// <param name="replaceData">If true, will add and replace polygons in this object's data</param>
    /// <returns>Resulting polygon(s)</returns>
    public List<SpriteBreakerPolygon> IntersectPolygons( List<SpriteBreakerPolygon> polygons, bool triangulateResult = true, bool replaceData = true ) {
        // at least two operands
        if ( polygons.Count < 2 ) return null;
        PolyBool polyBool = PolyBool.instance;
        Epsilon.eps = Mathf.Pow( 10, -csgEpsilon );
        SegmentList segments = null;
        for ( int i = 0, np = polygons.Count; i < np; i++ ) {
            SpriteBreakerPolygon polygon = polygons[ i ];
            if ( replaceData && data != null ) data.polygons.Remove( polygon );
            // make polygon
            Polygon poly = new Polygon();
            poly.regions = new List<List<Vector2>>();
            poly.regions.Add( polygon.edges );
            // first segment
            if ( segments == null ) {
                segments = polyBool.segments( poly, csgUpscale );
            } else {
                SegmentList seg = polyBool.segments( poly, csgUpscale );
                CombinedSegmentLists comb = polyBool.combine( segments, seg );
                segments = polyBool.selectIntersect( comb );
            }
        }

        // ensure data
        if ( replaceData && data == null ) data = new SpriteBreakerData();

        // convert result to one or more polygons
        List<SpriteBreakerPolygon> resultList = new List<SpriteBreakerPolygon>();
        Polygon result = polyBool.polygon( segments, 1.0f / csgUpscale );
        for ( int i = 0; i < result.regions.Count; i++ ) {
            SpriteBreakerPolygon polygon = new SpriteBreakerPolygon();
            polygon.edges = new List<Vector2>( result.regions[ i ] );
            for ( int j = 0, np = polygon.edges.Count; j < np; j++ ) {
                polygon.pivot += polygon.edges[ j ];
            }
            polygon.pivot /= polygon.edges.Count;
            if ( triangulateResult ) polygon.Triangulate();
            if ( replaceData ) data.polygons.Add( polygon );
            resultList.Add( polygon );
        }
        return resultList;
    }

    /// <summary>
    /// Replaces data with shards generated using current generation parameters
    /// </summary>
    public void Generate( SpriteBreakerData subjectData=null ) {

        // if subjectData isn't given, assume it's this SpriteBreaker
        if ( subjectData == null ) {

            // which data to use as a starting point

            // quad
            if ( generateFrom == BreakerGenerateFrom.Quad ) {
                // if starting from empty, reset to sprite's edges first
                data = new SpriteBreakerData();
                data.InitAsQuad();

            // asset
            } else if ( generateFrom == BreakerGenerateFrom.Asset && dataAsset != null ) {
                data = dataAsset.data.Clone();

            // as is
            } else {
                // new data
                if ( data == null ) data = new SpriteBreakerData();
                if ( data.polygons.Count == 0 ) data.PopulateFromSprite( currentSprite );
            }
            
            subjectData = data;
            
        }

        // remove instances
        if ( subjectData == data ) Clear();

        // dispatch
        SpriteBreakerGenerator.instance.Generate( subjectData, this, subjectData == data );

    }

    #region private

    // deletes a shard, respecting undo in edit mode
    private void DeleteShard ( SpriteBreakerPolygon poly ) {
        if ( poly.gameObject != null ) {
            if ( !Application.isPlaying ) {
                #if UNITY_EDITOR
                Undo.DestroyObjectImmediate( poly.gameObject );
                #endif
            } else {
                DestroyImmediate( poly.gameObject );
            }
        }
    }

    // spawns (or updates) a single shard
    private GameObject SpawnShard( int polyIndex, Transform parent, Sprite baseSprite, SpriteRenderer baseRenderer, Rect uvTransform ) {

        // create gameObject
        GameObject shard;
        SpriteBreakerPolygon poly = data.polygons[ polyIndex ];
        bool flipX = false, flipY = false;
        if ( baseRenderer != null ) {
            flipX = baseRenderer.flipX;
            flipY = baseRenderer.flipY;
        } 
        Vector3 polyPivot = new Vector3( flipX ? ( 1 - poly.pivot.x ) : poly.pivot.x, flipY ? ( 1 - poly.pivot.y ) : poly.pivot.y, 0 );
        Vector3 spritePivot = new Vector3( flipX ? ( baseSprite.rect.width - baseSprite.pivot.x ) : baseSprite.pivot.x, flipY ? ( baseSprite.rect.height - baseSprite.pivot.y ) : baseSprite.pivot.y, 0 );
        bool reusingShard = ( poly.gameObject != null );

        // create game object
        if ( reusingShard ) shard = poly.gameObject;
        else if ( shardPrototype != null ) shard = Instantiate( shardPrototype, parent );
        else {
            shard = new GameObject();
            shard.transform.SetParent( parent );
        }

        // set transform
        poly.startPosition = shard.transform.localPosition = ( new Vector3( baseSprite.rect.width * polyPivot.x, baseSprite.rect.height * polyPivot.y, poly.zOffset ) - spritePivot ) / baseSprite.pixelsPerUnit;
        shard.transform.localRotation = Quaternion.identity;
        shard.transform.localScale = Vector3.one;

        // name, tag
        shard.name = poly.name.Length > 0 ? poly.name : "";
        if ( poly.tag.Length > 0 ) shard.tag = poly.tag;

        // find or add MeshRenderer and MeshFilter
        MeshFilter meshFilter = shard.GetComponent<MeshFilter>();
        if ( meshFilter == null ) meshFilter = shard.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = shard.GetComponent<MeshRenderer>();
        if ( meshRenderer == null ) {
            meshRenderer = shard.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.allowOcclusionWhenDynamic = false;
        }
        poly.cachedRenderer = meshRenderer;

        // set material
        if ( meshRenderer.sharedMaterial == null ) {

            // use from spriteRenderer
            if ( baseRenderer != null ) {
                meshRenderer.sharedMaterial = baseRenderer.sharedMaterial;
            }

            // or make one
            if ( meshRenderer.sharedMaterial == null ) {
                if ( _defaultMaterial == null ) {
                    Shader defaultShader = Shader.Find( "Sprites/Default" );
                    if ( defaultShader == null ) Debug.LogError( "Can't find Sprites/Default shader, add SpriteRenderer with a valid material component to this GameObject" );
                    else _defaultMaterial = new Material( defaultShader );
                }
                if ( _defaultMaterial != null ) meshRenderer.sharedMaterial = _defaultMaterial;
            }
        }

        // apply texture and other params
        if ( baseRenderer != null ) {
            // copy sorting layer
            meshRenderer.sortingLayerID = baseRenderer.sortingLayerID;
            meshRenderer.sortingOrder = baseRenderer.sortingOrder + poly.sortingOrderOffset;
        } else {
            meshRenderer.sortingOrder = poly.sortingOrderOffset;
        }
        materialPropertyBlock.SetColor( _Color, poly.shardColor * shardsColor );
        meshRenderer.SetPropertyBlock( materialPropertyBlock );
        meshRenderer.shadowCastingMode = castShadows;
        meshRenderer.receiveShadows = receiveShadows;

        // get ready
        Mesh mesh = ( meshFilter.sharedMesh == null && Application.isPlaying ) ? meshFilter.mesh : new Mesh();
        mesh.Clear();
        int numVerts = poly.vertices.Length;
        Vector2[] uvs = new Vector2[ numVerts ];
        Vector3[] vertices = new Vector3[ numVerts ];
        Vector2[] colliderPoints = null;
        Vector3[] colliderVertices = null;
        bool convexPositive = false, convexNegative = false;

        // prep collider arrays
        if ( createColliders == CreateColliders.Collider2D ) {
            colliderPoints = new Vector2[ numVerts ];
        } else if ( createColliders == CreateColliders.Collider3D ) {
            colliderVertices = new Vector3[ numVerts * 2 ];
        }
        
        // uv
        (Vector2 spriteUVOrigin, Vector2 spriteUVSize) = (uvTransform.position, uvTransform.size);

        // create mesh and collider shapes
        for ( int i = 0; i < numVerts; i++ ) {
            uvs[ i ] = poly.vertices[ i ] * spriteUVSize + spriteUVOrigin;
            Vector3 vert = poly.vertices[ i ];
            if ( flipX ) vert.x = 1 - vert.x;
            if ( flipY ) vert.y = 1 - vert.y;
            vertices[ i ] = ( ( vert - polyPivot ) * baseSprite.rect.size ) / baseSprite.pixelsPerUnit;

            // 2d collider points
            if ( colliderPoints != null ) colliderPoints[ i ] = vertices[ i ];

            // 3d collider points
            if ( colliderVertices != null ) {
                colliderVertices[ i + numVerts ] = colliderVertices[ i ] = vertices[ i ];
                colliderVertices[ i ].z = -colliderThickness * 0.5f;
                colliderVertices[ i + numVerts ].z = colliderThickness * 0.5f;
                // check if poly is concave
                Vector2 vert1 = poly.vertices[ ( i + 1 ) % numVerts ], vert2 = poly.vertices[ ( i + 2 ) % numVerts ];
                vert = poly.vertices[ i ];
                float crossLen = ( ( vert.x - vert1.x ) * ( vert2.y - vert1.y ) - ( vert.y - vert1.y ) * ( vert2.x - vert1.x ) );
                if ( crossLen < 0 ) {
                    convexNegative = true;
                } else if ( crossLen > 0 ) {
                    convexPositive = true;
                }
            }
        }

        // assign
        mesh.SetVertices( vertices );
        mesh.SetTriangles( poly.triangles, 0 );
        mesh.SetNormals( poly.normals );
        mesh.SetUVs( 0, uvs ); 
        mesh.Optimize();
        if ( Application.isPlaying ) {
            meshFilter.sharedMesh = null;
            meshFilter.mesh = mesh;
        } else {
            meshFilter.mesh = null;
            meshFilter.sharedMesh = mesh;
        }

        // add physics 
        if ( poly.includeInPhysics ) {

            // collider 2d
            if ( createColliders == CreateColliders.Collider2D ) {
                Rigidbody2D body = shard.GetComponent<Rigidbody2D>();
                if ( body == null ) body = shard.AddComponent<Rigidbody2D>();
                PolygonCollider2D coll = shard.GetComponent<PolygonCollider2D>();
                if ( coll == null ) coll = shard.AddComponent<PolygonCollider2D>();

                // assign points
                coll.points = colliderPoints;
                poly.cachedBody = body;

                // set mass
                if ( !body.useAutoMass ) body.mass = massMultiplier > 0 ? (massMultiplier * baseSprite.pixelsPerUnit * poly.Area()) : -massMultiplier;

            // collider 3d
            } else if ( createColliders == CreateColliders.Collider3D ) {
                Rigidbody body = shard.GetComponent<Rigidbody>();
                if ( body == null ) body = shard.AddComponent<Rigidbody>();
                MeshCollider coll = shard.GetComponent<MeshCollider>();
                if ( coll == null ) coll = shard.AddComponent<MeshCollider>();
                coll.convex = true;

                // extruded mesh
                Mesh colliderMesh = new Mesh();

                // convex - make shape extrusion
                if ( !( convexNegative && convexPositive ) ) {
                    // copy triangles
                    int[] colliderTriangles = new int[ poly.triangles.Length * 2 + poly.vertices.Length * 6 ];
                    for ( int i = 0, nt = poly.triangles.Length; i < nt; i++ ) {
                        colliderTriangles[ i ] = poly.triangles[ i ];
                        colliderTriangles[ i + nt ] = poly.triangles[ i ] + numVerts;
                    }
                    // sides
                    for ( int i = 0, triOffset = poly.triangles.Length; i < numVerts; i++ ) {
                        int b = ( i + numVerts ) % numVerts, c = i + numVerts, d = b + numVerts, t = triOffset + i * 6;
                        colliderTriangles[ t ] = i; colliderTriangles[ t + 1 ] = c; colliderTriangles[ t + 2 ] = b;
                        colliderTriangles[ t + 3 ] = b; colliderTriangles[ t + 4 ] = c; colliderTriangles[ t + 5 ] = d;
                    }
                    colliderMesh.vertices = colliderVertices;
                    colliderMesh.triangles = colliderTriangles;

                // concave? make hull
                } else {

                    List<Vector3> hullVerts = new List<Vector3>( numVerts * 2 );
                    List<int> hullTris = new List<int>();
                    ConvexHullCalculator.instance.GenerateHull( colliderVertices, ref hullVerts, ref hullTris );
                    colliderMesh.SetVertices( hullVerts );
                    colliderMesh.SetTriangles( hullTris, 0 );

                }

                coll.sharedMesh = colliderMesh;
                body.mass = massMultiplier > 0 ? (massMultiplier * baseSprite.pixelsPerUnit * poly.Area() * colliderThickness * 0.05f) : -massMultiplier;
                poly.cachedBody = body;

            } else if ( createColliders == CreateColliders.FakePhysics ) {

                poly.fakePhysicsMass = massMultiplier > 0 ? (massMultiplier * baseSprite.pixelsPerUnit * poly.Area() * 0.1f) : (-massMultiplier);
                poly.cachedBody = null;

            }
        }

        #if UNITY_EDITOR
        // in edit mode, save undo
        if ( !Application.isPlaying ) {
            if ( reusingShard )
                Undo.RegisterCompleteObjectUndo( shard, "Updated shard" );
            else
                Undo.RegisterCreatedObjectUndo( shard, "Added shard" );
        }
        #endif

        return shard;
    }

    protected void FinishUp (bool noRestart=false) {
        // call event
        OnTimeToLiveExpired.Invoke( gameObject );
        _simulating = false;
        shardsColor.a = _initialFadeAlpha;

        // final action
        if ( endAction == EndAction.DestroyObject ) {
            Destroy( gameObject );
        } else if ( endAction == EndAction.DestroyParent ) {
            Destroy( shardsParent != null ? shardsParent.gameObject : transform.parent.gameObject );
        } else if ( endAction == EndAction.DeactivateShards ) {
            for ( int i = 0, np = data.polygons.Count; i < np; i++ ) {
                SpriteBreakerPolygon poly = data.polygons[ i ];
                if ( poly.gameObject != null ) poly.gameObject.SetActive( false );
            }
        } else if ( endAction == EndAction.Restart && !noRestart ) {
            RestartSimulation();
        } else if ( endAction == EndAction.Reset ) {
            Clear();
        } else if ( endAction == EndAction.ClearDataAndReset ) {
            Clear();
            data = null;
        }
    }

    protected void ApplyShardInitialImpulse( SpriteBreakerPolygon poly ) {
        if ( poly == null || poly.gameObject == null ) return;
        if ( createColliders == CreateColliders.Collider2D ) {
            // body is Rigidbody2D
            Rigidbody2D body = (Rigidbody2D) poly.cachedBody;
            if ( body != null ) {
                float value = initialRadialImpulse * ( 1 + UnityEngine.Random.Range( -initialRadialImpulsePlusMinus, initialRadialImpulsePlusMinus ) * 0.01f );
                body.linearVelocity = Vector2.zero; body.angularVelocity = 0;
                body.AddForce( poly.gameObject.transform.TransformDirection( poly.gameObject.transform.localPosition.normalized ) * value, ForceMode2D.Impulse );
                value = UnityEngine.Random.Range( -initialLinearImpulsePlusMinus, initialLinearImpulsePlusMinus );
                Vector3 value3 = new Vector3( 
                    initialLinearImpulse.x * ( 1 + value * 0.01f ) * Mathf.Sign( UnityEngine.Random.value - 0.5f ),
                    initialLinearImpulse.y * ( 1 + value * 0.01f ) * Mathf.Sign( UnityEngine.Random.value - 0.5f ),
                    initialLinearImpulse.z * ( 1 + value * 0.01f ) * Mathf.Sign( UnityEngine.Random.value - 0.5f ) );
                body.AddForce( poly.gameObject.transform.TransformDirection( value3 ), ForceMode2D.Impulse );
                value = UnityEngine.Random.Range( -initialRotationalImpulsePlusMinus, initialRotationalImpulsePlusMinus );
                value = initialRotationalImpulse.z * ( 1 + value * 0.01f ) * Mathf.Sign( UnityEngine.Random.value - 0.5f );
                body.AddTorque( value, ForceMode2D.Impulse );
            }
        } else if ( createColliders == CreateColliders.Collider3D ) {
            // body is Rigidbody
            Rigidbody body = (Rigidbody) poly.cachedBody;
            if ( body != null ) {
                float value = initialRadialImpulse * ( 1 + UnityEngine.Random.Range( -initialRadialImpulsePlusMinus, initialRadialImpulsePlusMinus ) * 0.01f );
                body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero;
                body.AddForce( poly.gameObject.transform.TransformDirection( poly.gameObject.transform.localPosition.normalized ) * value, ForceMode.Impulse );
                value = UnityEngine.Random.Range( -initialLinearImpulsePlusMinus, initialLinearImpulsePlusMinus );
                Vector3 value3 = new Vector3( 
                    initialLinearImpulse.x * ( 1 + value * 0.01f ) * Mathf.Sign( UnityEngine.Random.value - 0.5f ),
                    initialLinearImpulse.y * ( 1 + value * 0.01f ) * Mathf.Sign( UnityEngine.Random.value - 0.5f ),
                    initialLinearImpulse.z * ( 1 + value * 0.01f ) * Mathf.Sign( UnityEngine.Random.value - 0.5f ) );
                body.AddForce( poly.gameObject.transform.TransformDirection( value3 ), ForceMode.Impulse );
                value = UnityEngine.Random.Range( -initialRotationalImpulsePlusMinus, initialRotationalImpulsePlusMinus );
                value3.x = initialRotationalImpulse.x * ( 1 + value * 0.01f ) * Mathf.Sign( UnityEngine.Random.value - 0.5f );
                value = UnityEngine.Random.Range( -initialRotationalImpulsePlusMinus, initialRotationalImpulsePlusMinus );
                value3.y = initialRotationalImpulse.y * ( 1 + value * 0.01f ) * Mathf.Sign( UnityEngine.Random.value - 0.5f );
                value = UnityEngine.Random.Range( -initialRotationalImpulsePlusMinus, initialRotationalImpulsePlusMinus );
                value3.z = initialRotationalImpulse.z * ( 1 + value * 0.01f ) * Mathf.Sign( UnityEngine.Random.value - 0.5f );
                body.AddTorque( value3, ForceMode.Impulse );
            }
        } else if ( createColliders == CreateColliders.FakePhysics ) {
            // fake physics
            float value = initialRadialImpulse * ( 1 + UnityEngine.Random.Range( -initialRadialImpulsePlusMinus, initialRadialImpulsePlusMinus ) * 0.01f );
            poly.fakePhysicsVelocity = poly.gameObject.transform.localPosition.normalized * value / poly.fakePhysicsMass;
            value = UnityEngine.Random.Range( -initialLinearImpulsePlusMinus, initialLinearImpulsePlusMinus );
            Vector3 value3 = new Vector3( 
                initialLinearImpulse.x * ( 1 + value * 0.01f ) * Mathf.Sign( UnityEngine.Random.value - 0.5f ),
                initialLinearImpulse.y * ( 1 + value * 0.01f ) * Mathf.Sign( UnityEngine.Random.value - 0.5f ),
                initialLinearImpulse.z * ( 1 + value * 0.01f ) * Mathf.Sign( UnityEngine.Random.value - 0.5f ) );
            poly.fakePhysicsVelocity += value3 / poly.fakePhysicsMass;
            value = UnityEngine.Random.Range( -initialRotationalImpulsePlusMinus, initialRotationalImpulsePlusMinus );
            value3.x = initialRotationalImpulse.x * ( 1 + value * 0.01f ) * Mathf.Sign( UnityEngine.Random.value - 0.5f );
            value = UnityEngine.Random.Range( -initialRotationalImpulsePlusMinus, initialRotationalImpulsePlusMinus );
            value3.y = initialRotationalImpulse.y * ( 1 + value * 0.01f ) * Mathf.Sign( UnityEngine.Random.value - 0.5f );
            value = UnityEngine.Random.Range( -initialRotationalImpulsePlusMinus, initialRotationalImpulsePlusMinus );
            value3.z = initialRotationalImpulse.z * ( 1 + value * 0.01f ) * Mathf.Sign( UnityEngine.Random.value - 0.5f );
            poly.fakePhysicsRotationalVelocity += value3 / poly.fakePhysicsMass;
        }
    }
    
    // returns true if any of the polygons have shards attached
    private bool _HasShards () {
        if ( data == null ) return false;
        for ( int i = 0, np = data.polygons.Count; i < np; i++ ) {
            SpriteBreakerPolygon poly = data.polygons[ i ];
            if ( poly.gameObject != null ) return true;
        }
        return false;
    }

    // returns true if capable of breaking
    private bool _CanBreak () {
        bool hasSprite = ( sprite != null );
        if ( !hasSprite ) {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            hasSprite = ( sr != null && sr.sprite != null );
        }
        return hasSprite;
    }

    // returns effective sprite
    private Sprite _GetSprite() {
        if ( sprite != null ) return sprite;
        SpriteRenderer rs = gameObject.GetComponent<SpriteRenderer>();
        if ( rs != null ) return rs.sprite;
        return null;
    }

    // returns shards as gameObjects array
    private GameObject[] _GetShards() {
        if ( data == null || data.polygons.Count == 0 ) return null;
        int numPolys = data.polygons.Count;
        GameObject[] ss = new GameObject[ numPolys ];
        for ( int i = 0; i < numPolys; i++ ) ss[ i ] = data.polygons[ i ].gameObject;
        return ss;
    }

    private void UpdateMaterialPropertyBlock ( Renderer rend=null ) {
        if ( materialPropertyBlock == null ) materialPropertyBlock = new MaterialPropertyBlock();
        if ( rend == null ) rend = GetComponent<Renderer>();
        if ( rend == null ) {
            materialPropertyBlock.SetTexture( "_MainTex", sprite?.texture );
        } else {
            rend.GetPropertyBlock( materialPropertyBlock );
        }
    }

    public void ApplyCastShadowsMode () {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if ( sr != null ) { 
            sr.shadowCastingMode = castShadows;
            sr.receiveShadows = receiveShadows;
        }
        if ( data == null ) return;
        foreach ( SpriteBreakerPolygon poly in data.polygons ) {
            if ( poly.cachedRenderer != null ) {
                poly.cachedRenderer.shadowCastingMode = castShadows;
                poly.cachedRenderer.receiveShadows = receiveShadows;
            }
        }
    }
    
    // init
    private void Awake() {
        
        // auto break
        if ( autoBreakOnAwake ) {
            Break();
        }
        
        // update colors and texture on all attached shards
        if ( data != null ) {
            UpdateMaterialPropertyBlock();
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if ( sr != null ) {
                sr.shadowCastingMode = castShadows;
                sr.receiveShadows = receiveShadows;
            }
            for ( int i = 0, np = data.polygons.Count; i < np; i++ ) {
                SpriteBreakerPolygon poly = data.polygons[ i ];
                if ( poly.gameObject == null ) continue;
                poly.UpdateRenderer( this, materialPropertyBlock );
                if ( poly.cachedRenderer != null ) {
                    poly.cachedRenderer.shadowCastingMode = castShadows;
                    poly.cachedRenderer.receiveShadows = receiveShadows;
                }
            }
            _colorPreviousUpdate = shardsColor;
        } else {
            data = new SpriteBreakerData();
            data.InitAsQuad();
        }
    }

    // called every frame
    private void Update() {
        if ( data == null || data.polygons.Count == 0 ) return;

        // time to live
        float timeSinceShatter = Time.time - timeBroken;
        bool runPhysics = (createColliders == CreateColliders.FakePhysics);

        // what needs to be done this frame
        bool updateColors = updateShardsColor; // && _colorPreviousUpdate != shardsColor;
        if ( _simulating && fadeAfter > Mathf.Epsilon && timeSinceShatter > fadeAfter ) {
            shardsColor.a = Mathf.Lerp( _initialFadeAlpha, 0, ( timeSinceShatter - fadeAfter ) / ( timeToLive - fadeAfter ) );
            updateColors = true;
        }

        // loop over polys
        if ( runPhysics || updateColors ) {
            for ( int i = 0, np = data.polygons.Count; i < np; i++ ) {
                SpriteBreakerPolygon poly = data.polygons[ i ];
                // have gameObject
                if ( poly.gameObject != null && poly.gameObject.activeSelf ) {
                    
                    if ( runPhysics && poly.includeInPhysics ) poly.UpdateFakePhysics( fakeGravity );
                    if ( updateColors ) poly.UpdateRenderer( this, materialPropertyBlock );
                }
            }
            _colorPreviousUpdate = shardsColor;
        }

        // if done, wrap up
        if ( _simulating && timeToLive > Mathf.Epsilon && timeSinceShatter > timeToLive ) {
            FinishUp();
        }

    }

    /// <summary>
    /// Returns first polygon whose name matches parameter
    /// </summary>
    /// <param name="name">name to find</param>
    /// <returns></returns>
    public SpriteBreakerPolygon GetPolygon( String name ) {
        if ( data == null ) return null;
        for ( int i = 0, np = data.polygons.Count; i < np; i++ ) {
            SpriteBreakerPolygon p = data.polygons[ i ];
            if ( p.name == name ) return p;
        }
        return null;
    }

#if UNITY_EDITOR
    // Draws preview of shards    
    private void OnDrawGizmos() {

        // use asset or data
        SpriteBreakerData useData = data;
        bool usingAsset = false;
        if ( ( useData == null || useData.polygons.Count == 0 ) && dataAsset ) {
            useData = dataAsset.data;
            usingAsset = true;
        }

        // nothing to draw
        if ( useData == null ) return;

        // setup
        Transform parent = shardsParent ? shardsParent : transform;
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Sprite baseSprite = ( sprite != null ? sprite : ( spriteRenderer ? spriteRenderer.sprite : null ) );
        bool flipX = false, flipY = false;
        Vector3 spritePivot = Vector3.zero;
        if ( baseSprite != null ) {
            flipX = spriteRenderer != null && spriteRenderer.flipX;
            flipY = spriteRenderer != null && spriteRenderer.flipY;
            spritePivot = new Vector3( flipX ? ( baseSprite.rect.width - baseSprite.pivot.x ) : baseSprite.pivot.x, flipY ? ( baseSprite.rect.height - baseSprite.pivot.y ) : baseSprite.pivot.y, 0 );
            Handles.color = Color.Lerp( gizmoColor, Color.white, UnityEditor.Selection.Contains( gameObject ) ? 0.5f : 0 );
        }

        // for each poly
        for ( int i = 0, np = useData.polygons.Count; i < np; i++ ) {
            SpriteBreakerPolygon poly = useData.polygons[ i ];
            if ( poly == null || poly.edges.Count <= 2 ) continue;

            // update renderer color
            if ( materialPropertyBlock == null ) this.UpdateMaterialPropertyBlock( spriteRenderer );
            if ( poly.gameObject != null && updateShardsColor ) poly.UpdateRenderer( this, materialPropertyBlock );

            // draw shard preview 
            if ( drawEditorGizmos && baseSprite != null ) {
                Vector3 prevPoint = new Vector3();
                for ( int j = 0, cnt = poly.edges.Count; j <= cnt; j++ ) {
                    Vector3 vert = poly.edges[ j % cnt ]; vert.z = 0;
                    if ( flipX ) vert.x = 1 - vert.x;
                    if ( flipY ) vert.y = 1 - vert.y;
                    vert.x *= baseSprite.rect.width;
                    vert.y *= baseSprite.rect.height;
                    vert -= spritePivot;
                    vert.x /= baseSprite.pixelsPerUnit;
                    vert.y /= baseSprite.pixelsPerUnit;
                    vert = parent.TransformPoint( vert );
                    if ( j > 0 ) {
                        if ( usingAsset ) Handles.DrawDottedLine( prevPoint, vert, 1.0f );
                        else Handles.DrawLine( prevPoint, vert );
                    }
                    prevPoint = vert;
                }
            }
        }

    }

#endif
#endregion
}

// event for individual shard spawn callback
[Serializable]
public class ShardEvent : UnityEvent<GameObject, SpriteBreakerPolygon, SpriteBreaker> { }

// event for time expired callback
[Serializable]
public class BreakEvent : UnityEvent<GameObject> { }

