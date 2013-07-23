using UnityEngine;
using System.Collections.Generic;

public class PlayerControl : MonoBehaviour {
	
	#region public types
	
	public enum SelectionState
	{
		None,
		PendingRelease,
		SelectingArea,
		Selected,
		Aiming
	}
	
	#endregion
	
	#region public variables
	
	public LayerMask unitMask = new LayerMask();
	public LayerMask movePlaneMask = new LayerMask();
	public float distanceBeforeAreaSelect = 10f;
	public float maxAimingDistance = 5f;
	//TODO: Move to protected
	public List<UnitTracker> selectedUnits = new List<UnitTracker>();
	public SelectionState selectState = SelectionState.None;
	
	
	#endregion
	
	#region protected variables
	
	protected Vector3 mouseDown = Vector2.zero;
	protected Vector2 aimVector = Vector2.zero;
	protected UnitTracker aimingUnit = null;
	
	#endregion
	
	#region public methods
	
	public void Fire()
	{
		GameObject rocket = Resources.Load("Rocket") as GameObject;
		GameObject instRocket = Instantiate(rocket) as GameObject;
		PhysicsBody phyRocket = instRocket.GetComponent("PhysicsBody") as PhysicsBody;
		//instRocket.transform.position = aimingUnit.transform.position;
		phyRocket.Position = aimingUnit.transform.position;
		phyRocket.ApplyForce(aimVector);
		Debug.Log( "Fire!!!" + "x: " + aimVector.x + ", y:" + aimVector.y);
	}
	public void Aim(Vector2 _aimVector)
	{
		aimVector = _aimVector;
	}
	
	public bool TrySelect(Vector2 _screenPos)
	{
		bool r = false;
		
		DeselectAll();
		
		Ray ray = Camera.mainCamera.ScreenPointToRay(_screenPos);
		
		RaycastHit hitInfo = new RaycastHit();
		
		if( Physics.Raycast(ray, out hitInfo, 1000, unitMask) )
		{
			if(hitInfo.collider != null)
			{
				UnitTracker unit = hitInfo.collider.GetComponent<UnitTracker>();
				if(unit != null)
				{
					Select(unit);
					r = true;
				}
			}
		}
		
		return r;
	}
	
	public bool TrySelectArea(Rect _screenArea)
	{
		bool r = false;
		
		DeselectAll();
		
		List<UnitTracker> team = GameManager.GetTeam(GameManager.clientTeam);
		for(int i = 0; i < team.Count; i++)
		{
			Vector2 screenPos = Camera.mainCamera.WorldToScreenPoint( team[i].transform.position );
			if(_screenArea.Contains(screenPos))
			{
				Select(team[i]);
				r = true;
			}
		}
		
		return r;
	}
	
	public void Select(UnitTracker _unit)
	{
		//debug
		_unit.GetComponentInChildren<Renderer>().material.color = Color.red;
		
		//select
		selectedUnits.Add(_unit);
		selectState = SelectionState.Selected;
	}
	
	public void DeselectAll()
	{
		//debug
		foreach(UnitTracker unit in selectedUnits)
			unit.GetComponentInChildren<Renderer>().material.color = Color.white;
		
		//deselect
		selectedUnits.Clear();
		selectState = SelectionState.None;
	}
	
	public void MoveSelectedUnitsTo(Vector3 _pos)
	{
		for(int i = 0; i < selectedUnits.Count; i++)
		{
			AIPathXY aiPath = selectedUnits[i].GetComponent<AIPathXY>();
			if(aiPath != null)
			{
				aiPath.SendMessageUpwards("MoveTo", _pos);;
			}
		}
	}
	
	#endregion
	
	#region monobehaviour methods
	
	// Use this for initialization
	void Start () 
	{
		mouseDown = Input.mousePosition;
	}
	
	void OnGUI ()
	{
		switch(selectState)
		{
		case SelectionState.SelectingArea:
			Rect area = Rect.MinMaxRect(Mathf.Min(mouseDown.x,Input.mousePosition.x), Mathf.Min(Screen.height-mouseDown.y,Screen.height-Input.mousePosition.y),
										Mathf.Max(mouseDown.x,Input.mousePosition.x), Mathf.Max(Screen.height-mouseDown.y,Screen.height-Input.mousePosition.y) );
			GUI.Box(area,"");
			break;
		}
	}
	// Update is called once per frame
	void Update () 
	{
		if(Input.GetMouseButtonDown(0))
		{
			//cache pos
			mouseDown = Input.mousePosition;
			
			switch(selectState)
			{
			case SelectionState.None:
			case SelectionState.Selected:
				//raycast test for selection
				selectState = SelectionState.PendingRelease;
				break;
			case SelectionState.PendingRelease:
				//we shouldn't be here...
				break;
			case SelectionState.SelectingArea:
				//we shouldn't be here...
				break;
			}
		}
		else if(Input.GetMouseButtonUp(0))
		{
			switch(selectState)
			{
			case SelectionState.None:
			case SelectionState.Selected:
				//nothing to do
				break;
			case SelectionState.PendingRelease:
				//perform single select
				if(!TrySelect(mouseDown))
					DeselectAll();
				break;
			case SelectionState.SelectingArea:
				//perform selection in given area
				
				Rect area = Rect.MinMaxRect(Mathf.Min(mouseDown.x,Input.mousePosition.x), Mathf.Min(mouseDown.y,Input.mousePosition.y),
											Mathf.Max(mouseDown.x,Input.mousePosition.x), Mathf.Max(mouseDown.y,Input.mousePosition.y) );
				
				if(!TrySelectArea(area))
					DeselectAll();
				break;
			}
		}
		else if( Input.GetMouseButton(0) )
		{
			//test for movement
			if( selectState == SelectionState.PendingRelease && Vector3.Distance(mouseDown, Input.mousePosition) > distanceBeforeAreaSelect )
				selectState = SelectionState.SelectingArea;
		}
		
		if( Input.GetMouseButtonDown(1) )
		{
			switch(selectState)
			{
			case SelectionState.Selected:
				//Test for a unit
				Ray characterRay = Camera.mainCamera.ScreenPointToRay(Input.mousePosition);
		
				RaycastHit hitInfo = new RaycastHit();
				
				if( Physics.Raycast(characterRay, out hitInfo, 1000, unitMask) )
				{
					//TODO: Tidy this functionality up. (change when assets arrive)
					if(hitInfo.collider != null)
					{
						UnitTracker unit = hitInfo.collider.GetComponent<UnitTracker>();
						if(unit != null && selectedUnits.Contains(unit))
						{
							selectState = SelectionState.Aiming;
							aimingUnit = unit;
						}
					}
				}
				else
				{	
					Ray ray = Camera.mainCamera.ScreenPointToRay(Input.mousePosition);
					if(Physics.Raycast(ray, out hitInfo, 1000, movePlaneMask))
						MoveSelectedUnitsTo(hitInfo.point);
				}
				break;
			}
		}
		else if(Input.GetMouseButtonUp(1))
		{
			switch(selectState)
			{
			case SelectionState.Aiming:
				if(selectedUnits.Count > 0)
					selectState = SelectionState.Selected;
				else selectState = SelectionState.None;
				Fire();
				break;
			}
		}
		else
		{
			switch(selectState)
			{
			case SelectionState.Aiming: //TODO: If aiming bugs occur insure mouse is depressed.
				//Update aiming direction
				RaycastHit hitInfo = new RaycastHit();
				Ray ray = Camera.mainCamera.ScreenPointToRay(Input.mousePosition);
				if(aimingUnit != null && Physics.Raycast(ray, out hitInfo, 1000, movePlaneMask))
				{
					Vector3 v = hitInfo.point - aimingUnit.transform.position;
					if(v.magnitude > maxAimingDistance)
					{
						v = v.normalized*maxAimingDistance;
					}
					Aim(v);
				}
					
				break;
			}
		}
	}
	
	#endregion
}
