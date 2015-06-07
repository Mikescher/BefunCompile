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
			var result = new UnstackifyState();
			result.Stack = this.Stack.ToList();

			return result;
		}

		public void Push(UnstackifyValue v)
		{
			Stack.Add(v);
		}

		public UnstackifyValue Pop()
		{
			var last = Stack.Last();
			Stack.RemoveAt(Stack.Count - 1);
			return last;
		}

		public UnstackifyValue Peek()
		{
			return Stack.Last();
		}

		public static bool StatesEqual(UnstackifyState a, UnstackifyState b)
		{
			if (a.Stack.Count != b.Stack.Count)
				return false;

			for (int i = 0; i < a.Stack.Count; i++)
			{
				if (a.Stack[i] != b.Stack[i])
					return false;
			}

			return true;
		}

		public void Swap()
		{
			var x1 = Pop();
			var x2 = Pop();

			Push(x1);
			Push(x2);
		}
	}
}
