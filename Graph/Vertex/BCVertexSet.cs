using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexSet : BCVertex, MemoryAccess
	{
		public BCVertexSet(BCDirection d, Vec2i pos)
			: base(d, new Vec2i[] { pos })
		{

		}

		public BCVertexSet(BCDirection d, Vec2i[] pos)
			: base(d, pos)
		{

		}

		public override string ToString()
		{
			return "SET";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexSet(Direction, Positions);
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return new MemoryAccess[] { this };
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, CalculateInterface ci)
		{
			var yy = stackbuilder.Pop();
			var xx = stackbuilder.Pop();
			var vv = stackbuilder.Pop();

			ci.SetGridValue(xx, yy, vv);

			if (Children.Count > 1)
				throw new ArgumentException("#");
			return Children.FirstOrDefault();
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
			return false;
		}

		public override bool IsOnlyStackManipulation()
		{
			return false;
		}

		public override bool IsCodePathSplit()
		{
			return false;
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			return "{long v0=sp();long v1=sp();gw(v1,v0,sp());}";
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return "{int64 v0=sp();int64 v1=sp();gw(v1,v0,sp());}";
		}

		public override string GenerateCodePython(BCGraph g)
		{
			return "v0=sp()" + Environment.NewLine + "v1=sp()" + Environment.NewLine + "gw(v1,v0,sp())";
		}
	}
}
