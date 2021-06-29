using UnityEngine;
using System.Collections;

// ---------------------------------------------------------------------------------
// Class	:	Cannon
// Desc		:	This is the script attached to the cannon that the player
//				operates. 
//				The original game seemed to only allow the player to have one
//				projectile active at a time so I have done that here. The Cannon
//				script also manages the player projectile and its re-use.
// ---------------------------------------------------------------------------------
public class Cannon : MonoBehaviour 
{
	// Inspector assigned
	public	float 	  	Speed		 			= 50.0f;			// Max movement speed
	public  GameObject 	ProjectilePrefab		= null;				// Player projectile prefab
	public  AudioClip  	ShootingAudioClip  		= null;				// Sound of cannon firing

	// Internals
	private	AudioSource	ShootingAudioSource		= null;				// This audio source will be created to play the shooting sound
	private Transform 	_myTransform 			= null;				// Cached transform component
	private Vector3  	_myPosition  			= Vector3.zero;		// Current position
	private bool      	_canFire				= true;				// Can the player fire at the moment
	private bool		_canMove				= true;				// Can the player move at the moment
	private Vector3		_startPosition		    = new Vector3(0.0f, -28.79f, 0.0f);

	// Cached scene manager reference
	private SceneManager		_sceneManager			= null;

	// Cached projectile script reference
	private CannonProjectile	_cannonProjectileScript = null;

	// ----------------------------------------------------------------------------
	// Name	:	Start
	// Desc :	Called prior to the first update of the cannon.
	// ----------------------------------------------------------------------------
	void Start () 
	{
		// Cache components and positions
		_myTransform 			= transform;
		_myTransform.position 	= _startPosition;
		_myPosition	 			= _myTransform.position;
		_sceneManager			= SceneManager.instance;

		// Instantiate the player's projectile (which is in a disabled state by default)
		if (ProjectilePrefab!=null)
		{
			GameObject go = Instantiate ( ProjectilePrefab) as GameObject;

			// Cache a reference to its CannonProjectile component/script
			if (go!=null)
			{
				_cannonProjectileScript = go.GetComponent<CannonProjectile>();
			}
		}

		// Create/Add a new AudioSource to our cannon
		ShootingAudioSource = gameObject.AddComponent<AudioSource>();

		// Assign it our shooting clip
		ShootingAudioSource.clip = ShootingAudioClip;

		// Do not play this automatically
		ShootingAudioSource.playOnAwake = false;
	
	}
	
	// ----------------------------------------------------------------------------
	// Name	:	Update
	// Desc	:	Called each frame to process the player's input and update the
	//			position of the cannon.
	// ----------------------------------------------------------------------------
	void Update () 
	{
		// This bool isn't used but there for future support :)
		if (!_canMove) return;

		// If the game is in anything other than a playing state then don't update
		// the cannon's position
		if (_sceneManager!=null && _sceneManager.levelState!=LevelState.Playing) return;

		// Get horizontal Axis reading and mutliply by speed to get our horizontal movement delta 
		float delta = Input.GetAxis("Horizontal") * Speed;

		// Clamp the X position of the cannon so it can not leave the play area
		_myPosition.x = Mathf.Clamp ( _myPosition.x + (delta * Time.deltaTime), -115.0f, 115.0f);

		// Assign our new position to the cannon's transform
		_myTransform.position = _myPosition;

		// If we are currently allowed to fire and we are firing
		if (_canFire && Input.GetButton ("Fire1"))
		{
			// And our one and only projectile is not currently in use
			if (_cannonProjectileScript!=null && !_cannonProjectileScript.isActive)
			{
				// Call the projectile's Fire function specifying the player's position
				// that it should start from.
				_cannonProjectileScript.Fire ( _myPosition );

				// Play the audio clip
				ShootingAudioSource.Play();

				// Start a co-routine that locks our ability to fire unless a certain
				// time has passed. 
				StartCoroutine( LockFire() );
			}
		}
	} 

	// ----------------------------------------------------------------------------
	// Name	:	Reset
	// Desc	:	Called when the player dies and the cannon has to be reset
	//			for use as the next life.
	// ----------------------------------------------------------------------------
	public void Reset()
	{
		// Enable the cannon and set its start position
		gameObject.SetActive (true);
		_myTransform.position = _startPosition;
		_myPosition= _startPosition;

		// We must enable on reset because when the object is deactivated
		// its coroutines are killed. Which means, LockFire may have left
		// _canFire disabled
		_canFire = true;

	}

	// -----------------------------------------------------------------------------
	// Name	:	LockFire - Coroutine
	// Desc	:	Called when the player presses fire to set the _canFire bool
	//			to false for roughly 1/3 of a second. This stops the player having
	//			a rapid fire capability as we only have 1 projectile
	// -----------------------------------------------------------------------------
	IEnumerator LockFire()
	{
		_canFire = false;
		yield return new WaitForSeconds(0.35f);
		_canFire = true;
	}
}