using BefunCompile.Graph.Expression;
using System;
using System.Linq;

namespace BefunCompile.Graph.Optimizations.Unstackify
{
	public class UnstackifyWalker
	{
		private readonly BCGraph Graph;

		public UnstackifyWalker(BCGraph graph)
		{
			this.Graph = graph;
		}

		public void Run()
		{
			UnstackifyStateHistory history = new UnstackifyStateHistory();

			Walk(Graph.Root, history, new UnstackifyState());

			ReplaceSystemVariables(history);
		}

		private void Walk(BCVertex vertex, UnstackifyStateHistory history, UnstackifyState state)
		{
			history.AddState(vertex, state);

			var out_state = vertex.WalkUnstackify(history, state);

			foreach (var child in vertex.Children)
			{
				if (history.Contains(child))
				{
					var prev_state = history.StateDict[child];

					if (!UnstackifyState.StatesEqual(prev_state, out_state))
					{
						PoisonState(prev_state);
						PoisonState(out_state);
					}
					else
					{
						// all good - do nothing
					}
				}
				else
				{
					Walk(child, history, out_state);
				}
			}
		}

		private void PoisonState(UnstackifyState out_state)
		{
			foreach (var value in out_state.Stack)
			{
				value.Poison();
			}
		}

		private void ReplaceSystemVariables(UnstackifyStateHistory history)
		{
			var Variables = history.StackValues.Where(p => !p.IsPoisoned).ToList();

			int idx = 0;
			foreach (var variable in Variables)
			{
				var systemvar = ExpressionVariable.CreateSystemVariable(idx++);
				Graph.Variables.Add(systemvar);

				variable.Replacement = systemvar;
			}

			foreach (var vertex in Graph.Vertices.ToList())
			{
				ReplaceVariablesInVertex(Variables, vertex);
			}
		}

		private void ReplaceVariablesInVertex(System.Collections.Generic.List<UnstackifyValue> Variables, BCVertex vertex)
		{
			var replacements = Variables
				.SelectMany(p => p.AccessCounter)
				.Where(p => p.Vertex == vertex)
				.ToList();

			if (replacements.Count > 0)
			{
				bool duplicate_err = replacements.Any(p => 
					replacements
						.Where(q => q != p)
						.Where(q => p.Type == q.Type)
						.Where(q => p.Modifier == q.Modifier)
						.Any()
					);

				if (duplicate_err)
					throw new Exception();

				var newVertex = vertex.ReplaceUnstackify(replacements);

				Graph.ReplaceVertex(vertex, newVertex);
			}
		}
	}
}
