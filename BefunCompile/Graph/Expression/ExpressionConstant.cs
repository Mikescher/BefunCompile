using BefunCompile.CodeGeneration.Generator;
using BefunCompile.Graph.Optimizations.Unstackify;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BefunCompile.Graph.Expression
{
	public class ExpressionConstant : BCExpression
	{
		public readonly long Value;

		private ExpressionConstant(long v)
		{
			this.Value = v;
		}

		public static BCExpression Create(long v)
		{
			return new ExpressionConstant(v);
		}

		public override long Calculate(ICalculateInterface ci)
		{
			return Value;
		}

		public override string GetRepresentation()
		{
			return Value.ToString();
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
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
			return false;
		}

		public override string GenerateCode(CodeGenerator cg, bool forceLongReturn)
		{
			return cg.GenerateCodeExpressionConstant(this, forceLongReturn);
		}

		public override BCModArea GetSideEffects()
		{
			return BCModArea.None;
		}

		public override BCExpression ReplaceUnstackify(UnstackifyValueAccess access)
		{
			return this;
		}

		public bool IsSimpleASCIIChar()
		{
			return (Value >= ' ' && Value <= '~' && Value != '\"' && Value != '\\');
		}

		public char AsSimpleASCIIChar()
		{
			return (char)(Value % 128);
		}

		public override bool IsIdentical(BCExpression other)
		{
			var arg = other as ExpressionConstant;

			return Value == arg?.Value;
		}

		public override bool IsConstant(int value)
		{
			return this.Value == value;
		}
	}
}
