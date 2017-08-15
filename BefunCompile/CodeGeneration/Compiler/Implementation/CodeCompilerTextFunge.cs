using System.IO;

namespace BefunCompile.CodeGeneration.Compiler
{
	class CodeCompilerTextFunge : CodeCompiler
	{
		protected override void Compile(string code, string path, IOutputReciever dbgOutput)
		{
			var fn1 = Path.GetTempFileName() + ".tf";
			File.WriteAllText(fn1, code);

			var bgc = ProcessLauncher.ProcExecute("BefunGen", string.Format("\"{0}\" \"{1}\"", fn1, path), dbgOutput, TIMEOUT_COMPILE);

			if (bgc.ExitCode != 0)
			{
				File.Delete(fn1);

				throw new CodeCompilerError(bgc.StdErr, bgc.ExitCode);
			}
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
