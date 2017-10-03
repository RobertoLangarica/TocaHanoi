using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour 
{
	/**
	 * Audio Mixer with 3 groups arranged as follows:
	 * Master
	 * 	-FX
	 * 	-Music
	 * 
	 * (Group master as parent of FX and Music)
	 * 
	 * The volume should be exposed for script access for every group using the corresponding names:
	 * masterVolume
	 * fxVolume
	 * musicVolume	
	 **/ 
	public AudioMixer mixer;
	public AudioMixerGroup fxGroup;
	public AudioMixerGroup musicGroup;

	//TODO write an editor tool to avoid this
	public  bool FXAllowed = true;//Maybe the user dowsn't want FX
	private bool _FXAllowed = true;//for internal control of the above
	public bool MusicAllowed = true;//Maybe the user dowsn't want Music
	private bool _MusicAllowed = true;//for internal control of the above


	/**
	 * TODO: Some better inspector drawing
	 * We want to adrees every object by name and for better initialization in the Editor
	 * we use this structure;
	 **/ 
	[Serializable]
	public struct SoundNameCouple
	{
		public string name;
		public SoundObject sound;
	}
	[Header("Sound Clips")]
	//Sounds
	public List<SoundNameCouple> clips;

	/*
	 We will manage the sounds using Dictionary<,> instead of List<> for performance
	reasons since we play sounds by name and a item search in Dictionary is faster than a search in a
	List. If there will be alot of sounds in the game then this will payoff.

	Downsides: Since we take the clips List<> and populate the sounds Dictionary<,> at start. Every new clip 
	added or remove from clips wil not be reflect in SoundManager.

	TODO: Solve the Downside listed by adding a way for dynamically add/remove.
	 */
	private Dictionary<string,SoundObject> sounds;

	/*
	Structure to save any playing sound distinguished by MixerGroup and by name.

	Note: Once again we use Dictionary expecting a hevy load of searches
	 */
	private Dictionary<AudioMixerGroup,Dictionary<string,SoundObject>> playingSounds;

	private Coroutine CFadeMaster;//Coroutine reference for control
	private Coroutine CFadeFX;//Coroutine reference for control
	private Coroutine CFadeMusic;//Coroutine reference for control

	//Struct used to pass params to the FadeVolume Coroutine
	public struct FadeVolumeConfiguration
	{
		public float volume;
		public float starterVolume;
		public float duration;
		public string mixerProperty;//Name of the exposed property in the mixer
		public Action<bool> OnCompleteStop;//A little hardcoding to make the FadeAndStop works
	}

	private float _masterVolume;
	private float _fxVolume;
	private float _musicVolume;

	void Awake()
	{
		//We populate the sounds Dictionary using the clips couples
		sounds = new Dictionary<string, SoundObject>();
		foreach(SoundNameCouple couple in clips)
		{
			sounds.Add(couple.name,couple.sound);
			//Making the names match
			couple.sound.soundName = couple.name;
		}

		//For the sounds currently playing
		playingSounds = new Dictionary<AudioMixerGroup, Dictionary<string, SoundObject>>();
		playingSounds.Add(musicGroup,new Dictionary<string, SoundObject>());
		playingSounds.Add(fxGroup,new Dictionary<string, SoundObject>());

		//Reading the global volumes from the mixer
		UpdateGlobalVolumes();
	}

	//TODO Write that editor tool that we are avoiding
	//Avoiding the editor script for the global blockers
	void Update()
	{
		//We react only at change
		if(_MusicAllowed != MusicAllowed)
		{
			OnMusicAllowedChange(MusicAllowed);
		}

		//We react only at change
		if(_FXAllowed != FXAllowed)
		{
			OnFXAllowedChange(FXAllowed);
		}
	}

	private void OnMusicAllowedChange(bool newValue)
	{
		_MusicAllowed = newValue;

		//We stop the music with a fadeout
		if(!_MusicAllowed)
		{
			FadeOutAndStopMusic(1.0f);
		}
	}

	private void OnFXAllowedChange(bool newValue)
	{
		_FXAllowed = newValue;

		//We stop the music with a fadeout
		if(!_FXAllowed)
		{
			FadeOutAndStopFX(0.3f);
		}
	}

	#region Sound Play/Pause/Resume/Stop
	/**Search/Play and retrieve the specified sound.**/ 
	public SoundObject PlayFX(string soundName) {return PlaySound(soundName,fxGroup,true,null);	}
	public SoundObject PlayFX(string soundName,SoundConfiguration configurations) {return PlaySound(soundName,fxGroup,true,configurations);	}
	/**
	 * Search/Play and retrieve the specified sound.
	 * 
	 * restartIfPlayingAlready true-Play the sound no matter what
	 * restartIfPlayingAlready false-Play only if it is not previously playing
	 **/ 
	public SoundObject PlayFX(string soundName, bool restartIfPlayingAlready,SoundConfiguration configurations )	{ return PlaySound(soundName,fxGroup,restartIfPlayingAlready,configurations);	}
	public SoundObject PlayFX(string soundName, bool restartIfPlayingAlready )	{ return PlaySound(soundName,fxGroup,restartIfPlayingAlready,null);	}
	public SoundObject PlayFX(string soundName, bool restartIfPlayingAlready, bool playAsLoop)	{ return PlaySound(soundName,fxGroup,restartIfPlayingAlready,playAsLoop,null);	}
	public SoundObject PlayFX(string soundName, bool restartIfPlayingAlready, bool playAsLoop, SoundConfiguration configurations)	{ return PlaySound(soundName,fxGroup,restartIfPlayingAlready,playAsLoop,configurations);	}

	/**Play the specified sound.**/ 
	public void PlayFX(SoundObject sound)	{PlaySound(sound,fxGroup,true,null);	}
	public void PlayFX(SoundObject sound,SoundConfiguration configurations)	{PlaySound(sound,fxGroup,true,configurations);	}
	/**
	 * Play the specified sound.
	 * 
	 * restartIfPlayingAlready true-Play the sound no matter what
	 * restartIfPlayingAlready false-Play only if it is not previously playing
	 **/ 
	public void PlayFX(SoundObject sound, bool restartIfPlayingAlready, SoundConfiguration configurations)	{PlaySound(sound,fxGroup,restartIfPlayingAlready,configurations);	}
	public void PlayFX(SoundObject sound, bool restartIfPlayingAlready)	{PlaySound(sound,fxGroup,restartIfPlayingAlready,null);	}
	public void PlayFX(SoundObject sound, bool restartIfPlayingAlready, bool playAsLoop)	{PlaySound(sound,fxGroup,restartIfPlayingAlready,playAsLoop,null);	}
	public void PlayFX(SoundObject sound, bool restartIfPlayingAlready, bool playAsLoop, SoundConfiguration configurations)	{PlaySound(sound,fxGroup,restartIfPlayingAlready,playAsLoop,configurations);	}



	/**Search/Play and retrieve the specified sound.**/ 
	public SoundObject PlayMusic(string soundName)	{return PlaySound(soundName,musicGroup,true,null);	}
	public SoundObject PlayMusic(string soundName,SoundConfiguration configurations)	{return PlaySound(soundName,musicGroup,true,configurations);	}
	/**
	 * Search/Play and retrieve the specified sound.
	 * 
	 * restartIfPlayingAlready true-Play the sound no matter what
	 * restartIfPlayingAlready false-Play only if it is not previously playing
	 **/ 
	public SoundObject PlayMusic(string soundName, bool restartIfPlayingAlready,SoundConfiguration configurations)		{return PlaySound(soundName,musicGroup,restartIfPlayingAlready,configurations);	}
	public SoundObject PlayMusic(string soundName, bool restartIfPlayingAlready)		{return PlaySound(soundName,musicGroup,restartIfPlayingAlready,null);	}
	public SoundObject PlayMusic(string soundName, bool restartIfPlayingAlready, bool playAsLoop)		{return PlaySound(soundName,musicGroup,restartIfPlayingAlready,playAsLoop,null);	}
	public SoundObject PlayMusic(string soundName, bool restartIfPlayingAlready, bool playAsLoop, SoundConfiguration configurations)	{return PlaySound(soundName,musicGroup,restartIfPlayingAlready,playAsLoop,configurations);	}

	/**Play the specified sound.**/ 
	public void PlayMusic(SoundObject sound)	{PlaySound(sound,musicGroup,true,null);	}
	public void PlayMusic(SoundObject sound, SoundConfiguration configurations)	{PlaySound(sound,musicGroup,true,configurations);	}
	/**
	 * Play the specified sound.
	 * 
	 * restartIfPlayingAlready true-Play the sound no matter what
	 * restartIfPlayingAlready false-Play only if it is not previously playing
	 **/ 
	public void PlayMusic(SoundObject sound, bool restartIfPlayingAlready, SoundConfiguration configurations)	{PlaySound(sound,musicGroup,restartIfPlayingAlready,configurations);	}
	public void PlayMusic(SoundObject sound, bool restartIfPlayingAlready)	{PlaySound(sound,musicGroup,restartIfPlayingAlready,null);	}
	public void PlayMusic(SoundObject sound, bool restartIfPlayingAlready, bool playAsLoop)	{PlaySound(sound,musicGroup,restartIfPlayingAlready,playAsLoop,null);	}
	public void PlayMusic(SoundObject sound, bool restartIfPlayingAlready, bool playAsLoop, SoundConfiguration configurations)		{PlaySound(sound,musicGroup,restartIfPlayingAlready,playAsLoop,configurations);	}


	/**Search/Play and retrieve the sound specified using the mixer group provided**/
	private SoundObject PlaySound(string soundName, AudioMixerGroup mixerGroup, bool restartIfPlayingAlready, bool playAsLoop, SoundConfiguration configurations)
	{
		if(configurations == null)
		{
			configurations = new SoundConfiguration();
		}

		configurations.setIsLoop(playAsLoop);

		return PlaySound(soundName, mixerGroup, restartIfPlayingAlready, configurations);
	}


	private SoundObject PlaySound(string soundName, AudioMixerGroup mixerGroup, bool restartIfPlayingAlready, SoundConfiguration configurations)
	{
		//search for the object
		SoundObject sound = GetSoundByName(soundName);

		if(sound != null)
		{
			PlaySound(sound, mixerGroup, restartIfPlayingAlready, configurations);
			return sound;
		}
		else
		{	Debug.LogWarning("There is no sound with the name: "+soundName);	}

		return null;
	}
		

	/**Play the sound using the mixer group provided.**/ 
	private void PlaySound(SoundObject sound, AudioMixerGroup mixerGroup, bool restartIfPlayingAlready, bool playAsLoop, SoundConfiguration configurations)
	{
		if(configurations == null)
		{
			configurations = new SoundConfiguration();
		}

		configurations.setIsLoop(playAsLoop);

		PlaySound(sound,mixerGroup,restartIfPlayingAlready,configurations);
	}

	private void PlaySound(SoundObject sound, AudioMixerGroup mixerGroup, bool restartIfPlayingAlready, SoundConfiguration configurations)
	{
		//The reproduction is allowed?
		if((mixerGroup == fxGroup && !_FXAllowed) || (mixerGroup == musicGroup && !_MusicAllowed))
		{
			/*
			 WARNING If the game logic use the returned sound on previous calls to wait for stop event we could have problems.
			 TODO Maybe play the sound with volume zero? Or dispatch stop right away?
			 */
			//Denied!
			return;
		}

		//We should avoid the PlayCall?
		if(!restartIfPlayingAlready && sound.isPlaying)
		{	return;	}

		//We store the audio so it can be globally paused/stoped
		playingSounds[mixerGroup][sound.soundName] = sound;
		sound.OnAudioStopped += OnAudioStopped;

		//correct group
		sound.audioSource.outputAudioMixerGroup = mixerGroup;
		sound.Play(configurations);
	}

	private void OnAudioStopped(SoundObject sound)
	{
		//Stop hearing the stopped notification
		sound.OnAudioStopped -= null;

		//TODO The audioSource.outputAudioMixerGroup could change before this function. A fix is needed.
		//Remove from the playing sounds
		playingSounds[sound.audioSource.outputAudioMixerGroup].Remove(sound.soundName);
	}

	/**
	 * Pause the specified sound (music or fx).
	 **/ 
	public void PauseSound(string soundName)
	{
		SoundObject sound = GetSoundByName(soundName);
		if(sound != null)
		{	PauseSound(sound);	}
	}
	public void PauseSound(SoundObject sound)
	{	sound.Pause();	}



	/**
	 * Resume the specified sound (music or fx).
	 **/ 
	public void ResumeSound(string soundName)
	{
		SoundObject sound = GetSoundByName(soundName);
		if(sound != null)
		{	ResumeSound(sound);	}
	}
	public void ResumeSound(SoundObject sound)
	{	sound.Resume();	}


	/**
	 * Stop the specified sound (music or fx).
	 **/ 
	public void StopSound(string soundName, bool allowFadeOut = false)
	{
		SoundObject sound = GetSoundByName(soundName);
		if(sound != null)
		{	StopSound(sound,allowFadeOut);	}
	}
	public void StopSound(SoundObject sound, bool allowFadeOut = false)
	{
		sound.Stop(!allowFadeOut);
	}


	
	/**
	 * Pause all the sounds (music or fx)
	 **/ 
	public void PauseAll()
	{
		PauseAllFX();
		PauseAllMusic();
	}

	/**
	 * Pause all FX
	 **/ 
	public void PauseAllFX()
	{		
		//We don't use foreach to avoid the nasty OutOfSync error
		string[] keys = new string[playingSounds[fxGroup].Keys.Count];
		playingSounds[fxGroup].Keys.CopyTo(keys,0);
		for(int i = 0; i < keys.Length; i++)
		{
			//Stop hearing the notification
			playingSounds[fxGroup][keys[i]].Pause();
		}
	}

	/**
	 * Pause all Music
	 **/ 
	public void PauseAllMusic()
	{
		//We don't use foreach to avoid the nasty OutOfSync error
		string[] keys = new string[playingSounds[musicGroup].Keys.Count];
		playingSounds[musicGroup].Keys.CopyTo(keys,0);
		for(int i = 0; i < keys.Length; i++)
		{
			//Stop hearing the notification
			playingSounds[musicGroup][keys[i]].Pause();
		}
	}

	/**
	 * Resume all the sounds (music or fx)
	 **/ 
	public void ResumeAll()
	{
		ResumeAllFX();
		ResumeAllMusic();
	}

	/**
	 * Resume all FX
	 **/ 
	public void ResumeAllFX()
	{
		//We don't use foreach to avoid the nasty OutOfSync error
		string[] keys = new string[playingSounds[fxGroup].Keys.Count];
		playingSounds[fxGroup].Keys.CopyTo(keys,0);
		for(int i = 0; i < keys.Length; i++)
		{
			//Stop hearing the notification
			playingSounds[fxGroup][keys[i]].Resume();
		}
	}

	/**
	 * Resume all Music
	 **/ 
	public void ResumeAllMusic()
	{
		//We don't use foreach to avoid the nasty OutOfSync error
		string[] keys = new string[playingSounds[musicGroup].Keys.Count];
		playingSounds[musicGroup].Keys.CopyTo(keys,0);
		for(int i = 0; i < keys.Length; i++)
		{
			//Stop hearing the notification
			playingSounds[musicGroup][keys[i]].Resume();
		}
	}

	/**
	 * Stop all the sounds (music or fx)
	 **/ 
	public void StopAllSounds(bool allowFadeOut = false)
	{
		StopAllFX(allowFadeOut);
		StopAllMusic(allowFadeOut);
	}

	/**
	 * Stop all FX
	 **/ 
	public void StopAllFX(bool allowFadeOut = false)
	{
		//We don't use foreach to avoid the nasty OutOfSync error
		string[] keys = new string[playingSounds[fxGroup].Keys.Count];
		playingSounds[fxGroup].Keys.CopyTo(keys,0);
		for(int i = 0; i < keys.Length; i++)
		{
			//Stop hearing the notification
			playingSounds[fxGroup][keys[i]].OnAudioStopped -= OnAudioStopped;
			playingSounds[fxGroup][keys[i]].Stop(!allowFadeOut);
		}

		//There is no more sounds playing on this group
		playingSounds[fxGroup].Clear();
	}

	/**
	 * Stop all Music
	 **/ 
	public void StopAllMusic(bool allowFadeOut = false)
	{
		//We don't use foreach to avoid the nasty OutOfSync error
		string[] keys = new string[playingSounds[musicGroup].Keys.Count];
		playingSounds[musicGroup].Keys.CopyTo(keys,0);
		for(int i = 0; i < keys.Length; i++)
		{
			//Stop hearing the notification
			playingSounds[musicGroup][keys[i]].OnAudioStopped -= OnAudioStopped;
			playingSounds[musicGroup][keys[i]].Stop(!allowFadeOut);
		}

		//There is no more sounds playing on this group
		playingSounds[musicGroup].Clear();
	}
		
	public SoundObject GetSoundByName(string soundName)
	{
		SoundObject result;
		sounds.TryGetValue(soundName,out result);
		return result;
	}
	#endregion

	#region Fades
	/**
	 * Fade Master volume to the value specified during the specified time;
	 **/ 
	public void FadeMasterVolume(float toVolume,float fadeDuration)
	{
		//Stop previous fade
		if(CFadeMaster != null)
		{
			StopCoroutine(CFadeMaster);
		}

		//Parameters to the fade
		FadeVolumeConfiguration fadeConfiguration = new FadeVolumeConfiguration();
		fadeConfiguration.volume = toVolume;
		fadeConfiguration.starterVolume = _masterVolume;
		fadeConfiguration.duration = fadeDuration;
		fadeConfiguration.mixerProperty = "masterVolume";

		CFadeMaster = StartCoroutine("FadeVolume",fadeConfiguration);
	}

	/**
	 * Fade FX volume to the value specified during the specified time;
	 **/ 
	public void FadeFXVolume(float toVolume,float fadeDuration)
	{
		//Stop previous fade
		if(CFadeFX != null)
		{
			StopCoroutine(CFadeFX);
		}

		//Parameters to the fade
		FadeVolumeConfiguration fadeConfiguration = new FadeVolumeConfiguration();
		fadeConfiguration.volume = toVolume;
		fadeConfiguration.starterVolume = _fxVolume;
		fadeConfiguration.duration = fadeDuration;
		fadeConfiguration.mixerProperty = "fxVolume";

		CFadeFX = StartCoroutine("FadeVolume",fadeConfiguration);
	}

	/**
	 * Fade Music volume to the value specified during the specified time;
	 **/ 
	public void FadeMusicVolume(float toVolume,float fadeDuration)
	{
		//Stop previous fade
		if(CFadeMusic != null)
		{
			StopCoroutine(CFadeMusic);
		}

		//Parameters to the fade
		FadeVolumeConfiguration fadeConfiguration = new FadeVolumeConfiguration();
		fadeConfiguration.volume = toVolume;
		fadeConfiguration.starterVolume = _musicVolume;
		fadeConfiguration.duration = fadeDuration;
		fadeConfiguration.mixerProperty = "musicVolume";

		CFadeMusic = StartCoroutine("FadeVolume",fadeConfiguration);
	}

	protected IEnumerator FadeVolume(FadeVolumeConfiguration config)
	{
		float inverseDuration = 1.0f/config.duration;
		float elapsed = 0;
		float progress;

		while(elapsed < config.duration)
		{
			elapsed += Time.deltaTime;
			progress = elapsed*inverseDuration;

			//Mathf.Lerp are already clamping the progress between 0-1
			SetVolumeTo(config.mixerProperty, Mathf.Lerp(config.starterVolume,config.volume, progress));

			UpdateGlobalVolumes();
			yield return 0;
		}

	}

	/**
	 * FadeOut the master volume to 0 in the specified time.
	 * After the fadeOut all the sounds are stopped and the volume is restored.
	 * 
	 **/ 
	public void FadeOutAndStopAll(float fadeDuration)
	{
		//Stop previous fade
		if(CFadeMaster != null)
		{
			StopCoroutine(CFadeMaster);
		}

		//Parameters to the fade
		FadeVolumeConfiguration fadeConfiguration = new FadeVolumeConfiguration();
		fadeConfiguration.volume = 0;
		fadeConfiguration.starterVolume = _masterVolume;
		fadeConfiguration.duration = fadeDuration;
		fadeConfiguration.mixerProperty = "masterVolume";
		fadeConfiguration.OnCompleteStop += StopAllSounds;

		CFadeMaster = StartCoroutine("FadeVolumeAndStop",fadeConfiguration);
	}

	/**
	 * FadeOut the fx volume to 0 in the specified time.
	 * After the fadeOut all the sounds are stopped and the volume is restored.
	 * 
	 **/ 
	public void FadeOutAndStopFX(float fadeDuration)
	{
		//Stop previous fade
		if(CFadeFX != null)
		{
			StopCoroutine(CFadeFX);
		}

		//Parameters to the fade
		FadeVolumeConfiguration fadeConfiguration = new FadeVolumeConfiguration();
		fadeConfiguration.volume = 0;
		fadeConfiguration.starterVolume = _fxVolume;
		fadeConfiguration.duration = fadeDuration;
		fadeConfiguration.mixerProperty = "fxVolume";
		fadeConfiguration.OnCompleteStop += StopAllFX;

		CFadeFX = StartCoroutine("FadeVolumeAndStop",fadeConfiguration);
	}

	/**
	 * FadeOut the music volume to 0 in the specified time.
	 * After the fadeOut all the sounds are stopped and the volume is restored.
	 * 
	 **/ 
	public void FadeOutAndStopMusic(float fadeDuration)
	{
		//Stop previous fade
		if(CFadeMusic != null)
		{
			StopCoroutine(CFadeMusic);
		}

		//Parameters to the fade
		FadeVolumeConfiguration fadeConfiguration = new FadeVolumeConfiguration();
		fadeConfiguration.volume = 0;
		fadeConfiguration.starterVolume = _musicVolume;
		fadeConfiguration.duration = fadeDuration;
		fadeConfiguration.mixerProperty = "musicVolume";
		fadeConfiguration.OnCompleteStop += StopAllMusic;

		CFadeMusic = StartCoroutine("FadeVolumeAndStop",fadeConfiguration);
	}

	/**
	 * After the fade it makes a call to config.OnComplete to stop the sounds.
	 * At the end restore the volume to the starterValue.
	 **/ 
	protected IEnumerator FadeVolumeAndStop(FadeVolumeConfiguration config)
	{
		float inverseDuration = 1.0f/config.duration;
		float elapsed = 0;
		float progress;

		while(elapsed < config.duration)
		{
			elapsed += Time.deltaTime;
			progress = elapsed*inverseDuration;

			//Mathf.Lerp are already clamping the progress between 0-1
			SetVolumeTo(config.mixerProperty, Mathf.Lerp(config.starterVolume,config.volume, progress));

			UpdateGlobalVolumes();
			yield return 0;
		}

		//Should be the stop function
		if(config.OnCompleteStop != null)
		{
			//A little hardcoding on the parameters
			config.OnCompleteStop(false);
		}

		//Restore volume
		SetVolumeTo(config.mixerProperty, config.starterVolume);
	}


	#endregion

	#region VolumeControl
	/**
	 * Master volume.  0-mute, 1-Complete sound
	 **/ 	
	public float masterVolume
	{
		get	{	return _masterVolume;	}
		set	{ SetVolumeTo("masterVolume",value); }
	}

	/**
	 * Master FX volume. 0-mute, 1-Complete sound
	 **/
	public float fxVolume
	{
		get	{	return _fxVolume;	}
		set	{ SetVolumeTo("fxVolume",value); }
	}

	/**
	 * Master Music volume. 0-mute, 1-Complete sound
	 **/
	public float musicVolume
	{
		get	{	return _musicVolume;	}
		set	{ SetVolumeTo("musicVolume",value); }
	}

	/**
	 * Set the volume writing the mixerProperty on the mixer
	 **/
	private void SetVolumeTo(string mixerProperty, float volume)
	{
		if(volume < 0 || volume > 1)
		{
			Debug.LogWarning("The volume should be a value between 0 and 1 (0-mute to 1-Complete sound).");
		}	
			
		//TODO since it is volume attenuation and not volume percent, it doesn't feel quite right. So a fix is needed.
		//TODO support the extra range for attenuation (-80 to 20) so the volumes could get louder
		//The volume attenuation goes from -80 to 0
		float value = 80*volume - 80;//(max-min)*volume + min
		mixer.SetFloat(mixerProperty,value);	
	}

	private void UpdateGlobalVolumes()
	{
		//TODO support the extra range for attenuation (-80 to 20) so the volumes could get louder
		//The volume attenuation goes from -80 to 0
		//volume = (value-min)/(max-min)
		float value;
		mixer.GetFloat("masterVolume",out value);
		_masterVolume = value/80 + 1;

		mixer.GetFloat("fxVolume",out value);
		_fxVolume = value/80 + 1;

		mixer.GetFloat("musicVolume",out value);
		_musicVolume = value/80 + 1;
	}
	#endregion
}
