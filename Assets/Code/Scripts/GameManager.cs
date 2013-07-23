using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
	#region static variables
	
	public const int TURN_BUFFER_SIZE = 1;
	
	private static GameManager instance = null;
	
	public static int CurrentTurn { get { return instance.currentTurn; } }
	
	
	public static int clientTeam = 0;
		
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
	
	
	
	#endregion
	
	#region protected variables
	
	public int currentTurn = 0;
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
	}
	
	void LateUpdate()
	{
		turnTick += Time.deltaTime;
		float turnLength = 1f / Network.sendRate;
		if(turnTick > turnLength )
		{
			//TODO: validate number of player controls matches number of players
			PlayerControl[] players = (PlayerControl[]) FindSceneObjectsOfType(typeof(PlayerControl));
			
			Time.timeScale = 1;
			
			foreach(PlayerControl p in players)
			{						
				if(!p.IsUpToDate)
				{
					//pause simulation
					Time.timeScale = 0;
					return;
				}
				
				p.TryCaptureTurn(currentTurn+TURN_BUFFER_SIZE);
			}
			
			foreach(PlayerControl p in players)
			{
				p.ProcessTurn(currentTurn);
			}
			
			turnTick = turnTick - turnLength;
			
			currentTurn++; //increment turn
		}
	}
	
	#endregion
	
	#region ctor
	
	#endregion
}
