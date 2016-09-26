using System;

namespace BefunCompile.Exceptions
{
	public class CodeGenException : Exception
	{
		public CodeGenException(string m)
			: base(m)
		{
			//
		}

		public CodeGenException()
			: base()
		{
			//
		}
	}
}
