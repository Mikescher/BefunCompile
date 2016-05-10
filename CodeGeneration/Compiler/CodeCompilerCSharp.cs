using System.IO;
using System.Text;

namespace BefunCompile.CodeGeneration.Compiler
{
	class CodeCompilerCSharp : CodeCompiler
	{
		protected override void Compile(string code, string path, StringBuilder dbgOutput)
		{
			var fn1 = Path.GetTempFileName() + ".b93.cs";
			File.WriteAllText(fn1, code);

			var csc = ProcExecute("csc", string.Format("/out:\"{1}\" /optimize /nologo \"{0}\"", fn1, path), dbgOutput);

			if (csc.ExitCode != 0)
			{
				File.Delete(fn1);

				throw new CodeCompilerError(csc.StdOut, csc.ExitCode);
			}
		}

		protected override string Execute(string path)
		{
			var prog = ProcExecute(path, string.Empty);

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
