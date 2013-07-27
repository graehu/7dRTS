using UnityEngine;
using System.Collections;

public class UnitTracker : MonoBehaviour {
	
	public Vector3 UnitPos { get { return AI == null ? transform.position : AI.transform.position; } }
	
	public GameObject aimingReticle = null;
	
	public AIPathXY AI = null;
	
	public GameObject graphics = null;
	
	public int health = 100;
	
	public int playerID = 0;
	protected bool isTracking = false;
	
	#region monobehaviour methods
	
	// Use this for initialization
	void Start()
	{
		StartTracking();
	}
	
	void OnDestroy()
	{
		StopTracking();
	}
	
	void Update()
	{
		if(health == 0)
		{
			StopTracking();
			if(Network.peerType != NetworkPeerType.Disconnected)
			{
				if(networkView.isMine)
				{
					Network.RemoveRPCs(networkView.viewID);
					Network.RemoveRPCs(transform.parent.networkView.viewID);
					Network.Destroy(transform.parent.networkView.viewID);
				}
			}
			else
				Destroy(transform.parent.gameObject);
		}
	}
	
	#endregion
	
	#region private methods
	
	void OnDamage()
	{
		health = 0;
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
		
		StartTracking();
	}
	
	#endregion
}
