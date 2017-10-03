using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Every Pin is a stack of rings.
 * 
 * Each Pin is sensing for collision with rings so it can send notification when a ring is passing over
 **/
public class Pin : MonoBehaviour 
{
	[Header("Effects")]
	public ScaleSelectionAnimation scaleAnimation;
	public Color selectedColor;//We treat as selected a pin when it is the starter pin on the exercise
	public Color unselectedColor = Color.white;
	public SpriteRenderer spriteToTint;

	//Dispatched after the OnTriggerEnter2D with the other collider and the instanceID to this object
	public Action<Collider2D,int> OnCollisionDetected;
	//Dispatched after the OnTriggerExit2D with the other collider and the instanceID to this object
	public Action<Collider2D,int> OnCollisionLost;
	
	public List<SelectableRing> ringsStack = new List<SelectableRing>();//public so it could be used for the ExerciseSolver and for QA

	[HideInInspector] public int index; //Index in the exercise
		
	/**
	 * Ring added to the stack
	 */
	public void ringPush(SelectableRing ring)
	{
		ringsStack.Add(ring);
	}

	/**
	 * Remove and return the last ring (Last In First Out)
	 **/ 
	public SelectableRing ringPop()
	{
		if(!isEmpty())
		{
			SelectableRing result = ringsStack[ringsStack.Count-1];
			ringsStack.RemoveAt(ringsStack.Count-1);
			return result;
		}

		return null;
	}

	/**
	 * Return the last ring without removing anything
	 **/
	public SelectableRing ringPeek()
	{
		if(!isEmpty())
		{
			return ringsStack[ringsStack.Count-1];
		}

		return null;
	}

	/**
	 * clears the ring stack
	 **/ 
	public void ringClear()
	{
		ringsStack.Clear();
	}

	public bool isEmpty()
	{
		return (ringsStack.Count == 0);
	}

	public int ringCount()
	{
		return ringsStack.Count;
	}

	void OnTriggerEnter2D(Collider2D other) 
	{
		//A little visual feedback
		//scaleAnimation.Play();

		if(OnCollisionDetected != null)
		{
			OnCollisionDetected(other,GetInstanceID());
		}
	}

	void OnTriggerExit2D(Collider2D other)
	{
		if(OnCollisionLost != null)
		{
			OnCollisionLost(other,GetInstanceID());
		}
	}

	public void SetSelection(bool selected)
	{
		if(selected)
		{
			spriteToTint.color = selectedColor;
		}
		else
		{
			spriteToTint.color = unselectedColor;
		}
	}
}
