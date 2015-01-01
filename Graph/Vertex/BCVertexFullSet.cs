using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexFullSet : BCVertex, MemoryAccess
	{
		public readonly BCExpression X;
		public readonly BCExpression Y;

		public BCVertexFullSet(BCDirection d, Vec2i pos, BCExpression xx, BCExpression yy)
			: base(d, new Vec2i[] { pos })
		{
			this.X = xx;
			this.Y = yy;
		}

		public BCVertexFullSet(BCDirection d, Vec2i[] pos, BCExpression xx, BCExpression yy)
			: base(d, pos)
		{
			this.X = xx;
			this.Y = yy;
		}

		public override string ToString()
		{
			return string.Format("SET({0}, {1})", X, Y);
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexFullSet(direction, positions, X, Y);
		}

		public override IEnumerable<MemoryAccess> listConstantVariableAccess()
		{
			if (X is ExpressionConstant && Y is ExpressionConstant)
				return new MemoryAccess[] { this }.Concat(X.listConstantVariableAccess()).Concat(Y.listConstantVariableAccess());
			else
				return X.listConstantVariableAccess().Concat(Y.listConstantVariableAccess());
		}

		public override IEnumerable<MemoryAccess> listDynamicVariableAccess()
		{
			if (X is ExpressionConstant && Y is ExpressionConstant)
				return X.listDynamicVariableAccess().Concat(Y.listDynamicVariableAccess());
			else
				return new MemoryAccess[] { this }.Concat(X.listDynamicVariableAccess()).Concat(Y.listDynamicVariableAccess());
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder)
		{
			throw new System.NotImplementedException();
		}
	}
}
