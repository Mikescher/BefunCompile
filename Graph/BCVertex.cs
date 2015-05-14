using BefunCompile.Exceptions;
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Vertex;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph
{
	public abstract class BCVertex
	{
		public readonly BCDirection Direction;
		public Vec2i[] Positions;

		public readonly List<BCVertex> Children = new List<BCVertex>();

		public readonly List<BCVertex> Parents = new List<BCVertex>();

		protected BCVertex(BCDirection d, Vec2i[] pos)
		{
			Direction = d;
			Positions = pos;
		}

		public virtual void AfterGen()
		{
			// NOP
		}

		public void UpdateParents()
		{
			foreach (var child in Children)
			{
				child.Parents.Add(this);
			}
		}

		public bool TestParents()
		{
			return Children.All(child => child.Parents.Contains(this)) && Parents.All(parent => parent.Children.Contains(this));
		}

		public static BCVertex FromChar(BCDirection d, long c, Vec2i pos, out BCDirection[] outgoingEdges)
		{
			if (BCDirectionHelper.isSMDirection(d) && c != '"')
			{
				outgoingEdges = new[] { d };
				return new BCVertexPush(d, pos, ExpressionConstant.Create(c));
			}

			switch (c)
			{
				case '"':
					outgoingEdges = new[] { BCDirectionHelper.switchSMDirection(d) };
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
					outgoingEdges = new[] { d };
					return new BCVertexPush(d, pos, ExpressionConstant.Create(c - '0'));

				case '$':
					outgoingEdges = new[] { d };
					return new BCVertexPop(d, pos);

				case ':':
					outgoingEdges = new[] { d };
					return new BCVertexDup(d, pos);

				case '!':
					outgoingEdges = new[] { d };
					return new BCVertexNot(d, pos);

				case '\\':
					outgoingEdges = new[] { d };
					return new BCVertexSwap(d, pos);

				case '@':
					outgoingEdges = new BCDirection[] { };
					return new BCVertexNOP(d, pos);

				case ' ':
				case '\t':
				case '#':
					outgoingEdges = new[] { d };
					return new BCVertexNOP(d, pos);

				case '+':
				case '-':
				case '*':
				case '/':
				case '`':
				case '%':
					outgoingEdges = new[] { d };
					return new BCVertexBinaryMath(d, pos, c);

				case '>':
					outgoingEdges = new[] { BCDirection.FROM_LEFT };
					return new BCVertexNOP(d, pos);
				case '<':
					outgoingEdges = new[] { BCDirection.FROM_RIGHT };
					return new BCVertexNOP(d, pos);
				case '^':
					outgoingEdges = new[] { BCDirection.FROM_BOTTOM };
					return new BCVertexNOP(d, pos);
				case 'v':
					outgoingEdges = new[] { BCDirection.FROM_TOP };
					return new BCVertexNOP(d, pos);
				case '?':
					outgoingEdges = new[] { BCDirection.FROM_LEFT, BCDirection.FROM_TOP, BCDirection.FROM_RIGHT, BCDirection.FROM_BOTTOM };
					return new BCVertexRandom(d, pos);

				case '.':
				case ',':
					outgoingEdges = new[] { d };
					return new BCVertexOutput(d, pos, c);

				case '&':
				case '~':
					outgoingEdges = new[] { d };
					return new BCVertexInput(d, pos, c);

				case '|':
					outgoingEdges = new[] { BCDirection.FROM_TOP, BCDirection.FROM_BOTTOM };
					return new BCVertexDecision(d, pos);
				case '_':
					outgoingEdges = new[] { BCDirection.FROM_LEFT, BCDirection.FROM_RIGHT };
					return new BCVertexDecision(d, pos);

				case 'g':
					outgoingEdges = new[] { d };
					return new BCVertexGet(d, pos);
				case 'p':
					outgoingEdges = new[] { d };
					return new BCVertexSet(d, pos);

				default:
					throw new UnknownCharacterException(c);
			}
		}

		public abstract BCVertex Duplicate();
		public abstract BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, CalculateInterface ci);

		public abstract IEnumerable<MemoryAccess> ListConstantVariableAccess();
		public abstract IEnumerable<MemoryAccess> ListDynamicVariableAccess();

		public abstract bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement);

		public abstract bool IsOnlyStackManipulation();
		public abstract bool IsCodePathSplit();
		public abstract bool IsBlock();
		public abstract bool IsRandom();
		public abstract IEnumerable<ExpressionVariable> GetVariables();
		public abstract IEnumerable<int> GetAllJumps(BCGraph g);


		public abstract string GenerateCodeCSharp(BCGraph g);
		public abstract string GenerateCodeC(BCGraph g);
		public abstract string GenerateCodePython(BCGraph g);
	}
}
