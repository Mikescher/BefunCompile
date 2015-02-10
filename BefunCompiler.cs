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
		public const string VERSION = "1.0.4";

		private readonly long[,] sourceGrid;
		private readonly int width;
		private readonly int height;

		private readonly bool ignoreSelfModification;
		private readonly bool implementSafeStackAccess;
		private readonly bool implementSafeGridAccess;
		private readonly bool useGZip;
		private readonly bool formatOutput;

		public int log_Cycles_Minimize { get; private set; }
		public int log_Cycles_Substitute { get; private set; }
		public int log_Cycles_Flatten { get; private set; }
		public int log_Cycles_Variablize { get; private set; }
		public int log_Cycles_CombineBlocks { get; private set; }
		public int log_Cycles_ReduceBlocks { get; private set; }

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
			return GenerateBlockReducedGraph();
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

		public BCGraph GenerateUntouchedGraph() // O:0 
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

			return graph;
		}

		public BCGraph GenerateMinimizedGraph(int level = -1) // O:1 
		{
			BCGraph graph = GenerateUntouchedGraph();

			for (int i = level; i != 0; i--)
			{
				bool op = graph.Minimize();

				if (!graph.TestGraph())
					throw new Exception("Internal Parent Exception :( ");

				if (!op)
				{
					log_Cycles_Minimize = level - i;

					break;
				}
			}

			return graph;
		}

		public BCGraph GenerateSubstitutedGraph(int level = -1) // O:2 
		{
			BCGraph graph = GenerateMinimizedGraph();

			for (int i = level; i != 0; i--)
			{
				bool op = graph.Substitute();

				if (!graph.TestGraph())
					throw new Exception("Internal Parent Exception :( ");

				if (!op)
				{
					log_Cycles_Substitute = level - i;

					break;
				}
			}

			return graph;
		}

		public BCGraph GenerateFlattenedGraph(int level = -1) // O:3 
		{
			BCGraph graph = GenerateSubstitutedGraph();

			for (int i = level; i != 0; i--)
			{
				bool op = graph.FlattenStack();

				if (!graph.TestGraph())
					throw new Exception("Internal Parent Exception :( ");

				if (!op)
				{
					log_Cycles_Flatten = level - i;

					break;
				}
			}

			return graph;
		}

		public BCGraph GenerateVariablizedGraph() // O:4 
		{
			BCGraph graph = GenerateFlattenedGraph();

			var constGets = graph.ListConstantVariableAccess().ToList();
			var dynamGets = graph.ListDynamicVariableAccess().ToList();

			var accessPositions = constGets.Select(p => p.getConstantPos()).ToList();
			var codePositions = graph.GetAllCodePositions().ToList();

			if (!ignoreSelfModification && accessPositions.Any(p => codePositions.Contains(p)))
			{
				throw new SelfModificationException();
			}

			if (dynamGets.Count == 0)
				graph.SubstituteConstMemoryAccess(GetGridValue);

			return graph;
		}

		public BCGraph GenerateBlockCombinedGraph(int level = -1) // O:5 
		{
			BCGraph graph = GenerateVariablizedGraph();

			for (int i = level; i != 0; i--)
			{
				bool op = graph.CombineBlocks();

				if (!graph.TestGraph())
					throw new Exception("Internal Parent Exception :( ");

				if (!op)
				{
					log_Cycles_CombineBlocks = level - i;

					break;
				}
			}

			return graph;
		}

		public BCGraph GenerateBlockReducedGraph(int level = -1) // O:6 
		{
			BCGraph graph = GenerateBlockCombinedGraph();

			for (int i = level; i != 0; i--)
			{
				bool op = graph.ReduceBlocks();

				if (!graph.TestGraph())
					throw new Exception("Internal Parent Exception :( ");

				if (!op)
				{
					log_Cycles_ReduceBlocks = level - i;

					break;
				}
			}

			return graph;
		}

	}
}

//TODO TESTS (use faster euler probs)
//TODO [swap]+[BinMath] -> [BinMath, swapped=true]
//TODO Sort goto-blocks to allow fall-trough
//TODO better parenthesis
//TODO BCVertexBinaryMath Replacements IN-BLOCK (+ possible others)
//TODO find variable initializer -> direct use
//TODO evtl other optimizations (analyse code)