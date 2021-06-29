// ---------------------------------------------------------------------------------
// FILE	:	SceneManager.cs
// DESC	:	Contains the scene manager which pretty much manages is the entire game
//          play scene.
// ---------------------------------------------------------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//#pragma warning disable  0618

// States the SceneManager - and thus the game - can be in.
public enum LevelState { GetReady , Playing , NextLife, Pause, GameOver };

// ---------------------------------------------------------------------------------------------
// Class : Alien
// Desc  : Stores information about each invader instance (exluding the mystery invader). Allows
//		   us efficent access to any invader's GameObject, Transform and Invader script
// ---------------------------------------------------------------------------------------------
public class Alien
{
	public GameObject AlienGameObject 	= null;
	public Invader    InvaderScript 	= null; 
	public Transform  AlienTransform 	= null; 
}

// ---------------------------------------------------------------------------------------------
// Class : MysteryAlien
// Desc	 : Stores information about the Mystery Invader. Same as above but for mystery invader.
// ---------------------------------------------------------------------------------------------
public class MysteryAlien
{
	public GameObject 		AlienGameObject = null;
	public MysteryInvader   InvaderScript 	= null; 
	public Transform  		AlienTransform 	= null;
}

// ---------------------------------------------------------------------------------------------
// Class : SceneManager
// Desc	 : The class that manages the entire game scene.
// ---------------------------------------------------------------------------------------------
public class SceneManager : MonoBehaviour 
{
	public  Hud                 HUD					= null;   					// Script that displays GUI elements
	public  List<GameObject> 	AlienPrefab 		= new List<GameObject>();	// List of Invader Prefabs
	public  List<AudioClip>     MovementSounds		= new List<AudioClip>();	// Audio clips used for invader movement
	public  AudioClip 			InvaderHitClip		= null;						// Audio Clip used for Invader death 

	public  List<AudioClip>		ExplosionSounds		= new List<AudioClip>();	// Audio Clips used for explosions
	private List<Alien> 		_alienInstances		= new List<Alien>();		// The instantiated Invaders (55 at game start)
	private MysteryAlien 		_mysteryAlien		= new MysteryAlien();		// The Mystery Invader instance
	private List<int>			_alientColCounts 	= new List<int>();   		// A list containing the number of invaders alive in each column
	private GameObject 		 	_waveObject  		= null;						// Parent object all regular invaders are parented to. Move this moves the entire wave.
	private Transform       	_waveTransform		= null;						// Transform of the wave object
	private GameManager			_gameManager		= null;						// Cached reference to GameManager

	public  GameObject				AlienProjectilePrefab 	 = null;			// Prefab of alien projectile

	// Contains a list/pool of alien projectiles that are constantly recycled to
	// stop us always instantiating them (GC reduction)
	public  List<AlienProjectile>	AlienProjectileInstances = new List<AlienProjectile>();
	
	// These settings are fetched from the game manager and cached for the current level

	private  float 	_xStep 				= 2.5f;		// Horizontal movement speed/size
	private  float	_xStepMultiplier	= 1.001f;	// How much the movement size increases with each invader kill
	private  float 	_yStep 				= 10.0f;	// Vertical movement size
	private  float	_moveDelay		 	= 55.0f;	// Delay in seconds between moves
	private  float 	_moveDelayDecrement = 1.0f;		// The amount the delay increases with each invader death
	private float	_lowMoveClamp		= 1;		// Minimum delay allowed between alien moves
	private  int 	_maxAlienProjectiles = 2;
	private	 int 	_liveAlienProjectiles  = 0;
	private  int 	_mysteryChance   	= 2;
    private int     _invaderFireChance = 10;		// % chance invaders will fire this move step 

	// At scene startup these are the minimum and maximum extents that the wave object containing all the invaders
	// can move from left to right. When these extents are reached the wave object moves down a row. These extents
	// are adjusted throughout the game as columns are cleared.
	private  int XMin 					= -20;
	private  int XMax 					= 20;

	// Internals
	private int 				_delayCounter 		= 0;					// Used to time the delay between each move
	private Vector3 			_wavePos 			= Vector3.zero;			// Current position of the wave
	private AudioSource 		_moveAudioSource	= null;					// Audio source used for movement sounds
	private  AudioSource  		_invaderHit			= null;					// Audio source used for Invader death sound
	private List<AudioSource> 	_explosions		= new List<AudioSource>();	// Audio source used for explosion sounds
	private int					_movementClipIndex  = 0;					// Current movement sound clip being played
	private float				_highMoveClamp		= 50;					// Maximum delay allowed betweeb alien moves
	private float           	_mysteryInvaderTimer= 0.0f;					// Used to impose a minimum duration with 
																			// which Mystery Invader can appear

	// Explosion Particle System transforms
	public 	Transform		  InvaderExploder		= null;		// Used for invader death
	public 	Transform         BaseExploder			= null;		// Used for bunker missile hit
	public  Transform		  PlayerExploder		= null;		// Used for player death


	// Cacd the actual systems as well so we can manually emit particles
	private ParticleSystem   _invaderExploderPS  	= null;
	private ParticleSystem   _baseExploderPS		= null;
	private ParticleSystem  _playerExploderPS		= null;

	// Text Meshes
	public GameObject         WarningText       = null;		// Displayed when invaders get low
 	public 	Transform         PointsTransform	= null;		// Points Text (Mystery Invader)
	private TextMesh		 _pointsMesh		= null;
	private GameObject       _pointsGameObject  = null;

	// Player's Cannon
	public GameObject		PlayerCannon		= null;
	private Cannon			_playerCannonScript = null;

	// Initial Invader Count
	private int _invaderCount 	= 55;

	// The current state of the game - start at GetReady state
	private  LevelState		_levelState = LevelState.GetReady;
	public LevelState levelState{ get{ return _levelState;}}

	// Singleton accessor. Only one scene manager should exist in the
	// scene
	private static SceneManager _instance	= null;
	public static SceneManager instance
	{
		get
		{
			if (_instance==null)
			{
				_instance = (SceneManager)FindObjectOfType(typeof(SceneManager)) ;
			}

			return _instance;
		}
	}


	// -------------------------------------------------------------------------------------------------------
	// Name	:	Start
	// Desc	:	Cache references and instantiate the grid of invaders
	// -------------------------------------------------------------------------------------------------------
	void Start () 
	{
		// Get the game manager instance
		_gameManager = GameManager.instance;


		// Fetch all the parameters for the current level from the game manager and cache
		if (_gameManager!=null)
		{
			_maxAlienProjectiles = _gameManager.currentLevelMaxAlienProjectiles;
			_xStep				 = _gameManager.currentLevelHorizontalStartSpeed;
			_xStepMultiplier	 = _gameManager.currentLevelHorizontalSpeedMultiplier;
			_yStep				 = _gameManager.currentLevelVerticalSpeed;
			_mysteryChance		 = _gameManager.currentLevelMysteryInvaderChance;
			_invaderFireChance	 = _gameManager.currentLevelAlienFireChance;
			_lowMoveClamp		 = _gameManager.currentLevelMoveDelayLowClamp;
			_moveDelay			 = _gameManager.currentLevelStartMoveDelay;
			_moveDelayDecrement	 = _gameManager.currentLevelMoveDelayDecrement;
		}

		// Create a new game object and will be the parent of all the regular invaders
		// we are about to instantiate
		_waveObject		= new GameObject("Wave Container");
		_waveTransform 	= _waveObject.transform;

		// Top left corner of wave to start creating Invaders
		int x=-100;
		int y= 160;

		// We will instantiate 5 rows of 11 Invaders
		for (int rows=0; rows<5; rows++)
		{
			for( int cols=0; cols<11; cols++)
			{
				// Instantiate the correct Invader prefab depending on the row
				GameObject go = Instantiate( AlienPrefab[rows], new Vector3( x, y, 0), Quaternion.identity) as GameObject;

				// If successfully instantiated
				if (go!=null)
				{
					// Parent to the wave object
					go.transform.parent = _waveTransform;

					// Create a new Instance of an Alien object to store in our alien instances list
					Alien alien = new Alien();

					// Cache the GameObject and Transform references
					alien.AlienGameObject = go;
					alien.AlienTransform  = go.transform;

					// Store a reference to the Invader script/Component also
					alien.InvaderScript   = go.GetComponent<Invader>();

					// Tell the Invader script what column it has been assigned to
					alien.InvaderScript.Column = cols;

					// Add the new instance to our list
					_alienInstances.Add ( alien ); 
				}

				// Invaders are space 20 apart
				x+=20;				
			}

			// Get ready for new row. Move X back to -100 and
			// y down 20 units to the next row
			x=-100;
			y-= 20;
		}

		// Initially there are five Invaders in each of the 11 columns
		// so store the columns counts
		for (int cols=0; cols<11; cols++)
		{
			_alientColCounts.Add (5);
		}

		// Set the initial wave object's position
		_waveTransform.position = new Vector3(XMin,-30,0);
		_waveTransform.rotation = Quaternion.identity;

		// Cache the position of the wave in vector for
		// efficient changing and resetting
		_wavePos 			    = _waveTransform.position;

		// Create a pool of Invader Projectiles that we can re-use through the game
		// so we don't have to instantiate one every time we need one
		for (int i=0; i< _maxAlienProjectiles; i++)
		{
			// Instantiate a new projectile from the inspector assigned prefab
			GameObject go = Instantiate ( AlienProjectilePrefab ) as GameObject;
			if (go!=null) 
			{
				// Fetch the AlienProjectile component/script reference from that
				// object and store in our AlienProjectiles list/pool
				AlienProjectile instance = go.GetComponent<AlienProjectile>();
				if (instance!=null)
				{
					AlienProjectileInstances.Add (instance);
				}
			}
		}

		// Add a new Audio Source to this game object that will be used to play movement sounds
		_moveAudioSource 				= gameObject.AddComponent<AudioSource>();
		_moveAudioSource.playOnAwake 	= false;

		// Add Audio Source used to play Invader death sound
		_invaderHit      				= gameObject.AddComponent<AudioSource>();
		_invaderHit.clip 				= InvaderHitClip;
		_invaderHit.playOnAwake			= false;

		// We start playing movement clip 0 but iterate though the list. This is what makes
		// the tone change when the invaders move. Each clip is played as the invaders move so
		// as they move faster and faster, the movement sounds play faster generating suspense.
		_movementClipIndex = 0;

		// Create 4 AudioSources and cache references to them that we can use for playing overlapping
		// explosion sounds
		for (int i=0; i<4; i++)
		{
			_explosions.Add (gameObject.AddComponent<AudioSource>());
		}

		// Instantiate the MysteryAlien (disabled by default) and store a reference to
		// its Game Object, Transform and MysteryInvader script.
		_mysteryAlien.AlienGameObject = Instantiate( AlienPrefab[5] ) as GameObject;
		_mysteryAlien.InvaderScript   = _mysteryAlien.AlienGameObject.GetComponent<MysteryInvader>();
		_mysteryAlien.AlienTransform  = _mysteryAlien.AlienGameObject.transform;

		// Player cannon should already exist in the scene so just fetch its Cannon
		// script/component.
		if (PlayerCannon!=null)
		{
			_playerCannonScript = PlayerCannon.GetComponent<Cannon>();
		}

		// Fetch references to the Particle Emitters/System ujsed by the game and assigned via the inspector
		if (InvaderExploder!=null)
		{
			_invaderExploderPS = InvaderExploder.GetComponent<ParticleSystem>();
		}

		if (BaseExploder!=null)
		{
			_baseExploderPS = BaseExploder.GetComponentInChildren<ParticleSystem>();
		}

		if (PlayerExploder!=null)
		{
			_playerExploderPS = PlayerExploder.GetComponentInChildren<ParticleSystem>();
		}

		// Cache a reference to the game object that displays the points text when you destroy
		// the mystery invader.
		if (PointsTransform!=null)
		{
			_pointsMesh 		= PointsTransform.GetComponent<TextMesh>();
			_pointsGameObject	= PointsTransform.gameObject;

		}

		//Turn off Warning text at level start
		if (WarningText)
			WarningText.SetActive(false);

		// Display the GetReadyPanel (HUD functon) prior to game starting. So set text of the HUD panel.
		if (HUD && _gameManager) 
			HUD.SetGetReadyPanelText (_gameManager.currentLevelName, "Lives Remaining : "+_gameManager.lives+"\nPress Space to Continue");
	
		// And start the coroutine that fades in HUD and fades out intro music and waits for player to press space
		StartCoroutine ( GetReadyForNextLife(0.0f));
	}

	// ------------------------------------------------------------------------------------------------------
	// Name	:	TurnOffPointsText
	// Desc	:	Turns off the text displayed when you destroy the Mystery Invader
	// ------------------------------------------------------------------------------------------------------
	void TurnOffPointsText()
	{
		if (_pointsGameObject!=null) _pointsGameObject.SetActive(false);
	}

	// ------------------------------------------------------------------------------------------------------
	// Name	:	FixedUpdate
	// Desc	:	Update the position and animation of the Invaders at timed intervals
	// ------------------------------------------------------------------------------------------------------
	void FixedUpdate () 
	{
		// We only process movements if we are in playing state
		if (_levelState!= LevelState.Playing) return;

		// Increase the timer that is used to record the last time a Mystery
		// Invader was spawned
		_mysteryInvaderTimer+=Time.deltaTime;

		// Increase the delay counter that is used to time the delay between
		// each invader movement.
		_delayCounter++;

		// If the delay counter is greater than the current movement delay of our invaders
		// its time to move the wave of aliens again
		if (_delayCounter>= Mathf.Clamp (_moveDelay, _lowMoveClamp , _highMoveClamp))
		{
			// Play the next movement sound in the movement clips array (there are 4)
			if (!_moveAudioSource.isPlaying)
			{
				// Assign the next clip in the list and play it
				_moveAudioSource.clip = MovementSounds[_movementClipIndex++];
				_moveAudioSource.Play();
			}

			// If we have played the last movement sound in the array start at
			// beginning again
			if (_movementClipIndex>3) _movementClipIndex = 0;

			// Get the position of the invaders parent container object
			_wavePos = _waveTransform.position;

			// If we have reached the left or right extremes - depending on our movement
			// direction (x step) then it is time to flip the movement direction and move
			// the aliens down a row.
			if ( (_wavePos.x>=XMax && _xStep>0) || (_wavePos.x<=XMin && _xStep<0))
			{
				_xStep=-_xStep;
				_wavePos.y-=_yStep;
			}
			// Otherwise just do another step in the current movement direction
			else
			_wavePos.x+=_xStep;
			
			// Now set the new position we just calculated above to the transform of
			// the invader's parent container object
			_waveTransform.position = _wavePos;

			// Because we have moved, we need to tell each invader to update its
			// current frame - each Invader has two meshes which are toggled on
			// and off to get the primitive animations of the game. We will also
			// test the invaders position to see if any invaders have landed thus
			// ending the game
			bool closeToLanding = false;

			// Loop through each alien instance
			for (int i=0; i<_alienInstances.Count; i++)
			{
				// Fetch it from the list
				Alien alien = _alienInstances[i];

				// Ignore if the alien has been distroyed and is not current active
				if (alien!=null && alien.InvaderScript!=null && alien.AlienGameObject.activeSelf)
				{
					// Call its UpdateFrame function which will cause to to switch between its
					// two child meshes (animation)
					alien.InvaderScript.UpdateFrame();

					// if the alien's Y position is less than 10 then game should end
					if (alien.InvaderScript.altitude<10.0f)
					{
						// Set level state to game over
						_levelState = LevelState.GameOver;

						// Deactivate the mystery alien if it is active
						if (_mysteryAlien.AlienGameObject)
						{
							_mysteryAlien.AlienGameObject.SetActive(false);
						}

						// Turn off warning text
						if(WarningText) WarningText.SetActive (false);

						// If the HUD component is available set the text of the HUD
						// to Game Over and run the coroutine that displays end of game sequence
						if (HUD)
						{
							HUD.SetGetReadyPanelText("Game Over", "The Invaders Have Landed\nPress Space to Continue");
							StartCoroutine ( GetReadyForNextLife() );
							return;
						}
					}
					else
					// Otherwise the invader is not landed yet but if less than 30 unit's high then
					// he is getting close so set boolean
					if (alien.InvaderScript.altitude<=30)
					{
						closeToLanding = true;
					}
				}
			}

			// If any invader is close to landing
			if(closeToLanding)
			{
				// Turn on the warning text
				if (WarningText) WarningText.SetActive (true);

				// Play AirRaid music (3rd entry in music list)
				if (_gameManager)
					_gameManager.PlayMusic(2, 1.0f);
			}
			else
			// Otherwise NO invaders were found to be close to landing
			if (_levelState==LevelState.Playing)
			{
				// So turn off Warning text
				if (WarningText) WarningText.SetActive (false);

				// Stop playing music (the Air Raid siren)
				if (_gameManager)
					_gameManager.StopMusic (1);
			}



			// We have moved so it is time to reset the delay counter so we don't do any of the 
			// above again until the correct amount of time has elapsed
			_delayCounter = 0;

			// Everytime we move we also decide whether we should launch another alien projectile
			// which is why we get more missiles as the invaders near the bottom of the screen.
			// We only fire another one if we have less active than the number allowed for this level and
			// if we roll a random number under our _invaderFireChance property.
			if (_liveAlienProjectiles<_maxAlienProjectiles &&  Random.Range (0,100)<_invaderFireChance)
			{
				// We don't instantiate projectiles but instead re-use
				// from a pool. So loop through the projectile pool and find one not in use
				for (int i=0; i<_maxAlienProjectiles; i++)
				{
					// Get the projectile from the pool
					AlienProjectile instance = AlienProjectileInstances[i];

					// Only use this if it is not currently in use
					if (instance!=null && !instance.isActive)
					{
						// Choose a column at random to fire it from
						int col = Random.Range (0,10);

						// If the column is empty then choose again
						if (_alientColCounts[col] == 0) continue;

						// We now have the column to fire from so let us now find
						// the lowest invader in that column (a max of 5 in the row)
						for (int y=4; y>=0; y--)
						{
							// Search for an invader starting from the bottom of the column
							Alien invader = _alienInstances[y*11+col];

							// If this invader is still active it must be the lowest in the column
							// that is left alive so we fire from here
							if (invader.AlienGameObject.activeSelf)
							{
								// Fire the projectile from the invader's position
								instance.Fire ( invader.AlienTransform.position, false);

								// Increase the number of currently live projectiles
								_liveAlienProjectiles++;
								break;
							}
						}
						break;
					}
				}
			}

			// Is the mystery alien setup correctly
			if (_mysteryAlien.AlienGameObject!=null)
			{	
				// If its' not currently active then let's see if it is time to spawn it again
				if (!_mysteryAlien.AlienGameObject.activeSelf)
			  	{
				   	// Roll the dice again and see if we should turn it on again
				    if (_mysteryInvaderTimer>20.0f &&
				        Random.Range (0,100)<_mysteryChance)
					{
						// Reset the timer so we don't try and respwn it too soon
						_mysteryInvaderTimer = 0.0f;

						// Activate the mystery invader
						_mysteryAlien.AlienGameObject.SetActive(true);
					}
				}
				else
				{
					// Otherwise it's active so lets see if it should fire. We will use a projectile from
					// the invader's projectile pool
					if (_liveAlienProjectiles<_maxAlienProjectiles &&  Random.Range (0,100)<_invaderFireChance*2.0f)
					{
						// Loop through projectile pool and find one not in use
						for (int i=0; i<_maxAlienProjectiles; i++)
						{
							// If this projectile currently inactive, if so use it.
							AlienProjectile instance = AlienProjectileInstances[i];
							if (instance!=null && !instance.isActive)
							{
								// Fire the projectile from the mystery invaders position
								instance.Fire ( _mysteryAlien.AlienTransform.position, true);
								_liveAlienProjectiles++;
								break;
							}
						}
					}
				}
			}
		}
	}

	// ---------------------------------------------------------------------------------------------
	// Name	:	RegisterAlienProjectile
	// Desc	:	This is called by an alien projectile when it deactivates and is ready to be used
	//			again. 
	// ----------------------------------------------------------------------------------------------	
	public void RegisterAlienProjectile()
	{
		_liveAlienProjectiles--;
	}

	// ----------------------------------------------------------------------------------------------
	// Name	: RegisterBaseHit
	// Desc : This is called by any projectile when it hits the bunkers and is about to be
	//		  deactivated. Called by player and alien projectiles. Position of the missle
	//		  hit is passed so we know where to emit particles.
	// ----------------------------------------------------------------------------------------------
	public void RegisterBaseHit( Vector3 pos )
	{
		// If we have assigned a valid Game Object for base explosions
		if (BaseExploder!=null)
		{
			
			// Set its position to the passed position
			BaseExploder.position = pos;

			// If it has a particle emitter component
			// and we have a reference to it then tell it
			// to emit 50 particles.
			if (_baseExploderPS)
				_baseExploderPS.Emit(5);
		}

		// We have multiple audio sources for playing explosions so we can
		// play more than one overlapping.
		if (ExplosionSounds.Count>0)
		{
			// Loop through the audio source pool
			for( int i=0; i< _explosions.Count; i++)
			{
				// Get the current audio source
				AudioSource src = _explosions[i];

				// If its a valid audio source and it isn't already being used
				// (in other words if it isn't playing a sound at the moment) - Then
				// use it.
				if (src!=null && !src.isPlaying)
				{
					// Choose a random index into the Explosion Clips list.
					int effectIndex = Random.Range (0, ExplosionSounds.Count);

					// Assuming we are not indexing into a null entry
					if (ExplosionSounds[effectIndex]!=null)
					{
						// Assign the random clip to our audio source
						src.clip 	= ExplosionSounds[effectIndex];

						// Set the volume to 1/4 value (assuming explosions are authored full value)
						src.volume 	= 0.25f;

						// Set the pitch randomly to get further variation
						src.pitch   = Random.value+0.8f;

						// Play this bad boy
						src.Play();
					}
				}
			}
		}
	}

	// -----------------------------------------------------------------------------
	// Name	:	Player Hit
	// Desc	:	This is called whenever the players cannon is hit by an
	//			alien missile. It is called from the AlienProjectile class
	//			in response to OnTriggerEnter
	// ----------------------------------------------------------------------------
	public void PlayerHit( Vector3 pos )
	{

		// If we are not in the playing state them apply no damage
		// Might be a stray missile just passing at level complete.
		if (_levelState!=LevelState.Playing) return;

		// Assuming we have a reference to the cannon game object
		if (PlayerCannon!=null)
		{
			// Turn it off..its been destroyed
			PlayerCannon.SetActive (false);

			// If we have assigned a game object for cannon explosions
			if (PlayerExploder!=null)
			{
				// Set it to the position of the cannon
				PlayerExploder.position = pos;

				// And emit 200 particles from its particle emitter
				if (_playerExploderPS!=null)
				{
					_playerExploderPS.Emit (300);
				}
			}
		}
	
		// If we have a Game Manager
		if (_gameManager)
		{
			// If this wasn't our last life then put us in the NextLife
			// State - this is like the get ready state
			if (_gameManager.lives>1)
				_levelState = LevelState.NextLife;
			// Otherwise put us in the game over state
			else
				_levelState = LevelState.GameOver;
		}

		// Disable any active Mystery Invader
		if (_mysteryAlien.AlienGameObject)
		{
			_mysteryAlien.AlienGameObject.SetActive(false);
		}

		// Disable Warning text
		if(WarningText) WarningText.SetActive (false);

		// If the game manager reference is valid
		if (_gameManager)
		{
			// Tell game manager to subract a live
			_gameManager.DecrementLives();

			// Set the hud panel text to say Next Life or Game over depending on the game state
			if (HUD)
			{
				if (_levelState==LevelState.NextLife)
					HUD.SetGetReadyPanelText("Get Ready", "Lives Remaining : "+_gameManager.lives+"\nPress Space to Continue");
				else
					HUD.SetGetReadyPanelText("Game Over", "You have failed Earthling\nPress Space to Continue");
			}
	    }

		// Now find a free audio source we can play a random explosion sound on
		if (ExplosionSounds.Count>0)
		{
			AudioSource src = _explosions[0];
			if (src!=null)
			{
				if (ExplosionSounds[0]!=null)
				{
					src.clip 	= ExplosionSounds[0];
					src.volume 	= 1.0f;
					src.Play();
				}
			}
		}

		// Display the HUD - GetReady/GameOver/NextLife sequence
		StartCoroutine ( GetReadyForNextLife() );
	}

	// --------------------------------------------------------------------------------
	// Name	:	GetReadyForNextLife
	// Desc	:	Timed display of the hud between lives / Levels
	// --------------------------------------------------------------------------------
	private IEnumerator GetReadyForNextLife(float initialDelay = 2.0f)
	{
		// Wait for Explosions to end (roughly)
		yield return new WaitForSeconds(initialDelay);
	
		// If we are going into Game Over state then play Game Over music/Audio
		if (_gameManager && _levelState==LevelState.GameOver)
			_gameManager.PlayMusic (1, 4);

		// Fade the HUD in to display the Next level/ Game Over text
		if (HUD!=null) 
			yield return StartCoroutine ( HUD.FadeIn(0.5f));

		// Reset timer
		float timer=0;

		// Keep looping until key/button press
		while(true)
		{
			// Accumulate time
			timer+=Time.deltaTime;

			// Ignore any key presses in first 2 seconds so the user
			// doesn't accidently skip past it if they just died in a
			// firing frenzy
			if (Input.GetButtonDown("Fire1") && timer>2.0f) break;

			// ALWAYS yield
			yield return null;
		}

		// A key has been pressed to continue so if we are in the game over state
		if (_gameManager && _levelState==LevelState.GameOver)
		{
			// Tell the game manager to load main menu or high score entry screens
			_gameManager.GameOver();

			// Chew up time - this coroutine will be terminated anyway on scene load
			// in the above function.
			yield return new WaitForSeconds(100);
		}

		// Fade out the HUD if this is not a game over scenario
		if (HUD!=null)
			yield return StartCoroutine ( HUD.FadeOut(0.5f));

		// Reset and reactivate the cannon game object
		if (_playerCannonScript) _playerCannonScript.Reset ();

		// Update the HUD's lives (the cannons in the bottom left)
		if (HUD!=null)
		{
			// Update the HUD with the new life count
			HUD.UpdateRemainingLives();

			// Reset the hud
			HUD.Reset(false);
		}

		// Start Playing Again
		_levelState = LevelState.Playing;
	}

	// ---------------------------------------------------------------------------
	// Name	:	RegisterHit
	// Desc	:	Called when and invader gets destroyed. It decreases the invader
	//			count and finishes the level if all invaders are destroyed. it
	//			also checks for empty columns in the wave and if found the extents
	//			of the wave are adjusted. it also emits particles and plays a sound
	//			at the invaders position.
	// 			Is passed the column index the dead invader was in, the amount of
	//			points that should be awarded and the position that the alien died
	//			at.
	// ---------------------------------------------------------------------------
	public void RegisterHit( int colIndex, int points, Vector3 pos )
	{
		// If we have a valid game manager
		if (_gameManager!=null) 
		{
			// Tell game manager to increase score
			_gameManager.IncreaseScore( points );

			// Update the HUD
			if (HUD!=null) HUD.UpdateScore();
		}

		// Decrease the invaders in the column except when
		// -1 which means its the MysteryAlien Invader
		if (colIndex>-1)
		{
			// Decrease the count for this invader's column
			_alientColCounts[colIndex]--;

			// Decrease overall invader count
			_invaderCount--;

			// Update Game Difficulty with the death of
			// each invader by decreasing movement delay and
			// and potentially increasing the step distance
			// with each move.
			_moveDelay-=_moveDelayDecrement;
			_xStep*=_xStepMultiplier;
			
			// Compile new extents that the WAVE of aliens can
			// move now that a column may have been cleared
			for (int i=0;i< 11; i++)
			{
				if (_alientColCounts[i]==0)
					XMin= -20 - ((i+1)*20);
				else
					break;
			}
			
			for (int i=10; i>=0; i--)
			{
				if (_alientColCounts[i]==0)
					XMax= 20 + ((11-i)*20);
				else
					break;
			}
		}
		// Otherwise its the Mystery Invader that has been killed
		// so display the points text mesh by the invader and display
		// text for 2 seconds.
		else
		{
			if (_pointsGameObject!=null)
			{
				PointsTransform.position = pos;
				_pointsGameObject.SetActive(true);
				if (_pointsMesh)
					_pointsMesh.text= points+" Points";
				Invoke ( "TurnOffPointsText" , 2.0f);
			}
		}

		// Play Invader Hit sound effect
		_invaderHit.Play();

		// Play the invader death particle system (emit 50 particles)
		if (InvaderExploder!=null)
		{
			InvaderExploder.position = pos;
			if (_invaderExploderPS)
				_invaderExploderPS.Emit(50);
		}
	
		// If all invaders have now been killed
		if (_invaderCount<=0 && _levelState==LevelState.Playing)
		{
			// Turn off the mystery alien if its enabled
			if (_mysteryAlien.AlienGameObject)
			{
				_mysteryAlien.AlienGameObject.SetActive(false);
			}

			// Its time for another level so set into the Get Ready state
			// and call the game manager's level complete function (which
			// increases the level and reloads the game scene with the next
			// level's data
			if (_gameManager!=null)
			{
				_levelState= LevelState.GetReady;
				_gameManager.LevelComplete();
			}
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
//#pragma warning restore  0618