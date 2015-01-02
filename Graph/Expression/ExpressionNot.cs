
using System;
using System.Collections.Generic;
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

		public override string getRepresentation()
		{
			return "NOT(" + Value.getRepresentation() + ")";
		}

		public override IEnumerable<MemoryAccess> listConstantVariableAccess()
		{
			return Value.listConstantVariableAccess();
		}

		public override IEnumerable<MemoryAccess> listDynamicVariableAccess()
		{
			return Value.listDynamicVariableAccess();
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
	}
}
