using UnityEngine;
using System.Collections;

// ----------------------------------------------------------------------------------
// Class	:	MysteryInvader
// Desc		:	The script that sends the mystery invader across the screen.
// -----------------------------------------------------------------------------------
public class MysteryInvader : MonoBehaviour 
{
	// Inspector Assigned
	public float	Speed		 = 10.0f;			//	Speed at which it moves

	// Cached component references
	private Rigidbody 	 _myRigidBody 	= null;
	private AudioSource	 _myAudioSource	= null;		// Should be added to object (not created programmatically)
	private SceneManager _mySceneManager= null;
	private GameObject   _myGameObject  = null;
	private Transform    _myTransform	= null;

	// --------------------------------------------------------------------------------
	// Name	:	Awake
	// Desc	:	Cache all component references
	// --------------------------------------------------------------------------------
	void Awake()
	{ 
		_myGameObject   = gameObject;
		_myTransform	= transform;
		_myRigidBody 	= GetComponent<Rigidbody>();
		_myAudioSource  = GetComponent<AudioSource>();
		_mySceneManager = SceneManager.instance;
	}

	// -------------------------------------------------------------------------------
	// Name	:	OnEnable
	// Desc	:	The object is always in the scene but only enabled when it is time
	//			for another mystery alien to appear. Therefore, when it is enabled
	//			it must be set back to its start position and velocity and its 
	//			sound effect played.
	// -------------------------------------------------------------------------------
	void OnEnable()
	{
		_myRigidBody.position 	= new Vector3( -237 , 150 , 0);
		_myRigidBody.velocity 	= new Vector3( Speed , 0 , 0);
		_myAudioSource.Play ();
	}

	// -------------------------------------------------------------------------------
	// Name	:	FixedUpdate
	// Desc	:	Test for the Mystery Invader passing off the right hand side of the
	//			screen. If it has, then disable the Game Object.
	// -------------------------------------------------------------------------------
	void FixedUpdate()
	{
		if (_myRigidBody.position.x>=247.0f) 
		{
			_myGameObject.SetActive(false);

		}
	}

	// -------------------------------------------------------------------------------
	// Name	:	OnDisable
	// Desc	:	Called when the object is destroyed.
	// -------------------------------------------------------------------------------
	void OnDisable()
	{
		// Set velocity to zero (why? dunno I just did it P)
		_myRigidBody.velocity = new Vector3( 0 , 0 , 0);

		// Stop the Mystery Invader alarm sound effect
		_myAudioSource.Stop ();

		// If the scene manager is present
		if (_mySceneManager!=null)
		{
			// If we are playing and the x position is still on screen then it must mean this
			// has been turned off because the player's projectile hit it so do some additional
			// stuff
			if (_myTransform.position.x<270.0f && _mySceneManager.levelState==LevelState.Playing)
			{
				// Calculate the points to award based on offset from center of screen.
				float points = 100.0f - Mathf.Clamp (Mathf.Abs (_myTransform.position.x)/2.0f, 0.0f,60.0f);

				// If over 91 then clamp to 100 so we have a wider band of pixels in the center where
				// 100 points can be achieved
				if (points>91) points = 100.0f;

				// Tell the scene manager an invader has been hit so they can be awarded the points.
				// We also pass the position so particles can be emitted at that location.
				// -1 for the first parameter = - this is a "Mystery" Invader
				_mySceneManager.RegisterHit( -1 ,(int)points, _myTransform.position);
			}
		}
	}
}
