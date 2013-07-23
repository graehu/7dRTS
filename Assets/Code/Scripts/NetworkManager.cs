using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour {
	
	#region variables
	
	NetworkView connection = new NetworkView();
	bool hostListRecieved = false;	
	
	#endregion
	
	#region monobehaviour methods
	
	// Use this for initialization
	void Start()
	{
		//Requesting host list
		Debug.Log ("Requesting host list for 2EZ7dRTS");
		MasterServer.ClearHostList();
		MasterServer.RequestHostList("2EZ7dRTS");
	}
	
	void OnDestroy()
	{
		MasterServer.UnregisterHost();
		Network.Disconnect();
	}
	
	// Update is called once per frame
	void Update ()
	{
		if(hostListRecieved && Network.peerType == NetworkPeerType.Disconnected)
		{
			HostData [] hosts = MasterServer.PollHostList();
			
			if(hosts.Length > 0)
			{
				//try to connect to the first host
				Network.Connect(hosts[0].guid);
				
				MasterServer.ClearHostList();
			}
			else
			{
				Debug.Log("No Hosts Found. Hosting...");
				//become a host
				Network.InitializeServer(1, 25000);
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
	
	void OnGUI()
	{
		GUI.TextArea( new Rect(0, 0, 2000, 2000) , "" + Network.peerType.ToString());	
	}
	
	#endregion
	
	#region server callbacks
	
	void OnServerInitialized()
	{
		Debug.Log("Server Initialised");
		
		string gameName = "2EasyRts"+((int)Random.value).ToString();
		MasterServer.RegisterHost("2EZ7dRTS", gameName);
	}
	
	void OnPlayerConnected(NetworkPlayer _player)
	{
		Debug.Log("Client Connected: " + _player.guid);
		
		//wait for game to be full and unregister as host
		MasterServer.UnregisterHost();
		
		//Network.CloseConnection(); //use this to disconnect subsequent players
	}
	
	#endregion
	
	#region client callback
	
	void OnConnectedToServer()
	{
		Debug.Log("Connected To Server");
		//move to next scene.
	}
	
	void OnFailedToConnect(NetworkConnectionError error)
	{
		//TODO: do something with error
		Debug.Log("Connection Error: " + error.ToString());
	}
	
	#endregion
	
	#region master server callbacks
	
	void OnMasterServerEvent(MasterServerEvent _event)
	{
		Debug.Log("MasterServerEvent: " + _event.ToString());
		
		switch(_event)
		{
		case MasterServerEvent.HostListReceived:
			hostListRecieved = true;
			break;
		case MasterServerEvent.RegistrationSucceeded:
			Debug.Log("Waiting For Players...");
			break;
		}
		
	}
	
	#endregion
}
