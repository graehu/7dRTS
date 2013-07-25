using UnityEngine;
using System.Collections;

public class Rocket : PhysicsBody {
	
	#region public members
	public float firePower = 5f; 	// the inital power of the shot.
	public float maxThrust = 1f; 	// the speed of the thruster with full fuel.
	public float minThrust = 0f; 	// the speed of the thruster once the fuel runs out.
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
			
			//TODO: Remove if air resistence is added.
			//To make up for a lack of air resistence, if a body is flying faster than terminal velocity
			//don't apply it's force. (also scale its force the closer it gets to it's terminal velocity)
			if(current.velocity.magnitude < actualThrust)
				actualThrust = actualThrust*(1 - current.velocity.magnitude/actualThrust);
			else actualThrust = 0; //possibly make this it's min force? probably not a good idea.
			
			ApplyForce(actualThrust*current.velocity.normalized, ForceMode.Force);
		}
		else
		{
			if(current.velocity.magnitude < minThrust)
				actualThrust = actualThrust*(1 - current.velocity.magnitude/minThrust);
			else actualThrust = 0;
			
			ApplyForce(minThrust*current.velocity.normalized, ForceMode.Force);
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
