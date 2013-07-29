using UnityEngine;
using System.Collections;

public class TroopAnimator : MonoBehaviour {

	protected Animator animator;

	public float DirectionDampTime = .25f;

	void Start () 
	{
		animator = GetComponent<Animator>();
	}

	void Update () 
	{
		if(animator)
		{
			animator.SetBool("Firing", true);
			animator.SetFloat("AimDir", 0.0f);
			
			//get the current state
			AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

			//if we're in "Run" mode, respond to input for jump, and set the Jump parameter accordingly. 
			if(stateInfo.nameHash == Animator.StringToHash("Base Layer.RunBT"))
			{
				if(Input.GetButton("Fire1")) 
					animator.SetBool("Jump", true );
			}
			else
			{
				animator.SetBool("Jump", false);				
			}

			float h = Input.GetAxis("Horizontal");
			float v = Input.GetAxis("Vertical");

			//set event parameters based on user input
			animator.SetFloat("Speed", h*h+v*v);
			animator.SetFloat("Direction", h, DirectionDampTime, Time.deltaTime);
		}		
	}   
}
