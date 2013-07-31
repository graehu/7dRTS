using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
	#region static variables
	
	public const int TURN_BUFFER_SIZE = 2;
	public const float UNIT_SPACING = 2f;
	
	public static GameManager Instance { get { return instance; } }
	private static GameManager instance = null;
	
	public static int CurrentTurn { get { return instance.currentTurn; } }
	public static float TurnLength { get { return 1f / (Network.sendRate); } }
	
	private static List<UnitTracker> units = new List<UnitTracker>();
	
	public static PlayerControl LocalPlayer { get { return Instance.localPlayerControl; } }
	
	#endregion
	
	#region static methods
	
	public static void AddUnit(int _teamID, UnitTracker _unit)
	{
		if(!units.Contains(_unit))
			units.Add(_unit);
	}
	
	public static void RemoveUnit(int _teamID, UnitTracker _unit)
	{
		units.Remove(_unit);
	}
		
	public static List<UnitTracker> GetTeam(int _teamID)
	{
		return units.FindAll(u => u.playerID == _teamID);
	}
	
	/// <summary>
	/// Gets all units. (slight overhead from grouping teams)
	/// </summary>
	/// <returns>
	/// The all units.
	/// </returns>
	public static List<UnitTracker> GetAllUnits()
	{
		return new List<UnitTracker>(units);
	}
	
	#endregion
	
	
	#region public variables
	
	public static string gameType = "7dRTS_TooEasyGames";
	public static string gameName = "GameName";
	public static string IP = "127.0.0.1";
	
	public static float musicVolume = 1;
	public static float fxVolume = 1;
	
	public PlayerControl playerContolPrefab = null;
	
	public bool localGame = false;
	public int maxConnections = 1;
	
	public Transform[] playerCamPositions = new Transform[0];
	
	public GameObject winPopupPrefab = null;
	public GameObject loosePopupPrefab = null;
	
	public AudioSource musicAudioSource = null;
	public AudioSource fxAudioSource = null;
	
	public Texture2D controlsGUI = null;
	
	public bool isGameFinished = false;
	
	#endregion
	
	#region protected variables
	
	protected bool isRunning = false;
	protected PlayerControl localPlayerControl = null;
	
	public int currentTurn = 0;
	public float turnTick = 0;
	
	#endregion
	
	#region public methods
	
	public void PlaySoundFx(AudioClip _clip, float _volume, Vector3 _soundPos)
	{
		Vector2 camPos = Camera.mainCamera.transform.position;
		Vector2 distance = (Vector2)_soundPos - camPos;
		float maxDistance = 75;
		
		float volume = _volume * Mathf.Clamp01(1 - (distance.magnitude/maxDistance));
		
		if(_clip != null)
			fxAudioSource.PlayOneShot(_clip, volume*fxVolume);
	}
	
	public void PlaySoundFx2D(AudioClip _clip, float _volume)
	{		
		if(_clip != null)
			fxAudioSource.PlayOneShot(_clip, _volume*fxVolume);
	}

	#endregion
	
	#region private methods
	
	void UpdateWorld(float deltaTime)
	{
		List<UnitTracker> allunits = GetAllUnits();
		for(int i = 0; i < allunits.Count; i++)
		{
			UnitTracker unit = allunits[i];
			unit.AI.StepAlongPath(deltaTime);
			unit.weapons[unit.CurrentWeapon].UpdateWeapon(deltaTime);
		}
		
		for(int i = 0; i < PhysicsBody.bodies.Count; i++)
		{
			PhysicsBody.bodies[i].Simulate(deltaTime);
		}
		
		//test for win condition
		if(!isGameFinished)
		{
			for(int i = 0; i < maxConnections + 1; i++)
			{
				if(GetTeam(i).Count == 0)
				{
					if(i == LocalPlayer.Index)
					{
						//lose
						Instantiate(loosePopupPrefab);
					}
					else
					{
						//win
						Instantiate(winPopupPrefab);
					}
					
					isGameFinished = true;
					
					break;
				}
			}
		}
	}
	
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
	
	void Update()
	{				
		if(isRunning)
		{		
			Time.timeScale = isGameFinished ? 0 : 1;
			
			turnTick += Time.deltaTime;
			
			if(turnTick >= TurnLength)
			{			
				localPlayerControl.TryCaptureTurn(GameManager.CurrentTurn+GameManager.TURN_BUFFER_SIZE);
				
				//TODO: cache this somewhere and validate number of player controls matches number of players
				PlayerControl[] players = (PlayerControl[]) FindSceneObjectsOfType(typeof(PlayerControl));
				
				//wait for all players to be ready
				if(players.Length == maxConnections + 1)
				{
					bool isUpToDate = true;
					foreach(PlayerControl p in players)
					{		
						if(!p.IsUpToDate)
						{						
							isUpToDate = false;
							
							break;
						}
					}
					
					if(isUpToDate)
					{									
						foreach(PlayerControl p in players)
						{
							p.ProcessTurn(currentTurn);
						}
						
						UpdateWorld(TurnLength);
						
						turnTick = turnTick - TurnLength;
						
						currentTurn++; //increment turn
					}
				}
			}
		}
		else
		{
			turnTick = 0;
		}
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
				GUI.DrawTexture(Rect.MinMaxRect(Screen.width/2f - controlsGUI.width/4f, Screen.height - controlsGUI.height/2f,
												Screen.width/2f + controlsGUI.width/4f, Screen.height), controlsGUI);
				
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
				
				GUILayout.BeginHorizontal();
					
					if(GUILayout.Button("Connect IP"))
					{
						NetworkManager.ConnectToServer(IP);
					}
					
					IP = GUILayout.TextField(IP, GUILayout.MaxWidth(100));
				
				GUILayout.EndHorizontal();
				
				if(GUILayout.Button("Play Offline"))
				{
					BeginLocalGame();
				}
				
				GUILayout.BeginHorizontal();
				
					GUILayout.Label("Music Volume");
				
					musicVolume = GUILayout.HorizontalSlider(musicVolume, 0, 1, GUILayout.MaxWidth(100));
					musicAudioSource.volume = musicVolume;
				
				GUILayout.EndHorizontal();
				
				GUILayout.BeginHorizontal();
				
					GUILayout.Label("FX Volume");
				
					fxVolume = GUILayout.HorizontalSlider(fxVolume, 0, 1, GUILayout.MaxWidth(100));
				
				GUILayout.EndHorizontal();
				
				/*
				GUILayout.BeginHorizontal();
				
					bool fullscreen = GUILayout.Toggle(Screen.fullScreen, "FullScreen");
					if(Screen.fullScreen != fullscreen)
					{
						Resolution highRes = Screen.resolutions[Screen.resolutions.Length-1];
						Screen.SetResolution(highRes.width, highRes.height, fullscreen);
					}
				
				GUILayout.EndHorizontal();
				*/
				
				if(GUILayout.Button("Quit"))
				{
					Application.Quit();
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
			
			GUILayout.Label("Turn#: " + CurrentTurn);
			
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
			sp.SpawnLocal();
		}
		
		isRunning = true;
		localGame = true;
	}
	
	#endregion
}
