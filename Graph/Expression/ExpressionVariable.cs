
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
namespace BefunCompile.Graph.Expression
{
	public class ExpressionVariable : BCExpression, MemoryAccess
	{
		public readonly string Identifier;
		public readonly long initial;

		public readonly Vec2l position;

		private ExpressionVariable(string ident, long init, Vec2l pos)
		{
			this.Identifier = ident;
			this.initial = init;
			this.position = pos;
		}

		public static ExpressionVariable Create(string ident, long init, Vec2l pos)
		{
			return new ExpressionVariable(ident, init, pos);
		}

		public override long Calculate(CalculateInterface ci)
		{
			return ci.GetVariableValue(this);
		}

		public override string getRepresentation()
		{
			return Identifier;
		}

		public override IEnumerable<MemoryAccess> listConstantVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override IEnumerable<MemoryAccess> listDynamicVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
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

		public override bool Subsitute(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			return false;
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			return Identifier;
		}

		public override string GenerateCodeC(BCGraph g)
		{
			return Identifier;
		}

		public override bool isOnlyStackManipulation()
		{
			return true;
		}
	}
}
