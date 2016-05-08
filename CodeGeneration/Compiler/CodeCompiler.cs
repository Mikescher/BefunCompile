using System;
using System.Collections.Generic;
using System.Text;

namespace BefunCompile.CodeGeneration.Compiler
{
	public abstract class CodeCompiler
	{
		private static Dictionary<OutputLanguage, CodeCompiler> _instances = null;

		private static CodeCompiler Instance(OutputLanguage lang)
		{
			if (_instances == null) _instances = new Dictionary<OutputLanguage, CodeCompiler>();

			if (_instances.ContainsKey(lang)) return _instances[lang];

			switch (lang)
			{
				case OutputLanguage.CSharp:
					return _instances[lang] = new CodeCompilerCSharp();
				case OutputLanguage.C:
					return _instances[lang] = new CodeCompilerC();
				case OutputLanguage.Python:
					return _instances[lang] = new CodeCompilerPython();
				case OutputLanguage.Java:
					return _instances[lang] = new CodeCompilerJava();
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static void Compile(OutputLanguage l, string code, string path, StringBuilder DebugOutput)
		{
			Instance(l).Compile(code, path, DebugOutput);
		}

		public static string Execute(OutputLanguage l, string path)
		{
			return Instance(l).Execute(path);
		}

		public static string GetCodeExtension(OutputLanguage l)
		{
			return Instance(l).GetCodeExtension();
		}

		public static string GetBinaryExtension(OutputLanguage l)
		{
			return Instance(l).GetBinaryExtension();
		}

		public static object GetAcronym(OutputLanguage l)
		{
			return Instance(l).GetAcronym();
		}

		protected abstract void Compile(string code, string path, StringBuilder DebugOutput);
		protected abstract string Execute(string path);
		protected abstract string GetCodeExtension();
		protected abstract string GetBinaryExtension();
		protected abstract string GetAcronym();
	}
}
