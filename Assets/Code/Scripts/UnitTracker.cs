using UnityEngine;
using System.Collections;

public class UnitTracker : MonoBehaviour {
	
	public int unitID = 0;
	public int teamID = 0;
	
	public AIPathXY AI = null;
	
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
	void LateUpdate () 
	{
		if(AI != null)
		{
			transform.rotation = AI.transform.rotation;
			float easing = 0.2f;
			transform.position = (AI.transform.position*(1f-0.2f)) + (transform.position*(0.2f)) ;
			//transform.position = AI.transform.position;
		}
	}
	
	#endregion
}
