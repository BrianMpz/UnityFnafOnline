﻿using UnityEngine;

public class Playsound : MonoBehaviour

{
	public void Clicky()
	{
		GetComponent<AudioSource>().Play();
	}


}
