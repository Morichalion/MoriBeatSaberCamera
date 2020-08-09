using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MorichalionStuff;
using UnityEngine.Assertions.Must;

public class MoriBeatSaberCameraSettings : IPluginSettings
{
	
}
public partial class MoriBeatSaberCamera : IPluginCameraBehaviour
{
	MoriBeatSaberCameraSettings _settings = new MoriBeatSaberCameraSettings();

	public string ID => "MoriBeatSaberKam";
	public string name => "Mori's Beat Saber Cam";
	public string author => "Morichalion";
	public string version => "0.1.3";

	public IPluginSettings settings => _settings;

	public event EventHandler ApplySettings;
	private BeatSaberStatus BS;
	PluginCameraHelper _helper;


	//overlay stuff
	private bool overlayactive = true;
	private GameObject overlay;
	public float overlayScale = .5f;
	public float overlayOffsetX = -.5f;
	public float overlayOffsetY = -.3f;
	OverlayController overlayCon;

	public float FOV = 60.0f;
	//
	RigCam rig;
	menuCam men;


    public void	OnActivate(PluginCameraHelper helper) {
		_helper = helper;//got my cam stuff. 
		BS = new BeatSaberStatus();
		
		rig = new RigCam();

		//rig.worlCam = helper.manager.camera.worldCamera.transform;
		rig.worlCam = helper.behaviour.manager.camera.transform;

		men = new menuCam();

		//men.WorldCam = helper.manager.camera.worldCamera.transform;
		men.WorldCam = helper.behaviour.manager.camera.transform;

		CheckSettings();
		if (overlayactive==true)
		{
			try
			{
				//getting my overlay
				string assetPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
				string dp = System.IO.Path.Combine(assetPath, @"LIV\Plugins\CameraBehaviours\MoriBScam\overlay.yap");
				AssetBundle myasset = AssetBundle.LoadFromFile(dp);

				var prefab = myasset.LoadAsset<GameObject>("Overlay");
				myasset.Unload(false);
				//Transform c = _helper.manager.camera.worldCamera.transform;
				Transform c = _helper.behaviour.manager.camera.transform;
				overlay = UnityEngine.Object.Instantiate(prefab);
				overlay.transform.parent = c;
				overlay.transform.localPosition = new Vector3((overlayOffsetX+0.0f),(overlayOffsetY +.05f),(1.0f));
				float sc = overlayScale * 0.1f;
				overlay.transform.localScale = new Vector3(sc, sc, sc);
				overlay.transform.rotation = c.rotation;
				overlay.transform.Rotate(-90.0f, 0f, 0f);
				overlayCon = overlay.AddComponent<MorichalionStuff.OverlayController>();
				overlayCon._helper = helper;
				overlayCon.position = overlay.transform.localPosition;
				overlayCon.scale = overlay.transform.localScale;
				overlayCon.BS = BS;
				//overlay.SetActive(false);
				
			}
			catch
			{
				debug("couldn't instantiate overlay");
				overlayactive = false;
			}
		}
	}

	public void OnDeactivate()
	{
		if (overlayactive == true)
		{
			GameObject.Destroy(overlay);
			rig.KillThis();
			BS.shutDown();

		}
	}

	public void OnDestroy()
	{
		
	}

	public void OnFixedUpdate()
	{
		
	}

	public void OnLateUpdate()
	{
		
	}

	public void OnSettingsDeserialized()
	{
		
	}

	public void OnUpdate()
	{

		//dump the log from the bs socket. 
		if (BS.debug.Count > 0)
		{
			debug(BS.debug[0]);
			BS.debug.RemoveAt(0);
		}
		if(BS.menu == false)
		{
			camLook = rig.PositionUpdate(_helper);
			//Transform a = rig.UpdateCameraWithThis.transform;
			//_helper.UpdateCameraPose(a.position, a.rotation);
			//camLook = rig.CameraLook.transform.position;
			//camPos = rig.UpdateCameraWithThis.transform.position;
		}
		if(BS.menu == true)
		{
			men.camLookPos = camLook;
			camLook = men.menuCamUpdate(_helper);
			
		}
	}
	public Vector3 camLook = new Vector3();
	//public Vector3 camPos = new Vector3(0.0f, 0.0f, 0.0f);
	//public Vector3 camLook = new Vector3(0.0f, 0.0f, 0.0f);


	public string debugLogging = "false";
	public void debug(string str)
	{
		if (debugLogging == "true")
		{
			
			string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			string settingLoc = System.IO.Path.Combine(docPath, @"LIV\Plugins\CameraBehaviours\MoriBScam\");
			string target = System.IO.Path.Combine(settingLoc, "debug.txt");

			using (var sw = new System.IO.StreamWriter(target, true))
			{
				sw.WriteLine(str);
			}
		}
	}
}
