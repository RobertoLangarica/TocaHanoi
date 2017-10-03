using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Objects that can be selected with Touch. 
 * 
 * Note: This objects are managed by and ObjectSelector.
 **/ 
public class SelectableObject : MonoBehaviour {

	protected Vector3 startPosition;
	protected Vector3 selectionOffset;

	public virtual void SelectionBegan(Vector2 selectionPoint)
	{
		startPosition = transform.position;
		selectionOffset = (Vector3)selectionPoint - startPosition;
	}

	public virtual void SelectionMoved(Vector3 selectionPosition)
	{
		transform.position = selectionPosition - selectionOffset;
		//Debug.Log("Position: "+ );
	}

	public virtual void SelectionStationary()
	{
		//Debug.Log("Stationary");
	}
		
	public virtual void SelectionEnded()
	{
		//Debug.Log("END");
	}

	public virtual void SelectionCanceled()
	{
		//Debug.Log("CANCEL");
	}
}
