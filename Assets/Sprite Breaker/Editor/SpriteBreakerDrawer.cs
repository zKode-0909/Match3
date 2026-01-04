using UnityEngine;
using UnityEditor;

// inspector for SpriteBreaker component
[CustomEditor( typeof( SpriteBreaker ) )]
[CanEditMultipleObjects]
public class SpriteBreakerDrawer : Editor {

    private SerializedProperty
        sprite,
        shardPrototype,
        ShardCreated,
        shardsParent,
        autoBreakOnAwake,
        drawEditorGizmos,
        gizmoColor,
        updateShardsColor,
        shardsColor, castShadows, receiveShadows,
        disableRendererOnBreak,
        createColliders,
        dataAsset,
        colliderThickness, massMultiplier, fakeGravity,
        initialRadialImpulse, initialRadialImpulsePlusMinus,
        initialLinearImpulse, initialLinearImpulsePlusMinus,
        initialRotationalImpulse, initialRotationalImpulsePlusMinus,
        timeToLive, fadeAfter, endAction, TimeToLiveExpired,
        csgUpscale, csgEpsilon,
        generateType, generateSeed, generateFrom, generateRandomness, generateAngle, generateSpacing, generateFrequency, generateOrigin;

    private static bool
        _showEvents = false,
        _showGenerate = false,
        _showGeneratePreview = true,
        _showData = true,
        _showAppearance = false,
        _showPhysics = false,
        _showAdvanced = false;
    private static SpriteBreakerData copiedData = null; // copy/paste
    private Vector2 _scrollPos = new Vector2(); // used by Data foldout
    private SpriteBreakerData previewGenerateData = null;
    private Random.State previewRandomState;
    private bool _settingGenerateOrigin = false;

    private GUIContent
        spriteFieldLabel = new GUIContent( "Sprite" ),
        spriteFieldLabelRenderer = new GUIContent( "Sprite (using Sprite Renderer)" ),
        spriteFieldLabelOverride = new GUIContent( "Sprite (overriding Sprite Renderer)" ),
        autoBreakOnAwakeLabel = new GUIContent( "Break on Awake"),
        labelBreak = new GUIContent( "Break", "Generate shards" ),
        labelClear = new GUIContent( "Clear", "Delete generated shards" ),
        labelBreakerData = new GUIContent( "Breaker Data" ),
        labelEdit = new GUIContent( "Edit Data", "Edit shard data in an editor" ),
        labelCopy = new GUIContent( "Copy" ),
        labelPaste = new GUIContent( "Paste" ),
        labelClearData = new GUIContent( "Clear Data" ),
        labelEvents = new GUIContent( "Events" ),
        labelEmpty = new GUIContent( "No data" ),
        labelUpdateShardsColor = new GUIContent( "Keep shards color updated" ),
        labelCastShadows = new GUIContent( "Shadow casting mode"),
        labelReceiveShadows = new GUIContent( "Receive shadows"),
        labelDisableRendererOnBreak = new GUIContent( "Disable SpriteRenderer on break", "Disabled/enables renderer attached to this gameObject when Break/Clear is performed" ),
        labelMultipleSelection = new GUIContent( "- multiple selection -" ),
        labelAppearance = new GUIContent( "Appearance" ),
        labelAssetHeader = new GUIContent( "Asset:" ),
        labelAssetLoad = new GUIContent( "Load Data", "Replace shard data with data in asset" ),
        labelAssetSave = new GUIContent( "Save to Asset", "Save current data to asset" ),
        labelAssetReplace = new GUIContent( "Replace Asset", "Replace current asset's data" ),
        labelPhysics = new GUIContent( "Simulation" ),
        labelGenerate = new GUIContent( "Generate" ),
        labelRegenerate = new GUIContent( "Generate Shards" ),
        labelGenerateSeed = new GUIContent( "Seed" ),
        labelGenerateFrom = new GUIContent( "Use" ),
        labelGenerateRandomness = new GUIContent( "Randomness" ),
        labelGenerateAngle = new GUIContent( "Angle" ),
        labelGenerateFrequency = new GUIContent( "Frequency" ),
        labelGenerateSpacing = new GUIContent( "Spacing" ),
        labelGenerateOrigin = new GUIContent( "Origin" ),
        labelPreview = new GUIContent( "Preview:" ),
        labelAdvanced = new GUIContent( "Advanced" ),
        labelColliders = new GUIContent( "Physics" ),
        labelGravity = new GUIContent( "Gravity" ),
        labelImpulse = new GUIContent( "Initial impulse:" ),
        labelRadial = new GUIContent( "Radial" ),
        labelLinear = new GUIContent( "Linear" ),
        labelRotational = new GUIContent( "Rotational" ),
        labelPlusMinus = new GUIContent( "±%" );

    GUIStyle
        hoverButton,
        boldLabel, leftAlign,
        fatButton, editButton, greyLabel,
        buttonLeft, buttonMid, buttonRight;

    // drawer init
    private void OnEnable () {
        sprite = serializedObject.FindProperty( "sprite" );
        shardPrototype = serializedObject.FindProperty( "shardPrototype" );
        shardsParent = serializedObject.FindProperty( "shardsParent" );
        autoBreakOnAwake = serializedObject.FindProperty( "autoBreakOnAwake" );
        ShardCreated = serializedObject.FindProperty( "OnShardCreated" );
        drawEditorGizmos = serializedObject.FindProperty( "drawEditorGizmos" );
        gizmoColor = serializedObject.FindProperty( "gizmoColor" );
        updateShardsColor = serializedObject.FindProperty( "updateShardsColor" );
        shardsColor = serializedObject.FindProperty( "shardsColor" );
        castShadows = serializedObject.FindProperty( "castShadows" );
        receiveShadows = serializedObject.FindProperty( "receiveShadows" );
        disableRendererOnBreak = serializedObject.FindProperty( "disableRendererOnBreak" );
        createColliders = serializedObject.FindProperty( "createColliders" );
        colliderThickness = serializedObject.FindProperty( "colliderThickness" );
        massMultiplier = serializedObject.FindProperty( "massMultiplier" );
        initialRadialImpulse = serializedObject.FindProperty( "initialRadialImpulse" );
        initialRadialImpulsePlusMinus = serializedObject.FindProperty( "initialRadialImpulsePlusMinus" );
        initialLinearImpulse = serializedObject.FindProperty( "initialLinearImpulse" );
        initialLinearImpulsePlusMinus = serializedObject.FindProperty( "initialLinearImpulsePlusMinus" );
        initialRotationalImpulse = serializedObject.FindProperty( "initialRotationalImpulse" );
        initialRotationalImpulsePlusMinus = serializedObject.FindProperty( "initialRotationalImpulsePlusMinus" );
        fakeGravity = serializedObject.FindProperty( "fakeGravity" );
        timeToLive = serializedObject.FindProperty( "timeToLive" );
        fadeAfter = serializedObject.FindProperty( "fadeAfter" );
        endAction = serializedObject.FindProperty( "endAction" );
        TimeToLiveExpired = serializedObject.FindProperty( "OnTimeToLiveExpired" );
        dataAsset = serializedObject.FindProperty( "dataAsset" );
        generateType = serializedObject.FindProperty( "generateType" );
        generateSeed = serializedObject.FindProperty( "generateSeed" );
        generateFrom = serializedObject.FindProperty( "generateFrom" );
        generateRandomness = serializedObject.FindProperty( "generateRandomness" );
        generateAngle = serializedObject.FindProperty( "generateAngle" );
        generateSpacing = serializedObject.FindProperty( "generateSpacing" );
        generateFrequency = serializedObject.FindProperty( "generateFrequency" );
        generateOrigin = serializedObject.FindProperty( "generateOrigin" );
        csgUpscale = serializedObject.FindProperty( "csgUpscale" );
        csgEpsilon = serializedObject.FindProperty( "csgEpsilon" );
    }

    // main
    public override void OnInspectorGUI() {

        serializedObject.Update();

        // init styles
        if ( hoverButton == null ) {
            hoverButton = new GUIStyle( GUI.skin.label ) {
                active = GUI.skin.button.active, 
                onHover = GUI.skin.button.onHover
            };
            hoverButton.onHover.textColor = Color.red;
            hoverButton.alignment = TextAnchor.MiddleLeft;
            leftAlign = new GUIStyle( GUI.skin.label ) {
                alignment = TextAnchor.MiddleLeft
            };
            boldLabel = new GUIStyle( GUI.skin.label ) {
                fontStyle = FontStyle.Bold
            };
            fatButton = new GUIStyle( GUI.skin.button ) {
                margin = new RectOffset( 8, 8, 8, 8 ), 
                fontStyle = FontStyle.Bold, 
                fontSize = 12, 
                alignment = TextAnchor.MiddleCenter
            };
            editButton = new GUIStyle( GUI.skin.button ) {
                fontStyle = FontStyle.Bold
            };
            buttonLeft = GUI.skin.GetStyle( "buttonleft" );
            buttonMid = GUI.skin.GetStyle( "buttonmid" );
            buttonRight = GUI.skin.GetStyle( "buttonright" );
            greyLabel = new GUIStyle( GUI.skin.label ) {
                fontStyle = FontStyle.Italic, 
                normal = {textColor = new Color( 0.4f, 0.4f, 0.4f )}
            };
            previewRandomState = UnityEngine.Random.state;
        }

        // if hit undo, force redraw on generator
        if ( Event.current.type == EventType.ValidateCommand && Event.current.commandName == "UndoRedoPerformed" ) {
            Event.current.Use();
            previewGenerateData = null;
            Repaint();
        }

        // gather properties/actions
        bool single = !serializedObject.isEditingMultipleObjects;
        SpriteBreaker breaker = (SpriteBreaker) serializedObject.targetObject;
        SpriteRenderer spriteRenderer = breaker.GetComponent<SpriteRenderer>();
        bool hasOwnSprite = ( sprite.objectReferenceValue != null );
        bool hasRendererSprite = ( spriteRenderer != null && spriteRenderer.sprite != null );
        bool hasSprite = ( hasOwnSprite || hasRendererSprite );
        bool canEdit = ( single && hasSprite );
        bool canCopy = ( single && breaker.data != null && breaker.data.polygons.Count > 0 );
        bool canPaste = ( SpriteBreakerDrawer.copiedData != null );
        bool canShatter = false, canUnshatter = false;
        bool shadowModeChanged = false;
        Sprite actualSprite = hasOwnSprite ? breaker.sprite : ( hasRendererSprite ? spriteRenderer.sprite : null );
        Object[] selecteds = serializedObject.targetObjects;
        for ( int i = 0; i < selecteds.Length; i++ ) {
            SpriteBreaker obj = (SpriteBreaker) selecteds[ i ];
            canShatter |= obj.canBreak;
            canUnshatter |= obj.hasShards;
            if ( canShatter && canUnshatter ) break;
        }

        // Sprite
        GUIContent label = spriteFieldLabel;
        EditorGUIUtility.labelWidth = 180;
        EditorGUI.BeginChangeCheck();
        if ( single ) {
            if ( !hasOwnSprite && hasRendererSprite ) label = spriteFieldLabelRenderer;
            else if ( hasOwnSprite && hasRendererSprite ) label = spriteFieldLabelOverride;
        }
        EditorGUILayout.PropertyField( sprite, label );
        // if sprite changed
        if ( EditorGUI.EndChangeCheck() ) {
            previewGenerateData = null; // refresh generate preview
            UpdateEditorWindow();
        }

        // Shard prototype
        EditorGUILayout.PropertyField( shardPrototype );

        // Shards parent
        EditorGUILayout.PropertyField( shardsParent );

        // break on awake
        EditorGUILayout.PropertyField( autoBreakOnAwake, autoBreakOnAwakeLabel );

        // Box
        Rect rect = EditorGUILayout.BeginHorizontal( fatButton, GUILayout.Height( 30 ) );
        EditorGUI.DrawRect( rect, Color.grey );

        // Break button
        EditorGUI.BeginDisabledGroup( !canShatter );
        if ( GUILayout.Button( labelBreak, fatButton, GUILayout.Height( 20 ) ) ) Break( true );
        EditorGUI.EndDisabledGroup();

        // Clear button
        EditorGUI.BeginDisabledGroup( !canUnshatter );
        if ( GUILayout.Button( labelClear, fatButton, GUILayout.Height( 20 ) ) ) Break( false );
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();

        // asset 
        SpriteBreakerDataAsset asset = (SpriteBreakerDataAsset) dataAsset.objectReferenceValue;

        // Data foldout
        labelBreakerData.text = "Breaker Data";
        bool dataWasEmpty = ( breaker.data == null || breaker.data.polygons.Count == 0 );
        if ( !dataWasEmpty ) labelBreakerData.text += " - " + breaker.data.polygons.Count + " poly" + ( breaker.data.polygons.Count > 1 ? "s" : "" );
        else if ( breaker.dataAsset != null ) labelBreakerData.text += " - " + breaker.dataAsset.data.polygons.Count + " poly" + ( breaker.dataAsset.data.polygons.Count > 1 ? "s from asset" : "" );
        if ( _showData = EditorGUILayout.BeginFoldoutHeaderGroup( _showData, labelBreakerData ) ) {

            // Edit data button
            GUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup( !canEdit );
            if ( GUILayout.Button( labelEdit, editButton ) ) {
                SpriteBreakerEditor editor = (SpriteBreakerEditor) EditorWindow.GetWindow( typeof( SpriteBreakerEditor ) );
                if ( dataWasEmpty ) {
                    Undo.RegisterCompleteObjectUndo( breaker, "Create breaker data" );
                    if ( breaker.dataAsset != null ) breaker.data = breaker.dataAsset.data.Clone();
                    else breaker.data = new SpriteBreakerData();
                    EditorUtility.SetDirty( breaker );
                }
                // show editor
                editor.Show();
                if ( breaker.data.polygons.Count == 0 && actualSprite != null ) breaker.data.PopulateFromSprite( actualSprite );
                editor.SetBreaker( breaker );
                // populate with sprite
                editor.Repaint();
            }
            EditorGUI.EndDisabledGroup();

            // Clear data button
            if ( GUILayout.Button( labelClearData ) ) {
                ClearData();
            }

            GUILayout.FlexibleSpace();

            // Copy data button
            EditorGUI.BeginDisabledGroup( !canCopy );
            if ( GUILayout.Button( labelCopy ) ) {
                SpriteBreakerDrawer.copiedData = breaker.data.Clone();
                Repaint();
            }
            EditorGUI.EndDisabledGroup();

            // Paste data button
            EditorGUI.BeginDisabledGroup( !canPaste );
            if ( GUILayout.Button( labelPaste ) ) {
                PasteData();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.EndHorizontal();

            // single object
            if ( single ) {

                // have data
                int numPolys;
                if ( breaker.data != null && ( numPolys = breaker.data.polygons.Count ) > 0 ) {
                    EditorGUILayout.LabelField( "Data: " + numPolys + " polygon" + ( numPolys != 1 ? "s" : "" ), boldLabel );
                    GUIContent text = new GUIContent();

                    // scroll view if more than 5 shards
                    if ( numPolys > 5 ) _scrollPos = GUILayout.BeginScrollView( _scrollPos, GUILayout.Height( 100 ), GUILayout.ExpandWidth( true ) );

                    // for each polygon
                    for ( int i = 0; i < numPolys; i++ ) {
                        SpriteBreakerPolygon poly = breaker.data.polygons[ i ];
                        text.text = ( poly.name.Length > 0 ? poly.name : ( "Poly " + ( i + 1 ).ToString() ) ) + ": " +
                            poly.edges.Count + " edges" +
                            ( poly.triangles != null ? ( ", " + poly.triangles.Length / 3 + " tris" ) : ( "" ) );

                        Rect row = EditorGUILayout.BeginHorizontal( GUILayout.Height( 20 ) );

                        // click to select/edit
                        if ( GUILayout.Button( text, hoverButton, GUILayout.Height( 16 ), GUILayout.Width( EditorGUIUtility.currentViewWidth - 120 ) ) ) {
                            SpriteBreakerEditor editor = (SpriteBreakerEditor) EditorWindow.GetWindow( typeof( SpriteBreakerEditor ) );
                            editor.SetBreaker( breaker, i );
                        }
                        // object field
                        if ( poly.gameObject != null ) {
                            EditorGUI.BeginDisabledGroup( true );
                            EditorGUILayout.ObjectField( poly.gameObject, typeof( GameObject ), true, GUILayout.Height( 16 ), GUILayout.ExpandWidth( true ) );
                            EditorGUI.EndDisabledGroup();
                        }
                        EditorGUILayout.EndHorizontal();
                        row.width -= 100;
                        EditorGUIUtility.AddCursorRect( row, MouseCursor.Link );

                    }

                    // end scrollview
                    if ( numPolys > 5 ) GUILayout.EndScrollView();

                } else {
                    EditorGUILayout.LabelField( labelEmpty, boldLabel );
                }

                // multiple objects
            } else {

                // check if data is all empty
                foreach ( SpriteBreaker obj in serializedObject.targetObjects ) {
                    if ( obj.data != null && obj.data.polygons.Count > 0 ) {
                        // not all empty
                        EditorGUILayout.LabelField( labelMultipleSelection, greyLabel );
                        break;
                    }
                }

            }

            int undoGroup = 0;
            string undoString;

            // Asset operations
            EditorGUILayout.LabelField( labelAssetHeader, boldLabel );
            EditorGUILayout.BeginHorizontal();

            // Load asset into object
            EditorGUI.BeginDisabledGroup( asset == null );
            if ( GUILayout.Button( labelAssetLoad ) ) {
                undoGroup = Undo.GetCurrentGroup();
                undoString = "Replace data with asset";
                Object[] objs = serializedObject.targetObjects;
                for ( int i = 0, n = objs.Length; i < n; i++ ) {
                    Undo.RegisterCompleteObjectUndo( objs[ i ], undoString );
                    EditorUtility.SetDirty( objs[ i ] );
                    SpriteBreaker shat = (SpriteBreaker) objs[ i ];
                    shat.data = asset.data.Clone();
                }
                Undo.CollapseUndoOperations( undoGroup );
                UpdateEditorWindow();
                previewGenerateData = null; // refresh preview too
            }
            EditorGUI.EndDisabledGroup();

            // single save/replace
            if ( single ) {
                SpriteBreaker shat = (SpriteBreaker) serializedObject.targetObject;
                EditorGUI.BeginDisabledGroup( shat.data == null || shat.data.polygons.Count == 0 );
                if ( GUILayout.Button( asset == null ? labelAssetSave : labelAssetReplace ) ) {
                    undoString = asset == null ? "Save data to asset" : "Replace asset data";
                    undoGroup = Undo.GetCurrentGroup();
                    EditorUtility.SetDirty( shat );
                    if ( asset == null ) {
                        asset = ScriptableObject.CreateInstance<SpriteBreakerDataAsset>();
                        asset.data = shat.data.Clone();
                        Undo.RecordObject( asset, undoString );
                        Undo.RegisterCompleteObjectUndo( shat, undoString );
                        shat.dataAsset = asset;
                        EditorUtility.SetDirty( shat );
                    } else {
                        Undo.RecordObject( asset, undoString );
                        asset.data = shat.data.Clone();
                    }
                    // save
                    string path = AssetDatabase.GetAssetPath( asset );
                    if ( path != null && path.Length > 0 ) {
                        AssetDatabase.SaveAssets();
                    } else {
                        string assetName = shat.gameObject.name.Length > 0 ? shat.gameObject.name : "GameObject";
                        AssetDatabase.CreateAsset( asset, AssetDatabase.GenerateUniqueAssetPath( "Assets/sprite-breaker-" + assetName + ".asset" ) );
                    }
                    EditorUtility.SetDirty( asset );
                    Undo.CollapseUndoOperations( undoGroup );
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.ObjectField( dataAsset, typeof( SpriteBreakerDataAsset ), GUIContent.none, GUILayout.MinWidth( 40 ), GUILayout.ExpandWidth( true ) );
            EditorGUILayout.EndHorizontal();
            if ( EditorGUI.EndChangeCheck() ) {
                undoGroup = Undo.GetCurrentGroup();
                undoString = "Set data asset";
                Object[] objs = serializedObject.targetObjects;
                if ( dataAsset.objectReferenceValue != null ) {
                    asset = (SpriteBreakerDataAsset) dataAsset.objectReferenceValue;
                    for ( int i = 0, n = objs.Length; i < n; i++ ) {
                        Undo.RegisterCompleteObjectUndo( objs[ i ], undoString );
                        EditorUtility.SetDirty( objs[ i ] );
                        SpriteBreaker shat = (SpriteBreaker) objs[ i ];
                        // if had no data, copy from asset
                        if ( shat.data == null || shat.data.polygons.Count == 0 ) {
                            shat.data = asset.data.Clone();
                        }
                    }
                }
                Undo.CollapseUndoOperations( undoGroup );
            }
            EditorGUILayout.Space();
        }
        EditorGUILayout.EndFoldoutHeaderGroup(); // end data foldout        

        // Generate foldout
        if ( _showGenerate = EditorGUILayout.BeginFoldoutHeaderGroup( _showGenerate, labelGenerate ) ) {

            Rect generateParamsBox = EditorGUILayout.BeginVertical();
            GUI.changed = false;
            EditorGUI.BeginChangeCheck();

            // Generate type - future
            bool generateTypeDiff = generateType.hasMultipleDifferentValues;
            /*
            EditorGUILayout.BeginHorizontal();
            if ( GUILayout.Toggle( ( !generateTypeDiff && generateType.intValue == (int) SpriteBreaker.ShatterGenerateType.Radial ), "Radial", buttonLeft ) ) {
                generateType.intValue = (int) SpriteBreaker.ShatterGenerateType.Radial;
            }
            EditorGUI.BeginDisabledGroup( true ); // future
            if ( GUILayout.Toggle( ( !generateTypeDiff && generateType.intValue == (int) SpriteBreaker.ShatterGenerateType.Directional ), "Directional", buttonMid ) ) {
                generateType.intValue = (int) SpriteBreaker.ShatterGenerateType.Directional;
            }
            if ( GUILayout.Toggle( ( !generateTypeDiff && generateType.intValue == (int) SpriteBreaker.ShatterGenerateType.Bricks ), "Bricks", buttonMid ) ) {
                generateType.intValue = (int) SpriteBreaker.ShatterGenerateType.Bricks;
            }
            if ( GUILayout.Toggle( ( !generateTypeDiff && generateType.intValue == (int) SpriteBreaker.ShatterGenerateType.Voronoi ), "Voronoi", buttonMid ) ) {
                generateType.intValue = (int) SpriteBreaker.ShatterGenerateType.Voronoi;
            }
            if ( GUILayout.Toggle( ( !generateTypeDiff && generateType.intValue == (int) SpriteBreaker.ShatterGenerateType.Custom ), "Custom", buttonRight ) ) {
                generateType.intValue = (int) SpriteBreaker.ShatterGenerateType.Custom;
            }
            EditorGUI.EndDisabledGroup();            
            EditorGUILayout.EndHorizontal();
            */

            // Common properties
            EditorGUIUtility.labelWidth = 90;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField( generateSeed, labelGenerateSeed, GUILayout.ExpandWidth( false ) );
            GUILayout.FlexibleSpace(); EditorGUIUtility.labelWidth = 30;
            EditorGUILayout.PropertyField( generateFrom, labelGenerateFrom, GUILayout.ExpandWidth( false ) );
            EditorGUILayout.EndHorizontal();
            EditorGUIUtility.labelWidth = 90;
            EditorGUILayout.PropertyField( generateRandomness, labelGenerateRandomness );
            EditorGUILayout.PropertyField( generateAngle, labelGenerateAngle );
            EditorGUILayout.PropertyField( generateSpacing, labelGenerateSpacing );
            EditorGUILayout.PropertyField( generateFrequency, labelGenerateFrequency );
            EditorGUILayout.PropertyField( generateOrigin, labelGenerateOrigin );

            // Specific to generator
            if ( generateTypeDiff ) {
                EditorGUILayout.LabelField( labelMultipleSelection, greyLabel );
            } else {
                // 
            }

            // end parameters block
            bool parameterChanged = EditorGUI.EndChangeCheck();
            EditorGUILayout.EndVertical();

            // check if preview can be drawn
            if ( breaker.data != null && !( generateTypeDiff ||
                ( generateSeed.hasMultipleDifferentValues || generateRandomness.hasMultipleDifferentValues || generateAngle.hasMultipleDifferentValues || generateSpacing.hasMultipleDifferentValues || generateFrequency.hasMultipleDifferentValues || generateOrigin.hasMultipleDifferentValues )
                ) ) {

                // preview toggle
                _showGeneratePreview = EditorGUILayout.ToggleLeft( labelPreview, _showGeneratePreview, boldLabel );

                if ( _showGeneratePreview ) {

                    // if preview regenerate is required
                    if ( previewGenerateData == null || parameterChanged ) {
                        // make copy of data, cut it using current params
                        if ( generateFrom.intValue == (int) SpriteBreaker.BreakerGenerateFrom.Data ) {
                            previewGenerateData = breaker.data.Clone();
                        } else if ( generateFrom.intValue == (int) SpriteBreaker.BreakerGenerateFrom.Asset && asset != null ) {
                            previewGenerateData = asset.data.Clone();
                        } else {
                            if ( previewGenerateData != null ) previewGenerateData.polygons.Clear();
                            else previewGenerateData = new SpriteBreakerData();
                        }
                        if ( previewGenerateData.polygons.Count == 0 ) previewGenerateData.InitAsQuad();
                        if ( generateSeed.intValue == 0 ) previewRandomState = UnityEngine.Random.state;
                        breaker.Generate( previewGenerateData );
                        labelPreview.text = "Preview (" + previewGenerateData.polygons.Count + " polys)";
                    }

                    // draw result
                    Rect previewRect = EditorGUILayout.BeginVertical( GUILayout.Height( Mathf.Min( Screen.width, 150 ) ) );
                    GUILayout.Space( previewRect.height );
                    Vector2 previewSize = new Vector2( 1, 1 );
                    Sprite spr = breaker.currentSprite;
                    if ( spr != null ) previewSize = spr.rect.size;
                    float minScale = Mathf.Min( previewRect.width / previewSize.x, previewRect.height / previewSize.y );
                    previewSize *= minScale;
                    Rect previewSpace = new Rect( previewRect.position + 0.5f * new Vector2( previewRect.width - previewSize.x, previewRect.height - previewSize.y) - new Vector2( 2, 2 ),
                                                    previewSize + new Vector2( 4, 4 ) );
                    EditorGUI.DrawRect( previewSpace, Color.black );
                    previewSpace.size = previewSize; previewSpace.position += new Vector2( 2, 2 );

                    // mouse click / drag resets origin
                    if ( Event.current.type == EventType.MouseDown && previewSpace.Contains( Event.current.mousePosition ) ) {
                        _settingGenerateOrigin = true;
                    }

                    // do drag
                    if ( Event.current.type == EventType.MouseDrag && _settingGenerateOrigin ) {
                        Vector2 pos = ( Event.current.mousePosition - previewSpace.position ) / previewSpace.size;
                        pos.y = 1 - pos.y;
                        generateOrigin.vector2Value = pos;
                        previewGenerateData = null;
                        Repaint();
                    } else if ( Event.current.type == EventType.MouseUp ) _settingGenerateOrigin = false;

                    // redraw
                    if ( Event.current.type == EventType.Repaint ) {
                        Handles.color = Color.white;
                        // for each poly
                        for ( int i = 0, np = previewGenerateData.polygons.Count; i < np; i++ ) {
                            SpriteBreakerPolygon poly = previewGenerateData.polygons[ i ];
                            if ( poly == null || poly.edges.Count <= 2 ) continue;
                            Vector3 prevPoint = new Vector3();
                            for ( int j = 0, cnt = poly.edges.Count; j <= cnt; j++ ) {
                                Vector3 vert = poly.edges[ j % cnt ]; vert.z = 0;
                                vert.y = 1.0f - vert.y;
                                vert = previewSpace.position + vert * previewSize;
                                if ( j > 0 ) Handles.DrawLine( prevPoint, vert );
                                EditorGUI.DrawRect( new Rect( vert.x - 1.25f, vert.y - 1.25f, 2.5f, 2.5f ), Color.white );
                                prevPoint = vert;
                            }
                        }
                    }

                    GUILayout.EndVertical();
                } else {
                    labelPreview.text = "Preview";
                }
            }

            EditorGUILayout.Space();

            // generate shards
            if ( GUILayout.Button( labelRegenerate, fatButton, GUILayout.Height( 20 ), GUILayout.ExpandWidth( true ) ) ) {
                int undoGroup = Undo.GetCurrentGroup();
                Object[] objs = serializedObject.targetObjects;
                for ( int i = 0, no = objs.Length; i < no; i++ ) {
                    SpriteBreaker sobj = (SpriteBreaker) objs[ i ];
                    Undo.RegisterCompleteObjectUndo( sobj, "Generate shards" );
                    if ( generateSeed.intValue == 0 && i == 0 ) {
                        UnityEngine.Random.state = previewRandomState;
                    }
                    // if already have preview, use that
                    if ( previewGenerateData != null ) sobj.data = previewGenerateData.Clone();
                    else sobj.Generate();
                    EditorUtility.SetDirty( sobj );
                }
                Undo.CollapseUndoOperations( undoGroup );
                UpdateEditorWindow();
            }
            EditorGUILayout.Space();
        }
        EditorGUILayout.EndFoldoutHeaderGroup(); // end generate foldout

        // Appearance foldout
        if ( _showAppearance = EditorGUILayout.BeginFoldoutHeaderGroup( _showAppearance, labelAppearance ) ) {

            // Shards color
            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 210;
            EditorGUILayout.PropertyField( updateShardsColor, labelUpdateShardsColor );
            EditorGUILayout.PropertyField( shardsColor, GUIContent.none, GUILayout.ExpandWidth( true ) );
            EditorGUILayout.EndHorizontal();
            
            // cast shadows
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField( castShadows, labelCastShadows );
            EditorGUILayout.PropertyField( receiveShadows, labelReceiveShadows );
            shadowModeChanged = EditorGUI.EndChangeCheck();
            
            // Disable renderer
            EditorGUILayout.PropertyField( disableRendererOnBreak, labelDisableRendererOnBreak );

            // Draw gizmos + color
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField( drawEditorGizmos );
            if ( drawEditorGizmos.hasMultipleDifferentValues || drawEditorGizmos.boolValue ) {
                EditorGUILayout.PropertyField( gizmoColor, GUIContent.none, GUILayout.ExpandWidth( true ) );
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // Physics foldout
        if ( _showPhysics = EditorGUILayout.BeginFoldoutHeaderGroup( _showPhysics, labelPhysics ) ) {
            EditorGUIUtility.labelWidth = 120;

            // basic
            EditorGUILayout.PropertyField( timeToLive );
            EditorGUI.BeginDisabledGroup( !timeToLive.hasMultipleDifferentValues && timeToLive.floatValue <= Mathf.Epsilon );
            EditorGUILayout.PropertyField( fadeAfter );
            EditorGUILayout.PropertyField( endAction );
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField( createColliders, labelColliders );
            EditorGUI.indentLevel++;
            // 3d only
            if ( createColliders.enumValueIndex == (int) SpriteBreaker.CreateColliders.Collider3D && !createColliders.hasMultipleDifferentValues ) {
                EditorGUILayout.PropertyField( colliderThickness );
            }
            // common physics
            if ( createColliders.enumValueIndex != (int) SpriteBreaker.CreateColliders.None ) {
                // mass
                EditorGUILayout.PropertyField( massMultiplier );

                // gravity
                if ( createColliders.enumValueIndex == (int) SpriteBreaker.CreateColliders.FakePhysics && !createColliders.hasMultipleDifferentValues ) {
                    EditorGUILayout.PropertyField( fakeGravity, labelGravity );
                }

                EditorGUILayout.LabelField( labelImpulse, boldLabel );

                // radial impulse
                EditorGUILayout.BeginHorizontal(); EditorGUIUtility.labelWidth = 92;
                EditorGUILayout.PropertyField( initialRadialImpulse, labelRadial ); EditorGUI.indentLevel--; EditorGUIUtility.labelWidth = 28;
                EditorGUILayout.PropertyField( initialRadialImpulsePlusMinus, labelPlusMinus, GUILayout.MaxWidth( 60 ) ); EditorGUI.indentLevel++;
                EditorGUILayout.EndHorizontal();

                // linear impulse
                EditorGUILayout.BeginHorizontal(); EditorGUIUtility.labelWidth = 80;
                EditorGUILayout.PropertyField( initialLinearImpulse, labelLinear ); EditorGUI.indentLevel--; EditorGUIUtility.labelWidth = 28;
                EditorGUILayout.PropertyField( initialLinearImpulsePlusMinus, labelPlusMinus, GUILayout.MaxWidth( 60 ) ); EditorGUI.indentLevel++;
                EditorGUILayout.EndHorizontal();

                // rotational impulse
                EditorGUILayout.BeginHorizontal(); EditorGUIUtility.labelWidth = 80;
                EditorGUILayout.PropertyField( initialRotationalImpulse, labelRotational ); EditorGUI.indentLevel--; EditorGUIUtility.labelWidth = 28;
                EditorGUILayout.PropertyField( initialRotationalImpulsePlusMinus, labelPlusMinus, GUILayout.MaxWidth( 60 ) ); EditorGUI.indentLevel++;
                EditorGUILayout.EndHorizontal();
                EditorGUIUtility.labelWidth = 120;
            }
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // Events foldout        
        if ( _showEvents = EditorGUILayout.BeginFoldoutHeaderGroup( _showEvents, labelEvents ) ) {
            EditorGUILayout.PropertyField( ShardCreated );
            EditorGUILayout.PropertyField( TimeToLiveExpired );
            EditorGUILayout.Space();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        // Advanced foldout
        if ( _showAdvanced = EditorGUILayout.BeginFoldoutHeaderGroup( _showAdvanced, labelAdvanced ) ) {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField( csgUpscale );
            EditorGUILayout.PropertyField( csgEpsilon );
            if ( EditorGUI.EndChangeCheck() ) {
                // force redraw
                previewGenerateData = null;
            }
            EditorGUILayout.Space();
        }
        serializedObject.ApplyModifiedProperties();
        
        // apply shadow mode
        if ( shadowModeChanged ) {
            foreach ( SpriteBreaker shat in selecteds ) {
                shat.ApplyCastShadowsMode();
            }
        }
    }

    // Shatter button clicked
    private void Break ( bool s ) {

        // start undo group
        Undo.SetCurrentGroupName( "Break" );
        int group = Undo.GetCurrentGroup();

        // store undo object for each selected
        foreach ( SpriteBreaker obj in serializedObject.targetObjects ) {
            Undo.RegisterCompleteObjectUndo( obj, "Break" );
        }
        Undo.FlushUndoRecordObjects();

        // apply
        foreach ( SpriteBreaker obj in serializedObject.targetObjects ) {
            if ( s ) obj.Break();
            else obj.Clear();
            EditorUtility.SetDirty( obj );
        }

        // finish
        Undo.CollapseUndoOperations( group );
    }

    // Clear data clicked
    void ClearData () {

        // start undo group
        Undo.SetCurrentGroupName( "Clear data" );
        int group = Undo.GetCurrentGroup();

        // store undo for each selected object
        foreach ( SpriteBreaker obj in serializedObject.targetObjects ) {
            Undo.RegisterCompleteObjectUndo( obj, "Clear data" );
        }
        Undo.FlushUndoRecordObjects();

        // apply
        foreach ( SpriteBreaker obj in serializedObject.targetObjects ) {
            obj.Clear();
            obj.data = null;
            EditorUtility.SetDirty( obj );
        }

        // finish
        Undo.CollapseUndoOperations( group );
        UpdateEditorWindow();
        previewGenerateData = null; // refresh preview too
        Repaint();
    }

    // Paste data clicked
    void PasteData () {

        // make sure copied data's there
        if ( SpriteBreakerDrawer.copiedData == null ) return;
        
        // start undo group
        Undo.SetCurrentGroupName( "Paste data" );
        int group = Undo.GetCurrentGroup();

        // store undo for each selected object
        foreach ( SpriteBreaker obj in serializedObject.targetObjects ) {
            Undo.RegisterCompleteObjectUndo( obj, "Paste data" );
        }
        Undo.FlushUndoRecordObjects();

        // apply
        foreach ( SpriteBreaker obj in serializedObject.targetObjects ) {
            obj.data = SpriteBreakerDrawer.copiedData.Clone();
            EditorUtility.SetDirty( obj );
        }

        // finish
        Undo.CollapseUndoOperations( group );
        UpdateEditorWindow();
        previewGenerateData = null; // refresh preview too
        Repaint();
    }

    // force editor window to redraw data
    void UpdateEditorWindow() {
        EditorApplication.delayCall += EditorWindow.FocusWindowIfItsOpen<SpriteBreakerEditor>;
    }

}

/*

    EditorGUI.ObjectField( new Rect( position.x, position.y, position.width - 60, EditorGUIUtility.singleLineHeight ), property );
    if ( GUI.Button( new Rect( position.x + position.width - 58, position.y, 58, EditorGUIUtility.singleLineHeight ), "Create" ) ) {
        string selectedAssetPath = "Assets";
        if ( property.serializedObject.targetObject is MonoBehaviour ) {
            MonoScript ms = MonoScript.FromMonoBehaviour( (MonoBehaviour) property.serializedObject.targetObject );
            selectedAssetPath = System.IO.Path.GetDirectoryName( AssetDatabase.GetAssetPath( ms ) );
        }
        Type type = fieldInfo.FieldType;
        if ( type.IsArray ) type = type.GetElementType();
        else if ( type.IsGenericType && type.GetGenericTypeDefinition() == typeof( List<> ) ) type = type.GetGenericArguments()[ 0 ];
        property.objectReferenceValue = CreateAssetWithSavePrompt( type, selectedAssetPath );
    }



    // Creates a new ScriptableObject via the default Save File panel
    // property.objectReferenceValue = CreateAssetWithSavePrompt( type, selectedAssetPath );
    ScriptableObject CreateAssetWithSavePrompt( Type type, string path ) {
        path = EditorUtility.SaveFilePanelInProject( "Save ScriptableObject", "New " + type.Name + ".asset", "asset", "Enter a file name for the ScriptableObject.", path );
        if ( path == "" ) return null;
        ScriptableObject asset = ScriptableObject.CreateInstance( type );
        AssetDatabase.CreateAsset( asset, path );
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        AssetDatabase.ImportAsset( path, ImportAssetOptions.ForceUpdate );
        EditorGUIUtility.PingObject( asset );
        return asset;
    }
 */
