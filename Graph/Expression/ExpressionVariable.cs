
using BefunCompile.CodeGeneration;
using BefunCompile.CodeGeneration.Generator;
using BefunCompile.Graph.Optimizations.Unstackify;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BefunCompile.Graph.Expression
{
	public class ExpressionVariable : BCExpression, MemoryAccess
	{
		public readonly string Identifier;
		public long initial;

		public readonly Vec2l position;
		public readonly List<List<BCVertex>> Scope; // [NULL] by user Variables

		public readonly bool isUserDefinied;

		private ExpressionVariable(string ident, long init, Vec2l pos, bool usrVar, List<List<BCVertex>> scope)
		{
			this.Identifier = ident;
			this.initial = init;
			this.position = pos;
			this.isUserDefinied = usrVar;
			this.Scope = scope;
		}

		private static ExpressionVariable Create(string ident, long init, Vec2l pos, bool usrVar, List<List<BCVertex>> scope)
		{
			return new ExpressionVariable(ident, init, pos, usrVar, scope);
		}

		public static ExpressionVariable CreateUserVariable(int ident_idx, long init, Vec2l pos)
		{
			return ExpressionVariable.Create("x" + ident_idx, init, pos, true, null);
		}

		public static ExpressionVariable CreateSystemVariable(int ident_idx, List<List<BCVertex>> scope)
		{
			return ExpressionVariable.Create("t" + ident_idx, 0, null, false, scope);
		}

		public override long Calculate(ICalculateInterface ci)
		{
			return ci.GetVariableValue(this);
		}

		public override string GetRepresentation()
		{
			return Identifier;
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
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

		public override IEnumerable<ExpressionVariable> GetVariables()
		{
			return new[] { this };
		}

		public override bool IsAlwaysLongReturn()
		{
			return true;
		}

		public override string GenerateCode(OutputLanguage l, BCGraph g, bool forceLongReturn)
		{
			return CodeGenerator.GenerateCodeExpressionVariable(l, this, g, forceLongReturn);
		}

		public override bool IsNotGridAccess()
		{
			return true;
		}

		public override bool IsNotStackAccess()
		{
			return true;
		}

		public override bool IsNotVariableAccess()
		{
			return false;
		}

		public override BCExpression ReplaceUnstackify(UnstackifyValueAccess access)
		{
			return this;
		}

		public override bool IsIdentical(BCExpression other)
		{
			var arg = other as ExpressionVariable;

			if (arg == null) return false;

			return Identifier == arg.Identifier;
		}

		public ExpressionConstant GetInitialConstant()
		{
			return (ExpressionConstant) ExpressionConstant.Create(initial);
		}
	}
}
