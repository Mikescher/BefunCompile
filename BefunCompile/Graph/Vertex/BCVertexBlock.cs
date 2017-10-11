using BefunCompile.CodeGeneration.Generator;
using BefunCompile.Exceptions;
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Optimizations.StacksizePredictor;
using BefunCompile.Graph.Optimizations.Unstackify;
using BefunCompile.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Graph.Vertex
{
	public class BCVertexBlock : BCVertex
	{
		public readonly BCVertex[] nodes;

		public BCVertexBlock(BCDirection d, Vec2i pos, BCVertex inner)
			: base(d, new [] { pos })
		{
			nodes = new [] { inner };
		}

		public BCVertexBlock(BCDirection d, Vec2i[] pos, BCVertex inner)
			: base(d, pos)
		{
			nodes = new [] { inner };
		}

		public BCVertexBlock(BCDirection d, Vec2i[] pos, params BCVertex[] nodearr)
			: base(d, pos)
		{
			nodes = nodearr.ToArray();
		}

		public BCVertexBlock(BCDirection d, Vec2i[] pos, BCVertexBlock blockA, BCVertexBlock blockB)
			: base(d, pos)
		{
			nodes = blockA.nodes.Concat(blockB.nodes).Where(p => !(p is BCVertexNOP)).ToArray();
		}

		public override string ToString()
		{
			return string.Join(Environment.NewLine, nodes.Select(p => p.ToString()));
		}

		public override BCVertex Duplicate()
		{
			return new BCVertexBlock(Direction, Positions, nodes);
		}

		public override IEnumerable<MemoryAccess> ListConstantVariableAccess()
		{
			return nodes.SelectMany(p => p.ListConstantVariableAccess());
		}

		public override IEnumerable<MemoryAccess> ListDynamicVariableAccess()
		{
			return nodes.SelectMany(p => p.ListDynamicVariableAccess());
		}

		public override BCVertex Execute(StringBuilder outbuilder, GraphRunnerStack stackbuilder, ICalculateInterface ci)
		{
			foreach (BCVertex t in nodes)
			{
				t.Execute(outbuilder, stackbuilder, ci);
			}

			if (nodes.Last().Children.Count > 1)
				throw new ArgumentException("#");
			return nodes.Last().Children.FirstOrDefault();
		}

		public override int? GetStacksizePredictorDelta()
		{
			return nodes.Aggregate<BCVertex, int?>(0, (c, t) => StacksizePredictor.StacksizeAdd(c, t.GetStacksizePredictorDelta()));
		}

		public override bool SubsituteExpression(Func<BCExpression, bool> prerequisite, Func<BCExpression, BCExpression> replacement)
		{
			bool found = false;

			foreach (BCVertex t in nodes)
			{
				if (t.SubsituteExpression(prerequisite, replacement))
					found = true;
			}

			return found;
		}

		public BCVertexBlock GetWithRemovedNode(BCVertex node)
		{
			return new BCVertexBlock(Direction, Positions, nodes.Where(p => p != node).ToArray());
		}

		public override BCModArea GetSideEffects()
		{
			return nodes.Select(n => n.GetSideEffects()).Aggregate(BCModArea.None, (a,b) => a | b);
		}
		
		public override bool IsCodePathSplit()
		{
			return false;
		}

		public override bool IsBlock()
		{
			return true;
		}

		public override bool IsRandom()
		{
			return nodes.Any(p => p.IsRandom());
		}

		public override IEnumerable<ExpressionVariable> GetVariables()
		{
			return nodes.SelectMany(p => p.GetVariables());
		}

		public override IEnumerable<int> GetAllJumps(BCGraph g)
		{
			return nodes.SelectMany(p => p.GetAllJumps(g));
		}

		public override string GenerateCode(CodeGenerator cg)
		{
			return cg.GenerateCodeBCVertexBlock(this);
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
			var arg = other as BCVertexBlock;

			if (arg == null) return false;

			if (arg.nodes.Length != this.nodes.Length) return false;

			return !nodes.Where((t, i) => !this.nodes[i].IsIdentical(arg.nodes[i])).Any();
		}
	}
}
