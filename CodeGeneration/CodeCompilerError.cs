using System;

namespace BefunCompile.CodeGeneration
{
	public class CodeCompilerError : Exception
	{
		public readonly string StdErr;
		public readonly int ExitCode;

		public CodeCompilerError(string stderr, int exitcode) : base(exitcode + ": " + stderr)
		{
			StdErr = stderr;
			ExitCode = exitcode;
		}
	}
}
