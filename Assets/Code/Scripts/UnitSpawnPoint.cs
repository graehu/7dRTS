using UnityEngine;
using System.Collections;

public class UnitSpawnPoint : MonoBehaviour 
{

	#region public variables
	
	public GameObject characterPrefab = null;
	
	public int teamID = 0;
	
	#endregion
	
	#region public methods
	
	public void SpawnNetworked()
	{
		GameObject obj = Network.Instantiate(characterPrefab, transform.position, transform.rotation, teamID) as GameObject;
		UnitTracker unit = obj.GetComponentInChildren<UnitTracker>();
	}
	
	public void SpawnLocal()
	{
		GameObject obj = Instantiate(characterPrefab, transform.position, transform.rotation) as GameObject;
		UnitTracker unit = obj.GetComponentInChildren<UnitTracker>();
	}
	
	#endregion
}
