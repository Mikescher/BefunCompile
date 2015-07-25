using BefunCompile.Graph.Optimizations.Unstackify;
using System;
using System.Collections.Generic;

namespace BefunCompile.Graph.Expression
{
	public abstract class BCExpression
	{
		public abstract String GetRepresentation();

		public abstract long Calculate(CalculateInterface ci);

		public abstract IEnumerable<MemoryAccess> ListConstantVariableAccess();
		public abstract IEnumerable<MemoryAccess> ListDynamicVariableAccess();

		public abstract bool IsNotGridAccess();
		public abstract bool IsNotStackAccess();
		public abstract bool IsNotVariableAccess();

		public abstract bool Subsitute(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement);

		public abstract IEnumerable<ExpressionVariable> GetVariables();

		public abstract bool IsAlwaysLongReturn();

		public abstract string GenerateCodeCSharp(BCGraph g, bool forceLongReturn);
		public abstract string GenerateCodeC(BCGraph g, bool forceLongReturn);
		public abstract string GenerateCodePython(BCGraph g, bool forceLongReturn);

		public abstract BCExpression ReplaceUnstackify(UnstackifyValueAccess access);

		public override string ToString()
		{
			return GetRepresentation();
		}

		protected string Paren(string input, bool doParenthesis = true)
		{
			return doParenthesis ? ('(' + input + ')') : input;
		}

		public abstract bool IsIdentical(BCExpression other);
	}
}
