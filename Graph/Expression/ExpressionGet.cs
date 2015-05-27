
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

		public override string GetRepresentation()
		{
			return "GET(" + X.GetRepresentation() + ", " + Y.GetRepresentation() + ")";
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			if (X is ExpressionConstant && Y is ExpressionConstant)
				return new MemoryAccess[] { this }.Concat(X.ListConstantVariableAccess()).Concat(Y.ListConstantVariableAccess());
			else
				return X.ListConstantVariableAccess().Concat(Y.ListConstantVariableAccess());
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			if (X is ExpressionConstant && Y is ExpressionConstant)
				return X.ListDynamicVariableAccess().Concat(Y.ListDynamicVariableAccess());
			else
				return new MemoryAccess[] { this }.Concat(X.ListDynamicVariableAccess()).Concat(Y.ListDynamicVariableAccess());
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

		public override IEnumerable<ExpressionVariable> GetVariables()
		{
			return Enumerable.Empty<ExpressionVariable>();
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			return string.Format("gr({0},{1})", X.GenerateCodeCSharp(g), Y.GenerateCodeCSharp(g));
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return string.Format("gr({0},{1})", X.GenerateCodeC(g), Y.GenerateCodeC(g));
		}

		public override string GenerateCodePython(BCGraph g)
		{
			return string.Format("gr({0},{1})", X.GenerateCodePython(g), Y.GenerateCodePython(g));
		}

		public override bool IsNotGridAccess()
		{
			return false;
		}

		public override bool IsNotStackAccess()
		{
			return X.IsNotStackAccess() && Y.IsNotStackAccess();
		}

		public override bool IsNotVariableAccess()
		{
			return X.IsNotVariableAccess() && Y.IsNotVariableAccess();
		}
	}
}
