﻿using UnityEngine;
using System.Collections;

public class BallControlLeftScript : MonoBehaviour {

	public float fuerza;
	
	public Rigidbody ball; // Reference to the ball controller.
	
	
	
	void Start()
	{
		ball = GetComponent<Rigidbody>();
		
	}
	
	
	
	
	void FixedUpdate(){
		ball.AddForce(new Vector3(-1,0,0)*fuerza);
		
	}
}
