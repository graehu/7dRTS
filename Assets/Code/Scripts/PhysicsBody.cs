using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PhysicsBody : MonoBehaviour {
	
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
	
	State previous = new State();
    State current = new State();
	public Vector2 velo = Vector2.zero;
	List<Vector2> activeForces = new List<Vector2>();
	
	public void ApplyForce(Vector2 force)
	{
		//current.momentum = current.mass*force;
		//previous.momentum = force;
		activeForces.Add(force);
		current.momentum = force;
	}
	float t = 0f;

	// Use this for initialization
	void Awake ()
	{
		current.size = 1;
		current.mass = 1;
		current.inverseMass = 1.0f / current.mass;
		current.position = transform.position;
		current.momentum = Vector2.zero;
		current.angularMomentum = Vector2.zero;
		current.inertiaTensor = current.mass * current.size * current.size * 1.0f / 6.0f;
		current.inverseInertiaTensor = 1.0f / current.inertiaTensor;
		current.recalculate();
		previous = current;
	}
	// Update is called once per frame
	void Update ()
	{
		t += Time.deltaTime;
		previous = current;
		integrate(current, t, Time.deltaTime);
		transform.position = new Vector3(current.position.x, current.position.y, transform.position.z);
		//m_forces.clear();
		//if physics are 2d. do this:
		current.orientation.x = 0;
		current.orientation.y = 0;
	}
		
	class State
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
	        velocity = momentum * inverseMass;
	        angularVelocity = angularMomentum * inverseInertiaTensor;
	        //orientation.normalise();
	        //spin = (quaternion(0, angularVelocity.i, angularVelocity.j, angularVelocity.k) * orientation)*0.5;
	    }
	};



	class Derivative
	{
		public Vector2 velocity;                ///< velocity is the derivative of position.
		public Vector2 force;                  	///< force in the derivative of momentum.
		public Quaternion spin = new Quaternion(); ///< spin is the derivative of the orientation quaternion.
		public Vector2 torque;                 	///< torque is the derivative of angular momentum.
	};
	
	


void integrate(State _state, float t, float dt)
{
	Derivative a = evaluate(_state, t);
	Derivative b = evaluate(_state, t, dt*0.5f, a);
	Derivative c = evaluate(_state, t, dt*0.5f, b);
	Derivative d = evaluate(_state, t, dt, c);
	
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
	force.y = -(9.8f/2);// * _state.position.j;

	for(int i = 0; i < activeForces.Count; i++)
		force = force + activeForces[i];//*/



	// sine force to add some randomness to the motion
	
	//force.i += 10;// * sin(t*0.9f + 0.5f);
	//force.j += 11;// * sin(t*0.5f + 0.4f);
	//force.k += 12;// * sin(t*0.7f + 0.9f);

	// sine torque to get some spinning action

	//torque.i = 1.0f;// * sin(t*0.9f + 0.5f);
	//torque.j = 1.1f;// * sin(t*0.5f + 0.4f);
	//torque.k = 1.2f;// * sin(t*0.7f + 0.9f);

	// damping torque so we dont spin too fast

	//torque -= 0.2f * _state.angularVelocity;
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
}
