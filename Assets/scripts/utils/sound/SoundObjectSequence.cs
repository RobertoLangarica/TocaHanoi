using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * Implementation of SoundObject that use his clips as a sequence.
 * 
 * When SoundObjectSequence is played it will play one clip at a time in the provided order
 * and notify the stop only when the last clip ended;
 **/ 
public class SoundObjectSequence : SoundObject 
{
	/**
	 * Starts playing the first clip.
	 **/ 
	override public void Play(SoundConfiguration configurations)
	{
		if(isEmpty())
		{
			//nothing to play
			Debug.LogWarning("There is no AudioClip on: "+soundName+" to play.");
			return;
		}

		if(configurations != null)
		{
			volume = configurations.volume < 0 ? volume:configurations.volume;
			fadeInTime = configurations.fadeInTime < 0 ? fadeInTime:configurations.fadeInTime;
			fadeOutTime = configurations.fadeOutTime < 0 ? fadeOutTime:configurations.fadeOutTime;	

			if(configurations.loopHasChanged())
			{	isLoop = configurations.getLoopValue();}
		}

		PlayClipAt(0,true);
	}

	override protected void PrepareAwaitForCompletion()
	{
		//Wait for completion
		float waitTime = audioSource.clip.length - audioSource.time;


		//if the audio is a loop we ignore the fadeOut, also if this isn't the last clip we ignore the fadeOut
		if(fadeOutTime > 0 && waitTime > fadeOutTime && !isLoop && currentPlayingClipIndex == clips.Length-1)
		{
			waitTime-= fadeOutTime;
		}

		//Cancel any previous wait so every play has it own
		CancelInvoke("OnAudioBeforeFadeOut");
		Invoke("OnAudioBeforeFadeOut",waitTime);
	}

	/**
	 * We use AudioCompleted to properly loop the sound
	 **/ 
	override protected void AudioCompleted()
	{
		//This was the las clip
		if(currentPlayingClipIndex == clips.Length-1)
		{
			//We start with the first sound is this is a loop
			if(isLoop)
			{
				PlayClipAt(0,false);
			}
			else
			{
				//Stop
				Stop(false);	
			}
		}
		else
		{

			PlayClipAt(++currentPlayingClipIndex,false);
		}
	}
}
