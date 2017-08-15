using System.IO;

namespace BefunCompile.CodeGeneration.Compiler
{
	class CodeCompilerBefunge93 : CodeCompiler
	{
		protected override void Compile(string code, string path, IOutputReciever dbgOutput)
		{
			File.WriteAllText(path, code);
		}

		protected override string Execute(string path, IOutputReciever dbgOutput, int? timeout = null)
		{
			var bfr = ProcessLauncher.ProcExecute("BefunRun", string.Format("\"{0}\" --errorlevel=3", path), dbgOutput, timeout);

			if (bfr.ExitCode != 0)
			{
				throw new CodeCompilerError(bfr.StdErr, bfr.ExitCode);
			}

			return bfr.StdOut;
		}

		protected override string GetCodeExtension()
		{
			return "b93";
		}

		protected override string GetBinaryExtension()
		{
			return "b93";
		}

		protected override string GetAcronym()
		{
			return "B93";
		}
	}
}
