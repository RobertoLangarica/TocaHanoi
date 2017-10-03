using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Movement animation that use the current target position as start point
 **/ 
public class ContinuousMovementAnimation : AnimatedElement 
{
	public Transform target;
	public Vector3 destination;//We expect this to be dynamic
	public AnimationCurve animationCurve;

	public Action OnAnimationComplete;

	private Vector3 startPosition;

	//For progress
	private bool animationFinished;
	private float elapsedTime;
	private float inverseDuration;

	protected override void StartAnimation()
	{
		startPosition = target.position;

		StartCoroutine("animationUpdate");
	}

	public override void StopAnimation()
	{
		base.StopAnimation();
		StopCoroutine("animationUpdate");
	}

	public override void ResetAnimation ()
	{
		//this animation doesn't do a position reset on the target
		_progress = 0;
		elapsedTime = 0;
		animationFinished = false;
	}

	private IEnumerator animationUpdate()
	{
		do
		{
			if(delay > 0)
			{
				yield return new WaitForSeconds(delay);	
			}

			/*
			We get the inverse here so whenever the duration change on editor 
			we could see the effect on the next animation cycle
			*/
			inverseDuration = 1.0f/duration;

			while(!animationFinished)
			{
				elapsedTime += Time.deltaTime;
				_progress = elapsedTime*inverseDuration;

				//target.position = Vector3.Lerp(startPosition,destination,_progress)*animationCurve.Evaluate(_progress) + startPosition;
				target.position = (destination-startPosition)*animationCurve.Evaluate(_progress) + startPosition;

				if(elapsedTime >= duration)
				{
					animationFinished = true;
				}
				yield return 0;
			}

			if(isLoop)
			{
				//back to the start
				ResetAnimation();
			}
			//If it is finished the we notfy it
			else if(OnAnimationComplete != null)
			{
				OnAnimationComplete();				
			}

		}while(isLoop);

		_isPlaying = false;
	}
}
