using UnityEngine;
using System.Collections;

public class FollowTarget : MonoBehaviour {
	
	public Transform target = null;
	public PhysicsBody body = null;
	
	public float maxSpeed = 10;
	public float maxTurningSpeed = 10;
		
	// Update is called once per frame
	void LateUpdate () 
	{		
		Transform t;
		
		float speed = maxSpeed * Time.deltaTime;
		float turningSpeed = maxTurningSpeed * Time.deltaTime;
		
		if(body != null)
		{
			speed = body.LastVelocity.magnitude * Time.deltaTime;
			//maxTurningSpeed = body.angualarVelocity.magnitude;
			turningSpeed = 0 * GameManager.TurnLength;
			
			t = body.transform;
		}
		else if(target != null)
		{
			t = target;
		}
		else
			return;
			
			
		if(maxSpeed > 0)
			transform.position = Vector3.MoveTowards( transform.position, t.position, speed);
		else
			transform.position = t.position;
		
		if(maxTurningSpeed > 0)
			transform.rotation = Quaternion.RotateTowards( transform.rotation, t.rotation, turningSpeed);
		else
			transform.rotation = t.rotation;	
	}
}
