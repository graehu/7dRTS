using UnityEngine;
using System.Collections;

public class Rocket : PhysicsBody {
	
	#region public members
	public float firePower = 5f; 	// the inital power of the shot.
	public float maxThrust = 1f; 	// the power of the thruster with full fuel.
	public float minThrust = 0f; 	// the power of the thruster once the fuel runs out.
	public float fuelDuration = 1f; // how long the fuel lasts in seconds.
	#endregion
	
	
	#region private members
	float fuelTick = 0;
	#endregion

	// Use this for initialization
	void Start ()
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{
		//This force needs to scale.
		float actualThrust = 0;
		fuelTick += Time.deltaTime;
		if(fuelTick < fuelDuration)
		{
			actualThrust = Mathf.Lerp(maxThrust, minThrust, fuelTick/fuelDuration);
			ApplyForce(actualThrust*current.velocity.normalized, ForceMode.Force);
		}
		base.Update();
	}
	
	void OnDrawGizmos()
	{
		Vector2 dir = current.velocity.normalized;
		Vector3 endpoint = new Vector3(transform.position.x+dir.x, transform.position.y+dir.y,transform.position.z);
		Gizmos.DrawLine(transform.position, endpoint);
	}
}
