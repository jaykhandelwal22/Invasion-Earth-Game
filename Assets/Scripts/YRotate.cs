using UnityEngine;
using System.Collections;

public class YRotate : MonoBehaviour 
{
	public float Speed = 1.0f;

	private Transform _myTransform = null;

	// Use this for initialization
	void Start () 
	{
		_myTransform = transform;
	}
	
	// Update is called once per frame
	void Update ()
	{
		_myTransform.Rotate(0.0f, Speed*Time.deltaTime, 0.0f);
	}
}
