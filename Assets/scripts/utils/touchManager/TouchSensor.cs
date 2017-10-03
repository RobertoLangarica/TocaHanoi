using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * Evaluate an notify the status of every Touch
 **/
public class TouchSensor : MonoBehaviour 
{

	//Actions dispatched for every Touch
	public Action<Touch> OnTouchBegan;//TouchBegan(Touch)
	public Action<Touch> OnTouchStationary;//TouchStationary(fingerID)
	public Action<Touch> OnTouchMoved;//TouchMoved(fingerID)
	public Action<Touch> OnTouchEnded;//TouchEnded(fingerID)
	public Action<Touch> OnTouchCanceled;//TouchCanceled(fingerID)

	private Dictionary<int,TouchPhase> previousPhase;//record for the previous phase of every touch

	protected virtual void Start()
	{
		previousPhase = new Dictionary<int, TouchPhase>();
	}

	/**
	 * This function is overrided for mouse binding
	 **/ 
	protected virtual void Update () 
	{
		if(Input.touchCount > 0)
		{
			for(int i = 0; i < Input.touchCount; i++)
			{
				ProcessTouch(Input.GetTouch(i));
			}

		}
	}

	protected void ProcessTouch(Touch touch)
	{
		switch(touch.phase)
		{
			case TouchPhase.Began:
				previousPhase[touch.fingerId] = TouchPhase.Began;

				if(OnTouchBegan != null)
				{
					OnTouchBegan(touch);
				}
			break;

			case TouchPhase.Stationary:
				//Avoid the dispatch of stationary every frame after entering this phase
				if(previousPhase[touch.fingerId] != TouchPhase.Stationary)
				{
					previousPhase[touch.fingerId] = TouchPhase.Stationary;

					if(OnTouchStationary != null)
					{
						OnTouchStationary(touch);
					}
				}
			break;

			case TouchPhase.Moved:
				previousPhase[touch.fingerId] = TouchPhase.Moved;

				//TODO: filter distance to avoid the dispatching of distances with little change
				if(OnTouchMoved != null)
				{
					OnTouchMoved(touch);
				}
			break;

			case TouchPhase.Ended:
				if(OnTouchEnded != null)
				{
					OnTouchEnded(touch);
				}
			break;

			case TouchPhase.Canceled:
				if(OnTouchCanceled != null)
				{
					OnTouchCanceled(touch);
				}
			break;
		}
	}
}
