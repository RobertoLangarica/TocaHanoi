using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AnimatedElement : MonoBehaviour
{
	public string animationName;//just for visual aid on the editor
	public float duration;
	public float delay;
	public bool isLoop;

	protected bool _isPlaying = false;
	protected float _progress;//progress from 0 to 1

	protected abstract void StartAnimation();
	public abstract void ResetAnimation();

	/**
	 * Play the animation always from the start
	 **/ 
	public void Play()
	{
		if(_isPlaying)
		{
			StopAnimation();
			ResetAnimation();
		}
		else
		{
			ResetAnimation();
		}

		_isPlaying = true;
		StartAnimation();
	}

	/**
	 * Plays the animation from the current status
	 **/ 
	public void Resume()
	{
		//only works if it is not playing already
		if(!_isPlaying)
		{
			_isPlaying = true;
			StartAnimation();
		}
	}

	public virtual void StopAnimation()
	{
		_isPlaying = false;
	}

	public float progress
	{
		get{return _progress;}
		//TODO set progress
	}

	public bool isPlaying
	{
		get{return _isPlaying;}
	}
}
