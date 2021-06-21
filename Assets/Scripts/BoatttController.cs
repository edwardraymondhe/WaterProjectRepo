using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatttController : MonoBehaviour
{
    // private InputControls _controls;

    private float _throttle;
    private float _steering;

    private bool _paused;

    public Engine engine;
    
    private void Awake()
    {
        Debug.Log("Calling wake");

        // _controls = new InputControls();
        
        // _controls.BoatControls.Trottle.performed += context => _throttle = context.ReadValue<float>();
        // _controls.BoatControls.Trottle.canceled += context => _throttle = 0f;
        
        // _controls.BoatControls.Steering.performed += context => _steering = context.ReadValue<float>();
        // _controls.BoatControls.Steering.canceled += context => _steering = 0f;

        // _controls.BoatControls.Reset.performed += ResetBoat;
        // _controls.BoatControls.Pause.performed += FreezeBoat;

        // _controls.DebugControls.TimeOfDay.performed += SelectTime;
    }
    private void Update() {
        transform.position = new Vector3(transform.position.x, 2f, transform.position.z);
    }

    void FixedUpdate()
    {
        Debug.Log("Throttle: " + _throttle);
        engine.Accelerate(Input.GetAxis("BoatVer"));
        
        Debug.Log("Steering: " + _steering);

        engine.Turn(Input.GetAxis("BoatHor"));

    }
}
