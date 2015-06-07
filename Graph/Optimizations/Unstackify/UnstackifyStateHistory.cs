using System;
using System.Collections.Generic;

namespace BefunCompile.Graph.Optimizations.Unstackify
{
	public class UnstackifyStateHistory
	{
		public Dictionary<BCVertex, UnstackifyState> StateDict;

		public HashSet<UnstackifyValue> StackValues;

		public UnstackifyStateHistory()
		{
			StateDict = new Dictionary<BCVertex, UnstackifyState>();
			StackValues = new HashSet<UnstackifyValue>();
		}

		public void AddState(BCVertex vertex, UnstackifyState state)
		{
			if (Contains(vertex))
				throw new Exception();

			StateDict[vertex] = state;

			state.Stack.ForEach(p => StackValues.Add(p));
		}

		public bool Contains(BCVertex vx)
		{
			return StateDict.ContainsKey(vx);
		}
	}
}
