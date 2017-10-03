using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * ObjectSelector use a TouchSensor to evaluate when an object is selected and react properly.
 * 
 * NOTE: In order to be selected, the targets should have a Selectable script.
 **/ 
public class ObjectSelector : MonoBehaviour 
{
	public LayerMask LayerFilter;
	public TouchSensor Sensor;

	private Dictionary<int,Selection> selectedObjects;//Selection is declared at the end of the file

	void Start () 
	{
		selectedObjects = new Dictionary<int, Selection>();

		if(Sensor == null)
		{
			Debug.LogError("Sensor is null. A TouchSensor or MouseTouchSensor is needed.");
		}

		Sensor.OnTouchBegan += OnTouchBegan;
		Sensor.OnTouchStationary += OnTouchStationary;
		Sensor.OnTouchMoved += OnTouchMoved;
		Sensor.OnTouchEnded += OnTouchEnded;
		Sensor.OnTouchCanceled += OnTouchCanceled;
	}
	

	private void OnTouchBegan(Touch touch)
	{
		//TODO Multi selection
		if(existSelection())
		{
			//There is a previous selection so we ignore the incoming touch
			return;
		}

		//Raycast to the world so we can pick an object
		RaycastHit2D raycast = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(new Vector3(touch.position.x,touch.position.y,0)),Vector3.forward,1000,LayerFilter);

		if(raycast.collider != null)
		{
			SelectableObject selectedObject = raycast.collider.gameObject.GetComponent<SelectableObject>();

			if(selectedObject != null)
			{
				//adding selection
				selectedObjects.Add(touch.fingerId,new Selection());
				selectedObjects[touch.fingerId].obj = selectedObject;
				selectedObjects[touch.fingerId].selectorId = touch.fingerId;


				//notify about the selection
				selectedObjects[touch.fingerId].obj.SelectionBegan(raycast.point);
			}
		}
	}

	public bool existSelection()
	{
		return selectedObjects.Count > 0;
	}
		
	private void OnTouchStationary(Touch touch)
	{
		//Search for an object selected by this finger
		Selection currentSelected = getSelectionFromFinger(touch.fingerId);

		if(currentSelected == null)
		{
			//no selection
			return;
		}

		currentSelected.obj.SelectionStationary();
	}

	private void OnTouchMoved(Touch touch)
	{
		//Search for an object selected by this finger
		Selection currentSelected = getSelectionFromFinger(touch.fingerId);

		if(currentSelected == null)
		{
			//no selection
			return;
		}

		Vector3 newPosition = Camera.main.ScreenToWorldPoint(new Vector3(touch.position.x,touch.position.y,0));
		//We keep the object Z coordinate
		newPosition.z = currentSelected.obj.transform.position.z;
		currentSelected.obj.SelectionMoved(newPosition);
	}

	private void OnTouchEnded(Touch touch)
	{
		//Search for an object selected by this finger
		Selection currentSelected = getSelectionFromFinger(touch.fingerId);

		if(currentSelected == null)
		{
			//no selection
			return;
		}

		removeSelectionFromFinger(touch.fingerId);
		currentSelected.obj.SelectionEnded();
	}

	private void OnTouchCanceled(Touch touch)
	{
		//Search for an object selected by this finger
		Selection currentSelected = getSelectionFromFinger(touch.fingerId);

		if(currentSelected == null)
		{
			//no selection
			return;
		}

		removeSelectionFromFinger(touch.fingerId);
		currentSelected.obj.SelectionCanceled();
	}

	/**
	 * Returns the selection if there is one that match with the finger
	 **/ 
	private Selection getSelectionFromFinger(int fingerId)
	{
		if(selectedObjects.ContainsKey(fingerId))
		{
			return selectedObjects[fingerId];
		}

		//empty selection
		return null;
	}

	private void removeSelectionFromFinger(int fingerId)
	{
		if(selectedObjects.ContainsKey(fingerId))
		{
			selectedObjects.Remove(fingerId);
		}
	}
}

public class Selection
{
	public SelectableObject obj;
	public int selectorId;

	public Selection()
	{
		obj = null;
		selectorId = -1;
	}
}
