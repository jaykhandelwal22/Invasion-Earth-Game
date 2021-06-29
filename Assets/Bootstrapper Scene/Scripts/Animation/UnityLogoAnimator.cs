// -------------------------------------------------------------------------------------------
// FILE	:	UnityLogoAnimator
// DESC	:	Simple script that performs the animation and fade in fade out of the
//			animated Unity Cube object in the Bootstrapper scene.
// NOTE	:	This has very little re-use value.
// -------------------------------------------------------------------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// --------------------------------------------------------------------------------------------
// Name	:	UnityLogoAnimator
// Desc	:	Animate a transform from a source to target waypoint over time using an animation
//			curve. It also maintains a list of other materials that will be fades in/out.
//			Uses a simple technique of a blocking object (cube) close to the camera that is
//			faded in (black) and out so the objects behind it fade in/out of view.
// Note	:	This class is VERY specific to exactly the scene I was creating so has little 
//			as-is reuse value. However, you can still see alternative methods of doing a
//			fadein/fadeout using an obstructing object.
// --------------------------------------------------------------------------------------------
public class UnityLogoAnimator : MonoBehaviour 
{
	
	public Transform 		UnityCube		=	null;		// Transform of the object to be animated
	public Transform 		DestPos 	 	=  	null;		// The target waypoint for the object
	public Transform 		SrcPos		 	=  	null;		// The source waypoint for the object
	public AnimationCurve	Curve;							// Animation curve to use for waypoint lerp
	public float	 		Interpolator	=  	-3.0f;		// Interpolator describing the current lerp position
	public float	 		Speed       	=	0.0f;		// Speed of the animation
	
	public List<Material>	TextMaterials   		= new List<Material>();	// A list of materials to fade in (used for bootstapper text)
	public Material         FadeOutMaterial			= null;					// Material of the obstruction object that will be used for fade out
	public Material         UnityLogo				= null;					// Material of the Unity Cube object's material
	public Material         GameInstitutePresents 	= null;					// Material of the Game Institute Presents object

	// ----------------------------------------------------------------------------------------
	// Name	:	Start
	// Desc	:	Called once prior to the first update.
	// ----------------------------------------------------------------------------------------
	void Start () 
	{
		// Set the Unity Cube object to its starting position
		UnityCube.position = SrcPos.position;
		UnityCube.rotation = SrcPos.rotation;
		
		// Make sure the obstructing object (our fade out cube) is initially set to
		// totally transparent so it isn't blocking anything from view.
		FadeOutMaterial.SetColor("_Color", new Color(0,0,0,0));
		
		// Set the unity cube object to also be initially transparent
		UnityLogo.SetColor("_Color", new Color(1,1,1,0));
		
		// Set all our text object to also be transparent to begin with
		foreach( Material mat in TextMaterials)
		{
			mat.SetColor("_Color", new Color(1.0f, 1.0f, 1.0f, 0.0f));	
		}
		
		// Begin the sequence of fading in the spinning cube (3 seconds)
		StartCoroutine(BeginFadeIn());
		
		// Start the coroutine that fades in Game Institute Presents, and then fades it back out
		// after a second
		StartCoroutine(ShowGameInstitutePresents());
	}
	
	// ---------------------------------------------------------------------------------------
	// Name	:	Update
	// Desc	:	Update the interpolator that animates the Unity CUbe object from its starting
	//			waypoint to its destination waypoint. Then remap the interpolator using our
	//			animation curve to get the final t value with which to lerp between the
	//			source and target waypoints to get the current position of the object,
	// ---------------------------------------------------------------------------------------
	void Update () 
	{
		// If the cube animation isn't over yet
		if (Interpolator<1.0f)
		{
			// Update the interpolator based on the Inspector set speed
			Interpolator+=Speed * Time.deltaTime;
			
			// If the cube has reached its destination position then the animation
			// of the cube is over and its time to fade in all the text object around it.
			if (Interpolator>=1.0f) StartCoroutine( FadeInText() );
		}
		
		// Calculate the position and rotation of the camera from the current interpolator value. This is fed into the
		// Animation Curve and the result used as the t value for the lerp.
		UnityCube.position = Vector3.Lerp		( SrcPos.position, DestPos.position, Curve.Evaluate(Mathf.Clamp01(Interpolator)) );
		UnityCube.rotation = Quaternion.Slerp	( SrcPos.rotation, DestPos.rotation, Curve.Evaluate(Mathf.Clamp01(Interpolator)) );
	}
	
	// ---------------------------------------------------------------------------------------
	// Name	:	FadeInText - Coroutine
	// Desc	:	Fade in the text over 5 seconds. Display for 1 second and then begin the
	//			fade out.
	// ---------------------------------------------------------------------------------------
	private IEnumerator FadeInText()
	{
		// Set timer to zero
		float timer = 0.0f;
		
		// Keep on going until timer reached 5 second
		while (timer<5.0f)
		{
			// Update timer with seconds passed
			timer+=Time.deltaTime;
			
			// Loop through each of the text materials that need to be faded in
			foreach( Material mat in TextMaterials)
			{
				// Update the alpha component of their material color based on
				// how close to 5.0 the timer is. At 5 seconds all text materials
				// will be fully opaque
				mat.SetColor("_Color", new Color(1.0f, 1.0f, 1.0f, timer/5.0f));	
			}
			
			// Yield until next update
			yield return null;
		}
		
		// The five second timer has been reached so let us no reset the timer to zero
		// and do nothing for 1 second, simply displaying the text
		timer = 0.0f;
		while (timer<1.0f)
		{
			timer+=Time.deltaTime;
			yield return null;
		}
		
		// Text has been displayed for 1 second so lets begin fading out the scene
		StartCoroutine( BeginFadeOut() );
	}
	
	
	// ---------------------------------------------------------------------------------------
	// Name	:	BeginFadeOut - Coroutine
	// Desc	:	Begind fading out all the objects in the scene as this scene is about to
	//			transition.
	// ---------------------------------------------------------------------------------------
	private IEnumerator BeginFadeOut()
	{
		if (GameManager.instance)
		{
			GameManager.instance.PlayMusic(0, 6.0f);
		}

		// Reset timer
		float timer = 0.0f;
		
		// Two second fade out
		while (timer<2.0f)
		{
			// Update timer
			timer+=Time.deltaTime;
			
			// Start to make the obstructing object opaque over a period of two second.
			// The big black cube will fade in. Because this cube has been placed in
			// front of the camera it will obscure everything behind it (creating a fade out)
			FadeOutMaterial.SetColor("_Color", new Color(0,0,0,timer/2.0f));
			
			// Yield until next update
			yield return null;
		}

		UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
	}
	
	// ---------------------------------------------------------------------------------------
	// Name	:	BeginFadeIn - Coroutine
	// Desc	:	Called from Start to begin fading in the spinning unity cube's material at
	//			scene startup.
	// ---------------------------------------------------------------------------------------
	private IEnumerator BeginFadeIn()
	{
		// Start timer coutning from zero
		float timer = 0.0f;
		
		// This will be a 3 second fade in
		while (timer<3.0f)
		{
			// Update the timer with seconds passed
			timer+=Time.deltaTime;
			
			// Update the alpha channel of the cube's color so that it
			// becomes more and more opaque as we reach 3 seconds
			UnityLogo.SetColor("_Color", new Color(1,1,1,(timer/3.0f)));
			
			// Yield until next update
			yield return null;
		}
			
	}
	
	// ---------------------------------------------------------------------------------------
	// Name	:	ShowGameInstitutePresents - Coroutine
	// Desc	:	Called at startup to fade in the Game Institute Presents object, hold it there
	//			for a small amount of time and then fade it out.
	// ---------------------------------------------------------------------------------------
	private IEnumerator ShowGameInstitutePresents()
	{	
		// Reset Timer
		float timer = 0.0f;
		
		// 3 second fade in
		while (timer<3.0f)
		{
			// Update Timer
			timer+=Time.deltaTime;
			
			// Make the 'Game Institute Presents' object more opaque as we approach 3 seconds
			GameInstitutePresents.SetColor("_Color", new Color(1,1,1,(timer/3.0f)));
			
			// Yield control
			yield return null;
		}
		
		// Reset Time
		timer=0.0f;
		
		// Display at full opaqueness for 1,0 second
		while(timer<1.0f)
		{
			// Update timer
			timer+=Time.deltaTime;
			
			// Yield control
			yield return null;
		}
		
		// Reset Timer
		timer = 0.0f;
		
		// Fade out text over a 3.5 second period
		while (timer<3.5f)
		{
			// Update timer
			timer+=Time.deltaTime;
			
			// Make material more transparent as we apporach our target time (3.5)
			GameInstitutePresents.SetColor("_Color", new Color(1,1,1,1.0f-(timer/3.5f)));
			
			// Yield control
			yield return null;
		}
			
	}
	
	// ---------------------------------------------------------------------------------------
	// Name	:	OnGUI
	// Desc	:	Called by unity whenever the GUI is about to be redrawn or processed. We simply
	//			inform unity to hide the mouse cursor during this scene.
	// ---------------------------------------------------------------------------------------
	void OnGUI()
	{
		Cursor.visible = false;	
	}
}
