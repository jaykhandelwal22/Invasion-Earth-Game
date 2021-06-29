using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

// ------------------------------------------------------------------------------
// Class	:	HighscoreDisplay
// Desc		:	Manages the display of the High Score table.
// ------------------------------------------------------------------------------
public class HighscoreDisplay : MonoBehaviour 
{
	// Text Mesh used to display all the names - Inspector Assigned
	public TextMesh Names 		= null;

	// Text Mesh used to display all the scores - Inspector Assigned
	public TextMesh Scores		= null;

	// Describes the number of seconds the high score table should be displayed
	// before it instructs the scene manager to display the main menu again
	public float 	DisplayTime	 = 6.0f;

	// Used internally to timed events
	private float _timer = 0.0f;

	// -------------------------------------------------------------------------
	// Name	:	OnEnable
	// Desc	:	Called by Unity whenever the behavior becomes enabled
	// -------------------------------------------------------------------------
	void OnEnable () 
	{
		// Set internal timer to zero
		_timer = 0.0f;

		// Cache a reference to the Game Manager used by the game
		GameManager gameManager = GameManager.instance;

		// Assuming we have a game manager
		if (gameManager!=null)
		{
		
			// Ask the Game Manager for a sorted list of highscorers ( names and scores)
			// This is returned as a list of HighScoreEntry objects.
			List<HighScoreEntry> highScores = gameManager.GetHighScores();

			// If we have some data returned by the game manager (should always)
			if (highScores!=null)
			{
				// Allocate two string builder object to efficiently
				// build a string of scorers and scores
				StringBuilder namesText = new StringBuilder();
				StringBuilder scoresText= new StringBuilder();

				// Loop through all the highscores returned by the game manager
				// Support for a maximum of 8 entries
				for (int i=0; i<Mathf.Min (highScores.Count, 8); i++)
				{
					// Fetch the HighScoreEntry from the list
					HighScoreEntry hse = highScores[i];

					// Assuming it has a valid name and score
					if (hse!=null && hse.Name!=null)
					{
						// Add the name to the name stringbuilder
						namesText.Append(hse.Name).Append ("\n");

						// Add the score to the score stringbuilder
						scoresText.Append(hse.Score).Append ("\n");
					}
				}

				// If we have a valid reference to a textmesh to display names
				if (Names!=null)
				{
					// Convert the names stringbuilder to a string and set as the text mesh's
					// text property.
					Names.text = namesText.ToString ();
				}
			 
				// If we have a valid reference to a textmesh for displaying scores
				if (Scores!=null)
				{
					// Convert the scores string builder to a string and set as the text mesh's
					// text property
					Scores.text = scoresText.ToString ();
				}
			}
		}
	}

	// ------------------------------------------------------------------------------------------
	// Name	:	Update
	// Desc	:	Called by Unity each frame. Here we use it to track time and to instruct the
	//			scene manager to display the manu menu screen if the high score table display
	//			duration has been exceeded.
	// -----------------------------------------------------------------------------------------
	void Update()
	{
		// Increase the timer
		_timer+=Time.deltaTime;

		// If we are in display mode (not input a new high score mode)
		// then we are in main menu and can flick between different modes.
		// SceneManager_MainMenu will not be present otherwise
		if (SceneManager_MainMenu.instance!=null)
		{
			// If the timer as supassed the display time then tell scene manager
			// so it can do what it wants to do with this game object (i.e disable it).
			if (_timer>DisplayTime)
				SceneManager_MainMenu.instance.NextScreen();
		}
	}
}
