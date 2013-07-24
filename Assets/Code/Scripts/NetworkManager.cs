using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour {
	
	public enum NetworkState
	{
		Initialising,
		Disconnected,
		ServerInitialising,
		ServerHosted,
		ServerRunning,
		ClientConnecting,
		ClientRunning
	}
	
	#region private variables
	
	string gameType = "7dRTS_TooEasyGames";
	string gameName = "GameName";
	HostData server = null;
	HostData [] availableHosts = new HostData[0];
	
	bool hostListRecieved = false;
	NetworkState networkState = NetworkState.Disconnected;

	#endregion
	
	#region public methods
	
	public void StartServer(string gameName)
	{
		Network.InitializeServer(1, 25000, true);
		
		networkState = NetworkState.ServerInitialising;
	}
	
	public void FindServer()
	{
		availableHosts = MasterServer.PollHostList();
				
		if( availableHosts.Length > 0)
		{
			server = availableHosts[0];
			
			//try to connect to the first host
			Network.Connect(server.guid);
			
			MasterServer.ClearHostList();
			
			networkState = NetworkState.ClientConnecting;
			
			Debug.Log("Connecting To Server: " + server.gameName);
		}
	}
	
	#endregion
	
	#region monobehaviour methods
	
	void Start()
 	{
		//Requesting host list
		Debug.Log ("Requesting host list for " + gameType);
		MasterServer.ClearHostList();
		MasterServer.RequestHostList(gameType);
	 }
	
	void OnDestroy()
	{
		MasterServer.UnregisterHost();
		Network.Disconnect();
	}
	
	void OnGUI()
	{
		bool reset = false;
		
		GUILayout.BeginVertical("Box");
		
		GUILayout.Label(networkState.ToString());
		
		switch(networkState)
		{
		case NetworkState.Initialising:
			if(hostListRecieved)
				networkState = NetworkState.Disconnected;
			break;
		case NetworkState.Disconnected:
			
			GUILayout.BeginHorizontal();
			
				if(GUILayout.Button("Create Game"))
					StartServer(gameName);
				
				gameName = GUILayout.TextField(gameName, GUILayout.MaxWidth(100));
			
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
		
				GUILayout.Label("Game Type:");
				gameType = GUILayout.TextField(gameType, GUILayout.MaxWidth(100));
			
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			
				if(GUILayout.Button("Join Game"))
					FindServer();
				
				GUILayout.Label("Hosts: " + availableHosts.Length);
			
			GUILayout.EndHorizontal();
			
			break;
		case NetworkState.ServerInitialising:
			break;
		case NetworkState.ServerHosted:
			if(GUILayout.Button("Stop"))
				reset = true;
			break;
		case NetworkState.ServerRunning:
			GUILayout.Label("Game: " + gameName);
			if(GUILayout.Button("End Game"))
				reset = true;
			break;
		case NetworkState.ClientConnecting:
			if(GUILayout.Button("Stop"))
				reset = true;
			break;
		case NetworkState.ClientRunning:
			GUILayout.Label("Game: " + server.gameName);
			if(GUILayout.Button("End Game"))
				reset = true;
			break;
		}
		
		GUILayout.Label("Connections: " + Network.connections.Length);
		
		GUILayout.EndVertical();	
		
		if(reset)
		{
			Application.LoadLevel(Application.loadedLevel);
			//does disconections on destroy, this should be done here if this object is perminant
		}
		
	}
	
	#endregion
	
	#region server callbacks
	
	void OnServerInitialized()
	{
		Debug.Log("Server Initialised");
		
		Debug.Log("Registering Game: " + gameName);
		
		MasterServer.RegisterHost(gameType, gameName);
	}
	
	void OnPlayerConnected(NetworkPlayer _player)
	{
		Debug.Log("Client Connected: " + _player.guid);
	}
	
	void OnPlayerDisconnected(NetworkPlayer _player)
	{
		Debug.Log(string.Format("Player {0} left, cleaning up...", _player));
		Network.RemoveRPCs(_player);
		Network.DestroyPlayerObjects(_player);
	}
	
	#endregion
	
	#region client callback
	
	void OnConnectedToServer()
	{
		Debug.Log("Connected To Server");
		networkState = NetworkState.ClientRunning;
	}
	
	void OnDisconnectedFromServer(NetworkDisconnection info)
	{
		Debug.Log(info.ToString());
	}
	
	void OnFailedToConnect(NetworkConnectionError error)
	{
		//TODO: do something with error
		Debug.Log("Connection Error: " + error.ToString());
		
		networkState = NetworkState.Disconnected;
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
			networkState = NetworkState.ServerHosted;
			break;
		}
		
	}
	
	#endregion
}
