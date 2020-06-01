using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
//This is going to be a .dll that controls a gameobject that will be instantiated and just "runs forever"
namespace MorichalionStuff
{
    public class OverlayController : MonoBehaviour {
        public PluginCameraHelper _helper;

        public Vector3 position;//local, in relation to the ui camera.
           public Vector3 scale;//local, in relation to the ui camera.

        public string SongTitle;

        public int score = 0;
        public int maxScore = 0;
        public float percent = 0.0f;

        GameObject overIcon;
        TextMesh overScore;
        TextMesh overRank;
        TextMesh overPercent;
        TextMesh overTitle;

        void Start()
        {
            foreach (Transform t in this.gameObject.GetComponentInChildren<Transform>(true))
                {
                t.gameObject.layer = 10;
                    string name = t.gameObject.name;
                    //debug("Changed layer on " + t.gameObject.name);
                    //define overlay elements that need to be referened later on. 
                    if (name == "Icon")
                    {
                        overIcon = t.gameObject;
                    }
                    if (name == "Score")
                    {
                        overScore = t.gameObject.GetComponent<TextMesh>();
                    }
                    if (name == "Rank")
                    {
                        overRank = t.gameObject.GetComponent<TextMesh>();
                    }
                    if (name == "Percent")
                    {
                        overPercent = t.gameObject.GetComponent<TextMesh>();
                    }
                    if (name == "Title")
                    {
                        overTitle = t.gameObject.GetComponent<TextMesh>();
                    }
                }

        }
        public void OnActivate()
        {

        }
        public BeatSaberStatus BS;

        
        public void Update()
        {
            if (BS.menu == false)
            {
                this.gameObject.transform.localPosition = position;   
                //icon update
                if (BS.cover != "empty")
                {
                    try
                    {
                        BS.debug.Add("Should have updated the overlay");
                        byte[] bits = Convert.FromBase64String(BS.cover);
                        Texture2D t = new Texture2D(256, 256);
                        ImageConversion.LoadImage(t, bits);
                        MeshRenderer bob = overIcon.GetComponent<MeshRenderer>();
                        bob.material.SetTexture("_MainTex", t);
                    }
                    catch
                    {
                        BS.debug.Add("Failed to update the overlay");
                    }
                    BS.cover = "empty";
                }
                overScore.text = BS.score.ToString();
                overRank.text = BS.rank;
                float percentRaw = (float)BS.score / BS.currentMaxScore;
                
                string percent = (percentRaw * 100.0f).ToString().Substring(0, 4) + "%";
                overPercent.text = percent;
                overTitle.text = BS.songName;
            }
            if(BS.menu == true)
            {
                Vector3 moveaway = new Vector3(400f, 440f, 300f);
                this.gameObject.transform.position = moveaway;
            }
        }
    }

    public class RigCam
    {
        private GameObject RigCamObject; //Center point for the camera rig.
        private GameObject FloorReference; //transform on representing the head's directions on the floor. 
        public GameObject CameraLook; //CameraLook
        private List<Vector3> CameraLooks; //List of positions that that coule be assigned to 'CameraPosition'
        private Vector3 CameraTargetPosition = new Vector3(0.0f, 0.0f, 0.0f);
        private Vector3 CameraTargetPositionVelocity = new Vector3(0.0f, 0.0f, 0.0f);

        private Vector3 RigCamObjectPositionGoal;
        private Vector3 RigCamObjectVelocity = new Vector3(0.0f,0.0f,0.0f);


        private float timeMin = 10.0f;
        private float timeMax = 15.0f;
        private float waiting = 0.1f;

        public GameObject UpdateCameraWithThis;
        public Transform worlCam;
        //private Vector3 CameraPosition; //LocalPosition of the camera in relation to the rigCamObject. 
        public Vector3 PositionUpdate(PluginCameraHelper camhelper)
        {
            waiting -= Time.deltaTime;
            if (waiting < 0.0f)
            {
                waiting += UnityEngine.Random.Range(timeMin, timeMax);

                CameraTargetPosition = CameraLooks[UnityEngine.Random.Range(0, CameraLooks.Count)];
            }
            CameraLook.transform.position = Vector3.SmoothDamp(CameraLook.transform.position, CameraTargetPosition, ref CameraTargetPositionVelocity, 1.5f);
            
            
            
            //get floor position of the head. 
            Vector3 headFloorPoint = new Vector3(camhelper.playerHead.position.x, 0.0f, camhelper.playerHead.position.z);
            Vector3 aheaddistance = new Vector3(0.0f, 0.0f, 1.0f);
            Vector3 ahead = camhelper.playerHead.TransformPoint(aheaddistance);
            ahead = new Vector3(ahead.x, 0.0f, ahead.z);//Directional vector from headfloorpoint. 

            FloorReference.transform.position = headFloorPoint;
            FloorReference.transform.rotation = Quaternion.LookRotation(ahead - headFloorPoint);

            //update the look point. 

            RigCamObject.transform.position = Vector3.SmoothDamp(RigCamObject.transform.position, FloorReference.transform.TransformPoint(RigCamObjectPositionGoal), ref RigCamObjectVelocity, 1.5f);
            RigCamObject.transform.rotation = Quaternion.LookRotation(CameraLook.transform.position - RigCamObject.transform.position);

            //orlCam.position = UpdateCameraWithThis.transform.position;
            //worlCam.rotation = UpdateCameraWithThis.transform.rotation;
            camhelper.UpdateCameraPose(UpdateCameraWithThis.transform.position, UpdateCameraWithThis.transform.rotation);


            return CameraLook.transform.position;

            
        }
        public RigCam()
        {
          
            //constructor. Makes all the things. 
            RigCamObject = new GameObject();
            FloorReference = new GameObject();
            CameraLook = new GameObject();
            UpdateCameraWithThis = new GameObject();
            UpdateCameraWithThis.transform.parent = RigCamObject.transform;
            UpdateCameraWithThis.transform.localPosition = new Vector3(0.0f, 2.0f, -4.0f);
           
            RigCamObjectPositionGoal  = new Vector3(0.0f, 0.0f, 1.0f);
            CameraLooks = new List<Vector3>();
            CameraLooks.Add(new Vector3(2.0f, 0.0f, 4.0f));
            CameraLooks.Add(new Vector3(-2.0f, 0.0f, 4.0f));
            CameraLooks.Add(new Vector3(2.0f, 0.0f, -4.0f));
            CameraLooks.Add(new Vector3(-2.0f, 0.0f, -4.0f));

        }
        public void KillThis()
        {
            GameObject.Destroy(RigCamObject);
            GameObject.Destroy(FloorReference);
            GameObject.Destroy(CameraLook);
            GameObject.Destroy(UpdateCameraWithThis);
            
            

        }
    }

    public class menuCam
    {

        //Menu camera Input is the world-camera. Output is smooth-damped coords for the 
        public Transform WorldCam;
        public Vector3 camPos = new Vector3(2.0f,1.8f,-3.0f);
        public Vector3 camPosVel = new Vector3();
        public Vector3 camLook = new Vector3(0.0f,1.2f,0.0f);
        public Vector3 camLookPos = new Vector3();
        public Vector3 camLookVel = new Vector3();
        public menuCam()
        {
            
        }
        public Vector3 menuCamUpdate(PluginCameraHelper helper)
        {
            
            Vector3 pos = Vector3.SmoothDamp(WorldCam.position, camPos, ref camPosVel, .5f);
            camLookPos = Vector3.SmoothDamp(camLookPos, camLook, ref camLookVel, .5f);
            Quaternion rot = Quaternion.LookRotation(camLookPos - WorldCam.position);
            helper.UpdateCameraPose(pos, rot);

            return camLookPos;
        }

    }
}