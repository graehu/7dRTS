using UnityEngine;
using System.Collections;

public class CameraControl : MonoBehaviour {
	
	#region public variables
	
	public Collider boundingArea = null;
	
	public float edgeScrollSpeed = 10;
	public float zoomSpeed = 10;
	
	public LayerMask movePlaneMask;
	
	#endregion
	
	#region protected variables
	
	public Vector3 lastMousePos = Vector3.zero;
	
	#endregion
	
	#region public methods
	
	Vector3 GetWorldMousePosition()
	{
		//TODO handle case where ray doesn't intersect plane
		Ray ray = Camera.mainCamera.ScreenPointToRay(Input.mousePosition);
		RaycastHit hitInfo = new RaycastHit();
		Physics.Raycast(ray, out hitInfo, 1000, movePlaneMask);
		return hitInfo.point;
	}
	
	#endregion
	
	#region monobehaviour mnethods
	
	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{
		Vector3 step = Vector3.zero;
		
		if(Screen.fullScreen)
		{
			if(Input.mousePosition.x >= Screen.width-2)
				step.x = 1;
			else if(Input.mousePosition.x <= 2)
				step.x = -1;
			if(Input.mousePosition.y >= Screen.height-2)
				step.y = 1;
			else if(Input.mousePosition.y <= 2)
				step.y = -1;
		
			step *= edgeScrollSpeed * Time.deltaTime;
		}
		
		if(Input.GetMouseButtonDown(2))
		{
			if(Camera.mainCamera.isOrthoGraphic)
				lastMousePos = Camera.mainCamera.ScreenToWorldPoint(Input.mousePosition);
			else
				lastMousePos = GetWorldMousePosition();
		}
		else if(Input.GetMouseButton(2))
		{
			Vector3 mousePos;
			
			if(Camera.mainCamera.isOrthoGraphic)
				mousePos = Camera.mainCamera.ScreenToWorldPoint(Input.mousePosition);
			else
				mousePos = GetWorldMousePosition();
			
			step = lastMousePos - mousePos;
			lastMousePos = mousePos + step;
		}
		
		float zoomStep = Input.GetAxis("Mouse ScrollWheel");
		if(Camera.mainCamera.isOrthoGraphic)
		{
			step.z = zoomStep;
		}
		else
		{
			step.z = zoomStep;
		}
		step.z *= zoomSpeed;
		
		Camera.mainCamera.transform.Translate(step);
		Vector3 pos = Camera.mainCamera.transform.position;

		//bound Z
		pos.z = Mathf.Clamp(pos.z, boundingArea.bounds.min.z, boundingArea.bounds.max.z);
		
		//calc bounds compensation to keep world in view
		float comp = Mathf.InverseLerp(boundingArea.bounds.min.z, boundingArea.bounds.max.z, pos.z);
		
		Vector3 min = boundingArea.bounds.center - (boundingArea.bounds.extents * comp);
		Vector3 max = boundingArea.bounds.center + (boundingArea.bounds.extents * comp);
		
		//bound in XY
		pos.y = Mathf.Clamp(pos.y, min.y, max.y);
		pos.x = Mathf.Clamp(pos.x, min.x, max.x);
		
		Camera.mainCamera.transform.position = pos;
	}
	
	#endregion
}
