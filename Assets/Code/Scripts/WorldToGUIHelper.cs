using UnityEngine;
using System.Collections;
 
public class WorldToGUIHelper : MonoBehaviour {
 
public Transform target;  // Object that this label should follow
public Vector3 offset = Vector3.up;    // Units in world space to offset; 1 unit above object by default
public bool clampToScreen = false;  // If true, label will be visible even if object is off screen
public float clampBorderSize = 0.05f;  // How much viewport space to leave at the borders when a label is being clamped
public bool useMainCamera = true;   // Use the camera tagged MainCamera
public Camera cameraToUse ;   // Only use this if useMainCamera is false
Camera cam ;
Transform thisTransform;
Transform camTransform;
 
	void Start () 
    {
	    thisTransform = transform;
    if (useMainCamera)
        cam = Camera.main;
    else
        cam = cameraToUse;
    camTransform = cam.transform;
	}
 
 
    void LateUpdate()
    {
		if(guiTexture != null)
 			guiTexture.enabled = target.gameObject.activeSelf;
		if(guiText != null)
 			guiText.enabled = target.gameObject.activeSelf;
		
        if (clampToScreen)
        {
			Vector2 relClampBorderSize = new Vector2(clampBorderSize/Screen.width,clampBorderSize/Screen.height);
            Vector3 relativePosition = camTransform.InverseTransformPoint(target.position);
            relativePosition.z =  Mathf.Max(relativePosition.z, 1.0f);
            thisTransform.position = cam.WorldToViewportPoint(camTransform.TransformPoint(relativePosition + offset));
            thisTransform.position = new Vector3(Mathf.Clamp(thisTransform.position.x, relClampBorderSize.x, 1.0f - relClampBorderSize.x),
                                             Mathf.Clamp(thisTransform.position.y, relClampBorderSize.y, 1.0f - relClampBorderSize.y),
                                             thisTransform.position.z);
 
        }
        else
        {
            thisTransform.position = cam.WorldToViewportPoint(target.position + offset);
        }
    }
}