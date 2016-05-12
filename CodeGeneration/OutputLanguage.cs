
using BefunCompile.CodeGeneration.Compiler;
using BefunCompile.CodeGeneration.Generator;
using System;

namespace BefunCompile.CodeGeneration
{
	public enum OutputLanguage
	{
		CSharp,
		C,
		Python2,
		Python3,
		Java,
	}

	public static class OutputLanguageHelper
	{
		public static OutputLanguage? ParseFromAbbrev(string abbrev)
		{
			switch (abbrev)
			{
				case "cs":
				case "csharp":
					return OutputLanguage.CSharp;
				case "c":
				case "ansic":
					return OutputLanguage.C;
				case "python":
				case "py":
				case "python3":
				case "py3":
					return OutputLanguage.Python3;
				case "python2":
				case "py2":
					return OutputLanguage.Python2;
				case "java":
					return OutputLanguage.Java;
				default:
					return null;
			}
		}

		public static CodeCompiler CreateCompiler(OutputLanguage ol)
		{
			switch (ol)
			{
				case OutputLanguage.CSharp:
					return new CodeCompilerCSharp();
				case OutputLanguage.C:
					return new CodeCompilerC();
				case OutputLanguage.Python2:
					return new CodeCompilerPython2();
				case OutputLanguage.Python3:
					return new CodeCompilerPython3();
				case OutputLanguage.Java:
					return new CodeCompilerJava();
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static CodeGenerator CreateGenerator(OutputLanguage ol)
		{
			switch (ol)
			{
				case OutputLanguage.CSharp:
					return new CodeGeneratorCSharp();
				case OutputLanguage.C:
					return new CodeGeneratorC();
				case OutputLanguage.Python2:
					return new CodeGeneratorPython2();
				case OutputLanguage.Python3:
					return new CodeGeneratorPython3();
				case OutputLanguage.Java:
					return new CodeGeneratorJava();
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
