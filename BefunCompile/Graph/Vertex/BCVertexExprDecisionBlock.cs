using BefunCompile.CodeGeneration;
using BefunCompile.CodeGeneration.Generator;
using BefunCompile.Exceptions;
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Optimizations.StacksizePredictor;
using BefunCompile.Graph.Optimizations.Unstackify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexExprDecisionBlock : BCVertex, IDecisionVertex
	{
		public readonly BCVertexBlock Block;
		public readonly BCVertexExprDecision Decision;

		public BCVertex EdgeTrue
		{
			get { return Decision.EdgeTrue; }
			set { Decision.EdgeTrue = value; }
		}

		public BCVertex EdgeFalse
		{
			get { return Decision.EdgeFalse; }
			set { Decision.EdgeFalse = value; }
		}

		public BCVertexExprDecisionBlock(BCDirection d, BCVertexBlock block, BCVertexExprDecision dec)
			: base(d, block.Positions.Concat(dec.Positions).ToArray())
		{
			Block = block;
			Decision = dec;
		}

		public override string ToString()
		{
			return Block + Environment.NewLine + Decision;
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexExprDecisionBlock(Direction, (BCVertexBlock)Block.Duplicate(), (BCVertexExprDecision)Decision.Duplicate());
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return Block.ListConstantVariableAccess().Concat(Decision.ListConstantVariableAccess());
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return Block.ListDynamicVariableAccess().Concat(Decision.ListDynamicVariableAccess());
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, ICalculateInterface ci)
		{
			Block.Execute(outbuilder, stackbuilder, ci);

			return Decision.Execute(outbuilder, stackbuilder, ci);
		}

		public override int? GetStacksizePredictorDelta()
		{
			return StacksizePredictor.StacksizeAdd(Decision.GetStacksizePredictorDelta(), Block.GetStacksizePredictorDelta());
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			bool found1 = Block.SubsituteExpression(prerequisite, replacement);
			bool found2 = Decision.SubsituteExpression(prerequisite, replacement);

			return found1 || found2;
		}

		public override BCModArea GetSideEffects()
		{
			return Block.GetSideEffects() | Decision.GetSideEffects();
		}
		
		public override bool IsCodePathSplit()
		{
			return true;
		}

		public override bool IsBlock()
		{
			return true;
		}

		public override bool IsRandom()
		{
			return Block.IsRandom() || Decision.IsRandom();
		}

		public override IEnumerable<ExpressionVariable> GetVariables()
		{
			return Block.GetVariables().Concat(Decision.GetVariables());
		}

		public override IEnumerable<int> GetAllJumps(BCGraph g)
		{
			return Block.GetAllJumps(g).Concat(Decision.GetAllJumps(g));
		}

		public override string GenerateCode(CodeGenerator cg)
		{
			return cg.GenerateCodeBCVertexExprDecisionBlock(this);
		}

		public override UnstackifyState WalkUnstackify(UnstackifyStateHistory history, UnstackifyState state)
		{
			throw new CodeGenException("O:5 is not valid on node type " + this.GetType().Name);
		}

		public override BCVertex ReplaceUnstackify(List<UnstackifyValueAccess> access)
		{
			throw new CodeGenException("O:5 is not valid on node type " + this.GetType().Name);
		}

		public override bool IsIdentical(BCVertex other)
		{
			var arg = other as BCVertexDecisionBlock;

			if (arg == null) return false;

			return this.Block.IsIdentical(arg.Block) && this.Decision.IsIdentical(arg.Decision);
		}
	}
}
