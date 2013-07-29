using UnityEngine;
using System.Collections;

public class CameraControl : MonoBehaviour {
	
	#region public variables
	
	public Collider boundingArea = null;
	
	public float edgeScrollSpeed = 10;
	public float zoomSpeed = 10;
	
	public float minZoomDistance = 1;
	
	public LayerMask movePlaneMask;
	
	#endregion
	
	#region protected variables
	
	public Vector3 desiredPos = Vector3.zero;
	public Vector3 lastMousePos = Vector3.zero;
	
	#endregion
	
	#region public methods
	
	public void MoveTo(Vector3 _pos)
	{
		desiredPos = _pos;
	}
	
	#endregion
	
	#region private methods
	
	Vector3 GetWorldMousePosition()
	{
		//TODO handle case where ray doesn't intersect plane
		Ray ray = Camera.mainCamera.ScreenPointToRay(Input.mousePosition);
		RaycastHit hitInfo = new RaycastHit();
		Physics.Raycast(ray, out hitInfo, 1000, movePlaneMask);
		return hitInfo.point;
	}
	
	Vector2 GetCameraFrustumCrossSectionSize(float distance)
	{		
		float frustumHeight = 2.0f * distance * Mathf.Tan(Camera.mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
		float frustumWidth = frustumHeight * Camera.mainCamera.aspect;
		
		//return Rect.MinMaxRect(pos.x - frustumWidth*0.5f, pos.y + frustumHeight*0.5f, pos.x + frustumWidth*0.5f, pos.y - frustumHeight*0.5f);
		return new Vector2(frustumWidth, frustumHeight);
	}
	
	float GetDistanceForCameraCrossSectionSize(float width, float height)
	{
		//choose smallest fitting height
		height = Mathf.Min( height, width / Camera.mainCamera.aspect );
		//calc distance
		float distance = height * 0.5f / Mathf.Tan(Camera.mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
		return distance;
	}
	
	#endregion
	
	#region monobehaviour mnethods
	
	// Use this for initialization
	void Start () 
	{
		desiredPos = transform.position;
	}
	
	// Update is called once per frame
	void Update () 
	{
		Vector3 step = Vector3.zero;
		
		//handle edge scrolling
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
		
		//handle drag scroll
		if(Input.GetMouseButtonDown(2))
		{
			lastMousePos = GetWorldMousePosition();
		}
		else if(Input.GetMouseButton(2))
		{
			Vector3 mousePos;
			
			mousePos = GetWorldMousePosition();
			
			step = lastMousePos - mousePos;
			lastMousePos = mousePos + step;
		}
		
		//handle zooming
		Vector3 zoomStep = Vector3.zero;
		
		float maxZoomDistance = GetDistanceForCameraCrossSectionSize(boundingArea.bounds.size.x, boundingArea.bounds.size.y);
		
		float zoomInput = Input.GetAxis("Mouse ScrollWheel");
		
		if(zoomInput != 0)
			zoomStep = Camera.mainCamera.ScreenPointToRay(Input.mousePosition).direction * zoomSpeed * zoomInput;	
		
		step += zoomStep;
		
		//step the desired position
		desiredPos += step;
		
		//bound desired position
		Vector3 crossSectionSize = GetCameraFrustumCrossSectionSize(Mathf.Abs(desiredPos.z));		
		Vector3 min = boundingArea.bounds.min + (crossSectionSize*0.5f);
		Vector3 max = boundingArea.bounds.max - (crossSectionSize*0.5f);
		
		desiredPos.x = Mathf.Clamp(desiredPos.x, min.x, max.x);
		desiredPos.y = Mathf.Clamp(desiredPos.y, min.y, max.y);
		desiredPos.z = Mathf.Clamp(desiredPos.z, -maxZoomDistance, -minZoomDistance);
		
		//ease towards desired position
		Camera.mainCamera.transform.position = (Camera.mainCamera.transform.position + desiredPos) * 0.5f;
	}
	
	#endregion
}
