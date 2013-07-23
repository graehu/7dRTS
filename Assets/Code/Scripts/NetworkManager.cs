using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour {
	
	NetworkView connection = new NetworkView();
	bool hosting = false;
	bool connected = true;

	// Use this for initialization
	void Start()
	{
		//Requesting host list
		Debug.Log ("Requesting network hostlist for 2EZ7dRTS");
		MasterServer.RequestHostList("2EZ7dRTS");	
	}
	
	// Update is called once per frame
	void Update ()
	{
		if(connected == false && Network.peerType == NetworkPeerType.Disconnected)
		{
			HostData [] hosts = null;
			
			if(!hosting)
				hosts = MasterServer.PollHostList();
			
			//if the list isn't empty try to connect to the dude at the top!
			if(!hosting && hosts.Length > 0)
			{
				//Now connecting hopefully
				//TODO: do something with error
				Debug.Log ("Attempting to connect to " + hosts[0].ip);
				NetworkConnectionError error = Network.Connect(hosts[0].guid);
				
			}
			else if (!hosting)//if it is empty, host.
			{
				Debug.Log("Becoming a host");
				Network.InitializeServer(1, 25000);
				MasterServer.RegisterHost("2EZ7dRTS", "2EasyRts");
				
				hosting = true;
			}
		}
		
		//these two probably aren't needed.
		if(Network.peerType == NetworkPeerType.Client)
		{
			//Do clienty stuff	
		}
		else if(Network.peerType == NetworkPeerType.Server)
		{
			//Do servery stuff
		}
	}
	
	void OnFailedToConnect(NetworkConnectionError error)
	{
		//TODO: do something with error
		Debug.Log("Failed to connect");
	}
	
	void OnConnectedToServer()
	{
		connected = true;
		Debug.Log("Connected Yay!!!");
		//move to next scene.
	}
	
	void OnPlayerConnected()
	{
		connected = true;
		Debug.Log("Connected Yay!!!");
		//move to next scene
	}
}
