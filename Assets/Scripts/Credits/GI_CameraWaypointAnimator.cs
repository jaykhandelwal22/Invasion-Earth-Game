// -----------------------------------------------------------------------------------
// FILE	:	GI_CameraWaypointAnimator.cs
// DESC	:	Simple class for animating a camera between a list of waypoint using
//			a user defined animation curve for smooth easein/easeout type 
//			animation.
//
//			Classes
//				GI_CameraWaypointAnimator_Waypoint
//					Represents a single waypoint. It contains a transform describing
//					the position and orientation fo the waypoint in the scene, Move
//					Speed describing the speed at which the camera should move
//					towards the target and AnimationCurve to allow the developer to
//					define the shape of the movement animation.
//
//				GI_CameraWaypointAnimator
//					This the main MonoBehavior derived component of the script. It
//					manages a list of waypoints and update the position of the camera
//					towards the current waypoint. It also allows for the registration
//					of callback methods so another component can recieve notifications
//					of a waypoint change.
//
// -----------------------------------------------------------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// This is the signiture to use for callback functions. Your function
// will be passed the index of the new waypoint.
public delegate void CameraWaypointAnimatorCallback ( int newWaypoint );

// -----------------------------------------------------------------------------------
// Class:	GI_CameraWaypointAnimator_Waypoint
// Desc	:   Represents a single waypoint in the animation system
// -----------------------------------------------------------------------------------
[System.Serializable]
public class GI_CameraWaypointAnimator_Waypoint
{
	// Transform of the waypoint (just use any empty game object and position 
	// it in your scene and then drag it in here using the inspector.
	public Transform		Waypoint		=	null;
	
	// The speed at which to move towards the next waypoint
	public float     		MoveSpeed		=	0.001f;
	
	// Animation Curve used to map the linear interpolator between waypoints into any 
	// curve we desire (good for ease-in / ease-out)
	public AnimationCurve	Curve;
}

// ----------------------------------------------------------------------------------
// Class:	GI_CameraWaypointAnimator
// Desc	:	Stores a list of waypoints and updates the camera position and
//			rotation between them using animation curves
// -----------------------------------------------------------------------------------
public class GI_CameraWaypointAnimator : MonoBehaviour 
{
	// Default Curve for programatically added waypoints
	public AnimationCurve	DefaultCurve;

	// Waypoint list. You can simply change the size of this list in the inspector
	// and then drag and drop any transforms you wish to use for waypoints.
	public List<GI_CameraWaypointAnimator_Waypoint>	CameraWaypoints =	new List<GI_CameraWaypointAnimator_Waypoint>();
	
	// Can be used to call an extrenal function when a waypoint change is about to happen. We store these
	// in a list so that multiple components can each register their own callbacks
	private List<CameraWaypointAnimatorCallback>	Callbacks       = 	new List<CameraWaypointAnimatorCallback>();

	public  float 			WaypointStay	=	1.5f; 	

	// The current transform of the camera being animated
	private Transform		CameraTransform	= 	null;	
	
	// The current target waypoint that we are moving the camera towards.
	private int				CurrentWaypoint =   0;
	
	// The positions and rotation the camera is currently interpolating between. We store
	// the source and target positions and rotations of the waypoints we are animating between.
	private Vector3			SourcePosition	=   Vector3.zero;
	private Vector3			TargetPosition	=	Vector3.zero;
	private Quaternion		SourceRotation	=	Quaternion.identity;
	private Quaternion		TargetRotation	=	Quaternion.identity;
	
	// The interpolator that steps with time from 0.0 to 1.0 between
	// source and target waypoints (defined above).
	private float			Interpolator	=	0.0f;
	
	// A special feature that allows you to push any waypoint in the array
	// into the queue temporarily. So if the camera was currently going from
	// wp 2 to wp 3; you could set this to 2 meaning the when the camera reached
	// wp3 it would go back to wp2 and then carry on as normal. This is only temporary
	// so the system would NOT get stuck in a loop going 3 2 2 3 3 2 2 3. That is to say,
	// we do not overwrite or store this waypoint override in the main waypoint list.
	// I use this in the Tournament Trophy Room when the cameras is scrolling down the
	// list of track photos. If there are more photos still to see when I reach the bottom,
	// I simply flip the board around and instruct the system to jump to the previous waypoint 
	// at the top fo the board.
	private int				NextWaypointOverride	=   -1;
	
	// ------------------------------------------------------------------------------
	// Name	:	Start
	// Desc	:	Setup the source and target values for the first step. The source
	//			is always taken from the main cameras current position so you
	//			can start the sequence from anywhere in the scene by simply placing
	//			your camera there.
	// ------------------------------------------------------------------------------
	//
	void Awake()
	{
		// Store the current transform of the main camera		
		CameraTransform = Camera.main.transform;
		
		// If there is no camera or no camera waypoints ecist then exit
		if (!CameraTransform) 	return;
		if (CameraWaypoints.Count==0) return;
		
		// Make a copy of the camera's starting position and rotation as this will be the source of the first interpolation
		SourcePosition = new Vector3(CameraTransform.position.x, CameraTransform.position.y, CameraTransform.position.z);
		SourceRotation = new Quaternion(CameraTransform.rotation.x, CameraTransform.rotation.y, CameraTransform.rotation.z, CameraTransform.rotation.w);
		
		// Check the first way point is valid
		if (CameraWaypoints[0].Waypoint==null) return;
		
		// Setup the first target position and rotation
		TargetPosition = CameraWaypoints[0].Waypoint.position;
		TargetRotation = CameraWaypoints[0].Waypoint.rotation;
		
		// Set interpolator between source and target to the beginning. When this reaches
		// 1,0, the camera will be at the target waypoint.
		Interpolator = 0.0f;
	}
	
	// ----------------------------------------------------------------
	// Name	:	RegisterWaypointChangeCallback
	// Desc	:	Adds a callback (delegate) to the function list that
	//			will be called as the waypoint is about to change. This
	//			allows external objects to be notified. 
	// ----------------------------------------------------------------
	public void RegisterCameraWaypointChangeCallback( CameraWaypointAnimatorCallback func)
	{
		if (!Callbacks.Contains(func))	Callbacks.Add( func );
	}

	// ----------------------------------------------------------------
	// Name	:	RemoveWaypointChangeCallback
	// Desc	:	Removes a callback from the callback list.
	// ----------------------------------------------------------------
	public void RemoveCameraWaypointChangeCallback( CameraWaypointAnimatorCallback func)
	{
		if (Callbacks.Contains(func)) Callbacks.Remove(func);
	}
	
	// ------------------------------------------------------------------------------
	// Name	:	AddWaypoint
	// Desc	:	Allows other scripts to add waypoints for when the waypoint list
	//			is not known until runtime. This is needed when your do not know
	//			what the waypoints will be until the game is running and therefore can
	//			not add them in the Inspector.
	// ------------------------------------------------------------------------------
	public void AddWaypoint (Transform trans, float speed )
	{
		// Create a new waypoint
		GI_CameraWaypointAnimator_Waypoint newWP = new GI_CameraWaypointAnimator_Waypoint();
		
		// Set the default curve
		newWP.Curve = DefaultCurve;
		
		// Set the speed of this waypoint
		newWP.MoveSpeed = speed;
		
		// Set the position and rotation (transform) of this waypoint int the scene
		newWP.Waypoint = trans;
		
		// Add the new waypoints
		CameraWaypoints.Add(newWP);
	}
		
	
	// ------------------------------------------------------------------------------
	// Name	:	SetWaypointOverride
	// Desc	:	Allows you to set a waypoint override to occur when the current target
	//			waypoint is reached. This allows you to alter the otherwise linear
	//			walkthrough of the waypoint array and instruct the animator to jump
	//			to any point in the waypoint list at the next change. The animation
	//			will then continue normally from that point. The index passed should
	//			be the index of a waypoint that already exists in the waypoint list.
	// ------------------------------------------------------------------------------
	public void SetNextWaypoint ( int index )
	{
		// If index is bogus then return
		if (index<0 || index>CameraWaypoints.Count-1) return;
		
		// Set the waypoint override so it will be processed at the next waypoint
		// change.
		NextWaypointOverride = index; 
	}
	
	// -------------------------------------------------------------------------------
	// Name	:	SetCurrentWaypoint
	// Desc	:	Immediately overrides the current target waypoint with a new one.
	//			The camera will immediately begin heading from its current position
	//			to the new waypoint whose index you specify
	// -------------------------------------------------------------------------------
	public void SetCurrentWaypoint( int index )
	{
		// If the index is bogus return
		if (index<0 || index>CameraWaypoints.Count-1) return;
		
		// Reset the animation interpolator to the beginning
		Interpolator = 0.0f;
		
		// Set the index passed as the new and current target waypoint
		CurrentWaypoint = index;
		
		// Set the camera's current position and orientation at this moment in time to the
		// new source waypoint of the interpolation.
		SourcePosition = new Vector3(CameraTransform.position.x, CameraTransform.position.y, CameraTransform.position.z);
		SourceRotation = new Quaternion(CameraTransform.rotation.x, CameraTransform.rotation.y, CameraTransform.rotation.z, CameraTransform.rotation.w);
		
		// Set the target waypoint to the waypoint whose index was passed.
		TargetPosition = CameraWaypoints[CurrentWaypoint].Waypoint.position;
		TargetRotation = CameraWaypoints[CurrentWaypoint].Waypoint.rotation;
	}
	
	// --------------------------------------------------------------------------------
	// Name	:	AdvanceWaypoint
	// Desc	:	Forces the system to immediately jump to the next waypoint in the list
	//			even if the current target waypoint has not yet been reached
	// --------------------------------------------------------------------------------
	public void AdvanceWaypoint()
	{
		// Increase the current waupoint index with wrap around.
		CurrentWaypoint++;
		if (CurrentWaypoint>CameraWaypoints.Count-1) CurrentWaypoint = 0;
		
		// Reset interpolator
		Interpolator = 0.0f;
	
		// The current camera position is the new interpolator source
		SourcePosition = new Vector3(CameraTransform.position.x, CameraTransform.position.y, CameraTransform.position.z);
		SourceRotation = new Quaternion(CameraTransform.rotation.x, CameraTransform.rotation.y, CameraTransform.rotation.z, CameraTransform.rotation.w);
		
		// The previous NEXT waypoint is the new current target
		TargetPosition = CameraWaypoints[CurrentWaypoint].Waypoint.position;
		TargetRotation = CameraWaypoints[CurrentWaypoint].Waypoint.rotation;
	}
	
	// ------------------------------------------------------------------------------
	// Name	:	Update
	// Desc	:	Handles the update of the interpolator and the setting of the new
	//			camera position. It also takes care of advancing to the next waypoint
	//			when the current one is reached and calling any registered callbacks
	// ------------------------------------------------------------------------------
	void Update () 
	{
		
		// We can only proceed if we have a valid camera transform and a valid target waypoint
		if (CameraTransform!=null && CurrentWaypoint<CameraWaypoints.Count)
		{
			// Increase the Interpolator towards 1.0f by the current target's move speed
			// scaled by delta time.
            if (Interpolator <= 1.0f)
                Interpolator += Time.deltaTime * CameraWaypoints[CurrentWaypoint].MoveSpeed;
            else
                Interpolator += Time.deltaTime;

			// Now that we have the new Interpolator value lets feed into our animation curve
			// to get the remapped t value of the camera at this time.
			float pointOnCurve = CameraWaypoints[CurrentWaypoint].Curve.Evaluate(Mathf.Clamp01 (Interpolator));
			
			// Point on curve now describes the position 
			CameraTransform.position = Vector3.Lerp( SourcePosition, TargetPosition,  pointOnCurve);
			CameraTransform.rotation =  Quaternion.Slerp( SourceRotation, TargetRotation,  pointOnCurve);	
			
			// If Interpolator is greater than 1.0 then we have reached or surpassed the target waypoint
			// and it is time to move to a new waypoint
			if (Interpolator>=WaypointStay+1.0f)
			{				
				// If there is a waypoint override set then set this as the new target
				if (NextWaypointOverride!=-1)
				{	
					// This will be the next target waypoint to head towards
					CurrentWaypoint = NextWaypointOverride;
					
					// Reset the override. Overrides are a one-time deal
					NextWaypointOverride = -1;
				}
				else
					// No override set simply increment the currebt waypoint so
					// the next one in the list becomes the new animation target.
					CurrentWaypoint ++;
				
				// If we are at the end fo the list then wrap around to waypoint zero
				if (CurrentWaypoint>CameraWaypoints.Count-1) CurrentWaypoint = 0;
							
				// Reset the animation interpolator
				Interpolator = 0.0f;
				
				// The old target waypoint becomes the new source waypoint
				SourcePosition = TargetPosition;
				SourceRotation = TargetRotation;
				
				// If the new waypoint is valid
				if (CameraWaypoints[CurrentWaypoint].Waypoint!=null)
				{
					// Set it as the new target to animation the camera towards
					TargetPosition = CameraWaypoints[CurrentWaypoint].Waypoint.position;
					TargetRotation = CameraWaypoints[CurrentWaypoint].Waypoint.rotation;
				}
				
				// Have extrenal components registered any callbacks
				if (Callbacks.Count>0)
				{
					// If so then loop through each registered callback function and
					// call it passing in the index of the new target waypoint.
					foreach(CameraWaypointAnimatorCallback callback in Callbacks)
					{
						callback( CurrentWaypoint);		
					};
				}
			} // End if animation step is over	
			
		}// End if valid inputs
	}
}
