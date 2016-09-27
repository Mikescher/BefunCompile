using System.IO;
using System.Linq;
using System.Text;

namespace BefunCompile.CodeGeneration.Compiler
{
	class CodeCompilerPython2 : CodeCompiler
	{
		protected override void Compile(string code, string path, StringBuilder dbgOutput)
		{
			File.WriteAllText(path, code);
		}

		protected override string Execute(string path)
		{
			var pyPath = FilesystemCompilerSearch.FindPYTH2().FirstOrDefault();
			if (pyPath == null) throw new CodeCompilerEnvironmentException("python-2 not found on this system");

			var prog = ProcessLauncher.ProcExecute(pyPath, string.Format("\"{0}\"", path));

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
			return "PY2";
		}
	}
}
