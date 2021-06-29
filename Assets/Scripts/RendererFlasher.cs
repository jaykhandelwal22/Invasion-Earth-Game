using UnityEngine;
using System.Collections;

// -----------------------------------------------------------------------
// Class : RendererFlasher
// Desc	 : Simple class used to flash a 3D game object with a renderer
//		   attached at a specified time interval.
// 		   It simply turns on and off the object's renderer
// -----------------------------------------------------------------------
public class RendererFlasher : MonoBehaviour 
{
	// Inspector assigned flicker rate (seconds between changed)
	public  float 		flickerRate = 0.5f;

	// Cached renderer component
	private Renderer 	_myRenderer = null;

	// Internal timer
	private float		_timer		= 0.0f;
	
	// ----------------------------------------------------------------
	// Name	:	OnEnable
	// Desc	:	Called by unity when the object to which this script is
	//			attached is enabled.
	// ----------------------------------------------------------------
	void OnEnable () 
	{
		// Cache renderer component
		_myRenderer = GetComponent<Renderer>();

		// Reset timer
		_timer		= 0.0f;
	}

	// ----------------------------------------------------------------
	// Name	:	OnDisable
	// Desc	:	Called by unity when the object to which this script is
	//			attached is disabled.
	// ----------------------------------------------------------------
	void OnDisable()
	{
		// If we have a renderer enable it before the game object is
		// disabled so it is on by default the next time it is enabled.
		if (_myRenderer!=null)
			_myRenderer.enabled = true;
	}

	// ---------------------------------------------------------------
	// Name	:	Update
	// Desc	:	Called every frame by Unity. Toggles the state of the
	//			renderer component when the specified time has elapsed.
	// ---------------------------------------------------------------
	void Update ()
	{
		// Increment the internal timer
		_timer+=Time.deltaTime;

		// Is it greater than our flicker timer
		if (_timer>flickerRate)
		{
			// Reset Timer
			_timer=0.0f;

			// Toggle renderer State
			if (_myRenderer!=null)
				_myRenderer.enabled = !_myRenderer.enabled;
			
		}
	}
}
