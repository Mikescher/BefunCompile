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

		public override string ToString()
		{
			return getRepresentation();
		}

	}
}
