using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexFullVarGet : BCVertex, MemoryAccess
	{
		public ExpressionVariable Variable;

		public BCVertexFullVarGet(BCDirection d, Vec2i pos, ExpressionVariable var)
			: base(d, new Vec2i[] { pos })
		{
			this.Variable = var;
		}

		public BCVertexFullVarGet(BCDirection d, Vec2i[] pos, ExpressionVariable var)
			: base(d, pos)
		{
			this.Variable = var;
		}

		public override string ToString()
		{
			return "GET(" + Variable.getRepresentation() + ")";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexFullVarGet(direction, positions, Variable);
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, CalculateInterface ci)
		{
			stackbuilder.Push(ci.GetVariableValue(Variable));

			if (children.Count > 1)
				throw new ArgumentException("#");
			return children.FirstOrDefault();
		}

		public override IEnumerable<MemoryAccess> listConstantVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override IEnumerable<MemoryAccess> listDynamicVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public BCExpression ToExpression()
		{
			return Variable;
		}

		public BCExpression getX()
		{
			return null;
		}

		public BCExpression getY()
		{
			return null;
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

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			bool found = false;

			if (prerequisite(Variable))
			{
				Variable = (ExpressionVariable)replacement(Variable);
				found = true;
			}
			if (Variable.Subsitute(prerequisite, replacement))
			{
				found = true;
			}

			return found;
		}

		public override bool isOnlyStackManipulation()
		{
			return false;
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			return string.Format("sa({0});", Variable.Identifier);
		}
	}
}
