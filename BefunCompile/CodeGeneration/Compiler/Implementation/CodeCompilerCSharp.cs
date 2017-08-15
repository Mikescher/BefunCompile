using System.IO;
using System.Linq;

namespace BefunCompile.CodeGeneration.Compiler
{
	class CodeCompilerCSharp : CodeCompiler
	{
		protected override void Compile(string code, string path, IOutputReciever dbgOutput)
		{
			var cscPath = FilesystemCompilerSearch.FindCSC().FirstOrDefault();
			if (cscPath == null) throw new CodeCompilerEnvironmentException("csc not found on this system");

			var fn1 = Path.GetTempFileName() + ".b93.cs";
			File.WriteAllText(fn1, code);

			var csc = ProcessLauncher.ProcExecute(cscPath, string.Format("/out:\"{1}\" /optimize /nologo \"{0}\"", fn1, path), dbgOutput, TIMEOUT_COMPILE);

			if (csc.ExitCode != 0)
			{
				File.Delete(fn1);

				throw new CodeCompilerError(csc.StdOut, csc.ExitCode);
			}
		}

		protected override string Execute(string path, IOutputReciever dbgOutput, int? timeout = null)
		{
			var prog = ProcessLauncher.ProcExecute(path, string.Empty, dbgOutput, timeout);

			if (prog.ExitCode != 0)
			{
				throw new CodeCompilerError(prog.StdErr, prog.ExitCode);
			}
			
			return prog.StdOut;
		}

		protected override string GetCodeExtension()
		{
			return "cs";
		}

		protected override string GetBinaryExtension()
		{
			return "exe";
		}

		protected override string GetAcronym()
		{
			return "CSC";
		}
	}
}
