using System.Collections;
using System.Collections.Generic;

/**
 * A Path is on object representing a posible step, it has weight and destination (Pin)
 **/ 
public class ExercisePath : System.Object {

	public int weight = 0;
	public PinNode destination;
	public RingNode from;

	public ExercisePath(int pathWeight, PinNode destinationPin, RingNode fromRing)
	{
		weight = pathWeight;
		destination = destinationPin;
		from = fromRing;
	}
}
