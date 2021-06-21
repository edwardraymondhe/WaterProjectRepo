using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WaterSystem;
using Unity.Mathematics;
using Unity.Collections;

public class WaveTester : MonoBehaviour
{
    public LineRenderer lineRenderer; // the line renderer
    public Vector3[] points;
    public Vector3[] dirs;
    public float rad;
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        for(int i = 0; i < 60; i++)
        {
            float x = Mathf.Cos(((float)i / 60.0f) * (float)360);
            float z = Mathf.Sin(((float)i / 60.0f) * (float)360);
            Vector3 dir = new Vector3(x, 0, z);
            dirs[i] = dir.normalized;
            
            points[i] = new Vector3(x * rad, 0.0f, z * rad) + transform.position;
        }
    }

    private void OnEnable() {
        lineRenderer = GetComponent<LineRenderer>();
        for(int i = 0; i < 60; i++)
        {
            float x = Mathf.Cos(((float)i / 60.0f) * (float)360);
            float z = Mathf.Sin(((float)i / 60.0f) * (float)360);
            Vector3 dir = new Vector3(x, 0, z);
            dirs[i] = dir.normalized;

            points[i] = new Vector3(x * rad, 0.0f, z * rad) + transform.position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < 60; i++)
            points[i] += Time.deltaTime * dirs[i];
        
        lineRenderer.positionCount = 60;
        for (int i = 0; i < 60; i++)
            lineRenderer.SetPosition(i, points[i]);
    }
}
