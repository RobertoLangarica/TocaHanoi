using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * This class is in charge of:
 * 	-Excercise validation (when the exercise is completed)
 * 	-Ring move validation (where a ring is dropped and if the ring could be selected)
 * 	-Ring positions (when the exercise is created)
 **/ 
public class ExerciseManager : MonoBehaviour 
{
	#region Declarations
	//Structure with parameters to be send to a coroutine. Used for the animations
	public struct EffectParams
	{
		public SelectableRing ring;
		public int ringIndex;
		public Pin pin;
	}

	//TODO Remove the hardcoding. Maybe read it from the sprite an use a percentage of the height or some other logic
	public float separationBetweenRings = 1.27f;
	//TODO Read the height dinamically
	public Transform pinHeightReference;//Reference in the scene for the height of the pins
	public ContinuousMovementAnimation overPinAnimation;
	public ContinuousMovementAnimation intoPinAnimation;
	public SoundManager soundManager;
	public HudWin hudWin;//Hud after complete

	[HideInInspector]public List<Pin> pins = new List<Pin>();//Public so ExerciseSolver can use it
	[HideInInspector]public List<SelectableRing> rings = new List<SelectableRing>();//Public so ExerciseSolver can use it

	[HideInInspector]public int starterPin;//The Pin where the exercise start (so the exercise cannot end here)
	private bool isStarted = false;//to block movements when the game isn't started
	private bool isExerciseCompleted = false;
	private int lastPinWithCollision = -1;//To know if a ring is dropped on a pin
	private bool aRingIsSelected;//To avoid the auto solving to run while dragin a ring

	//for scoring
	private int movesCount;
	private float startTimeAnchor;
	#endregion

	#region Game
	/**
	 * Should be called after the exercise creation.
	 **/ 
	public void StartGame()
	{
		isStarted = true;
		isExerciseCompleted = false;
		lastPinWithCollision = -1;
		movesCount = 0;
		startTimeAnchor = Time.realtimeSinceStartup;
		aRingIsSelected = false;

		/*
		 When testing on Android device the next errors came out (Unity team still worknig on them):
		 	-Unable to find AudioPluginOculusSpatializer
		 	-Unable to find libAudioPluginOculusSpatializer
		 Apparently they do nothing but our music doesn't sound (the FX played well). Maybe since StartGame is
		 called on the Start cycle of the GameObjects it gets some problems.
		 A delay seems to work, so be it.
		*/
		StartCoroutine("PlayGameSoundDelaye");
	}

	IEnumerator PlayGameSoundDelaye()
	{
		yield return new WaitForSeconds(0.25f);
		soundManager.PlayMusic("game",false,true);
	}

	/**
	 * Add a ring to be used on the exercise
	 **/ 
	public void AddRingReference(SelectableRing ring)
	{
		rings.Add(ring);

		//Ring delegates
		ring.OnCanceled += OnRingSelectionCanceled;
		ring.AskSelectionPermit += OnRingAskingToBeSelected;
		ring.OnDropped += OnRingDropped;
	}

	/**
	 * Add the ring to the indicated pin without validation.
	 **/ 
	public void AddRingToPin(SelectableRing ring, int pinIndex)
	{
		ring.pin = pinIndex;
		pins[pinIndex].ringPush(ring);
	}


	/**
	 * Add a pin to be used in the exercise.
	 **/ 
	public void AddPinReference(Pin pin)
	{
		pins.Add(pin);

		//Pin dlegates
		pin.OnCollisionDetected += OnPinCollsion;
		pin.OnCollisionLost += OnPinCollsionLost;
		pin.index = pins.Count-1;
	}


	/**
	 * When a Pin detect a collision with a ring.
	 * 
	 * Used to know on wich Pin the ring were dropped
	 **/ 
	private void OnPinCollsion(Collider2D other,int pinInstanceId)
	{
		if(!isStarted)
		{
			//the exercise hasn't started yet
			return;
		}

		if(other.tag == "Ring")
		{
			lastPinWithCollision = pinInstanceId;
		}	
		else
		{
			Debug.LogWarning("If the tag isn't \"Ring\" then the collision is ignored.");
		}
	}

	/**
	 * When a pin lost a collision
	 **/ 
	private void OnPinCollsionLost(Collider2D other,int pinInstanceId)
	{
		if(!isStarted)
		{
			//the exercise hasn't started yet
			return;
		}

		if(other.tag == "Ring")
		{
			//We remove the collision only if it was the last
			if(lastPinWithCollision == pinInstanceId)
			{
				lastPinWithCollision = -1;	
			}
		}	
		else
		{
			Debug.LogWarning("If the tag isn't \"Ring\" then the collision is ignored.");
		}
	}
		
	/**
	 * When the selection was canceled by game external factors
	 **/ 
	private void OnRingSelectionCanceled(SelectableRing ring)
	{
		if(!isStarted)
		{
			//the exercise hasn't started yet
			return;
		}

		aRingIsSelected = false;

		//Return the ring to his previous position
		AddRingToPin(ring,ring.pin);
		//TODO Without animation? We have to review the possible causes for a touch cancel and then decide
		PositionateRingOnPin(ring,ring.pin,pins[ring.pin].ringCount(),false);//visual add


		//QA: If there is a multiple selection active then this should be managed
		#if UNITY_EDITOR
		if(isMultipleSelectionActive)
		{
			//Stop the visual updating
			StopCoroutine("MultipleSelectionVisualUpdate");

			//Every ring back to his previous pin
			for(int i = 1; i < multipleRingSelection.Count; i++)
			{
				AddRingToPin(multipleRingSelection[i],ring.pin);//logical
				//TODO Without animation? We have to review the possible causes for a touch cancel and then decide
				PositionateRingOnPin(multipleRingSelection[i],ring.pin,pins[ring.pin].ringCount(),false);//visual
			}

			//Stop multiple selection
			isMultipleSelectionActive = false;	
		}
		#endif

	}

	private void OnRingAskingToBeSelected(SelectableRing ring)
	{
		if(!isStarted)
		{
			//the exercise hasn't started yet
			return;
		}

		//Collision to the pin of the ring selected (to avoid innecesary animations)
		lastPinWithCollision = pins[ring.pin].GetInstanceID();

		//Only the last ring can be selected
		if(pins[ring.pin].ringPeek().GetInstanceID() == ring.GetInstanceID())
		{
			//Removing the ring from his current Pin so it can be moved
			pins[ring.pin].ringPop();
			ring.SelectionAllowed();
			aRingIsSelected = true;
		}
		#if UNITY_EDITOR
		else
		{
			aRingIsSelected = true;
			//For QA (have fun Roy!)
			QA_OnRingAskingToBeSelected(ring);
		}
		#endif
	}

	private void OnRingDropped(SelectableRing ring)
	{
		if(!isStarted)
		{
			//the exercise hasn't started yet
			return;
		}

		aRingIsSelected = false;

		//check if the ring was dropped on a pin
		if(lastPinWithCollision != -1)
		{
			//The ring were dropen in the lastPinWithCollision ring
			for(int i = 0; i < pins.Count; i++)
			{
				if(pins[i].GetInstanceID() == lastPinWithCollision)
				{
					//Pin found

					/*
					 Lets see is this ring could be dropped here.
					 The ring can only be droopped on empty Pins or when the last ring on the pin 
					 is smaller.
					 */
					if(pins[i].isEmpty() || pins[i].ringPeek().sizeIndicator >= ring.sizeIndicator)
					{
						//Allowed

						//we count the move only if the ring is dropped on another pin
						if(ring.pin != i)//the ring remember the previous pin until a new one is added
						{movesCount++;}
							
						AddRingToPin(ring,i);//loginc add
						PositionateRingOnPin(ring,i,pins[i].ringCount(),true);//visual add

						//QA: If there is a multiple selection active then we manage it
						#if UNITY_EDITOR
						if(isMultipleSelectionActive)
						{
							//The visual position should not be modified since every ring is folowing the selected one
							for(int j = 1; j < multipleRingSelection.Count; j++)
							{
								//Add the selected rings to the pin 
								AddRingToPin(multipleRingSelection[j],i);//loginc add

								//Z sorting for all the extra selected rings(so the rings dont overlap)
								multipleRingSelection[j].setFrontSortingOrder(pins[i].ringCount());	
								multipleRingSelection[j].SortingOrderForSelection(false);
							}
						}
						#endif

						//if this isn't the starter pin then we check if the exercise is completed
						if(starterPin != i)
						{
							CheckIfExerciseIsCompleted(i);
						}

						break;
					}
					else
					{
						//Denied

						soundManager.PlayFX("denied");

						//The ring is returned to his previous Pin
						AddRingToPin(ring,ring.pin);//loginc add
						MoveRingOverPinEffect(ring,ring.pin);//visual add

						//QA: If there is a multiple selection active then we manage it
						#if UNITY_EDITOR
						if(isMultipleSelectionActive)
						{
							//The visual position should not be modified since every ring is folowing the selected one
							for(int j = 1; j < multipleRingSelection.Count; j++)
							{
								//Add the selected rings to the pin 
								AddRingToPin(multipleRingSelection[j],ring.pin);//loginc add

								//Z sorting for all the extra selected rings(so the rings dont overlap)
								multipleRingSelection[j].setFrontSortingOrder(pins[ring.pin].ringCount());	
								multipleRingSelection[j].SortingOrderForSelection(false);
							}
						}
						#endif
					}
				}
			}
		}
		else
		{
			//Pin dropped on the void
			//The ring is returned to his previous Pin
			AddRingToPin(ring,ring.pin);//loginc add
			MoveRingOverPinEffect(ring,ring.pin);//visual add

			//QA: If there is a multiple selection active then we manage it
			#if UNITY_EDITOR
			if(isMultipleSelectionActive)
			{
				//The visual position should not be modified since every ring is folowing the selected one
				for(int j = 1; j < multipleRingSelection.Count; j++)
				{
					//Add the selected rings to the pin 
					AddRingToPin(multipleRingSelection[j],ring.pin);//loginc add

					//Z sorting for all the extra selected rings(so the rings dont overlap)
					multipleRingSelection[j].setFrontSortingOrder(pins[ring.pin].ringCount());	
					multipleRingSelection[j].SortingOrderForSelection(false);
				}
			}
			#endif
		}


		//QA
		#if UNITY_EDITOR
		if(isMultipleSelectionActive)
		{
			//Stop updating the selected rings when the effect finished
			intoPinAnimation.OnAnimationComplete += QA_OnDroppedAnimationComplete;
		}
		#endif

	}

	private void CheckIfExerciseIsCompleted(int pinIndex)
	{
		if(pins[pinIndex].ringCount() == rings.Count)
		{
			//The win sound is played after the ring drop in the AfterDropSoundEffect funtion

			float elapsed = Time.realtimeSinceStartup - startTimeAnchor;

			//Exercise COMPLETED yeha!
			Debug.Log("Exercise completed. Movements: "+movesCount+" Time: "+elapsed);

			//HUD update
			hudWin.setInfo(elapsed,movesCount);

			isExerciseCompleted = true;
			isStarted = false;
		}
	}

	/**
	 * Animation effect to move the ring over the pin 
	 **/
	public void MoveRingOverPinEffect(SelectableRing ring,int pinIndex)
	{
		//Pin position
		Vector3 expectedPosition = pins[pinIndex].transform.position;

		//Y based on the pin height
		expectedPosition.y = pinHeightReference.position.y;

		//Coroutine params
		EffectParams effectParams = new EffectParams();
		effectParams.ring = ring;
		effectParams.pin = pins[pinIndex];
		effectParams.ringIndex = pins[pinIndex].ringCount();

		overPinAnimation.destination = expectedPosition;
		overPinAnimation.target = ring.transform;
		overPinAnimation.Play();

		StartCoroutine("WaitingForMoveRingOverEffectToFinish",effectParams);
	}

	IEnumerator WaitingForMoveRingOverEffectToFinish(EffectParams parameters)
	{
		while(overPinAnimation.isPlaying)
		{
			yield return 0;
		}

		//object go down on pin
		PositionateRingOnPin(parameters.ring,parameters.pin,parameters.ringIndex ,true);
	}

	/**
	 * Visual positionation of the ring on the indicated pin.
	 **/ 
	public void PositionateRingOnPin(SelectableRing ring, int pinIndex,int ringIndex, bool animate = false)
	{PositionateRingOnPin(ring, pins[pinIndex],ringIndex,animate);}
		
	/**
	 * Visual positionation of the ring on the indicated pin.
	 * 
	 **/ 
	public void PositionateRingOnPin(SelectableRing ring, Pin pin, int ringIndex, bool animate = false)
	{
		//Pin position
		Vector3 expectedPosition = pin.transform.position;

		//Ring sorting order (so the rings dont overlap)
		ring.setFrontSortingOrder(ringIndex);

		//Y position based on the ring index
		expectedPosition.y += (ringIndex-1) * separationBetweenRings;

		if(animate)
		{
			intoPinAnimation.destination = expectedPosition;
			intoPinAnimation.target = ring.transform;
			intoPinAnimation.OnAnimationComplete += AfterDropAnimation;//some effects after the animation
			intoPinAnimation.Play();
		}
		else
		{
			ring.transform.position = expectedPosition;
		}
	}

	private void AfterDropAnimation()
	{
		intoPinAnimation.OnAnimationComplete -= AfterDropAnimation;

		//always play the drop sound
		soundManager.PlayFX("drop_ring");

		//This drop lead to a Win?
		if(isExerciseCompleted)
		{
			soundManager.PlayFX("win");
			hudWin.Enter();
		}
	}


	#endregion

	#region QA
	#if UNITY_EDITOR
	private bool multipleSelectionAllowed = false;//Can Roy select multiple rings to move?
	private bool isMultipleSelectionActive = false;
	private List<SelectableRing> multipleRingSelection = new List<SelectableRing>();


	void Update()
	{
		//is any shift key pressed?
		multipleSelectionAllowed = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));


		//Auto solve exercise
		if(isStarted && !aRingIsSelected && Input.GetKeyUp(KeyCode.S))
		{
			ExerciseSolver solver = GameObject.Find("_ExerciseSolver").GetComponent<ExerciseSolver>();

			//Game as finished so user input gets Allowed
			isStarted = false;
			solver.SolveExercise();
		}
	}

	private int currentSolutionStep;
	private List<ExerciseStep> solutionSteps;
	public void SolveExerciseWith(List<ExerciseStep> steps)
	{
		currentSolutionStep = 0;
		solutionSteps = steps;
		StartCoroutine("DoSteps");
	}

	private IEnumerator DoSteps()
	{ 
		Vector3 pos;
		float duration;
		float moveDuration = intoPinAnimation.duration;

		//Lets make ir faster
		intoPinAnimation.duration = intoPinAnimation.duration*0.85f;
		while(currentSolutionStep < solutionSteps.Count)
		{
			SelectableRing ring = solutionSteps[currentSolutionStep].ring.ringReference;
			Pin from = solutionSteps[currentSolutionStep].from.pinReference;
			Pin to = solutionSteps[currentSolutionStep].to.pinReference;

			//Logic status
			from.ringPop();
			AddRingToPin(ring,to.index);
			movesCount++;

			//visual ststus
			ring.DoSelectionEffect();

			//A little wait
			yield return new WaitForSeconds(0.15f);

			//Alittle move upward
			pos = ring.transform.position;
			pos.y += separationBetweenRings;
			duration = intoPinAnimation.duration;

			intoPinAnimation.duration = 0.1f;
			intoPinAnimation.target = ring.transform;
			intoPinAnimation.destination = pos;
			intoPinAnimation.Play();

			yield return new WaitForSeconds(0.11f);

			intoPinAnimation.duration = duration;
			PositionateRingOnPin(ring,to.index,to.ringCount(),true);

			//We wait for the visuals again
			yield return new WaitForSeconds(intoPinAnimation.duration*0.85f);

			ring.SortingOrderForSelection(false);
			currentSolutionStep++;
		}

		intoPinAnimation.duration = moveDuration;

		CheckIfExerciseIsCompleted(solutionSteps[solutionSteps.Count-1].to.index);
	}

	private void QA_OnRingAskingToBeSelected(SelectableRing ring)
	{
		if(!multipleSelectionAllowed)
		{
			return;
		}


		Pin pin = pins[ring.pin];
		multipleRingSelection.Clear();

		//Select the ring and everything above him
		int selectedIndex = -1;
		for(int i = 0; i < pin.ringsStack.Count; i++)
		{
			//Search for the selected ring
			if(selectedIndex < 0)
			{
				if(pin.ringsStack[i].GetInstanceID() == ring.GetInstanceID())
				{
					selectedIndex = i;
					multipleRingSelection.Add(pin.ringsStack[i]);
				}
			}
			else
			{
				//Every ring after the selected should be added
				multipleRingSelection.Add(pin.ringsStack[i]);
			}
		}

		//Remove all the selected rings from the pin
		pin.ringsStack.RemoveRange(selectedIndex,multipleRingSelection.Count);

		//All the selected rings follow the movements of the one selected by the input
		StartCoroutine("MultipleSelectionVisualUpdate");

		//Allow selection and visually select the extra rings
		ring.SelectionAllowed();
		foreach(SelectableRing r in multipleRingSelection)
		{
			r.DoSelectionEffect();
		}

		isMultipleSelectionActive = true;
	}

	/**
	 * Update of the visuals for every selected ring
	 **/ 
	private IEnumerator MultipleSelectionVisualUpdate()
	{
		Vector3 positionHelper;

		while(true)
		{
			//The first one is the lead
			for(int i = 1; i < multipleRingSelection.Count; i++)
			{
				//Y offset for the correct ring separation
				positionHelper = multipleRingSelection[0].transform.position;
				positionHelper.y += i*separationBetweenRings;
				multipleRingSelection[i].transform.position = positionHelper;
				multipleRingSelection[i].transform.rotation = multipleRingSelection[0].transform.rotation;
			}
				
			yield return 0;
		}
	}

	private void QA_OnDroppedAnimationComplete()
	{
		intoPinAnimation.OnAnimationComplete -= QA_OnDroppedAnimationComplete;

		//We terminate the multiple selection
		StopCoroutine("MultipleSelectionVisualUpdate");
		isMultipleSelectionActive = false;
	}
	#endif
	#endregion
}
