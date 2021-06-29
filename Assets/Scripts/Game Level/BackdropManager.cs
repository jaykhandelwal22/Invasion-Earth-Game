using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// -------------------------------------------------------------------------------------------------------
// Class	:	BackdropMaterial
// Desc	:		An entry in the BackdropManager's internal list which describes the
//				the material to use and the texture resource that should be loaded for each level to be
//				displayed on the baccdrop
// --------------------------------------------------------------------------------------------------------
[System.Serializable]
public class BackdropMaterial
{
	public Material 	Mat 	= null;		// Inspector Assigned Material to use for this entry
	public string		TexName = null;		// Name of texture to load - should be in a Assets/Resources folder.
}

// --------------------------------------------------------------------------------------------------------
// Name	:	BackdropManager
// Desc	:	Simple script that is attached to a quad (backdrop) that displays a different background image
//			and material depending on the current level of the player. Use the inspector to set the
//			entries for each level.
// --------------------------------------------------------------------------------------------------------
public class BackdropManager : MonoBehaviour
{
	// Inspector Assigned Members
	public List <BackdropMaterial> Materials = new List<BackdropMaterial>();

	// GameManager reference
	private GameManager _gameManager = null;

	// Reference to the texture2D object that holds the current texture being displayed on the backdrop
	private Texture2D   _backdropTex = null;

	// --------------------------------------------------------------------------------------------------
	// Name : 	Start
	// Desc	:	Called at scene startup to load and assign the correct texture and material to the
	//			backdrop object (could be either a cube, plane or a quad).
	// --------------------------------------------------------------------------------------------------
	void Start () 
	{
		// Get Game Manager
		_gameManager = GameManager.instance;

		// If we have a game manager and we some entries in our BackdropMaterial array
		if (_gameManager && Materials.Count>0)
		{
			// fetch the current level the player is on clamping the result to the last
			// entry in the material list. 
			int level = Mathf.Min ( _gameManager.currentLevel, Materials.Count-1 );

			// Fetch the correct material from the list using the current level index 
			BackdropMaterial bdMat = Materials[level];

			// If its been assigned a valid value
			if (bdMat.Mat!=null)
			{
				// Set the renderer's material to this material.
				GetComponent<Renderer>().material = bdMat.Mat;

				// Textures are stored in the Assets/Resources/Textures folder. 
				_backdropTex = Resources.Load ( "Textures/"+bdMat.TexName) as Texture2D;

				// Set the texture of the material to the one just loaded.
				if (_backdropTex)
				{
					GetComponent<Renderer>().material.SetTexture("_MainTex", _backdropTex);
				}
			}
		}
	}

    void OnDisable()
    {
        if (_backdropTex)
        {
            GetComponent<Renderer>().material.mainTexture = null;
            Resources.UnloadAsset(_backdropTex);
            _backdropTex = null;
        }
    }
}
