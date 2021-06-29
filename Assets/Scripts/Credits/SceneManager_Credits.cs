using UnityEngine;
using System.Collections;

// -------------------------------------------------------------------------
// Class	:	SceneManager_Credits
// Desc		:	Manages the closing credits of my Invaders game
// -------------------------------------------------------------------------
public class SceneManager_Credits : MonoBehaviour
{
	public GameObject 				  StarField		 = null;	// Game Object (Particle System) that will be turned on at the of the sequence
	private GI_CameraWaypointAnimator _cameraAnimator = null;	// Reference to a camera animator component in the scene (attached to camera)
	private CameraFade 				  _cameraFade	 = null;	// Reference to a camera fade component in the scene (attached to Camera)
	private bool					  _quitting		 = false;	// Records whether we are in the processing of fading out and quitting the crefits sequence.
	private GameManager				  _gameManager	 = null;	// Reference ot the Game Manager

	// ---------------------------------------------------------------------
	// Name	:	Start
	// Desc	:	Cache/Find components, start music playing and begin initial
	//			fade-in of the credits sequence
	// ---------------------------------------------------------------------
	void Start () 
	{
		// Cache references
		_gameManager 		= GameManager.instance;
		_cameraAnimator 	= FindObjectOfType<GI_CameraWaypointAnimator>();
		_cameraFade 		= FindObjectOfType<CameraFade>();

		// If we have a camera animator in the scene then register a callback
		// so we are notified when the last waypoint is reached by the camera
		if (_cameraAnimator)
		{
			_cameraAnimator.RegisterCameraWaypointChangeCallback( CameraCallback );
		}

		// Tell the game manager to start playing the final credits music (track 3 - Hardcoded oops ) 
		if (_gameManager) _gameManager.PlayMusic ( 3, 1);

		// If we have a camera fade component in the scene then tell it to fade in ovcer 2.5 seconds
		if (_cameraFade) _cameraFade.FadeIn (2.5f);
	}

	// ---------------------------------------------------------------------------------------
	// Name	:	OnGUI
	// Desc	:	Called by unity whenever the GUI is about to be redrawn or processed. We simply
	//			inform unity to hide the mouse cursor during this scene.
	// ---------------------------------------------------------------------------------------
	void OnGUI()
	{
		// Make sure mouse remains disabled during the sequence
		Cursor.visible = false;	
	}

	// ---------------------------------------------------------------------------------------
	// Name	:	CameraCallback
	// Desc	:	Called by the camera's waypoint animator component everytime it starts heading
	//			towards a new waypoint.
	// ---------------------------------------------------------------------------------------
	public void CameraCallback ( int wayPoint )
	{
		// If we are heading towards the last waypoint, lets begin wrapping things up
		// for a smooth aplpication exit.
		if (wayPoint == _cameraAnimator.CameraWaypoints.Count-1)
		{
			// Turn on the starfield
			if (StarField) StarField.SetActive (true);

			// If we are not already in the process of quitting
			if (!_quitting)
			{
				// We are now
				_quitting = true;

				// Fade the camera out after 6 seconds delay and do a one second fade
				if (_cameraFade)  _cameraFade.FadeOut (1.0f, 6.0f);

				// Fade the music out of 7 seconds
				if (_gameManager) _gameManager.StopMusic (7.0f);

				// In 7 seconds - when fade of music and camera is complete - call
				// the ExitApp functions
				Invoke ( "ExitApp" , 7.5f);			
			}
		}
	}

	// --------------------------------------------------------------------------------------
	// Name : Update
	// Desc	: Update is called once per frame by Unity. Test for any key or button being
	//		  beind pressed. If so, terminate the credits early by initiating the fade out
	//		  and shutdown of the game
	// -------------------------------------------------------------------------------------
	void Update ()
	{
		if (Input.anyKeyDown && !_quitting)
		{
			_quitting = true;
			if (_cameraFade)  _cameraFade.FadeOut (2.0f, 0.0f);
			if (_gameManager) _gameManager.StopMusic (2.0f);
			Invoke ( "ExitApp" , 2.5f);			
		}
	}

	// ------------------------------------------------------------------------------------
	// Name	:	ExitApp
	// Desc	:	Stops all coroutines (paranoia check) and exits the application
	// ------------------------------------------------------------------------------------
	void ExitApp()
	{
		StopAllCoroutines();
		Application.Quit ();
	}
}
