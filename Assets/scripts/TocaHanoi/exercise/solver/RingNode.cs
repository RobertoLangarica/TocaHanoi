using System.Collections;
using System.Collections.Generic;

/**
 * Logical representation of a node.
 * 
 * -The RingNode has a list of pending Pins to visit (for pathfinding).
 **/ 
public class RingNode : System.Object {

	public SelectableRing ringReference;//Reference to the object emulated by this node
	private List<ExercisePath> pendingPaths;//Paths waiting to be visited
	public int size;//We dont use the size of the ringReference because we need more control on this value
	public bool isSolved;//Indicate that this ring is already on his correct place
	public int weight;//Weight gived by the solver to this ring DEPRECATED
	public int step = 0;//Step when this ring was last moved (0 not moved)
	public int index;//Index reference used in the ExerciseSolver

	public RingNode(SelectableRing reference,int ringSize)
	{
		ringReference = reference;
		pendingPaths = new List<ExercisePath>();
		size = ringSize; 
	}

	public void emptyPendingPaths()
	{
		pendingPaths.Clear();
	}

	public void addPath(ExercisePath path)
	{
		//Tha paths are sorted by weight (from lowest to highest)
		if(pendingPaths.Count == 0)
		{
			pendingPaths.Add(path);
		}
		else
		{
			for(int i = 0; i < pendingPaths.Count; i++)
			{
				if(path.weight <= pendingPaths[i].weight)
				{
					pendingPaths.Insert(i,path);
					break;
				}
			}	
		}
	}

	/**
	 * REmove and retrieve first path
	 **/ 
	public ExercisePath pathShift()
	{
		if(pendingPaths.Count > 0)
		{
			ExercisePath result = pendingPaths[0];
			pendingPaths.RemoveAt(0);

			return result;
		}

		return null;
	}

	/**
	 * Return the first path without removing it from the list
	 **/ 
	public ExercisePath firstPathPeek()
	{
		if(pendingPaths.Count > 0)
		{
			return pendingPaths[0];
		}

		return null;
	}

	public bool noPendingPaths()
	{
		return pendingPaths.Count == 0;
	}
}
