using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexDecision : BCVertex
	{
		public BCVertex edgeTrue = null;
		public BCVertex edgeFalse = null;

		public BCVertexDecision(BCDirection d, Vec2i pos)
			: base(d, new Vec2i[] { pos })
		{

		}

		public BCVertexDecision(BCDirection d, Vec2i[] pos)
			: base(d, pos)
		{

		}

		public override void AfterGen()
		{
			base.AfterGen();

			if (children.Count != 2)
				throw new Exception("Decision needs 2 children");

			edgeTrue = children.First(p => p.direction == BCDirection.FROM_RIGHT || p.direction == BCDirection.FROM_BOTTOM);
			edgeFalse = children.First(p => p.direction == BCDirection.FROM_LEFT || p.direction == BCDirection.FROM_TOP);
		}

		public override string ToString()
		{
			return "IF ?";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexDecision(direction, positions);
		}

		public override IEnumerable<MemoryAccess> listConstantVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override IEnumerable<MemoryAccess> listDynamicVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder)
		{
			var v = stackbuilder.PopBool();

			return v ? edgeTrue : edgeFalse;
		}
	}
}
