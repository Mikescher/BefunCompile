using BefunCompile.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace BefunCompile.Graph.Optimizations.Unstackify
{
	public class UnstackifyState
	{
		public List<UnstackifyValue> Stack;

		public UnstackifyState()
		{
			Stack = new List<UnstackifyValue>();
		}

		public UnstackifyState Clone()
		{
			var result = new UnstackifyState {Stack = this.Stack.ToList()};

			return result;
		}

		public void Push(UnstackifyValue v)
		{
			Stack.Add(v);
		}

		public UnstackifyValue Pop()
		{
			if (!Stack.Any())
				throw new UnstackifyWalkInvalidPopException();

			var last = Stack.Last();
			Stack.RemoveAt(Stack.Count - 1);
			return last;
		}

		public UnstackifyValue Peek()
		{
			if (!Stack.Any())
				throw new UnstackifyWalkInvalidPeekException();

			return Stack.Last();
		}

		public void AddScope(BCVertex vertex)
		{
			Stack.ForEach(p => p.AddScope(vertex));
		}

		public static bool StatesEqual(UnstackifyState a, UnstackifyState b)
		{
			if (a.Stack.Count != b.Stack.Count)
				return false;

			return !a.Stack.Where((t, i) => t != b.Stack[i]).Any();
		}

		public void Swap()
		{
			if (Stack.Count < 2)
				throw new UnstackifyWalkInvalidSwapException();

			var x1 = Pop();
			var x2 = Pop();

			Push(x1);
			Push(x2);
		}
	}
}
