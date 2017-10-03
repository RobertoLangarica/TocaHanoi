using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ExerciseSolver : MonoBehaviour 
{
	public  ExerciseManager manager;

	private List<RingNode> rings = new List<RingNode>();
	private List<PinNode> pins = new List<PinNode>();
	private List<ExerciseStep> solution = new List<ExerciseStep>(); //List with every movement to solve the exercise
	private int starterPinIndex;//Pin where the exercise start
	private int solutionPinIndex = -1;//Pin with the solution
	private int searchCount;//How many searche cycles are we made to solve the exercise

	[HideInInspector]public bool isSearching = false;

	public void SolveExercise()
	{
		if(isSearching)
		{
			//stop previous search
			StopCoroutine("Search");	
		}

		Debug.Log("Trying to solve the exercise.");
		//Clean previous exercise
		rings.Clear();
		pins.Clear();
		solution.Clear();
		solutionPinIndex = -1;
		searchCount = 0;
		isSearching = true;

		//We get a copy of the exercise
		CopyExcerciseStatus();

		//work distribution in frames
		StartCoroutine("Search");
		//SearchForSolution();
	}


	/**
	 * Copy the actual status from the ExerciseManager.
	 **/ 
	private void CopyExcerciseStatus()
	{
		starterPinIndex = manager.starterPin;

		//rings
		foreach(SelectableRing ring in manager.rings)
		{
			//Custom size from higher to lower. Ensure that there is 1 step of size difference only
			AddRing(ring,manager.rings.Count-rings.Count);
		}

		//pins
		PinNode pinNode;
		Pin pinReference;
		RingNode ringReference;
		for(int i = 0; i < manager.pins.Count; i++)
		{
			//adding
			pinReference = manager.pins[i];
			AddPin(pinReference);
			pinNode = pins[pins.Count-1];

			//We populate the pinNode, using the population from pinReference
			for(int j = 0; j < pinReference.ringsStack.Count; j++)
			{
				pinNode.ringPush(getRingNodeFromReference(pinReference.ringsStack[j]));	
			}

			//What rings are already in place
			if(starterPinIndex != i && !pinNode.isEmpty())
			{
				for(int k = 0; k < pinNode.ringCount(); k++)
				{
					ringReference = pinNode.ringAt(k);
					if(k == 0)
					{
						//Must be the bigger one
						if(ringReference.size == biggestRing.size)
						{
							ringReference.isSolved = true;

							//This pin is also the solution
							solutionPinIndex = pins.Count-1;
						}
						else
						{
							//No body is already solved on this pin
							break;
						}
					}
					//They should be consecutives in size
					else if(ringReference.size+1 == pinNode.ringAt(k-1).size)
					{
						ringReference.isSolved = true;
					}
					else
					{
						//No body is already solved on this pin
						break;
					}
				}
			}
		}
	}

	private void AddRing(SelectableRing ring,int size)
	{
		rings.Add(new RingNode(ring,size));
		rings[rings.Count-1].index = rings.Count-1;
	}

	private void AddPin(Pin pin)
	{
		pins.Add(new PinNode(pin));
		pins[pins.Count-1].index = pins.Count-1;
	}

	/**Quick access to the biggest node**/
	private RingNode biggestRing
	{
		get{return rings[0];}
	}

	/**
	 * Search among the ringNodes for the one that has a reference that matched with the one provided
	 **/ 
	private RingNode getRingNodeFromReference(SelectableRing reference)
	{
		foreach(RingNode ring in rings)
		{
			/*
			 WARNING: We found a bug using GetInstanceID in getPinContainingRing and change it for a size comparation
			 but since the size of this reference it is not guaranteed to be always the same we hope for the best
			 */
			if(ring.ringReference.GetInstanceID() == reference.GetInstanceID())
			{
				return ring;
			}
		}

		return null;
	}

	private IEnumerator Search()
	{
		bool solved = false;
		bool failed = false;
		int count = 0;
		while(!solved)
		{
			searchCount++;
			failed = !SearchForSolution();

			if(solutionPinIndex != -1 && pins[solutionPinIndex].ringCount() == rings.Count)
			{
				solved = true;
				break;
			}

			if(failed)
			{
				break;
			}

			//Search by 10 in a frame so it doesn't take much user time
			if(++count == 10)
			{
				yield return 0;	
			}
		}

		if(solved)
		{
			//EXERCISE COMPLETED!!!
			Debug.Log("¡EXERCISE COMPLETED!");
			prettyPrintSolution();

			//TODO send it to the manager
			#if UNITY_EDITOR
			manager.SolveExerciseWith(solution);
			#endif

		}
		else
		{
			//No solution
			Debug.Log("There is no solution for the exercise.");
			prettyPrintSolution();
		}

		isSearching = false;
	}

	/**
	 * 
	 * If the search return false that means that there is no solution.
	 * 
	 * We use the following steps roughly speakin (better described inside the function)
	 * 	1. Get a list of the possible rings to move (rings on top of their pins).
	 * 	2. Pick a the best ring to move
	 * 	3. Get their possible destinations.
	 * 	4. Evaluate and sort each destination (so we always accessed the more feasible one)
	 * 	6. Move and evaluate the result.
	 * 	7. Repeat until solved
	 * 
	 *  return false:is something fail,  true: is everything is ok
	 **/ 
	private bool SearchForSolution()
	{
		//first we get the rings on top of the pin
		List<RingNode> ringOptions = getRingsOnTop();
		RingNode ringToMove;

		//We filter the options
		if(ringOptions.Count == 0)
		{
			//In theory this should never happen so we notify it
			Debug.LogWarning("There is no ring to move. Something is wrong with the format of the exercise.");
			//We end the search
			return false;
		}
		else 
		{
			//Get the ring for move. Assuming there is only 3 pins, we could only move one ring (when there is two we ignored the last moved one)
			ringToMove = getBestRingToMove(ringOptions);

			if(ringToMove == null)
			{
				//In theory this should never happen so we notify it
				Debug.LogWarning("There is no ring to move. Something is wrong with the format of the exercise.");
				//We end the search
				return false;
			}
				
			//fill posible destinations
			fillPendingPaths(ringToMove);
		}

		//Traverse the first path on ring
		visitPath(ringToMove.pathShift());

		return true;
	}

	/**
	 * Retreive a list with every ring that is on top of a pin
	 **/ 
	private List<RingNode> getRingsOnTop()
	{
		List<RingNode> result = new List<RingNode>();

		foreach(PinNode pin in pins)
		{
			if(!pin.isEmpty())
			{
				result.Add(pin.ringPeek());
			}
		}

		return result;
	}

	/**
	 * Filter for the rings that can be moved DEPRECATED
	 **/
	private void filterOnlyMovableRings(List<RingNode> target)
	{
		for(int i = target.Count-1; i >= 0; i--)
		{
			//If the ring is in place then we shouldn't move it
			if(target[i].isSolved)
			{
				target.RemoveAt(i);	
			}
			else if(!DoRingFitInAnyPin(target[i]))
			{
				//if the ring has no possible destination then we remove it
				target.RemoveAt(i);	
			}
		}

		if(target.Count == 0)
		{
			prettyPrintSolution();
		}
	}

	private bool DoRingFitInAnyPin(RingNode ring)
	{
		foreach(PinNode pin in pins)
		{
			if(pin.isEmpty() || ring.size < pin.ringPeek().size)
			{
				return true;
			}
		}

		//No fit
		return false;
	}

	private void fillPendingPaths(RingNode ring)
	{
		PinNode pin;

		for(int i = 0; i < pins.Count; i++)
		{
			pin = pins[i];

			//This ring could fit in the pin
			if(pin.isEmpty() || ring.size < pin.ringPeek().size)
			{
				//NEW PATH
				ExercisePath path = new ExercisePath(0,pin,ring);

				//Calculate the path weight (so we can decide wich path is better)
				//Less weight makes a better path

				//if is occupied then it weights more than an empty one
				if(!pin.isEmpty())
				{
					path.weight += 1;

					//If the firs ring in the pin isn't solved it could have advantage based on size
					if(!pin.ringPeek().isSolved)
					{
						//Better than an empty one.
						path.weight -= (pin.getFirstUnsolvedRing().size - ring.size)*2;
					}
				}

				//If the path is the solution for this ring it has more weight
				if(pin.isEmpty())
				{
					//Is the biggest one
					if(ring.size == biggestRing.size)
					{
						path.weight -= rings.Count*2;
					}
				}
				else
				{
					RingNode rn = pin.ringPeek();

					if(rn.isSolved && ring.size + 1 == pin.ringPeek().size)
					{
						path.weight -= rings.Count*2;
					}
				}

				//add the path to the ring
				ring.addPath(path);
			}
		}
	}
		
	private RingNode getBestRingToMove(List<RingNode> options)
	{
		RingNode result = null;
		bool couldBeMoved;

		//choose
		foreach(RingNode ring in options)
		{
			//could be moved?
			couldBeMoved = false;

			if(!ring.isSolved)
			{
				for(int i = 0; i < pins.Count; i++)
				{
					//This ring could fit in the Path
					if(pins[i].isEmpty() || ring.size < pins[i].ringPeek().size)
					{
						couldBeMoved = true;
						break;
					}
				}
			}

			if(!couldBeMoved)
			{
				continue;
			}


			//There is no choice
			if(result == null)
			{
				result = ring;
			}
			//if this is the last ring in the solution then it isn't the best choice
			else if(solution.Count == 0 || solution[solution.Count-1].ring.size != ring.size)
			{
				result = ring;
			}
		}

		return result;
	} 

	private void visitPath(ExercisePath path)
	{
		//Adding the step to the solution
		ExerciseStep step = new ExerciseStep(getPinContainingRing(path.from),path.destination,path.from,solution.Count+1);
		solution.Add(step);
		path.from.step = step.index;

		//Changing the exercise status
		step.from.ringPop();//Remove the node from the current pin

		//Are we moving the biggest ring? (it is only moved once)
		if(step.ring.size == biggestRing.size)
		{
			solutionPinIndex = step.to.index;
			step.ring.isSolved = true;
		}
		//is this the final position for the ring?
		else if(step.to.index == solutionPinIndex)
		{
			RingNode rn = step.to.ringPeek();
			if(rn.isSolved && rn.size == step.ring.size+1)
			{
				step.ring.isSolved = true;	
			}
		}

		step.to.ringPush(step.ring);//Adding the ring to the destination

		//Erase pending paths from the previous step
		if(solution.Count > 1)
		{
			solution[solution.Count-2].ring.emptyPendingPaths();
		}
	}
		
	private PinNode getPinContainingRing(RingNode ring)
	{
		for(int i = 0; i < pins.Count; i++)
		{
			/*getPinContainingRing
			 We use size to check for the ring since there was a nasty bug with GetInstanceID
			*/
			if(!pins[i].isEmpty() && pins[i].ringPeek().size == ring.size)
			{
				return pins[i];
			}
		}

		return null;
	}

	private void prettyPrintSolution()
	{
		StringBuilder builder = new StringBuilder("Solution in ");

		builder.AppendFormat("{0} steps",solution.Count);

		for(int i = 0; i < solution.Count; i++)
		{
			builder.AppendFormat("\n\tS[{2}] R({0})->P({1})",solution[i].ring.index,solution[i].to.index,(i+1));
		}

		builder.Append("\n]");

		Debug.Log(builder.ToString());
	}
}
