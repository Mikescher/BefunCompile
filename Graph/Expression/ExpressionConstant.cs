
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

		public override long Calculate(CalculateInterface ci)
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

		public override string GenerateCodeCSharp(BCGraph g)
		{
			return Value.ToString() + "L";
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return Value.ToString() + "LL";
		}

		public override string GenerateCodePython(BCGraph g)
		{
			return Value.ToString();
		}

		public override bool IsNotGridAccess()
		{
			return true;
		}

		public override bool IsNotStackAccess()
		{
			return true;
		}

		public override bool IsNotVariableAccess()
		{
			return true;
		}

		public override BCExpression ReplaceUnstackify(UnstackifyValueAccess access)
		{
			return this;
		}
	}
}
