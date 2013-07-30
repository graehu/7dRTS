using UnityEngine;
using System.Collections;

public class Projectile : PhysicsBody {
	
	#region public members
	
	public int playerID = 0;
	
	public float firePower = 5f; 	// the inital power of the shot.
	public float maxThrust = 1f; 	// the speed of the thruster with full fuel.
	public float minThrust = 0f; 	// the speed of the thruster once the fuel runs out.
	public float fuelDuration = 1f; // how long the fuel lasts in seconds.
	
	public LayerMask collisionMask = new LayerMask();
	public float collisionRadius = 1;
	
	public LayerMask damageMask = new LayerMask();
	public float damageRadius = 1;
	
	public GameObject[] spawnOnDestroy = new GameObject[0];
	
	#endregion
	
	
	#region private members
	
	float fuelTick = 0;
	
	#endregion

	// Use this for initialization
	void Start ()
	{
	
	}
	
	public override void Simulate (float _dt) 
	{
		//This force needs to scale.
		float actualThrust = 0;
		fuelTick += _dt;

		if(fuelTick < fuelDuration)
		{
			actualThrust = Mathf.Lerp(maxThrust, minThrust, fuelTick/fuelDuration);			
			ApplyForce(actualThrust*current.velocity.normalized, ForceMode.Force);			
		}

		base.Simulate(_dt);
		
		if(Physics.CheckSphere(transform.position, collisionRadius, collisionMask))
		{			
			Collider[] damagable = Physics.OverlapSphere(transform.position, damageRadius, damageMask);
			
			bool validCollision = damagable.Length == 0;
			
			for(int i = 0; i < damagable.Length; i++)
			{
				UnitTracker unit = damagable[i].GetComponent<UnitTracker>();
				if(unit != null && unit.playerID != playerID)
				{
					unit.OnDamage(25);
					validCollision = true;
				}
			}
			
			if(!validCollision)
				return;
			
			GameObject parent = transform.parent.gameObject;
			
			//find, stop, detach and pend particles for destruction
			ParticleSystem[] particleSystems = parent.GetComponentsInChildren<ParticleSystem>();
			foreach(ParticleSystem p in particleSystems)
			{
				p.Stop();
				p.transform.parent = null;
				Destroy(p.gameObject, p.startLifetime);
			}
			
			//spawn things
			foreach(GameObject gobj in spawnOnDestroy)
			{
				GameObject instance = Instantiate( gobj, transform.position, Quaternion.identity ) as GameObject;
				Destroy(instance, 3);
			}
			
			//destroy self
			Destroy(parent);
		}
		
	}
	
	void OnDrawGizmos()
	{
		Vector2 dir = current.velocity.normalized;
		Vector3 endpoint = new Vector3(transform.position.x+dir.x, transform.position.y+dir.y,transform.position.z);
		Gizmos.DrawLine(transform.position, endpoint);
		Gizmos.DrawWireSphere(transform.position, collisionRadius);
	}
}
