using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MagicCurve;

 using static Unity.Mathematics.math;
using Unity.Mathematics;

[ExecuteAlways]
public class MoveObjectsAlongCurve : MonoBehaviour
{

    public Curve curve;
    public Transform[] objects;

    public Vector3[] offsets;
    public float[] scales;


    public float[] speeds;

    public bool updateInEditMode;



    public float[] positionsAlongPath;
    // Start is called before the first frame update


void OnEnable(){
    #if UNITY_EDITOR 
        EditorApplication.update += Always;
    #endif
}

void OnDisable(){
      #if UNITY_EDITOR 
        EditorApplication.update -= Always;
    #endif
}
void Always(){    
  #if UNITY_EDITOR 
  if( updateInEditMode) EditorApplication.QueuePlayerLoopUpdate();
  #endif
}


    // Update is called once per frame
    void Update()
    {


       float3 pos; float3 fwd; float3 up; float3 rit; float scale;

        for( int i = 0; i < objects.Length; i++ ){

            float newPositionAlongPath = positionsAlongPath[i] + speeds[i] ;

            curve.GetDataFromLengthAlongCurve( newPositionAlongPath , out pos ,out fwd, out up, out rit, out scale );

            objects[i].position = pos + rit * offsets[i].x + up * offsets[i].y + fwd * offsets[i].z;

            objects[i].rotation = Quaternion.LookRotation(fwd,up);
            objects[i].localScale = Vector3.one * scale * scales[i];

            positionsAlongPath[i] = newPositionAlongPath;
            positionsAlongPath[i] %= curve.totalCurveLength;
            
        }

        
        
    }


}