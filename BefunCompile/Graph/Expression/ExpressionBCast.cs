using BefunCompile.CodeGeneration.Generator;
using BefunCompile.Graph.Optimizations.Unstackify;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BefunCompile.Graph.Expression
{
	public class ExpressionBCast : BCExpression
	{
		public BCExpression Value;

		private ExpressionBCast(BCExpression v)
		{
			this.Value = v;
		}

		public ExpressionBCast() : base()
		{
		}

		public static BCExpression Create(BCExpression v)
		{
			ExpressionBCast r = new ExpressionBCast(v);

			if (v is ExpressionConstant)
				return ExpressionConstant.Create(r.Calculate(null));

			if (v is ExpressionNot)
				return ExpressionNot.Create((v as ExpressionNot).Value);

			if (v is ExpressionBCast)
				return ExpressionBCast.Create((v as ExpressionBCast).Value);

			if (v is ExpressionBinMath && (v as ExpressionBinMath).Type == Vertex.BinaryMathType.GT)
				return v;

			if (v is ExpressionBinMath && (v as ExpressionBinMath).Type == Vertex.BinaryMathType.LT)
				return v;

			if (v is ExpressionBinMath && (v as ExpressionBinMath).Type == Vertex.BinaryMathType.GET)
				return v;

			if (v is ExpressionBinMath && (v as ExpressionBinMath).Type == Vertex.BinaryMathType.LET)
				return v;


			return r;
		}

		public override long Calculate(ICalculateInterface ci)
		{
			return (Value.Calculate(ci) != 0) ? (1) : (0);
		}

		public override string GetRepresentation()
		{
			return "B_CAST(" + Value.GetRepresentation() + ")";
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return Value.ListConstantVariableAccess();
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return Value.ListDynamicVariableAccess();
		}

		public override bool Subsitute(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			bool found = false;

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

		public override IEnumerable<ExpressionVariable> GetVariables()
		{
			return Value.GetVariables();
		}

		public bool NeedsParen()
		{
			if (Value is ExpressionConstant)
				return false;

			if (Value is ExpressionVariable)
				return false;

			var math = Value as ExpressionBinMath;
			if (math != null)
			{
				if (math.Type == Vertex.BinaryMathType.ADD)
					return false;
				if (math.Type == Vertex.BinaryMathType.SUB)
					return false;
				if (math.Type == Vertex.BinaryMathType.MUL)
					return false;
				if (math.Type == Vertex.BinaryMathType.MOD)
					return false;
				if (math.Type == Vertex.BinaryMathType.DIV)
					return false;
			}

			return true;
		}

		public override bool IsAlwaysLongReturn()
		{
			return false;
		}

		public override string GenerateCode(CodeGenerator cg, bool forceLongReturn)
		{
			return cg.GenerateCodeExpressionBCast(this, forceLongReturn);
		}

		public override BCModArea GetSideEffects()
		{
			return Value.GetSideEffects();
		}

		public override BCExpression ReplaceUnstackify(UnstackifyValueAccess access)
		{
			return Create(Value.ReplaceUnstackify(access));
		}

		public override bool IsIdentical(BCExpression other)
		{
			var arg = other as ExpressionBCast;

			if (arg == null) return false;

			return Value.IsIdentical(arg.Value);
		}

		public override bool IsConstant(int value)
		{
			return false;
		}
	}
}
