using BefunCompile.Graph;
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

		public void generateGraph()
		{
			List<BCVertex> all = new List<BCVertex>();
			List<BCVertex> unfinished = new List<BCVertex>();
		}
	}
}
