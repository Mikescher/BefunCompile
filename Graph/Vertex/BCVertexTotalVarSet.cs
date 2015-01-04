using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexTotalVarSet : BCVertex, MemoryAccess
	{
		public ExpressionVariable Variable;
		public BCExpression Value;

		public BCVertexTotalVarSet(BCDirection d, Vec2i pos, ExpressionVariable var, BCExpression val)
			: base(d, new Vec2i[] { pos })
		{
			this.Variable = var;
			this.Value = val;
		}

		public BCVertexTotalVarSet(BCDirection d, Vec2i[] pos, ExpressionVariable var, BCExpression val)
			: base(d, pos)
		{
			this.Variable = var;
			this.Value = val;
		}

		public override string ToString()
		{
			return string.Format("SET({0}) = {1}", Variable, Value);
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexTotalVarSet(direction, positions, Variable, Value);
		}

		public override IEnumerable<MemoryAccess> listConstantVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override IEnumerable<MemoryAccess> listDynamicVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, CalculateInterface ci)
		{
			ci.SetVariableValue(Variable, Value.Calculate(ci));

			if (children.Count > 1)
				throw new ArgumentException("#");
			return children.FirstOrDefault();
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

			if (prerequisite(Value))
			{
				Value = replacement(Value);
				found = true;
			}
			if (Value.Subsitute(prerequisite, replacement))
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
			return string.Format("{0}={1};", Variable.Identifier, Value.GenerateCodeCSharp(g));
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return string.Format("{0}={1};", Variable.Identifier, Value.GenerateCodeCSharp(g));
		}
	}
}
