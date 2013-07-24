using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(NetworkView))]
public class PlayerControl : MonoBehaviour {
	
	#region public types
	
	public enum ControlAction
	{
		None,
		SelectSingle,
		SelectArea,
		MoveSelected,
		FireSelected
	}
	
	/// <summary>
	/// Data structure representing the players controls for a lock-step turn
	/// </summary>
	//
	[System.Serializable]
	public class ControlSnapShot
	{
		public int turnID = 0;
		public ControlAction action = PlayerControl.ControlAction.None;
		public Vector2 aimVector = Vector2.zero;
		public Vector3 mouseDown = Vector3.zero;
		public Vector3 mouseUp = Vector3.zero;
		
		public ControlSnapShot Clone ()
		{
			return (ControlSnapShot) MemberwiseClone();
		}
	}
	
	public enum SelectionState
	{
		None,
		PendingRelease,
		SelectingArea,
		Selected,
		Aiming
	}
	
	#endregion
	
	#region public properties
	
	public bool IsUpToDate 
	{ 
		get 
		{ 
			//return turnBuffer.Find(t => t.turnID >= GameManager.CurrentTurn + GameManager.TURN_BUFFER_SIZE) != null;
			return turnBuffer.Count >= GameManager.TURN_BUFFER_SIZE;
		} 
	}
	
	#endregion
	
	#region public variables
	
	public UnitTracker unitTemplate = null;
	
	public LayerMask unitMask = new LayerMask();
	public LayerMask movePlaneMask = new LayerMask();
	public float distanceBeforeAreaSelect = 10f;
	public float maxAimingDistance = 5f;
	
	//TODO: Move to protected
	public List<UnitTracker> selectedUnits = new List<UnitTracker>();
	public SelectionState selectState = SelectionState.None;
	
	#endregion
	
	#region protected variables
	
	protected ControlSnapShot snapShot = new ControlSnapShot();
	protected List<ControlSnapShot> turnBuffer = new List<ControlSnapShot>();
	
	protected UnitTracker aimingUnit = null;
	
	#endregion
	
	#region public methods
	
	public void Fire(Vector3 aimVector)
	{
		foreach(UnitTracker unit in selectedUnits)
		{
			GameObject rocket = Resources.Load("Rocket") as GameObject;
			GameObject instRocket = Instantiate(rocket) as GameObject;
			PhysicsBody phyRocket = instRocket.GetComponent("PhysicsBody") as PhysicsBody;
			//instRocket.transform.position = aimingUnit.transform.position;
			phyRocket.Position = unit.transform.position;
			phyRocket.ApplyForce(aimVector);
			Debug.Log( string.Format("'{0}' Fired: {1}", unit.name, aimVector.ToString()) );
		}
	}
	
	public void Aim(Vector2 _aimVector)
	{
		snapShot.aimVector = _aimVector;
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
				if(unit != null && unit.teamID == GameManager.clientTeam)
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
	
	#region input methods
	
	public void TryCaptureTurn(int _turnID)
	{		
		if(!enabled) return;
		
		//cache current snapshot and give it the appropriate turnID
		ControlSnapShot s = snapShot.Clone();
		s.turnID = _turnID;
		turnBuffer.Add(s);
		
		//remove all old turns
		turnBuffer.RemoveAll(t => t.turnID < GameManager.CurrentTurn);
		
		//clear action
		snapShot.action = ControlAction.None;
	}
	
	public void ProcessTurn(int _turnID)
	{
		//get turn
		ControlSnapShot s = turnBuffer.Find(t => t.turnID == _turnID);
		turnBuffer.Remove(s);
		
		switch(s.action)
		{
		case ControlAction.None:
			break;
		case ControlAction.SelectSingle:
			if(!TrySelect(s.mouseDown))
				DeselectAll();
			break;
		case ControlAction.SelectArea:
			Rect area = Rect.MinMaxRect(Mathf.Min(s.mouseDown.x,s.mouseUp.x), Mathf.Min(s.mouseDown.y,s.mouseUp.y),
										Mathf.Max(s.mouseDown.x,s.mouseUp.x), Mathf.Max(s.mouseDown.y,s.mouseUp.y) );
			if(!TrySelectArea(area))
				DeselectAll();
			break;
		case ControlAction.MoveSelected:
			Ray ray = Camera.mainCamera.ScreenPointToRay(s.mouseDown);
			RaycastHit hitInfo = new RaycastHit();
			if(Physics.Raycast(ray, out hitInfo, 1000, movePlaneMask))
				MoveSelectedUnitsTo(hitInfo.point);
			break;
		case ControlAction.FireSelected:
			Fire(s.aimVector);
			break;
		}
		
		//Debug.Log(string.Format("Player {0}: {1}", GameManager.clientTeam, s.action));
	}
	
	#endregion
	
	#region monobehaviour methods
	
	// Use this for initialization
	void Start () 
	{
		if(!string.IsNullOrEmpty(networkView.owner.ipAddress) && networkView.owner != Network.player)
			this.enabled = false;
		
		//warm buffer to desired size
		turnBuffer.Clear();
		snapShot = new ControlSnapShot();
		for(int i = GameManager.CurrentTurn; i < GameManager.CurrentTurn + GameManager.TURN_BUFFER_SIZE; i++)
		{
			ControlSnapShot s = snapShot.Clone();
			s.turnID = i;
			turnBuffer.Add(s);
		}
	}
	
	void OnGUI ()
	{		
		switch(selectState)
		{
		case SelectionState.SelectingArea:
			ControlSnapShot s = snapShot;
			
			Vector2 mouseUp = Input.mousePosition;
			if(s.action == ControlAction.SelectArea)
				mouseUp = s.mouseUp;
			
			Rect area = Rect.MinMaxRect(Mathf.Min(s.mouseDown.x,mouseUp.x), Mathf.Min(Screen.height-s.mouseDown.y,Screen.height-mouseUp.y),
										Mathf.Max(s.mouseDown.x,mouseUp.x), Mathf.Max(Screen.height-s.mouseDown.y,Screen.height-mouseUp.y) );
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
			snapShot.mouseDown = Input.mousePosition;
			
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
			snapShot.mouseUp = Input.mousePosition;
			
			switch(selectState)
			{
			case SelectionState.None:
			case SelectionState.Selected:
				//nothing to do
				break;
			case SelectionState.PendingRelease:
				//perform single select
				snapShot.action = ControlAction.SelectSingle;
				break;
			case SelectionState.SelectingArea:
				//perform selection in given area
				snapShot.action = ControlAction.SelectArea;
				break;
			}
		}
		else if( Input.GetMouseButton(0) )
		{
			//test for movement
			if( selectState == SelectionState.PendingRelease && Vector3.Distance(snapShot.mouseDown, Input.mousePosition) > distanceBeforeAreaSelect )
				selectState = SelectionState.SelectingArea;
		}
		
		if( Input.GetMouseButtonDown(1) )
		{
			snapShot.mouseDown = Input.mousePosition;
			
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
					snapShot.action = ControlAction.MoveSelected;
				}
				break;
			}
		}
		else if(Input.GetMouseButtonUp(1))
		{
			snapShot.mouseUp = Input.mousePosition;
			
			switch(selectState)
			{
			case SelectionState.Aiming:
				if(selectedUnits.Count > 0)
					selectState = SelectionState.Selected;
				else selectState = SelectionState.None;
				
				snapShot.action = ControlAction.FireSelected;
				
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
	
	#region networking methods
	
	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{
		//temp variables
		int turnID = -1;
		int action = -1;
		Vector3 aimVector = Vector3.zero;
		Vector3 mouseUp = Vector3.zero;
		Vector3 mouseDown = Vector3.zero;
		
		int bufferCount = turnBuffer.Count;
		
		//serialise buffer count so we know now many to recieve
		stream.Serialize(ref bufferCount);
		
		if(stream.isReading)
			turnBuffer.Clear();
		
		for(int i = 0; i < bufferCount; i++)
		{
			ControlSnapShot s = turnBuffer[i];
			
			//if we're writing, get variables to write
			if(stream.isWriting)
			{
				turnID = s.turnID;
				action = (int)s.action;
				aimVector = s.aimVector;
				mouseDown = s.mouseDown;
				mouseUp = s.mouseUp;
			}
			else //create new snapshot to read into
				s = new ControlSnapShot();
			
			//serialise
			stream.Serialize(ref turnID);
			stream.Serialize(ref action);
			stream.Serialize(ref aimVector);
			stream.Serialize(ref mouseDown);
			stream.Serialize(ref mouseUp);
			
			//if we're reading, fill new snapshot
			if(stream.isReading)
			{
				s.turnID = turnID;
				s.action = (ControlAction) action;
				s.aimVector = aimVector;
				s.mouseDown = mouseDown;
				s.mouseUp = mouseUp;
				turnBuffer.Add(s);
			}
			
		}
	}
	
	#endregion
}
