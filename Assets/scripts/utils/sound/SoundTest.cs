using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SoundTest : MonoBehaviour 
{
	public SoundManager manager;
	public InputField musicInput;
	public InputField fxInput;

	[Serializable]
	public struct SliderTxt
	{
		public Slider slider;
		public Text valueText;
	}

	public SliderTxt[] slidersMusic;//0 volume, 1 fadeIn, 2 FadeOut
	public SliderTxt[] slidersFX;//0 volume, 1 fadeIn, 2 FadeOut
	public SliderTxt[] slidersMasterVolume;//0 master, 1 music, 2 fx

	void Start()
	{
		UpdateValuesFromSlider();	
	}

	//*****MUSIC***********//
	public void PlayCurrentMusic()
	{manager.PlayMusic(currentMusic,new SoundConfiguration(){volume=musicVolume,fadeInTime=musicFadeIn,fadeOutTime=musicFadeOut});}

	public void PauseCurrentMusic()
	{manager.PauseSound(currentMusic);}

	public void ResumeCurrentMusic()
	{manager.ResumeSound(currentMusic);}

	public void StopCurrentMusic()
	{manager.StopSound(currentMusic,false);}

	public void StopCurrentMusicWithFade()
	{manager.StopSound(currentMusic,true);}
	//*********************//


	//*****FX***********//
	public void PlayCurrentFX()
	{manager.PlayFX(currentFX,false,new SoundConfiguration(){volume=fxVolume,fadeInTime=fxFadeIn,fadeOutTime=fxFadeOut});}

	public void PauseCurrentFX()
	{manager.PauseSound(currentFX);}

	public void ResumeCurrentFX()
	{manager.ResumeSound(currentFX);}

	public void StopCurrentFX()
	{manager.StopSound(currentFX,false);}

	public void StopCurrentFXWithFade()
	{manager.StopSound(currentFX,true);}
	//*********************//

	public void UpdateValuesFromSlider()
	{
		//Every text in the SliderTxt gets updated so it will reflex the slider value
		foreach(SliderTxt s in slidersMusic)
		{s.valueText.text = s.slider.value.ToString("#.##");}

		foreach(SliderTxt s in slidersFX)
		{s.valueText.text = s.slider.value.ToString("#.##");}

		foreach(SliderTxt s in slidersMasterVolume)
		{s.valueText.text = s.slider.value.ToString("#.##");}

		//for currentMusic/FX we update the values
		SoundObject sound = manager.GetSoundByName(currentMusic);
		if(sound!= null)
		{
			sound.volume = slidersMusic[0].slider.value;
			sound.fadeInTime = slidersMusic[1].slider.value;
			sound.fadeOutTime = slidersMusic[2].slider.value;
		}

		sound = manager.GetSoundByName(currentFX);
		if(sound!= null)
		{
			sound.volume = slidersFX[0].slider.value;
			sound.fadeInTime = slidersFX[1].slider.value;
			sound.fadeOutTime = slidersFX[2].slider.value;
		}


		//We update the master volumes
		manager.masterVolume = masterVolume;
		manager.musicVolume = masterMusicVolume;
		manager.fxVolume = masterFXVolume;
	}

	//target: master(0),fx(1),music(2)
	public void FadeIn(int target)
	{
		float volume = 1.0f;
		float duration = 1.5f;
		FadeVolumeTo(target,volume,duration);
	}

	//target: master,fx,music
	public void FadeOut(int target)
	{
		float volume = 0f;
		float duration = 1.5f;
		FadeVolumeTo(target,volume,duration);
	}

	//target: master(0),fx(1),music(2)
	private void FadeVolumeTo(int target,float volume,float duration)
	{
		switch(target)
		{
			case 0:
				//Master
				manager.FadeMasterVolume(volume,duration);
			break;

			case 1:
				//FX
				manager.FadeFXVolume(volume,duration);
			break;

			case 2:
				//Music
				manager.FadeMusicVolume(volume,duration);
			break;
		}
	}

	//target: master(0),fx(1),music(2)
	public void FadeAndStop(int target)
	{
		float duration = 1.5f;

		switch(target)
		{
			case 0:
				//Master
					manager.FadeOutAndStopAll(duration);
			break;

			case 1:
				//FX
				manager.FadeOutAndStopFX(duration);
			break;

			case 2:
				//Music
				manager.FadeOutAndStopMusic(duration);
			break;
		}
	}

	//*******VALUE SHORTCUTS************//
	public string currentMusic
	{	get{return musicInput.text;}}

	public string currentFX
	{	get{return fxInput.text;}}

	public float fxVolume
	{	get{return slidersFX[0].slider.value;}}

	public float fxFadeIn
	{	get{return slidersFX[1].slider.value;}}

	public float fxFadeOut
	{	get{return slidersFX[2].slider.value;}}

	public float musicVolume
	{	get{return slidersMusic[0].slider.value;}}

	public float musicFadeIn
	{	get{return slidersMusic[1].slider.value;}}

	public float musicFadeOut
	{	get{return slidersMusic[2].slider.value;}}

	public float masterVolume
	{	get{return slidersMasterVolume[0].slider.value;}}

	public float masterMusicVolume
	{	get{return slidersMasterVolume[1].slider.value;}}

	public float masterFXVolume
	{	get{return slidersMasterVolume[2].slider.value;}}
}
