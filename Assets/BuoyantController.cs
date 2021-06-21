using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuoyantController : MonoBehaviour
{
    public Transform[] floaters;
    public float underWaterDrag = 3f;
    public  float underWaterAngularDrag = 1f;
    public float airDrag = 0f;
    public float airAngularDrag = 0.05f;
    public float floatingPower = 15f;

    public float waterHeight = 0f;
    Rigidbody m_Rigidbody;
    bool underWater;
    int floatersUnderwater;
    private void Start() {
        m_Rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate() {
        for (int i = 0; i < floaters.Length; i++)
        {
            float diff = floaters[i].position.y - waterHeight;

        if(diff < 0)
            m_Rigidbody.AddForceAtPosition(Vector3.up * floatingPower * Mathf.Abs(diff), floaters[i].position, ForceMode.Force);
            floatersUnderwater += 1;
            if(!underWater)
            {
                underWater = true;
                SwitchState(true);
            }
        }
        
        if(underWater && floatersUnderwater == 0)
        {
            underWater = false;
            SwitchState(true);
        }
    }

    void SwitchState(bool isUnderwater)
    {
        if(isUnderwater)
        {
            m_Rigidbody.drag = underWaterDrag;
            m_Rigidbody.angularDrag = underWaterAngularDrag;
        }else{
            m_Rigidbody.drag = airDrag;
            m_Rigidbody.angularDrag = airAngularDrag;
        }
    }
}
