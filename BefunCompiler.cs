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
		private readonly char[,] sourceGrid;

		public BefunCompiler(string befsource)
		{
			this.source = befsource;
			this.sourceGrid = stringToCharArr(source);
		}

		private char[,] stringToCharArr(string str)
		{
			string[] lines = str.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
			int height = lines.Length;
			int width = lines.Max(p => p.Length);

			lines = lines.Select(p => p.PadRight(width, ' ')).ToArray();

			char[,] result = new char[width, height];

			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					result[x, y] = lines[y][x];
				}
			}

			return result;
		}

		public BCGraph generateGraph()
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
				char command = sourceGrid[pos.X, pos.Y];

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

			foreach (var vertex in graph.vertices)
			{
				vertex.AfterGen();
			}

			return graph;
		}
	}
}
