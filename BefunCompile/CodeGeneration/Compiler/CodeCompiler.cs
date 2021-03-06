﻿using System;
using System.Collections.Generic;
using System.IO;

namespace BefunCompile.CodeGeneration.Compiler
{
	public abstract class CodeCompiler
	{
		protected const int TIMEOUT_COMPILE = 16 * 1000; // ms

		private static Dictionary<OutputLanguage, CodeCompiler> _instances = null;

		private static CodeCompiler Instance(OutputLanguage lang)
		{
			if (_instances == null) _instances = new Dictionary<OutputLanguage, CodeCompiler>();

			if (_instances.ContainsKey(lang)) return _instances[lang];

			return _instances[lang] = OutputLanguageHelper.CreateCompiler(lang);
		}

		public static void Compile(OutputLanguage l, string code, string path)
		{
			Compile(l, code, path, new DummyReciever());
		}

		public static void Compile(OutputLanguage l, string code, string path, IOutputReciever dbgOutput)
		{
			Instance(l).Compile(code, path, dbgOutput);
		}

		public static string Execute(OutputLanguage l, string path, int? timeout = null)
		{
			return Execute(l, path, new DummyReciever(), timeout);
		}

		public static string Execute(OutputLanguage l, string path, IOutputReciever dbgOutput, int? timeout = null)
		{
			return Instance(l).Execute(path, dbgOutput, timeout);
		}

		public static string GetCodeExtension(OutputLanguage l)
		{
			return Instance(l).GetCodeExtension();
		}

		public static string GetBinaryExtension(OutputLanguage l)
		{
			return Instance(l).GetBinaryExtension();
		}

		public static string GetAcronym(OutputLanguage l)
		{
			return Instance(l).GetAcronym();
		}

		public static string ExecuteCode(OutputLanguage l, string code, IOutputReciever dbgOutput)
		{
			var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "." + GetBinaryExtension(l));

			try
			{
				Compile(l, code, path, dbgOutput ?? new DummyReciever());
				return Execute(l, path, dbgOutput);
			}
			finally
			{
				File.Delete(path);
			}
		}

		protected abstract void Compile(string code, string path, IOutputReciever dbgOutput);
		protected abstract string Execute(string path, IOutputReciever dbgOutput, int? timeout = null);
		protected abstract string GetCodeExtension();
		protected abstract string GetBinaryExtension();
		protected abstract string GetAcronym();
	}
}
