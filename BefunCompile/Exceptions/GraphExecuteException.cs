using System;

namespace BefunCompile.Exceptions
{
	public class GraphExecuteException : Exception
	{
		public GraphExecuteException(string m)
			: base(m)
		{
			//
		}

		public GraphExecuteException()
			: base()
		{
			//
		}
	}
}
