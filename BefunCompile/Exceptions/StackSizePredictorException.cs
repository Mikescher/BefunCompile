using System;

namespace BefunCompile.Exceptions
{
	class StackSizePredictorException : Exception
	{
		public StackSizePredictorException(string m)
			: base(m)
		{
			//
		}

		public StackSizePredictorException()
			: base()
		{
			//
		}
	}
}
