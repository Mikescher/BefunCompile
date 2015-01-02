﻿using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexFullSet : BCVertex, MemoryAccess
	{
		public BCExpression X;
		public BCExpression Y;

		public BCVertexFullSet(BCDirection d, Vec2i pos, BCExpression xx, BCExpression yy)
			: base(d, new Vec2i[] { pos })
		{
			this.X = xx;
			this.Y = yy;
		}

		public BCVertexFullSet(BCDirection d, Vec2i[] pos, BCExpression xx, BCExpression yy)
			: base(d, pos)
		{
			this.X = xx;
			this.Y = yy;
		}

		public override string ToString()
		{
			return string.Format("SET({0}, {1})", X, Y);
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexFullSet(direction, positions, X, Y);
		}

		public override IEnumerable<MemoryAccess> listConstantVariableAccess()
		{
			if (X is ExpressionConstant && Y is ExpressionConstant)
				return new MemoryAccess[] { this }.Concat(X.listConstantVariableAccess()).Concat(Y.listConstantVariableAccess());
			else
				return X.listConstantVariableAccess().Concat(Y.listConstantVariableAccess());
		}

		public override IEnumerable<MemoryAccess> listDynamicVariableAccess()
		{
			if (X is ExpressionConstant && Y is ExpressionConstant)
				return X.listDynamicVariableAccess().Concat(Y.listDynamicVariableAccess());
			else
				return new MemoryAccess[] { this }.Concat(X.listDynamicVariableAccess()).Concat(Y.listDynamicVariableAccess());
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder)
		{
			throw new System.NotImplementedException();
		}

		public BCExpression getX()
		{
			return X;
		}

		public BCExpression getY()
		{
			return Y;
		}

		public Vec2l getConstantPos()
		{
			BCExpression xx = getX();
			BCExpression yy = getX();

			if (xx == null || yy == null || !(xx is ExpressionConstant) || !(yy is ExpressionConstant))
				return null;
			else
				return new Vec2l(getX().Calculate(), getY().Calculate());
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			bool found = false;

			if (prerequisite(X))
			{
				X = replacement(X);
				found = true;
			}
			if (X.Subsitute(prerequisite, replacement))
			{
				found = true;
			}

			if (prerequisite(Y))
			{
				Y = replacement(Y);
				found = true;
			}
			if (Y.Subsitute(prerequisite, replacement))
			{
				found = true;
			}

			return found;
		}
	}
}
