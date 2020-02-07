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

using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;
using WebSocketSharp;

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
    public string version => "0.0.1";
    // Localy store the camera helper provided by LIV.
    PluginCameraHelper _helper;
    float _elaspedTime;

    //websocket declaration
    WebSocket ws;
    public string wsStatus = "closed";
    public float wsRetry = 0.0f;

    //camera phase
    public int phase = 0;

    //transform 
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

    //Menu Targets
    public Vector3 MenuCamPosition = new Vector3(2.0f, 1.8f, -3f);
    public Vector3 MenuCamLook = new Vector3(0f, 1.2f, 0f);

    //Dynamic Cam Targets(to be defined in relation to headTarget
    public Vector3 DynCamPosition = new Vector3(1.0f,2.3f,-3f);
    public Vector3 DynCamLook = new Vector3(.4f,-0.1f,3.0f);

    // Constructor is called when plugin loads
    public MoriBScam() { }

    // OnActivate function is called when your camera behaviour was selected by the user.
    // The pluginCameraHelper is provided to you to help you with Player/Camera related operations.
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
    public void CheckSettings()
    {
        string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string settingLoc = Path.Combine(docPath, @"LIV\MoriBScam\");
        //create the directory in the off-chance that it doesn't exist.
        Directory.CreateDirectory(settingLoc);

        debug("Just checkin' somthin'");

        debug(@settingLoc);
        settingLoc = Path.Combine(settingLoc, "ini.txt");
        //Check to see if the file exists
        if (File.Exists(@settingLoc))
        {
            //reads settings from the file.
            try
            {
                using (StreamReader sr = new StreamReader(@settingLoc))
                {
                    string line;

                    while ((line = sr.ReadLine()) != null)
                    {
                        debug("Working with the following item:");
                        debug(line);
                        debug("");
                        if (line.Contains("MenuCamPosition"))
                        {
                            string[] blah = line.Split('=');
                            MenuCamPosition = StringToVector3(blah[1]);
                            debug("");
                        }
                        if (line.Contains("MenuCamLook"))
                        {
                            string[] blah = line.Split('=');
                            MenuCamLook = StringToVector3(blah[1]);
                            debug("");
                        }
                        if (line.Contains("DynCamPosition"))
                        {
                            string[] blah = line.Split('=');
                            DynCamPosition = StringToVector3(blah[1]);
                            debug("");
                        }
                        if (line.Contains("DynCamLook"))
                        {
                            string[] blah = line.Split('=');
                            DynCamLook = StringToVector3(blah[1]);
                            debug("");
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
                ws.WriteLine("*MoriBScam settings.");
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
                //Parse.TryParseFloat("0.0");
            }
        }
    }
    
    public void OnActivate(PluginCameraHelper helper) {
        CheckSettings();



        _helper = helper;

        //headTarget;
        headTarget = new GameObject();
        headTarget.transform.rotation = _helper.playerHead.rotation;
        headTarget.transform.position = _helper.playerHead.position;

        ws = new WebSocket("ws://localhost:6557/socket");
        ws.OnOpen += (sender, e) => {

        };
        ws.OnMessage += (sender, e) =>
        {
            //this should fire when it recieves something. 
            //writeError(e.Data);
            if (e.Data.Contains("scene\":\"Menu"))
            {
                //if menu phase.
                phase = 0;
                debug(e.Data);
                debug("Should be entering Menu Camera Phase.");
            }

            //song phase (select which ones)
            if (e.Data.Contains("scene\":\"Song"))
            {
                //'standard' map types
                if(
                    e.Data.Contains("mode\":\"SoloStandard")
                    ||
                    e.Data.Contains("mode\":\"PartyStandard")
                    ||
                    e.Data.Contains("mode\":\"SoloNoArrows") || e.Data.Contains("mode\":\"PartyNoArrows")
                )
                {
                    phase = 1;
                    debug(e.Data);
                    debug("Standard Map camera: phase = 1");
                }
                //'360 and 90 degree maps
                if(
                    e.Data.Contains("mode\":\"Party360Degree")|| e.Data.Contains("mode\":\"Party90Degree")
                    ||
                    e.Data.Contains("mode\":\"Solo360Degree") || e.Data.Contains("mode\":\"Solo90Degree")
                )
                {
                    phase = 2;
                    debug(e.Data);
                    debug("Standard Map camera: phase = 2");
                }
            }
        };
        ws.OnClose += (sender, e) =>
        {
            debug("Websocket CLosed because: " + e.Reason);
            wsStatus = "closed";
            
        };
        ws.OnError += (sender, e) => {
            //debug("Websocket error: " + e.Message);
            if(e.Message.Contains("An exception has occurred while connecting"))
            {
                ws.CloseAsync();
            }
        };
        ws.OnOpen += (sender, e) =>
        {
            wsStatus = "open";
            debug("Connection made");

        };

        ws.ConnectAsync();

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
        //is the websocket connected?
        if(wsStatus == "closed")
        {
            wsRetry += Time.deltaTime;
            if(wsRetry > 5.0f)
            {
                wsRetry = 0.0f;
                ws.ConnectAsync();
            }
        }

        _elaspedTime += Time.deltaTime * _settings.speed;
        Transform headTransform = _helper.playerHead;

        //check for angle tolerance (only affects rotation camera
        if (Quaternion.Angle(headTransform.rotation, headTarget.transform.rotation) > 10.0f)
        {
            headTarget.transform.rotation = headTransform.rotation;
        }
        if (phase == 0)
        {
            //Menu camera
            MenuCam();
        }
        if (phase == 1)
        {
            //normal rotation levels
            StandardCam();
        }
        if (phase == 2)
        {
            //360 and 90 degree levels
            FollowCam();
        }
        
        //smoothdamp towards the targets
        camPosition = Vector3.SmoothDamp(camPosition, camPositionTarget, ref camPositionVelocity, camSpeed);
        camLook = Vector3.SmoothDamp(camLook, camLookTarget, ref camLookVelocity, camSpeed);
        Vector3 lookvector = camLook - camPosition;

        Quaternion rotat = Quaternion.LookRotation(lookvector);

        _helper.UpdateCameraPose(camPosition, rotat);
        _helper.UpdateFov(_settings.fov);
    }

    // OnLateUpdate is called after OnUpdate also everyframe and has a higher chance that transform updates are more recent.
    public void OnLateUpdate() {

    }

    // OnDeactivate is called when the user changes the profile to other camera behaviour or when the application is about to close.
    // The camera behaviour should clean everything it created when the behaviour is deactivated.
    public void OnDeactivate() {
        // Saving settings here
        ApplySettings?.Invoke(this, EventArgs.Empty);
        ws.CloseAsync();
    }

    // OnDestroy is called when the users selects a camera behaviour which is not a plugin or when the application is about to close.
    // This is the last chance to clean after your self.
    public void OnDestroy() {

    }
    public void debug(string str)
    {
        /*
        using (var sw = new StreamWriter(@"C:\Path\To\Your\errorMessages.txt", true))
        {
            sw.WriteLine(str);
        }
        */
        
    }
    public void MenuCam()
    {
        //camera for the menu. Just a static camera.
        camPositionTarget = MenuCamPosition;
        camLook = MenuCamLook;

    }
    public void StandardCam()
    {
        //Normal Game Camera. Static Camera. For now, Menu Cam. 
        camPositionTarget = MenuCamPosition;
        camLook = MenuCamLook;
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
        camPositionTarget = headTarget.transform.TransformPoint(DynCamPosition);
        camLookTarget = headTarget.transform.TransformPoint(DynCamLook);
    }
}
