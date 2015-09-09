using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Optimizations.Unstackify;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BefunCompile.Exceptions;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexInputVarSet : BCVertex, MemoryAccess
	{
		public ExpressionVariable Variable;
		public readonly bool modeInteger; // true = int | false = char

		public BCVertexInputVarSet(BCDirection d, Vec2i pos, ExpressionVariable var, bool modeInt)
			: base(d, new [] { pos })
		{
			this.Variable = var;
			this.modeInteger = modeInt;
		}

		public BCVertexInputVarSet(BCDirection d, Vec2i[] pos, ExpressionVariable var, bool modeInt)
			: base(d, pos)
		{
			this.Variable = var;
			this.modeInteger = modeInt;
		}

		public override string ToString()
		{
			return string.Format("SET({0}) = IN({1})", Variable, modeInteger ? "INT" : "CHAR");
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexInputVarSet(Direction, Positions, Variable, modeInteger);
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return Enumerable.Empty<MemoryAccess>();
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, ICalculateInterface ci)
		{
			throw new GraphExecuteException();
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

		public override bool IsOutput()
		{
			return false;
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

		public override bool IsCodePathSplit()
		{
			return false;
		}

		public override bool IsBlock()
		{
			return false;
		}

		public override bool IsRandom()
		{
			return false;
		}

		public override IEnumerable<ExpressionVariable> GetVariables()
		{
			return Variable.GetVariables();
		}

		public override IEnumerable<int> GetAllJumps(BCGraph g)
		{
			return Enumerable.Empty<int>();
		}

		public override string GenerateCodeCSharp(BCGraph g)
		{
			if (modeInteger)
				return string.Format("{{long v0;while(long.TryParse(System.Console.ReadLine(),out v0));{0}=v0;}}", Variable.Identifier);
			else
				return string.Format("{0}=System.Console.ReadLine();", Variable.Identifier);
		}

		public override string GenerateCodeC(BCGraph g)
		{
			if (modeInteger)
				return string.Format("{{char v0[128];int64 v1;fgets(v0,sizeof(v0),stdin);sscanf(v0,\"%lld\",&v1);{0}=v1;}}", Variable.Identifier);
			else
				return string.Format("{0}=getchar();", Variable.Identifier);
		}

		public override string GenerateCodePython(BCGraph g)
		{
			if (modeInteger)
				return string.Format("{0}=int(input(\"\"))", Variable.Identifier);
			else
				return string.Format("{0}=ord(input(\"\")[0])", Variable.Identifier);
		}

		public override UnstackifyState WalkUnstackify(UnstackifyStateHistory history, UnstackifyState state)
		{
			return state.Clone();
		}

		public override BCVertex ReplaceUnstackify(List<UnstackifyValueAccess> access)
		{
			return this;
		}

		public override bool IsIdentical(BCVertex other)
		{
			var arg = other as BCVertexInputVarSet;

			if (arg == null) return false;

			return this.Variable.IsIdentical(arg.Variable) && this.modeInteger == arg.modeInteger;
		}
	}
}
