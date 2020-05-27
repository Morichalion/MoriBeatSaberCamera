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
using System.Collections.Generic;
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
    public float bahbah = 1.0f;
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
    public string version => "0.0.17";
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
    public Vector3 DynCamPosition = new Vector3(1.0f, 2.3f, -3f);
    public Vector3 DynCamLook = new Vector3(.4f, 0.3f, 3.0f);

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

        string[] a = str.Split('(', ')');
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
    public float overlayScale = 1.0f;
    public float overlayOffsetX = 0.0f;
    public float overlayOffsetY = 0.0f;
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
                    List<Vector3> riglookpoints = new List<Vector3>();//temp list to convert to an array for the RigLook array. 

                    while ((line = sr.ReadLine()) != null)
                    {
                        if(line.Contains("RigLook ="))
                        {
                            string[] blah = line.Split('=');
                            riglookpoints.Add(StringToVector3(blah[1]));
                        }
                        if (line.Contains("RigCamResponseTime ="))
                        {
                            string[] blah = line.Split('=');
                            RigCamObjectResponsiveness = float.Parse(blah[1]);
                        }
                        if (line.Contains("RigCamChangeMin ="))
                        {
                            string[] blah = line.Split('=');
                            RigCamChangeMin = float.Parse( blah[1]);
                        }
                        if (line.Contains("RigCamChangeMax ="))
                        {
                            string[] blah = line.Split('=');
                            RigCamChangeMax = float.Parse(blah[1]);
                        }
                        
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
                        if (line.Contains("Twitch Oath ="))
                        {
                            string[] blah = line.Split('=');
                            twOath = blah[1].Trim();
                        }
                        if (line.Contains("Twitch Name ="))
                        {
                            string[] blah = line.Split('=');
                            twName = blah[1].Trim();
                        }
                        if(line.Contains("Debug ="))
                        {
                            string[] blah = line.Split('=');
                            debugLogging = blah[1].Trim();
                        }
                        if(line.Contains("FieldOfView ="))
                        {
                            //debug("Field of view default is " + _settings.fov.ToString());
                            string[] blah = line.Split('=');
                            _settings.fov = float.Parse(blah[1].Trim());
                            //debug("Field of view has been updated to " + _settings.fov.ToString());
                        }
                        if (line.Contains("Overlay ="))
                        {
                            if (!line.Contains("no"))
                            {
                                overlayactive = true;
                                
                            }
                        }
                        if (line.Contains("OverlayScale ="))
                        {
                            string[] blah = line.Split('=');
                            try
                            {
                                overlayScale = float.Parse(blah[1]);
                            }
                            catch
                            {
                                debug("Scale didn't parse");
                            }
                        }
                        if (line.Contains("OverlayOffsetX ="))
                        {
                            string[] blah = line.Split('=');
                            try
                            {
                                overlayOffsetX = float.Parse(blah[1]);
                            }
                            catch
                            {
                                debug("Scale didn't parse");
                            }
                        }
                        if (line.Contains("OverlayOffsetY ="))
                        {
                            string[] blah = line.Split('=');
                            try
                            {
                                overlayOffsetY = float.Parse(blah[1]);
                            }
                            catch
                            {
                                debug("Scale didn't parse");
                            }
                        }
                    }
                    RigCamObjectLookPositions = riglookpoints.ToArray();
                }
            }
            catch (Exception e)
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
                ws.WriteLine("*");
                ws.WriteLine("*Field of view. Default is 60.");
                ws.WriteLine("FieldOfView = 60");
                ws.WriteLine("*");
                ws.WriteLine("*");
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
                ws.WriteLine("**RigCam: Keeps camera at different positions in relation to an object's position and rotation.");
                ws.WriteLine("*This setting is for the root of the camera rig. The position is in relation to a player's head on the floor. Default is one meter forward.");
                ws.WriteLine("RigCamObjectPosition = " + RigCamObjectPosition.ToString());
                ws.WriteLine("*These are the camera position and the camera target point in relation to the RigCamObjectPosition");
                ws.WriteLine("RigCamPosition = " + RigCamCameraPosition.ToString());
                ws.WriteLine("RigCamCamerFocusPosition = " + RigCamFocusPosition.ToString());
                ws.WriteLine("*The rotation of RigCamObjectPosition is created by looking at one of these points. Just add more lines in the same format if you want more points. ");
                //dump all values from an array.
                foreach(Vector3 vec in RigCamObjectLookPositions)
                {
                    ws.WriteLine("RigLook = " + vec.ToString());
                }
                ws.WriteLine("*Frequency of change in seconds, wait time is random between min and max.");
                ws.WriteLine("RigCamChangeMin = " + RigCamChangeMin.ToString());
                ws.WriteLine("RigCamChangeMax = " + RigCamChangeMax.ToString());
                ws.WriteLine("*Speed of camera change time. Make this someting less than RigCamChangeMin*");
                ws.WriteLine("RigCamResponseTime = " + RigCamObjectResponsiveness.ToString());

                ws.WriteLine("*");
                ws.WriteLine("*");
                ws.WriteLine("**Twitch Login Info, for viewer-controlled cameras**");
                ws.WriteLine("Twitch Oath = " + twOath);
                ws.WriteLine("Twitch Name = " + twName);
                ws.WriteLine("*");
                ws.WriteLine("*");
                ws.WriteLine("*Overlay. Want it on? yap or no *");
                ws.WriteLine("Overlay = no");
                ws.WriteLine("*");
                ws.WriteLine("*Scale and position stuff for the overlay");
                ws.WriteLine("OverlayScale = 1.0");
                ws.WriteLine("OverlayOffsetX = 0.0");
                ws.WriteLine("OverlayOffsetY = 0.0");
                ws.WriteLine("Debugging on for this plugin? true or false");
                ws.WriteLine("Debug = False");


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

    public bool Activated = false;
    GameObject tempTest;
    MorichalionStuff.OverlayController oLay;
    public void OnActivate(PluginCameraHelper helper) {
        debug("Activate started");
        
        //local camera helper reference
        _helper = helper;

        //find and load settings.
        debug("Doin' settings");
        CheckSettings();


        /* overlay stuff defined here. */
        
       if (overlayactive == true)
            {
            debug("trying to set up overlay");
            try
            {
                //Trying to instantiate an arbitrary prefab. 
                string assetPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string dp = Path.Combine(assetPath, @"LIV\Plugins\CameraBehaviours\MoriBScam\overlay.yap");
                //debug("gettting items from here: " + p);
                AssetBundle myasset = AssetBundle.LoadFromFile(dp);
            
                var prefab = myasset.LoadAsset<GameObject>("Overlay");
                myasset.Unload(false);
            
                overlay = UnityEngine.Object.Instantiate(prefab);
                //set layers for each object, assign each to variables in turn.
                foreach (Transform t in overlay.GetComponentInChildren<Transform>(true))
                {
                    t.gameObject.layer = overlayLayerCur;
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
                debug("Got to end of overlay gen. Camera selection next");
            } catch (Exception e) { debug("found it: " + e.Message); }


                //Find reference WorldCamera so UI stuff can be attached to it. 
                
             Transform blah = _helper.manager.camera.uiCamera.transform;
             //Found WorldCamera. Set overlay as child and resize/rotate it.
             overlay.transform.parent = blah.transform;
             overlay.transform.transform.rotation = blah.transform.rotation;
             debug("trying to set overlay offset X:" + overlayOffsetX + " Y:" + overlayOffsetY);
             overlay.transform.transform.localPosition = new Vector3((overlayOffsetX + 0f), overlayOffsetY + .05f, 1.0f);
             Vector3 p = new Vector3((overlayOffsetX + 0f), overlayOffsetY + .05f, 1.0f);
             overlay.transform.localScale = new Vector3(overlayScale* 0.1f, overlayScale * 0.1f, overlayScale * 0.1f);
             Vector3 s = new Vector3(overlayScale * 0.1f, overlayScale * 0.1f, overlayScale * 0.1f);
             overlay.transform.transform.Rotate(-90.0f, 0f, 0f);
             oLay = overlay.AddComponent<MorichalionStuff.OverlayController>();
             oLay.ini(_helper,p,s);
             overlay.SetActive(false);
                    
            }
        


            




        //headTarget, used for the smoothdamp target for the follow cam. 
        headTarget = new GameObject();
        headTarget.transform.rotation = _helper.playerHead.rotation;
        headTarget.transform.position = _helper.playerHead.position;

        //BS websocket
        //debug("Just starting the websocket");
            ws = new WebSocket("ws://localhost:6557/socket");
        wsStatus = "closed";
        ws.OnOpen += (sender, e) =>
            {
                debug("BS connection open");
                wsStatus = "open";
            };
        ws.OnMessage += (sender, e) =>
            {

            //Triggered when menu gets selected. Resets all overlay values to default. 
            if (e.Data.Contains("scene\":\"Menu"))
                {
                    //debug("Menu mode. Data: " + e.Data);
                    phase = 0;
                    gctype = "menu";
                    if(overlayactive == true) { 
                    
                    overlayLayerCur = 9;
                    score = "0  ";
                    scorePercent = "100%";
                    overlay.SetActive(false);
                    oLay.OnActivate();
                }
                //debug("Should be entering Menu Phase.");


            }

            //song phase (select which ones)
            if (e.Data.Contains("scene\":\"Song"))
                {
                    //debug("Song mode. Data: " +  e.Data);
                    phase = 1;

                    if (overlayactive == true)
                    {
                        overlay.SetActive(true);
                        oLay.OnActivate();
                        overlayLayerCur = 10;
                        score = "0  ";
                        scorePercent = "100%";
                        //debug("Should be entering Song Phase.");

                        //set up the cover texture
                        JObject menuInfo = JObject.Parse(e.Data);
                        coverTexData = menuInfo["status"]["beatmap"]["songCover"].ToString();
                        SongTitle = menuInfo["status"]["beatmap"]["songName"].ToString();
                    }
                //'standard' map types
                if (e.Data.Contains("mode\":\"SoloStandard") || e.Data.Contains("mode\":\"PartyStandard") || e.Data.Contains("mode\":\"SoloNoArrows") || e.Data.Contains("mode\":\"PartyNoArrows"))
                    {
                        gctype = "default";
                    }
                //'360 and 90 degree maps
                if (e.Data.Contains("mode\":\"Party360Degree") || e.Data.Contains("mode\":\"Party90Degree") || e.Data.Contains("mode\":\"Solo360Degree") || e.Data.Contains("mode\":\"Solo90Degree"))
                    {
                        gctype = "follow";
                    }

                }//ScoreChanged event. 
            if (e.Data.Contains("scoreChanged"))
                {
                    if (overlayactive == true)
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
                }
                if (overlayactive == true)
                {
                    if (e.Data.Contains("t\":\"noteCut"))
                    {
                        JObject slice = JObject.Parse(e.Data);
                        int combo = (int)slice["status"]["performance"]["combo"];
                    }
                    if (e.Data.Contains("t\":\"noteMissed"))
                    {
                        JObject hit = JObject.Parse(e.Data);
                        int curScore = (int)hit["status"]["performance"]["score"];
                        int maxScore = (int)hit["status"]["performance"]["currentMaxScore"];
                        float curPercent = (float)curScore / maxScore;
                        curPercent *= 100.0f;
                        scorePercent = curPercent.ToString().Substring(0, 4) + "%";

                    }
                }
            };
        ws.OnClose += (sender, e) =>
            {
            // debug("Beat Saber websocket closed because: " + e.Reason);
            // debug("Retry timer is at " + wsRetry.ToString());
            //debug("ws Status was " + wsStatus);
            
                    wsStatus = "closed";
                    debug("Beat Saber Websocket is closed");
                    wsRetry = 0f;
                
            };
        ws.OnError += (sender, e) =>
            {
                if (e.Message.Contains("OnMessage event"))
                {
                    //Nothing to see here. Doing nothing here. 
                    debug("Beatsaber websocket error: " + e.Message);
                    debug("Exception is: " + e.Exception);
                }
                else
                {
                    wsRetry = 0f;
                    wsStatus = "closing";

                    if (e.Message.Contains("occurred in closing the connection"))
                    {
                        wsStatus = "closed";
                    }
                    else
                    {
                        if (ws.IsAlive)
                        {
                            debug("Error wasn't about something closing. So I'm attempting to close it so it can restart");
                            ws.CloseAsync();
                        }
                    }
                }
            };

        
        debug("BS websocket good now");

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

                if (e.Data.StartsWith("PING"))
                {
                    tw.Send("PONG :tmi.twitch.tv");
                }
                else
                {
                    //Split into array.
                    char[] delim = { ' ' };
                    string[] str = e.Data.Split(delim, 5);
                    foreach (string i in str)
                    {
                        //debug(i);
                    }
                    if (str[2] == "PRIVMSG")
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
                                }else if (str[4].Contains("default"))
                                {
                                    gctype = "default";
                                    twitchMessage("GameCam to Default");
                                    debug("GameCam to Default Mode");
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

        //RigCam() items
        
            RigCamStart();
       
        
        debug("Activated right");
        Activated = true;

             

    }

    public float handsraised = 0.0f;
    private float buttondown = 0.0f;
    public void ManualSwitcher()
    {
        try
        {
            if (_helper.manager.player.leftHand.GetAnyButtonDown())
            {
                buttondown -= Time.deltaTime;
                if (buttondown < 0.0f)
                {
                    debug("Okay, button is DOOOOW");
                    buttondown = 2.0f;
                }
            }
        }
        catch(Exception e)
        {
            debug("Button broked");
        }
        Vector3 left = _helper.playerLeftHand.transform.position;
        Vector3 right = _helper.playerRightHand.transform.position;
        Vector3 head = _helper.playerHead.transform.position;
        float dist = Vector3.Distance(left, right);
        if (dist < 0.25f) {
            if (left.y > head.y)
            {
                handsraised += Time.deltaTime;
                if(handsraised > 2.5f)
                {
                    debug("Camera change should have triggered");
                    handsraised = 0.0f;
                    phase = 1;
                    if(gctype == "menu")
                    {
                        gctype = "follow";
                    }
                    if(gctype == "follow")
                    {
                        gctype = "default";
                    }
                    if(gctype == "default")
                    {
                        gctype = "menu";
                    }
                    NotifyUser();
                    
                }
                else
                {
                    handsraised = 0.0f;
                }
            }
        }
    }

    public void NotifyUser()
    {
        //adds a notification to the user.
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
        if(Activated == false) { goto OnUpdateEnd; }

        Transform headTransform = _helper.playerHead;
        //overlay stuff. Should only be messed with if it's active. 
        if (overlayactive == true)
        {
            overPercent.text = scorePercent;
            overScore.text = "Score: " + score;
            overRank.text = rank;
            overTitle.text = SongTitle;
            /*
            * set the overlay layer. OverlayLayerCur is defined in a websocket event.
            * updating the overlay needs to be done during OnUpdate();
            //don't think I need this anymore. 
            if (overlayLayerCur != overlayLayer)
            {
                foreach (Transform t in overlay.GetComponentInChildren<Transform>(true))
                {
                    t.gameObject.layer = overlayLayerCur;
                    
                }
                overlayLayer = overlayLayerCur;
            }
            */
            /*
             * Updating the icon. 
             * coverTexData gets updated in the websocket. 
             */
            if (coverTexData != "empty")
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

        if (wsStatus == "closed")
        {
            wsRetry += Time.deltaTime;
            if (wsRetry > 2.0f && !ws.IsAlive)
            {
                debug("Attempting to connect to BS");
                wsStatus = "open";
                wsRetry = -1.0f;
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

        //get lookvector from the user head to the camera
            if (phase == 0)
            {
            //Menu camera
            MenuCam();
            }
            else if (phase == 1)
            {
                //Game Camera
                if (gctype == "fp")
                {
                    FPCam();
                } else if (gctype == "menu")
                {
                    MenuCam();
                }
                else if (gctype == "default")
                {
                    RigCam();
                }
                else if (gctype == "follow")
                {
                    FollowCam();
                }
            }
        ManualSwitcher();
    OnUpdateEnd:
        ;
    }

    // OnLateUpdate is called after OnUpdate also everyframe and has a higher chance that transform updates are more recent.
    public void OnLateUpdate() {
    }

    // OnDeactivate is called when the user changes the profile to other camera behaviour or when the application is about to close.
    // The camera behaviour should clean everything it created when the behaviour is deactivated.
    public void OnDeactivate() {
        if (Activated == false) {
            debug("Did not fire OnActivate(). Skipping shut down routines.");
            goto OnDeactivateEnd; 
        }
        // Saving settings here
        debug("Shuttin' down...");
        wsStatus = "closing";
        try
        {
            ws.Close();
        }
        catch (Exception e)
        {
            debug("Okay, trying to close this is causing oddities.");
            debug("The oddities are: " + e.Message);
        }
        if (twOath != "none")
        {
            tw.Close();
        }
        if (overlayactive == true)
        {
            UnityEngine.Object.Destroy(overlay);
            debug("Destroying the overlay");
            //overlayactive = false;
        }
        ApplySettings?.Invoke(this, EventArgs.Empty);
        UnityEngine.Object.Destroy(RigCamObject);
        UnityEngine.Object.Destroy(FloorReference);
        //These items will be children of RigCamObject
        UnityEngine.Object.Destroy(RigCamCamera);
        UnityEngine.Object.Destroy(RigCamFocus);
        UnityEngine.Object.Destroy(headTarget);
        debug("Successfully deactivated MoriBScam");

    OnDeactivateEnd:
        ;
        Activated = false;
}

    // OnDestroy is called when the users selects a camera behaviour which is not a plugin or when the application is about to close.
    // This is the last chance to clean after your self.
    public void OnDestroy() {
        debug("OnDestroy() was fired.");

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

    /// <summary>
    /// Different camera methods. The once all the per-frame stuff is updated above, one of these gets run. 
    /// 
    /// </summary>
    public void MenuCam()
    {
        //camera for the menu. Just a static camera.
        camPositionTarget = MenuCamPosition;
        camLookTarget = MenuCamLook;

        camPosition = Vector3.Lerp(camPosition, camPositionTarget, 2.0f * Time.deltaTime);
        camLook = Vector3.Lerp(camLook, camLookTarget, 2.0f * Time.deltaTime);

        Vector3 lookvector = camLook - camPosition;
        camRotation = Quaternion.LookRotation(lookvector);

        _helper.UpdateCameraPose(camPosition, camRotation);
        _helper.UpdateFov(_settings.fov);
        

    }


    //DefaultGameCam variables. Positions/viewtargets. All that fun stuff. 
    private Vector3[] DefaultGameCamPositions =
    {
        //far-off cameras
        new Vector3(1.5f, 2.0f, -3.0f),
        new Vector3(-1.5f, 2.0f, -3.0f),
        //over-the-shoulder cameras
        new Vector3(0.4f,2f,-.6f),
        new Vector3(-0.4f,2f,-.6f),
        //facing-the-player
        new Vector3 (0.0f,3.0f,3.0f),
        new Vector3 (2.0f,2.0f,3.0f),
        new Vector3 (-2.0f,2.0f, 3.0f)

    };
    private Vector3[] DefaultGameCamTargets =
    {
        //far-off cameras
        new Vector3(0.0f,1.0f,3.0f),
        new Vector3(0.0f,1.0f,3.0f),
        //over-the-shoulder targets
        new Vector3(-.25f,.7f,4.0f),
        new Vector3(.25f,.7f,4.0f),
        //facing-the-player
        new Vector3(0.0f,1.0f,0.0f),
        new Vector3(0.0f,1.0f,0.0f),
        new Vector3(0.0f,1.0f,0.0f)
    };
    private int DefaultGameCamPose = 0;    
    private float DefaultGameCamTimer = 0.0f;
    private float DefaultGameCamSpeed = 4.0f;
    public void DefaultGameCam()
    {
        
        //default game camera. This just pans the camera view from pose-to-pose. 
        DefaultGameCamTimer -= Time.deltaTime;
        if (DefaultGameCamTimer<0.0f)
        {
            
            //Set timer to random value 
            DefaultGameCamTimer += UnityEngine.Random.Range(10.0f, 15.0f);
            //Select another cameraposition.
            DefaultGameCamPose = UnityEngine.Random.Range(0,DefaultGameCamPositions.Length);
        }


        camPositionTarget = DefaultGameCamPositions[DefaultGameCamPose];
        camLookTarget = DefaultGameCamTargets[DefaultGameCamPose];

        camPosition = Vector3.SmoothDamp(camPosition, camPositionTarget, ref camPositionVelocity, DefaultGameCamSpeed);
        camLook = Vector3.SmoothDamp(camLook, camLookTarget, ref camLookVelocity, DefaultGameCamSpeed);

        Vector3 lookvector = camLook - camPosition;
        camRotation = Quaternion.LookRotation(lookvector);


        _helper.UpdateCameraPose(camPosition, camRotation);
        _helper.UpdateFov(_settings.fov);

    }
    /*
     * RigCam() will set the parent the camera to an empty. 
     * The empty position/rotation will be will be influenced by a point in relation to the playerhead. A unit or so ahead maybe.
     */

    public GameObject RigCamObject;
    public GameObject FloorReference;
    //These items will be children of RigCamObject
    public GameObject RigCamCamera;
    public GameObject RigCamFocus;
   

    public Vector3 RigCamObjectPosition = new Vector3(0.0f, 0.0f, 1.0f);// Position RigCamObject moves towards (For now, relative to head position on floor);
    public Vector3 RigCamObjectVelocity = new Vector3();
    public float RigCamObjectResponsiveness = 5f; //Speed value for smoothDamp. Should 

    public Vector3 RigCamObjectLook = new Vector3(0.0f, 0.0f, 0.0f); //current LookRotation for RigCamObject. 
    public Vector3[] RigCamObjectLookPositions =
    {
        new Vector3(2.0f,0.0f, 4.0f),
        new Vector3(-2.0f,0.0f,4.0f),
        new Vector3(2.0f,0.0f,-4.0f),
        new Vector3(-2.0f,0.0f,-4.0f)
    };
    public Vector3 RigCamObjectLookTarget = new Vector3(0.0f, 0.0f, 4.0f);//Look target for the rig Y must always be zero. 
    public Vector3 RigCamObjectLootTargetVelocity = new Vector3();//velocty for the smoothdamp on this. 
    

    public Vector3 RigCamCameraPosition = new Vector3(0.0f,3.0f,-4.0f);//Current position (only Y and Z change)
    public Vector3[] RigCamCameraPositions =
    {
        new Vector3 (0.0f,2f,-4f),
        new Vector3 (0.0f,2f,-4f),
        new Vector3 (0.0f,2f,-4f),
        new Vector3 (0.0f,2f,-4f)

    };//Select from these positions
    public Vector3 RigCamCameraVelocity = new Vector3();//velocity. 
    
    public Vector3 RigCamFocusPosition = new Vector3(0.0f,0.0f,0.0f);
    public Vector3[] RigCamFocusPositions =
    {
         new Vector3(0.0f,1.0f,0.0f),
         new Vector3(0.0f,1.0f,0.0f),
         new Vector3(0.0f,1.0f,0.0f),
         new Vector3(0.0f,1.0f,0.0f)
    };
    public Vector3 RigCamFocusVelocity = new Vector3();

    public int RigCamCamPose = 0;

    public float RigCamTimer = 0f;

    public float RigCamChangeMin = 10f;
    public float RigCamChangeMax = 15f;
    
   

    //
    public void RigCamStart()
    {
       RigCamObject = new GameObject();
        FloorReference = new GameObject();
        //These items will be children of RigCamObject
        RigCamCamera = new GameObject();
        RigCamFocus = new GameObject();


    //parent focus and camera to object.
    RigCamCamera.transform.parent = RigCamObject.transform;
        RigCamFocus.transform.parent = RigCamObject.transform;

        RigCamCamera.transform.localPosition = RigCamCameraPosition;
        RigCamFocus.transform.localPosition = RigCamFocusPosition;
        RigCamObject.transform.position = new Vector3(0.0f, 0.0f, 0.0f);

        debug("RigCam stuff should be set up");
    }
    public void RigCam()
    {
        //camera timer to change targets
        RigCamTimer -= Time.deltaTime;
        if (RigCamTimer < 0.0f)
        {

            //Set timer to random value 
            RigCamTimer += UnityEngine.Random.Range(RigCamChangeMin, RigCamChangeMax);
            //Select another cameraposition.
            RigCamCamPose = UnityEngine.Random.Range(0, RigCamObjectLookPositions.Length);
        }
        //simple lerp localpositions of camera and focus to desired positions
        /*
         * TODO
         * 
         * 1. set another vector3 and have the RigCamObject do a Lookrotation at it. 
         * 2. Smoothdamp that object around to different locations. 
         * 
         * the idea is that since the RigCamObject is stationary, everything will rotate around the same way. Hopefully. 
         */
        //
        var head = _helper.playerHead;

        //get a line that's straight ahead from the playerhead. 
        Vector3 headposition = new Vector3(head.position.x, 0.0f, head.position.z);//playerhead floor position. 
        Vector3 ahead = head.TransformPoint(RigCamObjectPosition);//point ahead of the head
        ahead = new Vector3(ahead.x, 0.0f, ahead.z);

        FloorReference.transform.position = headposition;
        FloorReference.transform.rotation = Quaternion.LookRotation(ahead - headposition);//floor found. Moves will be done in relation to this object. 

        RigCamObject.transform.position = Vector3.SmoothDamp(RigCamObject.transform.position, FloorReference.transform.TransformPoint(RigCamObjectPosition), ref RigCamObjectVelocity, RigCamObjectResponsiveness);

        Vector3 rootlookat = FloorReference.transform.TransformPoint(RigCamObjectLookPositions[RigCamCamPose]);

        RigCamObjectLook = Vector3.SmoothDamp(RigCamObjectLook, rootlookat, ref RigCamObjectLootTargetVelocity, RigCamObjectResponsiveness);

        RigCamObject.transform.rotation = Quaternion.LookRotation(RigCamObjectLook - RigCamObject.transform.position);

        //apply result to camera
        Vector3 looky = RigCamFocus.transform.position - RigCamCamera.transform.position;
        Quaternion rot = Quaternion.LookRotation(looky);


        _helper.UpdateCameraPose(RigCamCamera.transform.position, rot);
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
