using UnityEngine;
using System.Collections;

public class Weapon : MonoBehaviour {
	
	#region public members
	
	public GameObject muzzleParticle = null; //Plays instantly at spawn point
	public GameObject impactParticles = null; //Plays at impact point
	public GameObject trailParticles = null;  //attaches to projectiles
	public GameObject trailEffect = null;	  //attaches to projectiles
	public GameObject projectile = null;	      //The spawned object, needs to be a rocket of some sort.
	public float fireRate = 1f; 			      //rounds fired per second
	public AmmoType type = AmmoType.projectile;
	private Vector2 power = Vector2.zero;
	private bool isFiring = false;
	private GameObject muzzleInstance = null;
	
	#endregion
	
	public enum AmmoType
	{
		projectile,
		ray,
	}
	
	#region private members
	private float firetick = 0f;
	#endregion
	
	#region public methods
	public void BeginFire(Vector2 _power)
	{
		power = _power;
		isFiring = true; 
		
		if(muzzleParticle != null)
		{
			muzzleInstance = GameObject.Instantiate(muzzleParticle) as GameObject;
		}
	}
	public void EndFire()
	{
		isFiring = false;
		firetick = 0;
	 	Destroy(muzzleInstance);
	}
	#endregion
	#region private methods
	// Use this for initialization
	void Start ()
	{
	
	}
	// Update is called once per frame
	void Update ()
	{
		if(isFiring)
			firetick += Time.deltaTime;
		else 
			firetick = 0;
		
		if(firetick > fireRate)
		{
			firetick = 0;
			switch(type)
			{
			case AmmoType.projectile:
				//Spawn a rocket/projectile and let it fly
				if(projectile != null)
				{
					//TODO: Make the spawn point spawn more accurately
					Vector3 spawnPoint = new Vector3(power.normalized.x*2f, power.normalized.y*2f, 0) + transform.position;
					GameObject instProjectile = Instantiate(projectile, spawnPoint, Quaternion.identity) as GameObject;
					Projectile projb = instProjectile.GetComponentInChildren<Projectile>() as Projectile;
					
					
					if(muzzleInstance != null)
					{
						//TODO: Make sure this is instantiated first.
						muzzleInstance.transform.position = spawnPoint;
						muzzleInstance.transform.LookAt(spawnPoint);
						muzzleInstance.particleSystem.Play();
					}
					
					if(trailParticles != null)
					{
						GameObject trail = GameObject.Instantiate(trailParticles, spawnPoint, Quaternion.identity) as GameObject;
						trail.transform.parent = instProjectile.transform;
					}
					
					if(trailEffect != null)
					{
						GameObject trail = GameObject.Instantiate(trailEffect, spawnPoint, Quaternion.identity) as GameObject;
						trail.transform.parent = instProjectile.transform;
					}
					
					projb.ApplyForce(power*projb.firePower, ForceMode.Impulse);
				}
				else Debug.Log("The projectile field is blank or the object is null");
				
				break;
			case AmmoType.ray:
				//do a ray cast.
				break;
			}
		}
	
	}
#endregion
}
