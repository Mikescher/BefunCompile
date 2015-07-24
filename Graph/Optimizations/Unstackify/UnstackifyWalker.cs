using BefunCompile.Exceptions;
using BefunCompile.Graph.Expression;
using BefunCompile.Graph.Vertex;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BefunCompile.Graph.Optimizations.Unstackify
{
	public class UnstackifyWalker
	{
		private readonly BCGraph Graph;

		private int varIdentity = 0;

		private HashSet<BCVertex> ProtectedVertices = new HashSet<BCVertex>();

		public UnstackifyWalker(BCGraph graph)
		{
			this.Graph = graph;
		}

		public bool Run()
		{
			UnstackifyStateHistory history = new UnstackifyStateHistory();

			Walk(Graph.Root, history, new UnstackifyState());

			history.UpdatePoison();

			int repl = ReplaceSystemVariables(history);

			return repl > 0;
		}

		private void Walk(BCVertex vertex, UnstackifyStateHistory history, UnstackifyState state)
		{
			history.AddState(vertex, state);
			state.AddScope(vertex);

			UnstackifyState out_state;
			try
			{
				out_state = vertex.WalkUnstackify(history, state);
			}
			catch (UnstackifyWalkException)
			{
				PoisonState(state);
				foreach (var child in vertex.Children.Where(p => history.Contains(p)))
				{
					PoisonState(history.StateDict[child]);
				}

				return;
			}

			out_state.AddScope(vertex);

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

		private int ReplaceSystemVariables(UnstackifyStateHistory history)
		{
			ProtectedVertices.ToList().ForEach(p => history.PoisonVertex(p));

			history.RemovePoison();
			history.CreateVariables(Graph, ref varIdentity);
			
			foreach (var vertex in Graph.Vertices.ToList())
			{
				ReplaceVariablesInVertex(history, vertex);
			}

			return history.ValuesCount();
		}

		private void ReplaceVariablesInVertex(UnstackifyStateHistory history, BCVertex vertex)
		{
			var replacements = history.StackValues
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

				if (newVertex == vertex)
				{
					// do nothing
				}
				else if (newVertex is BCVertexNOP)
				{
					Graph.RemoveVertex(vertex);
				}
				else if (newVertex is BCVertexBlock)
				{
					(newVertex as BCVertexBlock).nodes.ToList().ForEach(p => ProtectedVertices.Add(p));

					Graph.ReplaceVertex(vertex, (newVertex as BCVertexBlock).nodes.ToList());
				}
				else
				{
					Graph.ReplaceVertex(vertex, newVertex);
				}
			}
		}
	}
}
