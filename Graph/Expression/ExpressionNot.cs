
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
			else
				return r;
		}

		public override long Calculate(CalculateInterface ci)
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

		public override string GenerateCodeCSharp(BCGraph g)
		{
			return string.Format("(({0})!=0)?0:1", Value.GenerateCodeCSharp(g));
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return string.Format("(({0})!=0)?0:1", Value.GenerateCodeCSharp(g));
		}

		public override string GenerateCodePython(BCGraph g)
		{
			return string.Format("(0)if(({0})!=0)else(1)", Value.GenerateCodePython(g));
		}

		public override bool IsOnlyStackManipulation()
		{
			return Value.IsOnlyStackManipulation();
		}
	}
}
