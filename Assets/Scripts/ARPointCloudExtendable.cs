using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

[DefaultExecutionOrder(ARUpdateOrder.k_PointCloud)]
[DisallowMultipleComponent]
public class ARPointCloudExtendable : ARPointCloud
{
    public void OverwritePointCloud(NativeArray<Vector3> savedPositions, NativeArray<ulong> savedIdentifiers, NativeArray<float> savedConfidenceValues)
    {
        
    }
}
