using UnityEngine;
using System.Collections;

public class UnitSpawnPoint : MonoBehaviour 
{

	#region public variables
	
	public GameObject characterPrefab = null;
	
	public int playerID = 0;
	
	#endregion
	
	#region public methods
	
	public void SpawnNetworked()
	{
		GameObject obj = Network.Instantiate(characterPrefab, transform.position, transform.rotation, 0) as GameObject;
		UnitTracker unit = obj.GetComponentInChildren<UnitTracker>();
		unit.playerID = playerID;
	}
	
	public void SpawnLocal()
	{
		GameObject obj = Instantiate(characterPrefab, transform.position, transform.rotation) as GameObject;
		UnitTracker unit = obj.GetComponentInChildren<UnitTracker>();
		unit.playerID = playerID;
	}
	
	#endregion
}
