using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/**
 * Base class to control sounds.
 * 	-Play/Pause/Resume/Stop
 * 	-Notify at start and end playing
 * 	-Control multiple clips (to play one at random)
 * 	-Volume FadeIn/Out
 **/ 
[RequireComponent(typeof(AudioSource))]
public class SoundObject : MonoBehaviour 
{
	public AudioSource _audioSource;
	public AudioClip[] clips;

	[Header("Configuration")]
	public bool isLoop = false;
	public float fadeInTime;
	public float fadeOutTime;

	[Header("Volume randomization between clips")]
	/**
	 When it is true the volume will be randomized for every clip played.
	Could lead to reduce the monotony.
	 **/
	public bool volumeRandomization = false;
	[Range(0.0f,1.10f)]public float minVolumePercent = 0.5f;

	public Action<SoundObject> OnAudioStarted;
	public Action<SoundObject> OnAudioStopped;
	[HideInInspector]public bool isPaused = false;

	protected bool _isPlaying;
	protected float _volume;
	protected int currentPlayingClipIndex;
	protected Coroutine FadeCoroutine;//Reference for control
	protected string _soundName = "";

	/**
	 * The name is used to identify the sound by the SoundManager.
	 * WARNING: It should only be writed once on the contrary SoundManager couldn't be
	 * 			able to control it. We are throwing an exception just in case.
	 **/ 
	public string soundName
	{
		get{ return _soundName;}
		set
		{
			if(_soundName != "")
			{
				Debug.LogError("The name of the SoundObject is used as a reference to it so it shouldn't be changed");
			}

			_soundName = value;
		}
	}

	public bool isPlaying
	{	get{return _isPlaying;}}

	/**
	 * Float value between 0(silence) and 1(sound)
	 **/ 
	public float volume
	{
		get	{	return _volume;	}
		set	
		{
			_volume = value;
			audioSource.volume = _volume;
		}
	}

	void Start()
	{
		audioSource.playOnAwake = false;

		//volume matching the audio source volume
		volume = audioSource.volume;
	}

	/**
	 * Starts playing a clip. If there is more than one then it picks one at random.
	 **/ 
	public void Play()	{	Play(null);	}
	public virtual void Play(SoundConfiguration configurations)
	{
		if(configurations != null)
		{
			volume = configurations.volume < 0 ? volume:configurations.volume;
			fadeInTime = configurations.fadeInTime < 0 ? fadeInTime:configurations.fadeInTime;
			fadeOutTime = configurations.fadeOutTime < 0 ? fadeOutTime:configurations.fadeOutTime;	

			if(configurations.loopHasChanged())
			{	isLoop = configurations.getLoopValue();}
		}

		PlayRandomClip(true);
	}
		
	/**
	 * Play any clip from at random.
	 * shouldNotifyStart: is used to avoid start notification in loops
	 **/ 
	protected void PlayRandomClip(bool shouldNotifyStart)
	{
		if(isEmpty())
		{
			//nothing to play
			Debug.LogWarning("There is no AudioClip on: "+soundName+" to play.");
			return;
		}

		if(clips.Length == 1)
		{
			//Avoiding the inecessary random
			PlayClipAt(0,shouldNotifyStart);
		}
		else
		{
			PlayClipAt(UnityEngine.Random.Range(0,clips.Length),shouldNotifyStart);	
		}

	}

	protected void PlayClipAt(int clipIndex, bool shouldNotifyStart = true)
	{
		if(clips[clipIndex] == null)
		{
			Debug.LogWarningFormat("The clip: {0} does not exist in the sound: {1}. Do you forgot to add it in the Editor?",clipIndex,soundName);	
			return;
		}

		currentPlayingClipIndex = clipIndex;

		//Volume
		float localVolume = volume;
		if(volumeRandomization)
		{
			localVolume = UnityEngine.Random.Range(volume*minVolumePercent,volume);
		}

		audioSource.volume = localVolume;

		//is already playing?
		if(audioSource.isPlaying)
		{
			//Avoiding any unexpected Invoke
			CancelInvoke("OnAudioBeforeFadeOut");

			//stop fadeIn/Out if needed
			if(FadeCoroutine != null)
			{
				StopCoroutine(FadeCoroutine);
				isFadingOut = false;
			}

			audioSource.Stop();
		}

		//Play
		audioSource.loop = false;//the loop is managed internally (for better control)
		audioSource.clip = clips[clipIndex];
		audioSource.Play();

		//Notification
		if(shouldNotifyStart && OnAudioStarted != null)
		{
			OnAudioStarted(this);
		}
			
		//Await for completion
		PrepareAwaitForCompletion();

		//fadeIn
		if(fadeInTime > 0)
		{
			audioSource.volume = 0;
			FadeIn(_volume);
		}
			
		//Not paused
		isPaused = false;

		_isPlaying = true;
	}
		
	protected void FadeIn(float targetVolume)
	{
		FadeCoroutine = StartCoroutine("FadeInVolume",targetVolume);
	}

	protected IEnumerator FadeInVolume(float targetVolume)
	{
		float duration = fadeInTime;
		float inverseDuration = 1.0f/duration;
		float elapsed = 0;
		float progress;
		float starterVolume = audioSource.volume;

		while(elapsed < duration)
		{
			//Only update the values when the sound isn't paused
			if(!isPaused)
			{	
				elapsed += Time.deltaTime;
				progress = elapsed*inverseDuration;

				//Mathf.Lerp are already clamping the progress between 0-1
				audioSource.volume = Mathf.Lerp(starterVolume,targetVolume, progress);
			}

			yield return 0;
		}
	}

	protected virtual void PrepareAwaitForCompletion()
	{
		//Wait for completion
		float waitTime = audioSource.clip.length - audioSource.time;

		//if the audio is a loop we ignore the fadeOut
		if(fadeOutTime > 0 && waitTime > fadeOutTime && !isLoop)
		{
			waitTime-= fadeOutTime;
		}

		//Cancel any previous wait so every play has it own
		CancelInvoke("OnAudioBeforeFadeOut");
		Invoke("OnAudioBeforeFadeOut",waitTime);
	}

	protected void OnAudioBeforeFadeOut()
	{
		AudioCompleted();
	}

	/**
	 * We use AudioCompleted to properly loop the sound
	 **/ 
	protected virtual void AudioCompleted()
	{
		//we play again if necessary
		if(isLoop)
		{
			PlayRandomClip(false);
		}
		else
		{
			//Stop
			Stop(false);	
		}
	}

	/**
	 * Use forceStop to avoid the fadeOut.
	 * 
	 * true by default so is Stop is called the sound stop immediatly
	 **/ 
	public void Stop(bool forceStop = true)
	{
		//Avoiding any unexpected Invoke
		CancelInvoke("OnAudioBeforeFadeOut");

		//It is already fadingOut?
		if(isFadingOut)
		{
			//If stop was called during the fadeOut we force the stop
			forceStop = true;
		}

		//Stop fadeIn/out if needed
		if(FadeCoroutine != null)
		{
			StopCoroutine(FadeCoroutine);
			isFadingOut = false;
		}

		if(fadeOutTime > 0 && !forceStop)
		{
			FadeOut();
		}
		else
		{
			//Only stop
			audioSource.Stop();

			if(OnAudioStopped != null)
			{
				OnAudioStopped(this);
			}
		}

		//Is not playing
		_isPlaying = false;
	}

	protected bool isFadingOut = false;
	protected void FadeOut()
	{
		isFadingOut = true;
		FadeCoroutine = StartCoroutine("FadeOutVolume");
	}

	protected IEnumerator FadeOutVolume()
	{
		//Rmeaining sound time
		float remainingTime = audioSource.clip.length - audioSource.time;
		//The fading time cannot be larger than the sound remaining time
		float duration = fadeOutTime > remainingTime ? remainingTime:fadeOutTime;
		float inverseDuration = 1.0f/duration;
		float elapsed = 0;
		float progress;
		float starterVolume = audioSource.volume;

		while(elapsed < duration)
		{
			//Only update the values when the sound isn't paused
			if(!isPaused)
			{	
				elapsed += Time.deltaTime;
				progress = elapsed*inverseDuration;

				//Mathf.Lerp are already clamping the progress between 0-1
				audioSource.volume = Mathf.Lerp(starterVolume,0, progress);
			}

			yield return 0;
		}
			
		isFadingOut = false;

		//Stop the sound
		Stop(true);
	}

	public void Pause()
	{
		//If it is not playing then there is no pause
		if(!_isPlaying)
		{
			return;
		}

		//Avoiding any unexpected Invoke
		CancelInvoke("OnAudioBeforeFadeOut");	

		audioSource.Pause();
		isPaused = true;
	}

	public void Resume()
	{
		//Avoid a phantom play if it is not paused
		if(!isPaused)
		{
			return;	
		}

		audioSource.Play();

		//Await for completion
		PrepareAwaitForCompletion();

		isPaused = false;
	}

	/**
	 * Indicate if there is no clips
	 **/ 
	public bool isEmpty()
	{
		return (clips.Length == 0);
	}

	/**
	 * Getter that helps to avoid force the user to put a reference to the audio source.
	 * 
	 * Also we use GetComponent here so it is only called when really needed and not in the
	 * Start/Awake (there could be a lot of sounds)
	 **/ 
	public AudioSource audioSource
	{
		get
		{
			if(_audioSource == null)
			{
				_audioSource = GetComponent<AudioSource>();
			}

			return _audioSource;
		}
	}
}
