using System.IO;
using System.Text;

namespace BefunCompile.CodeGeneration.Compiler
{
	class CodeCompilerPython : CodeCompiler
	{
		protected override void Compile(string code, string path, StringBuilder dbgOutput)
		{
			File.WriteAllText(path, code);
		}

		protected override string Execute(string path)
		{
			var prog = ProcExecute("python", string.Format("\"{0}\"", path));

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
			return "PYT";
		}
	}
}
