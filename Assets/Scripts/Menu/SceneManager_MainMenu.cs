using UnityEngine;
using System.Collections;

// ---------------------------------------------------------------------------------
// Class	:	SceneManager_MainMenu
// Desc		:	Manages two game object hierarchies that are toggled on or off
//			    For example, Screen1 is the main title screen
//				Screen2 is the heirarchy containing the high score table
// ----------------------------------------------------------------------------------
public class SceneManager_MainMenu : MonoBehaviour
{
	// The two hierarchies managed by this object
	public GameObject 	Screen1		= null;
	public GameObject 	Screen2		= null;

	// Reference to the camera's CameraFade component (if available)
	private CameraFade  _cameraFade	= null;

	// Internals
	private float 		_timer		= 0.0f;
	private bool      	_quitting	= false;

	// This is a singleton
	private static SceneManager_MainMenu _instance = null;
	public static SceneManager_MainMenu instance
	{
		get{
				if (_instance==null)
					_instance = (SceneManager_MainMenu)FindObjectOfType(typeof(SceneManager_MainMenu)) ;

				return _instance;
			}
	}

	// -----------------------------------------------------------------------------
	// Name	:	Start
	// Desc	:	By default, enable hierarchy Screen1 and disable Screen2. The they
	//			to cache a reference to the camera's CameraFade component so we can 
	//			invoke screen faded when going either into the main game or when
	//			exiting to the end credits scene.
	// -----------------------------------------------------------------------------
	void Start ()
	{
		if (Screen1) Screen1.SetActive(true);
		if (Screen2) Screen2.SetActive(false);

		_cameraFade = FindObjectOfType<CameraFade>();
		if (_cameraFade)
			_cameraFade.FadeIn (1.5f);

		Cursor.visible = false;	
	}

	// -----------------------------------------------------------------------------
	// Name	:	NextScreen
	// Desc	:	This is called by external objects to instruct the scene manager
	//			to switch between screens. For example, when Screen1 has completed
	//			its invader animations, it tells this script to activate the highscore
	//			screen (and vice versa)
	// -----------------------------------------------------------------------------
	public void NextScreen()
	{
		if (Screen1) Screen1.SetActive (!Screen1.activeSelf);
		if (Screen2) Screen2.SetActive (!Screen2.activeSelf);
	}



	// ---------------------------------------------------------------------------------------
	// Name	:	Update
	// Desc	:	Polls for key presses and either starts a new game or quits.
	// ---------------------------------------------------------------------------------------
	void Update()
	{
		// If the user has already hit ESC then we are already quitting
		// and waiting for the camera to fade out so do nothing.
		if (_quitting) return;

		// Increase internal timer
		_timer+=Time.deltaTime;

		// If this scene has just started (less than one second) then make sure
		// we clear out any buffered input. The player may be back here after a
		// frantic slapping of the keyboard in-game :)
		if (_timer<1.0f) 	Input.ResetInputAxes();

		// If the ESC key is pressed begin the quit sequence
		if (Input.GetKeyDown ( KeyCode.Escape))
		{
			// We are now in the quit sequence
			_quitting = true;

			// Instruct the game manager to fade out music over a 2 second duration
			if (GameManager.instance) 	GameManager.instance.StopMusic ( 2 );

			// Instruct the camera to fade to black over a 2 second duration
			if (_cameraFade)			_cameraFade.FadeOut (2.0f);

			// Invoke this function in 2.2 seconds which loads the closing credits
			// scene (we wait this long so screen and music are fully faded out)
			Invoke ("QuitMenu", 2.2f);
		}
		else
		if (Input.anyKeyDown)
		{
			if (GameManager.instance) GameManager.instance.StartNewGame();
		}
	}

	// ------------------------------------------------------------------------------------
	// Name	:	QuitMenu
	// Desc	:	Instructs the Game Manager to load the closing credits scene.
	// ------------------------------------------------------------------------------------
	void QuitMenu()
	{
			if (GameManager.instance) GameManager.instance.QuitGame();
	}
}
