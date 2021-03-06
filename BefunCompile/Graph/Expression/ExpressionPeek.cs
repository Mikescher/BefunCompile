﻿using BefunCompile.CodeGeneration.Generator;
using BefunCompile.Graph.Optimizations.Unstackify;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BefunCompile.Graph.Expression
{
	public class ExpressionPeek : BCExpression
	{
		private ExpressionPeek()
		{
			//--
		}

		public static ExpressionPeek Create()
		{
			return new ExpressionPeek();
		}

		public override long Calculate(ICalculateInterface ci)
		{
			return ci.PeekStack();
		}

		public override string GetRepresentation()
		{
			return "<peek>";
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
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
			BCExpression yy = getY();

			if (xx == null || yy == null || !(xx is ExpressionConstant) || !(yy is ExpressionConstant))
				return null;
			else
				return new Vec2l(getX().Calculate(null), getY().Calculate(null));
		}

		public override bool Subsitute(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			return false;
		}

		public override IEnumerable<ExpressionVariable> GetVariables()
		{
			return Enumerable.Empty<ExpressionVariable>();
		}

		public override bool IsAlwaysLongReturn()
		{
			return true;
		}

		public override string GenerateCode(CodeGenerator cg, bool forceLongReturn)
		{
			return cg.GenerateCodeExpressionPeek(this, forceLongReturn);
		}

		public override BCModArea GetSideEffects()
		{
			return BCModArea.Stack_Read;
		}
		
		public override BCExpression ReplaceUnstackify(UnstackifyValueAccess access)
		{
			return access.Value.Replacement;
		}

		public override bool IsIdentical(BCExpression other)
		{
			var arg = other as ExpressionPeek;

			if (arg == null) return false;

			return true;
		}

		public override bool IsConstant(int value)
		{
			return false;
		}
	}
}
