using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UnitTracker : MonoBehaviour {
	
	public Vector3 UnitPos { get { return AI == null ? transform.position : AI.transform.position; } }
	
	public GameObject aimingReticle = null;
	public GameObject healthBar = null;
	public GameObject jetPack = null;
	public GameObject selectionGraphic = null;
	public GameObject troopGraphic = null;
	
	public AIPathXY AI = null;
	
	public GameObject graphics = null;
	
	public int health = 100;
	
	public int playerID = 0;
	
	public int CurrentWeapon
	{
		set {
				currentWeapon = (value > 0 ? value : 0);
				currentWeapon = (currentWeapon>weapons.Count ? weapons.Count : currentWeapon);
			}
		get { return currentWeapon; }
	}

	protected int currentWeapon = 0;
	
	public List<Weapon> weapons;
	protected bool isTracking = false;
	protected float dir = 1;
	
	public AudioClip[] hurtSounds = new AudioClip[0];
	
	#region monobehaviour methods
	
	// Use this for initialization
	void Start()
	{
		dir = troopGraphic.transform.localScale.x;
		StartTracking();
	}
	
	void OnDestroy()
	{
		StopTracking();
	}
	
	void Update()
	{
		if(health <= 0)
		{
			StopTracking();
			if(Network.peerType != NetworkPeerType.Disconnected)
			{
				if(networkView.isMine)
				{
					Network.Destroy(transform.parent.gameObject);
				}
			}
			else
				Destroy(transform.parent.gameObject);
		}
		else
		{
			if(healthBar != null)
				healthBar.renderer.material.SetFloat("_Cutoff", Mathf.InverseLerp(100, 0, health));
		}
		
		if(!AI.TargetReached)
			dir = AI.TargetDirection.x; 
		
		Weapon weapon = weapons[currentWeapon];
		
		if(weapon != null && weapon.IsFiring)
			dir = weapon.Power.x;
			
		if (dir > 0) 
			troopGraphic.transform.localScale = new Vector3(1,1,1);
		else
			troopGraphic.transform.localScale = new Vector3(-1,1,1);
	}
	
	public void OnDamage(int amount)
	{
		health -= amount;
		if(hurtSounds.Length > 0)
		{
			GameManager.Instance.PlaySoundFx2D( hurtSounds[Random.Range(0,hurtSounds.Length)], 0.5f );
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
		
		StartTracking();
	}
	
	#endregion
}
