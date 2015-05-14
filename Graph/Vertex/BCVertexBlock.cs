using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexBlock : BCVertex
	{
		public readonly BCVertex[] nodes;

		public BCVertexBlock(BCDirection d, Vec2i pos, BCVertex inner)
			: base(d, new Vec2i[] { pos })
		{
			nodes = new BCVertex[] { inner };
		}

		public BCVertexBlock(BCDirection d, Vec2i[] pos, BCVertex inner)
			: base(d, pos)
		{
			nodes = new BCVertex[] { inner };
		}

		public BCVertexBlock(BCDirection d, Vec2i[] pos, BCVertex[] nodearr)
			: base(d, pos)
		{
			nodes = nodearr.ToArray();
		}

		public BCVertexBlock(BCDirection d, Vec2i[] pos, BCVertexBlock blockA, BCVertexBlock blockB)
			: base(d, pos)
		{
			nodes = blockA.nodes.Concat(blockB.nodes).Where(p => !(p is BCVertexNOP)).ToArray();
		}

		public override string ToString()
		{
			return string.Join(Environment.NewLine, nodes.Select(p => p.ToString()));
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexBlock(Direction, Positions, nodes);
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return nodes.SelectMany(p => p.ListConstantVariableAccess());
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return nodes.SelectMany(p => p.ListDynamicVariableAccess());
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, CalculateInterface ci)
		{
			for (int i = 0; i < nodes.Length; i++)
			{
				nodes[i].Execute(outbuilder, stackbuilder, ci);
			}

			if (nodes.Last().Children.Count > 1)
				throw new ArgumentException("#");
			return nodes.Last().Children.FirstOrDefault();
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			bool found = false;

			for (int i = 0; i < nodes.Length; i++)
			{
				if (nodes[i].SubsituteExpression(prerequisite, replacement))
					found = true;
			}

			return found;
		}

		public override bool IsOnlyStackManipulation()
		{
			return false;
		}

		public override bool IsCodePathSplit()
		{
			return false;
		}

		public override bool IsBlock()
		{
			return true;
		}

		public override bool IsRandom()
		{
			return nodes.Any(p => p.IsRandom());
		}

		public override IEnumerable<ExpressionVariable> GetVariables()
		{
			return nodes.SelectMany(p => p.GetVariables());
		}

		public override IEnumerable<int> GetAllJumps(BCGraph g)
		{
			return nodes.SelectMany(p => p.GetAllJumps(g));
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			return string.Join("", nodes.Select(p => p.GenerateCodeCSharp(g) + Environment.NewLine));
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return string.Join("", nodes.Select(p => p.GenerateCodeC(g) + Environment.NewLine));
		}

		public override string GenerateCodePython(BCGraph g)
		{
			return string.Join("", nodes.Select(p => p.GenerateCodePython(g) + Environment.NewLine));
		}
	}
}
