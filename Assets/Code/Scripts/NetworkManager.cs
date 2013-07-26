using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum NetworkState
{
	NetworkUninitialised,
	NetworkInitialising,
	ServerInitialising,
	ServerHosted,
	ServerPlaying,
	ClientSearching,
	ClientConnecting,
	ClientPlaying,
	Disconnected
}

public class NetworkManager : MonoBehaviour {
	
	#region public types
	
	#endregion
	
	#region public variables
	
	public static NetworkState State { get { return networkState; } }
	public static HostData [] AvailableHosts { get { return availableHosts; } }
	
	#endregion
	
	#region private variables
	
	static NetworkManager instance = null;
	
	static int listenPort = 25000;
	static HostData server = null;
	static string serverName = "TestServer";
	static string serverGameType = "TestGameType";
	static HostData [] availableHosts = new HostData[0];
	
	static NetworkState networkState = NetworkState.NetworkUninitialised;

	#endregion
	
	#region public methods
	
	public static void Initialise()
	{
		if(instance == null)
		{
			networkState = NetworkState.NetworkInitialising;
			GameObject gobj = new GameObject("_NetworkManager", typeof(NetworkManager));
			instance = gobj.GetComponent<NetworkManager>();
		}
	}
	
	public static void Disconnect()
	{
		if(networkState != NetworkState.Disconnected) //avoid recursion
		{
			networkState = NetworkState.Disconnected;
			Network.Disconnect(200);
			MasterServer.UnregisterHost();
		}
	}
	
	public static void StartServer(string _gameName, string _gameType)
	{		
		serverName = _gameName;
		serverGameType = _gameType;
		
		Network.InitializeServer(1, listenPort, !Network.HavePublicAddress());
		
		networkState = NetworkState.ServerInitialising;
	}
	
	public static void ConnectToServer(HostData _server)
	{
		server = _server;
		
		Network.Connect(server);
			
		MasterServer.ClearHostList();
		
		networkState = NetworkState.ClientConnecting;
		
		Debug.Log("Connecting To Server: " + server.gameName);
	}
	
	public static void RequestHostList(string _gameType)
	{
		MasterServer.ClearHostList();
		MasterServer.RequestHostList(_gameType);
		
		Debug.Log ("Requesting host list for " + _gameType);
	}
	
	#endregion
	
	#region monobehaviour methods
	
	void Start()
 	{
		Application.runInBackground = true;
		
		DontDestroyOnLoad(gameObject);
		
		networkState = NetworkState.Disconnected;
	 }
	
	void OnDestroy()
	{
		Disconnect();
	}
	
	#endregion
	
	#region server callbacks
	
	void OnServerInitialized()
	{
		Debug.Log("Server Initialised");
		
		Debug.Log("Registering Game: " + serverName);
		
		MasterServer.RegisterHost(serverGameType, serverName);
	}
	
	void OnPlayerConnected(NetworkPlayer _player)
	{
		Debug.Log("Client Connected: " + _player.guid);
		networkState = NetworkState.ServerPlaying;
	}
	
	void OnPlayerDisconnected(NetworkPlayer _player)
	{
		Debug.Log(string.Format("Player {0} left, cleaning up...", _player));
		Network.RemoveRPCs(_player);
		//Network.DestroyPlayerObjects(_player);
		
		Disconnect();
	}
	
	#endregion
	
	#region client callback
	
	void OnConnectedToServer()
	{
		Debug.Log("Connected To Server");
		networkState = NetworkState.ClientPlaying;
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
