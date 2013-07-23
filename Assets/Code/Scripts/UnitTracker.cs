using UnityEngine;
using System.Collections;

public class UnitTracker : MonoBehaviour {
	
	public int unitID = 0;
	public int teamID = 0;	
	
	#region monobehaviour methods
	
	// Use this for initialization
	void Awake()
	{
		GameManager.AddUnit(teamID, this);
	}
	
	void OnDestroy()
	{
		GameManager.RemoveUnit(teamID, this);
	}	
	
	// Update is called once per frame
	void Update () {
	
	}
	
	#endregion
}
