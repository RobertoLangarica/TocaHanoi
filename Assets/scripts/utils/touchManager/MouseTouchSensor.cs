using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Mouse event binding as Touch 
 **/
public class MouseTouchSensor : TouchSensor 
{
	private Touch currentTouch;

	override protected void Start()
	{
		base.Start();

		//Binded touch
		currentTouch = new Touch();
		currentTouch.phase = TouchPhase.Ended; //The ended phase do nothing
	}

	override protected void Update () 
	{
		#if !UNITY_STANDALONE && !UNITY_EDITOR
		//We use the standar version of TouchSensor::Update
		base.Update();
		return;
		#endif

		//Desktop mouse binding

		//left button
		if(Input.GetMouseButton(0))
			
		{
			if(currentTouch.phase == TouchPhase.Ended)
			{
				//First time
				currentTouch = new Touch();
				currentTouch.position = new Vector2(Input.mousePosition.x,Input.mousePosition.y);
				currentTouch.deltaPosition = Vector2.zero;
				currentTouch.phase = TouchPhase.Began;
			}
			else
			{
				if(currentTouch.position == (Vector2)Input.mousePosition)
				{
					//Not moving
					currentTouch.phase = TouchPhase.Stationary;
					currentTouch.deltaPosition = Vector2.zero;
				}
				else
				{
					//Moving
					currentTouch.phase = TouchPhase.Moved;
					currentTouch.deltaPosition = (Vector2)Input.mousePosition - currentTouch.position;
					currentTouch.position = new Vector2(Input.mousePosition.x,Input.mousePosition.y);
				}
			}

			ProcessTouch(currentTouch);
		}
		//if there is a release then we need to end the touch
		else if(currentTouch.phase != TouchPhase.Ended)
		{
			//Mouse release
			currentTouch.phase = TouchPhase.Ended;

			ProcessTouch(currentTouch);
		}
	}
}
