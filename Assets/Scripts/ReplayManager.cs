using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using SimpleFileBrowser;
using System.Threading.Tasks;

public class ReplayManager : MonoBehaviour
{
    [SerializeField] private Camera mainCam;
    [SerializeField] private GameObject marker;
    
    //list of loaded frames
    private List<FrameContainer> frameList = new List<FrameContainer>();
    private Dictionary<string, GameObject> markers = new Dictionary<string, GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void StartLoadFile()
    {
        FileBrowser.ShowLoadDialog(delegate(string path) { SuccessLoadFile(path); },
            delegate { CancelLoadFile(); }, false, Application.persistentDataPath,
            "Load File", "Select");
    }

    private void SuccessLoadFile(string path)
    {
        using (StreamReader reader = new StreamReader(path))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                try
                {
                    FrameContainer fc = JsonUtility.FromJson<FrameContainer>(line);
                    frameList.Add(fc);
                    
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                }
                
            }
        }
        
        Debug.Log(frameList.Count + " count");

        StartCoroutine(LoopFrames());
    }

    private IEnumerator LoopFrames()
    {
        Dictionary<string, PointContent> listPoints = new Dictionary<string, PointContent>();
        float startTime = Time.time;
        float logStartTime = frameList[0] != null ? frameList[0].logTime : 0f;

        foreach(FrameContainer fc in frameList) 
        {
            //wait until we are caught up to logtime
            yield return new WaitWhile(() => Time.time - startTime < fc.logTime - logStartTime);

            //add potential new points to list
            foreach(PointContent pc in fc.points) 
            {
                if(listPoints.ContainsKey(pc.name)) 
                {
                    if(pc.worldPosition != Vector3.zero)
                        listPoints[pc.name] = pc;
                }
                else 
                {
                    listPoints.Add(pc.name, pc);
                    GameObject go = Instantiate(marker);
                    go.name = pc.name;
                    markers.Add(pc.name, go);
                }
            }

            //move points about
            foreach(KeyValuePair<string, PointContent> kvp in listPoints) 
            {
                if(kvp.Value.worldPosition != Vector3.zero)
                    markers[kvp.Key].transform.position = kvp.Value.worldPosition;
                
                Vector3 mPos = markers[kvp.Key].transform.position;
                
                //check if the link anchor exists and set anchors accordingly
                if(markers.ContainsKey(kvp.Value.linkName))
                {
                    Vector3 aPos = markers[kvp.Value.linkName].transform.position;
                    
                    markers[kvp.Key].GetComponent<LineRenderer>().SetPositions(new Vector3[]{mPos,aPos});
                }
                else
                {
                    Debug.Log(kvp.Value.linkName);
                    markers[kvp.Key].GetComponent<LineRenderer>().SetPositions(new Vector3[]{mPos,mPos});
                }
            }

            //move camera about
            mainCam.transform.position = fc.cameraPose.position;
            mainCam.transform.rotation = fc.cameraPose.rotation;
        }
    }

    private void CancelLoadFile()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
