using BefunCompile.Graph.Vertex;
using System;
using System.Collections.Generic;

namespace BefunCompile.Graph
{
	public abstract class BCVertex
	{
		public readonly BCDirection direction;

		public List<BCVertex> children = new List<BCVertex>();

		public BCVertex(BCDirection d)
		{
			this.direction = d;
		}

		public static BCVertex fromChar(BCDirection d, char c, out BCDirection[] outgoingEdges)
		{
			switch (c)
			{
				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
					outgoingEdges = new BCDirection[] { d };
					return new BCVertexPush(d, c - '0');
				case '$':
					outgoingEdges = new BCDirection[] { d };
					return new BCVertexPop(d);
				case '@':
					outgoingEdges = new BCDirection[] { };
					return new BCVertexNOP(d);
				case '+':
				case '-':
				case '*':
				case '/':
				case '`':
				case '%':
					outgoingEdges = new BCDirection[] { d };
					return new BCVertexBinaryMath(d, c);


				default:
					throw new ArgumentException("[::] Unknown char: " + c);
			}
		}
	}
}
