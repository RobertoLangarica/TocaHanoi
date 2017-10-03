using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * This class recive the references of rings and pins and create a simple excercise
 * where every ring is stacked correctly on the first pin
 **/ 
public class ExerciseCreator : MonoBehaviour 
{
	public ExerciseManager exerciseManager;
	public List<SelectableRing> rings;
	public Pin[] pins;

	[Header("Dynamic exercise (alpha version)")]
	public bool createExerciseDynamically = false;
	public int ringCount = 4;
	public float scaleStepBetweenRings = 0.16f;
	public Color[] colorPalette;
	public SelectableRing ringPrefab;

	void Start()
	{
		//TODO an external menu to init the game
		if(!createExerciseDynamically)			
		{
			//The rings should be sorted by weight, for easy use and
			QuickSortMethod(rings,0,rings.Count);
			createAndStart();
		}
		else
		{
			createDynamicExercise();
			exerciseManager.StartGame();
		}
	}
		
	public void createAndStart()
	{
		createExcercise();
		exerciseManager.StartGame();
	}

	public void resetAndStart()
	{
		resetExercise();
		exerciseManager.StartGame();
	}

	/**
	 * Create the exercis using all the rings in scene
	 **/ 
	public void createExcercise()
	{
		//TODO randomized (just for the lols)
		int starterPin = 0;

		//pin references 
		foreach(Pin pin in pins)
		{
			exerciseManager.AddPinReference(pin);
			pin.SetSelection(false);//removing any selection from the pin
		}

		//ring references and positioning
		for(int i = rings.Count-1; i >= 0; i--)
		{
			exerciseManager.AddRingReference(rings[i]);
			exerciseManager.AddRingToPin(rings[i],starterPin);
			exerciseManager.PositionateRingOnPin(rings[i],starterPin,(rings.Count-i));
		}

		//Pin where the exercise start
		exerciseManager.starterPin = starterPin;
		//Pin color hint
		pins[starterPin].SetSelection(true);
	}

	/**
	 * Reset an exercise only after one was created previously
	 **/ 
	public void resetExercise()
	{
		int starterPin = UnityEngine.Random.Range(0,pins.Length);

		//Use a different starter pin than the actual
		while(exerciseManager.starterPin == starterPin)
		{
			starterPin = UnityEngine.Random.Range(0,pins.Length);
		}

		//removing any selection from the pin
		foreach(Pin pin in pins)
		{
			pin.SetSelection(false);
		}

		//Remove any previous state
		foreach(Pin pin in exerciseManager.pins)
		{
			pin.ringClear();
		}

		//Adding rings to starter pin 
		for(int i = rings.Count-1; i >= 0; i--)
		{
			exerciseManager.AddRingToPin(rings[i],starterPin);
			exerciseManager.PositionateRingOnPin(rings[i],starterPin,(rings.Count-i));
		}

		//Pin where the exercise start
		exerciseManager.starterPin = starterPin;
		//Pin color hint
		pins[starterPin].SetSelection(true);
	}


	private void createDynamicExercise()
	{
		SelectableRing ring;
		Vector3 scale;
		SpriteRenderer[] sprites;
		Color currentColor;

		//delete previous ring references and avoid a nullReference
		rings = new List<SelectableRing>();

		for(int i = 0; i < ringCount; i++)
		{
			//Instantiation
			ring = GameObject.Instantiate<SelectableRing>(ringPrefab,transform,true);
			scale = ring.transform.localScale;
			scale.x -= scaleStepBetweenRings*i;
			ring.transform.localScale = scale;

			//tint
			sprites = ring.GetComponentsInChildren<SpriteRenderer>();
			if(colorPalette.Length > i)
			{	currentColor = colorPalette[i];	}
			else
			{	currentColor = new Color(UnityEngine.Random.Range(.4f,1.0f),UnityEngine.Random.Range(.4f,1.0f),UnityEngine.Random.Range(.4f,1.0f));	}

			foreach(SpriteRenderer sprite in sprites)
			{
				sprite.color = currentColor;
			}

			//for the exercise
			ring.sizeIndicator = ringCount - i;

			//from lesser to biger size, for compatibility with the rest of the code
			rings.Insert(0,ring);
		}

		//Create exercise normally
		createExcercise();
	}

	/**
	 * Sort from lower to higher
	 * TODO: Support for reverse sorting order
	 **/ 
	private static void QuickSortMethod(List<SelectableRing> target, int start, int end)
	{
		if(start >= end)
		{
			return;
		}

		int p = partition(target,start,end);

		if(p-start > 1)
		{QuickSortMethod(target,start,p);}//left

		if(end -p+1 > 1)
		{QuickSortMethod(target,p+1,end);}//right

	}

	/**
	 * Array partition for QuickSort.
	 * 
	 * Partition selects a pivot and sort from start to end every value 
	 * so the values greater than pivote are to his right and the lesser ones are to his left.
	 * 
	 * TODO: Support for reverse sorting order
	 **/ 
	private static int partition(List<SelectableRing> target, int start, int end)
	{
		//TODO pick a better pivot
		//pivot selection (last for simplicity)
		SelectableRing pivot = target[end-1];
		int left,right;
		left = start;
		right = end-2;

		//We iterate until all the values greater than pivot are to his right and the lesser ones to his left
		while(left+1 < right)
		{
			//find a left reference that is greater than pivot
			while(target[left].sizeIndicator <= pivot.sizeIndicator && left+1 < right)
			{
				left++;
			}

			//find a right reference that is lesser than pivot
			while(target[right].sizeIndicator > pivot.sizeIndicator && right-1 > left)
			{
				right--;
			}

			/*
			 Left is greater than pivot and right is less or equal so we swap.
			 */
			if(target[left].sizeIndicator > pivot.sizeIndicator && target[right].sizeIndicator <= pivot.sizeIndicator)
			{
				//swap
				//TODO some XOR optimization
				SelectableRing tmp = target[left];
				target[left] = target[right];
				target[right] = tmp;
				left++;
			}
		}


		//move the pivot to final position that is greater than him
		if(target[left].sizeIndicator > pivot.sizeIndicator)
		{
			//swap
			//TODO some XOR optimization
			SelectableRing tmp = target[left];
			target[left]	= target[end-1];
			target[end-1]	= tmp;	

			//new pivot position
			return left;
		}
		else if(target[right].sizeIndicator > pivot.sizeIndicator)
		{
			//swap
			//TODO some XOR optimization
			SelectableRing tmp = target[right];
			target[right]	= target[end-1];
			target[end-1]	= tmp;	

			//new pivot position
			return right;
		}

		//the pivote doesn't move
		return end-1;
	}
}
