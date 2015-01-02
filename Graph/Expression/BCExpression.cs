using System;
using System.Collections.Generic;

namespace BefunCompile.Graph.Expression
{
	public abstract class BCExpression
	{
		public abstract String getRepresentation();
		public abstract long Calculate();

		public abstract IEnumerable<MemoryAccess> listConstantVariableAccess();
		public abstract IEnumerable<MemoryAccess> listDynamicVariableAccess();

		public abstract bool Subsitute(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement);

		public override string ToString()
		{
			return getRepresentation();
		}
	}
}
