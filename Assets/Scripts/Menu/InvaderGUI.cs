using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ---------------------------------------------------------------------------------
// Class	:	InvaderGUI
// Desc		:	This is a class that manages the animation and state changes
//				of GUI elements on the main menu page (sliding text, walking
//				invaders etc. Specifically written for the main menu only.)
// ---------------------------------------------------------------------------------
public class InvaderGUI : MonoBehaviour
{
	// If the object has multiple frames assign them here
	public GameObject Frame1 			= null;
	public GameObject Frame2 			= null;

	// The points text that is enabled when the invaders
	// reach their position in the main menu animation. Displays
	// how many points the invader is worth.
	public GameObject PointsText		= null;

	// An optional target script that can be enabled when the gui element
	// is in its center position (used to enable the renderflasher.cs
	// script on the "Press Any Key" GUI element when it reaches center screen.
	public MonoBehaviour TargetScript	= null;

	// A delay before the GUI element starts moving and animating
	public float	  StartOffset		= 2;

	// Speed that the element moves
	public float      Speed				= 1.0f;

	// Speed that the frame1 and frame2 objects are toggled
	public float      FrameSpeed		= 0.25f;

	// Set to true if this should be the last GUI element in the animated
	// sequence
	public bool       SequenceTerminator= false;

	// Vectors describing the start, middle and ending positions for the animation
	// Think of these as position keyframes
	public Vector3 		_target = Vector3.zero;
	public Vector3      _start  = Vector3.zero;
	public Vector3		_end	= Vector3.zero;

	// The current position of the gui element
	private Vector3     _current= Vector3.zero;

	// Cached transform component
	private Transform	_myTransform;

	// Used to interpolate between keyframes
	private float       _interpolator 		= 0.0f;

	// Is the animation coroutine running? (Stops us running it again until its finished)
	private bool		_sequenceRunning	= false;

	// Used to track accumulated time
	private float       _timer				= 0.0f;
	private float       _oldTimer			= 0.0f;

	// should this gui element animate between frames
	private bool		_animate			= true;

	// ----------------------------------------------------------------------------
	// Name	:	Awake
	// Desc :	Cache transform and assign target (middle keyframe) position.
	//			This script assumes the object is positioned at its target
	//			position in the scene.
	// ----------------------------------------------------------------------------
	void Awake ()
	{
		_myTransform 	= transform;
		_target	 		= _myTransform.localPosition;
	}
 
	// ----------------------------------------------------------------------------
	// Name	:	OnEnable
	// Desc	:	Setup the animation
	// ----------------------------------------------------------------------------
	void OnEnable()
	{
		// Initially set all keyframe positions to the local space target position
		_start  		= _end = _target;

		// Offset the start and end keyframe 0.9 in each direction
		_start.x 		= _target.x + 0.9f;
		_end.x			= _target.x - 0.9f;

		// The the position of this object to the start position
		_myTransform.localPosition = _start;

		// If this element has a points text mesh associated with it 
		// disable it initially. This should only be active when the
		// element in center screen
		if (PointsText!=null) PointsText.SetActive (false);

		// Initialize Internals
		_timer = 0;
		_sequenceRunning = false;
		_oldTimer = 0;
		_animate = true;
		_interpolator = 0.0f;

		// If we have a target script reference (which should be activated only
		// when the element has reached its target position) then initially disable it
		if (TargetScript!=null) TargetScript.enabled = false;

		// Turn of this objects renderer initially
		if (GetComponent<Renderer>()) GetComponent<Renderer>().enabled = false;

	}

	// ----------------------------------------------------------------------------
	// Name	:	Update
	// Desc	:	Called each frame to manage this elements animation and state
	// ----------------------------------------------------------------------------
	void Update() 
	{
		// Increase time
		_timer+=Time.deltaTime;

		// If the sequence isn't running yet and the initial delay timer
		// has been reached, start the animated sequence as a coroutine.
		// The sequenceRunning boolean assures that we don't try to run
		// the coroutine again until it has completed
		if (!_sequenceRunning && _timer>StartOffset)
			StartCoroutine (AnimatedSequence ());

		// If we have frames that need to be animated
		if (Frame1!=null && Frame2!=null && (_animate || !Frame1.activeSelf))
		{
			// If the current time minus the the last frame change timer
			// is greater than our frame speed its time to toggle frames again
			if (_timer-_oldTimer>FrameSpeed)
			{
				// Store the current time so we don;t change frame again
				// until FrameSpeed seconds has elapsed
				_oldTimer = _timer;

				// Toggle the frames
				Frame1.SetActive (!Frame1.activeSelf);
				Frame2.SetActive (!Frame2.activeSelf);
			}
		}
	}

	// ---------------------------------------------------------------------------
	// Name	:	AnimatedSequence - Coroutine
	// Desc	:	The animation function of the element
	// ---------------------------------------------------------------------------
	private IEnumerator AnimatedSequence()
	{
		// Animation is running
		_sequenceRunning = true;

		// Reset position to start keyframe
		_myTransform.localPosition = _start;

		// Reset interpolator
		_interpolator = 0.0f;
		_animate = true;

		// Turn off all renderers
		if (Frame1 && Frame1.GetComponent<Renderer>()) Frame1.GetComponent<Renderer>().enabled=false;
		if (Frame2 && Frame2.GetComponent<Renderer>()) Frame2.GetComponent<Renderer>().enabled=false;
		if (GetComponent<Renderer>()) GetComponent<Renderer>().enabled = false;

		// and wait 1 second
		yield return new WaitForSeconds(1);

		// Enable renderers
		if (Frame1 && Frame1.GetComponent<Renderer>()) Frame1.GetComponent<Renderer>().enabled=true;
		if (Frame2 &&Frame2.GetComponent<Renderer>()) Frame2.GetComponent<Renderer>().enabled=true;
		if (GetComponent<Renderer>()) GetComponent<Renderer>().enabled = true;

		// Start interpolating between 0.0 and 1.0 using delta time * animation speed
		while( _interpolator<1.0f)
		{
			// Increase interpolator based on anim speed
			_interpolator+=Time.deltaTime * Speed;

			// Interpolate between start and target position to get the current position
			_current = Vector3.Lerp (_start, _target, _interpolator);

			// Set the position as our current position
			_myTransform.localPosition = _current;

			// yield
			yield return null;
		}

		// Stop animating because element has reached target (center screen)
		_animate = false;

		// Activate target script ( text flasher for example)
		if (TargetScript!=null) TargetScript.enabled = true;

		// Activate points text if available (points under invaders)
		if (PointsText!=null) PointsText.SetActive (true);

		// Display at target state for 6 seconds
		yield return new WaitForSeconds(6);

		// Disable target script
		if (TargetScript!=null) TargetScript.enabled = false;

		// Turn off points text
		if (PointsText!=null) PointsText.SetActive (false);

		// Start animating again
		_animate = true;

		// Reset Interpolator
		_interpolator = 0.0f;

		// Interpolate between target and ending position
		while( _interpolator<1.0f)
		{
			_interpolator+=Time.deltaTime * Speed;
			_current = Vector3.Lerp (_target, _end, _interpolator);
			_myTransform.localPosition = _current;
			yield return null;
		}

		// Disable all renderers as we are now at the end of the sequence
		if (Frame1 && Frame1.GetComponent<Renderer>()) Frame1.GetComponent<Renderer>().enabled=false;
		if (Frame2 && Frame2.GetComponent<Renderer>()) Frame2.GetComponent<Renderer>().enabled=false;
		if (GetComponent<Renderer>()) GetComponent<Renderer>().enabled = false;

		// If this is a SequenceTerminator element then instruct the scene manager
		// to display the next screen (the high score table)
		if (SequenceTerminator && SceneManager_MainMenu.instance)
		{
			// Sequence is no longer running
			_sequenceRunning = false;
			SceneManager_MainMenu.instance.NextScreen();
			
		}

		// Don't run this coroutine again for 20 seconds
		yield return new WaitForSeconds(20);

		// Allow to run again from Update
		_sequenceRunning = false;
	}

	// ------------------------------------------------------------------------------
	// Name	:	OnDisable
	// Desc	:	Reset sequence running boolean to false
	// ------------------------------------------------------------------------------
	void OnDisable()
	{
		_sequenceRunning = false;
	}
}
