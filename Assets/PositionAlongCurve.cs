using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteAlways]
public class PositionAlongCurve : MonoBehaviour
{
    public CubicProcMeshDirectionsMatter curve;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        transform.position = curve.GetPositionAlongPath( curve.curveStart );
    }
}
