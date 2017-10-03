using System.Collections;
using System.Collections.Generic;

/**
 * Loginc representation of a Pin (a stack of rings) 
 **/
public class PinNode : System.Object {

	private List<RingNode> ringStack;
	public Pin pinReference;//Reference to the object emulated by this node
	public int index;//Index reference used in the ExerciseSolver

	public PinNode(Pin reference)
	{
		pinReference = reference;
		ringStack = new List<RingNode>();
	}
	/**
	 * Ring added to the stack
	 */
	public void ringPush(RingNode ring)
	{
		ringStack.Add(ring);
	}

	/**
	 * Remove and return the last ring (Last In First Out)
	 **/ 
	public RingNode ringPop()
	{
		if(!isEmpty())
		{
			RingNode result = ringStack[ringStack.Count-1];
			ringStack.RemoveAt(ringStack.Count-1);
			return result;
		}

		return null;
	}

	/**
	 * Return the last ring without removing anything
	 **/
	public RingNode ringPeek()
	{
		if(!isEmpty())
		{
			return ringStack[ringStack.Count-1];
		}

		return null;
	}

	public RingNode ringAt(int ringIndex)
	{
		return ringStack[ringIndex];
	}

	public RingNode getFirstUnsolvedRing()
	{
		for(int i = 0; i < ringStack.Count; i++)
		{
			if(!ringStack[i].isSolved)
			{
				return ringStack[i];
			}
		}

		return null;
	}

	public bool isEmpty()
	{
		return (ringStack.Count == 0);
	}

	public int ringCount()
	{
		return ringStack.Count;
	}
}
