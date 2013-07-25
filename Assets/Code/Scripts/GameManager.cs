using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
	#region static variables
	
	public const int TURN_BUFFER_SIZE = 2;
	
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
		
		if(localGame)
			BeginLocalGame();
	}
	
	void LateUpdate()
	{
		float time = localGame ? Time.time : (float)Network.time;
		float turnLength = 1f / (Network.sendRate);
		
		if(isRunning)
		{			
			if(turnTick < turnLength)
			{
				turnTick += time - lastTurnTimestamp;
			}
			else
			{
				//TODO: validate number of player controls matches number of players
				PlayerControl[] players = (PlayerControl[]) FindSceneObjectsOfType(typeof(PlayerControl));
				
				Time.timeScale = 1;
					
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
				
				foreach(PlayerControl p in players)
				{
					p.ProcessTurn(currentTurn);
				}
				
				turnTick = turnTick - turnLength;
				
				currentTurn++; //increment turn
			}
		}
		lastTurnTimestamp = time;
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
		
		lastTurnTimestamp = (float)_info.timestamp;
		isRunning = true;
	}
	
	void BeginLocalGame()
	{
		//TODO: this should be done after the network level loading
		GameObject gobj = Instantiate(playerContolPrefab.gameObject) as GameObject;
		localPlayerControl = gobj.GetComponent<PlayerControl>();
		
		lastTurnTimestamp = (float)Time.time;
		isRunning = true;
	}
	
	#endregion
}
