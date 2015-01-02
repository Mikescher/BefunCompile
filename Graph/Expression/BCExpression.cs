using System;
using System.Collections.Generic;

namespace BefunCompile.Graph.Expression
{
	public abstract class BCExpression
	{
		public abstract String getRepresentation();

		//public long Calculate()
		//{
		//	return Calculate(null);
		//}
		public abstract long Calculate(CalculateInterface ci);

		public abstract IEnumerable<MemoryAccess> listConstantVariableAccess();
		public abstract IEnumerable<MemoryAccess> listDynamicVariableAccess();

		public abstract bool Subsitute(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement);

		public override string ToString()
		{
			return getRepresentation();
		}
	}
}
