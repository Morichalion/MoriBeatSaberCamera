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


// User defined settings which will be serialized and deserialized with Newtonsoft Json.Net.
// Only public variables will be serialized.
public class ReebooterSettings : IPluginSettings {
    public float fov = 5f;
    public float distance = 2f;
    public float speed = 1f;
    public float bahbah = 1.0f;
    public string damnsit = "I dunnot a damnthing if this iz werkin' or not. ><";
}

// The class must implement IPluginCameraBehaviour to be recognized by LIV as a plugin.
public class Reebooter : IPluginCameraBehaviour {

    // Store your settings localy so you can access them.
    ReebooterSettings _settings = new ReebooterSettings();

    // Provide your own settings to store user defined settings .   
    public IPluginSettings settings => _settings;


    // Invoke ApplySettings event when you need to save your settings.
    // Do not invoke event every frame if possible.
    public event EventHandler ApplySettings;


    // ID is used for the camera behaviour identification when the behaviour is selected by the user.
    // It has to be unique so there are no plugin collisions.
    public string ID => "Reebooter";
    // Readable plugin name "Keep it short".
    public string name => "Plugin REEEEBOOOT";
    // Author name.
    public string author => "Morichalion";
    // Plugin version.
    public string version => "0.0.1";
    // Localy store the camera helper provided by LIV.
    PluginCameraHelper _helper;


    // Constructor is called when plugin loads
    public Reebooter() { }



    // OnActivate function is called when your camera behaviour was selected by the user.
    // The pluginCameraHelper is provided to you to help you with Player/Camera related operations.

    public void OnActivate(PluginCameraHelper helper) {
        //local camera helper reference
        _helper = helper;
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
    }

    // OnLateUpdate is called after OnUpdate also everyframe and has a higher chance that transform updates are more recent.
    public void OnLateUpdate() {
    }

    // OnDeactivate is called when the user changes the profile to other camera behaviour or when the application is about to close.
    // The camera behaviour should clean everything it created when the behaviour is deactivated.
    public void OnDeactivate() {
        // Saving settings here

        ApplySettings?.Invoke(this, EventArgs.Empty);
    }

    // OnDestroy is called when the users selects a camera behaviour which is not a plugin or when the application is about to close.
    // This is the last chance to clean after your self.
    public void OnDestroy() {

    }
    public void debug(string str)
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