using UnityEngine;
using System.Collections;

public class FollowTarget : MonoBehaviour {
	
	public Transform target = null;
	public float maxSpeed = 10;
	public float maxTurningSpeed = 10;
		
	// Update is called once per frame
	void Update () 
	{
		if(target != null)
		{
			if(maxSpeed > 0)
				transform.position = Vector3.MoveTowards( transform.position, target.position, maxSpeed * Time.deltaTime);
			else
				transform.position = target.position;
			
			if(maxTurningSpeed > 0)
				transform.rotation = Quaternion.RotateTowards( transform.rotation, target.rotation, maxTurningSpeed * Time.deltaTime);
			else
				transform.rotation = target.rotation;	
		}
	}
}
