using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Vertex;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Text;

namespace BefunCompile.Graph
{
	public abstract class BCVertex
	{
		public readonly BCDirection direction;
		public Vec2i[] positions;

		public List<BCVertex> children = new List<BCVertex>();

		public List<BCVertex> parents = new List<BCVertex>();

		public BCVertex(BCDirection d, Vec2i[] pos)
		{
			this.direction = d;
			this.positions = pos;
		}

		public virtual void AfterGen()
		{
			// NOP
		}

		public void UpdateParents()
		{
			foreach (var child in children)
			{
				child.parents.Add(this);
			}
		}

		public bool TestParents()
		{
			foreach (var child in children)
			{
				if (!child.parents.Contains(this))
					return false;
			}

			foreach (var parent in parents)
			{
				if (!parent.children.Contains(this))
					return false;
			}

			return true;
		}

		public static BCVertex fromChar(BCDirection d, long c, Vec2i pos, out BCDirection[] outgoingEdges)
		{
			if (BCDirectionHelper.isSMDirection(d) && c != '"')
			{
				outgoingEdges = new BCDirection[] { d };
				return new BCVertexPush(d, pos, ExpressionConstant.Create(c));
			}

			switch (c)
			{
				case '"':
					outgoingEdges = new BCDirection[] { BCDirectionHelper.switchSMDirection(d) };
					return new BCVertexNOP(d, pos);

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
					return new BCVertexPush(d, pos, ExpressionConstant.Create(c - '0'));

				case '$':
					outgoingEdges = new BCDirection[] { d };
					return new BCVertexPop(d, pos);

				case ':':
					outgoingEdges = new BCDirection[] { d };
					return new BCVertexDup(d, pos);

				case '!':
					outgoingEdges = new BCDirection[] { d };
					return new BCVertexNot(d, pos);

				case '\\':
					outgoingEdges = new BCDirection[] { d };
					return new BCVertexSwap(d, pos);

				case '@':
					outgoingEdges = new BCDirection[] { };
					return new BCVertexNOP(d, pos);

				case ' ':
				case '\t':
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
					return new BCVertexRandom(d, pos);

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
				case '_':
					outgoingEdges = new BCDirection[] { BCDirection.FROM_LEFT, BCDirection.FROM_RIGHT };
					return new BCVertexDecision(d, pos);

				case 'g':
					outgoingEdges = new BCDirection[] { d };
					return new BCVertexGet(d, pos);
				case 'p':
					outgoingEdges = new BCDirection[] { d };
					return new BCVertexSet(d, pos);

				default:
					throw new ArgumentException("[::] Unknown char: " + c);
			}
		}

		public abstract BCVertex Duplicate();
		public abstract BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, CalculateInterface ci);

		public abstract IEnumerable<MemoryAccess> listConstantVariableAccess();
		public abstract IEnumerable<MemoryAccess> listDynamicVariableAccess();

		public abstract bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement);

		public abstract bool isOnlyStackManipulation();
		public abstract string GenerateCodeCSharp(BCGraph g);
		public abstract string GenerateCodeC(BCGraph g);
	}
}
