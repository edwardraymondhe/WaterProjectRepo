using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine.UI;

public class WaveLineManager : MonoBehaviour
{
    public WaveLine[] waveLines;

    public float wavePointSpawnSpan = 1f;
    public float waveSpeed, waveWidth;
    private float currentCounter = 0f;

    void Start()
    {
        waveLines = GetComponentsInChildren<WaveLine>();
    }

    // Update is called once per frame
    void Update()
    {
        currentCounter += Time.deltaTime;

        if(currentCounter > wavePointSpawnSpan)
        {
            foreach (var item in waveLines)
            {
                item.UpdateParam(waveSpeed, waveWidth);
                item.InsertWavePoint();
            }
            currentCounter = 0;
        }
    }
    
    public void SetWavePointSpawnSpan(Slider slider)
    {
        this.wavePointSpawnSpan = slider.value;
    }
    public void SetWaveSpeed(Slider slider)
    {
        this.waveSpeed = slider.value;
    }
    public void SetWaveWidth(Slider slider)
    {
        this.waveWidth = slider.value;
    }
}
