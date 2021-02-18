﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
 using static Unity.Mathematics.math;
using Unity.Mathematics;


namespace MagicCurve{

// TODO: 
// hotkeys ( trasnform rotate scale etc )

[CustomEditor(typeof(Curve))]
public class CurveEditor : Editor
{

            bool hasChanged = false;

    Curve curve;
     
    bool rightClick = false;
    Vector2 clickPos;



    float curvePosition = 0;

    GUIStyle label;


    
            float3 t; float3 d; float w; float3 p;
            
            float3 p1; float3 p2; float3 p3; float a; float whichVal;
            float3 u; float3 r; 
            Color c;

    
    void OnEnable(){
        
        curve = (Curve)target;
        curve.GetComponent<AudioSource>().hideFlags = HideFlags.HideInInspector;

        label = new GUIStyle();
        label.fontSize = (int)(15 * curve.interfaceScale+1);;
        label.font = curve.labelFont;
        label.alignment = TextAnchor.UpperCenter;



    }
    void OnSceneGUI()
    {   
        label.fontSize = (int)(15 * curve.interfaceScale+1);;
        label.font = curve.labelFont;
        curvePosition += curve.curveVizSpeed;
        if( curvePosition >= 1 ){ curvePosition = 0; }//whichPoint = whichPoint % 1;
        Input();
        Draw();
        CheckForPlay();
        SceneView.RepaintAll();

    }

     void CheckForPlay(){
         if( isMoving == true && moveDelta > .3f){
             moveDelta = 0;
             curve.playClip( curve.moveClip  , length(startMovePoint - currentMovePoint) / GetSize( startMovePoint,1));
         }

          if( isScaling == true && scaleDelta > .3f){
             scaleDelta = 0;
             curve.playClip( curve.scaleClip  , length(startScale - currentScale) / GetSize( startScale,1));
         }

         if( isRotating == true && rotateDelta > 10){
             rotateDelta = 0;
             curve.playClip( curve.rotateClip  , Quaternion.Angle( startRot, currentRot) / 100);
         }
     }

    bool isRotating;
    bool isScaling;
    bool isMoving;


    float rotateDelta;
    float scaleDelta;
    float moveDelta;

    float3 startMovePoint;
    float3 currentMovePoint;


    Quaternion startRot;
    Quaternion currentRot;

    float3 startScale;
    float3 currentScale;


    void Input()
    {
        Event guiEvent = Event.current;
        Ray mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);

        if (guiEvent.type == EventType.MouseDown  && guiEvent.button == 0)
        {
            hasChanged = false;
            rotateDelta = 0;
            scaleDelta = 0;
            moveDelta = 0;
            
        }

        if (guiEvent.type == EventType.MouseUp  && guiEvent.button == 0)
        {
            if( hasChanged ){ curve.FullBake(); }
            hasChanged = false;
            isRotating = false;
            isMoving = false;
            isScaling = false;
        }

        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.shift)
        {
            Undo.RecordObject(curve,"Add Point");
            curve.AddPointAtEnd(mouseRay);
            hasChanged = true;
            curve.FullBake();
        }

        if (guiEvent.type == EventType.KeyDown && guiEvent.keyCode == KeyCode.Backspace)
        {
            Undo.RecordObject(curve,"Delete Point");
            curve.DeletePoint();
            hasChanged = true;
            curve.FullBake();
        }
    }





    

    float GetSize( float3 val , float scale ){
        return HandleUtility.GetHandleSize(val) * scale * curve.interfaceScale;
    }

    void Draw(){

        if( curve ){
        if( curve.enabled  ){
        Quaternion lookAtCam = Quaternion.LookRotation( Camera.current.transform.forward, Camera.current.transform.up );

        if( rightClick ){   
            Debug.Log("itsHappening");
            Handles.DrawLine( Vector3.zero , Vector3.one * 1000);
        }
       /*


                Actual Curve Rendering;

            */

            if( curve.showCurve ){


            Handles.color = Color.HSVToRGB(1,0,1);//,.5f,1);

            Vector3[] lilPos = new Vector3[curve.curveVizLength]; 

            for( int i = 0; i < curve.curveVizLength; i++ ){
                curve.GetCubicInformation( (float)i/(curve.curveVizLength-1) , out p  , out d , out t , out w );
                lilPos[i] = p;
            }


            Handles.DrawPolyLine(lilPos);

            }



            if( curve.showBakedPoints ){
                for( int i = 0; i < curve.bakedPoints.Length; i++){
                    c = Color.HSVToRGB((((float)i/(float)curve.bakedPoints.Length))%1,.5f,1f);
                    Handles.color = c;
                    Handles.DrawWireCube(curve.bakedPoints[i], Vector3.one * GetSize(curve.bakedPoints[i], .05f) );
                }
            }
    
        
            /*

                Baked Point Renderering

            */
            

            if( curve.showEvenBasis ){

                for( int i = 0; i < curve.evenBasisVizCount; i++ ){
                    float v = (float)i / ((float) curve.evenBasisVizCount-1);
                    DrawBasisAtPoint( v , Color.HSVToRGB(0,.5f,1f) , Color.HSVToRGB(0.33f,.5f,1f),Color.HSVToRGB(0.66f,.5f,1f) , 1);
                }
            }

            Handles.color = Color.HSVToRGB(.7f,.5f,1f);

            int newCount = curve.bakedPoints.Length;
            int totalDivisor = 1;
            while( newCount > 100 ){
                newCount /= 2;
                totalDivisor *= 2;

            }
            for( int i = 0; i < curve.addPointVizCount; i++ ){

                if( curve.showAddPositions ){

                    float valAlongCurve = (float)i/((float)curve.addPointVizCount-1);

                    curve.GetDataFromValueAlongCurve(valAlongCurve , out p , out d , out u , out r  , out w); 
                    float s =  GetSize(p, .05f);
                    bool hit = Handles.Button( p , Quaternion.identity ,s, s,  Handles.DotHandleCap );

                    if( hit ){
                        Undo.RecordObject(curve,"addPoint");
                        curve.AddPoint(valAlongCurve);
                        hasChanged = true;
                    }
                }

               
            }
        

        /*


            Control Point Rendering



        */
        for( int i = 0; i < curve.positions.Count; i++ ){

        
           
            Vector3 pos = curve.positions[i];
            Quaternion rot = curve.rotations[i];
            Vector3 forward = rot * Vector3.forward;
            Vector3 right = rot * Vector3.right;
            Vector3 up = rot * Vector3.up;
            float size = GetSize(pos , .1f);
            
            Vector3 newPos;

            //Transform t = curve.positions[i];


/*

    Select Controls

*/ 
            
       
            if( i != curve.selectedPoint && curve.showAllControls != true ){
                Handles.color = Color.HSVToRGB(.3f,.3f,1);//,.5f,1);
                bool hit = Handles.Button( pos , lookAtCam , GetSize( pos , .65f ) , GetSize( pos , .65f ) ,  Handles.CircleCap );
                
                if( hit ){ 
                    Undo.RecordObject(curve,"Select New Point");
                    curve.SelectNode(i);
                    hasChanged = true;
                }
            }

            
         
            if(i == curve.selectedPoint ){
                // draw it but can't hit 
                Handles.color = Color.HSVToRGB(.9f,1,1);//,.5f,1);
                //bool    hit = Handles.Button( pos , lookAtCam , GetSize( pos , 2f ) ,0 ,  Handles.CircleCap );
                        //hit = Handles.Button( pos , lookAtCam , GetSize( pos , .62f ) ,0 ,  Handles.CircleCap );
                        //hit = Handles.Button( pos , lookAtCam , GetSize( pos , .85f ) ,0 ,  Handles.CircleCap );
            }
         


         /*

            Move Controls

         */ 
   if( (i == curve.selectedPoint || curve.showAllControls ) && curve.showMoveControls){
                Handles.color = Color.HSVToRGB(.6f,0.5f,1);
                newPos = Handles.FreeMoveHandle( pos ,  rot,GetSize(pos , .3f) , Vector3.zero ,  Handles.CircleHandleCap );
                
                if( newPos != pos ){
                    Undo.RecordObject(curve,"Move");
                    curve.positions[i] = newPos;
                    hasChanged = true;
                    curve.selectedPoint=i;
                    moveDelta += length(newPos - pos) / GetSize(pos , 1);

                    currentMovePoint = newPos;
                    if( isMoving == false ){
                        startMovePoint = newPos;
                    }

                    isMoving = true;
                }


                if( isMoving){
                    Handles.DrawDottedLine(startMovePoint,currentMovePoint, length(startMovePoint-currentMovePoint)/4);
                    Handles.DrawSolidDisc( startMovePoint , currentMovePoint-startMovePoint , GetSize(startMovePoint,.1f));
                }
                    
            }     
                


            // Rotate Controls
            if( (i == curve.selectedPoint || curve.showAllControls ) && curve.showRotateControls ){
                
         
                
                Quaternion newRot;
               

                Handles.color = Color.HSVToRGB(0,.5f,1);
                newRot = Handles.Disc( rot ,pos ,  up , GetSize(pos,.6f)  , false , 0);
                if( newRot != rot ){

                    currentRot = newRot;
                    if( isRotating == false ){
                        startRot = newRot;
                    }
                    isRotating = true;

                    rotateDelta += Quaternion.Angle(newRot,rot);
                    Undo.RecordObject(curve,"Rotate");
                    curve.rotations[i] = newRot;
                    hasChanged = true;
                    curve.selectedPoint=i;
                }
                Handles.DrawLine( pos , pos + up * GetSize(pos,.65f));
                Handles.DrawWireCube(pos + up * GetSize(pos,.65f),  Vector3.one * GetSize(pos,.05f));


                Handles.color = Color.HSVToRGB(.1f,.5f,1);
                newRot = Handles.Disc( rot ,pos ,  right ,GetSize( pos,.6f)  , false , 0);
                if( newRot != rot ){
                     currentRot = newRot;
                    if( isRotating == false ){
                        startRot = newRot;
                    }
                    isRotating = true;
                    rotateDelta += Quaternion.Angle(newRot,rot);
                    Undo.RecordObject(curve,"Rotate");
                    curve.rotations[i] = newRot;
                    hasChanged = true;
                    curve.selectedPoint=i;
                }
                Handles.DrawLine( pos , pos + right * GetSize(pos,.65f));
                Handles.DrawWireCube(pos + right * GetSize(pos,.65f),  Vector3.one * GetSize(pos,.05f));
                
                Handles.color = Color.HSVToRGB(.2f,.5f,1);
                 newRot = Handles.Disc( rot ,pos ,  forward ,GetSize( pos,.6f) , false , 0);
                if( newRot != rot ){
                     currentRot = newRot;
                    if( isRotating == false ){
                        startRot = newRot;
                    }
                    isRotating = true;
                    rotateDelta += Quaternion.Angle(newRot,rot);
                    Undo.RecordObject(curve,"Rotate");
                    curve.rotations[i] = newRot;
                    hasChanged = true;
                    curve.selectedPoint=i;
                }
                Handles.DrawLine( pos , pos + forward * GetSize(pos,.65f));
                Handles.DrawWireCube(pos + forward * GetSize(pos,.65f),  Vector3.one * GetSize(pos,.05f));
                
                
                
            }


            // Scale Controls
             if( (i == curve.selectedPoint || curve.showAllControls ) && curve.showScaleControls ){
                
                Vector3 fPos1;Vector3 fPos2; Vector3 dir;

                Handles.color = Color.HSVToRGB(.4f,.5f,1);
                size= GetSize( pos,.1f);
                float outSize = GetSize( pos , .7f);
                
                
                
                fPos1 = pos + forward * outSize;
                fPos2 = pos + forward * (outSize + curve.powers[i].x );
                float handleSize = GetSize( fPos2,.1f);
                
                newPos = Handles.FreeMoveHandle(fPos2, Quaternion.identity, handleSize, Vector3.zero, Handles.CircleHandleCap);
                dir = (fPos2 -pos);
                Handles.DrawLine(fPos2 , fPos1);


                Handles.DrawPolyLine( new []{ 
                    fPos2 + handleSize *forward * 2 - right * outSize * .1f , 
                    fPos2 + handleSize *forward * 2 +  forward * outSize * .1f , 
                    fPos2 + handleSize *forward * 2 + right * outSize * .1f 
                });
             
                if( newPos != fPos2 ){      
                    currentScale = fPos2;
                    if( isScaling == false ){
                        startScale = fPos2;
                        isScaling = true;
                    }
                    scaleDelta += length(newPos - fPos2) / GetSize(pos , 1);
                    Undo.RecordObject(curve,"Change Power");
                    curve.ChangePowerX( i,  Mathf.Clamp( dot( newPos - fPos1 , dir ) , 0 , 1) * (newPos - fPos1).magnitude );
                    hasChanged = true;
                    curve.selectedPoint=i;
                }



                fPos1 = pos - forward * outSize;
                fPos2 = pos - forward * (outSize + curve.powers[i].y);
                
               handleSize = GetSize( fPos2,.1f);
                newPos = Handles.FreeMoveHandle(fPos2, Quaternion.identity, handleSize, Vector3.zero, Handles.CircleHandleCap);
                dir = (fPos2 -pos);
                Handles.DrawLine(fPos2 , fPos1);
                    Handles.DrawPolyLine( new []{ 
                    fPos2 - handleSize *forward * 2 - right * outSize * .1f , 
                    fPos2 - handleSize *forward * 2 + right * outSize * .1f 
                });
             
                if( newPos != fPos2 ){      
                    currentScale = fPos2;
                    if( isScaling == false ){
                        startScale = fPos2;
                        isScaling = true;
                    }
                    scaleDelta += length(newPos - fPos2) / GetSize(pos , 1);
                    Undo.RecordObject(curve,"Change Power");
                    curve.ChangePowerY( i,  Mathf.Clamp( dot( newPos - fPos1 , dir ) , 0 , 1) * (newPos - fPos1).magnitude);
                    hasChanged = true;
                    curve.selectedPoint=i;
                }

         
                Handles.color = Color.HSVToRGB(.5f,.5f,1);

                fPos1 = pos + right * outSize;
                fPos2 = pos + right * (outSize + curve.powers[i].z);
                
               handleSize = GetSize( fPos2,.1f);
                newPos = Handles.FreeMoveHandle(fPos2, Quaternion.identity, handleSize, Vector3.zero, Handles.CircleHandleCap);
                dir = (fPos2 -pos);
                Handles.DrawLine(fPos2 , fPos1);
                Handles.DrawLine(fPos2 , fPos1);
                    Handles.DrawPolyLine( new []{ 
                    fPos2 + handleSize *right * 2 - forward * outSize * .1f , 
                    
                    fPos2 + handleSize *right * 2 +  right * outSize * .1f , 
                    fPos2 + handleSize *right * 2 + forward * outSize * .1f 
                });
             
            
                if( newPos != fPos2 ){
                      currentScale = fPos2;
                    if( isScaling == false ){
                        startScale = fPos2;
                        isScaling = true;
                    }
                    scaleDelta += length(newPos - fPos2) / GetSize(pos , 1);
                    Undo.RecordObject(curve,"Change Power");
                    curve.ChangePowerZ( i,  Mathf.Clamp( dot( newPos - fPos1 , dir ) , 0 , 1) * (newPos - fPos1).magnitude);
                    hasChanged = true;
                    curve.selectedPoint=i;
                }

                fPos1 = pos - right * outSize;
                fPos2 = pos - right * (outSize + curve.powers[i].z);
                
                handleSize = GetSize( fPos2,.1f);
                newPos = Handles.FreeMoveHandle(fPos2, Quaternion.identity, handleSize, Vector3.zero, Handles.CircleHandleCap);
                dir = (fPos2 -pos);
                Handles.DrawLine(fPos2 , fPos1);
                   Handles.DrawLine(fPos2 , fPos1);
                    Handles.DrawPolyLine( new []{ 
                    fPos2 - handleSize *right * 2 - forward * outSize * .1f , 
                    fPos2 - handleSize *right * 2 + forward * outSize * .1f 
                });
             
                if( newPos != fPos2 ){
                    

                    currentScale = fPos2;
                    if( isScaling == false ){
                        startScale = fPos2;
                        isScaling = true;
                    }
                    
                    scaleDelta += length(newPos - fPos2) / GetSize(pos , 1);
                    Undo.RecordObject(curve,"Change Power");
                    curve.ChangePowerZ( i,  Mathf.Clamp( dot( newPos - fPos1 , dir ) , 0 , 1) * (newPos - fPos1).magnitude);
                    hasChanged = true;
                    curve.selectedPoint=i;
                }


                
                Handles.color = Color.HSVToRGB(.2f,.5f,1);
                Handles.DrawPolyLine( new []{ 
                    pos + up * outSize + handleSize * up * 2 - forward * outSize * .1f , 
                    pos + up * outSize + handleSize * up * 2 +  up * outSize * .1f , 
                    pos + up * outSize + handleSize * up * 2 + forward * outSize * .1f 
                });

                   if( isScaling){
                Handles.color = Color.HSVToRGB(.4f,0,1);

                    Vector3[] positions = new []{(Vector3)startScale,(Vector3)currentScale};
                    Handles.DrawLine(startScale,currentScale);
                    Handles.DrawWireDisc( startScale , Camera.current.transform.position - (Vector3)startScale , GetSize(startScale,.03f));
                }
    

            }






















                

            if( curve.showPointArrows ){
            // Arrows
            Handles.color = Color.HSVToRGB(.8f,0,1);
            Vector3[] positions = new Vector3[4];
            positions[0] = pos + size * 5 * forward - right * size ;
            positions[1] = pos + size * 5 * forward + forward * size* 1.6f;
            positions[2] = pos + size * 5 * forward + right * size ;
            positions[3] = pos + size * 5 * forward - right * size ;
            Handles.DrawPolyLine(positions);
            Handles.DrawLine(pos,pos + size * 5 * forward);


            positions = new Vector3[4];
            positions[0] = pos + size * 3 * up - right * size ;
            positions[1] = pos + size * 3 * up + up * size * 1.6f *(3.0f/5.0f) ;
            positions[2] = pos + size * 3 * up + right * size ;
            positions[3] = pos + size * 3 * up - right * size ;
            Handles.DrawPolyLine(positions);
            }


              
               /* fPos = pos - t.forward * curve.powers[i].y;
                newPos = Handles.FreeMoveHandle(fPos, Quaternion.identity, 1, Vector3.zero, Handles.CircleHandleCap);
               
                if( newPos != fPos ){
                    t.LookAt(fPos);
                }*/

                // Go through and change out matricies
                if( hasChanged ){
                    curve.UpdateMatrices();
                }


     
            
            
        }




     



            /*

                Head And Tail

            */

            if( curve.haveFun ){

                Handles.color = Color.HSVToRGB(curvePosition * 10 % 1,1,1);//,.5f,1);
                curve.GetCubicInformation( 0 , out p  , out d , out t , out w );

                for(int i = 0; i < 30; i++ ){
                    whichVal =((float)i/30);
                    a = whichVal  * 6.28f;

                    
                    Handles.color = Color.HSVToRGB((whichVal +curvePosition * 10) % 1,1,1);//,.5f,1);
                    p1 = p;
                    p2 = p - d * 10 + Mathf.Sin( a) * t * 10 - Mathf.Cos(a) * cross(d,t) * 10;
                    
                    Handles.DrawLine(p1,p2);
                    Handles.DrawSolidDisc(p2,(Vector3)(p2-p1),1);
                }

            }





/*

Movement

*/



                float3 evenPoint = float3(0,0,0); float3 curvePoint = float3(0,0,0);

                if( curve.showEvenMovementBasis ){
                    Color c = Color.HSVToRGB( (curvePosition * 10+.5f) %1,.5f,1);
                    DrawBasisAtPoint( curvePosition, c,c,c ,3);
                    
                    DrawLabelAtPoint( curvePosition, "Even Speed" , Color.white );
                }

                float3 pd; 
                if( curve.showCurveMovementBasis ){
                 
                    Color c = Color.HSVToRGB( (curvePosition * 10) %1,.5f,1);
                    DrawBasisAtPointAtCurvePoint(curvePosition,c,c,c,3);

                    DrawLabelAtCurvePoint( curvePosition, "Curve Speed" , Color.white );
            
                    /*
                    GUIContent c = new GUIContent("Curve");
                    Vector2 size = label.CalcSize(c);
                    Handles.Label(p , c,label);*/
                    
                    
                }


                if( curve.showCurveMovementBasis && curve.showEvenMovementBasis ){
                    Handles.color = Color.black;// Color.HSVToRGB( curvePosition * 10 %1,0,0);
                    Handles.DrawLine( evenPoint , curvePoint );
                }

      



            



        }}

    }



    void DrawBasisAtPoint(float valAlongCurve , Color cX,Color cY, Color cZ , float scale ){

    
        curve.GetDataFromValueAlongCurve(valAlongCurve , out p , out d , out u , out r  , out w);    

        p1 = p + r * w * scale;
        p2 = p + u * w * scale;
        p3 = p + d * w * scale;

        Handles.color = cX;
        Handles.DrawLine( p , p1 );
        Handles.DrawSolidDisc(p1,(Vector3)(p1-p),GetSize(p,.04f));
        
        Handles.color = cY;
        Handles.DrawLine( p , p2 );
        Handles.DrawSolidDisc(p2,(Vector3)(p2-p),GetSize(p,.04f));

        Handles.color = cZ;
        Handles.DrawLine( p , p3 );
        Handles.DrawSolidDisc(p3,(Vector3)(p3-p),GetSize(p,.04f));


    }

    void DrawBasisAtPointAtCurvePoint(float valAlongCurve , Color cX,Color cY, Color cZ , float scale){
        
        curve.GetDataFromCurvePoint(valAlongCurve , out p , out d , out u , out r  , out w);    

        p1 = p + r * w * scale;
        p2 = p + u * w * scale;
        p3 = p + d * w * scale;

        Handles.color = cX;
        Handles.DrawLine( p , p1 );
        Handles.DrawSolidDisc(p1,(Vector3)(p1-p),GetSize(p,.04f));
        
        Handles.color = cY;
        Handles.DrawLine( p , p2 );
        Handles.DrawSolidDisc(p2,(Vector3)(p2-p),GetSize(p,.04f));

        Handles.color = cZ;
        Handles.DrawLine( p , p3 );
        Handles.DrawSolidDisc(p3,(Vector3)(p3-p),GetSize(p,.04f));
    }


    void DrawLabelAtPoint( float v , string s , Color c){
        
        Handles.BeginGUI();
        Handles.color = c;
        curve.GetDataFromValueAlongCurve(v , out p , out d , out u , out r  , out w);    
        Vector2 pos2D = HandleUtility.WorldToGUIPoint(p);
        float insc = curve.interfaceScale;

        GUI.color = Color.white;
        
        label.normal.textColor = Handles.color;
        GUI.Label(new Rect(pos2D.x-50*insc, pos2D.y-30*insc, 100*insc, 20*insc), s, label);
        Handles.EndGUI();
    }



    void DrawLabelAtCurvePoint( float v , string s , Color c){
        
        Handles.BeginGUI();
        Handles.color = c;
        curve.GetDataFromCurvePoint(v , out p , out d , out u , out r  , out w);    
        Vector2 pos2D = HandleUtility.WorldToGUIPoint(p);
        float insc = curve.interfaceScale;

        GUI.color = Color.white;
        
        label.normal.textColor = Handles.color;
        GUI.Label(new Rect(pos2D.x-50*insc, pos2D.y-30*insc, 100*insc, 20*insc), s, label);
        Handles.EndGUI();
    }
    
   
}
}