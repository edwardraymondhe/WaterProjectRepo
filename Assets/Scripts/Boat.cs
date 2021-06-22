using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boat : MonoBehaviour
{
    public GameObject water;

    private Rigidbody rb;

    // Start is called before the first frame update
    void Start()
    {
        rb = this.GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        float waterHeight = water.GetComponent<MyWaterSystem.Water>().GetWaveHeight(new Vector3(transform.position.x, transform.position.y, 0));
        float currentHeight = transform.position.y;
        float ratio = waterHeight / currentHeight;

        rb.AddForce(-ratio * Physics.gravity);
    }
}
