using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexTotalSet : BCVertex, MemoryAccess
	{
		public BCExpression X;
		public BCExpression Y;
		public BCExpression Value;

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

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, CalculateInterface ci)
		{
			ci.SetGridValue(X.Calculate(ci), Y.Calculate(ci), Value.Calculate(ci));

			if (children.Count > 1)
				throw new ArgumentException("#");
			return children.FirstOrDefault();
		}

		public BCExpression getX()
		{
			return X;
		}

		public BCExpression getY()
		{
			return Y;
		}

		public Vec2l getConstantPos()
		{
			BCExpression xx = getX();
			BCExpression yy = getY();

			if (xx == null || yy == null || !(xx is ExpressionConstant) || !(yy is ExpressionConstant))
				return null;
			else
				return new Vec2l(getX().Calculate(null), getY().Calculate(null));
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			bool found = false;

			if (prerequisite(X))
			{
				X = replacement(X);
				found = true;
			}
			if (X.Subsitute(prerequisite, replacement))
			{
				found = true;
			}

			if (prerequisite(Y))
			{
				Y = replacement(Y);
				found = true;
			}
			if (Y.Subsitute(prerequisite, replacement))
			{
				found = true;
			}

			if (prerequisite(Value))
			{
				Value = replacement(Value);
				found = true;
			}
			if (Value.Subsitute(prerequisite, replacement))
			{
				found = true;
			}

			return found;
		}

		public override bool isOnlyStackManipulation()
		{
			return false;
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			return string.Format("gw({0},{1},{2});", X.GenerateCodeCSharp(g), Y.GenerateCodeCSharp(g), Value.GenerateCodeCSharp(g));
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return string.Format("gw({0},{1},{2});", X.GenerateCodeCSharp(g), Y.GenerateCodeCSharp(g), Value.GenerateCodeCSharp(g));
		}
	}
}
