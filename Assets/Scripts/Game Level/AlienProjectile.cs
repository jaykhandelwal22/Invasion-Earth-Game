using UnityEngine;
using System.Collections;

// ----------------------------------------------------------------------------------------------------
// Class	:	AlienProjectile
// Desc		:	This is the behavior that is attached - and controls - the missiles that the aliens
//				and the mystery aliens fire at the player. These are instantiated in a pool by
//				the scene manager and are activated and positioned by the scene manager by
//				calling this behavior's Fire function.
// ----------------------------------------------------------------------------------------------------
public class AlienProjectile : MonoBehaviour
{
		// Public Fields assigned via inspector
		public 	float 			Speed = 100.0f;							// Speed the bullet should move down the screen (inspector assigned)
		public  SphereCollider  BlastRadiusTrigger = null;				// Sphere collider that will be enabled if the missle hits a bunker
																		// to destroy a random number of bunker sub-cubes. This trigger is
																		// disabled by default and only turned on when a bunker is hit.

		// State controllers
		private Vector3			_myPosition = Vector3.zero;		// Current position of projectile
		private Vector3     	_myVelocity = Vector3.zero;		// Current velocity of projectile

		// Cached components
		private Renderer    	_myRenderer = null;					// Renderer
		private Rigidbody		_myRigidbody = null;				// Rigidbody
		private SceneManager 	_mySceneManager = null;				// Scene Manager
		private Material        _myMaterial = null;					// Material instance reference. This is required because the mystery alien
																	// alters the color of the projectile's material so it is yellow instead
																	// of white.
		private float           _sizeMultiplier = 1.0f;				// Multiplier allowing you to increase the blast/damage radius used when
		// when a bunker is hit

		private bool 			_isActive = false;					// Is this projectile currently active and in use. This is required because
																	// the projectiles are all instantiated at scene startup and stored inactive
																	// in a pool. The scene manager - when a new projectile is needed - scans the pool
																	// for a projectile that is inactive
		public  bool 			isActive {								// Public property for above state
				get{ return _isActive;}
		}
	
		private Collider    _myCollider = null;						// Main projectile trigger used to detect collisions with bunkers and player

		// ---------------------------------------------------------------------------------------------
		// Name	:	Start
		// Desc	:	Setup the default values of the projectile
		// ---------------------------------------------------------------------------------------------
		void Start ()
		{
				// Cache components and set initial values
				_isActive = false;
				_myVelocity = new Vector3 (0.0f, -Speed, 0.0f); 
				_myPosition = new Vector3 (0, - 30, 0);
				_myRigidbody = GetComponent<Rigidbody>();
				_myRigidbody.velocity = Vector3.zero;
				_myRenderer = GetComponent<Renderer>();
				_myRenderer.enabled = _isActive;
				_myCollider = GetComponent<Collider>();
				_myCollider.enabled = _isActive;
				_mySceneManager = SceneManager.instance;
				_myMaterial = GetComponent<Renderer>().material;

				// Blast Radius sphere trigger is off by default. We only turn it on
				// if main trigger registers a hit against a bunker.
				if (BlastRadiusTrigger != null) 
						BlastRadiusTrigger.enabled = false;

		}
	
		// ----------------------------------------------------------------------------------------------
		// Name	:	FixedUpdate
		// Desc	:	Tests the Y position of the projectile and turn it off if it goes off the bottom
		//			of the screen.
		// ----------------------------------------------------------------------------------------------
		void FixedUpdate ()
		{
				// Is it time to turn it off
				if (_myRigidbody.position.y < -35) 
				{
						// Call DisableProjectile function that Re-registers the projectile
						// as being inactive so it can be selected from the pool again
						// by the scene manager in the future.
						DisableProjectile ();
				}
		}

		// ----------------------------------------------------------------------------------------------
		// Name	:	Fire
		// Desc	:	This is called by the SceneManager to activate a new projectile from the pool.
		//			It is passed the position we would like to fire from (position of the invader
		//			that fired it) and a boolean indicating if this is being fired from the
		//			mystery invader - in which case we change its color.
		// ----------------------------------------------------------------------------------------------
		public void Fire (Vector3 pos, bool mysteryBomb = false)
		{
				// Missile is in use so return
				if (_isActive)
						return;

				// If this is being fired from the mystery invader then set the diffuse color
				// to yellow and use a sphere collider multiplier of 2.0 so we do twice as
				// much damage - potentially - to a bunker.
				if (mysteryBomb) 
				{
						_myMaterial.color = Color.yellow;
						_sizeMultiplier = 2.0f;
				}
		    // Otherwise make the projectile regular white color and set blast radius multiplier to standard
			else 
				{
						_myMaterial.color = Color.white;
						_sizeMultiplier = 1.0f;
				}

				// We are now turning this projectile on so enable its main collider annd renderer.
				_myCollider.enabled = _myRenderer.enabled = true;

				// This projectile is now in use
				_isActive = true;

				// Set the position and velocity of the rigidbody
				_myRigidbody.position = pos;
				_myRigidbody.velocity = _myVelocity;

				// Disable blast radius trigger by default. This is only activated when it hits
				// a bunker.
				if (BlastRadiusTrigger != null)
						BlastRadiusTrigger.enabled = false;
		}

		// --------------------------------------------------------------------------------------------------
		// Name	:	OnTriggerEnter
		// Desc	:	Called by Unity when the projectile hits something - this is called for the projectile's
		//			trigger NOT the blast radius trigger which is a seperate object
		// --------------------------------------------------------------------------------------------------
		void OnTriggerEnter (Collider col)
		{

				// If a null collider is passed return (should never happen)
				if (col == null)						return;			

				// If we are not in a playing state (such as the player is already dead OR
				// ALL invaders are dead) then return;
				if (_mySceneManager!=null && _mySceneManager.levelState!=LevelState.Playing) return;
				
				_myCollider.enabled=false;
				_myRenderer.enabled=false;

				// Has it hit a bunker - if so activate blast radius trigger
				// so bunkers dissolve more organically
				if (col.gameObject.layer == LayerMask.NameToLayer ("Bases")) 
				{
						// Disable the base cube the projectile has directly hit
						col.gameObject.SetActive (false);

						// Activate the blast trigger - if it has been assigned via the inspector
						if (BlastRadiusTrigger != null) {
								// Set the radius of the blast trigger randomly - but using the size multiplier
								// so we get bigger potential radius for mystery invader fired projectiles.
								BlastRadiusTrigger.radius = Random.Range (3.0f, 9.0f * _sizeMultiplier);

								// Enable this puppy and start collecting bunker cubes. These these cubes will
								// be collected in the BlastRadius script attached to the sphere collider.
								BlastRadiusTrigger.enabled = true;
						}

						// If the scene manager is present then let it know that the missle has impacted a bunker.
						// This allows the scene manager to emit a particle explosion and play an explosion sound
						// at the hit position.
						if (_mySceneManager != null) {
								_mySceneManager.RegisterBaseHit (_myRigidbody.position);
						}
			 
						// Wait a tenth of a second before disabling the projectile to give it a chance to travel a little
						// bit with the sphere trigger enabled to give it time to collect some cubes and do some damage as
						// it travels through the bunker.
						Invoke ("DisableProjectile", 0.1f);
		
				}
					// If it isn't a bunker it mush be the player's cannon as only those two layers are collidable with
					// with the alien projectile layer.
				else 
				{
						// Tell scene manager we have been hit. This will decrement our number of lives and potentially lead
						// to a game over state. Will also emit a player explosion, disable the cannon and play an
						// explosion sound effect
						_mySceneManager.PlayerHit (_myRigidbody.position);

						// Disable the projectile as its job is done
						DisableProjectile ();
				}
		}

		// -----------------------------------------------------------------------------------------------------------
		// Name	:	DisableProjectile
		// Desc	:	Set its active boolean to false so it can be selected from the pool again in the future. Also,
		//			reset its state ready for reuse. Disable colliders/triggers and renderer.
		// -----------------------------------------------------------------------------------------------------------
		void DisableProjectile ()
		{
				// Reset state ready for re-use
				_isActive = false;
				_myRenderer.enabled = false;
				_myCollider.enabled = false;
				_myRigidbody.velocity = Vector3.zero;
				_myRigidbody.position = _myPosition;

				// Tell scene manager that it can decrement it number of active projectiles
				_mySceneManager.RegisterAlienProjectile ();

				// Disable blast radus trigger
				if (BlastRadiusTrigger != null) 
						BlastRadiusTrigger.enabled = false;
		}

}
