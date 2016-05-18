using BefunCompile.CodeGeneration;
using BefunCompile.CodeGeneration.Generator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CodeCompiler = BefunCompile.CodeGeneration.Compiler.CodeCompiler;

namespace BefunCompile.Consoleprogram
{
	public static class Program
	{
		private static CommandLineArguments cmda;

		private static string[] inputfiles;
		private static string outputfileformat;
		private static OutputLanguage[] languages;

		private static bool optionIgnoreSelfmod;
		private static bool optionOverride;
		private static CodeGeneratorOptions cgOptions;

		static void Main(string[] args)
		{
			cmda = new CommandLineArguments(args);

			if (cmda.Contains("help") || cmda.Contains("h"))
			{
				printCMDHelp();
			}
			else if (cmda.Count() == 0)
			{
				Console.WriteLine("No command line arguments supplied:");
				Console.WriteLine("");
				Console.WriteLine("");
				printCMDHelp();
				return;
			}

			loadArguments(cmda);

			run();
		}

		private static IEnumerable<OutputLanguage> ParseLanguages(CommandLineArguments cmda)
		{
			var allLanguages = ((OutputLanguage[]) Enum.GetValues(typeof (OutputLanguage))).ToList();

			foreach (var arg in new[] {"lang", "language", "languages"})
			{
				var data = cmda.GetStringDefault(arg, "").ToLower().Split(';');
				foreach (var datum in data)
				{
					if (datum == "all")
					{
						foreach (var lang in allLanguages) yield return lang;
					}
					else
					{
						var lang = OutputLanguageHelper.ParseFromAbbrev(datum);
						if (lang != null) yield return lang.Value;
					}
				}
			}
		}

		private static void loadArguments(CommandLineArguments cmda)
		{
			inputfiles = GetWildcardFiles(cmda.GetStringDefault("file", null));

			outputfileformat = cmda.GetStringDefault("out", null) ?? cmda.GetStringDefault("output", null);

			languages = ParseLanguages(cmda).Distinct().ToArray();

			optionIgnoreSelfmod = cmda.Contains("unsafe") || cmda.Contains("ignoreselfmod") || cmda.Contains("ism");

			optionOverride = cmda.Contains("override") || cmda.Contains("f");

			var optionFormat = cmda.Contains("format") || cmda.Contains("fmt");
			var optionSafeStackAccess = cmda.Contains("safestack") || cmda.Contains("ss");
			var optionSafeGridAccess = cmda.Contains("safegrid") || cmda.Contains("sg");
			var optionUseGZip = cmda.Contains("usegzip") || cmda.Contains("gzip");

			cgOptions = new CodeGeneratorOptions(optionFormat, optionSafeStackAccess, optionSafeGridAccess, optionUseGZip, false);
		}

		private static string[] GetWildcardFiles(string wc)
		{
			if (wc == null || wc == "")
				return new string[] { };

			Regex rex = new Regex("^" + Regex.Escape(wc).Replace(@"\*", @"[^\/]*").Replace(@"\?", @"[^\/]?") + "$", RegexOptions.Compiled);

			return Directory
				.GetFiles(Path.GetDirectoryName(wc))
				.Where(p => rex.IsMatch(p))
				.ToArray();
		}

		private static void printCMDHelp()
		{
			Console.WriteLine("CMD Arguments:");
			Console.WriteLine("==============");

			Console.WriteLine("");

			Console.WriteLine("file:");
			Console.WriteLine("    " + "The input filepath.");
			Console.WriteLine("    " + "(You can use * to target multiple Files)");
			Console.WriteLine("    " + "for example: /folder/file_*.b93");
			Console.WriteLine("    " + "         or: /folder/file_001.b93");

			Console.WriteLine("");

			Console.WriteLine("out | output:");
			Console.WriteLine("    " + "The output filepath");
			Console.WriteLine("    " + "use parameters to access the input filename");
			Console.WriteLine("    " + "parameters:");
			Console.WriteLine("    " + "    " + "{fn} : Input filename");
			Console.WriteLine("    " + "    " + "{f}  : Input filename without extension");
			Console.WriteLine("    " + "    " + "{p}  : Input file full path");
			Console.WriteLine("    " + "    " + "{fp} : Path to input file folder");
			Console.WriteLine("    " + "    " + "{i}  : Counter");
			Console.WriteLine("    " + "    " + "{l}  : The target language");
			Console.WriteLine("    " + "    " + "{le} : The target language file extension");
			Console.WriteLine("    " + "for example: /folder/compiled/{f}.cs");
			Console.WriteLine("    " + "         or: /folder/output.cs");
			Console.WriteLine("    " + "         or: /folder/compiled/{f}.{le}");
			Console.WriteLine("    " + "         or: /folder/compiled/{l}/{f}.{le}");

			Console.WriteLine("");

			Console.WriteLine("lang | language | languages:");
			Console.WriteLine("    " + "The target languages");
			Console.WriteLine("    " + "[ c | csharp | python | all ]");
			Console.WriteLine("    " + "seperate multiple target languages with a semicolon");
			Console.WriteLine("    " + "use 'all' to target all languages");

			Console.WriteLine("");

			Console.WriteLine("unsafe | ignoreselfmod | ism:");
			Console.WriteLine("    " + "Ignore code manipulations at runtime");

			Console.WriteLine("");

			Console.WriteLine("format | fmt:");
			Console.WriteLine("    " + "Enable output formatting");

			Console.WriteLine("");

			Console.WriteLine("safestack | ss");
			Console.WriteLine("    " + "Enable safe stack access");

			Console.WriteLine("");

			Console.WriteLine("override | f");
			Console.WriteLine("    " + "Override existing files");

			Console.WriteLine("");

			Console.WriteLine("safegrid | sg");
			Console.WriteLine("    " + "Enable safe grid access");

			Console.WriteLine("");

			Console.WriteLine("usegzip | gzip");
			Console.WriteLine("    " + "Enable GZip Grid compression (if possible)");

			Console.WriteLine("");

			Console.WriteLine("");
			Console.WriteLine("");
			Console.WriteLine("Press [Enter] to continue...");
			Console.ReadLine();

		}

		private static string GetLangExt(OutputLanguage l)
		{
			return CodeCompiler.GetCodeExtension(l);
		}

		private static void run()
		{
			if (inputfiles.Length == 0)
			{
				Console.WriteLine("Error: No files found.");
				return;
			}

			if (languages.Length == 0)
			{
				Console.WriteLine("Error: No languages specified.");
				return;
			}

			if (string.IsNullOrEmpty(outputfileformat))
			{
				Console.WriteLine("Error: No output file specified.");
				return;
			}

			int counter = 0;
			foreach (var input in inputfiles)
			{
				foreach (var lang in languages)
				{
					counter++;

					string outputfilename = outputfileformat
						.Replace("{fn}", Path.GetFileName(input))
						.Replace("{f}", Path.GetFileNameWithoutExtension(input))
						.Replace("{p}", input)
						.Replace("{fp}", Path.GetDirectoryName(input))
						.Replace("{i}", counter.ToString())
						.Replace("{l}", lang.ToString())
						.Replace("{le}", GetLangExt(lang));

					string source = File.ReadAllText(input);

					var comp = new BefunCompiler(source, optionIgnoreSelfmod, cgOptions);

					string code;
					try
					{
						code = comp.GenerateCode(lang);
					}
					catch (Exception e)
					{
						Console.Error.WriteLine("Fatal Failure on file " + Path.GetFileName(input) + ": " + e.GetType().Name);
						break;
					}

					if (File.Exists(outputfilename) && !optionOverride)
					{
						Console.Error.WriteLine("Error: File " + Path.GetFileName(outputfilename) + " already exists.");
						break;
					}

					Directory.CreateDirectory(Path.GetDirectoryName(outputfilename));
					File.WriteAllText(outputfilename, code);

					Console.Error.WriteLine(string.Format("[{0:000}]File {1} succesfully compiled to {2}", counter, Path.GetFileName(input), Path.GetFileName(outputfilename)));
				}
			}
		}
	}
}
