using System;
using System.IO;
using System.Text;

namespace BefunCompile.CodeGeneration.Compiler
{
	class CodeCompilerC : CodeCompiler
	{
		protected override void Compile(string code, string path, StringBuilder dbgOutput)
		{
			var fn1 = Path.GetTempPath() + Guid.NewGuid() + ".b93.c";

			File.WriteAllText(fn1, code);

			var gcc = ProcExecute("gcc", string.Format(" -x c \"{0}\" -o \"{1}\"", fn1, path), dbgOutput);

			if (gcc.ExitCode != 0)
			{
				File.Delete(fn1);

				throw new CodeCompilerError(gcc.StdErr, gcc.ExitCode);
			}
		}

		protected override string Execute(string path)
		{
			var prog = ProcExecute(path, string.Empty);

			if (prog.ExitCode != 0)
			{
				throw new CodeCompilerError(prog.StdOut, prog.ExitCode);
			}
			
			return prog.StdOut;
		}

		protected override string GetCodeExtension()
		{
			return "c";
		}

		protected override string GetBinaryExtension()
		{
			return "exe";
		}

		protected override string GetAcronym()
		{
			return "GCC";
		}
	}
}
