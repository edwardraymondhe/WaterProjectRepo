using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform targetPosition;
    public Transform boatPosition;
    public float dampFactor = 1;
    public Vector3 cameraOffset;
    void Update()
    {
        transform.LookAt(targetPosition);
        Vector3 cameraPosition = boatPosition.position + cameraOffset;
        transform.position = Vector3.Lerp(transform.position, cameraPosition, dampFactor * Time.deltaTime);
    }
}
