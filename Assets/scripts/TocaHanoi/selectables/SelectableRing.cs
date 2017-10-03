using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Implementation of SelectableObject for the rings of the Hanoi excercise;
 * 
 * This rings:
 * 	-Ask for permission before selection.
 * 	-Have a selection position offset more natural.
 * 	-Have more natural movements and behaviour when dragged.
 * 	-Notify when dropped (after any effect has finished).
 **/ 
public class SelectableRing : SelectableObject 
{
	
	public Collider2D collider;
	public SpriteRenderer backSprite;
	public SpriteRenderer frontSprite;

	[Header("Interaction")]
	public float maxHeightPercentToAnchor = 0.33f; //0-bottom. 1-top. When the selection get over this value it gets down
	public float maxWidthPercentToAnchor = 0.33f; //0-left. 1-right. When the selection get over this value(on any side) it gets down
	public float onDeviceYOffset = 0.9f;//Offset used only in mobile (so the ring move over the finger);
	public string selectedSortingLayer = "Interaction";
	public bool askBeforeGetSelected = true;//If true an external script should call OnSelectionAllowed

	[Header("Game configuration")]
	public int sizeIndicator = 0;//Used in the game to evaluate the size of this ring

	[Header("Effects")]
	public ScaleSelectionAnimation selectionAnimation;
	public ContinuousMovementAnimation movementAnimation;
	public ContinuousRotationAnimation rotationAnimation;


	public Action<SelectableRing> OnDropped;
	public Action<SelectableRing> OnCanceled;//when the movement was canceled (by externals factors to the game)
	public Action<SelectableRing> AskSelectionPermit;
	[HideInInspector]public int pin;//used for exercise validation outside this script 

	private string backLayer;//sorting layer of the back sprite (we store it so we can restore the sorting)
	private string frontLayer;//sorting layer of the front sprite (we store it so we can restore the sorting)
	private int backSortingOrder;//sorting index of the back sprite (we store it so we can restore the sorting)
	private int frontSortingOrder;//sorting index of the frony sprite (we store it so we can restore the sorting)
	private Vector2 selectionPosition;
	private bool waitingSelection;//when this object is waiting for selection permit
	private Bounds noTransformedBounds;//Boundaries with no transforms at start (used calculate the rotation)

	void Start()
	{
		//Reference values so we can get the sprites back to they original sorting layer/order
		backLayer = backSprite.sortingLayerName;
		frontLayer = frontSprite.sortingLayerName;

		backSortingOrder = backSprite.sortingOrder;
		frontSortingOrder = frontSprite.sortingOrder;

		//when the ring is created dynamically the scale could be changed
		selectionAnimation.finalScaleValue = this.transform.localScale;
		selectionAnimation.intialScaleValue= this.transform.localScale;

		//For rotation
		noTransformedBounds = collider.bounds;
	}

	public override void SelectionBegan(Vector2 selectionPoint)
	{
		selectionPosition = selectionPoint;
		if(askBeforeGetSelected)
		{
			//We ask for persmission
			waitingSelection = true;

			if(AskSelectionPermit != null)
			{
				AskSelectionPermit(this);
			}
		}
		else
		{
			//normal selection
			waitingSelection = false;
			SelectionAllowed();

		}
	} 

	public void SelectionAllowed()
	{
		waitingSelection = false;

		base.SelectionBegan(selectionPosition);

		//The effect whe it gets selected
		DoSelectionEffect();

		Vector3 offset = GetOffsetCorrection(selectionPosition);

		//If the offset corrected is different from the actual
		if(offset != selectionOffset)
		{
			selectionOffset = offset;

			//Trigger a move so it gets updated using the current selection position
			Vector3 newPos = new Vector3(selectionPosition.x,selectionPosition.y,transform.position.z);
			SelectionMoved(newPos);
		}


	}

	/**
	 * Visual effect for when this object is selected
	 **/ 
	public void DoSelectionEffect()
	{
		//Sorting order so the object is in front of everything
		SortingOrderForSelection(true);
		//little pop when selected
		selectionAnimation.Play();
	}

	public void OnSelectionDenied()
	{
		//TODO any effect?
	}

	public override void SelectionMoved (Vector3 selectionPosition)
	{
		if(waitingSelection)
		{
			//we never get really selected
			return;
		}

		RotateTowardsMovement(selectionPosition);

		#if UNITY_EDITOR
		/*HARDCODING I'm forcing a duration a little bit longer on editor.
		 A quicker move feels right on device but not in the editor. 
		 I want that you have a good experience on the testing*/
		movementAnimation.duration = movementAnimation.duration*1.66f;
		#endif

		//Move towards the selection with some easing
		movementAnimation.destination = selectionPosition - selectionOffset;
		movementAnimation.Play();

		#if UNITY_EDITOR
		//HARDCODING 
		movementAnimation.duration = movementAnimation.duration/1.66f;
		#endif
	}

	public override void SelectionStationary ()
	{
		base.SelectionStationary ();

		RemoveRotation();
	}
	public override void SelectionEnded()
	{
		if(waitingSelection)
		{
			//we never get really selected
			return;
		}

		base.SelectionEnded();

		RemoveRotation();
		//sorting back to normal
		SortingOrderForSelection(false);




		if(OnDropped != null)
		{
			//Only notofy whitout any movement
			if(movementAnimation.isPlaying)
			{
				movementAnimation.StopAnimation();	
			}

			OnDropped(this);
		}
	}

	public override void SelectionCanceled()
	{
		if(waitingSelection)
		{
			//we never get really selected
			return;
		}

		base.SelectionCanceled();

		//sorting back to normal
		SortingOrderForSelection(false);

		if(OnCanceled != null)
		{
			OnCanceled(this);	
		}
	}
		
	/**
	 * Offset correction so the object has the anchor movement in a safe visual area
	 **/ 
	private Vector3 GetOffsetCorrection(Vector2 selectionPoint)
	{
		//Offfset correction so it feels natural
		Bounds bounds = collider.bounds;//bounds change with every new position so it can't be cached

		//computing safe area
		float heightLimit = bounds.min.y + bounds.size.y*maxHeightPercentToAnchor;
		float leftLimit = bounds.min.x + bounds.size.x*maxWidthPercentToAnchor;
		float rightLimit = bounds.max.x - bounds.size.x*maxWidthPercentToAnchor;
		Vector3 newPos;
		Vector3 newOffset;


		//Check if the selection where outside the safe area
		if(selectionPoint.y > heightLimit || (selectionPoint.x < leftLimit || selectionPoint.x > rightLimit))
		{
			//reference to the selction point
			newPos= new Vector3(selectionPoint.x,selectionPoint.y,transform.position.z);

			//Y
			if(selectionPoint.y > heightLimit)
			{
				//We move the object so it lays on the heightLimit	
				newPos.y = heightLimit;
			}

			//X
			if(selectionPoint.x > rightLimit)
			{
				//To the right limit	
				newPos.x = rightLimit;
			}
			else if(selectionPoint.x < leftLimit)
			{
				//To the left limit	
				newPos.x = leftLimit;
			}
				
			//new offset for every movement using the new position
			newOffset = newPos - startPosition;

			//Offset only for device
			#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
			newOffset.y -= onDeviceYOffset;
			#endif

			return newOffset;
		}
			
		//Offset only for device
		#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
		newOffset = selectionOffset;
		newOffset.y -= onDeviceYOffset;
		return newOffset;
		#endif

		//offset with no change
		return selectionOffset;
	}
		
	private void RotateTowardsMovement(Vector3 destination)
	{
		float angle = 45;
		float upAngle = 45;
		float downAngle = 30;
		//We get the new center and assume that there is no neew size
		noTransformedBounds.center = collider.bounds.center;
		/**
		 * To wich quadrant is the destination pointing
		 * -------------
		 * |  1  |  2  |
		 * -------------
		 * |  3  |  4  |
		 * -------------
		 **/ 
		if(destination.x < noTransformedBounds.center.x)
		{
			//1 or 3
			if(destination.y > noTransformedBounds.center.y)
			{
				//1
				angle = -upAngle;	
			}
			else
			{
				//3
				angle = downAngle;
			}
		}
		else
		{
			//2 or 4
			if(destination.y < noTransformedBounds.center.y)
			{
				//4
				angle = -downAngle;	
			}
			else
			{
				//2
				angle = upAngle;
			}
		}

		//We clamp the destination to a point that is contained in the boundaries
		destination.x = Mathf.Clamp(destination.x,noTransformedBounds.min.x,noTransformedBounds.max.x);
		destination.y = Mathf.Clamp(destination.y,noTransformedBounds.min.y,noTransformedBounds.max.y);

		//Vector from center to max
		Vector3 max = noTransformedBounds.max - noTransformedBounds.center;
		//Vector from center to calmped destination
		Vector3 d = destination - noTransformedBounds.center;
		//Angle percentage depending on how far from the center is the movement
		float anglePercentage = d.sqrMagnitude/max.sqrMagnitude;
		angle *= anglePercentage;

		//Rotate
		rotationAnimation.angle = angle;
		rotationAnimation.delay = 0;
		rotationAnimation.Play();
	}
		
	private void RemoveRotation()
	{
		//remove rotation
		rotationAnimation.angle = 0;
		//rotationAnimation.delay = rotationAnimation.duration;
		rotationAnimation.Play();
	}

	/**
	 * Sets the sorting order for the objetc when is selected and when is not
	 **/
	public void SortingOrderForSelection(bool isSelected)
	{
		if(isSelected)
		{
			//values in front of everything
			backSprite.sortingLayerName = selectedSortingLayer;
			frontSprite.sortingLayerName = selectedSortingLayer;

			//backSprite.sortingOrder = 0;
			//frontSprite.sortingOrder = 1;

		}
		else
		{
			//Back to the original values
			backSprite.sortingLayerName = backLayer;
			frontSprite.sortingLayerName = frontLayer;

			backSprite.sortingOrder = backSortingOrder;
			frontSprite.sortingOrder = frontSortingOrder;
		}
	}

	/**
	 * Change the frontSprite sortingOrder 
	 * Note: When the rings start to scramble they also overlap one at each other 
	 * and we need to arrange the sorting order dynamically to avoid that
	 **/ 
	public void setFrontSortingOrder(int sortingOrder)
	{
		frontSortingOrder = sortingOrder;
		frontSprite.sortingOrder = frontSortingOrder;
	}
}
