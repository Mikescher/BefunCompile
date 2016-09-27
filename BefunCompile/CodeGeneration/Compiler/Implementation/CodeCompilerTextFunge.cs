using System.IO;
using System.Text;

namespace BefunCompile.CodeGeneration.Compiler
{
	class CodeCompilerTextFunge : CodeCompiler
	{
		protected override void Compile(string code, string path, StringBuilder dbgOutput)
		{
			var fn1 = Path.GetTempFileName() + ".tf";
			File.WriteAllText(fn1, code);

			var bgc = ProcessLauncher.ProcExecute("BefunGen", string.Format("\"{0}\" \"{1}\"", fn1, path), dbgOutput);

			if (bgc.ExitCode != 0)
			{
				File.Delete(fn1);

				throw new CodeCompilerError(bgc.StdErr, bgc.ExitCode);
			}
		}

		protected override string Execute(string path)
		{
			var bfr = ProcessLauncher.ProcExecute("BefunRun", string.Format("\"{0}\" --errorlevel=3", path));

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
