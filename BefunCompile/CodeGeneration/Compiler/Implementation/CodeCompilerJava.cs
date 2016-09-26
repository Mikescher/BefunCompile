using System;
using System.IO;
using System.Text;

namespace BefunCompile.CodeGeneration.Compiler
{
	class CodeCompilerJava : CodeCompiler
	{
		protected override void Compile(string code, string path, StringBuilder dbgOutput)
		{
			var guid = Guid.NewGuid();

			var fn0 = Path.Combine(Path.GetTempPath(), guid.ToString());
			var fn1 = Path.Combine(Path.GetTempPath(), guid.ToString(), "Program.java");
			var fn3 = Path.Combine(Path.GetTempPath(), guid.ToString(), "Program.class");

			Directory.CreateDirectory(fn0);

			File.WriteAllText(fn1, code);

			#region JVC

			var jvc = ProcExecute("javac", string.Format("\"{0}\"", fn1), dbgOutput);
			
			File.Delete(fn1);
				
			if (jvc.ExitCode != 0)
			{
				Directory.Delete(fn0, true);

				throw new CodeCompilerError(jvc.StdErr, jvc.ExitCode);
			}

			#endregion

			#region JAR

			var jar = ProcExecute("jar", string.Format("-cfve \"{0}\" Program \"{1}\"", path, Path.GetFileName(fn3)), fn0, dbgOutput);

			File.Delete(fn3);

			if (jar.ExitCode != 0)
			{
				Directory.Delete(fn0, true);

				throw new CodeCompilerError(jar.StdErr, jar.ExitCode);
			}

			#endregion

			Directory.Delete(fn0, true);
		}

		protected override string Execute(string path)
		{
			var prog = ProcExecute("java", string.Format("-jar \"{0}\"", path));

			if (prog.ExitCode != 0)
			{
				throw new CodeCompilerError(prog.StdErr, prog.ExitCode);
			}

			return prog.StdOut;
		}

		protected override string GetCodeExtension()
		{
			return "java";
		}

		protected override string GetBinaryExtension()
		{
			return "jar";
		}

		protected override string GetAcronym()
		{
			return "JVC";
		}
	}
}
