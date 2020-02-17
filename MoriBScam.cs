/*
Copyright 2019 LIV inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software 
and associated documentation files (the "Software"), to deal in the Software without restriction, 
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED 
TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, 
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/



using System;
using System.IO;

using UnityEngine;
using WebSocketSharp;
using Newtonsoft.Json.Linq;


// User defined settings which will be serialized and deserialized with Newtonsoft Json.Net.
// Only public variables will be serialized.
public class MoriBScamPluginSettings : IPluginSettings {
    public float fov = 60f;
    public float distance = 2f;
    public float speed = 1f;
}

// The class must implement IPluginCameraBehaviour to be recognized by LIV as a plugin.
public class MoriBScam : IPluginCameraBehaviour {
    
    // Store your settings localy so you can access them.
    MoriBScamPluginSettings _settings = new MoriBScamPluginSettings();

    // Provide your own settings to store user defined settings .   
    public IPluginSettings settings => _settings;

    // Invoke ApplySettings event when you need to save your settings.
    // Do not invoke event every frame if possible.
    public event EventHandler ApplySettings;

    // ID is used for the camera behaviour identification when the behaviour is selected by the user.
    // It has to be unique so there are no plugin collisions.
    public string ID => "MoriBScam";
    // Readable plugin name "Keep it short".
    public string name => "Mori's Beat Saber Cam";
    // Author name.
    public string author => "Morichalion";
    // Plugin version.
    public string version => "0.1.0";
    // Localy store the camera helper provided by LIV.
    PluginCameraHelper _helper;
    //float _elaspedTime;

    //websocket declaration for Beat Saber HTTP status
    private WebSocket ws;
    private string wsStatus = "closed";
    private float wsRetry = 5.0f;

    //websocket for Twitch
    WebSocket tw;
    private string twStatus = "closed";
    private float twRetry = 5.0f;
    //Login info for twitch.
    private string twOath = "none";
    private string twName = "none";


    /* camera-type information */
    //phase 0= menu 1= game
    public int phase = 0;
    //gctype is the game-cameray type (fp, menu, follow). 
    public string gctype = "menu";

    //transform for smooth camera stuff. 
    public GameObject headTarget;

    //custom camera stuff
    //velocity and positions (for SmoothDamp)
    public float camSpeed = .5f;
    public Vector3 camPositionVelocity = Vector3.zero;
    public Vector3 camLookVelocity = Vector3.zero;
    public Vector3 camPosition = Vector3.zero;
    public Vector3 camLook = Vector3.zero;
    public Vector3 camPositionTarget = Vector3.zero;
    public Vector3 camLookTarget = Vector3.zero;
    //camera quaternion, for the FP camera.
    public Quaternion camRotation;

    //Menu Targets
    public Vector3 MenuCamPosition = new Vector3(2.0f, 1.8f, -3f);
    public Vector3 MenuCamLook = new Vector3(0f, 1.2f, 0f);

    //Dynamic Cam Targets(to be defined in relation to headTarget
    public Vector3 DynCamPosition = new Vector3(1.0f,2.3f,-3f);
    public Vector3 DynCamLook = new Vector3(.4f,0.3f,3.0f);

    /*
     * Score overlay stuff.
     * A few basic things for now. Will add things later on, I s'pose.
     */
    //Unity Components. These items get updated in OnUpdate. 
    public GameObject overlay;
    public TextMesh overScore;
    public TextMesh overPercent;
    public TextMesh overRank;
    public TextMesh overTitle;
    public GameObject overIcon;
    //overlay variables. These get updated at the websocket. 
    public string coverTexData = "empty";
    public string SongTitle = "empty";
    public string rank = "SSS";
    public string scorePercent = "100%";
    public string score = "Score: 0";
    public int overlayLayerCur = 4;
    //overlayLayer is a check  for which layer it's all supposed to be on. Gets updated in OnUpdate
    public int overlayLayer = 0;
    public bool overlayactive = false; //false ensures nothing gets done. true means all the overlay stuff works. 

    // Constructor is called when plugin loads
    public MoriBScam() { }

    
    public Vector3 StringToVector3(string str)
    {
        //This is going to change the string value from the file into a Vector3
        
        string[] a = str.Split('(',')');
        string[] b = a[1].Split(',');
        string[] args = { b[0], b[1], b[2] };
        float[] vals = { 0.0f, 0.0f, 0.0f };
        Vector3 vec;

        try
        {   
            vals[0] = float.Parse(args[0]);
            vals[1] = float.Parse(args[1]);
            vals[2] = float.Parse(args[2]);

            return vec = new Vector3(vals[0], vals[1], vals[2]);

        }
        catch
        {
            return vec = new Vector3(vals[0], vals[1], vals[2]);
        }
        /*
        foreach (string i in b)
        {
            debug("Reading someting from ini.txt: " + i);
            return null;
        }
        */
    }
    public string ResourceLoc;
    public void CheckSettings()
    {
        string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string settingLoc = Path.Combine(docPath, @"LIV\Plugins\CameraBehaviours\MoriBScam\");
        //create the directory in the off-chance that it doesn't exist.
        Directory.CreateDirectory(settingLoc);
        ResourceLoc = settingLoc;

        settingLoc = Path.Combine(settingLoc, "ini.txt");
        //Check to see if the file exists
        if (File.Exists(@settingLoc))
        {
            //reads settings from the file.
            try
            {
                using (StreamReader sr = new StreamReader(@settingLoc))
                {
                    //apply settings
                    string line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("MenuCamPosition"))
                        {
                            string[] blah = line.Split('=');
                            MenuCamPosition = StringToVector3(blah[1]);
                        }
                        if (line.Contains("MenuCamLook"))
                        {
                            string[] blah = line.Split('=');
                            MenuCamLook = StringToVector3(blah[1]);
                        }
                        if (line.Contains("DynCamPosition"))
                        {
                            string[] blah = line.Split('=');
                            DynCamPosition = StringToVector3(blah[1]);
                        }
                        if (line.Contains("DynCamLook"))
                        {
                            string[] blah = line.Split('=');
                            DynCamLook = StringToVector3(blah[1]);
                        }
                        if(line.Contains("Twitch Oath ="))
                        {
                            string[] blah = line.Split('=');
                            twOath = blah[1].Trim();
                        }
                        if (line.Contains("Twitch Name ="))
                        {
                            string[] blah = line.Split('=');
                            twName = blah[1].Trim();
                        }
                        if (line.Contains("Overlay ="))
                        {
                            if (!line.Contains("no"))
                            {
                                overlayactive = true;
                                debug("Overlay should be active now. ");
                            }
                        }
                    }
                }
            }
            catch(Exception e)
            {
                debug(e.Message);
            }
        }
        else
        {
            //creates the file.
            using (var ws = new StreamWriter(@settingLoc))
            {
                ws.WriteLine("*Morichalion's Lazy Streamer Beat Saber Camera for LIV Avatars settings.");
                ws.WriteLine("*Units are in Meters.");
                ws.WriteLine("*");
                ws.WriteLine("*Left-Right,Up-Down,Forward-Back");
                ws.WriteLine("*");
                ws.WriteLine("*Menu camera settings. Origin is your playspace center.");
                ws.WriteLine("MenuCamPosition = " + MenuCamPosition.ToString());
                ws.WriteLine("MenuCamLook = " + MenuCamLook.ToString());
                ws.WriteLine("*");
                ws.WriteLine("*");
                ws.WriteLine("*Dynamic camera settings. Location and rotation are in relation to your headset.");
                ws.WriteLine("DynCamPosition = " + DynCamPosition.ToString());
                ws.WriteLine("DynCamLook = " + DynCamLook.ToString());
                ws.WriteLine("*");
                ws.WriteLine("*");
                ws.WriteLine("**Twitch Login Info, for viewer-controlled cameras**");
                ws.WriteLine("Twitch Oath = " + twOath);
                ws.WriteLine("Twitch Name = " + twName);
                ws.WriteLine("*");
                ws.WriteLine("*");
                ws.WriteLine("*Overlay. Want it on? yap or no *");
                ws.WriteLine("Overlay = no");


                //Parse.TryParseFloat("0.0");
            }
        }
    }
    /*
     * Twitch websocket methods go here.
     */
    public void twitchMessage(string str)
    {
        tw.Send("PRIVMSG #morichalion :" + str);
    }



    // OnActivate function is called when your camera behaviour was selected by the user.
    // The pluginCameraHelper is provided to you to help you with Player/Camera related operations.
   

    public void OnActivate(PluginCameraHelper helper) {
        //local camera helper reference
        _helper = helper;

        //find and load settings.
        debug("Doin' settings");
        CheckSettings();


        /* overlay stuff defined here. */
        if(overlayactive == true)
        {
            //Trying to instantiate an arbitrary prefab. 
            string assetPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string p = Path.Combine(assetPath, @"LIV\Plugins\CameraBehaviours\MoriBScam\overlay.yap");
            //debug("gettting items from here: " + p);
            var myasset = AssetBundle.LoadFromFile(p);
            var prefab = myasset.LoadAsset<GameObject>("Overlay");
            overlay = UnityEngine.Object.Instantiate(prefab);
            //set layers for each object, assign each to variables in turn.
            foreach (Transform t in overlay.GetComponentInChildren<Transform>(true))
            {
               t.gameObject.layer = overlayLayerCur;
                string name = t.gameObject.name;
                //debug("Changed layer on " + t.gameObject.name);
                //define overlay elements that need to be referened later on. 
                if(name == "Icon")
                {
                    overIcon = t.gameObject;
                }
                if(name == "Score")
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



            //Find reference WorldCamera so UI stuff can be attached to it. 
            foreach (Camera c in Camera.allCameras)
            {
                if (c.name == "WorldCamera")
                {
                //Found WorldCamera. Set over as child and resize/rotate it.
                
                    overlay.transform.parent = c.transform;
                    overlay.transform.transform.rotation = c.transform.rotation;
                    overlay.transform.transform.localPosition = new Vector3 (0f, .05f, 1.0f);
                    overlay.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    overlay.transform.transform.Rotate(-90.0f, 0f, 0f);
                }
            }
        }

        






        //headTarget, used for the smoothdamp target for the follow cam. 
        headTarget = new GameObject();
        headTarget.transform.rotation = _helper.playerHead.rotation;
        headTarget.transform.position = _helper.playerHead.position;


        //BS websocket
        ws = new WebSocket("ws://localhost:6557/socket");
        ws.OnOpen += (sender, e) => {
            debug("BS connection open");
            wsStatus = "open";
        };
        ws.OnMessage += (sender, e) =>
        {

            //Triggered when menu gets selected. Resets all overlay values to default. 
            if (e.Data.Contains("scene\":\"Menu"))
            {
                phase = 0;
                gctype = "menu";
                overlayLayerCur = 4;
                score = "0  ";
                scorePercent = "100%";
                debug("Should be entering Menu Phase.");


            }

            //song phase (select which ones)
            if (e.Data.Contains("scene\":\"Song"))
            {
                phase = 1;
                overlayLayerCur = 10;
                score = "0  ";
                scorePercent = "100%";
                debug("Should be entering Song Phase.");

                //set up the cover texture
                JObject menuInfo = JObject.Parse(e.Data);
                SongTitle = menuInfo["status"]["beatmap"]["songName"].ToString();
                coverTexData = menuInfo["status"]["beatmap"]["songCover"].ToString();
                //'standard' map types
                if (e.Data.Contains("mode\":\"SoloStandard") || e.Data.Contains("mode\":\"PartyStandard") || e.Data.Contains("mode\":\"SoloNoArrows") || e.Data.Contains("mode\":\"PartyNoArrows"))
                {
                    gctype = "menu";
                }
                //'360 and 90 degree maps
                if (e.Data.Contains("mode\":\"Party360Degree") || e.Data.Contains("mode\":\"Party90Degree") || e.Data.Contains("mode\":\"Solo360Degree") || e.Data.Contains("mode\":\"Solo90Degree"))
                {
                    gctype = "follow";
                }

            }//ScoreChanged event. 
            if (e.Data.Contains("scoreChanged"))
            {
                //gets score information for the overlay
                JObject hit = JObject.Parse(e.Data);
                //Regular score. 
                score = hit["status"]["performance"]["score"].ToString();

                //Percent
                int curScore = (int)hit["status"]["performance"]["score"];
                int maxScore = (int)hit["status"]["performance"]["currentMaxScore"];
                float curPercent = (float)curScore / maxScore;
                curPercent *= 100.0f;
                scorePercent = curPercent.ToString().Substring(0, 4) + "%";
                //rank
                rank = hit["status"]["performance"]["rank"].ToString();

            }
            if (e.Data.Contains("t\":\"noteCut"))
            {
                JObject slice = JObject.Parse(e.Data);
                int combo = (int)slice["status"]["performance"]["combo"];              
            }
            if (e.Data.Contains("t\":\"noteMissed"))
            {
                JObject slice = JObject.Parse(e.Data);
                int combo = (int)slice["status"]["performance"]["combo"];
            }
        };
        ws.OnClose += (sender, e) =>
        {
            debug("Beat Saber websocket closed because: " + e.Reason);
            debug("Retry timer is at " + wsRetry.ToString());
            debug("ws Status was " + wsStatus);
            wsStatus = "closed";
            wsRetry = 0.0f;
        };
        ws.OnError += (sender, e) =>
        {
            debug("Beatsaber websocket error: " + e.Message);
        };
        


        //BS websocket End



        /*
         * Twitch Websocket Configuration.
         */
        // debug("Starting twitch IRC config");
        if (twOath != "none")
        {
            tw = new WebSocket("ws://irc-ws.chat.twitch.tv:80");
            debug("'new WebSocket' worked");
            tw.OnOpen += (sender, e) =>
            {
                twStatus = "open";
                debug("Should be connected to Twitch IRC now. ");
                //send configuration info
                //Authentication
                tw.Send("PASS " + twOath);
                tw.Send("NICK " + twName);
                tw.Send("CAP REQ :twitch.tv/tags");
                tw.Send("CAP REQ :twitch.tv/commands");
                tw.Send("JOIN #" + twName);
            };
            tw.OnMessage += (sender, e) =>
            {

            if(e.Data.StartsWith("PING"))
            {
                tw.Send("PONG :tmi.twitch.tv");
            }
            else
            {
                //Split into array.
                char[] delim = { ' ' };
                string[] str = e.Data.Split(delim,5);
                foreach(string i in str)
                {
                    //debug(i);
                }
                if(str[2] == "PRIVMSG")
                {
                    //All the User-Command-type stuff goes here. 
                    if (str[0].Contains("broadcaster/") || str[0].Contains("moderator/") || str[0].Contains("vip/"))
                    {
                        //Moderator-level protected twitch command stuff.
                        //debug("Broadcaster or Moderator is detected");
                        if (str[4].StartsWith(":!cam"))
                        {
                            //cam command. 
                            //twitchMessage("Cam Command detected!");
                            //debug("Should have posted a note to the chat");
                            if (str[4].Contains("fp"))
                            {
                                gctype = "fp";
                                twitchMessage("GameCam to FP Mode");
                                debug("GameCam to FP Mode");
                            }
                            else if (str[4].Contains("follow"))
                            {
                                gctype = "follow";
                                twitchMessage("GameCam to Follow Mode");
                                debug("GameCam to Follow Mode");
                            }
                            else if (str[4].Contains("menu"))
                            {
                                gctype = "menu";
                                twitchMessage("GameCam to Menu Mode");
                                debug("GameCam to Menu Mode");
                            }
                        }
                    }
                }
            }
        };
            tw.OnError += (sender, e) =>
        {
            tw.CloseAsync();
        };
            tw.OnClose += (sender, e) =>
        {
            twStatus = "closed";
        };
        }

    }



    // OnSettingsDeserialized is called only when the user has changed camera profile or when the.
    // last camera profile has been loaded. This overwrites your settings with last data if they exist.
    public void OnSettingsDeserialized() {
        
    }

    // OnFixedUpdate could be called several times per frame. 
    // The delta time is constant and it is ment to be used on robust physics simulations.
    public void OnFixedUpdate() {

    }

    // OnUpdate is called once every frame and it is used for moving with the camera so it can be smooth as the framerate.
    // When you are reading other transform positions during OnUpdate it could be possible that the position comes from a previus frame
    // and has not been updated yet. If that is a concern, it is recommended to use OnLateUpdate instead.
    public void OnUpdate() {
        
        Transform headTransform = _helper.playerHead;

        //overlay stuff. Should only be messed with if it's active. 
        if(overlayactive == true)
        { 
            overPercent.text = scorePercent;
            overScore.text = "Score: " + score;
            overRank.text = rank;
            overTitle.text = SongTitle;
            /*
            * set the overlay layer. OverlayLayerCur is defined in a websocket event.
            * updating the overlay needs to be done during OnUpdate();
            */
            if(overlayLayerCur != overlayLayer)
        {
            foreach (Transform t in overlay.GetComponentInChildren<Transform>(true))
            {
                t.gameObject.layer = overlayLayerCur;
            }
            overlayLayer = overlayLayerCur;
        }
            /*
             * Updating the icon. 
             * coverTexData gets updated in the websocket. 
             */
            if(coverTexData != "empty")
            {
                //update the icon.
                byte[] bits = Convert.FromBase64String(coverTexData);
                Texture2D t = new Texture2D(256, 256);
                ImageConversion.LoadImage(t, bits);
                MeshRenderer bob = overIcon.GetComponent<MeshRenderer>();
                bob.material.SetTexture("_MainTex", t);
                coverTexData = "empty";
            }
        }
        //is the BS websocket connected?

        if(wsStatus == "closed")
        {
            wsRetry += Time.deltaTime;
            if(wsRetry > 1.0f)
            {
                debug("Attempting to connect to BS");
                wsStatus = "open";
                wsRetry = 0.0f;
                ws.ConnectAsync();
            }
        }
        //is the Twitch websocket connected Only checked for is it's got login info for twitch. 
        if (twOath != "none")
        {
            if (twStatus == "closed")
            {
                twRetry += Time.deltaTime;
                if (twRetry > 5.0f)
                {
                    debug("Attemption to connect to Twitch IRC");
                    debug("oath is " + twOath);
                    twRetry = 0.0f;
                    twStatus = "open";
                    tw.ConnectAsync();
                }
            }
        }
        
        

        //check for angle tolerance (only affects rotation camera
        if (Quaternion.Angle(headTransform.rotation, headTarget.transform.rotation) > 15.0f)
        {
            headTarget.transform.rotation = headTransform.rotation;
        }
        //update camLook and camPosition (different methods depending on phases. 
        
        
        if (phase == 0)
        {
            //Menu camera
            MenuCam();
        }
        if (phase == 1)
        {
            //Game Camera
            if (gctype == "fp")
            {
                FPCam();
            }else if (gctype == "menu")
            {
                MenuCam();
            }
            else if(gctype == "follow")
            {
                FollowCam();
            }
        }        
    }

    // OnLateUpdate is called after OnUpdate also everyframe and has a higher chance that transform updates are more recent.
    public void OnLateUpdate() {
    }

    // OnDeactivate is called when the user changes the profile to other camera behaviour or when the application is about to close.
    // The camera behaviour should clean everything it created when the behaviour is deactivated.
    public void OnDeactivate() {
        // Saving settings here
        
        ws.CloseAsync();
        if (twOath != "none")
        {
            tw.CloseAsync();
        }
        if (overlayactive == true)
        {
            UnityEngine.Object.Destroy(overlay);
        }

        ApplySettings?.Invoke(this, EventArgs.Empty);
    }

    // OnDestroy is called when the users selects a camera behaviour which is not a plugin or when the application is about to close.
    // This is the last chance to clean after your self.
    public void OnDestroy() {

    }
    public void debug(string str)
    {
        /*
        string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string settingLoc = Path.Combine(docPath, @"LIV\Plugins\CameraBehaviours\MoriBScam\");
        string target = Path.Combine(settingLoc, "debug.txt");
        
        using (var sw = new StreamWriter(target, true))
        {
            sw.WriteLine(str);
        }
        */
    }
    public void MenuCam()
    {
        //camera for the menu. Just a static camera.
        camPositionTarget = MenuCamPosition;
        camLookTarget = MenuCamLook;

        camPosition = Vector3.Lerp(camPosition, camPositionTarget, 3.0f *Time.deltaTime);
        camLook = Vector3.Lerp(camLook, camLookTarget, 3.0f*Time.deltaTime);
        
        Vector3 lookvector = camLook - camPosition;
        camRotation = Quaternion.LookRotation(lookvector);

        _helper.UpdateCameraPose(camPosition, camRotation);
        _helper.UpdateFov(_settings.fov);


    }
    public void FollowCam()
    {
        /*
         * camera for 360 and 90 degree levels.
         * Basic idea is that there's going to be a couple of vectors 
         * that get smoothdamped between current camera location 
         * and head location/rotation assuming location and/or rotation
         * is out of certain tolerances. 
         */
        Transform headTransform = _helper.playerHead;
        if (Quaternion.Angle(headTransform.rotation, headTarget.transform.rotation) > 15.0f)
        {
            headTarget.transform.rotation = headTransform.rotation;
        }

        camPositionTarget = headTarget.transform.TransformPoint(DynCamPosition);
        camLookTarget = headTarget.transform.TransformPoint(DynCamLook);


        camPosition = Vector3.SmoothDamp(camPosition, camPositionTarget, ref camPositionVelocity, camSpeed);
        camLook = Vector3.SmoothDamp(camLook, camLookTarget, ref camLookVelocity, camSpeed);

        Vector3 lookvector = camLook - camPosition;
        camRotation = Quaternion.LookRotation(lookvector);

        _helper.UpdateCameraPose(camPosition, camRotation);
        _helper.UpdateFov(_settings.fov);
    }
    public void FPCam()
    {
        /*
         * First-Person camera. 
         * It's going to be like the follow cam, but follows the head on a frame-by-frame basis. 
         */
        Transform head = _helper.playerHead;


        camPosition = Vector3.Lerp(camPosition, head.position, 5.0f * Time.deltaTime);
        camRotation = Quaternion.Lerp(camRotation, head.rotation, 5.0f * Time.deltaTime);

        _helper.UpdateCameraPose(camPosition, camRotation);
        _helper.UpdateFov(_settings.fov);
    }
}