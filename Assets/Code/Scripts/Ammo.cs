using UnityEngine;
using System.Collections;

public class Ammo : MonoBehaviour {
	
	ParticleSystem impactEffect = null;
	ParticleSystem trailEffect = null;
	public AmmoType type = AmmoType.rocket;
	public enum AmmoType
	{
		rocket,
		bullet,
	}

	// Use this for initialization
	void Start ()
	{
	
	}
	
	// Update is called once per frame
	void Update ()
	{
		
	
	}
}
