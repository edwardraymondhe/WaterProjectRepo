using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BuoyantController : MonoBehaviour
{
    public Transform[] floaters;

    public float randomForcePower = 5f;

    public float underwaterDrag = 3f;

    public float underwaterAngularDrag = 1f;

    public float airDrag = 0f;

    public float airAngularDrag = 0.05f;

    public float waterHeight = 0.0f;

    public float floatingPower = 15f;

    private Rigidbody rb;

    int floatersUnderwater;

    bool underwater;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        floatersUnderwater = 0;
        for (int i = 0; i < floaters.Length; i++)
        {
            float difference = floaters[i].position.y - waterHeight;

            // random force
            float forceX = Random.Range(-0.2f, 0.2f);
            float forceY = Random.Range(-2.5f, 2.5f);
            float forceZ = Random.Range(-0.2f, 0.2f);
            Vector3 randomForce = new Vector3(forceX, forceY, forceZ);

            rb.AddForceAtPosition(randomForcePower * randomForce, floaters[i].position, ForceMode.Force);

            if (difference < 0)
            {
                rb.AddForceAtPosition(Vector3.up * floatingPower * Mathf.Abs(difference), floaters[i].position, ForceMode.Force);
                floatersUnderwater += 1;
                if (!underwater)
                {
                    underwater = true;
                    SwitchState(true);
                }
            }
        }
        
        if (underwater && floatersUnderwater == 0)
        {
            underwater = false;
            SwitchState(false);
        }
    }

    void SwitchState(bool isUnderwater)
    {
        if (isUnderwater)
        {
            rb.drag = underwaterDrag;
            rb.angularDrag = underwaterAngularDrag;
        }
        else
        {
            rb.drag = airDrag;
            rb.angularDrag = airAngularDrag;
        }
    }
}