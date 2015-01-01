using BefunCompile.Graph.Vertex;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BefunCompile.Graph.Expression
{
	public class ExpressionBinMath : BCExpression
	{
		public readonly BCExpression ValueA;
		public readonly BCExpression ValueB;

		public readonly BinaryMathType Type;

		private ExpressionBinMath(BCExpression a, BCExpression b, BinaryMathType t)
		{
			this.ValueA = a;
			this.ValueB = b;
			this.Type = t;
		}

		public static BCExpression Create(BCExpression a, BCExpression b, BinaryMathType t)
		{
			ExpressionBinMath r = new ExpressionBinMath(a, b, t);

			if (a is ExpressionConstant && b is ExpressionConstant)
				return ExpressionConstant.Create(r.Calculate());
			else
				return r;
		}

		public override long Calculate()
		{
			long cA = ValueA.Calculate();
			long cB = ValueB.Calculate();

			switch (Type)
			{
				case BinaryMathType.ADD:
					return cA + cB;
				case BinaryMathType.SUB:
					return cA - cB;
				case BinaryMathType.MUL:
					return cA * cB;
				case BinaryMathType.DIV:
					return (cB == 0) ? 0 : (cA / cB);
				case BinaryMathType.GT:
					return (cA > cB) ? 1 : 0;
				case BinaryMathType.MOD:
					return (cB == 0) ? 0 : (cA % cB);
				default:
					throw new ArgumentException();
			}
		}

		public override string getRepresentation()
		{
			return "(" + ValueA.getRepresentation() + " + " + ValueB.getRepresentation() + ")";
		}

		public override IEnumerable<MemoryAccess> listConstantVariableAccess()
		{
			return ValueA.listConstantVariableAccess().Concat(ValueB.listConstantVariableAccess());
		}

		public override IEnumerable<MemoryAccess> listDynamicVariableAccess()
		{
			return ValueA.listDynamicVariableAccess().Concat(ValueB.listDynamicVariableAccess());
		}
	}
}
