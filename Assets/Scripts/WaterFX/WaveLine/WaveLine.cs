using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WavePoint
{
    public float aliveTime;
    public Vector3 position;
    public Vector3 direction;
    public WavePoint(Transform tran, float dir)
    {
        position = tran.position;
        direction = tran.right * (dir > 0 ? 1.0f : -1.0f);

        aliveTime = 0f;
    }
}
public class WaveLine : MonoBehaviour{
    public LineRenderer lineRenderer; // the line renderer
    public List<WavePoint> wavePoints;

    public float wavePointLifeTime = 10f;

    public int wavePointCount = 0;
    public float wavePointDirection = 1.0f;
    public GameObject wavePointOrigin;

    private void Start() {
        lineRenderer = GetComponent<LineRenderer>();
        wavePoints = new List<WavePoint>();
    }

    private void Update() {

        for (int i = wavePoints.Count - 1; i >= 0; i--)
        {
            WavePoint wavePoint = wavePoints[i];
            if(wavePoint.aliveTime > wavePointLifeTime)
            {
                wavePoints.RemoveAt(i);
            }else{
                wavePoint.aliveTime += Time.deltaTime;
                wavePoint.position += (1.0f - wavePoint.aliveTime / wavePointLifeTime )* Time.deltaTime * wavePoint.direction;
            }
        }
        wavePointCount = wavePoints.Count;


        lineRenderer.positionCount = wavePointCount + 1;
        lineRenderer.SetPosition(0, wavePointOrigin.transform.position);
        for (int i = 0; i < wavePointCount; i++)
            lineRenderer.SetPosition(i + 1, wavePoints[i].position);
    }

    public void InsertWavePoint() {
        wavePoints.Insert(0, new WavePoint(wavePointOrigin.transform, wavePointDirection));
    }
}