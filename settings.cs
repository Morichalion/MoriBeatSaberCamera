using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
public partial class MoriBeatSaberCamera : IPluginCameraBehaviour
{
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

    string ResourceLoc;
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
                        if (line.Contains("RigLook ="))
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
                            RigCamChangeMin = float.Parse(blah[1]);
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
                        if (line.Contains("Debug ="))
                        {
                            string[] blah = line.Split('=');
                            debugLogging = blah[1].Trim();
                        }
                        if (line.Contains("FieldOfView ="))
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
                            else { overlayactive = false; }
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
                foreach (Vector3 vec in RigCamObjectLookPositions)
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
}
