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
			nodes = blockA.nodes.Concat(blockB.nodes).ToArray();
		}

		public override string ToString()
		{
			return string.Join(Environment.NewLine, nodes.Select(p => p.ToString()));
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexBlock(direction, positions, nodes);
		}

		public override IEnumerable<MemoryAccess> listConstantVariableAccess()
		{
			return nodes.SelectMany(p => p.listConstantVariableAccess());
		}

		public override IEnumerable<MemoryAccess> listDynamicVariableAccess()
		{
			return nodes.SelectMany(p => p.listDynamicVariableAccess());
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, CalculateInterface ci)
		{
			for (int i = 0; i < nodes.Length; i++)
			{
				nodes[i].Execute(outbuilder, stackbuilder, ci);
			}

			if (nodes.Last().children.Count > 1)
				throw new ArgumentException("#");
			return nodes.Last().children.FirstOrDefault();
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

		public override bool isOnlyStackManipulation()
		{
			return false;
		}

		public override string GenerateCode(BCGraph g)
		{
			return string.Join("", nodes.Select(p => p.GenerateCode(g) + Environment.NewLine));
		}
	}
}
