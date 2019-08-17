using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Threading.Tasks;
using UnityEngine.XR.ARFoundation;

public class RecordingManager : MonoBehaviour
{
    [SerializeField] private Image recordingBar;
    
    [SerializeField] private Text textBox;

    [SerializeField] private Transform arCamera;

    [SerializeField] private DepthPrediction depthPrediction;

    private float MAX_RECORDING_TIME = 60f; //in seconds
    private float recording_time = 0f;
    private bool recording = false;
    private bool isCurrentlyWriting = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartRecording() 
    {
        if(recording) 
        {
            StopAllCoroutines();
            StopRecording();
            depthPrediction.recording = false;
        }
        else
        {
            //StartCoroutine(depthPrediction.TestFreezeFrame());
            StartCoroutine(Record());
            depthPrediction.recording = true;
        }
    }

    private IEnumerator Record() 
    {
        recording = true;

        textBox.text += "RECORDING\n";

        //set up writer object and path
        string path = Path.Combine(Application.persistentDataPath, "data");
        string filePath = Path.Combine(path, DateTime.Now.ToString("yyyy-M-d-HH-mm-ss"));

        if(!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        StreamWriter writer = null;
        
        try
        {
            writer = new StreamWriter(filePath + ".json", true);
        }
        catch (Exception e)
        {
            textBox.text += e.Message + "\n";
            StopRecording();
        }

        //write whenever the writer is available and update GUI
        while(recording_time < MAX_RECORDING_TIME) 
        {
            recordingBar.fillAmount = recording_time / MAX_RECORDING_TIME;
            recording_time += Time.deltaTime;
            
            if(!isCurrentlyWriting)
                StartCoroutine(SaveJSONInfo(writer));
            
            yield return new WaitForEndOfFrame();
        }

        writer.Close();
        StopRecording();

    }

    private IEnumerator SaveJSONInfo(StreamWriter writer)
    {
        isCurrentlyWriting = true;
        
        Pose p = new Pose(arCamera.position, arCamera.rotation);
        List<PointContent> l = depthPrediction.points;
        FrameContainer fc = new FrameContainer(p,l);
        
        string json = JsonUtility.ToJson(fc);

        Task t = null;

        try
        {
            t = writer.WriteLineAsync(json);
        }
        catch (Exception e)
        {
            textBox.text += e.Message + "\n";
        }

        while (t != null && !t.IsCompleted)
        {
            yield return new WaitForEndOfFrame();
        }
        
        isCurrentlyWriting = false;
        
        yield return null;
    }

    public void StopRecording()
    {
        recordingBar.fillAmount = 0f;
        recording_time = 0f;

        recording = false;
    }
}

[Serializable]
public class FrameContainer
{
    public float logTime;
    public Pose cameraPose;
    public List<PointContent> points;

    public FrameContainer(Pose cameraPose, List<PointContent> points)
    {
        logTime = Time.time;
        this.cameraPose = cameraPose;
        this.points = points;
    }
}
