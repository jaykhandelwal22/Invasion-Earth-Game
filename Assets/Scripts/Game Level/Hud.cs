using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// ----------------------------------------------------------------------------------
// Class	:	Hud
// Desc		:	This script manages HUD elements. This is not really
//				a proper Hud class but really just a script that ties together the
//				controlling of non-game elements. Such as, score, lives and the
//				get ready elements that are displayed between levels and at game over.
// ----------------------------------------------------------------------------------
public class Hud : MonoBehaviour 
{
	// Inspector Assigned
	public TextMesh 			HiScore;					//	Highscore text mesh
	public TextMesh 			CurrentScore;				//	Current score text mesh	
	public GameObject 			GetReadyPanel;				//	Black cube that creates 
															//  a background for GetReady 
															//  panel.
	public TextMesh 			GetReadyPanel_Line1;		//  Text mesh of line one of GetReady panel
	public TextMesh 			GetReadyPanel_Line2;		//	Text mesh of line two of GetReady panel
	public GameObject			LivesPrefab 	= null;		//  Prefab that will be displayed at
															//  bottom of screen for each life left.
	public Transform			LivesPosition	= null;		//  Position that the extra lives objects should be placed

	// Internals
	private Material 			GetReadyPanel_Mat;			//  Material of panel cube for timed fade in/out

	// A list containing the instances of the extra lives prefab (one in this list for each life left)
	private List<GameObject>   	_LivesInstances= new List<GameObject>();

	// Cached component references
	private GameManager		   	_gameManager = null;
	private GameObject _myGameObject;

	// Name and score of the person currently at top of table
	private string 	_highScoreName  = null;
	private uint	_highScore		=0;

	// ----------------------------------------------------------------------------
	// Name	:	Start
	// Desc	:	Called by Unity at scene startup to configure the HUD component.
	// ----------------------------------------------------------------------------
	void Start () 
	{
		// Cache Game Object and Game Manager
		_myGameObject = gameObject;
		_gameManager  = GameManager.instance;

		// Cache the Get Ready Black Cube Material so later we
		// can fade in/out by altering alpha of material color
		if ( GetReadyPanel!=null &&  GetReadyPanel.GetComponent<Renderer>()!=null)
		{
			GetReadyPanel_Mat = GetReadyPanel.GetComponent<Renderer>().material;
		}

		// Instantiate a maximum of 10 lives (can be what you want) in case
		// we wish to grant extra lives throughout the level
		// TODO: Add the extra life feature
		for(int i=0; i<10; i++)
		{
			// Instantiate prefab
			GameObject go = Instantiate ( LivesPrefab ) as GameObject;

			// Parent it to our position transform (inspector assigned)
			go.transform.parent = LivesPosition;

			// Offset its local position horizontally based on life number
			go.transform.localPosition = new Vector3( i*20, 0 , 0);

			// Add to our list of instances
			_LivesInstances.Add (go);
		}

		// Update the extra lives panel based on current state of GameManager
		UpdateRemainingLives ();

		// Get the highscorer's name and score from the game manager
		if (_gameManager!=null)
			_gameManager.GetHighScore( out _highScoreName, out _highScore );

		// Set the score to the players current score (fetched from game manager)
		if (CurrentScore!=null && _gameManager!=null) 
		{
			CurrentScore.text = "Current Score : "+_gameManager.currentScore;
		}

		// Set the text for the highscore text mesh based on data returned from
		// the GameManager
		if (HiScore!=null && _gameManager!=null) 
		{
			HiScore.text = "<High>"+_highScoreName+" : "+_highScore;
		}

		// Set the Get Ready panel to default state
		Reset( false );
	}

	// -----------------------------------------------------------------------------
	// Name	:	Reset
	// Desc	:	Sets the state of this object to an initial state
	// -----------------------------------------------------------------------------
	public void Reset( bool activate)
	{
		// Start the panel fully faded out.
		Color col;
		if (GetReadyPanel_Mat!=null)
		{
			col		= GetReadyPanel_Mat.color;
			col.a	= 0.0f;
			GetReadyPanel_Mat.color = col;
		}

		// Start with the line 1 and 2 on the panel 
		// also being fully faded out.
		if (GetReadyPanel_Line1!=null)
		{
			col  	= GetReadyPanel_Line1.color;
			col.a	= 0;
			GetReadyPanel_Line1.color = col;

			col  	= GetReadyPanel_Line2.color;
			col.a	= 0;
			GetReadyPanel_Line2.color = col;
		}

		// Set the active state of the HUD (as indicated by input paramater)
		if (_myGameObject!=null)
			_myGameObject.SetActive(activate);

		// If the current score of the player is greater than the current highscore
		// then make the highscore text bright red so the player instantly knows 
		// he is the new top of the table.
		if (_gameManager && HiScore!=null && _gameManager.currentScore>_highScore)
			HiScore.color = Color.red;
	}

	// ------------------------------------------------------------------------------
	// Name	:	SetGetReadyPaneltext
	// Desc	:	Called by the SceneManager when the game is starting, is over or if
	//			the player is between levels to set the text of lines 1 and 2 on the
	//			HUD panel
	// ------------------------------------------------------------------------------
	public void SetGetReadyPanelText( string line1, string line2)
	{
		// Simply set the passed lines of text in the text meshes
		if (GetReadyPanel_Line1) GetReadyPanel_Line1.text	=	line1;
		if (GetReadyPanel_Line2) GetReadyPanel_Line2.text	=	line2;
	}

	// ------------------------------------------------------------------------------
	// Name : FadeIn - Coroutine
	// Desc	: Fades in the HUD panel and the text over the passed duration.
	// ------------------------------------------------------------------------------
	public IEnumerator FadeIn( float duration )
	{
		if (GetReadyPanel_Mat && GetReadyPanel_Line1 && GetReadyPanel_Line2)
		{
			// Reset everything to transparent
			Reset ( true );

			// Reset timer and cache the current colors of the
			// background panel and the two lines of text
			float timer 			= 0;
			Color panelColor		= GetReadyPanel_Mat.color;
			Color panelLine1Color	= GetReadyPanel_Line1.color;
			Color panelLine2Color	= GetReadyPanel_Line2.color;

			//Divide by Zero Protection
			if (duration<0.0001f){ duration = 1.0f; timer = duration-0.0001f;}

			// Count up to the duration
			while (timer<duration)
			{
				// Update timer
				timer+= Time.deltaTime;

				// Calculate the new Alpha values of the materials
				// used by the panel and the two lines of text
				panelColor.a 		= (0.80f / duration) * timer;
				panelLine1Color.a	= (1.0f / duration) * timer;
				panelLine2Color.a	= (1.0f / duration) * timer;

				// Update the materials with the new color
				GetReadyPanel_Mat.color 	= panelColor;
				GetReadyPanel_Line1.color 	= panelLine1Color;
				GetReadyPanel_Line2.color   = panelLine2Color;

				yield return null;
			}
		}
	}

	// --------------------------------------------------------------------------
	// Name	:	FadeOut - Coroutine
	// Desc	:	Same as above function but in reverse
	// --------------------------------------------------------------------------
	public IEnumerator FadeOut( float duration )
	{
		if (GetReadyPanel_Mat && GetReadyPanel_Line1 && GetReadyPanel_Line2)
		{
			// Rest everything to transparent
			Reset ( true );
			
			float timer 			= duration;
			Color panelColor		= GetReadyPanel_Mat.color;
			Color panelLine1Color	= GetReadyPanel_Line1.color;
			Color panelLine2Color	= GetReadyPanel_Line2.color;

			// Divide by Zero protection
			if (duration<0.001f) { duration = 1.0f; timer = 0.0001f;}

			while (timer>0)
			{
				timer= Mathf.Max (timer-Time.deltaTime , 0.0f) ;
				panelColor.a 		= (0.80f / duration) * timer;
				panelLine1Color.a	= (1.0f / duration) * timer;
				panelLine2Color.a	= (1.0f / duration) * timer;
				
				GetReadyPanel_Mat.color 	= panelColor;
				GetReadyPanel_Line1.color 	= panelLine1Color;
				GetReadyPanel_Line2.color   = panelLine2Color;
				
				yield return null;
			}
		}
	}

	// -----------------------------------------------------------------------------
	// Name	:	UpdateRemainingLives
	// Desc	:	Used to update how many 'Remaining Lives' are shown on screen
	//			by asking the game manager
	// -----------------------------------------------------------------------------
	public void UpdateRemainingLives( )
	{
		// No game manager so return
		if (_gameManager==null) return;

		// Loop through all the remaining life instances we created at starup (10)
		for( int i=0; i<10; i++)
		{
			// Fetch each instance from our list
			if (_LivesInstances[i]!=null)
			{
				// If the index is lower than  our current life count
				// then show it. otherwise, disable it.
				if (i<_gameManager.lives-1) 
					_LivesInstances[i].SetActive (true);
				else
					_LivesInstances[i].SetActive(false);
			}
		}

	}

	// ---------------------------------------------------------------------------------
	// Name	:	UpdateScore
	// Desc	:	Fetche the score from the GameManager and sets the text mesh.
	// ---------------------------------------------------------------------------------
	public void UpdateScore()
	{
		if (_gameManager!=null)
		{
			if (CurrentScore!=null) CurrentScore.text = "Current Score : "+_gameManager.currentScore;
			if (HiScore!=null && _gameManager.currentScore>_highScore)
				HiScore.color = Color.red;
		}
	}
}
