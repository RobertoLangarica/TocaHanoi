using System.Collections;
using System.Collections.Generic;

/**
 * Object representing an exercise step.Used to reconstruct the exercise once is completed.
 * 
 * Contains the next information:
 * -From (pin from where the ring came)
 * -Ring (the ring that made the step)
 * -To  (destination pin)
 * -stepIndex
 **/ 
public class ExerciseStep : System.Object 
{
	public PinNode from;
	public PinNode to;
	public RingNode ring;
	public int index;

	public ExerciseStep(PinNode fromNode,PinNode toNode, RingNode ringNode, int stepIndex)
	{
		from = fromNode;
		to = toNode;
		ring = ringNode;
		index = stepIndex;
	}
}
