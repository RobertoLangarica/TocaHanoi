using System.Collections;
using System.Collections.Generic;

/**
 * SoundConfig 
 * 
 * Specified the dessired properties to be set to a SoundObject
 * 
 * NOTE: The isLoop is managed independly
 **/ 
public class SoundConfiguration : System.Object 
{
	public float fadeInTime = -1;
	public float fadeOutTime = -1;
	public float volume = -1;

	//We take special care with loop so it dont get overwritten
	private bool isLoop;
	private int _loop = -1;
	public void setIsLoop(bool loop)
	{
		isLoop = loop;
		_loop = 1;
	}

	public bool loopHasChanged(){ return _loop > 0;}
	public bool getLoopValue(){ return isLoop;}
}
