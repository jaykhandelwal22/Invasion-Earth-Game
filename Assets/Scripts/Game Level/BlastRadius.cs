using UnityEngine;
using System.Collections;

// ----------------------------------------------------------------------------------
// Class	:	BlastRadius
// Desc		:	This script should be attached to the sphere trigger attached
//				to a projectile. it simply turns off ANY objects that fall
//				within the trigger. As this is sensitive to only the BUNKER layer,
//				this will collect any sub cubes and destroy them.
// ----------------------------------------------------------------------------------
public class BlastRadius : MonoBehaviour 
{
	void OnTriggerEnter( Collider col)
	{
		col.gameObject.SetActive (false);
	}

}
