// -----------------------------------------------------------------------
// Class : CameraAnimator
// Desc  : Is added to the main menu camera and rotates it contantly so we
//		   get a nice spinning skybox.
// -----------------------------------------------------------------------
using UnityEngine;
using System.Collections;

public class CameraAnimator : MonoBehaviour
{
	// Component Cache
	private Transform _myTransform  = null;

	// The rotation deltas we would like to rotate our camera
	// (per second) about its 3 axis.
	public Vector3   _eulers		= new Vector3( 1.0f, 0.0f, 5.0f);

	// -------------------------------------------------------------------
	// Name	:	Start
	// Desc	:	Called just prior to the first render of the scene. Is used
	//			to cache the Transform component of whatever object this
	//			script is attached to. 
	// --------------------------------------------------------------------
	void Start()
	{
		// Cache transform componet
		_myTransform = transform;
	}

	// --------------------------------------------------------------------
	// Name	:	Update
	// Desc	:	Called every frame to rotate the object to which
	//			this script is attached.
	// ---------------------------------------------------------------------
	void Update () 
	{	
		// Apply the rotation to the transform
		_myTransform.Rotate(_eulers * Time.deltaTime);
	}
}
