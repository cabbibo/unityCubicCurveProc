using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MagicCurve;
using static Unity.Mathematics.math;
using Unity.Mathematics;



[ExecuteInEditMode]

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Curve))]
public class TubeAlongCurve : MonoBehaviour
{

    public Curve curve;
    public int lengthSegments = 50;
    public int radialSegments = 8;
    public float radius = 1;


    Vector3[] positions;
    Vector3[] normals;
    Vector4[] tangents;
    Vector2[] uvs;
    int[] triangles;

    public int totalVertCount;
    public int totalTriCount;

    MeshFilter filter;

    public void OnEnable(){
        filter = GetComponent<MeshFilter>();
        curve = GetComponent<Curve>();
        curve.BakeChanged.AddListener(BuildMesh);

    }

    public void OnDisable(){
        curve.BakeChanged.AddListener(BuildMesh);
    }



    void BuildMesh(Curve c){
        
        totalVertCount = lengthSegments * radialSegments;
        totalTriCount = (lengthSegments-1) * (radialSegments-1) * 3 * 2;

        positions = new Vector3[totalVertCount];
        normals = new Vector3[totalVertCount];
        tangents = new Vector4[totalVertCount];
        uvs = new Vector2[totalVertCount];
        triangles = new int[totalTriCount];

        // Building the triangles first
        int index  = 0;
        for( int i = 0; i < lengthSegments-1; i++){
            for( int j = 0; j < radialSegments-1; j++ ){
                

                // Getting indicies to build a tube
                int baseID = radialSegments * i + j;
                int id1 = baseID;
                int id2 = baseID + 1;
                int id3 = baseID + radialSegments;
                int id4 = baseID + radialSegments + 1;

                triangles[index++] = id1;
                triangles[index++] = id2;
                triangles[index++] = id4;
                triangles[index++] = id1;
                triangles[index++] = id4;
                triangles[index++] = id3;

            }
        }

        // reset index of array
        index  = 0;
        for( int i = 0; i < lengthSegments; i++){
            
            float lengthAlongTube = (float)i/(lengthSegments-1);
            float3 centerPos = curve.GetPositionFromValueAlongCurve( lengthAlongTube );
            float3 forward = curve.GetForwardFromValueAlongCurve(lengthAlongTube);
            for( int j = 0; j < radialSegments; j++ ){
                
                float aroundness = ((float)j/(radialSegments-1));
                float angle = aroundness * Mathf.PI*2;

                float xAmount = Mathf.Sin(angle);
                float yAmount = Mathf.Cos(angle);
                float w = curve.GetWidthFromValueAlongCurve(lengthAlongTube);
                float3 fPos = curve.GetOffsetPositionFromValueAlongCurve( lengthAlongTube , xAmount*w*radius, yAmount*w*radius );
                float3 normal = fPos - centerPos;
                float4 tangent = float4(cross(normal,forward),1);
                float2 uv = float2( lengthAlongTube, aroundness);

                positions[index] = transform.InverseTransformPoint(fPos);
                tangents[index] = float4(transform.InverseTransformDirection(tangent.xyz),1);
                normals[index] = transform.InverseTransformDirection(normal);
                uvs[ index] = uv;

                index++;

            }
        }


        Mesh m = new Mesh();

        
        m.Clear();

        m.vertices = positions;
        m.tangents = tangents;
        m.normals = normals;
        m.uv = uvs;
        m.triangles = triangles;

        filter.mesh = m;





    }

}
