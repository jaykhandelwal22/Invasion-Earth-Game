using UnityEngine;
using System.Collections;

// --------------------------------------------------------------------------------
// Class	: CannonProjectile
// Desc		: The script that is attached to the player's one and only projectile
// --------------------------------------------------------------------------------
public class CannonProjectile : MonoBehaviour
{
	// Inspector Assigned
	public 	float 			Speed 			= 100.0f;			// Speed the projectile moves updward
	public  SphereCollider  BlastRadiusTrigger 	= null;			// Reference to the sphere collider used for bunker destruction

	// Cached components
	private Vector3     	_myVelocity  	= Vector3.zero;
	private Renderer    	_myRenderer  	= null;
	private Rigidbody		_myRigidbody	= null;
	private Transform       _myTransform    = null;
	private SceneManager 	_mySceneManager	= null;

	// is this projectile active (used by the Cannon to determine
	// if this projectile is ready to be fired again)
	private bool 			_isActive		= false;
	public  bool 			isActive
	{
		get{ return _isActive;}
	}

	// The bullets regular collider/trigger
	private Collider    _myCollider		= null;

	// ------------------------------------------------------------------------------
	// Name	:	Start
	// Desc	:	Initialize 
	// ------------------------------------------------------------------------------
	void Start()
	{
		// Cache components
		_isActive 				= false;
		_myVelocity 			= new Vector3(0.0f, Speed , 0.0f); 
		_myRigidbody 			= GetComponent<Rigidbody>();
		_myRigidbody.velocity	= Vector3.zero;
		_myRenderer				= GetComponent<Renderer>();
		_myRenderer.enabled  	= _isActive;
		_myCollider				= GetComponent<Collider>();
		_myCollider.enabled  	= _isActive;
		_myTransform			= transform;
		_mySceneManager = SceneManager.instance;

		// Turn off blast trigger until we hit a bunker
		if (BlastRadiusTrigger!=null) 
			BlastRadiusTrigger.enabled = false;
	}

	// -------------------------------------------------------------------------------------------
	// Name	:	FixedUpdate
	// Desc	:	Test if the player's projectile has gone off the top of the screen and if so
	//			disable it.
	// -------------------------------------------------------------------------------------------
	void FixedUpdate () 
	{
		if (_myRigidbody.position.y >140) 
		{
			DisableProjectile();
		}
	}

	// -------------------------------------------------------------------------------------------
	// Name	:	Fire
	// Desc	:	Called by the Cannon behaviour when the user presses fire button.
	// -------------------------------------------------------------------------------------------
	public void Fire( Vector3 pos)
	{
		// If bullet is already in use return and do nothing
		if (_isActive) return;

		// Turn the bullet on by enabling the renderer and the collider
		_myCollider.enabled = _myRenderer.enabled = true;

		// Set its active state
		_isActive=true;

		// Set position and velocity
		_myTransform.position = pos;
		_myRigidbody.velocity = _myVelocity;

		// Disable blast radius trigger initially
		if (BlastRadiusTrigger!=null)
			BlastRadiusTrigger.enabled = false;
	}

	// --------------------------------------------------------------------------------------------
	// Name	: 	OnTriggerEnter
	// Desc	:	Called when either an invader or a bunker cube enters this projectiles regular
	//			trigger.
	// --------------------------------------------------------------------------------------------
	void OnTriggerEnter( Collider col )
	{
		// If we are not in a playing state (sych as the player has just been destroyed)
		// then ignore the request. Missiles fired after the player is killed don't count.
		if (_mySceneManager!=null && _mySceneManager.levelState!=LevelState.Playing) return;

		// Kill the object we have hit (either invader or base cube)
		col.gameObject.SetActive (false);

		_myCollider.enabled=false;
		_myRenderer.enabled=false;

		// Has it hit a bunker - if so spawn blast radius trigger
		// so bunkers dissolve more organically
		if (col.gameObject.layer == LayerMask.NameToLayer("Bases"))
		{
			// Turn on the blast radius trigger  
			if (BlastRadiusTrigger!=null) 
			{
				// Randomly choose the exact radius for more random looking
				// bunker damage
				BlastRadiusTrigger.radius  = Random.Range ( 3.0f, 8.0f);
				BlastRadiusTrigger.enabled = true;
			}

			// A base hit has happend so tell scene manager so it can
			// spawn a base explosion
			if (_mySceneManager!=null)
			{
				_mySceneManager.RegisterBaseHit( _myTransform.position );
				_myCollider.enabled = false;
			}
		}

		// Turn off the projectile
		Invoke("DisableProjectile", 0.1f);

	}

	// -------------------------------------------------------------------------------------------
	// Name	: Disable Projectile
	// Desc	: Reset the projectile into its default (unfired) state
	// -------------------------------------------------------------------------------------------
	void DisableProjectile()
	{
		_isActive = false;
		_myRenderer.enabled = false;
		_myCollider.enabled = false;
		_myRigidbody.velocity = Vector3.zero;
		if (BlastRadiusTrigger!=null)
			BlastRadiusTrigger.enabled = false;
	}
}
