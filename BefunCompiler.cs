using BefunCompile.Graph;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BefunCompile
{
	public class BefunCompiler
	{
		private readonly string source;
		private readonly long[,] sourceGrid;
		private int width;
		private int height;

		public int log_Cycles_Minimize { get; private set; }
		public int log_Cycles_Substitute { get; private set; }
		public int log_Cycles_Flatten { get; private set; }
		public int log_Cycles_Variablize { get; private set; }

		public BefunCompiler(string befsource)
		{
			this.source = befsource;
			this.sourceGrid = stringToCharArr(source);

			width = sourceGrid.GetLength(0);
			height = sourceGrid.GetLength(1);
		}

		private long[,] stringToCharArr(string str)
		{
			string[] lines = str.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
			int height = lines.Length;
			int width = lines.Max(p => p.Length);

			lines = lines.Select(p => p.PadRight(width, ' ')).ToArray();

			long[,] result = new long[width, height];

			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					result[x, y] = lines[y][x];
				}
			}

			return result;
		}

		public long GetGridValue(long x, long y)
		{
			if (x < 0 || y < 0 || x >= width || y >= height)
				return 0;
			else
				return sourceGrid[x, y];
		}

		public BCGraph generateUntouchedGraph() // O:0
		{
			List<BCVertex> all = new List<BCVertex>();
			var unfinished = new Stack<Tuple<BCVertex, Vec2i, BCDirection>>(); /*<parent, position, direction>*/

			int width = sourceGrid.GetLength(0);
			int height = sourceGrid.GetLength(1);

			BCGraph graph = new BCGraph();

			{
				BCDirection[] next;
				graph.root = BCVertex.fromChar(BCDirection.FROM_LEFT, sourceGrid[0, 0], new Vec2i(0, 0), out next);
				graph.vertices.Add(graph.root);
				foreach (var direction in next)
				{
					unfinished.Push(Tuple.Create(graph.root, new Vec2i(0, 0).Move(direction, width, height, sourceGrid[0, 0] == '#'), direction));
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

				BCVertex vertex = BCVertex.fromChar(currentDir, command, pos, out next);
				graph.vertices.Add(vertex);

				parent.children.Add(vertex);

				foreach (var direction in next)
				{
					Vec2i newpos = pos.Move(direction, width, height, command == '#');
					BCVertex search = graph.getVertex(newpos, direction);
					if (search == null)
						unfinished.Push(Tuple.Create(vertex, newpos, direction));
					else
						vertex.children.Add(search);
				}
			}

			graph.AfterGen();
			graph.UpdateParents();

			if (!graph.TestGraph())// TODO REM  ??
				throw new Exception("Internal Parent Exception :( ");

			return graph;
		}

		public BCGraph generateMinimizedGraph(int level = -1) // O:1
		{
			BCGraph graph = generateUntouchedGraph();

			for (int i = level; i != 0; i--)
			{
				bool op = graph.Minimize();

				if (!graph.TestGraph())// TODO REM  ??
					throw new Exception("Internal Parent Exception :( ");

				if (!op)
				{
					log_Cycles_Minimize = level - i;

					break;
				}
			}

			return graph;
		}

		public BCGraph generateSubstitutedGraph(int level = -1) // O:2
		{
			BCGraph graph = generateMinimizedGraph(-1);

			for (int i = level; i != 0; i--)
			{
				bool op = graph.Substitute();

				if (!graph.TestGraph())// TODO REM  ??
					throw new Exception("Internal Parent Exception :( ");

				if (!op)
				{
					log_Cycles_Substitute = level - i;

					break;
				}
			}

			return graph;
		}

		public BCGraph generateFlattenedGraph(int level = -1) // O:3
		{
			BCGraph graph = generateSubstitutedGraph(-1);

			for (int i = level; i != 0; i--)
			{
				bool op = graph.FlattenStack();

				if (!graph.TestGraph())// TODO REM  ??
					throw new Exception("Internal Parent Exception :( ");

				if (!op)
				{
					log_Cycles_Flatten = level - i;

					break;
				}
			}

			return graph;
		}

		public BCGraph generateVariablizedGraph(int level = -1) // O:4
		{
			BCGraph graph = generateFlattenedGraph(-1);

			var constGets = graph.listConstantVariableAccess().ToList();
			var dynamGets = graph.listDynamicVariableAccess().ToList();

			if (dynamGets.Count == 0)
				graph.SubstituteConstMemoryAccess(GetGridValue);

			return graph;
		}
	}
}
