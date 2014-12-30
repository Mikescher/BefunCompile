﻿using BefunCompile.Graph.Vertex;
using BefunCompile.Math;
using System;
using System.Collections.Generic;

namespace BefunCompile.Graph
{
	public abstract class BCVertex
	{
		public readonly BCDirection direction;
		public readonly Vec2i[] positions;

		public List<BCVertex> children = new List<BCVertex>();

		public BCVertex(BCDirection d, Vec2i pos)
		{
			this.direction = d;
			this.positions = new Vec2i[] { pos };
		}

		public virtual void AfterGen()
		{
			// NOP
		}

		public static BCVertex fromChar(BCDirection d, char c, Vec2i pos, out BCDirection[] outgoingEdges)
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
					return new BCVertexPush(d, pos, c - '0');

				case '$':
					outgoingEdges = new BCDirection[] { d };
					return new BCVertexPop(d, pos);

				case ':':
					outgoingEdges = new BCDirection[] { d };
					return new BCVertexDup(d, pos);

				case '\\':
					outgoingEdges = new BCDirection[] { d };
					return new BCVertexSwap(d, pos);

				case '@':
					outgoingEdges = new BCDirection[] { };
					return new BCVertexNOP(d, pos);

				case ' ':
				case '#':
					outgoingEdges = new BCDirection[] { d };
					return new BCVertexNOP(d, pos);

				case '+':
				case '-':
				case '*':
				case '/':
				case '`':
				case '%':
					outgoingEdges = new BCDirection[] { d };
					return new BCVertexBinaryMath(d, pos, c);

				case '>':
					outgoingEdges = new BCDirection[] { BCDirection.FROM_LEFT };
					return new BCVertexNOP(d, pos);
				case '<':
					outgoingEdges = new BCDirection[] { BCDirection.FROM_RIGHT };
					return new BCVertexNOP(d, pos);
				case '^':
					outgoingEdges = new BCDirection[] { BCDirection.FROM_BOTTOM };
					return new BCVertexNOP(d, pos);
				case 'v':
					outgoingEdges = new BCDirection[] { BCDirection.FROM_TOP };
					return new BCVertexNOP(d, pos);
				case '?':
					outgoingEdges = new BCDirection[] { BCDirection.FROM_LEFT, BCDirection.FROM_TOP, BCDirection.FROM_RIGHT, BCDirection.FROM_BOTTOM };
					return new BCVertexNOP(d, pos);

				case '.':
				case ',':
					outgoingEdges = new BCDirection[] { d };
					return new BCVertexOutput(d, pos, c);

				case '&':
				case '~':
					outgoingEdges = new BCDirection[] { d };
					return new BCVertexInput(d, pos, c);

				case '|':
					outgoingEdges = new BCDirection[] { BCDirection.FROM_TOP, BCDirection.FROM_BOTTOM };
					return new BCVertexDecision(d, pos);

				default:
					throw new ArgumentException("[::] Unknown char: " + c);
			}
		}
	}
}
