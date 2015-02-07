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

		public abstract bool IsOnlyStackManipulation();
		public abstract bool Subsitute(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement);

		public abstract IEnumerable<ExpressionVariable> GetVariables();

		public abstract string GenerateCodeCSharp(BCGraph g);
		public abstract string GenerateCodeC(BCGraph g);
		public abstract string GenerateCodePython(BCGraph g);

		public override string ToString()
		{
			return GetRepresentation();
		}
	}
}
