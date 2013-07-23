using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour {
	
	#region variables
	
	const string GAME_TYPE = "2EZ7dRTS";
	
	bool hostListRecieved = false;	
	bool connectingToServer = false;
	
	#endregion
	
	#region monobehaviour methods
	
	// Use this for initialization
	void Start()
	{
		//Requesting host list
		Debug.Log ("Requesting host list for " + GAME_TYPE);
		MasterServer.ClearHostList();
		MasterServer.RequestHostList("GAME_TYPE");
	}
	
	void OnDestroy()
	{
		MasterServer.UnregisterHost();
		Network.Disconnect();
	}
	
	// Update is called once per frame
	void Update ()
	{
		if(Network.peerType == NetworkPeerType.Disconnected)
		{
			if(hostListRecieved && !connectingToServer)
			{
				HostData [] hosts = MasterServer.PollHostList();
				
				if( hosts.Length > 0)
				{
					Debug.Log("Connecting To Server: " + hosts[0].gameName);
					
					//try to connect to the first host
					Network.Connect(hosts[0].guid);
					
					connectingToServer = true;
					
					MasterServer.ClearHostList();
				}
				else
				{
					Debug.Log("No Hosts Found. Hosting...");
					//become a host
					Network.InitializeServer(1, 25000);
				}
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
		
		string gameName = "Aaron's Game";
		
		Debug.Log("Registering Game: " + gameName);
		
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
		
		connectingToServer = false;
	}
	
	void OnDisconnectedFromServer(NetworkDisconnection info)
	{
		Debug.Log(info.ToString());
	}
	
	void OnFailedToConnect(NetworkConnectionError error)
	{
		//TODO: do something with error
		Debug.Log("Connection Error: " + error.ToString());
		
		connectingToServer = false;
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
