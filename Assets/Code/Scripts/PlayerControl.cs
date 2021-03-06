using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(NetworkView))]
public class PlayerControl : MonoBehaviour {
	
	#region public types
	
	public enum ActionType
	{
		None,
		SelectSingle,
		SelectArea,
		MoveSelected,
		FireSelected,
		FireSelectedPoint
	}
	
	/// <summary>
	/// Data structure representing the players controls for a lock-step turn
	/// </summary>
	//
	[System.Serializable]
	public class ControlAction
	{		
		public ActionType type = ActionType.None;
		
		public Vector3 aimVector = Vector3.zero;
		
		//cursor positions in world space on the game plane
		public Vector3 worldMouseDown = Vector3.zero;
		public Vector3 worldMouseUp = Vector3.zero;
		
		public ControlAction Clone ()
		{
			return (ControlAction) MemberwiseClone();
		}
	}
	
	[System.Serializable]
	public class ControlSnapshot
	{
		public int turnID = 0;
		public List<ControlAction> actions = new List<ControlAction>();
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
			//make sure we have spawned everything on turn 0
			if(GameManager.CurrentTurn == 0 && GameManager.GetTeam(Index).Count == 0)
				return false;
			
			if(turnBuffer.Find(t => t.turnID == GameManager.CurrentTurn) != null)
				return true;
			else
				return false;
		} 
	}
	
	public int Index { get { return playerID; } }
	public bool IsMine { get { return Network.peerType == NetworkPeerType.Disconnected || networkView.isMine; } }
	
	#endregion
	
	#region public variables
	
	public UnitTracker unitTemplate = null;
	
	public LayerMask unitMask = new LayerMask();
	public LayerMask movePlaneMask = new LayerMask();
	public float distanceBeforeAreaSelect = 10f;
	public float maxAimingDistance = 5f;
	
	public AudioClip selectionSound = null;
	public AudioClip moveSound = null;
	public AudioClip attackSound = null;
	
	#endregion
	
	#region protected variables
	
	protected int playerID = 0;
	
	protected ControlAction currentAction = new ControlAction();
	protected ControlSnapshot currentSnapshot = new ControlSnapshot();
	protected List<ControlSnapshot> turnBuffer = new List<ControlSnapshot>();
	
	protected List<UnitTracker> selectedUnits = new List<UnitTracker>();
	protected SelectionState selectState = SelectionState.None;
	
	protected UnitTracker aimingUnit = null;
	
	protected Vector3 lastMouseDownPos = Vector3.zero;
	protected Vector3 lastMouseUpPos = Vector3.zero;
	
	protected float commandSoundTimeStamp = 0;
	
	#endregion
	
	#region public methods
	
	public void Fire(Vector3 aimVector)
	{
		foreach(UnitTracker unit in selectedUnits)
		{	
			if(unit.AI.TargetReached)
			{
				float powerScale = aimVector.magnitude/maxAimingDistance;
				Vector2 power = aimVector.normalized*powerScale;
				
				Vector3 basePos = unit.transform.position;
				unit.aimingReticle.transform.position = new Vector3(basePos.x+aimVector.x, basePos.y+aimVector.y, basePos.z);;
				 
				//Tells the unit to tell it's weapon to fire.
				unit.weapons[unit.CurrentWeapon].BeginFire(power, playerID);
			}
		}
		GameManager.Instance.PlaySoundFx2D(attackSound, 0.6f);
	}
	
	public void FireAtPoint(Vector3 _pos)
	{
		foreach(UnitTracker unit in selectedUnits)
		{	
			if(unit.AI.TargetReached)
			{
				Vector3 power = (_pos - unit.transform.position).normalized; //use max power
				Vector3 aimVector = power * maxAimingDistance;
				
				unit.aimingReticle.SetActive(true);
				Vector3 basePos = unit.transform.position;
				unit.aimingReticle.transform.position = new Vector3(basePos.x+aimVector.x, basePos.y+aimVector.y, basePos.z);;
				
				//Tells the unit to tell it's weapon to fire.
				unit.weapons[unit.CurrentWeapon].BeginFire(power, playerID);
			}
		}
		PlayCommandSound(attackSound);
	}
	
	public void Aim(Vector2 _aimVector)
	{
		foreach(UnitTracker unit in selectedUnits)
		{	
			if(unit.AI.TargetReached)
			{
				Vector3 pos = unit.transform.position;
				Vector3 endpoint = new Vector3(_aimVector.x+pos.x, _aimVector.y+pos.y, pos.z);
				unit.aimingReticle.SetActive(true);
				unit.aimingReticle.transform.position = endpoint;
		
				currentAction.aimVector = _aimVector;
			}
		}
	}
	
	public bool TrySelect(Vector3 _pos)
	{		
		Debug.Log("Try Select Single");
		
		bool r = false;
		
		DeselectAll();
		
		UnitTracker unit = GetUnitAtPosition(_pos);
		if(unit != null)
		{
			Select(unit);
			r = true;
		}
		
		if(r) PlayCommandSound(selectionSound);
		
		return r;
	}
	
	public bool TrySelectArea(Vector3 p1, Vector3 p2)
	{
		Debug.Log("Try Select Area");
		
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
		
		if(r) PlayCommandSound(selectionSound);
		
		return r;
	}
	
	public void Select(UnitTracker _unit)
	{		
		//select
		selectedUnits.Add(_unit);
		
		if(IsMine)
			_unit.selectionGraphic.SetActive(true);
	}
	
	public void DeselectAll()
	{
		if(IsMine)
		{
			foreach(UnitTracker unit in selectedUnits)
				unit.selectionGraphic.SetActive(false);
		}
		
		//deselect
		selectedUnits.Clear();
	}
	
	public void MoveSelectedUnitsTo(Vector3 _pos)
	{
		Pathfinding.NNConstraint constraint = new Pathfinding.NNConstraint();
		
		//_pos.y -= (selectedUnits.Count/2)+1;
		
		//AstarPath.active.GetNearest(
		for(int i = 0; i < selectedUnits.Count; i++)
		{
			AIPathXY aiPath = selectedUnits[i].AI;
			if(aiPath != null)
			{				
				Pathfinding.NNInfo info = AstarPath.active.GetNearest(_pos, constraint);
				
				//_pos.y += i;
				
				if(info.constrainedNode != null)
				{
					constraint.excludedNodes.Add(info.constrainedNode);
					_pos = info.constClampedPosition;
				}
				else
				{
					constraint.excludedNodes.Add(info.node);
					_pos = info.clampedPosition;
				}
				
				aiPath.MoveTo( _pos);
			}
			selectedUnits[i].aimingReticle.SetActive(false);
			selectedUnits[i].gameObject.BroadcastMessage("EndFire", SendMessageOptions.RequireReceiver);
		}
		
		if(selectedUnits.Count > 0)
			PlayCommandSound(moveSound);
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
	
	public void PlayCommandSound(AudioClip _clip)
	{
		if(IsMine && Time.time > commandSoundTimeStamp + 1f)
		{
			commandSoundTimeStamp = Time.time;
			GameManager.Instance.PlaySoundFx2D(moveSound, 0.6f);
		}
	}
	
	#endregion
	
	#region input methods
	
	public void TryCaptureTurn(int _turnID)
	{		
		if(!enabled) return;
		
		//Debug.Log(string.Format("Player {0} Capturing {1}", Index, _turnID));
		
		//cache current snapshot and give it the appropriate turnID if there isn't one made already
		if(turnBuffer.Find(t => t.turnID == _turnID) == null)
		{
			ControlSnapshot s = new ControlSnapshot();
			s.turnID = _turnID;
			s.actions.AddRange(currentSnapshot.actions);
			
			turnBuffer.Add(s);
			
			//clear cache
			currentSnapshot = new ControlSnapshot();
		}
		
		//remove expired turnes
		turnBuffer.RemoveAll(t => t.turnID < GameManager.CurrentTurn - GameManager.TURN_BUFFER_SIZE - 1);
	}
	
	public void ProcessTurn(int _turnID)
	{
		//Debug.Log(string.Format("Player {0} Processing {1}", Index, _turnID));
		
		//cleanup selection
		for(int i = 0; i < selectedUnits.Count; i++)
		{
			if(selectedUnits[i] == null)
				selectedUnits.RemoveAt(i--);
		}
		
		//get turn
		ControlSnapshot s = turnBuffer.Find(t => t.turnID == _turnID);
		
		foreach(ControlAction action in s.actions)
		{
			switch(action.type)
			{
			case ActionType.None:
				break;
			case ActionType.SelectSingle:
				if(!TrySelect(action.worldMouseDown))
					DeselectAll();
				break;
			case ActionType.SelectArea:
				if(!TrySelectArea(action.worldMouseDown,action.worldMouseUp))
					DeselectAll();
				break;
			case ActionType.MoveSelected:
				MoveSelectedUnitsTo(action.worldMouseDown);
				break;
			case ActionType.FireSelected:
				Fire(action.aimVector);
				break;
			case ActionType.FireSelectedPoint:
				FireAtPoint(action.worldMouseDown);
				break;
			}
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
	
	public void RecordAction(ActionType _type)
	{
		ControlAction a = currentAction.Clone();
		a.type = _type;
		currentSnapshot.actions.Add(a);
		
		if(selectedUnits.Count > 0)
			selectState = SelectionState.Selected;
		else 
			selectState = SelectionState.None;
	}
	
	#endregion
	
	#region monobehaviour methods
	
	// Use this for initialization
	void Awake () 
	{						
		//warm buffer to desired size
		turnBuffer.Clear();
		currentAction = new ControlAction();
		currentSnapshot = new ControlSnapshot();
		for(int i = GameManager.CurrentTurn; i < GameManager.CurrentTurn + GameManager.TURN_BUFFER_SIZE; i++)
		{
			ControlSnapshot s = new ControlSnapshot();
			s.turnID = i;
			turnBuffer.Add(s);
		}
	}
	
	void Start()
	{
		if(networkView.isMine || Network.peerType == NetworkPeerType.Disconnected)
		{
			CameraControl cam = Camera.mainCamera.GetComponent<CameraControl>();
			cam.MoveTo( GameManager.Instance.playerCamPositions[Index].position );
		}
		DeselectAll();
	}
	
	void OnDrawGizmos()
	{
		if(Input.GetMouseButton(1) && aimingUnit != null)
		{
			Vector2 dir = currentAction.aimVector;
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
				Vector3 p1 = Camera.mainCamera.WorldToScreenPoint(currentAction.worldMouseDown);
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
				currentAction.worldMouseDown = GetWorldMousePosition();
				
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
				currentAction.worldMouseUp = GetWorldMousePosition();
				
				switch(selectState)
				{
				case SelectionState.None:
				case SelectionState.Selected:
					//nothing to do
					break;
				case SelectionState.PendingRelease:
					RecordAction(ActionType.SelectSingle);
					break;
				case SelectionState.SelectingArea:
					//perform selection in given area
					RecordAction(ActionType.SelectArea);
					break;
				}
			}
			else if( Input.GetMouseButton(0) )
			{
				//test for movement
				if( selectState == SelectionState.PendingRelease && Vector3.Distance(lastMouseDownPos, Input.mousePosition) > distanceBeforeAreaSelect )
					selectState = SelectionState.SelectingArea;
			}
			else //mouse0 unpressed
			{
				if(selectState == SelectionState.PendingRelease)
				{
					RecordAction(ActionType.SelectSingle);
				}
			}
		}
		
		//do not process right mouse if left if pressed
		if( Input.GetMouseButton(0) == false)
		{
			if( Input.GetMouseButtonDown(1) )
			{
				currentAction.worldMouseDown = GetWorldMousePosition();
				
				UnitTracker unit = GetUnitAtPosition(currentAction.worldMouseDown);
				
				if(unit != null && selectedUnits.Contains(unit))
				{
					selectState = SelectionState.Aiming;
					aimingUnit = unit;
				}
				else if(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
				{
					RecordAction(ActionType.FireSelectedPoint);
				}
				else
				{	
					RecordAction(ActionType.MoveSelected);
				}
			}
			else if(Input.GetMouseButtonUp(1))
			{
				lastMouseUpPos = Input.mousePosition;
				currentAction.worldMouseUp = GetWorldMousePosition();
				
				switch(selectState)
				{
				case SelectionState.Aiming:
					
					RecordAction(ActionType.FireSelected);
					
					if(selectedUnits.Count > 0)
						selectState = SelectionState.Selected;
					else 
						selectState = SelectionState.None;
					
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
	
	void OnNetworkInstantiate(NetworkMessageInfo info)
	{
		playerID = int.Parse(networkView.owner.ToString());
		if(networkView.isMine)
		{
			name = "LocalPlayerContol";
		}
		else
		{
			name = "OtherPlayerControl";
			this.enabled = false;
			return;
		}
	}
	
	void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info)
	{		
		int bufferCount = 0;
		int turnID = -1;
		int actionCount = 0;
		int actionType = 0;
		Vector3 aimVector = Vector3.zero;
		Vector3 worldMouseUp = Vector3.zero;
		Vector3 worldMouseDown = Vector3.zero;		
		
		if(stream.isWriting)
		{			
			bufferCount = turnBuffer.Count;
			stream.Serialize(ref bufferCount);
		
			for(int i = 0; i < bufferCount; i++)
			{
				ControlSnapshot s = turnBuffer[i];
				
				turnID = s.turnID;
				stream.Serialize(ref turnID);
				
				actionCount = s.actions.Count;
				stream.Serialize(ref actionCount);
				
				for(int j = 0; j < actionCount; j++)
				{
					ControlAction a = s.actions[0];
					
					actionType = (int)a.type;
					aimVector = a.aimVector;
					worldMouseUp = a.worldMouseUp;
					worldMouseDown = a.worldMouseDown;
					
					stream.Serialize(ref actionType);
					stream.Serialize(ref aimVector);
					stream.Serialize(ref worldMouseUp);
					stream.Serialize(ref worldMouseDown);
				}
			}
		}
		else //reading
		{
			stream.Serialize(ref bufferCount);
			turnBuffer.Clear();			
		
			for(int i = 0; i < bufferCount; i++)
			{
				ControlSnapshot s = new ControlSnapshot();
				
				stream.Serialize(ref turnID);
				s.turnID = turnID;
				
				stream.Serialize(ref actionCount);
				
				for(int j = 0; j < actionCount; j++)
				{
					ControlAction a = new ControlAction();
					
					stream.Serialize(ref actionType);
					stream.Serialize(ref aimVector);					
					stream.Serialize(ref worldMouseUp);		
					stream.Serialize(ref worldMouseDown);
					
					a.type = (ActionType) actionType;
					a.aimVector = aimVector;
					a.worldMouseUp = worldMouseUp;
					a.worldMouseDown = worldMouseDown;
					
					s.actions.Add(a);
				}
				
				turnBuffer.Add(s);
			}
		}
	}
	
	#endregion
}
