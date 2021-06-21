using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

public class WaveLineManager : MonoBehaviour
{
    public WaveLine[] waveLines;

    public float wavePointSpawnSpan = 1f;
    public float currentCounter = 0f;
    void Start()
    {
        waveLines = GetComponentsInChildren<WaveLine>();
    }

    private void OnEnable() {
    }

    // Update is called once per frame
    void Update()
    {
        currentCounter += Time.deltaTime;

        if(currentCounter > wavePointSpawnSpan)
        {
            foreach (var item in waveLines)
            {
                item.InsertWavePoint();
            }
            
            currentCounter = 0;
        }
    }
}
