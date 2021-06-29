using UnityEngine;
using System.Collections;

// --------------------------------------------------------------------------------------------
// Class :  CameraFade
// Desc	 :  Hooks into the OnPostRender event to render a quad over the screen for 
//		    fade-in/fade outs. 
// Use	 : MUST be attached to a game object with a camera component. 
// --------------------------------------------------------------------------------------------
public class CameraFade : MonoBehaviour 
{
	// The material that we will use to draw our transparent screen quad
	private Material 			FadeMaterial 	= null;

	// Current Fade Level (1 = totally faded)
	private float   			_currentFade	= 1.0f;

	// ----------------------------------------------------------------------------------------
	// Name	:  FadeIn
	// Desc	:  Public function which other components can call to initiate the screen fade-in.
	//		   Simply wraps a call to a coroutine that does the actual work.Take as a 
	//		   parameter the duration of the fade in seconds.
	// ----------------------------------------------------------------------------------------
	public void FadeIn( float duration = 1.0f )
	{
		// Perform the fade via a coroutine
		StartCoroutine ( DoFadeIn( duration ) );
	}

	// ----------------------------------------------------------------------------------------
	// Name	:	DoFadeIn - Coroutine
	// Desc	:	The coroutine that actually performs the fade-in over the passed duration.
	// ----------------------------------------------------------------------------------------
	private IEnumerator DoFadeIn( float duration )
	{
		// Start a timer at zero
		float timer = 0.0f;

		// Keep looping while the time is less that the fade time
		while (timer<duration && duration>0.01f)
		{
			// Increment the timer
			timer+=Time.deltaTime;

			// Set the fade value between 1 and 0 based on timer/duration factor
			_currentFade = 1- (timer/duration);

			// Yield control with each iteration
			yield return null;
		}

		// We are outside the loop so the fade must be complete. Let's just make
		// sure the current fade is now clamped to 0.
		_currentFade = 0.0f;
	}

	// ----------------------------------------------------------------------------------------
	// Name	:	FadeOut
	// Desc	:	Public function that other components can call to initiate a screen fade-out.
	//			Is passed the number of second over which the fade should complete.
	//-----------------------------------------------------------------------------------------
	public void FadeOut( float duration = 1.0f, float delay = 0.0f )
	{
		StartCoroutine ( DoFadeOut( duration, delay ) );
	}

	// ----------------------------------------------------------------------------------------
	// Name	:	DoFadeOut - Coroutine
	// Desc	:	The coroutine that actually performs the fade-out over the passed duration.
	//			Takes a second parameter that allows you to specify an initial delay that
	//			the function should wait before beginning the fade-out.
	// ----------------------------------------------------------------------------------------
	private IEnumerator DoFadeOut( float duration, float delay )
	{
		// Wait for the initial delay time
		yield return new WaitForSeconds(delay);

		// Set timer to zero
		float timer = 0.0f;

		// Loop while the timer is less than the requested duration
		while (timer<duration && duration>0.01f)
		{
			// Increment the timer
			timer+=Time.deltaTime;

			// Set the current fade using the timer/duration factor
			_currentFade = timer/duration;

			// Remember to yield control back each iteration
			yield return null;
		}

		// Fade is complete so set fade value to 1.0f
		_currentFade = 1.0f;
	}

	// ----------------------------------------------------------------------------------------
	// Name	:	OnPostRender
	// Desc	:	Called after the camera attached to this game object has rendered the scene.
	//			We use this to draw a transparent quad over the final image that is rendered.
	// ----------------------------------------------------------------------------------------
	void OnPostRender() 
	{

			
			// If we have not created our material.
			if(!FadeMaterial) 
			 {
			     FadeMaterial = new Material(Shader.Find ("Game Institute/Camera Fade"));
			 }
			
			// Set the color to black and the alpha value to the current fade intensity.
			FadeMaterial.SetColor("_Color", new Color(0.0f,0.0f,0.0f, _currentFade) );
			
			// Draw a quad over the whole screen with the above shader using the GL class.
			// First save the state of the cameras view/projection matrix so we can restore it
			// before we leave
			GL.PushMatrix ();
			
			// Create an Orthographic projection matrix
			GL.LoadOrtho ();
			
			// For each pass in the material (only one in ours)
			for (var i = 0; i < FadeMaterial.passCount; ++i) 
			{
				// Set the material for the current pass
				FadeMaterial.SetPass (i);
				
				// We want to draw a quad so interpret the next
				// four vertex3 statements as the corners of this quad
				GL.Begin( GL.QUADS );
				
				// The quad corner points
				GL.Vertex3( 0, 0, 0.1f );
				GL.Vertex3( 1, 0, 0.1f );
				GL.Vertex3( 1, 1, 0.1f );
				GL.Vertex3( 0, 1, 0.1f );
				
				// We are done rendering quads
				GL.End();
			}
			
			// Restore the camera's view/projection matrix
			GL.PopMatrix (); 
	}
}

