using BefunCompile.CodeGeneration;
using BefunCompile.CodeGeneration.Generator;
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Optimizations.Unstackify;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexExprDecision : BCVertex, IDecisionVertex
	{
		public BCVertex EdgeTrue { get; set; }
		public BCVertex EdgeFalse { get; set; }

		public BCExpression Value;

		public BCVertexExprDecision(BCDirection d, Vec2i[] pos, BCVertex childTrue, BCVertex childFalse, BCExpression val)
			: base(d, pos)
		{
			EdgeTrue = childTrue;
			EdgeFalse = childFalse;
			Value = val;
		}

		public override string ToString()
		{
			return "IF (" + Value + ")";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexExprDecision(Direction, Positions, EdgeTrue, EdgeFalse, Value);
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return Value.ListConstantVariableAccess();
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return Value.ListDynamicVariableAccess();
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, ICalculateInterface ci)
		{
			var v = Value.Calculate(ci) != 0;

			return v ? EdgeTrue : EdgeFalse;
		}

		public override int? GetStacksizePredictorDelta()
		{
			return 0;
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			bool found = false;

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

		public override BCModArea GetSideEffects()
		{
			return Value.GetSideEffects();
		}
		
		public override bool IsCodePathSplit()
		{
			return true;
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
			return Value.GetVariables();
		}

		public override IEnumerable<int> GetAllJumps(BCGraph g)
		{
			yield return g.Vertices.IndexOf(EdgeTrue);
			yield return g.Vertices.IndexOf(EdgeFalse);
		}

		public override string GenerateCode(CodeGenerator cg)
		{
			return cg.GenerateCodeBCVertexExprDecision(this);
		}

		public override UnstackifyState WalkUnstackify(UnstackifyStateHistory history, UnstackifyState state)
		{
			state = state.Clone();

			if (Value.IsStackAccess())
			{
				state.Peek().AddAccess(this, UnstackifyValueAccessType.READ);
			}

			return state;
		}

		public override BCVertex ReplaceUnstackify(List<UnstackifyValueAccess> access)
		{
			return new BCVertexExprDecision(Direction, Positions, EdgeTrue, EdgeFalse, Value.ReplaceUnstackify(access.Single()));
		}

		public override bool IsIdentical(BCVertex other)
		{
			var arg = other as BCVertexExprDecision;

			if (arg == null) return false;

			return Value.IsIdentical(arg.Value);
		}
	}
}
