using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
 using static Unity.Mathematics.math;
using Unity.Mathematics;


[CustomEditor(typeof(Curve))]
public class CurveEditor : Editor
{

            bool hasChanged = false;

    Curve curve;
     
    bool rightClick = false;
    Vector2 clickPos;



    float curvePosition = 0;

    GUIStyle label;
    
    void OnEnable(){
        
        curve = (Curve)target;

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
             isMoving = false;
             curve.playClip( curve.moveClip );
         }

          if( isScaling == true && scaleDelta > .3f){
             scaleDelta = 0;
             isScaling = false;
             curve.playClip( curve.scaleClip );
         }

         if( isRotating == true && rotateDelta > 10){
             rotateDelta = 0;
             isRotating = false;
             curve.playClip( curve.rotateClip );
         }
     }

    bool isRotating;
    bool isScaling;
    bool isMoving;


    float rotateDelta;
    float scaleDelta;
    float moveDelta;


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


        if( curve.enabled  ){
        Quaternion lookAtCam = Quaternion.LookRotation( Camera.current.transform.forward, Camera.current.transform.up );

        if( rightClick ){   
            Debug.Log("itsHappening");
            Handles.DrawLine( Vector3.zero , Vector3.one * 1000);
        }
            float3 t; float3 d; float w; float3 p;

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
    
        
            /*

                Baked Point Renderering

            */
            

            if( curve.showEvenBasis ){
            Vector3[] pos2 = new Vector3[ curve.bakedPoints.Length * 2 ];

            Handles.color = Color.HSVToRGB(0,.5f,1f);
            for(int i = 0; i< curve.bakedPoints.Length; i++){
                pos2[i * 2 + 0 ] = curve.bakedPoints[i];
                pos2[i * 2 + 1 ] = curve.bakedPoints[i] + curve.bakedTangents[i] * curve.bakedWidths[i] * .5f;
            }            
            Handles.DrawLines(pos2);

            Handles.color = Color.HSVToRGB(.33f,.5f,1f);
            for(int i = 0; i< curve.bakedPoints.Length; i++){
                pos2[i * 2 + 0 ] = curve.bakedPoints[i] ;
                pos2[i * 2 + 1 ] = curve.bakedPoints[i] + curve.bakedNormals[i] * curve.bakedWidths[i] * .5f;
            }            
            Handles.DrawLines(pos2);

             Handles.color = Color.HSVToRGB(.66f,.5f,1f);
            for(int i = 0; i< curve.bakedPoints.Length; i++){
                pos2[i * 2 + 0 ] = curve.bakedPoints[i] ;
                pos2[i * 2 + 1 ] = curve.bakedPoints[i] + curve.bakedDirections[i] * curve.bakedWidths[i] * .5f;
            }            
            Handles.DrawLines(pos2);

            }

            Handles.color = Color.HSVToRGB(.7f,.5f,1f);

            int newCount = curve.bakedPoints.Length;
            int totalDivisor = 1;
            while( newCount > 100 ){
                newCount /= 2;
                totalDivisor *= 2;

            }
            for( int i = 0; i < newCount; i++ ){

                if( curve.showAddPositions ){
                    int fID = i * totalDivisor;
                    Quaternion rot = Quaternion.LookRotation(  curve.bakedDirections[fID] , curve.bakedNormals[fID] );
                    float s =  GetSize(curve.bakedPoints[fID], .05f);// HandleUtility.GetHandleSize(curve.bakedPoints[i]) * .04f;
                    bool hit = Handles.Button( curve.bakedPoints[fID] , rot ,s, s,  Handles.DotHandleCap );

                    if( hit ){
                        Undo.RecordObject(curve,"addPoint");
                        curve.AddPoint(fID);
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

            
    

                


            // Rotate Controls
            if( (i == curve.selectedPoint || curve.showAllControls ) && curve.showRotateControls ){
                
         
                
                Quaternion newRot;
               

                Handles.color = Color.HSVToRGB(0,.5f,1);
                newRot = Handles.Disc( rot ,pos ,  up , GetSize(pos,.6f)  , false , 0);
                if( newRot != rot ){
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
                fPos2 = pos + forward * (outSize + curve.powers[i].x * size);
                
                newPos = Handles.FreeMoveHandle(fPos2, Quaternion.identity, size, Vector3.zero, Handles.CircleHandleCap);
                dir = (fPos2 -pos);
                Handles.DrawLine(fPos2 , fPos1);
             
                if( newPos != fPos2 ){
                    scaleDelta += length(newPos - fPos2) / GetSize(pos , 1);
                    isScaling = true;
                    Undo.RecordObject(curve,"Change Power");
                    curve.ChangePowerX( i,  Mathf.Clamp( dot( newPos - fPos1 , dir ) , 0 , 1) * (newPos - fPos1).magnitude / size);
                    hasChanged = true;
                    curve.selectedPoint=i;
                }



                fPos1 = pos - forward * outSize;
                fPos2 = pos - forward * (outSize + curve.powers[i].y * size);
                
                newPos = Handles.FreeMoveHandle(fPos2, Quaternion.identity, size, Vector3.zero, Handles.CircleHandleCap);
                dir = (fPos2 -pos);
                Handles.DrawLine(fPos2 , fPos1);
             
                if( newPos != fPos2 ){
                    scaleDelta += length(newPos - fPos2) / GetSize(pos , 1);
                    isScaling = true;
                    Undo.RecordObject(curve,"Change Power");
                    curve.ChangePowerY( i,  Mathf.Clamp( dot( newPos - fPos1 , dir ) , 0 , 1) * (newPos - fPos1).magnitude / size);
                    hasChanged = true;
                    curve.selectedPoint=i;
                }

         
                Handles.color = Color.HSVToRGB(.5f,.5f,1);

                fPos1 = pos + right * outSize;
                fPos2 = pos + right * (outSize + curve.powers[i].z * size);
                
                newPos = Handles.FreeMoveHandle(fPos2, Quaternion.identity, size, Vector3.zero, Handles.CircleHandleCap);
                dir = (fPos2 -pos);
                Handles.DrawLine(fPos2 , fPos1);
             
                if( newPos != fPos2 ){
                    scaleDelta += length(newPos - fPos2) / GetSize(pos , 1);
                    isScaling = true;
                    Undo.RecordObject(curve,"Change Power");
                    curve.ChangePowerZ( i,  Mathf.Clamp( dot( newPos - fPos1 , dir ) , 0 , 1) * (newPos - fPos1).magnitude / size);
                    hasChanged = true;
                    curve.selectedPoint=i;
                }

                fPos1 = pos - right * outSize;
                fPos2 = pos - right * (outSize + curve.powers[i].z * size);
                
                newPos = Handles.FreeMoveHandle(fPos2, Quaternion.identity, size, Vector3.zero, Handles.CircleHandleCap);
                dir = (fPos2 -pos);
                Handles.DrawLine(fPos2 , fPos1);
             
                if( newPos != fPos2 ){
                    scaleDelta += length(newPos - fPos2) / GetSize(pos , 1);
                    isScaling = true;
                    Undo.RecordObject(curve,"Change Power");
                    curve.ChangePowerZ( i,  Mathf.Clamp( dot( newPos - fPos1 , dir ) , 0 , 1) * (newPos - fPos1).magnitude / size);
                    hasChanged = true;
                    curve.selectedPoint=i;
                }
                /*

                float defaultSize = HandleUtility.GetHandleSize(pos);

                Handles.color = Color.HSVToRGB(.5f,.5f,1);

                fPos = pos + right * curve.powers[i].z;
                newPos = Handles.FreeMoveHandle(fPos, Quaternion.identity, size, Vector3.zero, Handles.CircleHandleCap);
                dir = (newPos - pos);
                Handles.DrawLine(newPos-dir.normalized*size , pos);
                
                if( newPos != fPos ){
                    scaleDelta += length(newPos - fPos) / GetSize(pos , 1);
                    isScaling = true;
                    Undo.RecordObject(curve,"Change Power");
                    curve.ChangePowerZ( i,  (newPos - pos).magnitude);
                    hasChanged = true;
                    curve.selectedPoint=i;
                }

                fPos = pos - right * curve.powers[i].z;
                newPos = Handles.FreeMoveHandle(fPos, Quaternion.identity, size, Vector3.zero, Handles.CircleHandleCap);
                dir = (newPos - pos);
                Handles.DrawLine(newPos-dir.normalized*size , pos);

                if( newPos != fPos ){
                    Undo.RecordObject(curve,"Change Power");
                    scaleDelta += length(newPos - fPos) / GetSize(pos , 1);
                    isScaling = true;
                    curve.ChangePowerZ( i, (newPos - pos).magnitude);
                    hasChanged = true;
                    curve.selectedPoint=i;
                }
*/
        


            }

   
            if( i != curve.selectedPoint && curve.showAllControls != true ){
                Handles.color = Color.HSVToRGB(.3f,.3f,1);//,.5f,1);
                bool hit = Handles.Button( pos , lookAtCam , GetSize( pos , .65f ) , GetSize( pos , .65f ) ,  Handles.CircleCap );
                
                if( hit ){ 
                    Undo.RecordObject(curve,"Select New Point");
                    curve.selectedPoint = i;
                    hasChanged = true;
                }
            }
            
            if(i == curve.selectedPoint ){
                // draw it but can't hit 
                Handles.color = Color.HSVToRGB(.9f,.3f,1);//,.5f,1);
                bool    hit = Handles.Button( pos , lookAtCam , GetSize( pos , .8f ) ,0 ,  Handles.CircleCap );
                        //hit = Handles.Button( pos , lookAtCam , GetSize( pos , .62f ) ,0 ,  Handles.CircleCap );
                        //hit = Handles.Button( pos , lookAtCam , GetSize( pos , .85f ) ,0 ,  Handles.CircleCap );
            }
         

            if( (i == curve.selectedPoint || curve.showAllControls ) && curve.showMoveControls){
                Handles.color = Color.HSVToRGB(.6f,0.5f,1);
                newPos = Handles.FreeMoveHandle( pos ,  rot,GetSize(pos , .3f) , Vector3.zero ,  Handles.CircleHandleCap );
                if( newPos != pos ){
                    Undo.RecordObject(curve,"Move");
                    curve.positions[i] = newPos;
                    hasChanged = true;
                    curve.selectedPoint=i;
                    moveDelta += length(newPos - pos) / GetSize(pos , 1);
                    isMoving = true;
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

            float3 p1; float3 p2; float3 p3; float a; float whichVal;
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
                        int i = (int)Mathf.Floor( curvePosition * (float)curve.bakedPoints.Length );
                        Handles.color = Color.HSVToRGB( (curvePosition * 10+.5f) %1,.5f,1);
                        p = curve.bakedPoints[i];
                        evenPoint = p;
                        p1 =  curve.bakedPoints[i] + curve.bakedTangents[i] * curve.bakedWidths[i] * .8f;
                        p2 =  curve.bakedPoints[i] + curve.bakedNormals[i] * curve.bakedWidths[i] * .8f;
                        p3 =  curve.bakedPoints[i] + curve.bakedDirections[i] * curve.bakedWidths[i] * .8f;
                        Handles.DrawLine( p , p1 );
                        Handles.DrawLine( p , p2 );
                        Handles.DrawLine( p , p3 );
                        
                        Handles.DrawSolidDisc(p1,(Vector3)(p1-p),GetSize(p,.04f));
                        Handles.DrawSolidDisc(p2,(Vector3)(p2-p),GetSize(p,.04f));
                        Handles.DrawSolidDisc(p3,(Vector3)(p3-p),GetSize(p,.04f));

                         Handles.BeginGUI();
                    Vector3 pos = p;
                    Vector2 pos2D = HandleUtility.WorldToGUIPoint(pos);
                    
                    label.normal.textColor = Handles.color;
                    float insc = curve.interfaceScale;
                    GUI.Label(new Rect(pos2D.x-50*insc, pos2D.y-30*insc, 100*insc, 20*insc), "Even Speed", label);
                    Handles.EndGUI();
                        
                    
                }

                float3 pd; 
                if( curve.showCurveMovementBasis ){
                 
                    Handles.color = Color.HSVToRGB(curvePosition * 10 %1,.5f,1);
                    curve.GetCubicInformation( curvePosition , out pd , out d , out t , out w);
                    p =   pd;
                    curvePoint = p;
                    p1 =  pd + t * w * .8f;
                    p2 =  pd - cross(d,t) * w * .8f;
                    p3 =  pd + d * w * .8f;
                    Handles.DrawLine( p , p1 );
                    Handles.DrawLine( p , p2 );
                    Handles.DrawLine( p , p3 );
                    
                    Handles.DrawSolidDisc(p1,(Vector3)(p1-p),GetSize(p,.04f));
                    Handles.DrawSolidDisc(p2,(Vector3)(p2-p),GetSize(p,.04f));
                    Handles.DrawSolidDisc(p3,(Vector3)(p3-p),GetSize(p,.04f));


                    Handles.BeginGUI();
                    Vector3 pos = p;
                    Vector2 pos2D = HandleUtility.WorldToGUIPoint(pos);
                    float insc = curve.interfaceScale;

                    GUI.color = Color.white;
                    
        label.normal.textColor = Handles.color;
                    GUI.Label(new Rect(pos2D.x-50*insc, pos2D.y-30*insc, 100*insc, 20*insc), "Curve Speed", label);
                    Handles.EndGUI();
                    /*
                    GUIContent c = new GUIContent("Curve");
                    Vector2 size = label.CalcSize(c);
                    Handles.Label(p , c,label);*/
                    
                    
                }


                if( curve.showCurveMovementBasis && curve.showEvenMovementBasis ){
                    Handles.color = Color.black;// Color.HSVToRGB( curvePosition * 10 %1,0,0);
                    Handles.DrawLine( evenPoint , curvePoint );
                }

      



            



        }

    }

   
}
