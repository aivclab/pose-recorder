using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using NetMQ;
using NetMQ.Sockets;
using UnityEngine.Experimental.Rendering;
using Newtonsoft.Json;

public class DepthPrediction : MonoBehaviour
{
    [SerializeField] private GameObject cameraObject;

    [SerializeField] private ARSessionOrigin origin;

    [SerializeField] private ARRaycastManager raycastManager;

    [SerializeField] private ARPointCloudManager pointCloudManager;

    [SerializeField] private Text textBox;
    
    [SerializeField] private ARCameraManager cameraManager;
    [SerializeField] private ARCameraBackground cameraBackground;
    [SerializeField] private RenderTexture rendTex;
    
    private Texture2D tex;
    
    private RequestSocket client = new RequestSocket();
    private bool serverWorking = false;
    public bool recording;

    public List<PointContent> points { get; private set; }

    private List<ARRaycastHit> hitResults;
    private List<float> confidences;
    private List<Vector3> featurePoints;

    //entirely here for test reasons, change to keypoints found by pose detector later
    //private Dictionary<string, Vector3> pointDict = new Dictionary<string, Vector3>();

    private Dictionary<string, string> pointConnections = new Dictionary<string, string>
    {
        {"left_eye", "nose"},
        {"left_ear", "left_eye"},
        {"right_eye", "nose"},
        {"right_ear", "right_eye"},
        {"right_shoulder", "nose"},
        {"left_shoulder", "nose"},
        {"left_elbow", "left_shoulder"},
        {"right_elbow", "right_shoulder"},
        {"right_wrist", "right_elbow"},
        {"left_wrist", "left_elbow"},
        {"left_hip", "left_shoulder"},
        {"right_hip", "right_shoulder"},
        {"left_knee", "left_hip"},
        {"right_knee", "right_hip"},
        {"left_ankle", "left_knee"},
        {"right_ankle", "right_knee"}
    };

    // Start is called before the first frame update
    void Start()
    {
        points = new List<PointContent>();
        hitResults = new List<ARRaycastHit>();
        confidences = new List<float>();
        
        client.Connect("tcp://10.24.11.87:8989");
        Debug.Log("connected");
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        /*
         * textBox.text = "";
        textBox.text += "Camera position: " + cameraObject.transform.position + "\n";
        textBox.text += "Camera rotation: " + cameraObject.transform.rotation + "\n";
        textBox.text += "Origin distance: " + Vector3.Distance(cameraObject.transform.position, 
            origin.transform.position) + "\n";

        raycastManager.Raycast(new Vector3(Screen.width / 2f, Screen.height / 2f, 0), 
            hitResults, TrackableType.All);   

        if(hitResults.Count != 0) 
        {
            textBox.text += "Hit distance: " + hitResults[0].distance + "\n";
            textBox.text += "Hit pose + " + hitResults[0].pose.position + "\n";
            textBox.text += "Hit type: " + hitResults[0].hitType + "\n";
        }
         */


        if(!serverWorking && recording)
            GrabFrame();

        //UpdateDict();


    }

    private void GrabFrame()
    {
        unsafe
        {
            try
            {
                /*
                 * TESTING GRABBING FROM CPU
                 
                XRCameraImage cameraImage;
                
                if(!cameraManager.TryGetLatestImage(out cameraImage)) 
                    cameraImage.Dispose();
                
                
                
                textBox.text += "GOT LATEST IMAGE \n";

                XRCameraImageConversionParams param = new XRCameraImageConversionParams(
                    cameraImage, TextureFormat.RGB24, CameraImageTransformation.None);



                int size = cameraImage.GetConvertedDataSize(param);
                var buffer = new NativeArray<byte>(size, Allocator.Temp);

                cameraImage.Convert(param, new IntPtr(buffer.GetUnsafePtr()), buffer.Length);

                cameraImage.Dispose();

                
                tex = new Texture2D(param.outputDimensions.x, param.outputDimensions.y, param.outputFormat, false);
                tex.LoadRawTextureData(buffer);
                tex.Apply();

                if (param.outputDimensions.x >= param.outputDimensions.y)
                {
                    tex.Resize(128, (int) ((128f / param.outputDimensions.x) * param.outputDimensions.y));
                }
                else
                {
                    tex.Resize((int) (128f / param.outputDimensions.y) * param.outputDimensions.x, 128);
                }
                */

                //tex = cameraBackground.material.mainTexture as Texture2D;
                
                serverWorking = true;
                
                Graphics.Blit(null, rendTex, cameraBackground.material);

                var activeRenderTexture = RenderTexture.active;
                RenderTexture.active = rendTex;

                if (tex == null)
                {
                    tex = new Texture2D(rendTex.width, rendTex.height, TextureFormat.RGB24, false);
                }
                
                tex.ReadPixels(new Rect(0,0, rendTex.width, rendTex.height), 0,0);
                tex.Apply();

                RenderTexture.active = activeRenderTexture;

                byte[] bytes = tex.EncodeToJPG();

                //textBox.text += bytes.Length + "\n";
                
                //buffer.Dispose();

                StartCoroutine(SendZmqRequest(bytes));

                //textBox.text += "SENDING REQUEST \n";
            }
            catch (Exception e)
            {
                textBox.text += e.ToString() + "\n";
                textBox.text += e.Message + "\n";
            }
        }
    }

    private IEnumerator SendZmqRequest(byte[] bytes)
    {
        Task task = new Task(() => client.SendFrame(bytes));
        task.Start();

        while (!task.IsCompleted && !task.IsCanceled)
        {
            Debug.Log("sending a frame");
            yield return new WaitForEndOfFrame();
        }

        string response = "";
        Task task2 = new Task(() => response = client.ReceiveFrameString());
        task2.Start();

        while (!task2.IsCompleted)
        {
            Debug.Log("waiting for response");
            yield return new WaitForEndOfFrame();
        }

        textBox.text += response + "\n";

        try
        {
            List<Dictionary<string, int[]>> l = JsonConvert.DeserializeObject<List<Dictionary<string, int[]>>>(@response);
            
            points.Clear();

            textBox.text += l.Count + " in list\n";

            int num = 0;

            foreach (Dictionary<string, int[]> dict in l)
            {
                string prefix = "person_" + num;

                textBox.text += dict.Count + " in dict\n";

                Dictionary<string, Vector3> pointDict = new Dictionary<string, Vector3>();

                foreach (KeyValuePair<string, int[]> kvp in dict)
                {
                    textBox.text += kvp.Key + " added\n";
                    
                    //hardcoded 240x426px
                    var val = (int[])kvp.Value;

                    textBox.text += "after val\n";
                    
                    float scaleX = Screen.width / 240f;
                    float scaleY = Screen.height / 426f;

                    textBox.text += "after scale\n";
                    
                    Vector3 scaledVec = new Vector3(val[0] * scaleX, val[1] * scaleY, val[2]);

                    textBox.text += "after applying scale\n";
                    
                    pointDict.Add(kvp.Key, scaledVec);

                    textBox.text += "after dict add\n";
                }
                
                UpdateDict(prefix, pointDict);
            }
        }
        catch (Exception e)
        {
            textBox.text += e.ToString();
        }
        
        yield return new WaitForEndOfFrame();


        serverWorking = false;
    }


    private void UpdateDict(string prefix, Dictionary<string, Vector3> pointDict)
    {
        textBox.text += "starting person\n";
        
        foreach (KeyValuePair<string, Vector3> kvp in pointDict)
        {
            textBox.text += "testing " + kvp.Key + " on position " + kvp.Value + "\n";

            //get known values (name of point and its screen position)
            string name = prefix + "_" + kvp.Key;
            Vector3 screenPos = kvp.Value;

            textBox.text += "after init\n";

            //attempt to get its anchor point if it is defined (ie. hand to elbow)
            pointConnections.TryGetValue(kvp.Key, out string anchor);
            anchor = prefix + "_" + anchor;

            textBox.text += "after get connection\n";

            //attempt to find its world position and confidence
            Vector3 worldPosition = new Vector3(0, 0, 0);
            float confidence = 0f;

            raycastManager.Raycast(screenPos, hitResults, TrackableType.All);

            textBox.text += "after raycast \n";
            
            if (hitResults.Count != 0)
            {
                worldPosition = hitResults[0].pose.position;

                //if we hit a keypoint, find its pointcloud
                //then find the closest point in that pointcloud (because that's totally smart, thanks unity)
                //and get its confidence
                if (hitResults[0].hitType == TrackableType.FeaturePoint)
                {
                    /*
                    var conf = pointCloudManager.trackables[hitResults[0].trackableId].confidenceValues;
                    var poss = pointCloudManager.trackables[hitResults[0].trackableId].positions;

                    float dist = float.MaxValue;
                    int possPos = 0;

                    for (int i = 0; i < poss.Length; ++i)
                    {
                        float newDist = Vector3.Distance(worldPosition, poss[i]);

                        if (newDist < dist)
                        {
                            dist = newDist;
                            possPos = i;
                        }
                    }
                    */

                    confidence = 0f; //conf[possPos];
                }
            }

            PointContent pc = new PointContent(name, worldPosition, confidence, anchor);

            points.Add(pc);
        }
    }

    public IEnumerator TestFreezeFrame()
    {
        ARPointCloudExtendable oldPointCloud = pointCloudManager.pointCloudPrefab.GetComponent<ARPointCloudExtendable>();
        NativeArray<Vector3> savedPositions = new NativeArray<Vector3>();
        savedPositions.CopyFrom(oldPointCloud.positions);

        NativeArray<ulong> savedIdentifiers = new NativeArray<ulong>();
        savedIdentifiers.CopyFrom(oldPointCloud.identifiers);

        NativeArray<float> savedConfidenceValues = new NativeArray<float>();
        savedConfidenceValues.CopyFrom(oldPointCloud.confidenceValues);

        yield return new WaitForSeconds(2f);

        oldPointCloud.OverwritePointCloud(savedPositions, savedIdentifiers, savedConfidenceValues);
    }
}

[Serializable]
public struct PointContent
{
    public string name;
    public Vector3 worldPosition;
    public float confidence;
    public string linkName;

    public PointContent(string name, Vector3 world_position, float confidence, string link_name)
    {
        this.name = name;
        this.worldPosition = world_position;
        this.confidence = confidence;
        this.linkName = link_name;
    }
}

[Serializable]
public struct ResponseType
{
    public string name;
    public int[] data;
}