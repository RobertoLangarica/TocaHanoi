using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Rotation animation that use the current target position as start point
 **/ 
public class ContinuousRotationAnimation : AnimatedElement {

	public Transform target;
	public float angle;//We expect this to be dynamic
	public AnimationCurve animationCurve;

	private Quaternion startRotation;
	private Quaternion finalRotation;

	//For progress
	private bool animationFinished;
	private float elapsedTime;
	private float inverseDuration;

	protected override void StartAnimation()
	{
		startRotation = target.rotation;
		finalRotation = Quaternion.AngleAxis(angle,Vector3.forward);

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


				target.rotation = Quaternion.Lerp(startRotation,finalRotation,_progress*animationCurve.Evaluate(_progress));

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

		}while(isLoop);

		_isPlaying = false;
	}
}
