using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexTotalSet : BCVertex, MemoryAccess
	{
		public readonly BCExpression X;
		public readonly BCExpression Y;
		public readonly BCExpression Value;

		public BCVertexTotalSet(BCDirection d, Vec2i pos, BCExpression xx, BCExpression yy, BCExpression val)
			: base(d, new Vec2i[] { pos })
		{
			this.X = xx;
			this.Y = yy;
			this.Value = val;
		}

		public BCVertexTotalSet(BCDirection d, Vec2i[] pos, BCExpression xx, BCExpression yy, BCExpression val)
			: base(d, pos)
		{
			this.X = xx;
			this.Y = yy;
			this.Value = val;
		}

		public override string ToString()
		{
			return string.Format("SET({0}, {1}) = {2}", X, Y, Value);
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexTotalSet(direction, positions, X, Y, Value);
		}

		public override IEnumerable<MemoryAccess> listConstantVariableAccess()
		{

			if (X is ExpressionConstant && Y is ExpressionConstant)
				return new MemoryAccess[] { this }
					.Concat(X.listConstantVariableAccess())
					.Concat(Y.listConstantVariableAccess())
					.Concat(Value.listConstantVariableAccess());
			else
				return X.listConstantVariableAccess()
					.Concat(Y.listConstantVariableAccess())
					.Concat(Value.listConstantVariableAccess());
		}

		public override IEnumerable<MemoryAccess> listDynamicVariableAccess()
		{

			if (X is ExpressionConstant && Y is ExpressionConstant)
				return X.listDynamicVariableAccess()
					.Concat(Y.listDynamicVariableAccess())
					.Concat(Value.listDynamicVariableAccess());
			else
				return new MemoryAccess[] { this }
					.Concat(X.listDynamicVariableAccess())
					.Concat(Y.listDynamicVariableAccess())
					.Concat(Value.listDynamicVariableAccess());
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder)
		{
			throw new System.NotImplementedException();
		}
	}
}
