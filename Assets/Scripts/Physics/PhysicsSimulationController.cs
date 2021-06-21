using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

public class PhysicsSimulationController : MonoBehaviour
{
    public float density, volume;
    public float voxelResolution = 0.51f;
    private Bounds voxelBounds;
    public Vector3 centerOfMass = Vector3.zero;
    public float waterLevelOffset = 0f;

    private const float dampner = 0.005f;
    private const float waterDensity = 1;
    private float baseDrag;
    private float baseAngularDrag;

    private Vector3 totalForce;
    private Vector3[] voxels;
    private NativeArray<Vector3> samplePoints;
    public Vector3[] heights;
    private Vector3[] normals;
    private Vector3[] velocities;
    Collider[] colliders;
    private Rigidbody rb;
    public float PercentSubmerged = 0.1f;

    private void OnDestroy()
    {
        CleanUp();
    }

    private void CleanUp()
    {
        samplePoints.Dispose();
    }

    private void Init()
    {
        voxels = null;
        SetupColliders();
        SetupVoxels();
        SetupData();
        SetupPhysical();
    }

    private void SetupData()
    {
        heights = new Vector3[voxels.Length];
        normals = new Vector3[voxels.Length];
        samplePoints = new NativeArray<Vector3>(voxels.Length, Allocator.Persistent);
    }

    private void SetupPhysical()
    {
        if (!TryGetComponent(out rb))
            rb = gameObject.AddComponent<Rigidbody>();

        rb.centerOfMass = centerOfMass + voxelBounds.center;
        baseDrag = rb.drag;
        baseAngularDrag = rb.angularDrag;
        
        velocities = new Vector3[voxels.Length];
        float archimedesForceMagnitude = waterDensity * Mathf.Abs(Physics.gravity.y) * volume;
        totalForce = new Vector3(0, archimedesForceMagnitude, 0) / voxels.Length;
        // LocalToWorldJob.SetupJob(_guid, _voxels, ref _samplePoints);
    }

    private void SetupColliders()
    {
        colliders = GetComponentsInChildren<Collider>();

        if (colliders.Length == 0)
        {
            colliders = new Collider[1];
            colliders[0] = gameObject.AddComponent<BoxCollider>();
        }    
    }

    private void SetupVoxels()
    {
        Transform tra = transform;
        Quaternion rot = tra.rotation;
        Vector3 pos = tra.position;
        Vector3 size = tra.localScale;

        // 初始化 位置、旋转、大小
        tra.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        tra.localScale = Vector3.one;
        
        voxels = null;

        List<Vector3> points = new List<Vector3>();

        Bounds oldBounds = new Bounds();

        foreach (var nextCollider in colliders)
            oldBounds.Encapsulate(nextCollider.bounds);

        voxelBounds = oldBounds;


        Vector3 oldSize = voxelBounds.size;
        Vector3 newSize = new Vector3(Mathf.Ceil(oldSize.x / voxelResolution) * voxelResolution, Mathf.Ceil(oldSize.y / voxelResolution) * voxelResolution, Mathf.Ceil(oldSize.z / voxelResolution) * voxelResolution);
        voxelBounds.size = newSize;

        for (float ix = -voxelBounds.extents.x; ix < voxelBounds.extents.x; ix += voxelResolution)
        {
            for (float iy = -voxelBounds.extents.y; iy < voxelBounds.extents.y; iy += voxelResolution)
            {
                for (float iz = -voxelBounds.extents.z; iz < voxelBounds.extents.z; iz += voxelResolution)
                {
                    float x = (voxelResolution * 0.5f) + ix;
                    float y = (voxelResolution * 0.5f) + iy;
                    float z = (voxelResolution * 0.5f) + iz;

                    Vector3 p = new Vector3(x, y, z) + voxelBounds.center;

                    bool pointIsInCollider = false;
                    foreach (var t1 in colliders)
                    {
                        Vector3 cp = Physics.ClosestPoint(p, t1, Vector3.zero, Quaternion.identity);

                        if (Vector3.Distance(cp, p) < 0.01f)
                        {
                            pointIsInCollider = true;
                            // break;
                        }
                    }

                    if(pointIsInCollider)
                        points.Add(p);
                }
            }
        }
            
        voxels = points.ToArray();
        tra.SetPositionAndRotation(pos, rot);
        tra.localScale = size;
        float voxelVolume = Mathf.Pow(voxelResolution, 3f) * voxels.Length;
        var rawVolume = oldBounds.size.x * oldBounds.size.y * oldBounds.size.z;
        print("Voxel Volume: " + voxelVolume);
        print("Raw Volume: " + rawVolume);

        volume = Mathf.Min(rawVolume, voxelVolume);
        density = gameObject.GetComponent<Rigidbody>().mass / volume;
    }

    private void OnEnable() {
        Init();
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("In update");
        for (var i = 0; i < voxels.Length; i++)
            velocities[i] = rb.GetPointVelocity(samplePoints[i]);
    }

    private void GetVelocityPoints()
    {
        // UpdateDrag(submergedAmount);
    }

    private void FixedUpdate() {
        Debug.Log("In fixedUpdate");

        float submergedAmount = 0f;

        Physics.autoSyncTransforms = false;

        // for (var i = 0; i < voxels.Length; i++)
            // BuoyancyForce(samplePoints[i], velocities[i], heights[i].y + waterLevelOffset, ref submergedAmount);
        BuoyancyForce(Vector3.zero, velocities[0], heights[0].y + waterLevelOffset, ref submergedAmount);
        
        Physics.SyncTransforms();
        Physics.autoSyncTransforms = true;

        PercentSubmerged = math.lerp(PercentSubmerged, submergedAmount, 0.25f);
        rb.drag = baseDrag + baseDrag * (PercentSubmerged * 10f);
        rb.angularDrag = baseAngularDrag + PercentSubmerged * 0.5f;
    }

    private void BuoyancyForce(Vector3 position, Vector3 velocity, float waterHeight, ref float submergedAmount)
    {
        Debug.Log("In buoyancyForce");

        Debug.Log("Position: " + position);
        Debug.Log("velocity: " + velocity);
        Debug.Log("waterHeight: " + waterHeight);
        Debug.Log("VoxelResolution: " + voxelResolution);
        
        if (!(position.y - voxelResolution < waterHeight)) return;
            
        float k = math.clamp(waterHeight - (position.y - voxelResolution), 0f, 1f);

        submergedAmount += k / voxels.Length;

        Vector3 localDampingForce = dampner * rb.mass * -velocity;
        Vector3 force = localDampingForce + math.sqrt(k) * totalForce;
        Debug.Log(force + " : " + localDampingForce + " + " + math.sqrt(k) * totalForce);
        rb.AddForceAtPosition(force, position);
    }
}
