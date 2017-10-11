using System.IO;

namespace BefunCompile.CodeGeneration.Compiler
{
	class CodeCompilerTextFunge : CodeCompiler
	{
		protected override void Compile(string code, string path, IOutputReciever dbgOutput)
		{
			File.WriteAllText(path, code);
		}

		protected override string Execute(string path, IOutputReciever dbgOutput, int? timeout = null)
		{
			var bfr = ProcessLauncher.ProcExecute("BefunGen", string.Format("\"{0}\" --directrun", path), dbgOutput, timeout);

			if (bfr.ExitCode != 0)
			{
				throw new CodeCompilerError(bfr.StdErr, bfr.ExitCode);
			}

			return bfr.StdOut;
		}

		protected override string GetCodeExtension()
		{
			return "tf";
		}

		protected override string GetBinaryExtension()
		{
			return "b93";
		}

		protected override string GetAcronym()
		{
			return "TF";
		}
	}
}
