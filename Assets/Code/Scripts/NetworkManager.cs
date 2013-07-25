using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour {
	
	public enum NetworkState
	{
		Initialising,
		Lobby,
		ServerInitialising,
		ServerHosted,
		ServerPlaying,
		ClientSearching,
		ClientConnecting,
		ClientPlaying,
		Disconnected
	}
	
	#region public variables
	
	public int listenPort = 25000;
	
	#endregion
	
	#region private variables
	
	string gameType = "7dRTS_TooEasyGames";
	string gameName = "GameName";
	HostData server = null;
	HostData [] availableHosts = new HostData[0];
	
	NetworkState networkState = NetworkState.Initialising;

	#endregion
	
	#region public methods
	
	public void ReturnToLobby()
	{
		networkState = NetworkState.Lobby;
	}
	
	public void Disconnect()
	{
		if(networkState != NetworkState.Disconnected) //avoid recursion
		{
			networkState = NetworkState.Disconnected;
			Network.Disconnect();
			MasterServer.UnregisterHost();
		}
	}
	
	public void StartServer(string gameName)
	{		
		Network.InitializeServer(1, listenPort, !Network.HavePublicAddress());
		
		networkState = NetworkState.ServerInitialising;
	}
	
	public void ConnectToServer(HostData _server)
	{
		server = _server;
		
		Network.Connect(server);
			
		MasterServer.ClearHostList();
		
		networkState = NetworkState.ClientConnecting;
		
		Debug.Log("Connecting To Server: " + server.gameName);
	}
	
	public void RequestHostList()
	{
		MasterServer.ClearHostList();
		MasterServer.RequestHostList(gameType);
		
		Debug.Log ("Requesting host list for " + gameType);
	}
	
	#endregion
	
	#region monobehaviour methods
	
	void Start()
 	{
		Application.runInBackground = true;
		
		RequestHostList();
		
		//Requesting host list
		Debug.Log ("Requesting host list for " + gameType);
	 }
	
	void OnDestroy()
	{
		Disconnect();
	}
	
	void OnGUI()
	{				
		GUILayout.BeginVertical("box");
		
		GUILayout.Label(networkState.ToString());
		
		switch(networkState)
		{
		case NetworkState.Initialising:
			networkState = NetworkState.Lobby;
			break;
		case NetworkState.Lobby:
			
			GUILayout.BeginHorizontal();
			
				if(GUILayout.Button("Create Game"))
					StartServer(gameName);
				
				gameName = GUILayout.TextField(gameName, GUILayout.MaxWidth(100));
			
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
		
				GUILayout.Label("Game Type:");
				gameType = GUILayout.TextField(gameType, GUILayout.MaxWidth(100));
			
			GUILayout.EndHorizontal();
			
			if(GUILayout.Button("Update Hosts"))
			{
				RequestHostList();
			}
			
			GUILayout.Label("-hosts-");
			
			foreach(HostData host in availableHosts)
			{
				if(GUILayout.Button(string.Format("{0} ({1}/{2})", host.gameName, host.connectedPlayers, host.playerLimit)))
				{
					ConnectToServer(host);
				}
			}
				
			GUILayout.Label("-------");
			
			break;
		case NetworkState.ServerInitialising:
			
			if(GUILayout.Button("Stop"))
				Disconnect();
			
			break;
		case NetworkState.ServerHosted:
			
			if(GUILayout.Button("Stop"))
				Disconnect();
			
			GUILayout.Label(string.Format("({0}/{1}) Players", Network.connections.Length+1, Network.maxConnections+1));
			
			break;
			
		case NetworkState.ClientSearching:
			
			if(availableHosts.Length > 0)
			{
				//try to connect to the first host
				ConnectToServer(availableHosts[0]);
			}
			else
				RequestHostList();
			
			if(GUILayout.Button("Stop"))
				Disconnect();
			
			break;
		case NetworkState.ClientConnecting:
			
			if(GUILayout.Button("Stop"))
				Disconnect();
			
			break;
		case NetworkState.ClientPlaying:
		case NetworkState.ServerPlaying:
			
			GUILayout.Label("Game: " + gameName);
			
			if(GUILayout.Button("End Game"))
				Disconnect();
			
			GUILayout.Label("PlayerID: " + Network.player.ToString());
			
			break;
		case NetworkState.Disconnected:
			
			if(GUILayout.Button("Return To Lobby"))
				Application.LoadLevel(Application.loadedLevel);
			
			break;
		}
		
		GUILayout.EndVertical();	
		
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
		networkState = NetworkState.Playing;
	}
	
	void OnPlayerDisconnected(NetworkPlayer _player)
	{
		Debug.Log(string.Format("Player {0} left, cleaning up...", _player));
		Network.RemoveRPCs(_player);
		Network.DestroyPlayerObjects(_player);
		
		Disconnect();
	}
	
	#endregion
	
	#region client callback
	
	void OnConnectedToServer()
	{
		Debug.Log("Connected To Server");
		networkState = NetworkState.Playing;
	}
	
	void OnDisconnectedFromServer(NetworkDisconnection info)
	{
		Debug.Log(info.ToString());
		Disconnect();
	}
	
	void OnFailedToConnect(NetworkConnectionError error)
	{
		//TODO: do something with error
		Debug.Log(error.ToString());
		Disconnect();
	}
	
	#endregion
	
	#region master server callbacks
	
	void OnMasterServerEvent(MasterServerEvent _event)
	{
		Debug.Log("MasterServerEvent: " + _event.ToString());
		
		switch(_event)
		{
		case MasterServerEvent.HostListReceived:
			availableHosts = MasterServer.PollHostList();
			MasterServer.ClearHostList();
			break;
		case MasterServerEvent.RegistrationSucceeded:
			networkState = NetworkState.ServerHosted;
			break;
		}
		
	}
	
	#endregion
}
