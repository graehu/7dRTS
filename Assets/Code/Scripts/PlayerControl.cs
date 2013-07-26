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
		
		//cursor positions in world space on the game plane
		public Vector3 worldMouseDown = Vector2.zero;
		public Vector2 worldMouseUp = Vector2.zero;
		
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
			/*
			//make sure we have the desired buffer
			for(int i = 0; i < GameManager.TURN_BUFFER_SIZE; i++)
			{
				if(turnBuffer.Find(t => t.turnID == GameManager.CurrentTurn + i) != null)
					return true;
			}
			return false;
			*/
			
			/*if(GameManager.Instance.localGame)
				return true;*/
			
			if(turnBuffer.Find(t => t.turnID == GameManager.CurrentTurn) != null)
				return true;
			else
				return false;
		} 
	}
	
	public int Index { get { return int.Parse(networkView.owner.ToString()); } }
	
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
	
	protected Vector3 lastMouseDownPos = Vector3.zero;
	protected Vector3 lastMouseUpPos = Vector3.zero;
	
	#endregion
	
	#region public methods
	
	public void Fire(Vector3 aimVector)
	{
		foreach(UnitTracker unit in selectedUnits)
		{
			
			GameObject rocket = Resources.Load("Rocket") as GameObject;
			GameObject instRocket = Instantiate(rocket) as GameObject;
			Rocket phyRocket = instRocket.GetComponent("Rocket") as Rocket;
			phyRocket.Position = unit.transform.position;
			float powerScale = aimVector.magnitude/maxAimingDistance;
			
			aimVector = aimVector.normalized*(powerScale*phyRocket.firePower);	
			
			phyRocket.ApplyForce(aimVector, ForceMode.Impulse);
			Debug.Log( string.Format("'{0}' Fired: {1}", unit.name, aimVector.ToString()) );
		}
	}
	
	public void Aim(Vector2 _aimVector)
	{
		Vector2 dir = _aimVector;
		Vector3 pos = aimingUnit.transform.position;
		Vector3 endpoint = new Vector3(dir.x+pos.x, dir.y+pos.y, pos.z);
		aimingUnit.aimingReticle.renderer.enabled = true;
		aimingUnit.aimingReticle.transform.position = endpoint;

		snapShot.aimVector = _aimVector;
	}
	
	public bool TrySelect(Vector3 _pos)
	{		
		bool r = false;
		
		DeselectAll();
		
		UnitTracker unit = GetUnitAtPosition(_pos);
		if(unit != null)
		{
			Select(unit);
			r = true;
		}
		return r;
	}
	
	public bool TrySelectArea(Vector3 p1, Vector3 p2)
	{
		bool r = false;
		
		DeselectAll();
		
		//TODO create rect from points and test world pos along plane
		Rect area = Rect.MinMaxRect(Mathf.Min(p1.x,p2.x), Mathf.Min(p1.y,p2.y), 
									Mathf.Max(p1.x,p2.x), Mathf.Max(p1.y,p2.y));
		
		List<UnitTracker> team = GameManager.GetTeam(Index);
		for(int i = 0; i < team.Count; i++)
		{
			Vector2 pos = team[i].transform.position;
			if(area.Contains(pos))
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
		if(Index == 0)
			_unit.GetComponentInChildren<Renderer>().material.color = Color.red;
		else
			_unit.GetComponentInChildren<Renderer>().material.color = Color.green;
		
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
			AIPathXY aiPath = selectedUnits[i].AI;
			if(aiPath != null)
			{
				aiPath.SendMessageUpwards("MoveTo", _pos);
			}
			selectedUnits[i].aimingReticle.renderer.enabled = false;
		}
	}
	
	public UnitTracker GetUnitAtPosition(Vector3 _pos)
	{
		List<UnitTracker> units = GameManager.GetTeam(Index);
		
		foreach(UnitTracker unit in units)
		{
			if(unit.collider.bounds.Contains(_pos))
			{
				return unit;
			}
		}
		return null;
	}
	
	#endregion
	
	#region input methods
	
	public void TryCaptureTurn(int _turnID)
	{		
		if(!enabled) return;
		
		//Debug.Log(string.Format("Capturing Turn {0}", _turnID));
		
		//cache current snapshot and give it the appropriate turnID
		ControlSnapShot s = snapShot.Clone();
		s.turnID = _turnID;
		turnBuffer.Add(s);
		
		//remove expired turned
		turnBuffer.RemoveAll(t => t.turnID < GameManager.CurrentTurn - GameManager.TURN_BUFFER_SIZE);
		
		//clear action
		snapShot.action = ControlAction.None;
	}
	
	public void ProcessTurn(int _turnID)
	{
		Debug.Log(string.Format("Processing Turn {0}", _turnID));
		
		//temp variables
		RaycastHit hitInfo = new RaycastHit();
		
		//get turn
		ControlSnapShot s = turnBuffer.Find(t => t.turnID == _turnID);
		
		switch(s.action)
		{
		case ControlAction.None:
			break;
		case ControlAction.SelectSingle:
			if(!TrySelect(s.worldMouseDown))
				DeselectAll();
			break;
		case ControlAction.SelectArea:
			if(!TrySelectArea(s.worldMouseDown,s.worldMouseUp))
				DeselectAll();
			break;
		case ControlAction.MoveSelected:
			MoveSelectedUnitsTo(s.worldMouseDown);
			break;
		case ControlAction.FireSelected:
			Fire(s.aimVector);
			break;
		}
	}
	
	/// <summary>
	/// Gets the world mouse position by raycasting against the game plane
	/// </summary>
	/// <returns>
	/// The world mouse position.
	/// </returns>
	public Vector3 GetWorldMousePosition()
	{
		//TODO handle case where ray doesn't intersect plane
		Ray ray = Camera.mainCamera.ScreenPointToRay(Input.mousePosition);
		RaycastHit hitInfo = new RaycastHit();
		Physics.Raycast(ray, out hitInfo, 1000, movePlaneMask);
		return hitInfo.point;
	}
	
	#endregion
	
	#region monobehaviour methods
	
	// Use this for initialization
	void Start () 
	{
		if(!string.IsNullOrEmpty(networkView.owner.ipAddress))
		{
			if(networkView.owner == Network.player)
				name = "LocalPlayerContol";
			else
			{
				name = "OtherPlayerControl";
				this.enabled = false;
				return;
			}
		}
		
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
	void OnDrawGizmos()
	{
		if(Input.GetMouseButton(1) && aimingUnit != null)
		{
			Vector2 dir = snapShot.aimVector;
			Vector3 pos = aimingUnit.transform.position;
			Vector3 endpoint = new Vector3(dir.x+pos.x, dir.y+pos.y, pos.z);
			Gizmos.DrawLine(pos, endpoint);
		}
	}
	
	void OnGUI ()
	{		
		switch(selectState)
		{
		case SelectionState.SelectingArea:
			if(Input.GetMouseButton(0))
			{				
				Vector3 p1 = Camera.mainCamera.WorldToScreenPoint(snapShot.worldMouseDown);
				Vector3 p2 = Camera.mainCamera.WorldToScreenPoint(GetWorldMousePosition());
				
				Rect area = Rect.MinMaxRect(Mathf.Min(p1.x, p2.x), Mathf.Min(Screen.height - p1.y, Screen.height - p2.y), 
											Mathf.Max(p1.x, p2.x), Mathf.Max(Screen.height - p1.y, Screen.height - p2.y));
				
				GUI.Box(area,"");
			}
			break;
		}
	}
	// Update is called once per frame
	void Update () 
	{	
		//do not process left mouse if right if pressed
		if( Input.GetMouseButton(1) == false)
		{
			if(Input.GetMouseButtonDown(0))
			{
				//cache pos
				lastMouseDownPos = Input.mousePosition;
				snapShot.worldMouseDown = GetWorldMousePosition();
				
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
				lastMouseUpPos = Input.mousePosition;
				snapShot.worldMouseUp = GetWorldMousePosition();
				
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
				if( selectState == SelectionState.PendingRelease && Vector3.Distance(lastMouseDownPos, Input.mousePosition) > distanceBeforeAreaSelect )
					selectState = SelectionState.SelectingArea;
			}
		}
		
		//do not process right mouse if left if pressed
		if( Input.GetMouseButton(0) == false)
		{
			if( Input.GetMouseButtonDown(1) )
			{
				lastMouseDownPos = Input.mousePosition;
				snapShot.worldMouseDown = GetWorldMousePosition();
				
				UnitTracker unit = GetUnitAtPosition(snapShot.worldMouseDown);
				
				if(unit != null && selectedUnits.Contains(unit))
				{
					selectState = SelectionState.Aiming;
					aimingUnit = unit;
				}
				else
				{	
					snapShot.action = ControlAction.MoveSelected;
				}
			}
			else if(Input.GetMouseButtonUp(1))
			{
				lastMouseUpPos = Input.mousePosition;
				snapShot.worldMouseUp = GetWorldMousePosition();
				
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
			else if(Input.GetMouseButton(1))
			{
				switch(selectState)
				{
				case SelectionState.Aiming:
					
					//Update aiming direction
					if(aimingUnit != null)
					{
						Vector3 pos = GetWorldMousePosition();
						Vector3 v = pos - aimingUnit.transform.position;
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
			ControlSnapShot s = null;
			
			//if we're writing, get variables to write
			if(stream.isWriting)
			{
				s = turnBuffer[i];
				
				turnID = s.turnID;
				action = (int)s.action;
				aimVector = s.aimVector;
				mouseUp = s.worldMouseUp;
				mouseDown = s.worldMouseDown;
			}
			else //create new snapshot to read into
				s = new ControlSnapShot();
			
			//serialise
			stream.Serialize(ref turnID);
			stream.Serialize(ref action);
			stream.Serialize(ref aimVector);
			stream.Serialize(ref mouseUp);
			stream.Serialize(ref mouseDown);
			
			//if we're reading, fill new snapshot
			if(stream.isReading)
			{
				s.turnID = turnID;
				s.action = (ControlAction) action;
				s.aimVector = aimVector;
				s.worldMouseUp = mouseUp;
				s.worldMouseDown = mouseDown;
				turnBuffer.Add(s);
			}
			
		}
	}
	
	#endregion
}
