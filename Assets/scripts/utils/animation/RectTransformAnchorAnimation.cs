﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/**
 * Animation for the anchorMax and anchorMin from RectTransform
 **/
public class RectTransformAnchorAnimation : AnimatedElement 
{

	public RectTransform target;
	public Vector2 anchorMinDestination;
	public Vector2 anchorMaxDestination;
	public AnimationCurve animationCurve;

	public Action OnAnimationComplete;

	private Vector2 startMax;
	private Vector2 startMin;

	//For progress
	private bool animationFinished;
	private float elapsedTime;
	private float inverseDuration;

	protected override void StartAnimation()
	{
		startMax = target.anchorMax;
		startMin = target.anchorMin;

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
		float curveValue;
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

				curveValue = animationCurve.Evaluate(_progress);

				target.anchorMax = (anchorMaxDestination-startMax)* curveValue+ startMax;
				target.anchorMin = (anchorMinDestination-startMin)* curveValue+ startMin;

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
