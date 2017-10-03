using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/**
 * Simple controller for the HUD when the exercises is finished
 **/ 
public class HudWin : MonoBehaviour 
{
	public GameObject background;
	public RectTransform popUpTransform;//outisde by default
	public RectTransformAnchorAnimation anchorAnimation;
	public SoundManager soundManager;

	public GameObject infoContainer;//disable by default
	public Text movesText;
	public Text timeText;
	public Text bestMovesText;
	public Text bestTimeText;
	public Toggle fxToggle;
	public Toggle musicToggle;

	private float bestTime;
	private int bestMoves;

	/**
	 * Set the information to be showed
	 **/ 
	public void setInfo(float time, int moves)
	{
		//The best score is based primarily in moves and secondary in time
		if((bestMoves == moves && time < bestTime) || (moves < bestMoves))
		{
			bestMoves = moves;
			bestTime = time;
		}

		movesText.text = moves.ToString("000");
		bestMovesText.text = moves.ToString("000");
		timeText.text = Mathf.Floor(time).ToString("000")+"s";
		bestTimeText.text = Mathf.Floor(bestTime).ToString("000")+"s";

	}

	/**
	 * Show  background and popup
	 **/ 
	public void Enter()
	{
		background.SetActive(true);

		//HARDCODING The anchor is by default with an offsetY +2, we remove the offset
		Vector2 anchorMax = popUpTransform.anchorMax;
		Vector2 anchorMin = popUpTransform.anchorMin;
		anchorMax.y -= 2;
		anchorMin.y -= 2;

		anchorAnimation.target = popUpTransform;
		anchorAnimation.anchorMaxDestination = anchorMax;
		anchorAnimation.anchorMinDestination = anchorMin;
		anchorAnimation.OnAnimationComplete += OnEnterComplete;

		anchorAnimation.Play();

	}

	protected void OnEnterComplete()
	{
		anchorAnimation.OnAnimationComplete -= OnEnterComplete;
		infoContainer.SetActive(true);
	}

	/**
	 * Hide background and popup
	 **/ 
	public void Exit()
	{
		//HARDCODING The anchor is by default with an offsetY +2, we add that offset
		Vector2 anchorMax = popUpTransform.anchorMax;
		Vector2 anchorMin = popUpTransform.anchorMin;
		anchorMax.y += 2;
		anchorMin.y += 2;



		anchorAnimation.anchorMaxDestination = anchorMax;
		anchorAnimation.anchorMinDestination = anchorMin;
		anchorAnimation.OnAnimationComplete += OnExitComplete;
		anchorAnimation.Play();
	}

	private void OnExitComplete()
	{
		anchorAnimation.OnAnimationComplete -= OnExitComplete;

		//hide background and info after complete
		background.SetActive(false);
		infoContainer.SetActive(false);
	}

	public void UpdateFromToggle()
	{
		soundManager.MusicAllowed = musicToggle.isOn;
		soundManager.FXAllowed = fxToggle.isOn;
	}
}
