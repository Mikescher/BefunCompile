using BefunCompile.Exceptions;
using BefunCompile.Graph;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BefunCompile
{
	public class BefunCompiler
	{
		public const string VERSION = "1.0.7";

		public class GenerationLevel
		{
			public int Level;
			public string Name;
			public Func<GenerationLevel, int, BCGraph> Function;

			public BCGraph Run(int runlevel = -1) { return Function(this, runlevel); }
			public override string ToString() { return string.Format("O:{0} {1}", Level, Name); }
		}

		public readonly GenerationLevel[] GENERATION_LEVELS;

		private readonly long[,] sourceGrid;
		private readonly int width;
		private readonly int height;

		private readonly bool ignoreSelfModification;
		private readonly bool implementSafeStackAccess;
		private readonly bool implementSafeGridAccess;
		private readonly bool useGZip;
		private readonly bool formatOutput;

		public int[] log_Cycles;

		public BefunCompiler(string befsource, bool fmtOut, bool ignoreSelfMod, bool safeStackAcc, bool safeGridAcc, bool usegzip)
		{
			sourceGrid = StringToCharArr(befsource);

			width = sourceGrid.GetLength(0);
			height = sourceGrid.GetLength(1);

			ignoreSelfModification = ignoreSelfMod;
			implementSafeStackAccess = safeStackAcc;
			implementSafeGridAccess = safeGridAcc;
			formatOutput = fmtOut;
			useGZip = usegzip;

			//##########################################################################

			GENERATION_LEVELS = new[]
			{
				new GenerationLevel(){Level = 0, Name = "Raw",        Function = GenerateUntouchedGraph},
				new GenerationLevel(){Level = 1, Name = "Minimize",   Function = GenerateMinimizedGraph},
				new GenerationLevel(){Level = 2, Name = "Substitute", Function = GenerateSubstitutedGraph},
				new GenerationLevel(){Level = 3, Name = "Flatten",    Function = GenerateFlattenedGraph},
				new GenerationLevel(){Level = 4, Name = "Variablize", Function = GenerateVariablizedGraph},
				new GenerationLevel(){Level = 5, Name = "Unstackify", Function = GenerateUnstackifiedGraph},
				new GenerationLevel(){Level = 6, Name = "Nopify",     Function = GenerateNopifiedGraph},
				new GenerationLevel(){Level = 7, Name = "Combine",    Function = GenerateBlockCombinedGraph},
				new GenerationLevel(){Level = 8, Name = "Reduce",     Function = GenerateBlockReducedGraph},
			};
			log_Cycles = new int[GENERATION_LEVELS.Length];
		}

		private long[,] StringToCharArr(string str)
		{
			string[] lines = str.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
			int arrHeight = lines.Length;
			int arrWidth = lines.Max(p => p.Length);

			lines = lines.Select(p => p.PadRight(arrWidth, ' ')).ToArray();

			long[,] result = new long[arrWidth, arrHeight];

			for (int x = 0; x < arrWidth; x++)
			{
				for (int y = 0; y < arrHeight; y++)
				{
					result[x, y] = lines[y][x];
				}
			}

			return result;
		}

		private long GetGridValue(long x, long y)
		{
			if (x < 0 || y < 0 || x >= width || y >= height)
				return 0;

			return sourceGrid[x, y];
		}

		public BCGraph GenerateGraph()
		{
			return GENERATION_LEVELS.Last().Run();
		}

		public string GenerateCode(OutputLanguage lang)
		{
			switch (lang)
			{
				case OutputLanguage.CSharp:
					return GenerateGraph().GenerateCodeCSharp(formatOutput, implementSafeStackAccess, implementSafeGridAccess, useGZip);
				case OutputLanguage.C:
					return GenerateGraph().GenerateCodeC(formatOutput, implementSafeStackAccess, implementSafeGridAccess, useGZip);
				case OutputLanguage.Python:
					return GenerateGraph().GenerateCodePython(formatOutput, implementSafeStackAccess, implementSafeGridAccess, useGZip);
				default:
					return null;
			}
		}

		public BCGraph GenerateUntouchedGraph(GenerationLevel lvl, int level = -1) // O:0 
		{
			var unfinished = new Stack<Tuple<BCVertex, Vec2i, BCDirection>>(); /*<parent, position, direction>*/

			BCGraph graph = new BCGraph(sourceGrid, width, height);

			{
				BCDirection[] next;
				graph.Root = BCVertex.FromChar(BCDirection.FROM_LEFT, sourceGrid[0, 0], new Vec2i(0, 0), out next);
				graph.Vertices.Add(graph.Root);
				foreach (var direction in next)
				{
					unfinished.Push(Tuple.Create(graph.Root, new Vec2i(0, 0).Move(direction, width, height, sourceGrid[0, 0] == '#'), direction));
				}
			}

			while (unfinished.Count > 0)
			{
				BCDirection[] next;

				var current = unfinished.Pop();
				BCVertex parent = current.Item1;
				Vec2i pos = current.Item2;
				BCDirection currentDir = current.Item3;
				long command = sourceGrid[pos.X, pos.Y];

				BCVertex vertex = BCVertex.FromChar(currentDir, command, pos, out next);
				graph.Vertices.Add(vertex);

				parent.Children.Add(vertex);

				foreach (var direction in next)
				{
					Vec2i newpos = pos.Move(direction, width, height, command == '#' && !BCDirectionHelper.isSMDirection(direction));
					BCVertex search = graph.GetVertex(newpos, direction);
					if (search == null)
						unfinished.Push(Tuple.Create(vertex, newpos, direction));
					else
						vertex.Children.Add(search);
				}
			}

			graph.AfterGen();
			graph.UpdateParents();

			if (!graph.TestGraph())
				throw new Exception("Internal Parent Exception :( ");

			log_Cycles[lvl.Level] = 1;

			return graph;
		}

		public BCGraph GenerateMinimizedGraph(GenerationLevel lvl, int level = -1) // O:1 
		{
			BCGraph graph = GENERATION_LEVELS[lvl.Level - 1].Run();

			for (int i = level; i != 0; i--)
			{
				bool op = graph.Minimize();

				if (!graph.TestGraph())
					throw new Exception("Internal Parent Exception :( ");

				if (!op)
				{
					log_Cycles[lvl.Level] = level - i;

					break;
				}
			}

			return graph;
		}

		public BCGraph GenerateSubstitutedGraph(GenerationLevel lvl, int level = -1) // O:2 
		{
			BCGraph graph = GENERATION_LEVELS[lvl.Level - 1].Run();

			for (int i = level; i != 0; i--)
			{
				bool op = graph.Substitute();

				if (!graph.TestGraph())
					throw new Exception("Internal Parent Exception :( ");

				if (!op)
				{
					log_Cycles[lvl.Level] = level - i;

					break;
				}
			}

			return graph;
		}

		public BCGraph GenerateFlattenedGraph(GenerationLevel lvl, int level = -1) // O:3 
		{
			BCGraph graph = GENERATION_LEVELS[lvl.Level - 1].Run();

			for (int i = level; i != 0; i--)
			{
				bool op = graph.FlattenStack();

				if (!graph.TestGraph())
					throw new Exception("Internal Parent Exception :( ");

				if (!op)
				{
					log_Cycles[lvl.Level] = level - i;

					break;
				}
			}

			return graph;
		}

		public BCGraph GenerateVariablizedGraph(GenerationLevel lvl, int level = -1) // O:4 
		{
			BCGraph graph = GENERATION_LEVELS[lvl.Level - 1].Run();

			var constGets = graph.ListConstantVariableAccess().ToList();
			var dynamGets = graph.ListDynamicVariableAccess().ToList();

			var accessPositions = constGets.Select(p => p.getConstantPos()).ToList();
			var codePositions = graph.GetAllCodePositions().ToList();

			if (!ignoreSelfModification && accessPositions.Any(p => codePositions.Contains(p)))
			{
				throw new SelfModificationException();
			}

			for (int i = level; i != 0; i--)
			{
				bool op = graph.VariablizeGraph(GetGridValue, dynamGets, constGets);

				if (!graph.TestGraph())
					throw new Exception("Internal Parent Exception :( ");

				if (!op)
				{
					log_Cycles[lvl.Level] = level - i;

					break;
				}
			}

			return graph;
		}

		public BCGraph GenerateUnstackifiedGraph(GenerationLevel lvl, int level = -1) // O:5 
		{
			BCGraph graph = GENERATION_LEVELS[lvl.Level - 1].Run();


			for (int i = level; i != 0; i--)
			{
				bool op = graph.Unstackify();

				if (!graph.TestGraph())
					throw new Exception("Internal Parent Exception :( ");

				if (!op)
				{
					log_Cycles[lvl.Level] = level - i;

					break;
				}
			}

			return graph;
		}

		public BCGraph GenerateNopifiedGraph(GenerationLevel lvl, int level = -1) // O:6
		{
			BCGraph graph = GENERATION_LEVELS[lvl.Level - 1].Run();


			for (int i = level; i != 0; i--)
			{
				bool op = graph.Nopify();

				if (!graph.TestGraph())
					throw new Exception("Internal Parent Exception :( ");

				if (!op)
				{
					log_Cycles[lvl.Level] = level - i;

					break;
				}
			}

			return graph;
		}

		public BCGraph GenerateBlockCombinedGraph(GenerationLevel lvl, int level = -1) // O:7
		{
			BCGraph graph = GENERATION_LEVELS[lvl.Level - 1].Run();

			for (int i = level; i != 0; i--)
			{
				bool op = graph.CombineBlocks();

				if (!graph.TestGraph())
					throw new Exception("Internal Parent Exception :( ");

				if (!op)
				{
					log_Cycles[lvl.Level] = level - i;

					break;
				}
			}

			return graph;
		}

		public BCGraph GenerateBlockReducedGraph(GenerationLevel lvl, int level = -1) // O:8
		{
			BCGraph graph = GENERATION_LEVELS[lvl.Level - 1].Run();

			for (int i = level; i != 0; i--)
			{
				bool op = graph.ReduceBlocks();

				if (!graph.TestGraph())
					throw new Exception("Internal Parent Exception :( ");

				if (!op)
				{
					log_Cycles[lvl.Level] = level - i;

					break;
				}
			}

			return graph;
		}
	}
}

//TODO [swap]+[BinMath] -> [BinMath, swapped=true]
//TODO BCVertexBinaryMath Replacements IN-BLOCK (+ possible others)
//TODO EXPR + 1 != 0    --->     EXPR  != 1    (auch für GT, LT ...)
//TODO [!] other optimizations (analyse code)
//TODO blocks (O:7 - Structures)
//      - repeat { BLOCK } until (??)
//      - while (??) { BLOCK }
//      - if (??) { BLOCK }
//      - if (??) { BLOCK } else (??) { BLOCK }
//TODO Output: JAVA, JS, VB.Net
//TODO x = x + 1 --> x += 1  (dont represent in graph, only in output method)
//TODO x = x + 1 --> x++	 (dont represent in graph, only in output method)

//TODO [[FATAL]] !!!  problem_075 compile error (wrong output)