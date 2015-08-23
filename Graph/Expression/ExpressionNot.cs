
using BefunCompile.Graph.Optimizations.Unstackify;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BefunCompile.Graph.Expression
{
	public class ExpressionNot : BCExpression
	{
		public BCExpression Value;

		private ExpressionNot(BCExpression v)
		{
			this.Value = v;
		}

		public static BCExpression Create(BCExpression v)
		{
			ExpressionNot r = new ExpressionNot(v);

			if (v is ExpressionConstant)
				return ExpressionConstant.Create(r.Calculate(null));

			if (v is ExpressionBinMath && (v as ExpressionBinMath).Type == Vertex.BinaryMathType.GT)
				return ExpressionBinMath.Create((v as ExpressionBinMath).ValueA, (v as ExpressionBinMath).ValueB, Vertex.BinaryMathType.LET);

			if (v is ExpressionBinMath && (v as ExpressionBinMath).Type == Vertex.BinaryMathType.LT)
				return ExpressionBinMath.Create((v as ExpressionBinMath).ValueA, (v as ExpressionBinMath).ValueB, Vertex.BinaryMathType.GET);

			if (v is ExpressionBinMath && (v as ExpressionBinMath).Type == Vertex.BinaryMathType.GET)
				return ExpressionBinMath.Create((v as ExpressionBinMath).ValueA, (v as ExpressionBinMath).ValueB, Vertex.BinaryMathType.LT);

			if (v is ExpressionBinMath && (v as ExpressionBinMath).Type == Vertex.BinaryMathType.LET)
				return ExpressionBinMath.Create((v as ExpressionBinMath).ValueA, (v as ExpressionBinMath).ValueB, Vertex.BinaryMathType.GT);

			if (v is ExpressionNot)
				return ExpressionBCast.Create((v as ExpressionNot).Value);

			if (v is ExpressionBCast)
				return ExpressionNot.Create((v as ExpressionBCast).Value);

			return r;
		}

		public override long Calculate(ICalculateInterface ci)
		{
			return (Value.Calculate(ci) != 0) ? (0) : (1);
		}

		public override string GetRepresentation()
		{
			return "NOT(" + Value.GetRepresentation() + ")";
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
			return Enumerable.Empty<ExpressionVariable>();
		}

		private bool NeedsParen()
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

		public override string GenerateCodeCSharp(BCGraph g, bool forceLongReturn)
		{
			if (forceLongReturn)
				return string.Format("({0}!=0)?0L:1L", Paren(Value.GenerateCodeCSharp(g, false), NeedsParen()));
			else
				return string.Format("({0}!=0)?0:1", Paren(Value.GenerateCodeCSharp(g, false), NeedsParen()));
		}

		public override string GenerateCodeC(BCGraph g, bool forceLongReturn)
		{
			if (forceLongReturn)
				return string.Format("({0}!=0)?0LL:1LL", Paren(Value.GenerateCodeC(g, false), NeedsParen()));
			else
				return string.Format("({0}!=0)?0:1", Paren(Value.GenerateCodeC(g, false), NeedsParen()));
		}

		public override string GenerateCodePython(BCGraph g, bool forceLongReturn)
		{
			return string.Format("(0)if({0}!=0)else(1)", Paren(Value.GenerateCodePython(g, false), NeedsParen()));
		}

		public string GenerateDecisionCodeCSharp(BCGraph g, bool forceLongReturn)
		{
			return string.Format("{0}==0", Paren(Value.GenerateCodeCSharp(g, false), NeedsParen()));
		}

		public string GenerateDecisionCodeC(BCGraph g, bool forceLongReturn)
		{
			return string.Format("{0}==0", Paren(Value.GenerateCodeC(g, false), NeedsParen()));
		}

		public string GenerateDecisionCodePython(BCGraph g, bool forceLongReturn)
		{
			return string.Format("{0}==0", Paren(Value.GenerateCodePython(g, false), NeedsParen()));
		}

		public override bool IsNotGridAccess()
		{
			return Value.IsNotGridAccess();
		}

		public override bool IsNotStackAccess()
		{
			return Value.IsNotStackAccess();
		}

		public override bool IsNotVariableAccess()
		{
			return Value.IsNotVariableAccess();
		}

		public override BCExpression ReplaceUnstackify(UnstackifyValueAccess access)
		{
			return ExpressionNot.Create(Value.ReplaceUnstackify(access));
		}

		public override bool IsIdentical(BCExpression other)
		{
			var arg = other as ExpressionNot;

			if (arg == null) return false;

			return Value.IsIdentical(arg.Value);
		}
	}
}
