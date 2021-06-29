using UnityEngine;
using System.Collections;

// ------------------------------------------------------------------------------
// Class : Invader
// Desc	 : This script is attached to every regular invader in the wave.
// ------------------------------------------------------------------------------
public class Invader : MonoBehaviour 
{
	// Inspector Assigned
	public GameObject Frame1 = null;		// Frame 1 game object
	public GameObject Frame2 = null;		// Frame 2 game object
	public int Points        = 20;			// Points to award player on death

	// Not inspector assigned by accessible
	// to other objects / components
	public int Column		 = 0;					// The column in the wave this Invader has been assigned to 
	public bool Active		 = true;				// Is this invader currently active (not disabled through death)

	// Property
	public  float 		 altitude { get { return _myTransform.position.y;}} // What is Y component of this invaders position

	// Cached component references
	private SceneManager _mySceneManager = null;	
	private Transform    _myTransform	 = null;

	// ---------------------------------------------------------------------------
	// Name	:	Start
	// Desc	:	Setup the invader whe it is initially instantiated.
	// ---------------------------------------------------------------------------
	void Start () 
	{
		// Turn on the invader at level startup
		gameObject.SetActive(true);
		Active = true;

		// Activate Frame 1 and De-Activate Frame 2
		if (Frame1!=null) Frame1.SetActive(true);
		if (Frame2!=null) Frame2.SetActive (false);

		// Get scene manager singleton
		_mySceneManager = SceneManager.instance;
		_myTransform    = transform;

		//InvokeRepeating ("UpdateFrame",0,0.55f);
	}
	

	// --------------------------------------------------------------------------
	// Name	:	UpdateFrame
	// Desc	:	This is called by the scene manager when the Invaders have been
	//			moved and they should animated between their two frames. It 
	//			simply toggles the enabled state of the two game objects.
	// --------------------------------------------------------------------------
	public void UpdateFrame()
	{
		if (Frame1!=null) Frame1.SetActive(!Frame1.activeSelf);
		if (Frame2!=null) Frame2.SetActive (!Frame2.activeSelf);
	}

	// --------------------------------------------------------------------------
	// Name	:	OnDisable
	// Desc	:	Notifies the scene manager that an invader in the column
	// 			has been destroyed so it can adjust wave movement extents.
	// --------------------------------------------------------------------------
	void OnDisable()
	{
		// The invader is being turned off by the player's projectile so let the 
		// scene manager know an invader has died so it can :-
		// 1.) Award Points to player
		// 2.) Emit particles at invader position
		// 3.) Decrease invader count
		// 4.) Update column counts and recalculate wave extents
		// ---------------------------------------------------------------------
		if (_mySceneManager!=null)
		{
			_mySceneManager.RegisterHit( Column , Points, _myTransform.position );
		}

		Active = false;
	}
}
