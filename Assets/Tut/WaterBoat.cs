﻿using Ditzelgames;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [RequireComponent(typeof(WaterFloat))]
public class WaterBoat : MonoBehaviour
{
    //visible Properties
    public Transform Motor;
    public float SteerPower = 500f;
    public float Power = 5f;
    public float MaxSpeed = 10f;
    public float Drag = 0.1f;
    public float RotateSpeed = 50f;
    public float height = 2f;

    //used Components
    protected Rigidbody Rigidbody;
    protected ParticleSystem ParticleSystem;

    public void Awake()
    {
        ParticleSystem = GetComponentInChildren<ParticleSystem>();
        Rigidbody = GetComponent<Rigidbody>();
    }

    public void FixedUpdate()
    {
        //default direction
        var forceDirection = transform.forward;
        var steer = 0;

        //steer direction [-1,0,1]
        if (Input.GetKey(KeyCode.A))
            steer = 1;
        if (Input.GetKey(KeyCode.D))
            steer = -1;

        //Rotational Force
        Rigidbody.AddForceAtPosition(steer * transform.right * SteerPower / (100f - RotateSpeed), Motor.position);

        //compute vectors
        var forward = Vector3.Scale(new Vector3(1,0,1), transform.forward);
        var targetVel = Vector3.zero;

        //forward/backward poewr
        if (Input.GetKey(KeyCode.W))
            PhysicsHelper.ApplyForceToReachVelocity(Rigidbody, forward * MaxSpeed, Power);

        //Motor Animation // Particle system
        // Motor.SetPositionAndRotation(Motor.position, transform.rotation * StartRotation * Quaternion.Euler(0, 30f * steer, 0));
        // if (ParticleSystem != null)
        // {
        //     if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S))
        //         ParticleSystem.Play();
        //     else
        //         ParticleSystem.Pause();
        // }

        //moving forward
        var movingForward = Vector3.Cross(transform.forward, Rigidbody.velocity).y < 0;

        //move in direction
        Rigidbody.velocity = Quaternion.AngleAxis(Vector3.SignedAngle(Rigidbody.velocity, (movingForward ? 1f : 0f) * transform.forward, Vector3.up) * Drag, Vector3.up) * Rigidbody.velocity;

        //camera position
        //Camera.transform.LookAt(transform.position + transform.forward * 6f + transform.up * 2f);
        //Camera.transform.position = Vector3.SmoothDamp(Camera.transform.position, transform.position + transform.forward * -8f + transform.up * 2f, ref CamVel, 0.05f);

        // Fake sailing(before adding buoyant)
        // Vector3 pos = transform.position;
        // transform.position = new Vector3(pos.x, height, pos.z);
    }

    public static void ApplyForceToReachVelocity(Rigidbody rigidbody, Vector3 velocity, float force = 1, ForceMode mode = ForceMode.Force)
    {
        if (force == 0 || velocity.magnitude == 0)
            return;

        velocity = velocity + velocity.normalized * 0.2f * rigidbody.drag;

        //force = 1 => need 1 s to reach velocity (if mass is 1) => force can be max 1 / Time.fixedDeltaTime
        force = Mathf.Clamp(force, -rigidbody.mass / Time.fixedDeltaTime, rigidbody.mass / Time.fixedDeltaTime);

        //dot product is a projection from rhs to lhs with a length of result / lhs.magnitude https://www.youtube.com/watch?v=h0NJK4mEIJU
        if (rigidbody.velocity.magnitude == 0)
            rigidbody.AddForce(velocity * force, mode);
        else
        {
            var velocityProjectedToTarget = (velocity.normalized * Vector3.Dot(velocity, rigidbody.velocity) / velocity.magnitude);
            rigidbody.AddForce((velocity - velocityProjectedToTarget) * force, mode);
        }
    }

}