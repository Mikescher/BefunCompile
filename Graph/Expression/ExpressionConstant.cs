
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

		public override bool IsAlwaysLongReturn()
		{
			return false;
		}

		public override string GenerateCodeCSharp(BCGraph g, bool forceLongReturn)
		{
			if (Value >= Int32.MaxValue)
				forceLongReturn = true;

			return Value.ToString() + (forceLongReturn ? "L" : "");
		}

		public override string GenerateCodeC(BCGraph g, bool forceLongReturn)
		{
			if (Value >= Int32.MaxValue)
				forceLongReturn = true;

			return Value.ToString() + (forceLongReturn ? "LL" : "");
		}

		public override string GenerateCodePython(BCGraph g, bool forceLongReturn)
		{
			if (Value >= Int32.MaxValue)
				forceLongReturn = true;

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

		public bool IsSimpleASCIIChar()
		{
			return (Value >= ' ' && Value <= '~' && Value != '\"' && Value != '\\');
		}

		public char AsSimpleASCIIChar()
		{
			return (char)(Value % 128);
		}
	}
}
