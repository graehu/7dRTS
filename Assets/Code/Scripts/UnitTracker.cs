using UnityEngine;
using System.Collections;

public class UnitTracker : MonoBehaviour {
	
	public GameObject aimingReticle = null;
	
	public AIPathXY AI = null;
	
	public int health = 100;
	
	protected int playerID = 0;
	protected bool isTracking = false;
	
	#region monobehaviour methods
	
	// Use this for initialization
	void Start()
	{
		playerID = int.Parse ( networkView.owner.ToString () );
		StartTracking();
	}
	
	void OnDestroyed()
	{
		StopTracking();
	}
	
	// Update is called once per frame
	void LateUpdate () 
	{
		if(AI != null)
		{
			transform.rotation = AI.transform.rotation;
			transform.position = AI.transform.position;
		}
	}
	
	#endregion
	
	#region tracking methods
	
	void StartTracking()
	{
		if(!isTracking)
		{
			isTracking = true;
			GameManager.AddUnit(playerID, this);
		}
	}
	
	void StopTracking()
	{
		isTracking = false;
		GameManager.RemoveUnit(playerID, this);
	}
	
	#endregion
	
	#region networking callbacks
	
	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{
		int ID = playerID;
		int hp = health;
		
		stream.Serialize(ref ID);
		stream.Serialize(ref hp);
		
		if(stream.isReading)
		{
			playerID = ID;
			health = hp;
		}
	}
	
	#endregion
}
