
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
namespace BefunCompile.Graph.Expression
{
	public class ExpressionGet : BCExpression, MemoryAccess
	{
		public BCExpression PosX;
		public BCExpression PosY;

		private ExpressionGet(BCExpression xx, BCExpression yy)
		{
			this.PosX = xx;
			this.PosY = yy;
		}

		public static BCExpression Create(BCExpression xx, BCExpression yy)
		{
			return new ExpressionGet(xx, yy);
		}

		public override long Calculate(CalculateInterface ci)
		{
			return ci.GetGridValue(PosX.Calculate(ci), PosY.Calculate(ci));
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

		public BCExpression getX()
		{
			return PosX;
		}

		public BCExpression getY()
		{
			return PosY;
		}

		public Vec2l getConstantPos()
		{
			BCExpression xx = getX();
			BCExpression yy = getY();

			if (xx == null || yy == null || !(xx is ExpressionConstant) || !(yy is ExpressionConstant))
				return null;
			else
				return new Vec2l(getX().Calculate(null), getY().Calculate(null));
		}

		public override bool Subsitute(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			bool found = false;

			if (prerequisite(PosX))
			{
				PosX = replacement(PosX);
				found = true;
			}
			if (PosX.Subsitute(prerequisite, replacement))
			{
				found = true;
			}

			if (prerequisite(PosY))
			{
				PosY = replacement(PosY);
				found = true;
			}
			if (PosY.Subsitute(prerequisite, replacement))
			{
				found = true;
			}

			return found;
		}
	}
}
