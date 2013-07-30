using UnityEngine;
using System.Collections.Generic;

public class CustomAnimations : MonoBehaviour
{
	
	#region public types
	
	[System.Serializable]
	public class Animation
	{
		//params
		public string name = string.Empty;
		public bool onAwake = true;
		public bool isAdditive = true;
		public AnimationType type = AnimationType.None;
		public float length = 0.0f;
		public Easing.Method easingMethod = Easing.Method.None;
		public WrapMode wrapMode = WrapMode.Default;
		
		//values
		public Vector4 lastValue = Vector4.zero;
		public Vector4 startValue = Vector4.zero;
		public Vector4 endValue = Vector4.one;
		
		//states
		public bool isPlaying = false;
		public float tick = 0;
	}
	public enum AnimationType
	{
		None,
		ColorFade,
		Translation,
		Rotation,
		Scale
	}
	
	#endregion
	
	#region public variables
	
	public List<Animation> animations = new List<Animation>();
	
	#endregion
	
	#region public methods
	
	public void Play(string _name)
	{
		Animation anim = animations.Find(a => a.name == _name);
		anim.isPlaying = true;
	}
	
	public void Stop(string _name)
	{
		Animation anim = animations.Find(a => a.name == _name);
		anim.isPlaying = false;
	}
	
	public Animation Find(string _name)
	{
		return animations.Find(a => a.name == _name);
	}
	
	#endregion
	
	#region MonoBehaviour methods
	
	void Awake()
	{
		foreach(Animation anim in animations)
		{
			if(anim.onAwake)
				anim.isPlaying = true;
			
			switch(anim.type)
			{

			case AnimationType.ColorFade:
				if(renderer == null)
					Debug.LogError("No renderer found; required for colorFade animation");
				break;
			}
		}
	}
	
	void Update()
	{
		foreach(Animation anim in animations)
		{
			if(anim.isPlaying)
			{
				if(anim.type == AnimationType.None || anim.length == 0)
					continue;
				
				anim.tick += Time.deltaTime;
				
				float t = Easing.Wrap(anim.tick, anim.length, anim.wrapMode);
				Vector4 v = Easing.Ease(t, anim.startValue, anim.endValue, anim.length, anim.easingMethod);
				
				switch(anim.type)
				{
				case AnimationType.ColorFade:
					if(renderer == null) continue;
					if(anim.isAdditive)
					{
						renderer.material.color -= (Color) anim.lastValue;
						renderer.material.color += (Color) v;
					}
					else
						renderer.material.color = v;
					break;
				case AnimationType.Translation:
					if(anim.isAdditive)
					{
						transform.localPosition -= (Vector3) anim.lastValue;
						transform.localPosition += (Vector3) v;
					}
					else
						transform.localPosition = v;
					break;
				case AnimationType.Rotation:
					if(anim.isAdditive)
					{
						transform.localEulerAngles -= (Vector3) anim.lastValue;
						transform.localEulerAngles += (Vector3) v;
					}
					else
						transform.localEulerAngles = v;
					break;
				case AnimationType.Scale:
					if(anim.isAdditive)
					{
						transform.localScale -= (Vector3) anim.lastValue;
						transform.localScale += (Vector3) v;
					}
					else
						transform.localScale = v;
					break;
				}
				
				anim.lastValue = v;
			}
		}
	}
	
	#endregion
}
