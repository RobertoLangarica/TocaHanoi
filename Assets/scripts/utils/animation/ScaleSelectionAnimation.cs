using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleSelectionAnimation: AnimatedElement
{
	public Transform target;
	public Vector3 intialScaleValue = Vector3.one;
	public Vector3 finalScaleValue = Vector3.one*1.2f;
	public AnimationCurve animationCurve;

	//For progress
	private bool animationFinished;
	private float elapsedTime;
	private float inverseDuration;

	protected override void StartAnimation()
	{
		StartCoroutine("animationUpdate");
	}

	public override void StopAnimation()
	{
		base.StopAnimation();
		StopCoroutine("animationUpdate");
	}

	public override void ResetAnimation ()
	{
		_progress = 0;
		elapsedTime = 0;
		animationFinished = false;
		target.transform.localScale = intialScaleValue;
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
			We ge the inverse here so whenever the duration change on editor 
			we could see the effect on the next animation cycle
			*/
			inverseDuration = 1.0f/duration;

			while(!animationFinished)
			{
				elapsedTime += Time.deltaTime;
				_progress = elapsedTime*inverseDuration;

				target.localScale = Vector3.Lerp(intialScaleValue,finalScaleValue,_progress)*animationCurve.Evaluate(_progress);

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
	}
}
