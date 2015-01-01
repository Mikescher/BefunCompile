
using System.Collections.Generic;
namespace BefunCompile.Graph.Expression
{
	public class ExpressionNot : BCExpression
	{
		public readonly BCExpression Value;

		private ExpressionNot(BCExpression v)
		{
			this.Value = v;
		}

		public static BCExpression Create(BCExpression v)
		{
			ExpressionNot r = new ExpressionNot(v);

			if (v is ExpressionConstant)
				return ExpressionConstant.Create(r.Calculate());
			else
				return r;
		}

		public override long Calculate()
		{
			return (Value.Calculate() != 0) ? (0) : (1);
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
	}
}
