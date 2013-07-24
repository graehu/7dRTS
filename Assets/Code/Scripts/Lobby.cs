using UnityEngine;
using System.Collections;

public class Lobby : MonoBehaviour 
{

	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	void OnGUI()
	{
		Vector2 center = new Vector2(Screen.width*0.5f, Screen.height*0.5f);
		
		GUI.BeginGroup(new Rect(center.x, center.y, 640, 640));
		
		GUILayout.BeginVertical("Box");
		
		GUILayout.Label(Network.peerType.ToString());
		
		GUILayout.EndVertical();
		
		GUI.EndGroup();
	}
}
