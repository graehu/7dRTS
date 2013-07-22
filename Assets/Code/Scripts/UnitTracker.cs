using UnityEngine;
using System.Collections;

public class UnitTracker : MonoBehaviour {
	
	public int teamID = 0;
	
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
}
