using System;

namespace BefunCompile.Exceptions
{
	public abstract class UnstackifyWalkException : Exception
	{
		public UnstackifyWalkException(string m)
			: base(m)
		{
			//
		}

		public UnstackifyWalkException()
			: base()
		{
			//
		}
	}
}
