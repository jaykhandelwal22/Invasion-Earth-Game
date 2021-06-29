using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ForVideo : MonoBehaviour {

	[SerializeField]
	public List<GameObject> objects = new List<GameObject>();
	public float rotateSpeed = 5.0f;
	public float revealTime  = 1.0f;
	private float timer = 0;
	private int nextReveal = 0;


	// Update is called once per frame
	void Update ()
	{timer+=Time.deltaTime;
		if (timer<5 && nextReveal==0) return;

		if (timer>revealTime && nextReveal<18)
		{
			objects[nextReveal].SetActive(true);
			nextReveal++;
			timer = 0.0f;
		}

		transform.Rotate ( new Vector3(0.0f, Time.deltaTime*rotateSpeed, 0.0f));
	}
}
