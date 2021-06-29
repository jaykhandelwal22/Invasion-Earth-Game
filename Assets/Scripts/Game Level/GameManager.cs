// ------------------------------------------------------------------------
// FILE	:	GameManager.cs
// DESC	:	Contains the Game Manager class and support classes
// ------------------------------------------------------------------------
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

// ------------------------------------------------------------------------
// Class	:	LevelInfo
// Desc		:	Contains the game settings for a given level of the game
//				such as how fast the aliens move horizontally and 
//				vertically and the delay between movements.
// ------------------------------------------------------------------------
[System.Serializable]
public class LevelInfo
{
	public string Name						=	null; 	// Name of the Level (ie: Mars Defence)
	public float  StartMoveDelay			=	55.0f;	// Delay between alien moves at level start (seconds)
	public float  MoveDelayDecrement		=	1.0f; 	// How much the above delay is decreased with each alien death (seconds)
	public float  HorizontalStartSpeed		=	2.5f;	// Initial horizontal movement speed (how many units it moves)
	public float  HorizontalSpeedMultiplier	=	1.01f;	// How much the horizontal movement speed increases with each alien death
	public float  VerticalSpeed				=   12.0f;	// Vertical movement speed (how much they move down when they reach the end of a row
	public int	  AlienFireChance			=	25;		// % (out of 100) that an alien shot will be fired when they move
	public int	  MysteryInvaderChance		=	5;		// % (out of 100) that the MysteryInvader will be spawned (calculated each move)
	public int	  MaxAlienProjectiles		=   2;		// Maximum number of alien projectiles active at any time for this level.
	public float  MoveDelayLowClamp			=	0;		// Minimum movement delay allowed between alien moves.
}

// ------------------------------------------------------------------------
// Class	:	HighScoreEntry
// Desc		:	Represents a highscore entry in memory
// ------------------------------------------------------------------------
[System.Serializable]
public class HighScoreEntry
{
	public string  	Name  = null;		// Name of player to display in table
	public uint		Score = 0;			// Score of player to display in table
	public bool		Marker= false;		// Temp variable used between scene loads to track which score is the players
	public long     Stamp = 0;			// Timestamp of score ( used for consistent sorting)
}

// ---------------------------------------------------------------------------------
// Class	:	GameManager
// Desc		:	The GameManager is game global meaning that it is loaded once
//				at game startup and stays alive across all scene loads. It 
//				contains information about all the levels of the game as
//				well as manages the player's current status - lives left,
//				current level and score. It also manages loading/saving
//				the highscore table to disk.
//				The GameManager game object should also have an Audio source
//				attached which it will use to manage the playback and fades
//				of game music.
// ---------------------------------------------------------------------------------
public class GameManager : MonoBehaviour 
{
	// A list of all the levels in the game (Inspector Assigned)
	[SerializeField]
	private List<LevelInfo> 	 Levels		= new List<LevelInfo>();

	// Highscore table is just a list of HighScoreEntry objects that are sorted by score and timestamp
	[SerializeField]
	private List<HighScoreEntry> HighScores	= new List<HighScoreEntry>();

	// A list of AudioClips that can be played (such as varioius music tracks) which we
	// can instruct the GameManager to play by index
	[SerializeField]
	private List<AudioClip>      MusicClips 	= new List<AudioClip>();

	// Used to cache AudioSource component
	private AudioSource 		_music 			= null;

	// Current music clip in the above list that is currently playing
	private int 				_currentClip	= -1;

	// Number of lives remaining
	private int _lives 			= 3;
	public int lives{ get{ return _lives;}}

	// Play's current score
	private int _currentScore	= 0;
	public int currentScore{ get{return _currentScore;}}

	// Current level the player is on
	private int _currentLevel = 0;
	public int currentLevel{ get{ return _currentLevel;}}

	// A list of properties allowing external objects access to all the properties of the current level
	public string currentLevelName						{ get { return Levels[_currentLevel].Name;}}
	public float  currentLevelStartMoveDelay			{ get { return Levels[_currentLevel].StartMoveDelay;}}
	public float  currentLevelMoveDelayDecrement		{ get { return Levels[_currentLevel].MoveDelayDecrement;}}
	public float  currentLevelHorizontalStartSpeed		{ get { return Levels[_currentLevel].HorizontalStartSpeed;}}
	public float  currentLevelHorizontalSpeedMultiplier	{ get { return Levels[_currentLevel].HorizontalSpeedMultiplier;}}
	public float  currentLevelVerticalSpeed				{ get { return Levels[_currentLevel].VerticalSpeed;}}
	public int    currentLevelAlienFireChance			{ get { return Levels[_currentLevel].AlienFireChance;}}
	public int	  currentLevelMysteryInvaderChance		{ get { return Levels[_currentLevel].MysteryInvaderChance;}}
	public int 	  currentLevelMaxAlienProjectiles		{ get { return Levels[_currentLevel].MaxAlienProjectiles;}}
	public float  currentLevelMoveDelayLowClamp			{ get { return Levels[_currentLevel].MoveDelayLowClamp;}}

	//This will be used to store the file path of the highscore table
	private string _highScoresPath;

	// This is the file signiture of the highscore table file we will load or create
	private string _fileSigniture	=	"GI Space Invaders - Defenders of Sol v1.0";

	// Singleton Accessor - Only one GameManager should ever exist
	private static GameManager _instance	=null;
	public static GameManager instance
	{
		get
		{
			if (_instance==null)
			{
				_instance = (GameManager)FindObjectOfType(typeof(GameManager)) ;
			}

			return _instance;
		}
	}

	// ----------------------------------------------------------------------------------
	// UPGRADE NOTE:
	// In Unity 2017 the Application.isLoadingLevel property has been deprecated (shame).
	// We need to know when a level is loaded so we don't try to launch multiple load
	// requests in the LevelComplete function within a single frame.
	// Now we register a handler (OnSceneLoaded) which will be called by unity whenever
	// a scene has just been loaded. We use this to keep track of a bool (_isSceneLoading)
	// so that we only load a scene if this is set to false.
	// -----------------------------------------------------------------------------------
	private bool _isSceneLoading = false;

    void OnEnable()   {  UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;  }
   
    void OnDisable()    { UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;   }

	// On Scene Loaded Callback
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _isSceneLoading = false;
     }

   
	// -----------------------------------------------------------------------------
	// Name	:	Awake
	// Desc	:	Initialize the Game Manager state and loading
	//			previous scores from disk
	// -----------------------------------------------------------------------------
	void Awake () 
	{
		// Init Game State
		_lives 			= 3;
		_currentScore	= 0;
		_currentLevel   = 0;

		// This is a app global object which
		// should survive scene loads.
		DontDestroyOnLoad(gameObject);

		// Path where high scores are stored
		_highScoresPath = Application.persistentDataPath+"/HighScores";

		// Load in high scores
		LoadHighScores();
	}

	// ----------------------------------------------------------------------------
	// Name	:	Start
	// Desc	:	Cache audio component reference and make sure at startup that all
	//			music is turned off.
	// ----------------------------------------------------------------------------
	void Start()
	{
		// Cache AudioSource component is available
		_music = GetComponent<AudioSource>();

		// If an audio source exists
		if (_music)
		{
			// Make sure this isn't set to play on awake, make sure volume is initially
			// zero and that the source is not playing.
			_music.playOnAwake = false;
			_music.volume      = 0;
			_music.Stop();
		}
	}

	// -----------------------------------------------------------------------------
	// Name	:	PlayMusic
	// Desc	:	Plays an audio clip from the MusicClips list at the passed index
	//			and fades the music in over the specified duration.
	// -----------------------------------------------------------------------------
	public void PlayMusic( int clip, float fade)
	{
		// If no audio source component is attached to this game object then return.
		if (!_music) return;

		// If clip index is out of range then return
		if (MusicClips==null || MusicClips.Count<=clip || MusicClips[clip]==null) return; 

		// If the clip specified is current already playing then return
		if (_currentClip==clip && _music.isPlaying) return;

		// Make a note of the clip that we are about to play as this will become the current
		// playing clip
		_currentClip = clip;

		// Start a coroutine to fade the music in over time
		StartCoroutine ( FadeInMusic( clip , fade ));
	}

	// -------------------------------------------------------------------------------
	// Name	:	StopMusic
	// Desc	:	Stop any music that is playing by fading it out over the specified
	//			fade-time.
	// -------------------------------------------------------------------------------
	public void StopMusic( float fade = 2.0f)
	{
		// If no audio source component is attached to this game object then return
		if (!_music) return;

		// Set the current clip to -1 (none playing)
		_currentClip = -1;

		// Fade out the current clip
		StartCoroutine ( FadeOutMusic( fade ));
	}

	// ------------------------------------------------------------------------------
	// Name	:	FadeInMusic - Coroutine
	// Desc	:	The function that does the actual fading
	// ------------------------------------------------------------------------------
	private IEnumerator FadeInMusic(int clip, float fade)
	{
		// Minimum fade of 0.1 seconds
		if (fade<0.1f) fade = 0.1f;

		// We can only proceed if we have an audio source
		if (_music)
		{
			// If any music is current playing then stop it. This class assumes external
			// script will have requested that any previous music be faded out first.
			_music.volume 	= 0.0f;
			_music.Stop ();

			// Assign the new clip to the audio source and start playing it
			_music.clip 	= MusicClips[clip];
			_music.Play ();

			// Lets start recording the time that passes and fade in the volume
			float timer 	= 0.0f;

			// while we are still within the fade time
			while (timer<=fade)
			{
				// Increment timer
				timer+=Time.deltaTime;

				// Calculcate current volume factor
				_music.volume = timer/fade;

				// Remember to yield 
				yield return null;
			}

			// Fadein is complete
			_music.volume = 1.0f;
		}
	}


	// ------------------------------------------------------------------------------------
	// Name	:	FadeOutMusic	-	Coroutine
	// Desc :	Fades out the currently playing clip
	// ------------------------------------------------------------------------------------
	private IEnumerator FadeOutMusic( float fade )
	{
		// Minimum fade of 1.0
		if (fade<1.0f) fade=1.0f;

		// Must have an audio source
		if (_music)
		{
			// Force initial value to 1.0f
			_music.volume 	= 1.0f;

			// Reset timer
			float timer 	= 0.0f;

			// Loop for the fade time constantly calculting the volume factor
			while (timer<fade)
			{
				timer+=Time.deltaTime;
				_music.volume = 1.0f-timer/fade;
				yield return null;
			}

			// Fade-out complete so make sure value is zero and stop music playing
			_music.volume = 0.0f;
			_music.Stop ();
		}
	}

	// ----------------------------------------------------------------
	// Name	:	LoadHighScores
	// Desc	:	Load the saved high score table or create if it does
	//			not yet exist.
	// ----------------------------------------------------------------
	public void LoadHighScores()
	{
		// Does the highscore directory exist?
		bool	directoryExists	= Directory.Exists( _highScoresPath );

		// Does the actual highscore file exists
		bool    fileExists		= File.Exists(_highScoresPath+"/Highscores");

		// If directory does not exist then create it and create a new
		// high score table
		if (!directoryExists || !fileExists)
		{
			//Create the directory
			if (!directoryExists)
				Directory.CreateDirectory( _highScoresPath );

			// Create a new high score table
			if (!fileExists)		
				CreateNewHighScoreTable( "Highscores");
		}

		// It should be there now so load it
		LoadHighScores("Highscores");

	}	

	// ------------------------------------------------------------------------------
	// Name	:	CreateNewHighScoreTable
	// Desc	:	Create the highscore table for the first time
	// ------------------------------------------------------------------------------
	void CreateNewHighScoreTable( string fileName)
	{
		// Create timestamp that is used internally for sorting the highscore table
		// It is used in the case where two scores are the same.
		DateTime startOfEpoch = new DateTime(1970, 1, 1,0,0,0);
		long timeStamp = (long) ((DateTime.UtcNow - startOfEpoch).TotalMilliseconds/1000.0);

		// Create a new file using BineryWriter
		using (BinaryWriter writer = new BinaryWriter(File.Open(_highScoresPath+"/"+fileName, FileMode.Create)))
		{
			// Write out the header signiture
			writer.Write( _fileSigniture );

			// Now write 8 highscore entries
			for (int i=0; i<8; i++)
			{
				// Write out name (it's always about me :)
				writer.Write("GAZ");

				// Write out a default score for each entry
				writer.Write ( ((i+1 * 2) ) * 20 );

				// Write out score data
				writer.Write (timeStamp);
			}
		}
	}

	// ------------------------------------------------------------------------------
	// Name	:	LoadHighScores
	// Desc	:	Loads in the high score table from file
	//-------------------------------------------------------------------------------
	private bool LoadHighScores( string fileName )
	{
		int i;

		// Try and open up the highscore file using Binery Reader
		using (BinaryReader reader = new BinaryReader(File.Open(_highScoresPath+"/"+fileName, FileMode.Open)))
		{
			// Read the header
			string signiture = reader.ReadString();

			// If its not correct then this is not a file in our format
			if (signiture != _fileSigniture)
			{
				Debug.Log("Load Error: Invalid High Score File."); 
				return false;
			}

			// Clear any resident high scores
			HighScores.Clear ();

			// Read in data into list
			for ( i=0; i<8; i++ )
			{
				// Create new entry
				HighScoreEntry hs = new HighScoreEntry();

				// Such in the data from file
				hs.Name 		  = reader.ReadString();
				hs.Score		  = reader.ReadUInt32();
				hs.Stamp		  = reader.ReadInt64();

				// Add to highscore list
				HighScores.Add (hs);
			}
		}

		// Should be sorted in the file but let's just sort the list to be safe
		SortHighScores ();

		// Returns success
		return true;
	}
	
	// ------------------------------------------------------------------------------
	// Name	:	SaveHighScores
	// Desc	:   Write the score table currently resident in memory to disk
	// ------------------------------------------------------------------------------
	public void SaveHighScores()
	{
		// Nothing to save
		if (HighScores==null || HighScores.Count==0) return;

		// Sort the scores before saving
		SortHighScores ();

		// This is the folder and the file should live in
		string  fileName = _highScoresPath+"/HighScores";
		
		// Create a new file using Binery Writer
		using (BinaryWriter writer = new BinaryWriter(File.Open(fileName, FileMode.Create)))
		{
			// Write out the header
			writer.Write(_fileSigniture);

			// Write out the 8 highscores enties in the highscore list
			for (int i=0; i<8; i++)
			{
				// Safety Precaution: If for whatever unearthly reason
				// we find we don't have the correct number of score in the table
				// then just write out default values for the missing entries
				if (i>=HighScores.Count)
				{
					writer.Write("GAZ");
					writer.Write( 0 );
					writer.Write( (long) 0);
				}
				else
				{
					// Otherwise fetch the next highscore from the list
					HighScoreEntry hse = HighScores[i];

					// And write its Name, Score and timestamp to disk
					if (hse!=null && hse.Name!=null)
					{
						writer.Write(hse.Name);
						writer.Write (hse.Score);
						writer.Write (hse.Stamp);
					}
					// Safety Precaution: If for whatever reason the highscore
					// entry has null data (should never happen) then write out
					// default values for this entry.
					else
					{
						writer.Write("GAZ");
						writer.Write( 0 );
						writer.Write( (long) 0);
					}
				}
			}
		}
	}


	// -------------------------------------------------------------------------------
	// Name	:	DecrementLives
	// Desc	:	Decreases the number of lives and returns the new life count
	// -------------------------------------------------------------------------------
	public int DecrementLives()
	{
		if (_lives>0) _lives--;
		return _lives;
	}

	// -------------------------------------------------------------------------------
	// Name	:	GetHighScores
	// Desc	:	Returns the high score list sorted by score (and timestamp).
	//			Returns a List of HighScoreEntry objects.
	// -------------------------------------------------------------------------------
	public List<HighScoreEntry> GetHighScores()
	{
		SortHighScores ();
		return HighScores;
	}

	// -------------------------------------------------------------------------------
	// Name	:	SortHighScores
	// Desc	:	Sorts the list containing the high scores
	// -------------------------------------------------------------------------------
	private void SortHighScores()
	{ 
		// Sort based on score and timestamp by calling the List.Sort function
		// and passing in our own compare function
		HighScores.Sort
		(
			// Our sort function
			delegate ( HighScoreEntry h1, HighScoreEntry h2)
			{ 
				// If the two scores being compared are equal
				// the n sort by timestamp instead
				if (h2.Score.CompareTo(h1.Score)==0)
					return h2.Stamp.CompareTo (h1.Stamp);
				// Otherwise simply sort by score
				else
					return h2.Score.CompareTo(h1.Score);
			}
		);
	}

	// -------------------------------------------------------------------------------
	// Name	:	GetHighScore
	// Desc	:	Returns the name and score of the top entry in the table, This is 
	//			displayed in-game so the player can see what score they have to
	//			get to be the top.
	//			Returns name and score via out parameters
	// -------------------------------------------------------------------------------
	public void GetHighScore( out string name, out uint score)
	{
		// Set the output params to something incase there is an error
		name  = "ERR";
		score = 0;

		// If the highscore table does not exists or has no scores in it then just return (error)
		if (HighScores==null || HighScores.Count<1 || HighScores[0]==null || HighScores[0].Name==null) return;

		// Assign the name and score of the top entry in the highscore list to the output params
		name  = HighScores[0].Name;
		score = HighScores[0].Score;
	}

	// -------------------------------------------------------------------------------
	// Name	:	StartNewGame
	// Desc	:	Reset current score, number of lives and load the game level.
	// -------------------------------------------------------------------------------
	public void StartNewGame()
	{
		// Reset score and lives.
		_currentScore = 0;
		_currentLevel = 0;
		_lives        = 3;

		// Stop any music (3 second fade - ie from main menu to game scene)
		StopMusic (3);

		// Load the game scenne and start playing.
		UnityEngine.SceneManagement.SceneManager.LoadScene ("GameScene");
	}

	// -------------------------------------------------------------------------------
	// Name	:	LevelComplete
	// Desc	:	Called by the game scene when a level has been complete. This simply
	//			increases the current game level and RE-LOADS the GameScene so all
	//			invaders and bunkers are reset.
	// -------------------------------------------------------------------------------
	public void LevelComplete()
	{
		
		// Itis possible that level complete could be called multiple times in an
		// update. As load requests are not processed (probably until end of frame)
		// we need to make sure we don't issue multiple LoadScene requests causing
		// us to skip past entire levels. Instead, when we issue the request
		// we set an internal _isSceneLoading bool to true. This will block any
		// future requests to re-load the game scene. This boolean gets set BACK
		// to false in our OnSceneLoaded callback.
		// --------------------------------------------------------------------------
		if (!_isSceneLoading)
		{
			_isSceneLoading = true;

			// Increase level
			if (_currentLevel<Levels.Count-1)
				_currentLevel++;

			// Load the game scene
			UnityEngine.SceneManagement.SceneManager.LoadScene ("GameScene");
		}
	}

	// ---------------------------------------------------------------------
	// Name	:	QuitGame
	// Desc	:	Loads the closing credits. This is called by the MainMenu
	//			scene when the user presses ESC.
	// ---------------------------------------------------------------------
	public void QuitGame()
	{
				UnityEngine.SceneManagement.SceneManager.LoadScene ("Credits");
	}


	// ------------------------------------------------------------------------------
	// Name	:	GameOver
	// Desc	:	Checks the score and either reloads the main menu or loads the
	//			High Score entry scene. Called by Game Scene when player loses
	//			last life.
	// ------------------------------------------------------------------------------
	public void GameOver()
	{
		// Reset levels and lives
		_currentLevel = 0;
		_lives  = 3;

		// Start fading in the main menu / highscore table music
		PlayMusic (0,3);

		// Have we made the high score table?
		int playerTablePos  = -1;

		// Loop through the highscore table and find the first
		// entry that the players current score is greater than.
		// This will be out new position in the table.
		for (int i=0; i<HighScores.Count;i++)
		{
			// Get the entry to examine
			HighScoreEntry hse = HighScores[i];
			if (hse!=null)
			{
				// If our current score greater?
				if (_currentScore>=hse.Score)
				{
					// Yep! So remember this index where we need to insert
					// our new entry
					playerTablePos = i;

					//stop searching
					break;
				}
			}
		}

		// Have we earned a position in the table?
		if (playerTablePos!=-1)
		{
			// Create a new timestamp for out entry
			DateTime startOfEpoch = new DateTime(1970, 1, 1,0,0,0);
			long timeStamp = (long) ((DateTime.UtcNow - startOfEpoch).TotalMilliseconds/1000.0);

			// Allocate a new highScoreEntry
			HighScoreEntry nhse = new HighScoreEntry();

			// We don't know the player's name yet so lets set it to a placeholder until
			// we load the HighScoreEntry scene
			nhse.Name="---";

			// Record the current score
			nhse.Score=(uint)_currentScore;

			// We set this boolean to true so when we load the next scene (HighScoreEntry)
			// we can re-locate this entry and assign the player's name to it
			nhse.Marker = true;

			// Store timestamp
			nhse.Stamp = timeStamp;

			// Insert our new entry into the table
			HighScores.Insert ( playerTablePos, nhse );

			// We only want 8 entries bt we now have 9 so remove the bottom entry
			HighScores.RemoveAt( HighScores.Count-1 );

			// Load in the scene that inputs the high scorers name
			UnityEngine.SceneManagement.SceneManager.LoadScene ("HighscoreEntry");
		}
		else
		{
			// No high score achieved so go back to main menu
			UnityEngine.SceneManagement.SceneManager.LoadScene ("MainMenu");
		}
	}

	// ----------------------------------------------------------------------------
	// Name	:	SetPlayerName
	// Desc	;	Called by the HighScoreEntry scene to set the name of the current
	//			player that is being entered for a highscore entry.
	// ----------------------------------------------------------------------------
	public void SetPlayerName( string name )
	{
		int i;
	
		// Loop through the highscore entries and find the one with
		// the Market boolean set (see above function). This is the
		// entry the player has just achieved
		for (i=0; i<HighScores.Count;i++)
		{
			// Is this the entry which needs the name assigned
			if (HighScores[i].Marker)
			{
				// Assign the name
				HighScores[i].Name = name;
				break;
			}
		}
	
		// Just to be sure set all markers are now set to false
		for (i=0; i<HighScores.Count;i++) 
		{
			if (HighScores[i]!=null) HighScores[i].Marker=false;
		}

		// Save highscores to disk
		SaveHighScores();

		// Load the main menu
		UnityEngine.SceneManagement.SceneManager.LoadScene ("MainMenu");
		
	}

	// ----------------------------------------------------------------------------
	// Name	:	IncreaseScore
	// Desc	:	Called by the main SceneManager everytime an invader gets killed.
	// ----------------------------------------------------------------------------
	public void IncreaseScore( int points )
	{
		_currentScore+=points;
	}
}
