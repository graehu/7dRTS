using UnityEngine;
using System.Collections;

public class Rocket : PhysicsBody {
	
	public float firePower = 5f;
	public float thrust = 1; //the power of the thruster.
	public float fuelDuration = 1; //how long the fuel lasts in seconds./
	float tick = 0;
	
	//TODO: Probably not needed, remove after testing.
	/*Rocket(Vector2 initialDirection)
	{
		ApplyForce(initialDirection, ForceMode.Impulse);
	}*/
	
	

	// Use this for initialization
	void Start ()
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{
		//This force needs to scale.
		float actualThrust = 0;
		if(tick < fuelDuration)
		{
			actualThrust = Mathf.Lerp(thrust, 0, t/fuelDuration);
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
