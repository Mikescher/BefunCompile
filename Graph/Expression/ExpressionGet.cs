
using System;
using System.Collections.Generic;
using System.Linq;
namespace BefunCompile.Graph.Expression
{
	public class ExpressionGet : BCExpression, MemoryAccess
	{
		public readonly BCExpression PosX;
		public readonly BCExpression PosY;

		private ExpressionGet(BCExpression xx, BCExpression yy)
		{
			this.PosX = xx;
			this.PosY = yy;
		}

		public static BCExpression Create(BCExpression xx, BCExpression yy)
		{
			return new ExpressionGet(xx, yy);
		}

		public override long Calculate()
		{
			throw new NotImplementedException();
		}

		public override string getRepresentation()
		{
			return "GET(" + PosX.getRepresentation() + ", " + PosY.getRepresentation() + ")";
		}

		public override IEnumerable<MemoryAccess> listConstantVariableAccess()
		{
			if (PosX is ExpressionConstant && PosY is ExpressionConstant)
				return new MemoryAccess[] { this }.Concat(PosX.listConstantVariableAccess()).Concat(PosY.listConstantVariableAccess());
			else
				return PosX.listConstantVariableAccess().Concat(PosY.listConstantVariableAccess());
		}

		public override IEnumerable<MemoryAccess> listDynamicVariableAccess()
		{
			if (PosX is ExpressionConstant && PosY is ExpressionConstant)
				return PosX.listDynamicVariableAccess().Concat(PosY.listDynamicVariableAccess());
			else
				return new MemoryAccess[] { this }.Concat(PosX.listDynamicVariableAccess()).Concat(PosY.listDynamicVariableAccess());
		}
	}
}
