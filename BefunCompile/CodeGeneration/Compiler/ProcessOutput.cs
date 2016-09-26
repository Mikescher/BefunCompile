namespace BefunCompile.CodeGeneration.Compiler
{
	public class ProcessOutput
	{
		public readonly int ExitCode;
		public readonly string StdOut;
		public readonly string StdErr;

		public ProcessOutput(int ex, string stdout, string stderr)
		{
			ExitCode = ex;
			StdOut = stdout;
			StdErr = stderr;
		}
	}
}
