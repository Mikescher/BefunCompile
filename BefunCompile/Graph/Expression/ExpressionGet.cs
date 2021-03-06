﻿using BefunCompile.CodeGeneration.Generator;
using BefunCompile.Graph.Optimizations.Unstackify;
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

		public override long Calculate(ICalculateInterface ci)
		{
			return ci.GetGridValue(X.Calculate(ci), Y.Calculate(ci));
		}

		public override bool IsAlwaysLongReturn()
		{
			return true;
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
			return Enumerable.Concat(X.GetVariables(), Y.GetVariables());
		}

		public override string GenerateCode(CodeGenerator cg, bool forceLongReturn)
		{
			return cg.GenerateCodeExpressionGet(this, forceLongReturn);
		}

		public override BCModArea GetSideEffects()
		{
			return X.GetSideEffects() | Y.GetSideEffects() | BCModArea.Grid_Read;
		}

		public override BCExpression ReplaceUnstackify(UnstackifyValueAccess access)
		{
			return ExpressionGet.Create(X.ReplaceUnstackify(access), Y.ReplaceUnstackify(access));
		}

		public override bool IsIdentical(BCExpression other)
		{
			var arg = other as ExpressionGet;

			if (arg == null) return false;

			return X.IsIdentical(arg.X) && Y.IsIdentical(arg.Y);
		}

		public override bool IsConstant(int value)
		{
			return false;
		}
	}
}
