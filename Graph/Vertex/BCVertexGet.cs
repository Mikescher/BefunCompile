using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexGet : BCVertex, MemoryAccess
	{
		public BCVertexGet(BCDirection d, Vec2i pos)
			: base(d, new Vec2i[] { pos })
		{

		}

		public BCVertexGet(BCDirection d, Vec2i[] pos)
			: base(d, pos)
		{

		}

		public override string ToString()
		{
			return "GET";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexGet(direction, positions);
		}

		public override IEnumerable<MemoryAccess> listConstantVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override IEnumerable<MemoryAccess> listDynamicVariableAccess()
		{
			return new MemoryAccess[] { this };
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder)
		{
			throw new System.NotImplementedException();
		}

		public BCExpression getX()
		{
			return null;
		}

		public BCExpression getY()
		{
			return null;
		}

		public Vec2l getConstantPos()
		{
			BCExpression xx = getX();
			BCExpression yy = getX();

			if (xx == null || yy == null || !(xx is ExpressionConstant) || !(yy is ExpressionConstant))
				return null;
			else
				return new Vec2l(getX().Calculate(), getY().Calculate());
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			return false;
		}
	}
}
