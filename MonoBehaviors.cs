using System;
using System.IO;
using UnityEngine;
//This is going to be a .dll that controls a gameobject that will be instantiated and just "runs forever"
namespace MorichalionStuff
{
    public class OverlayController : MonoBehaviour {
        public PluginCameraHelper _helper;
        
        Vector3 position;//local, in relation to the ui camera.
        Vector3 scale;//local, in relation to the ui camera.

        public string SongTitle;
        public string coverTexData = "";

        public int score = 0;
        public int maxScore = 0;
        public float percent = 0.0f;
        
        void Start()
        {

        }
        public void OnActivate()
        {
            updatepostion = true;
            debug("OnActivate in OverlayController fired");

        }
        
        public string debugLogging = "false";
        public void debug(string str)
        {
            if (debugLogging == "true")
            {
                string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string settingLoc = Path.Combine(docPath, @"LIV\Plugins\CameraBehaviours\MoriBScam\");
                string target = Path.Combine(settingLoc, "debug.txt");

                using (var sw = new StreamWriter(target, true))
                {
                    sw.WriteLine(str);
                }
            }
        }
        void scoreUpdate(int current, int possible)
        {

        }
        private bool updatepostion = false;
       public void OnUpdate()
        {
            if(updatepostion == true)
            {
                updatepostion = false;
                try
                {
                    this.transform.parent = _helper.behaviour.manager.camera.transform;
                    //this.transform.parent = _helper.manager.camera.worldCamera.transform;
                    
                    this.transform.localPosition = position;

                    this.transform.rotation = _helper.behaviour.manager.camera.transform.rotation;
                    //this.transform.rotation = _helper.manager.camera.worldCamera.transform.rotation;
                    this.transform.Rotate(-90.0f, 0f, 0f);
                    debug("Should have updated the parent, position, and rotation of the overlay. ");
                }
                catch (System.Exception bad)
                {
                    debug("Broke bedcause: " + bad.Message.ToString());
                }

            }
        }
        public void ini(PluginCameraHelper helper, Vector3 pos, Vector3 scal)
        {
            _helper = helper;
            position = pos;
            scale = scal;
        }
    }
}