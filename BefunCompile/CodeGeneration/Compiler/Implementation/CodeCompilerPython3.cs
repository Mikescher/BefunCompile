using System.IO;
using System.Linq;

namespace BefunCompile.CodeGeneration.Compiler
{
	class CodeCompilerPython3 : CodeCompiler
	{
		protected override void Compile(string code, string path, IOutputReciever dbgOutput)
		{
			File.WriteAllText(path, code);
		}

		protected override string Execute(string path, IOutputReciever dbgOutput, int? timeout = null)
		{
			var pyPath = FilesystemCompilerSearch.FindPYTH3().FirstOrDefault();
			if (pyPath == null) throw new CodeCompilerEnvironmentException("python-3 not found on this system");

			var prog = ProcessLauncher.ProcExecute(pyPath, string.Format("\"{0}\"", path), dbgOutput, timeout);

			if (prog.ExitCode != 0)
			{
				throw new CodeCompilerError(prog.StdErr, prog.ExitCode);
			}

			return prog.StdOut;
		}

		protected override string GetCodeExtension()
		{
			return "py";
		}

		protected override string GetBinaryExtension()
		{
			return "py";
		}

		protected override string GetAcronym()
		{
			return "PY3";
		}
	}
}
