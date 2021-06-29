using UnityEngine;
using System.Collections;

// ------------------------------------------------------------------------------
// Class	:	HighscoreGUI
// Desc		:	Is the component that handles GUI display and Input for the Input
//				of the player's name into the HighScore table.
// ------------------------------------------------------------------------------
public class HighscoreGUI : MonoBehaviour 
{
	// Skin with custom font - Inspector Assigned
	public GUISkin Skin = null;

	// Default Entry
	private string def = "gaz";

	// --------------------------------------------------------------------------
	// Name	:	Start
	// Desc	:	Reset any previous input that is in the buffer.
	// --------------------------------------------------------------------------
	void Start () 
	{
		Input.ResetInputAxes();
	}

	// --------------------------------------------------------------------------
	// Name : OnGUI
	// Desc	: Display and Process the Input Text field for getting the name
	//		  of the player
	// --------------------------------------------------------------------------
	void OnGUI()
	{
		// Enable display of the mouse
		Cursor.visible=true;

		// Set the GUI skin to the one we assigned via the inpector
		GUI.skin = Skin;

		//GUI.FocusControl ("MyTextField"); // Disabled because of Unity 4 bug

		//Set up scaling matrix so we can work in EASY coordinates
		float rx = Screen.width / 1920.0f;
		float ry =  Screen.height/1080.0f;
		
		// Backup the current GUI matrix as we are about to change it so we can work
		// in standard coordinates for all resolutions
		Matrix4x4 oldMat = GUI.matrix;
		
		// Set up the scaling matrix
		GUI.matrix = Matrix4x4.TRS (new Vector3(0, 0, 0), Quaternion.identity, new Vector3 (rx, ry, 1));	

		// Output some text next to the Input Text field
		GUI.Label (new Rect(750,800,600,100),"Input your initials" );
		GUI.Label (new Rect(700,1025,600,100),"Press Enter To Submit" );

		// Display name Input box
		//GUI.SetNextControlName ("MyTextField");// Disabled because of Unity 4 bug
		def = GUI.TextField( new Rect(805,850, 310, 150), def.TrimStart(), 3).ToUpper();

		// If we key has been pressed
		if (Event.current.isKey) 
		{	
			// Find out which key
			switch (Event.current.keyCode) 
			{
				// Was it the return key. If so then submit the name
				// t the game manager so the game manager can store 
				// it in the highscore table along with the player's
				// current score.
				case KeyCode.Return:
				if (GameManager.instance)
					GameManager.instance.SetPlayerName(def);

				// let the system know we have processed this key event
				// so no-one else does
				Event.current.Use();
				
				break;
			}
				
		}

		// Restore the matrix
		GUI.matrix = oldMat;	
	
	
	}
}
