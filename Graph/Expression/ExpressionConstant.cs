
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

		public override string getRepresentation()
		{
			return Value.ToString();
		}

		public override IEnumerable<MemoryAccess> listConstantVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override IEnumerable<MemoryAccess> listDynamicVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override bool Subsitute(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			return false;
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			return Value.ToString();
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return Value.ToString();
		}

		public override bool isOnlyStackManipulation()
		{
			return true;
		}
	}
}
