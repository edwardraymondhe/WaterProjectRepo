using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WavePoint
{
    public float aliveTime;     // 质点当前已存活时间
    public Vector3 position;    // 质点当前位置
    public Vector3 direction;   // 质点初始方向
    public WavePoint(Transform tran, float dir)
    {
        position = tran.position;
        direction = tran.right * (dir > 0 ? 1.0f : -1.0f);
        aliveTime = 0f;
    }
}
public class WaveLine : MonoBehaviour{
    [Header("WaveLine Initialization")]
    public LineRenderer lineRenderer;
    public float wavePointDirection = 1.0f;
    public GameObject wavePointOrigin;
    private List<WavePoint> wavePoints;
    private int wavePointCount = 0;
    private float wavePointLifeTime = 10f;
    
    [Header("Editable")]
    public float waveSpeed = 1.0f;
    public float waveWidth = 0.4f;

    private void Start() {
        // 获取 Linerenderer 并初始化
        lineRenderer = GetComponent<LineRenderer>();
        wavePoints = new List<WavePoint>();
    }

    private void Update() {
        // 用于实时更新 Linerenderer 的宽度
        if(lineRenderer.widthMultiplier != waveWidth)
            lineRenderer.widthMultiplier = waveWidth;

        for (int i = wavePoints.Count - 1; i >= 0; i--)
        {
            WavePoint wavePoint = wavePoints[i];

            // 删除过期质点
            if(wavePoint.aliveTime > wavePointLifeTime)
            {
                wavePoints.RemoveAt(i);
            }else{
                
                wavePoint.aliveTime += Time.deltaTime;

                // 避免 tmp < 0 的可能性
                float tmp = 1.0f - wavePoint.aliveTime / wavePointLifeTime;

                // 根据质点方向在每次Update，更新其在原本位移方向上的位移结果
                wavePoint.position += waveSpeed * Mathf.Clamp(tmp, 0.001f, 1.0f) * Time.deltaTime * wavePoint.direction;
                wavePoint.position = new Vector3(wavePoint.position.x, 1f, wavePoint.position.z);
            }
        }

        // 将质点迭代后的位置信息 更新至 Linerenderer中的质点数组
        wavePointCount = wavePoints.Count;
        lineRenderer.positionCount = wavePointCount;
        lineRenderer.SetPosition(0, wavePointOrigin.transform.position);
        for (int i = 0; i < wavePointCount; i++)
            lineRenderer.SetPosition(i, wavePoints[i].position);
    }

    public void InsertWavePoint() {
        wavePoints.Insert(0, new WavePoint(wavePointOrigin.transform, wavePointDirection));
    }

    public void UpdateParam(float spd, float wid) {
        waveSpeed = spd;
        waveWidth = wid;
    }
}