using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System;
using SpriteBreakerUtil;

// SpriteBreakerData polygons editor
[Serializable]
public class SpriteBreakerEditor : EditorWindow {

    [Header("Icons and Textures")]
    public Texture2D pivotIcon;
    public Texture2D pointIcon;
    public Texture2D selectedPointIcon;
    public Texture2D solidTexture;
    public Texture2D checkerTexture;
    public Texture2D modeCreate;
    public Texture2D modeEdit;
    public Texture2D modeCut;
    public Texture2D editMove;
    public Texture2D editRotate;
    public Texture2D editScale;
    public Texture2D editFlipX;
    public Texture2D editFlipY;
    public Texture2D editUnion;
    public Texture2D editSubtract;
    public Texture2D editIntersect;
    public Texture2D editReset;

    // selection
    Sprite currentSprite;
    SpriteBreaker _currentBreaker;
    SpriteBreakerData currentData;

    // color scheme
    Color triangleColor = new Color( 1, 1, 1, 0.5f );
    Color polygonColor = new Color( 0.29f, 0.95f, 1 );
    Color disabledPolygonColor = new Color( 0.25f, 0.25f, 0.25f, 1 );
    Color hiddenPolygonColor = new Color( 0.29f, 0.95f, 1, 0.3f );
    Color outlineColor = Color.black;
    Color backgroundColor = new Color( 0.25f, 0.25f, 0.25f );
    Color isolateBackgroundColor = new Color( 0.5f, 0.5f, 0.5f );
    Color highlightColor = new Color( 1, 1, 1, 0.7f );
    Color selectedColor = new Color( 1f, 1f, 1f, 1 );
    Color cutColor = new Color( 1f, 0.15f, 0.11f, 1 );
    Color tooltipColor = new Color( 0.1f, 0.1f, 0.1f, 0.7f );

    private static GUIStyle
        labelStyle = new GUIStyle(),
        toolsStyle = new GUIStyle(),
        smallTextStyle = new GUIStyle(); 
        
    static GUIStyle buttonLeftStyle, buttonMidStyle, buttonRightStyle;
    static GUIContent popupIcon, resetButton, createButton, editButton, cutButton, 
        moveButton, rotateButton, scaleButton, flipXButton, flipYButton, unionButton, subtractButton, intersectButton;

    static List<SpriteBreakerPolygon> copiedPolys = new List<SpriteBreakerPolygon>();

    Vector2 scrollPos;
    float zoom = 1.0f;
    float viewPad = 100;
    bool needZoomExtents;

    Vector2 scrollInnerSize;
    SpriteBreakerPolygon onlyShowingPolygon = null;

    // action
    enum EditAction {
        None,
        Pan,
        Select,
        Move,
        MoveKeyboard,
        Create,
    }
    EditAction _currentAction = EditAction.None;
    EditAction currentAction { get => _currentAction; set => SetCurrentAction( value ); }

    // move mode
    enum MoveMode {
        Move,
        Rotate,
        Scale
    };
    MoveMode moveMode = MoveMode.Move;

    // main mode
    enum EditMode {
        Edit,
        Add,
        Cut
    }
    EditMode _editMode = EditMode.Edit;
    EditMode editMode { get => _editMode; set => SetEditMode( value ); }
    bool switchToEditOnFinishCreate = false;

    /* enum ResetDataTo {
        Sprite,
        Quad,
        Alpha
    }
    static ResetDataTo resetTo = ResetDataTo.Sprite; */

    // currently creating (or adding points to) polygon    
    SpriteBreakerPolygon currentlyCreating = null;
    bool creatingNewPolygon = false;

    // used to draw in order
    List<SpriteBreakerPolygon> sortedPolygons = new List<SpriteBreakerPolygon>();
    bool _resortPolys = false;

    // shown during move, etc.
    GUIContent _helpMessage = null;
    double _messageTimeOut = 0;
    string message {
        get => ( ( _helpMessage != null && _messageTimeOut < EditorApplication.timeSinceStartup ) ? _helpMessage.text : "" );
        set { if ( value == null ) { _helpMessage = null; } else { _messageTimeOut = EditorApplication.timeSinceStartup + 2; _helpMessage = (_helpMessage != null ? _helpMessage : new GUIContent()); _helpMessage.text = value; } }
    }

    // Static init function
    static void Init() {

        // static init
        toolsStyle.padding = new RectOffset( 4, 4, 4, 4 );
        smallTextStyle.fontSize = 10;

        // Get existing open window or if none, make a new one:
        SpriteBreakerEditor window = (SpriteBreakerEditor) EditorWindow.GetWindow( typeof( SpriteBreakerEditor ) );
        window.wantsMouseMove = true;
        window.ShowUtility();

    }

    // Main editor routine
    void OnGUI () {

        // gui init bullshit:
        if ( popupIcon == null ) {
            popupIcon = EditorGUIUtility.IconContent( "_Popup" );
            resetButton = new GUIContent( "Reset Sprite", editReset, "Resets polygon to sprite's custom outline shape." );
            createButton = new GUIContent( "Create", modeCreate, "Create polygon mode" );
            editButton = new GUIContent( "Edit", modeEdit, "Edit points and polys mode" );
            cutButton = new GUIContent( "Cut", modeCut, "Cut polys using a line mode" );
            moveButton = new GUIContent( "Move", editMove, "Move points or polys" );
            rotateButton = new GUIContent( "Rotate", editRotate, "Move points or polys" );
            scaleButton = new GUIContent( "Scale", editScale, "Scale points or polys" );
            flipXButton = new GUIContent( editFlipX, "Flip selection horizontally" );
            flipYButton = new GUIContent( editFlipY, "Flip selection vertically" );
            unionButton = new GUIContent( "Combine", editUnion, "Combine selected polys into one" );
            subtractButton = new GUIContent( "Subtract", editSubtract, "Subtract second selected poly from first" );
            intersectButton = new GUIContent( "Intersect", editIntersect, "Intersect selected polys" );
            buttonLeftStyle = GUI.skin.GetStyle( "buttonleft" );
            buttonMidStyle = GUI.skin.GetStyle( "buttonmid" );
            buttonRightStyle = GUI.skin.GetStyle( "buttonright" );
        }

        Event currentEvent = Event.current;

        // validate selection
        if ( _currentBreaker && _currentBreaker.data == null ) {
            currentSprite = null;
            currentData = null;
            sortedPolygons.Clear();
        }

        // scrub zoom 
        if ( currentEvent.type == EventType.ScrollWheel && currentAction == EditAction.None ) {

            zoom = Mathf.Max( 0.1f, Mathf.Min( 8.0f, zoom + HandleUtility.niceMouseDeltaZoom * 0.02f ) );
            Repaint();
            return;

        } else if ( currentEvent.type == EventType.MouseMove ) {

            Repaint();
            return;

        // Keyboard commands
        } else if ( currentEvent.isKey ) {

            // edit mode
            if ( _editMode == EditMode.Edit ) {

                if ( _currentAction == EditAction.None ) {
                    if ( currentEvent.type == EventType.KeyDown ) {
                        // delete = delete selection
                        if ( currentEvent.keyCode == KeyCode.Backspace || currentEvent.keyCode == KeyCode.Delete ) {
                            DeleteSelection();
                            // escape = deselect
                        } else if ( currentEvent.keyCode == KeyCode.Escape ) {
                            currentData.selectedPoints.Clear();
                            currentData.selectedPolygons.Clear();
                            // arrow keys - start moving
                        } else if ( currentEvent.keyCode == KeyCode.UpArrow || currentEvent.keyCode == KeyCode.DownArrow || currentEvent.keyCode == KeyCode.LeftArrow || currentEvent.keyCode == KeyCode.RightArrow ) {
                            MoveKeyboardStart();
                            currentEvent.Use();
                        }
                    }
                } else if ( _currentAction == EditAction.MoveKeyboard ) {
                    if ( currentEvent.type == EventType.KeyDown ) {
                        MoveKeyboard();
                        currentEvent.Use();
                    } else if ( currentEvent.type == EventType.KeyUp ) {
                        MoveKeyboardFinish();
                        currentEvent.Use();
                    }
                }
                Repaint();

                // create mode
            } else if ( _editMode == EditMode.Add ) {
                if ( currentEvent.type == EventType.KeyDown ) {
                    // creating
                    if ( _currentAction == EditAction.Create ) {
                        // backspace = step back
                        if ( currentEvent.keyCode == KeyCode.Backspace || currentEvent.keyCode == KeyCode.Delete ) {
                            CreateUndoPoint();
                            // escape = finish polygon
                        } else if ( currentEvent.keyCode == KeyCode.Escape || currentEvent.keyCode == KeyCode.Return ) {
                            CreateFinish();
                            // swap directon
                        } else if ( currentEvent.keyCode == KeyCode.Tab || currentEvent.keyCode == KeyCode.Space ) {
                            CreateSwapDirecton();
                        }
                        // not creating
                    } else if ( _currentAction == EditAction.None ) {
                        // escape = switch to edit
                        if ( currentEvent.keyCode == KeyCode.Escape ) {
                            editMode = EditMode.Edit;
                        }
                    }
                }
                Repaint();

            // cut mode
            } else if ( _editMode == EditMode.Cut ) {
                if ( currentEvent.type == EventType.KeyDown ) {
                    // creating
                    if ( _currentAction == EditAction.Create ) {
                        // backspace = step back
                        if ( currentEvent.keyCode == KeyCode.Backspace || currentEvent.keyCode == KeyCode.Delete ) {
                            CreateUndoPoint();
                            // escape = finish polygon
                        } else if ( currentEvent.keyCode == KeyCode.Escape || currentEvent.keyCode == KeyCode.Return ) {
                            CreateFinish();
                            // swap directon
                        } else if ( currentEvent.keyCode == KeyCode.Tab || currentEvent.keyCode == KeyCode.Space ) {
                            CreateSwapDirecton();
                        }
                    // not creating
                    } else if ( _currentAction == EditAction.None ) {
                        // escape = switch to edit
                        if ( currentEvent.keyCode == KeyCode.Escape ) {
                            editMode = EditMode.Edit;
                        }
                    }
                }
            }

            return;
        }

        // Special commands
        if ( currentEvent.type == EventType.ValidateCommand &&
                ( currentEvent.commandName == "UndoRedoPerformed" ||
                currentEvent.commandName == "Copy" || currentEvent.commandName == "Cut" || currentEvent.commandName == "Paste" ||
                currentEvent.commandName == "Duplicate" || currentEvent.commandName == "SelectAll" ) ) {
            if ( EditorWindow.focusedWindow == this ) currentEvent.Use(); // without this line we won't get ExecuteCommand
            sortedPolygons.Clear();
        } else
        if ( currentEvent.type == EventType.ExecuteCommand ) {
            currentEvent.Use();
            if ( currentData != null ) PerformCommand( currentEvent.commandName );
        }

        EditorGUI.BeginDisabledGroup( _currentAction != EditAction.None );

        // toolbar
        Rect toolsRect = EditorGUILayout.BeginHorizontal( toolsStyle );
        using ( new EditorGUIUtility.IconSizeScope( new Vector2(16, 16) ) ) {
            // num selected
            int selectedPts = 0, selectedPolys = 0;
            if ( currentData != null ) {
                selectedPts = currentData.selectedPoints.Sum( ( p ) => p.Value.Count );
                selectedPolys = currentData.selectedPolygons.Count;   
            }

            if ( GUILayout.Toggle( ( _editMode == EditMode.Add ), createButton, buttonLeftStyle ) ) {
                SetEditMode( EditMode.Add );
            }
            if ( GUILayout.Toggle( ( _editMode == EditMode.Edit ), editButton, buttonMidStyle ) ) {
                SetEditMode( EditMode.Edit );
            }
            if ( GUILayout.Toggle( ( _editMode == EditMode.Cut ), cutButton, buttonRightStyle ) ) {
                SetEditMode( EditMode.Cut );
            }

            GUILayout.Space( 20 );
            // edit mode
            if ( _editMode == EditMode.Edit ) {
                if ( GUILayout.Toggle( ( moveMode == MoveMode.Move ), moveButton, buttonLeftStyle ) ) {
                    moveMode = MoveMode.Move;
                }
                if ( GUILayout.Toggle( ( moveMode == MoveMode.Rotate ), rotateButton, buttonMidStyle ) ) {
                    moveMode = MoveMode.Rotate;
                }
                if ( GUILayout.Toggle( ( moveMode == MoveMode.Scale ), scaleButton, buttonRightStyle ) ) {
                    moveMode = MoveMode.Scale;
                }

                if ( selectedPts > 1 || selectedPolys > 0 ) {
                    GUILayout.Space(20);
                    if ( GUILayout.Button( flipXButton ) ) {
                        FlipSelection( false );
                    }
                    if ( GUILayout.Button( flipYButton ) ) {
                        FlipSelection( true );
                    }
                }
                
            } else if ( _editMode == EditMode.Add ) {

            }
            GUILayout.Space(20);
            if ( currentData != null ) {
                labelStyle.normal.textColor = Color.gray;
                if ( selectedPts > 0 || selectedPolys > 0 ) {
                    string sel = "";
                    if ( selectedPolys > 0 ) sel = selectedPolys + " poly" + ( selectedPolys != 1 ? "s" : "" );
                    sel += ( selectedPolys > 0 && selectedPts > 0 ) ? ", " : "";
                    if ( selectedPts > 0 ) sel += selectedPts + " point" + ( selectedPts != 1 ? "s" : "" );
                    sel += " selected";
                    labelStyle.alignment = TextAnchor.LowerLeft;
                    GUILayout.Label( sel, labelStyle, GUILayout.Height( 18 ) );
                }
                // CSG
                if ( selectedPolys >= 2 ) {
                    GUILayout.Space( 20 );
                    if ( GUILayout.Button( unionButton ) ) {
                        CSGSelection( CSGOperation.Union );
                    }
                    if ( GUILayout.Button( subtractButton ) ) {
                        CSGSelection( CSGOperation.Subtract );
                    }
                    if ( GUILayout.Button( intersectButton ) ) {
                        CSGSelection( CSGOperation.Intersect );
                    }
                }
            }
            GUILayout.FlexibleSpace();

            // reset
            if ( GUILayout.Button( resetButton ) ) {
                ResetSprite();
            }
            
            EditorGUILayout.Separator();
            zoom = GUILayout.HorizontalSlider( zoom, 0.1f, 8, GUILayout.Width( 60 ), GUILayout.Height( 24 ) );
            labelStyle.alignment = TextAnchor.LowerLeft;
            if ( GUILayout.Button( ( zoom * 100 ).ToString( "F0" ) + "% ", labelStyle, GUILayout.Width( 40 ), GUILayout.Height( 18 ) ) || needZoomExtents ) {
                ZoomExtents();
                needZoomExtents = true;
            }
        }
        EditorGUILayout.EndHorizontal();
        toolsRect.height += 2;

        EditorGUI.EndDisabledGroup();

        // common
        Rect windowRect = this.position;
        viewPad = Mathf.Max( 100, windowRect.width, windowRect.height );
        Rect scrollViewRect = new Rect( 0, toolsRect.height, windowRect.width, windowRect.height - toolsRect.height );
        EditorGUI.DrawRect( scrollViewRect, onlyShowingPolygon == null ? backgroundColor : isolateBackgroundColor);

        // sprite selected
        if ( currentSprite != null ) {

            Vector2 textureSize = new Vector2( currentSprite.texture.width, currentSprite.texture.height );
            Rect spriteRect = currentSprite.rect;
            Vector3[] linePts = new Vector3[ 2 ];

            // scroll view
            Rect view = new Rect( 0, 0, viewPad * 2 + Mathf.Max( scrollViewRect.width, currentSprite.rect.width * zoom ), viewPad * 2 + Mathf.Max( windowRect.height, currentSprite.rect.height * zoom ) );
            scrollInnerSize = view.size;
            scrollPos = GUI.BeginScrollView( scrollViewRect, scrollPos, view );

            // adjusted position
            if ( currentEvent.isMouse ) _mouse = currentEvent.mousePosition;

            // draw sprite texture
            Rect textureRect = new Rect( viewPad, viewPad, currentSprite.rect.width * zoom, currentSprite.rect.height * zoom );
            textureRect.center = new Vector2( view.width * 0.5f, view.height * 0.5f );
            Rect tileBackground = new Rect( 0, 0, 4 * textureRect.width / checkerTexture.height / zoom, 4 * textureRect.height / checkerTexture.width / zoom);
            GUI.DrawTextureWithTexCoords( textureRect, checkerTexture, tileBackground );
            textureRect = DrawTexturePreview( textureRect, currentSprite, textureSize, spriteRect.size );
            _mouseOnTexture = ( ( currentEvent.mousePosition - textureRect.position ) / zoom ) / spriteRect.size;
            _mouseOnTexture.y = 1 - _mouseOnTexture.y;
            textureRect.x -= viewPad; textureRect.y -= viewPad;
            textureRect.width += viewPad * 2; textureRect.height += viewPad * 2;

            // mouse overs
            _mouseOverEdge = -1;
            _mouseOverEdgePoly = -1;
            _mouseOverVertex = -1;
            _mouseOverVertexPoly = -1;
            _mouseOverPivot = false;

            // have data
            if ( currentData != null ) {

                // begin
                GUILayout.BeginArea( textureRect );
                // Vector2 viewPadOffset = new Vector2( viewPad, viewPad );
                Vector2 mousePos = currentEvent.mousePosition;

                Color color;
                Vector3 a = new Vector3(), b = new Vector3();

                // sorted polys
                if ( sortedPolygons.Count != currentData.polygons.Count ) {
                    sortedPolygons.Clear();
                    sortedPolygons.AddRange( currentData.polygons );
                    _resortPolys = true;
                }
                if ( _resortPolys ){
                    sortedPolygons.Sort( ( SpriteBreakerPolygon aa, SpriteBreakerPolygon bb ) => {
                        if ( currentlyCreating == aa ) return 1;
                        if ( currentlyCreating == bb ) return -1;
                        if ( onlyShowingPolygon == aa ) return 1;
                        if ( onlyShowingPolygon == bb ) return -1;
                        int ai = currentData.polygons.IndexOf( aa ),
                            bi = currentData.polygons.IndexOf( bb );
                        if ( _mouseOverPivotPoly >= 0 ) {
                            if ( ai == _mouseOverPivotPoly ) return 1;
                            if ( bi == _mouseOverPivotPoly ) return -1;
                        }
                        if ( currentData.selectedPolygons.Count > 0 ) {
                            if ( currentData.selectedPolygons.Contains( ai ) ) return 1;
                            if ( currentData.selectedPolygons.Contains( bi ) ) return -1;
                        }
                        return ai.CompareTo( bi );
                    } );
                    _resortPolys = false;
                }
                _mouseOverPivotPoly = -1;

                // first pass - draw all lines
                for ( int ipp = 0; ipp < sortedPolygons.Count; ipp ++ ) {
                    SpriteBreakerPolygon poly = sortedPolygons[ ipp ];
                    int ip = currentData.polygons.IndexOf( poly );

                    // determine if hidden
                    bool hiddenPoly = false;
                    if ( poly != currentlyCreating ) {
                        if ( onlyShowingPolygon != null && poly != onlyShowingPolygon ) hiddenPoly = true;
                        if ( _editMode == EditMode.Cut && currentData.selectedPolygons.Count > 0 && !currentData.selectedPolygons.Contains( ip ) ) {
                            hiddenPoly = true;
                        }
                    }

                    // draw triangles
                    if ( currentEvent.type == EventType.Repaint && poly.triangles != null && editMode == EditMode.Edit && !hiddenPoly && poly.edges.Count == poly.vertices.Length ) {
                        Handles.color = triangleColor;
                        for ( int it = 0, nt = poly.triangles.Length; it < nt; it += 3 ) {
                            int v0 = poly.triangles[ it ], v1 = poly.triangles[ it + 1 ], v2 = poly.triangles[ it + 2 ];
                            Vector2 p0 = toScreen( poly.vertices[ v0 ]),
                                    p1 = toScreen( poly.vertices[ v1 ]),
                                    p2 = toScreen( poly.vertices[ v2 ]);
                            a.x = p0.x; a.y = p0.y; b.x = p1.x; b.y = p1.y; Handles.DrawLine( a, b );
                            a.x = p1.x; a.y = p1.y; b.x = p2.x; b.y = p2.y; Handles.DrawLine( a, b );
                            a.x = p2.x; a.y = p2.y; b.x = p0.x; b.y = p0.y; Handles.DrawLine( a, b );
                        }
                    }

                    // poly has edges
                    if ( poly.edges != null && !(hiddenPoly && _editMode == EditMode.Cut ) ) {

                        bool polySelected = ( editMode == EditMode.Edit ) && currentData.selectedPolygons.Contains( ip );
                        color = ( poly.enabled ? polygonColor : disabledPolygonColor );

                        // mouse over pivot
                        Vector2 pivotPos = toScreen( poly.pivot );
                        if ( !_mouseOverPivot && _mouseOverPivotPoly == -1 && !hiddenPoly ) { // not already detected
                            _mouseOverPivot = ( editMode == EditMode.Edit && currentAction == EditAction.None && ( pivotPos - mousePos ).magnitude < 4 );
                            if ( _mouseOverPivot ) {
                                color = highlightColor;
                                _mouseOverPivotPoly = ip;
                                _resortPolys = true;
                            }
                            
                        }

                        // this is is creating poly
                        bool creatingPoly = ( currentlyCreating == poly );
                        if ( creatingPoly ) color = _editMode == EditMode.Cut ? cutColor : selectedColor;
                        else if ( _editMode == EditMode.Add ) color.a = 0.8f;

                        // edges
                        for ( int ie = 0, ne = poly.edges.Count - ( creatingPoly ? 1 : 0 ); ie < ne; ie++ ) {
                            Vector2 p0 = toScreen( poly.edges[ ie ] ),
                                    p1 = toScreen( poly.edges[ ( ie + 1 ) % poly.edges.Count ] );

                            // paint
                            if ( currentEvent.type == EventType.Repaint ) {
                                linePts[ 0 ] = p0; linePts[ 1 ] = p1;
                                if ( hiddenPoly ) {
                                    Handles.color = hiddenPolygonColor; Handles.DrawAAPolyLine( solidTexture, 2, linePts );
                                } else {
                                    Handles.color = ( polySelected ? selectedColor : outlineColor ); Handles.DrawAAPolyLine( solidTexture, 4, linePts );
                                    Handles.color = color; Handles.DrawAAPolyLine( solidTexture, 2, linePts );
                                }
                            }
                        }

                        // draw extra segment to mouse
                        if ( creatingPoly ) {
                            Vector2 p0 = toScreen( poly.edges[ poly.edges.Count - 1 ]);
                            linePts[ 0 ] = p0; linePts[ 1 ] = mousePos;
                            Handles.color = outlineColor; Handles.DrawAAPolyLine( solidTexture, 4, linePts );
                            Handles.color = color; Handles.DrawAAPolyLine( solidTexture, 2, linePts );

                            // if extending existing
                            if ( !creatingNewPolygon ) {
                                // draw line back to first point
                                p0 = toScreen( poly.edges[ 0 ] );
                                linePts[ 0 ] = p0;
                                Handles.color = outlineColor; Handles.DrawAAPolyLine( solidTexture, 4, linePts );
                                Handles.color = color; Handles.DrawAAPolyLine( solidTexture, 2, linePts );
                                GUI.DrawTexture( new Rect( mousePos.x - 6, mousePos.y - 6, 12, 12 ), selectedPointIcon );
                            }
                        }

                        if ( editMode == EditMode.Edit && !hiddenPoly ) {
                            // draw pivot
                            Rect r = new Rect( 0, 0, 12, 12 );
                            r.center = pivotPos;
                            GUI.DrawTexture( r, pivotIcon );
                            if ( poly.name.Length > 0 ) {
                                r.position = r.center + new Vector2( -50, -18 );
                                r.width = 100; r.height = 16;
                                smallTextStyle.normal.textColor = Color.white;
                                smallTextStyle.alignment = TextAnchor.UpperCenter;
                                GUI.Label( r, poly.name, smallTextStyle );
                            }
                        }
                    }
                }

                // second pass in reverse - draw points, collect mouseover/out etc
                for ( int ipp = sortedPolygons.Count - 1; ipp >= 0; ipp-- ) {
                    SpriteBreakerPolygon poly = sortedPolygons[ ipp ];
                    int ip = currentData.polygons.IndexOf( poly );

                    // determine if hidden
                    bool hiddenPoly = false;
                    if ( poly != currentlyCreating ) {
                        if ( onlyShowingPolygon != null && poly != onlyShowingPolygon ) hiddenPoly = true;
                        if ( _editMode == EditMode.Cut && currentData.selectedPolygons.Count > 0 && !currentData.selectedPolygons.Contains( ip ) ) {
                            hiddenPoly = true;
                        }
                    }

                    // points
                    if ( poly.edges != null && !hiddenPoly ) {

                        // draw points
                        Rect rect = new Rect();
                        List<int> selPts;
                        currentData.selectedPoints.TryGetValue( ip, out selPts );
                        for ( int ie = 0, ne = poly.edges.Count; ie < ne; ie++ ) {
                            Vector2 p0 = toScreen( poly.edges[ ie ] ), p1 = toScreen( poly.edges[ ( ie + 1 ) % poly.edges.Count ] );
                            bool pointSelected = ( editMode == EditMode.Edit ) && ( selPts != null ) && selPts.Contains( ie );
                            rect.width = rect.height = 8;
                            rect.center = p0;

                            // determine which edge is mouse over
                            if ( _mouseOverEdge == -1 && _mouseOverEdgePoly == -1 && !hiddenPoly ) { // not already detected
                                float mouseDist = HandleUtility.DistancePointToLineSegment( mousePos, p0, p1 );
                                if ( currentAction == EditAction.None && _mouseOverEdge < 0 && mouseDist <= 4 ) {
                                    _mouseOverEdge = ie; _mouseOverEdgePoly = ip;
                                }
                            }

                            if ( moveSet.Count == 0 || !moveSet.ContainsKey( (poly, ie) ) ){
                                // determine which vertex the mouse is over
                                if ( _mouseOverVertex == -1 && _mouseOverVertexPoly == -1 ) {
                                    if ( ( p0 - mousePos ).magnitude <= 4 ) {
                                        _mouseOverVertex = ie; _mouseOverVertexPoly = ip;
                                        rect.width = rect.height = 12;
                                        rect.center = p0;
                                    }
                                }
                            }

                            // draw
                            if ( currentEvent.type == EventType.Repaint ) {
                                GUI.DrawTexture( rect, pointSelected ? selectedPointIcon : pointIcon );
                            }

                        }

                    }

                }

                // editing
                if ( _editMode == EditMode.Edit ) {

                    // not doing anything
                    if ( _currentAction == EditAction.None ) {

                        // ctrl + click = switch to create
                        if ( currentEvent.type == EventType.MouseDown && ( currentEvent.command || currentEvent.control ) ) {
                            editMode = EditMode.Add;
                            // if we're over a vertex, start there
                            if ( _mouseOverVertexPoly >= 0 ) {
                                CreateStart( _mouseOverVertexPoly, _mouseOverVertex, false );
                                // split edge
                            } else {
                                CreateStart( _mouseOverEdgePoly, _mouseOverEdge, true );
                            }
                        }

                        // over point?
                        else if ( _mouseOverVertex >= 0 && _mouseOverVertexPoly >= 0 ) {
                            // mouse down/up on vertex?
                            if ( currentEvent.type == EventType.MouseDown ) {
                                MouseDownVertex( _mouseOverVertexPoly, _mouseOverVertex );
                            } else if ( currentEvent.type == EventType.MouseUp ) {
                                MouseUpVertex( _mouseOverVertexPoly, _mouseOverVertex );
                            }
                            // Repaint();

                        // over segment?
                        } else if ( _mouseOverEdge >= 0 && _mouseOverEdgePoly >= 0 ) {

                            if ( currentEvent.type == EventType.Repaint ) {
                                SpriteBreakerPolygon highlightedPoly = currentData.polygons[ _mouseOverEdgePoly ];
                                for ( int ie = 0, ne = highlightedPoly.edges.Count; ie < ne; ie++ ) {
                                    Vector3 p0 = toScreen( highlightedPoly.edges[ ie ] ),
                                            p1 = toScreen( highlightedPoly.edges[ ( ie + 1 ) % highlightedPoly.edges.Count ] );

                                    Vector3 dl = ( p0 - p1 ).normalized * 4;
                                    linePts[ 0 ] = p0 - dl; linePts[ 1 ] = p1 + dl;
                                    Handles.color = highlightColor; Handles.DrawAAPolyLine( solidTexture, 2, linePts );
                                }

                            }

                            // mouse down/up on edge?
                            if ( currentEvent.type == EventType.MouseDown ) {
                                MouseDownEdge( _mouseOverEdgePoly, _mouseOverEdge );
                            } else if ( currentEvent.type == EventType.MouseUp ) {
                                MouseUpEdge( _mouseOverEdgePoly, _mouseOverEdge );
                            }
                            // Repaint();

                        // over pivot
                        } else if ( _mouseOverPivot && _mouseOverPivotPoly >= 0 ) {

                            // hightlight pivot
                            Rect r = new Rect( 0, 0, 16, 16 );
                            r.center = toScreen( currentData.polygons[ _mouseOverPivotPoly ].pivot );
                            GUI.DrawTexture( r, pivotIcon );

                            // mouse down/up on pivot
                            if ( currentEvent.type == EventType.MouseDown ) {
                                MouseDownPivot( _mouseOverPivotPoly );
                            } else if ( currentEvent.type == EventType.MouseUp ) {
                                MouseUpPivot( _mouseOverPivotPoly );
                            }
                        }
                    }

                } else if ( _editMode == EditMode.Add || _editMode == EditMode.Cut ) {

                    // left click
                    if ( currentEvent.type == EventType.MouseDown && currentEvent.button == 0 ) {
                        // add point
                        if ( _currentAction == EditAction.Create ) {
                            MouseDownCreate( _mouseOverVertexPoly, _mouseOverVertex );
                            // start
                        } else {
                            CreateStart( _mouseOverVertexPoly, _mouseOverVertex, false );
                        }

                    } 

                } 

                // done with scroll view
                GUILayout.EndArea();

                // draw selection box
                if ( currentAction == EditAction.Select ) {

                    Rect rect = new Rect( _mouseDownAt, _mouse - _mouseDownAt );
                    a = new Vector3( rect.xMin, rect.yMin ); b = new Vector3( rect.xMax, rect.yMin );
                    Handles.DrawDottedLine( a, b, 1 ); a.Set( rect.xMax, rect.yMax, 0 );
                    Handles.DrawDottedLine( b, a, 1 ); b.Set( rect.xMin, rect.yMax, 0 );
                    Handles.DrawDottedLine( a, b, 1 ); a.Set( rect.xMin, rect.yMin, 0 );
                    Handles.DrawDottedLine( b, a, 1 );

                    color = selectedColor;
                    color.a = 0.2f;
                    EditorGUI.DrawRect( rect, color );

                }

            }

            // set cursor
            if ( _currentAction == EditAction.Pan ) EditorGUIUtility.AddCursorRect( view, MouseCursor.Pan );
            else if ( _currentAction == EditAction.Move ) {
                if ( moveMode == MoveMode.Move ) EditorGUIUtility.AddCursorRect( view, MouseCursor.MoveArrow );
                else if ( moveMode == MoveMode.Scale ) EditorGUIUtility.AddCursorRect( view, MouseCursor.ScaleArrow );
                else if ( moveMode == MoveMode.Rotate ) EditorGUIUtility.AddCursorRect( view, MouseCursor.RotateArrow );
            } else if ( _currentAction == EditAction.Select && currentEvent.shift ) EditorGUIUtility.AddCursorRect( view, MouseCursor.ArrowPlus );
            else if ( _currentAction == EditAction.Create && ( currentData.polygons.Count > 2 && _mouseOverVertexPoly == currentData.polygons.Count - 1 && _mouseOverVertex == 0 ) ) EditorGUIUtility.AddCursorRect( view, MouseCursor.Link );
            else if ( _editMode == EditMode.Edit && _currentAction == EditAction.None && currentEvent.alt && _mouseOverVertex >= 0 ) EditorGUIUtility.AddCursorRect( view, MouseCursor.ArrowMinus );

            // end 
            GUI.EndScrollView();

            // general mouse
            if ( currentEvent.type == EventType.MouseDown ) {
                MouseDown();
            } else if ( currentEvent.type == EventType.MouseUp || ( currentEvent.button >= 0 && currentEvent.type == EventType.MouseLeaveWindow ) || currentEvent.type == EventType.DragExited ) {
                MouseUp();
            } else if ( currentEvent.type == EventType.MouseMove || currentEvent.type == EventType.MouseDrag ) {
                MouseMove();
                Repaint();
            }

            // draw help
            if ( _helpMessage != null && _messageTimeOut > EditorApplication.timeSinceStartup ) {
                labelStyle.alignment = TextAnchor.MiddleCenter;
                labelStyle.normal.textColor = Color.white;
                var textDimensions = GUI.skin.label.CalcSize( _helpMessage );
                Rect rect = new Rect( 0, windowRect.height - 38, textDimensions.x + 10, textDimensions.y + 8 );
                EditorGUI.DrawRect( rect, tooltipColor );
                GUI.Label( rect, _helpMessage, labelStyle );
            }

        } else {

            // nothing selected
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = Color.white;
            scrollViewRect.Set( windowRect.width * 0.5f - 150, windowRect.height * 0.5f, 300, 20 );
            GUI.Label( scrollViewRect, "Select a single GameObject with SpriteRenderer\nor SpriteBreaker component to edit", labelStyle );

        }

        // second call centers it correctly
        if ( needZoomExtents ) {
            needZoomExtents = false;
            ZoomExtents();
        }

    }

    /* ============================================================================= Mouse events */

    int _mouseButtonDown = -1;
    Vector2 _mouseDownAt;
    Vector2 _mouse;
    Vector2 _mouseOnTexture;
    Vector2 _mouseDownOnTexture;
    Vector2 _transformPivot;

    int _mouseMoveInitialDirection; // 0 = horiz, 1 vert

    int _mouseDownPoly = -1;
    int _mouseDownVertex = -1;
    int _mouseOverEdge = -1;
    int _mouseOverEdgePoly = -1;
    int _mouseOverVertex = -1;
    int _mouseOverVertexPoly = -1;
    bool _mouseOverPivot = false;
    int _mouseOverPivotPoly = -1;
    bool _reverseAddPointDirection = false;

    Dictionary<(SpriteBreakerPolygon, int), Vector2> moveSet = new Dictionary<(SpriteBreakerPolygon, int), Vector2>();
    Dictionary<SpriteBreakerPolygon, Vector2> movePivotSet = new Dictionary<SpriteBreakerPolygon, Vector2>();
    Dictionary<int, int> touchingPoints = new Dictionary<int, int>();
    
    void MouseMove () {

        // mouse not down
        if ( _mouseButtonDown < 0 ) { return; }

        // threshold for mouse move
        Vector2 delta = ( _mouseDownAt - _mouse );
        float mouseDist = delta.magnitude;

        // not doing anything
        if ( _currentAction == EditAction.None && mouseDist > 4 ) {

            _mouseMoveInitialDirection = Mathf.Abs( delta.x ) > Mathf.Abs( delta.y ) ? 0 : 1;

            // initiate move
            if ( _mouseDownPoly >= 0 && _mouseButtonDown == 0 ) {

                MoveStart();

                // drag in empty space
            } else if ( editMode == EditMode.Edit && _mouseButtonDown == 0 ) {

                SelectStart();

                // right or mid button drag
            } else if ( _mouseButtonDown > 0 ) {

                PanStart();

            }

            // moving poins
        } else if ( _currentAction == EditAction.Move ) {

            MovePoints();

            // pan
        } else if ( _currentAction == EditAction.Pan ) {

            PanScreen();

        } 

    }

    void MouseDown () {
        _mouseDownAt = _mouse;
        _mouseDownOnTexture = _mouseOnTexture;
        _mouseButtonDown = Event.current.button;

        // if creating, switch to pan
        if ( _currentAction == EditAction.Create && _mouseButtonDown != 0 ) {

            _actionBeforePan = _currentAction;
            PanStart();

        } else if ( Event.current.clickCount == 2 ) {

            if ( onlyShowingPolygon != null ) {
                onlyShowingPolygon = null;
                message = "Exited isolate mode";
            } else if ( _mouseDownPoly >= 0 ) {
                // "isolate" polygon, clear selection
                onlyShowingPolygon = currentData.polygons[ _mouseDownPoly ];
                currentData.selectedPoints.Clear();
                currentData.selectedPolygons.Clear();
                message = "Polygon isolate mode";
            }
            _resortPolys = true;
        }
    }

    void MouseUp () {

        if ( _currentAction == EditAction.Select ) {

            SelectFinish();

        } else if ( _currentAction == EditAction.Move ) {

            MoveFinish();

        } else if ( _currentAction == EditAction.Pan ) {

            PanFinish();

        } else if ( editMode == EditMode.Edit && _mouseButtonDown == 0 && _currentAction == EditAction.None ) {

            // click in empty space = deselect
            currentData.selectedPoints.Clear();
            currentData.selectedPolygons.Clear();

        // right click in Add - exit to edit
        } else if ( _currentAction == EditAction.None && editMode == EditMode.Add && _mouseButtonDown > 0 ) {

            editMode = EditMode.Edit;

        }

        // finish action        
        if ( editMode == EditMode.Edit && _currentAction != EditAction.None ) currentAction = EditAction.None;
        _mouseButtonDown = -1;
        _mouseDownPoly = -1;
        _mouseDownVertex = -1;
        Repaint();
    }

    void MouseDownVertex ( int poly, int v ) {
        // remember mousedown
        _mouseDownPoly = poly;
        _mouseDownVertex = v;
        touchingPoints.Clear();
        if ( onlyShowingPolygon == null && currentData.selectedPoints.Count == 0 ) {
            // find all identical points to move together
            Vector2 p = currentData.polygons[ poly ].edges[ v ];
            for ( int ip = currentData.polygons.Count - 1; ip >= 0; ip-- ) {
                SpriteBreakerPolygon g = currentData.polygons[ ip ];
                if ( currentData.selectedPolygons.Count > 0 && !currentData.selectedPolygons.Contains( ip )) continue;
                for ( int ie = g.edges.Count - 1; ie >= 0; ie-- ) {
                    if ( ( ip != poly || ie != v ) && ( p - g.edges[ ie ] ).magnitude <= 0.0001f ) {
                        touchingPoints[ ip ] = ie;
                    }
                }
            }
        }
    }

    void MouseUpVertex ( int poly, int v ) {

        // if shift is down - toggle selection
        if ( Event.current.shift ) {

            ToggleVertexSelected();

            // delete vertex
        } else if ( Event.current.alt ) {

            DeleteVertex();

            // select only this vertex
        } else {

            currentData.selectedPoints.Clear();
            currentData.selectedPolygons.Clear();
            ToggleVertexSelected();
        }

        _mouseButtonDown = -1; // no mouseup
    }

    void MouseDownPivot ( int poly ) {
        _mouseDownPoly = poly;
        _mouseDownVertex = -2;
    }

    void MouseUpPivot ( int poly ) {

        // right click - edit poly
        if ( Event.current.button != 0 ) {
            // popup to edit poly
            Rect rect = new Rect( Event.current.mousePosition, new Vector2( 8, 8 ) );
            PolyPropertiesPopup popup = new PolyPropertiesPopup {
                editor = this, 
                poly = currentData.polygons[ poly ]
            };
            PopupWindow.Show( rect, popup );
        } else {
            // select poly
            SelectPolygon( poly, Event.current.shift );
        }
        _mouseButtonDown = -1; // no mouseup
    }

    void MouseDownEdge ( int poly, int e ) {
        // remember mousedown
        _mouseDownPoly = poly;
        _mouseDownVertex = -1;

        // if ctrl+clicking, add point!
        if ( Event.current.command || Event.current.control ) {
            WillModify( "Insert point" );
            SpriteBreakerPolygon polygon = currentData.polygons[ poly ];
            // get a point closest to the line
            Vector2 closest = NearestPointOnLine( polygon.edges[ e ], polygon.edges[ ( e + 1 ) % polygon.edges.Count ], _mouseOnTexture );
            polygon.edges.Insert( e + 1, closest ); //_mouseOnTexture
            polygon.Triangulate( false );
            DidModify();
            Repaint();
            _mouseDownPoly = -1;
        }
    }

    void MouseUpEdge ( int poly, int e ) {

        if ( _mouseButtonDown == 0 ) SelectPolygon( poly, Event.current.shift );
        else MouseUpPivot( poly );

        _mouseButtonDown = -1; // no mouseup

    }

    // create mode
    void MouseDownCreate ( int poly, int e ) {

        // mouse down on own poly
        bool ownPoly = ( poly == currentData.polygons.Count - 1 );

        // alt+clicked 
        if ( Event.current.alt ) {

            // on self
            if ( ownPoly && e >= 0 ) {

                // remove specific point
                CreateUndoPoint( e );

                // in space or another poly 
            } else {
                // undo point
                CreateUndoPoint();
            }

        // clicked on this poly
        } else if ( ownPoly ) {

            // if clicked on first or another point (when extending), we're done
            if ( e == 0 || !creatingNewPolygon ) {
                CreateFinish();
            }

        // clicked in empty space
        } else {

            // adding points in reverse
            if ( !creatingNewPolygon && _reverseAddPointDirection ) {
                currentlyCreating.edges.Insert( 0, ( poly >= 0 ) ? currentData.polygons[ poly ].edges[ e ] : _mouseOnTexture );
            } else {
                // snap to existing point or mouse pos
                currentlyCreating.edges.Add( ( poly >= 0 ) ? currentData.polygons[ poly ].edges[ e ] : _mouseOnTexture );         
            }
        }
    }

    /* ============================================================================= Actions */

    void PerformCommand ( string commandName ) {
        currentAction = EditAction.None;
        editMode = EditMode.Edit;
        switch ( commandName ) {
            case "Cut":
                if ( currentData.selectedPolygons.Count > 0 ) {
                    CopySelected();
                    DeleteSelection( true );
                    message = "Cut " + currentData.selectedPolygons.Count + " polygon" + ( currentData.selectedPolygons.Count != 1 ? "s" : "" );
                }
                break;
            case "Copy":
                if ( currentData.selectedPolygons.Count > 0 ) {
                    CopySelected();
                    message = "Copied " + currentData.selectedPolygons.Count + " polygon" + ( currentData.selectedPolygons.Count != 1 ? "s" : "" );
                }
                break;
            case "Paste":
                if ( copiedPolys.Count > 0 ) {
                    Paste();
                    message = "Pasted " + copiedPolys.Count + " polygon" + ( copiedPolys.Count != 1 ? "s" : "");
                }
                break;
            case "Duplicate":
                if ( currentData.selectedPolygons.Count > 0 ) {
                    DuplicateSelectedPolygons();
                    message = "Duplicated " + currentData.selectedPolygons.Count + " polygon" + ( currentData.selectedPolygons.Count != 1 ? "s" : "" );
                }
                break;
            case "SelectAll":
                bool selectPoints = ( currentData.selectedPolygons.Count == currentData.polygons.Count ) ||
                                ( onlyShowingPolygon != null && currentData.selectedPolygons.Count == 1 );
                currentData.selectedPoints.Clear();
                currentData.selectedPolygons.Clear();
                for ( int i = 0; i < currentData.polygons.Count; i++ ) {
                SpriteBreakerPolygon poly = currentData.polygons[ i ];
                    if ( onlyShowingPolygon != null && onlyShowingPolygon != poly ) continue;
                    if ( selectPoints ) {
                        currentData.selectedPoints.Add( i, new List<int>( Enumerable.Range( 0, poly.edges.Count ) ) );
                    } else {
                        currentData.selectedPolygons.Add( i );
                    }
                }
                    
                break;
        }

    }

    void DuplicateSelectedPolygons () {
        WillModify( "Duplicate polygons" );
        List<int> newSelected = new List<int>();
        for ( int i = 0; i < currentData.selectedPolygons.Count; i++ ) {
            newSelected.Add( currentData.polygons.Count );
            currentData.polygons.Add( currentData.polygons[ currentData.selectedPolygons[ i ] ].Clone( (5 / zoom) / currentSprite.rect.width, ( 5 / zoom ) / currentSprite.rect.height ) );
        }
        currentData.selectedPoints.Clear();
        currentData.selectedPolygons = newSelected;
        DidModify();
    }

    void CopySelected () {
        copiedPolys.Clear();
        for ( int i = 0; i < currentData.selectedPolygons.Count; i++ ) {
            copiedPolys.Add( currentData.polygons[ currentData.selectedPolygons[ i ] ].Clone() );
        }        
    }

    void Paste () {
        WillModify( "Paste polygons" );
        currentData.selectedPoints.Clear();
        currentData.selectedPolygons.Clear();
        for ( int i = 0; i < copiedPolys.Count; i++ ) {
            currentData.selectedPolygons.Add( currentData.polygons.Count );
            SpriteBreakerPolygon copy = copiedPolys[ i ].Clone();
            currentData.polygons.Add( copy );
        }
        DidModify();
    }

    // for each point
    void CreateStart ( int poly, int e, bool split=true ) {
        
        // if clicked on existing
        if ( poly >= 0 && split ) {

            WillModify( "Split edge" );

            // move it to the end of the polygons list
            currentlyCreating = currentData.polygons[ poly ];
            currentData.polygons[ poly ] = currentData.polygons[ currentData.polygons.Count - 1 ]; // move to end
            currentData.polygons[ currentData.polygons.Count - 1 ] = currentlyCreating;

            // the point w clicked on has to become the last point
            int last = currentlyCreating.edges.Count - 1;
            if ( e < last ) {
                List<Vector2> newEdges = currentlyCreating.edges.GetRange( e + 1, last - e );
                newEdges.AddRange( currentlyCreating.edges.GetRange( 0, e + 1 ) );
                currentlyCreating.edges = newEdges;
            }

            // flag
            creatingNewPolygon = false;
            switchToEditOnFinishCreate = true;

        } else {

            // just start from that point
            Vector2 startPoint = _mouseOnTexture;
            if ( !split && poly >= 0 ) {
                startPoint = currentData.polygons[ poly ].edges[ e ];
            }

            WillModify( "Create polygon" );

            // new
            currentlyCreating = new SpriteBreakerPolygon();
            currentlyCreating.edges = new List<Vector2>();
            currentData.polygons.Add( currentlyCreating );

            // add first point
            currentlyCreating.edges.Add( startPoint );

            // flag
            creatingNewPolygon = true;
            switchToEditOnFinishCreate = false;
        }

        // start
        currentAction = EditAction.Create;
        sortedPolygons.Clear();
    }

    void CreateFinish () {
        if ( currentlyCreating != null ) {

            // add mode
            if ( _editMode == EditMode.Add ) {
                // if not a polygon, abort
                if ( currentlyCreating.edges.Count <= 2 ) {
                    currentlyCreating.edges.Clear();
                    DeleteEmptyPolys();
                    DidModify( false );
                // accept
                } else {
                    currentlyCreating.Triangulate( true, creatingNewPolygon );
                    DidModify();
                }

            // cut
            } else if ( _editMode == EditMode.Cut ) {

                // remove cut line from polys
                currentData.polygons.Remove( currentlyCreating );

                // clicked on last point - add first point to make closed
                if ( _mouseOverVertex >= 0 && _mouseOverVertexPoly == currentData.polygons.Count && Event.current.button == 0 ) {                    
                    currentlyCreating.edges.Add( currentlyCreating.edges[ 0 ] );
                }

                // gather operands
                List <SpriteBreakerPolygon> subjects = new List<SpriteBreakerPolygon>();
                if ( currentData.selectedPolygons.Count > 0 ) {
                    for ( int i = 0; i < currentData.selectedPolygons.Count; i++ )
                        subjects.Add( currentData.polygons[ currentData.selectedPolygons[ i ] ] );
                } else if ( onlyShowingPolygon != null ) {
                    subjects.Add( onlyShowingPolygon );
                } else {
                    subjects.AddRange( currentData.polygons );  
                }

                // perform
                List<SpriteBreakerPolygon> res = _currentBreaker.SplitPolygons( subjects, currentlyCreating.edges );
                if ( res != null ) {
                    currentData.selectedPolygons.Clear();
                    for ( int i = 0; i < res.Count; i++ ) currentData.selectedPolygons.Add( currentData.polygons.IndexOf( res[ i ] ) );
                    DidModify();
                } else DidModify( false );
            }
        }

        // clear
        onlyShowingPolygon = null;
        currentlyCreating = null;
        sortedPolygons.Clear();
        currentAction = EditAction.None;

        // switch to edit if needed
        if ( switchToEditOnFinishCreate ) {
            switchToEditOnFinishCreate = false;
            editMode = EditMode.Edit;
        }
    }

    // reverse path, place current point at end    
    void CreateSwapDirecton() {
        if ( currentlyCreating != null && currentlyCreating.edges.Count > 1 ) {
            if ( creatingNewPolygon ) currentlyCreating.edges.Reverse();
            else {
                // back
                if ( Event.current.shift ) {
                    int lastEdge = currentlyCreating.edges.Count - 1;
                    Vector2 p = currentlyCreating.edges[ lastEdge ];
                    currentlyCreating.edges.RemoveAt( lastEdge );
                    currentlyCreating.edges.Insert( 0, p );
                    _reverseAddPointDirection = true;

                } else {
                    Vector2 p = currentlyCreating.edges[ 0 ];
                    currentlyCreating.edges.RemoveAt( 0 );
                    currentlyCreating.edges.Add( p );
                    _reverseAddPointDirection = false;
                }
            }
        }
    }

    void CreateUndoPoint ( int p = -1 ) {
        // remove point
        if ( p == -1 ) p = currentlyCreating.edges.Count - 1;
        currentlyCreating.edges.RemoveAt( p );

        // if nothing left, exit creation
        if ( currentlyCreating.edges.Count == 0 ) {
            CreateFinish();
        }
    }

    void PrepareToMoveSelection () {

        movePivotSet.Clear();
        moveSet.Clear();

        // keep track of counts
        Dictionary<SpriteBreakerPolygon, int> pointCounts = new Dictionary<SpriteBreakerPolygon, int>();

        // add all selected polygons to move set
        for ( int i = 0; i < currentData.selectedPolygons.Count; i++ ) {
            if ( currentData.selectedPolygons[ i ] > currentData.polygons.Count ) continue;
            SpriteBreakerPolygon poly = currentData.polygons[ currentData.selectedPolygons[ i ] ];
            pointCounts[ poly ] = poly.edges.Count;
            for ( int j = 0; j < poly.edges.Count; j++ ) {
                moveSet.Add( (poly, j), poly.edges[ j ] );
            }
        }

        // add all selected to move set
        foreach ( var polyToSet in currentData.selectedPoints ) {
            List<int> pts = polyToSet.Value;
            SpriteBreakerPolygon poly = currentData.polygons[ polyToSet.Key ];
            if ( pointCounts.ContainsKey( poly ) ) {
                pointCounts[ poly ] += pts.Count;
            } else {
                pointCounts.Add( poly, pts.Count );
            }
            foreach ( int pt in pts ) {
                if ( !moveSet.ContainsKey( (poly, pt) ) )
                    moveSet.Add( (poly, pt), poly.edges[ pt ] );
            }
        }

        // add pivots of fully selected polygons to moveSet
        foreach ( var ms in pointCounts ) {
            (SpriteBreakerPolygon poly, int np) = (ms.Key, ms.Value);
            if ( np >= poly.edges.Count ) {
                movePivotSet.Add( poly, poly.pivot );
            }
        }

    }

    // toggle a single ertex selection
    void ToggleVertexSelected () {
        List<int> selPts;
        if ( !currentData.selectedPoints.TryGetValue( _mouseDownPoly, out selPts ) ) {
            selPts = new List<int>();
            currentData.selectedPoints.Add( _mouseDownPoly, selPts );
        }

        // selected, deselect
        if ( selPts.Contains( _mouseDownVertex ) ) {
            selPts.Remove( _mouseDownVertex );
        } else {
            selPts.Add( _mouseDownVertex );
        }
        Repaint();
    }

    // selects a single poly, or toggles it
    void SelectPolygon ( int poly, bool toggle ) {
        bool isSelected = true;

        if ( toggle ) {
            if ( currentData.selectedPolygons.Contains( poly ) ) {
                currentData.selectedPolygons.Remove( poly );
                isSelected = false;
            } else
                currentData.selectedPolygons.Add( poly );
        } else {
            currentData.selectedPoints.Clear();
            currentData.selectedPolygons.Clear();
            currentData.selectedPolygons.Add( poly );
        }
        // if poly is now selected, remove its points from selection
        if ( isSelected ) {
            if ( currentData.selectedPoints.ContainsKey( poly ) ) currentData.selectedPoints.Remove( poly );
        }
        _resortPolys = true;
    }

    void MoveKeyboardStart () {
        // make sure we can move
        if ( currentData == null || ( currentData.selectedPoints.Count + currentData.selectedPolygons.Count ) == 0 ) return;

        // start
        currentAction = EditAction.MoveKeyboard;
        WillModify( "Move selection" );

        // do first move
        MoveKeyboard();
    }


    void MoveKeyboard () {

        // set direction
        Vector2 move = new Vector2();
        switch ( Event.current.keyCode ) {
        case KeyCode.LeftArrow:
            move.x = -1; break;
        case KeyCode.RightArrow:
            move.x = 1; break;
        case KeyCode.DownArrow:
            move.y = -1; break;
        case KeyCode.UpArrow:
            move.y = 1; break;            
        }

        // normalize size
        move /= currentSprite.rect.size;

        // boost with shift
        if ( Event.current.shift ) {
            move *= 10;
        }

        // apply move to selection
        foreach ( var keyVal in currentData.selectedPoints ) {
            SpriteBreakerPolygon poly = currentData.polygons[ keyVal.Key ];
            for ( int i = 0, np = keyVal.Value.Count; i < np; i++ ) {
                poly.edges[ keyVal.Value[ i ] ] += move;
            }
        }

        // move selected polys
        for ( int i = 0; i < currentData.selectedPolygons.Count; i++ ) {
            int sp = currentData.selectedPolygons[ i ];
            SpriteBreakerPolygon poly = currentData.polygons[ sp ];
            if ( currentData.selectedPoints.ContainsKey( sp ) ) continue;
            poly.pivot += move;
            for ( int j = 0, np = poly.edges.Count; j < np; j++ ) {
                poly.edges[ j ] += move;
            }
        }

    }

    // done keyboard move
    void MoveKeyboardFinish () {

        // retriangulate
        foreach ( var keyVal in currentData.selectedPoints ) {
            SpriteBreakerPolygon poly = currentData.polygons[ keyVal.Key ];
            poly.Triangulate(true, false, currentData);
        }
        for ( int i = 0; i < currentData.selectedPolygons.Count; i++ ) {
            SpriteBreakerPolygon poly = currentData.polygons[ currentData.selectedPolygons[ i ] ];
            if ( currentData.selectedPoints.ContainsKey( i ) ) continue;
            poly.Triangulate(false);
        }

        // apply
        DidModify();

        // done
        currentAction = EditAction.None;
        
    }

    void MoveStart () {

        SpriteBreakerPolygon mouseDownPoly = currentData.polygons[ _mouseDownPoly ];
        currentAction = EditAction.Move;
        moveSet.Clear();

        // save undo
        WillModify( "Transform points" );

        // if started on vertex
        if ( _mouseDownVertex >= 0 ) {

            // if started on selected vertex
            List<int> selPts = null; currentData.selectedPoints.TryGetValue( _mouseDownPoly, out selPts );
            if ( selPts != null && selPts.Contains( _mouseDownVertex ) ) {

                PrepareToMoveSelection();

            // started on unselected vertex
            } else {
                moveSet.Add( (mouseDownPoly, _mouseDownVertex), mouseDownPoly.edges[ _mouseDownVertex ] );
                
                // add touching
                foreach ( var keyVal in touchingPoints ) {
                    SpriteBreakerPolygon poly = currentData.polygons[ keyVal.Key ];
                    moveSet.Add( (poly, keyVal.Value), poly.edges[ keyVal.Value ]);
                }
            }

            // check if all points of a polygon were added, in which case move its pivot too
            Dictionary<SpriteBreakerPolygon, int> checkAllPoints = new Dictionary<SpriteBreakerPolygon, int>();
            foreach ( var keyVal in moveSet ) {
                (SpriteBreakerPolygon poly, int pt) = keyVal.Key;
                if ( checkAllPoints.ContainsKey( poly ) ) checkAllPoints[ poly ]++;
                else checkAllPoints.Add( poly, 1 );

                // add pivot if all points
                if ( checkAllPoints[ poly ] >= poly.edges.Count ) {
                    if ( !movePivotSet.ContainsKey( poly ) )
                        movePivotSet.Add( poly, poly.pivot );
                }
            }

        // move pivot    
        } else if ( _mouseDownVertex == -2 ) {

            movePivotSet.Add( mouseDownPoly, mouseDownPoly.pivot );

        // started move on edge of a poly
        } else {

            // if the poly was selected, move entire selection
            if ( currentData.selectedPolygons.Contains( _mouseDownPoly ) ) {

                PrepareToMoveSelection();

            // if poly wasn't selected
            } else {
                // move just the poly's points + pivot 
                for ( int i = 0; i < mouseDownPoly.edges.Count; i++ ) {
                    moveSet.Add( (mouseDownPoly, i), mouseDownPoly.edges[ i ] );
                }
                movePivotSet.Add( mouseDownPoly, mouseDownPoly.pivot );
            }

        }
        // compute pivot
        Vector2 accum = new Vector2();
        bool multiplePolys = false;
        SpriteBreakerPolygon lastPoly = mouseDownPoly;
        foreach ( var keyVal in moveSet ) {
            (SpriteBreakerPolygon poly, int pt) = keyVal.Key;
            accum += poly.edges[ pt ];
            if ( lastPoly != null && lastPoly != poly ) multiplePolys = true;
            lastPoly = poly;
        }
        // multiple polys or custom selection, use computed pivot
        if ( multiplePolys || moveSet.Count != lastPoly.edges.Count ) {
            _transformPivot = accum / moveSet.Count;
        } else {
            // if rotating a single poly, use its pivot
            _transformPivot = lastPoly.pivot;
        }

    }

    void MovePoints () {
        Vector2 mouseDelta = _mouseOnTexture - _mouseDownOnTexture;
        float maxWidthHeight = Mathf.Max( currentSprite.rect.width, currentSprite.rect.height );

        // move
        if ( moveMode == MoveMode.Move || ( moveSet.Count == 0 && movePivotSet.Count > 0 ) ) {

            // constrain horiz / vertically
            if ( Event.current.shift ) {
                if ( Math.Abs( mouseDelta.x ) > Math.Abs( mouseDelta.y ) ) {
                    mouseDelta.y = 0;
                } else {
                    mouseDelta.x = 0;
                }
            }

            // if one point, snap to nearby points of other polys
            if ( _mouseDownVertex >= 0 ) {
                float snapDist = ( 5 / maxWidthHeight ) / zoom;
                bool snapped = false;

                // snap draged point to edges
                Vector2 draggedPoint = currentData.polygons[ _mouseDownPoly ].edges[ _mouseDownVertex ];
                Vector2 origPt = moveSet[ (currentData.polygons[ _mouseDownPoly ], _mouseDownVertex) ];
                Vector2 destPt = origPt + mouseDelta;
                if ( Math.Abs( destPt.x ) <= snapDist ) {
                    mouseDelta.x = -origPt.x;
                } else if ( Math.Abs( 1 - destPt.x ) <= snapDist ) {
                    mouseDelta.x = ( 1 - origPt.x );
                }
                if ( Math.Abs( destPt.y ) <= snapDist ) {
                    mouseDelta.y = -origPt.y;
                } else if ( Math.Abs( 1 - destPt.y ) <= snapDist ) {
                    mouseDelta.y = ( 1 - origPt.y );
                }

                // points
                for ( int ip = 0; ip < currentData.polygons.Count && !snapped; ip++ ) {
                    SpriteBreakerPolygon poly = currentData.polygons[ ip ];
                    if ( onlyShowingPolygon != null && poly != onlyShowingPolygon ) continue;
                    for ( int i = 0; i < poly.edges.Count; i++ ) {
                        Vector2 snapPoint = poly.edges[ i ];
                        // don't snap to moveSet
                        if ( moveSet.ContainsKey( (poly, i) ) ) continue;
                        Vector2 diff = snapPoint - _mouseOnTexture;
                        if ( diff.magnitude <= snapDist ) {
                            snapped = true;
                            mouseDelta = snapPoint - origPt;
                            break;
                        }
                    }
                }

                // update dest
                destPt = origPt + mouseDelta;

                // siblings, if dragging one point
                if ( !snapped && moveSet.Count == 1 ) {
                    // prev neighbor
                    List<Vector2> edges = currentData.polygons[ _mouseDownPoly ].edges;
                    int prevIndex = _mouseDownVertex == 0 ? ( edges.Count - 1 ) : ( _mouseDownVertex - 1 );
                    int nextIndex = ( _mouseDownVertex + 1 ) % edges.Count;
                    Vector2 prevPt = edges[ prevIndex ], nextPt = edges[ nextIndex ];
                    float dx = Mathf.Abs( prevPt.x - destPt.x ), dy = Mathf.Abs( prevPt.y - destPt.y );
                    if ( dx > snapDist * 2 && dy < snapDist ) mouseDelta.y = prevPt.y - origPt.y;
                    else if ( dy > snapDist * 2 && dx < snapDist ) mouseDelta.x = prevPt.x - origPt.x;
                    dx = Mathf.Abs( nextPt.x - destPt.x ); dy = Mathf.Abs( nextPt.y - destPt.y );
                    if ( dx > snapDist * 2 && dy < snapDist ) mouseDelta.y = nextPt.y - origPt.y;
                    else if ( dy > snapDist * 2 && dx < snapDist ) mouseDelta.x = nextPt.x - origPt.x;
                }
            }

            // move moveset
            foreach ( var keyVal in moveSet ) {
                (SpriteBreakerPolygon poly, int pt) = keyVal.Key;
                poly.edges[ pt ] =  keyVal.Value + mouseDelta;
            }

            // move pivots
            foreach ( var keyVal in movePivotSet ) {
                SpriteBreakerPolygon poly = keyVal.Key;
                poly.pivot = keyVal.Value + mouseDelta;
            }

            Vector2 md = mouseDelta * currentSprite.rect.size;
            message = md.x.ToString( "F1" ) + ", " + md.y.ToString( "F1" );

        // Rotate
        } else if ( moveMode == MoveMode.Rotate ) {

            if ( moveSet.Count == 1 ) {
                message = "Can't rotate a single point";
            } else {
                float angle = maxWidthHeight * ( ( _mouseMoveInitialDirection == 0 ) ? -mouseDelta.x : -mouseDelta.y );
                if ( Event.current.shift ) angle = Mathf.Floor( angle / 15f ) * 15f;
                message = angle.ToString( "F1" ) + " deg";
                angle = Mathf.PI * angle / 180;
                float cos = Mathf.Cos( angle );
                float sin = Mathf.Sin( angle );

                // move moveset
                foreach ( var keyVal in moveSet ) {
                    (SpriteBreakerPolygon poly, int pt) = keyVal.Key;                    
                    Vector2 orig = keyVal.Value;
                    Vector2 trans = orig - _transformPivot;
                    trans.Set( trans.x * cos - trans.y * sin, trans.y * cos + trans.x * sin );
                    poly.edges[ pt ] = trans + _transformPivot;
                }

                // move pivots
                foreach ( var keyVal in movePivotSet ) {
                    SpriteBreakerPolygon poly = keyVal.Key;
                    Vector2 orig = keyVal.Value;
                    Vector2 trans = orig - _transformPivot;
                    trans.Set( trans.x * cos - trans.y * sin, trans.y * cos + trans.x * sin );
                    poly.pivot = trans + _transformPivot;
                }

            }

        // Scale
        } else if ( moveMode == MoveMode.Scale ) {

            if ( moveSet.Count == 1 ) {
                message = "Can't scale a single point";
            } else {

                mouseDelta = Vector2.one * ( ( _mouseMoveInitialDirection == 0 ) ? mouseDelta.x : mouseDelta.y );

                // constrain horiz / vertically
                if ( Event.current.shift ) {
                    if ( _mouseMoveInitialDirection == 0 ) {
                        mouseDelta.y = 0;
                    } else {
                        mouseDelta.x = 0;
                    }
                }
                message = ( mouseDelta.x * 100 + 100 ).ToString( "F0" ) + "% " + (mouseDelta.y * 100 + 100 ).ToString( "F0" ) + "%";                

                // scale moveset
                foreach ( var keyVal in moveSet ) {
                    (SpriteBreakerPolygon poly, int pt) = keyVal.Key;
                    Vector2 orig = keyVal.Value;
                    Vector2 trans = orig - _transformPivot;
                    trans *= Vector2.one + mouseDelta * 4;
                    poly.edges[ pt ] = trans + _transformPivot;
                }

                // move pivots
                foreach ( var keyVal in movePivotSet ) {
                    SpriteBreakerPolygon poly = keyVal.Key;
                    Vector2 orig = keyVal.Value;
                    Vector2 trans = orig - _transformPivot;
                    trans *= Vector2.one + mouseDelta * 4;
                    poly.pivot = trans + _transformPivot;
                }
            }
        }

        Repaint();
    }

    void MoveFinish () {

        // if we dragged a single point and dropped onto sibling
        if ( moveSet.Count == 1 && movePivotSet.Count == 0 ) {
            List<Vector2> edges = currentData.polygons[ _mouseDownPoly ].edges;
            Vector2 sz = currentSprite.rect.size;
            // find points on top of each other in this poly
            for ( int i = 0; i < edges.Count; i++ ) {
                Vector2 p0 = edges[ i ] * sz, p1 = edges[ ( i + 1 ) % edges.Count ] * sz;
                if ( Vector2.Distance( p0, p1 ) < 1 ) {
                    edges.RemoveAt( i );                    
                    break;
                }
            }
        }

        // if pivots were moved
        foreach ( var keyVal in movePivotSet ) {
            // re-triangulate
            keyVal.Key.Triangulate( false );
        }

        // retriangulate each affected poly
        foreach ( var keyVal in moveSet ) {
            (SpriteBreakerPolygon poly, int pt) = keyVal.Key;
            if ( movePivotSet.ContainsKey( poly ) ) continue;
            poly.Triangulate( false );
        }

        // APPLY
        DidModify();
        moveSet.Clear();
        movePivotSet.Clear();
    }

    bool _panMoved = false;
    void PanStart () {
        currentAction = EditAction.Pan;
        _panMoved = false;
    }

    void PanScreen () {
        scrollPos -= Event.current.delta;
        _panMoved = _panMoved || ( _mouseDownAt - _mouse ).magnitude > 2f;
        Repaint();
    }

    EditAction _actionBeforePan;
    void PanFinish () {

        // return to creation
        if ( currentlyCreating != null ) {
            currentAction = _actionBeforePan;
        } else currentAction = EditAction.None;

        // right click
        if ( !_panMoved ) {
            if ( currentlyCreating != null ) CreateFinish();
            else {
                currentAction = EditAction.None;
                editMode = EditMode.Edit;
            }
        }
    }

    void SelectStart () {
        // start marquee
        currentAction = EditAction.Select;
    }

    void SelectFinish () {
        // not adding to selection?
        if ( !Event.current.shift ) {
            // clear selection
            currentData.selectedPolygons.Clear();
            currentData.selectedPoints.Clear();
        }

        // add points in box to selection
        Rect rect = new Rect();
        rect.x = Mathf.Min( _mouseDownOnTexture.x, _mouseOnTexture.x );
        rect.y = Mathf.Min( _mouseDownOnTexture.y, _mouseOnTexture.y );
        rect.width = Mathf.Abs( _mouseDownOnTexture.x - _mouseOnTexture.x );
        rect.height = Mathf.Abs( _mouseDownOnTexture.y - _mouseOnTexture.y );
        List<SpriteBreakerPolygon> polys = currentData.polygons;
        for ( int i = polys.Count - 1; i >= 0; i-- ) {
            SpriteBreakerPolygon p = polys[ i ];
            if ( onlyShowingPolygon != null && onlyShowingPolygon != p ) continue;
            for ( int j = 0; j < p.edges.Count; j++ ) {
                if ( rect.Contains( p.edges[ j ] ) ) {
                    List<int> selPts;
                    if ( !currentData.selectedPoints.TryGetValue( i, out selPts ) ) {
                        currentData.selectedPoints.Add( i, selPts = new List<int>() );
                    }
                    if ( !selPts.Contains( j ) ) { selPts.Add( j ); }
                }
            }
        }
    }

    void DeleteSelection ( bool cutCommand=false ) {
        WillModify( cutCommand ? "Cut selection" : "Delete selection" );

        if ( !cutCommand ) {
            // delete points
            foreach ( var p in currentData.selectedPoints ) {
                // sort selected points, remove 
                SpriteBreakerPolygon poly = currentData.polygons[ p.Key ];
                List<int> selPts = p.Value;
                selPts.Sort();
                for ( int i = selPts.Count - 1; i >= 0; i-- ) {
                    poly.edges.RemoveAt( selPts[ i ] );
                }
            }
        }

        // delete polygons
        currentData.selectedPolygons.Sort();
        for ( int i = currentData.selectedPolygons.Count - 1; i >= 0; i-- ) {
            currentData.polygons.RemoveAt( currentData.selectedPolygons[ i ] );
        }

        // delete empty polygons
        DeleteEmptyPolys();

        // retriangulate
        for ( int i = 0; i < currentData.polygons.Count; i++ ) {
            SpriteBreakerPolygon poly = currentData.polygons[ i ];
            if ( poly.vertices == null || poly.vertices.Length != poly.edges.Count ) {
                poly.Triangulate( false );
            }
        }

        // clear selection
        currentData.selectedPolygons.Clear();
        currentData.selectedPoints.Clear();
        DidModify();
    }

    void DeleteVertex () {

        // check if clicked vertex was selected
        List<int> selPts;
        if ( !currentData.selectedPoints.TryGetValue( _mouseDownPoly, out selPts ) ) {
            selPts = new List<int>();
            currentData.selectedPoints.Add( _mouseDownPoly, selPts );
        }

        // if it was selected, delete selection
        if ( selPts.Contains( _mouseDownVertex ) ) DeleteSelection();

        // otherwise delete single vertex 
        else {

            WillModify( "Delete point" );

            // preserve selection - decrement any selected vertex indexes after this vertex
            for ( int i = 0; i < selPts.Count; i++ ) {
                if ( selPts[ i ] > _mouseDownVertex ) selPts[ i ] -= 1;
            }

            // remove it
            SpriteBreakerPolygon mouseDownPoly = currentData.polygons[ _mouseDownPoly ];
            mouseDownPoly.edges.RemoveAt( _mouseDownVertex );
            mouseDownPoly.Triangulate( false );
            DeleteEmptyPolys();

            DidModify();
        }

    }

    // called by deleting funcs to clean up empty polys afterwards
    void DeleteEmptyPolys () {
        if ( currentData == null ) return;
        List<SpriteBreakerPolygon> polys = currentData.polygons;
        for ( int i = polys.Count - 1; i >= 0; i-- ) {
            SpriteBreakerPolygon p = polys[ i ];
            if ( p.edges.Count == 0 ) polys.RemoveAt( i );
        }
    }

    void SetEditMode ( EditMode em ) {
        if ( em != _editMode ) {
            if ( _editMode == EditMode.Add && currentlyCreating != null ) CreateFinish();

            // switch edit mode
            _editMode = em;

        }
    }

    void SetCurrentAction ( EditAction a ) {

        if ( a != _currentAction ) {

            _currentAction = a;
            message = null; // clear
        }

    }

    void FlipSelection ( bool flipX ) {
        WillModify( "Flip points" );
        PrepareToMoveSelection();

        // compute pivot
        Vector2 accum = new Vector2();
        bool multiplePolys = false;
        SpriteBreakerPolygon lastPoly = null;
        foreach ( var keyVal in moveSet ) {
            (SpriteBreakerPolygon poly, int pt) = keyVal.Key;
            accum += poly.edges[ pt ];
            if ( lastPoly != null && lastPoly != poly ) multiplePolys = true;
            lastPoly = poly;
        }
        // multiple polys or custom selection, use computed pivot
        if ( multiplePolys || moveSet.Count != lastPoly.edges.Count ) {
            _transformPivot = accum / moveSet.Count;
        } else {
            // if rotating a single poly, use its pivot
            _transformPivot = lastPoly.pivot;
        }
        
        // flip transform
        Vector2 scaleTrans = new Vector2( 1, -1 ) * (flipX ? 1 : -1);
        
        // scale moveset
        foreach ( var keyVal in moveSet ) {
            (SpriteBreakerPolygon poly, int pt) = keyVal.Key;
            Vector2 orig = keyVal.Value;
            Vector2 trans = orig - _transformPivot;
            trans *= scaleTrans;
            poly.edges[ pt ] = trans + _transformPivot;
        }

        // move pivots
        foreach ( var keyVal in movePivotSet ) {
            SpriteBreakerPolygon poly = keyVal.Key;
            Vector2 orig = keyVal.Value;
            Vector2 trans = orig - _transformPivot;
            trans *= scaleTrans;
            poly.pivot = trans + _transformPivot;
        }
        
        // apply
        
        // if pivots were moved
        foreach ( var keyVal in movePivotSet ) {
            // re-triangulate
            keyVal.Key.Triangulate(true, false, currentData);
        }

        // retriangulate each affected poly
        foreach ( var keyVal in moveSet ) {
            (SpriteBreakerPolygon poly, int pt) = keyVal.Key;
            if ( movePivotSet.ContainsKey( poly ) ) continue;
            poly.Triangulate(true, false, currentData);
        }
        
        DidModify();
        movePivotSet.Clear();
        moveSet.Clear();
    }
    
    // reset to sprite
    public void ResetSprite () {

        if ( currentData == null ) {
            Debug.Log( "Unable to reset - no data assigned" );
            return;
        }
        WillModify( "Reset sprite" );
        if ( currentAction == EditAction.Create ) CreateFinish();
        currentAction = EditAction.None;
        currentData.PopulateFromSprite( currentSprite );
        sortedPolygons.Clear();
        DidModify();
    }

    // when selection changes
    void OnSelectionChange() {

        if ( Selection.gameObjects.Length != 1 ||
            ( _currentBreaker = Selection.activeGameObject.GetComponent<SpriteBreaker>() ) == null ){
            // clear
            currentSprite = null;
            _currentBreaker = null;
            currentData = null;
            sortedPolygons.Clear();
            onlyShowingPolygon = null;
            _mouseDownPoly = -1; _mouseDownVertex = -1;
            currentAction = EditAction.None;
            editMode = EditMode.Edit;

        } else SetBreaker( _currentBreaker );

    }

    // sets current SpriteBreaker whose data we'll be editing
    public void SetBreaker ( SpriteBreaker newBreaker, int selectPoly=-1 ) {

        // set current
        _currentBreaker = newBreaker;
        currentData = _currentBreaker.data;
        currentSprite = _currentBreaker.currentSprite;
        onlyShowingPolygon = null;

        // no data, but have sprite, make new data with sprite
        if ( currentData == null && currentSprite != null ) {
            currentData = new SpriteBreakerData();
            currentData.PopulateFromSprite( currentSprite );
        }

        // select poly
        if ( selectPoly >= 0 && selectPoly < currentData.polygons.Count ) {
            currentData.selectedPoints.Clear();
            currentData.selectedPolygons.Clear();
            currentData.selectedPolygons.Add( selectPoly );
            editMode = EditMode.Edit;
            currentAction = EditAction.None;
            _resortPolys = true;
        }

        // force update
        sortedPolygons.Clear();
        needZoomExtents = true;
        Focus();
        Repaint();
    }

    // fit texture to view
    void ZoomExtents () {
        // fit view
        if ( currentData != null && currentSprite != null) {
            Rect scrollViewRect = this.position; scrollViewRect.width -= 20; scrollViewRect.height -= 38;
            zoom = Mathf.Max( 0.1f, Mathf.Min( 20.0f,
                Math.Min( scrollViewRect.width / ( currentSprite.rect.width + 40 ), scrollViewRect.height / ( currentSprite.rect.height + 40 ) ) ) );
            Rect view = new Rect( 0, 0, viewPad * 2 + Mathf.Max( scrollViewRect.width, currentSprite.rect.width * zoom ), viewPad * 2 + Mathf.Max( scrollViewRect.height, currentSprite.rect.height * zoom ) );
            scrollPos.Set( ( scrollInnerSize.x - scrollViewRect.width ) * 0.5f, ( scrollInnerSize.y - scrollViewRect.height ) * 0.5f );
        }
        Repaint();
    }

    /* ============================================================================= CSG */

    PolyBool polyBool = new PolyBool();
    enum CSGOperation {
        Union, Subtract, Intersect, Split
    };
    void CSGSelection( CSGOperation operation ) {

        WillModify( operation.ToString() + " Selection" );
        try {
            // gather selection
            List<SpriteBreakerPolygon> selection = new List<SpriteBreakerPolygon>( currentData.selectedPolygons.Count );
            for ( int i = 0; i < currentData.selectedPolygons.Count; i++ ) {
                selection.Add( currentData.polygons[ currentData.selectedPolygons[ i ] ] );
            }

            // delete selection
            currentData.selectedPolygons.Sort();
            for ( int i = currentData.selectedPolygons.Count - 1; i >= 0; i-- ) {
                currentData.polygons.RemoveAt( currentData.selectedPolygons[ i ] );
            }
            currentData.selectedPoints.Clear();
            currentData.selectedPolygons.Clear();

            switch ( operation ) {
            case CSGOperation.Union:
                _currentBreaker.UnionPolygons( selection, true, true ); break;
            case CSGOperation.Subtract:
                _currentBreaker.SubtractPolygons( selection, true, true ); break;
            case CSGOperation.Intersect:
                _currentBreaker.IntersectPolygons( selection, true, true ); break;
            }

            // finished
            DidModify();
            sortedPolygons.Clear();

        } catch ( Exception e ) {
            message = "Error occured";
            Debug.Log( e.ToString() );
            DidModify( false );
        }
    }

    /* ============================================================================= Util */

    Rect DrawTexturePreview ( Rect pos, Sprite sprite, Vector2 fullSize, Vector2 size ) {
        Rect textureRect = sprite.textureRect, coords = textureRect;
        coords.x /= fullSize.x;
        coords.width /= fullSize.x;
        coords.y /= fullSize.y;
        coords.height /= fullSize.y;

        Vector2 ratio;
        ratio.x = pos.width / size.x;
        ratio.y = pos.height / size.y;
        float minRatio = Mathf.Min( ratio.x, ratio.y );
        
        Vector2 diff = Vector2.one, offs = Vector2.zero;
        if ( sprite.packingMode == SpritePackingMode.Tight ) {
            Rect rect = sprite.rect;
            if ( textureRect.width < rect.width ) {
                diff.x = textureRect.width / rect.width;
                offs.x = (textureRect.x - rect.x) * (pos.width/fullSize.x);
            }
            if ( textureRect.height < rect.height ) {
                diff.y = textureRect.height / rect.height;
                offs.y = pos.height - ((textureRect.y - rect.y) + textureRect.height) * (pos.height/size.y);
                //
            }
        }
        
        


        Vector2 center = pos.center;
        pos.width = size.x * minRatio;
        pos.height = size.y * minRatio;
        pos.center = center;
        Rect retPos = pos;
        //pos.x += offs.x;
        //pos.y += offs.y;
        pos.center += offs;
        pos.width *= diff.x;
        pos.height *= diff.y;

        GUI.DrawTextureWithTexCoords( pos, sprite.texture, coords );
        return retPos;
    }

    protected int _undoGroup = 0;
    public void WillModify ( string undoName ) {

        // store undo
        Undo.SetCurrentGroupName( undoName );
        _undoGroup = Undo.GetCurrentGroup();
        Undo.RegisterCompleteObjectUndo( _currentBreaker, undoName );
        Undo.FlushUndoRecordObjects();

    }

    public void DidModify ( bool did = true ) {
        EditorUtility.SetDirty( _currentBreaker );
        Undo.CollapseUndoOperations( _undoGroup );
        if ( Event.current != null ) Event.current.Use();
        if ( !did ) Undo.RevertAllInCurrentGroup();        
        _undoGroup = 0;
    }

    void OnFocus () {
        wantsMouseMove = true;
        wantsMouseEnterLeaveWindow = true;

        // update selection
        OnSelectionChange();
        sortedPolygons.Clear();
    }

    Vector2 NearestPointOnLine ( Vector2 a, Vector2 b, Vector2 pnt ) {
        Vector2 lineDir = ( b - a ).normalized;
        Vector2 v = pnt - a;
        float d = Vector2.Dot( v, lineDir );
        return a + lineDir * d;
    }

    Vector2 toScreen ( Vector2 pt ) {
        pt.x = pt.x * currentSprite.rect.size.x * zoom + viewPad;
        pt.y = ( 1 - pt.y ) * currentSprite.rect.size.y * zoom + viewPad;
        return pt;
    }

}

public class PolyPropertiesPopup : PopupWindowContent  {

    public SpriteBreakerEditor editor;
    public SpriteBreakerPolygon poly;
    bool modified = false;
    bool init = true;

    bool polyEnabled;
    bool polyActive;
    float zOffset;
    bool includeInPhysics;
    int sortingOrder;
    string name = "";
    string tag;
    float floatValue;
    string stringValue;
    Vector2 pivot;


    public override Vector2 GetWindowSize () {
        return new Vector2( 220, 245 );
    }

    public override void OnGUI ( Rect rect ) {
        
        GUILayout.BeginVertical( GUILayout.Width( 210 ) );
        GUILayout.Label( "Polygon properties", EditorStyles.boldLabel );
        EditorGUI.BeginChangeCheck();

        // fields
        GUILayoutOption labelWidth = GUILayout.Width( 100 );
        GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField( "Enabled", labelWidth );
            polyEnabled = EditorGUILayout.Toggle( polyEnabled );
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField( "Active", labelWidth );
            polyActive = EditorGUILayout.Toggle( polyActive );
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField( "Physics", labelWidth );
        includeInPhysics = EditorGUILayout.Toggle( includeInPhysics );
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField( "Name", labelWidth );
            GUI.SetNextControlName( "Name" );
            name = EditorGUILayout.TextField( name, GUILayout.ExpandWidth( true ) );
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField( "Sorting Order", labelWidth );
        sortingOrder = EditorGUILayout.IntField( sortingOrder, GUILayout.Width( 50 ) );
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField( "Z Offset", labelWidth );
        zOffset = EditorGUILayout.FloatField( zOffset, GUILayout.Width( 50 ) );
        GUILayout.EndHorizontal();
        pivot = EditorGUILayout.Vector2Field( "Pivot", pivot );
        GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField( "Tag", labelWidth );
            tag = EditorGUILayout.TagField( tag, GUILayout.ExpandWidth( true ) );
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField( "Float Value", labelWidth );
            floatValue = EditorGUILayout.FloatField( floatValue, GUILayout.Width( 50 ) );
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField( "String Value", labelWidth );
            stringValue = EditorGUILayout.TextField( stringValue, GUILayout.Width( 50 ) );
        GUILayout.EndHorizontal();

        if ( EditorGUI.EndChangeCheck() ) {
            // create undo on first modification
            if ( !modified ) {
                modified = true;
                editor.WillModify( "Polygon properties" );
            }
            // apply changes
            poly.enabled = polyEnabled;
            poly.active = polyActive;
            poly.name = name;
            poly.pivot = pivot;
            poly.zOffset = zOffset;
            poly.sortingOrderOffset = sortingOrder;
            poly.includeInPhysics = includeInPhysics;
            poly.tag = (tag == "" || tag == "Untagged") ? "" : tag;
            poly.floatValue = floatValue;
            poly.stringValue = stringValue;
            editor.Repaint();
            
        }

        GUILayout.EndVertical();

        // first run
        if ( init ) {
            init = false;
            EditorGUI.FocusTextInControl( "Name" );
        }
    }

    public override void OnOpen () {
        // copy properties
        polyEnabled = poly.enabled;
        polyActive = poly.active;
        zOffset = poly.zOffset;
        includeInPhysics = poly.includeInPhysics;
        sortingOrder = poly.sortingOrderOffset;
        name = poly.name;
        pivot = poly.pivot;
        tag = poly.tag;
        floatValue = poly.floatValue;
        stringValue = poly.stringValue;
    }

    public override void OnClose () {
        // save undo
        if ( modified ) editor.DidModify();
    }

}
