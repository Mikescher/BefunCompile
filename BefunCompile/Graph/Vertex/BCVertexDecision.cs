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
	public class BCVertexDecision : BCVertex, IDecisionVertex
	{
		public BCVertex EdgeTrue { get; set; }
		public BCVertex EdgeFalse { get; set; }

		public BCVertexDecision(BCDirection d, Vec2i pos)
			: base(d, new[] { pos })
		{
			EdgeTrue = null;
			EdgeFalse = null;
		}

		private BCVertexDecision(BCDirection d, Vec2i[] pos)
			: base(d, pos)
		{
			EdgeTrue = null;
			EdgeFalse = null;
		}

		public override void AfterGen()
		{
			base.AfterGen();

			if (Children.Count != 2)
				throw new Exception("Decision needs 2 children");

			EdgeTrue = Children.First(p => p.Positions[0].X < Positions[0].X || p.Positions[0].Y < Positions[0].Y);
			EdgeFalse = Children.First(p => p.Positions[0].X > Positions[0].X || p.Positions[0].Y > Positions[0].Y);
		}

		public override string ToString()
		{
			return "IF ?";
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexDecision(Direction, Positions);
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
			var v = stackbuilder.PopBool();

			return v ? EdgeTrue : EdgeFalse;
		}

		public override int? GetStacksizePredictorDelta()
		{
			return -1;
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			return false;
		}

		public override BCModArea GetSideEffects()
		{
			return BCModArea.Stack_Read | BCModArea.Stack_Write;
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
			return Enumerable.Empty<ExpressionVariable>();
		}

		public override IEnumerable<int> GetAllJumps(BCGraph g)
		{
			yield return g.Vertices.IndexOf(EdgeTrue);
			yield return g.Vertices.IndexOf(EdgeFalse);
		}

		public override string GenerateCode(CodeGenerator cg)
		{
			return cg.GenerateCodeBCVertexDecision(this);
		}

		public override UnstackifyState WalkUnstackify(UnstackifyStateHistory history, UnstackifyState state)
		{
			state = state.Clone();

			state.Pop().AddAccess(this, UnstackifyValueAccessType.READ);

			return state;
		}

		public override BCVertex ReplaceUnstackify(List<UnstackifyValueAccess> access)
		{
			return new BCVertexExprDecision(Direction, Positions, EdgeTrue, EdgeFalse, access.Single().Value.Replacement);
		}

		public override bool IsIdentical(BCVertex other)
		{
			var arg = other as BCVertexDecision;

			if (arg == null) return false;

			return this.EdgeTrue == arg.EdgeTrue && this.EdgeFalse == arg.EdgeFalse;
		}
	}
}
