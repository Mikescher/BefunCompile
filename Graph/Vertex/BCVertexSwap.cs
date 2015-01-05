
using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexSwap : BCVertex
	{
		public BCVertexSwap(BCDirection d, Vec2i pos)
			: base(d, new Vec2i[] { pos })
		{

		}

		public BCVertexSwap(BCDirection d, Vec2i[] pos)
			: base(d, pos)
		{

		}

		public override string ToString()
		{
			return "SWAP";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexSwap(direction, positions);
		}

		public override IEnumerable<MemoryAccess> listConstantVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override IEnumerable<MemoryAccess> listDynamicVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, CalculateInterface ci)
		{
			stackbuilder.Swap();

			if (children.Count > 1)
				throw new ArgumentException("#");
			return children.FirstOrDefault();
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			return false;
		}

		public override bool isOnlyStackManipulation()
		{
			return true;
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			return "{long v0=sp();long v1=sp();sa(v0);sa(v1);}";
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return "{int64 v0=sp();int64 v1=sp();sa(v0);sa(v1);}";
		}
	}
}
