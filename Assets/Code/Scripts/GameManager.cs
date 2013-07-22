using UnityEngine;
using System.Collections.Generic;

public static class GameManager
{
	#region static variables
	
	public static int clientTeam = 0;
		
		
	private static Dictionary<int,List<UnitTracker>> teams = new Dictionary<int, List<UnitTracker>>();
	
	#endregion
	
	#region public methods
	
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
	
	#region ctor
	
	#endregion
}
