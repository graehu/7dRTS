using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
	#region static variables
	
	public const int TURN_BUFFER_SIZE = 2;
	
	public static GameManager Instance { get { return instance; } }
	private static GameManager instance = null;
	
	public static int CurrentTurn { get { return instance.currentTurn; } }
	
	private static Dictionary<int,List<UnitTracker>> teams = new Dictionary<int, List<UnitTracker>>();
	
	#endregion
	
	#region static methods
	
	public static void AddUnit(int _teamID, UnitTracker _unit)
	{
		if(!teams.ContainsKey(_teamID))
			teams.Add(_teamID, new List<UnitTracker>());
		teams[_teamID].Add(_unit);
	}
	
	public static void RemoveUnit(int _teamID, UnitTracker _unit)
	{
		if(teams.ContainsKey(_teamID))
			teams[_teamID].Remove(_unit);
	}
		
	public static List<UnitTracker> GetTeam(int _teamID)
	{
		if(teams.ContainsKey(_teamID))
			return new List<UnitTracker>( teams[_teamID] );
		else
			return new List<UnitTracker>();
	}
	
	/// <summary>
	/// Gets all units. (slight overhead from grouping teams)
	/// </summary>
	/// <returns>
	/// The all units.
	/// </returns>
	public static List<UnitTracker> GetAllUnits()
	{
		List<UnitTracker> allUnits = new List<UnitTracker>();
		foreach(List<UnitTracker> team in teams.Values)
		{
			allUnits.AddRange(team);
		}
		return allUnits;
	}
	
	#endregion
	
	
	#region public variables
	
	public string gameType = "7dRTS_TooEasyGames";
	public string gameName = "GameName";
	
	public PlayerControl playerContolPrefab = null;
	
	public bool localGame = false;
	public int maxConnections = 1;
	
	#endregion
	
	#region protected variables
	
	protected bool isRunning = false;
	protected PlayerControl localPlayerControl = null;
	
	public int currentTurn = 0;
	public float lastTurnTimestamp = 0;
	public float turnTick = 0;
	
	#endregion
	
	#region public methods
	
	#endregion
	
	#region monobehaviour methods
	
	void Awake()
	{
		if(instance != null)
			Destroy(instance.gameObject);
		instance = this;
		
		NetworkManager.Initialise();
		NetworkManager.RequestHostList(gameType);
	}
	
	void LateUpdate()
	{
		float time = localGame ? Time.time : (float)Network.time;
		float turnLength = 1f / (Network.sendRate);
		
		if(isRunning)
		{		
			Time.timeScale = 1;
			
			if(turnTick < turnLength)
			{
				turnTick += time - lastTurnTimestamp;
			}
			else
			{
				//TODO: validate number of player controls matches number of players
				PlayerControl[] players = (PlayerControl[]) FindSceneObjectsOfType(typeof(PlayerControl));
					
				localPlayerControl.TryCaptureTurn(currentTurn+TURN_BUFFER_SIZE);
				
				foreach(PlayerControl p in players)
				{		
					if(!p.IsUpToDate)
					{
						//pause simulation
						Time.timeScale = 0;
						Debug.Log("Pause");
						return;
					}
				}
				//if(!GameManager.instance.localGame)
				//{
					foreach(PlayerControl p in players)
					{
						p.ProcessTurn(currentTurn);
					}
				//}
				//else players[0].ProcessTurn(currentTurn);
				
				turnTick = turnTick - turnLength;
				
				currentTurn++; //increment turn
			}
		}
		else //not running
		{
			Time.timeScale = 0;
			turnTick = 0;
		}
		
		lastTurnTimestamp = time;
	}
	
	void OnGUI()
	{				
		GUILayout.BeginVertical("box");
		
		GUILayout.Label(NetworkManager.State.ToString());
		
		switch(NetworkManager.State)
		{
		case NetworkState.Disconnected:
			
			if(isRunning)
			{
				if(localGame)
					GUILayout.Label("Playing Offline");
				else
					GUILayout.Label("Connection Lost");
				
				if(GUILayout.Button("Reset"))
				{
					Application.LoadLevel(Application.loadedLevel);
				}
			}
			else
			{
				GUILayout.BeginHorizontal();
				
					if(GUILayout.Button("Create Game"))
						NetworkManager.StartServer(gameName, gameType);
					
					gameName = GUILayout.TextField(gameName, GUILayout.MaxWidth(100));
				
				GUILayout.EndHorizontal();
				
				GUILayout.BeginHorizontal();
			
					GUILayout.Label("Game Type:");
					gameType = GUILayout.TextField(gameType, GUILayout.MaxWidth(100));
				
				GUILayout.EndHorizontal();
				
				if(GUILayout.Button("Update Hosts"))
				{
					NetworkManager.RequestHostList(gameType);
				}
				
				GUILayout.Label("-hosts-");
				
				foreach(HostData host in NetworkManager.AvailableHosts)
				{
					if(GUILayout.Button(string.Format("{0} ({1}/{2})", host.gameName, host.connectedPlayers, host.playerLimit)))
					{
						NetworkManager.ConnectToServer(host);
					}
				}
					
				GUILayout.Label("-------");
				
				if(GUILayout.Button("Play Offline"))
				{
					BeginLocalGame();
				}
			}
			
			break;
		case NetworkState.ServerInitialising:
			
			if(GUILayout.Button("Stop"))
				NetworkManager.Disconnect();
			
			break;
		case NetworkState.ServerHosted:
			
			if(GUILayout.Button("Stop"))
				NetworkManager.Disconnect();
			
			GUILayout.Label(string.Format("({0}/{1}) Players", Network.connections.Length+1, Network.maxConnections+1));
			
			break;
			
		case NetworkState.ClientSearching:
			
			if(NetworkManager.AvailableHosts.Length > 0)
			{
				//try to connect to the first host
				NetworkManager.ConnectToServer(NetworkManager.AvailableHosts[0]);
			}
			else
				NetworkManager.RequestHostList(gameType);
			
			if(GUILayout.Button("Stop"))
				NetworkManager.Disconnect();
			
			break;
		case NetworkState.ClientConnecting:
			
			if(GUILayout.Button("Stop"))
				NetworkManager.Disconnect();
			
			break;
		case NetworkState.ClientPlaying:
		case NetworkState.ServerPlaying:
			
			GUILayout.Label("Game: " + gameName);
			
			if(GUILayout.Button("End Game"))
				NetworkManager.Disconnect();
			
			GUILayout.Label("PlayerID: " + Network.player);
			
			foreach(NetworkPlayer player in Network.connections)
			{
				GUILayout.Label(string.Format("Player{0} {1}ms", player, Network.GetAveragePing(player)));
			}
			
			break;
		}
		
		GUILayout.EndVertical();	
		
	}
	
	#endregion
	
	#region network callbacks
	
	void OnConnectedToServer()
	{
		
	}
	
	void OnPlayerConnected(NetworkPlayer _player)
	{
		if(Network.connections.Length == maxConnections)
		{
			networkView.RPC("BeginGame", RPCMode.All);
			
			MasterServer.UnregisterHost();
		}
	}
	
	[RPC]
	void BeginGame(NetworkMessageInfo _info)
	{
		//TODO: this should be done after the network level loading
		GameObject gobj = Network.Instantiate(playerContolPrefab.gameObject, Vector3.zero, Quaternion.identity, 0) as GameObject;
		localPlayerControl = gobj.GetComponent<PlayerControl>();
		
		//spawn units
		UnitSpawnPoint[] spawnPoints = (UnitSpawnPoint[]) FindSceneObjectsOfType(typeof(UnitSpawnPoint));
		foreach(UnitSpawnPoint sp in spawnPoints)
		{
			if(sp.playerID == localPlayerControl.Index)
			{
				sp.SpawnNetworked();
			}
		}
		
		lastTurnTimestamp = (float)_info.timestamp;
		isRunning = true;
		localGame = false;
	}
	
	void BeginLocalGame()
	{
		//TODO: this should be done after the network level loading
		GameObject gobj = Instantiate(playerContolPrefab.gameObject) as GameObject;
		localPlayerControl = gobj.GetComponent<PlayerControl>();
		
		//spawn units
		UnitSpawnPoint[] spawnPoints = (UnitSpawnPoint[]) FindSceneObjectsOfType(typeof(UnitSpawnPoint));
		foreach(UnitSpawnPoint sp in spawnPoints)
		{
			if(sp.playerID == 0)
			{
				sp.SpawnLocal();
			}
		}
		
		lastTurnTimestamp = (float)Time.time;
		isRunning = true;
		localGame = true;
	}
	
	#endregion
}
