﻿
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

		public override long Calculate()
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
	}
}