using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PhysicsBody : MonoBehaviour {
	
	#region static helpers
	
	public static List<PhysicsBody> bodies = new List<PhysicsBody>();
	
	#endregion
	
	#region public members
	public float mass = 1f;
	public float size = 1f;
	#endregion
	
	
	#region protected members
	
	protected State previous = new State();
  	protected State current = new State();
	protected List<Vector2> activeForces = new List<Vector2>();

	//TODO: remove this after testing.
	public Vector2 velo = Vector2.zero;

	
	#endregion
	#region private members
	private float time = 0f;
	#endregion
	
	#region public properties
	
	public Vector2 Position
	{
		get {return current.position;}
		set {current.position = value;}
	}
	public Vector2 Velocity
	{
		get {return current.velocity;}
		set {current.velocity = value;}
	}
	public Vector2 LastVelocity
	{
		get {return previous.velocity;}
	}
	
	#endregion	
	
	/// <summary>
	/// Applies an impulsive force.
	/// </summary>
	/// <param name='force'>
	/// Force.
	/// </param>
	
	public void ApplyForce(Vector2 force, ForceMode mode)
	{
		//current.momentum = current.mass*force;
		//previous.momentum = force;
		switch(mode)
		{
		case ForceMode.Impulse:
			activeForces.Add(force);
			current.momentum += force;
			break;
		case ForceMode.Force:
			activeForces.Add(force);	
			break;
		default:
			Debug.Log("Unsupported ForceMode");
			break;
		}
	}


	// Use this for initialization
	void Awake ()
	{
		current.size = size;
		current.mass = mass;
		current.inverseMass = 1.0f / current.mass;
		current.position = transform.position;
		current.momentum = Vector2.zero;
		current.angularMomentum = Vector2.zero;
		current.inertiaTensor = current.mass * current.size * current.size * 1.0f / 6.0f;
		current.inverseInertiaTensor = 1.0f / current.inertiaTensor;
		current.recalculate();
		previous = current;
		
		//add to existing bodies
		bodies.Add(this);
	}
	
	void OnDestroy()
	{
		bodies.Remove(this);
	}
	
	// Update is called once per frame
	public virtual void Simulate (float _dt)
	{
		time += _dt;
		current.size = size;
		current.mass = mass;
		previous = current;
		integrate(current, time, _dt);
		transform.position = new Vector3(current.position.x, current.position.y, transform.position.z);
		transform.rotation = Quaternion.LookRotation(Velocity);
		activeForces.Clear();
		//if physics are 2d. do this:
		current.orientation.x = 0;
		current.orientation.y = 0;
	}
		
	protected class State
	{
	    /// primary physics state
	    public Vector2 position = Vector2.zero;            ///< the position of the cube center of mass in world coordinates (meters).
	    public Vector2 momentum = Vector2.zero;            ///< the momentum of the cube in kilogram meters per second.
	    public Quaternion orientation = new Quaternion();  ///< the orientation of the cube represented by a unit quaternion.
	    public Vector2 angularMomentum = Vector2.zero;     ///< angular momentum vector.
	
	    // secondary state
	    public Vector2 velocity = Vector2.zero;            ///< velocity in meters per second (calculated from momentum).
	    public Quaternion spin = new Quaternion();         ///< quaternion rate of change in orientation.
	    public Vector2 angularVelocity = Vector2.zero;     ///< angular velocity (calculated from angularMomentum).
	
	    /// constant state
	    public float size = 1f;                     ///< length of the cube sides in meters.
	    public float mass = 1f;                     ///< mass of the cube in kilograms.
	    public float inverseMass = 1f;              ///< inverse of the mass used to convert momentum to velocity.
	    public float inertiaTensor;            ///< inertia tensor of the cube (i have simplified it to a single value due to the mass properties a cube).
	    public float inverseInertiaTensor;     ///< inverse inertia tensor used to convert angular momentum to angular velocity.
		
		public void recalculate()
	    {
			
			//TODO: add a quaternion "Normalising" function to make this recalc orientations properly.
	        velocity = momentum * inverseMass;
	        angularVelocity = angularMomentum * inverseInertiaTensor;
	        //orientation.normalise();
	        //spin = (quaternion(0, angularVelocity.i, angularVelocity.j, angularVelocity.k) * orientation)*0.5;
	    }
		
		public State Clone() { return MemberwiseClone() as State; }
	}



	class Derivative
	{
		public Vector2 velocity;                ///< velocity is the derivative of position.
		public Vector2 force;                  	///< force in the derivative of momentum.
		public Quaternion spin = new Quaternion(); ///< spin is the derivative of the orientation quaternion.
		public Vector2 torque;                 	///< torque is the derivative of angular momentum.
	}
	
	


void integrate(State _state, float t, float dt)
{
	Derivative a = evaluate(_state, t);
	Derivative b = evaluate(_state.Clone(), t, dt*0.5f, a);
	Derivative c = evaluate(_state.Clone(), t, dt*0.5f, b);
	Derivative d = evaluate(_state.Clone(), t, dt, c);
	
	_state.position += 1.0f/6.0f * dt * (a.velocity + 2.0f*(b.velocity + c.velocity) + d.velocity);
	_state.momentum += 1.0f/6.0f * dt * (a.force + 2.0f*(b.force + c.force) + d.force);

	//state.orientation += (a.spin + (b.spin + c.spin)*2.0 + d.spin)*(1.0f/6.0f * dt);

	_state.angularMomentum += 1.0f/6.0f * dt * (a.torque + 2.0f*(b.torque + c.torque) + d.torque);

	_state.recalculate();
}	

Derivative evaluate(State _state, float t)
{
	Derivative output = new Derivative();
	output.velocity = _state.velocity;
	output.spin = _state.spin;
	forces(_state, t, ref output.force, ref output.torque);
	return output;
}

Derivative evaluate(State _state, float t, float dt, Derivative _derivative)
{
	_state.position += _derivative.velocity * dt;
	_state.momentum += _derivative.force * dt;
	//_state.orientation +=  _derivative.spin * dt;
	_state.angularMomentum += _derivative.torque * dt;
	_state.recalculate();
	
	Derivative output = new Derivative();
	output.velocity = _state.velocity;
	output.spin = _state.spin;
	forces(_state, t+dt, ref output.force, ref output.torque);
	return output;
}



void forces(State _state, float t, ref Vector2 force, ref Vector2 torque)
{
	// attract towards origin
	force.y = -((9.8f*2)*mass);// * _state.position.j;

	
	//TODO: Add wind force here. Should probably have a static phyics controller object.
	for(int i = 0; i < activeForces.Count; i++)
		force = force + activeForces[i];//*/
}
	//TODO: Make simple poly class so as this function can be useful.
	/*Vector2 collideSAT(polygon* _poly)
	{
		//Do interesting shaz here.
		std::vector<vec3f> temp = m_vertices;
		for(int i = 0; i < m_vertices.size(); i++)
			m_vertices[i] = m_vertices[i] + current.position;
			
		vec3f MTV = polygon::collideSAT(_poly);
	
		m_forces.push_back(MTV*200);//*1000
	
		current.position += MTV;
		m_vertices = temp;
		return MTV;
	}*/
	//TODO: Better implement this.
	/*
Vector2 collideSAT(rigidBody* _body)
{
	//Do interesting shaz here.
	for(int i = 0; i < m_vertices.size(); i++)
		m_vertices[i] = m_vertices[i] + current.position;

	for(int i = 0; i < _body->m_vertices.size(); i++)
		_body->m_vertices[i] = _body->m_vertices[i]+_body->getPos();

	vec3f MTV = polygon::collideSAT((polygon*)_body);
	

	for(int i = 0; i < m_vertices.size(); i++)
		m_vertices[i] = m_vertices[i] - current.position;

	for(int i = 0; i < _body->m_vertices.size(); i++)
		_body->m_vertices[i] = _body->m_vertices[i] - _body->getPos();

	m_forces.push_back(MTV*200);//*1000

	current.position += MTV*0.75f;
	return MTV;
}*/

///this function doesn't even get called...
	State interpolate(State a, State b, float alpha)
	{
		State state = b;
		state.position = a.position*(1-alpha) + b.position*alpha;
		state.momentum = a.momentum*(1-alpha) + b.momentum*alpha;
		//state.orientation = slerp(a.orientation, b.orientation, alpha);
		state.angularMomentum = a.angularMomentum*(1-alpha) + b.angularMomentum*alpha;
		state.recalculate();
		return state;
	}
	
	
	//I wrote this a couple years ago, looks pretty solid.
	//It basically just takes a list of verts which make objects clockwise.
	
	//Tests bodyA against bodyB and finds the MTV for A to depen B
	/*Vector2 CollideSAT(List<Vector2> bodyA, List<Vector2> bodyB)
	{
		List<Vector2> [] bodies = {bodyA, bodyB};
		Vector2 MTV;//Minimum Translation Vector. (to separate the bodied)
		float  MinOverlap = -99999;
		//this tests all of this poly's axes against the incoming poly and vice versa.
		for(uint i = 0; i < 2; i++)
		{
			for(uint ii = 0; ii < bodies[i].Count; ii++)
			{
				
				List<Vector2> body = bodies[i];
				///Create an edge direction vector. Then Find it's normal.
				Vector2 edgeDir =  (body[(ii+1)%bodies[i].Count]) - (body[ii]);
				edgeDir.Normalize(); //This might not be nessisary
				Vector2 normal = Vector2(-edgeDir.j, edgeDir.i); //This technically isn't a normal. It's just a perp line.
	
				//Find the projected shape's ranges on the normal.
				float [] max = new float[2];
				float [] min = new float[2];
				float [] diff = new float[2];
				
				for(uint iii = 0; iii < 2; iii++)
				{
					min[iii] = max[iii] = ((bodies[(i+iii)%2])[0].dot2(normal)); //2d dot product
					
					for(uint iv = 0; iv < (bodies[(i+iii)%2]).size(); iv++)
					{
						diff[iii] = ((*verts[(i+iii)%2])[iv].dot2(normal));//2d dot product
						
						if (diff[iii] < min[iii])
							min[iii] = diff[iii];
						
						else if(diff[iii] > max[iii]) 
							max[iii] = diff[iii];
					}
				}
				float d0 = min[0] - max[1]; //overlap 1
				float d1 = min[1] - max[0]; //overlap 2
				
				if(d0 > 0.0f || d1 > 0.0f) return false;
				else if(d0 > -abs(MinOverlap) || d1 > -abs(MinOverlap))
				{
					MinOverlap = (d0>d1?d0:-d1);
					MTV = -(normal*MinOverlap);
				}
			}
		}
		
		return MTV;
	}*/
}
