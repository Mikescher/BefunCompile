using BefunCompile.Exceptions;
using BefunCompile.Graph.Vertex;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BefunCompile.Graph.Optimizations.Unstackify
{
	public class UnstackifyWalker
	{
		private readonly BCGraph graph;

		private int varIdentity = 0;

		private readonly HashSet<BCVertex> protectedVertices = new HashSet<BCVertex>();

		public UnstackifyWalker(BCGraph graph)
		{
			this.graph = graph;
		}

		public bool Run()
		{
			UnstackifyStateHistory history = new UnstackifyStateHistory();

			Walk(graph.Root, history, new UnstackifyState());

			history.UpdatePoison();

			int repl = ReplaceSystemVariables(history);

			return repl > 0;
		}

		private void Walk(BCVertex vertex, UnstackifyStateHistory history, UnstackifyState state)
		{
			history.AddState(vertex, state);
			state.AddScope(vertex);

			UnstackifyState outState;
			try
			{
				outState = vertex.WalkUnstackify(history, state);
			}
			catch (UnstackifyWalkException)
			{
				PoisonState(state);
				foreach (var child in vertex.Children.Where(history.Contains))
				{
					PoisonState(history.StateDict[child]);
				}

				return;
			}

			outState.AddScope(vertex);

			foreach (var child in vertex.Children)
			{
				if (history.Contains(child))
				{
					var prevState = history.StateDict[child];

					if (!UnstackifyState.StatesEqual(prevState, outState))
					{
						PoisonState(prevState);
						PoisonState(outState);
					}
					else
					{
						// all good - do nothing
					}
				}
				else
				{
					Walk(child, history, outState);
				}
			}
		}

		private void PoisonState(UnstackifyState outState)
		{
			foreach (var value in outState.Stack)
			{
				value.Poison();
			}
		}

		private int ReplaceSystemVariables(UnstackifyStateHistory history)
		{
			protectedVertices.ToList().ForEach(history.PoisonVertex);

			history.RemovePoison();
			history.CreateVariables(graph, ref varIdentity);
			
			foreach (var vertex in graph.Vertices.ToList())
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
				bool duplicateErr = replacements.Any(p =>
					replacements
						.Where(q => q != p)
						.Where(q => p.Type == q.Type)
						.Where(q => p.Modifier == q.Modifier)
						.Any()
					);

				if (duplicateErr)
					throw new Exception();

				var newVertex = vertex.ReplaceUnstackify(replacements);

				if (newVertex == vertex)
				{
					// do nothing
				}
				else if (newVertex is BCVertexNOP)
				{
					graph.RemoveVertex(vertex);
				}
				else if (newVertex is BCVertexBlock)
				{
					(newVertex as BCVertexBlock).nodes.ToList().ForEach(p => protectedVertices.Add(p));

					graph.ReplaceVertex(vertex, (newVertex as BCVertexBlock).nodes.ToList());
				}
				else
				{
					graph.ReplaceVertex(vertex, newVertex);
				}
			}
		}
	}
}
