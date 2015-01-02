
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
namespace BefunCompile.Graph.Expression
{
	public class ExpressionGet : BCExpression, MemoryAccess
	{
		public BCExpression X;
		public BCExpression Y;

		private ExpressionGet(BCExpression xx, BCExpression yy)
		{
			this.X = xx;
			this.Y = yy;
		}

		public static BCExpression Create(BCExpression xx, BCExpression yy)
		{
			return new ExpressionGet(xx, yy);
		}

		public override long Calculate(CalculateInterface ci)
		{
			return ci.GetGridValue(X.Calculate(ci), Y.Calculate(ci));
		}

		public override string getRepresentation()
		{
			return "GET(" + X.getRepresentation() + ", " + Y.getRepresentation() + ")";
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

		public override bool Subsitute(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
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

			return found;
		}

		public override string GenerateCode(BCGraph g)
		{
			return string.Format("gr({0},{1})", X.GenerateCode(g), Y.GenerateCode(g));
		}

		public override bool isOnlyStackManipulation()
		{
			return false;
		}
	}
}
