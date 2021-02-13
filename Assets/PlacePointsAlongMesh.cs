using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class PlacePointsAlongMesh : MonoBehaviour
{


    public Transform[] thingsToPlace;
    public Transform[] thingsToPlace2;
    public float[] locationsToPlace;

    public Curve curve;

    // Start is called before the first frame update
    void Update()
    {
        for( int i = 0; i < thingsToPlace.Length; i ++){

            thingsToPlace[i].position  = curve.GetPositionFromValueAlongCurve(locationsToPlace[i]);
           // thingsToPlace2[i].position  = curve.GetPositionFromLengthAlongCurve(locationsToPlace[i]* curve.totalCurveLength);


            curve.SetTransformFromValueAlongCurve( locationsToPlace[i], thingsToPlace2[i] );
        }
    }



}
