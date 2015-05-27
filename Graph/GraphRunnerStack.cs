using System.Collections.Generic;

namespace BefunCompile.Graph
{
	public class GraphRunnerStack
	{
		public Stack<long> stack = new Stack<long>();

		public long Pop()
		{
			return stack.Count == 0 ? 0 : stack.Pop();
		}

		public long Peek()
		{
			return stack.Count == 0 ? 0 : stack.Peek();
		}

		public bool PopBool()
		{
			return Pop() != 0;
		}

		public void Swap()
		{
			var a = Pop();
			var b = Pop();
			Push(a);
			Push(b);
		}

		public void Dup()
		{
			Push(Peek());
		}

		public void Push(long v)
		{
			stack.Push(v);
		}

		public void Push(bool b)
		{
			stack.Push(b ? 1 : 0);
		}
	}
}
