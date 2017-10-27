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

		public List<string> LastRunInfo = new List<string>();

		public UnstackifyWalker(BCGraph graph)
		{
			this.graph = graph;
		}

		public bool Run()
		{
			LastRunInfo.Clear();

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
			history.CreateVariables(graph, ref varIdentity, ref LastRunInfo);

			LastRunInfo.Add("");

			foreach (var sv in history.StackValues)
			{
				LastRunInfo.Add($"[StackValue]");
				LastRunInfo.Add($"IsPoisoned={sv.IsPoisoned}");
				LastRunInfo.Add($"Replacement={sv.Replacement?.Identifier}");
				LastRunInfo.Add($"AccessCounter=");
				LastRunInfo.Add($"[");
				foreach (var a in sv.AccessCounter) LastRunInfo.Add($"   {("{" + a.Type + "}"),-11} | {("{" + a.Modifier + "}"),-12} [" + a.Vertex.ToOneLineString() + "]");
				LastRunInfo.Add($"]");
				LastRunInfo.Add($"Scope=");
				LastRunInfo.Add($"[");
				foreach (var s in sv.Scope) LastRunInfo.Add($"   [" + s.ToOneLineString() + "]");
				LastRunInfo.Add($"]");
				LastRunInfo.Add($"");
			}

			LastRunInfo.Add("");

			foreach (var vertex in graph.Vertices.ToList())
			{
				ReplaceVariablesInVertex(history, vertex, ref LastRunInfo);
			}

			return history.ValuesCount();
		}

		private void ReplaceVariablesInVertex(UnstackifyStateHistory history, BCVertex vertex, ref List<string> info)
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
					info.Add("[NoReplace]:    " + vertex.ToOneLineString());

					// do nothing
				}
				else if (newVertex is BCVertexNOP)
				{
					info.Add("[ReplaceToNop]: " + vertex.ToOneLineString());

					graph.RemoveVertex(vertex);
				}
				else if (newVertex is BCVertexBlock)
				{
					info.Add("[ReplaceBlock]: [" + vertex.ToOneLineString() + "] -->");
					foreach (var v in (newVertex as BCVertexBlock).nodes.ToList()) info.Add("  |->" + v.ToOneLineString());

					(newVertex as BCVertexBlock).nodes.ToList().ForEach(p => protectedVertices.Add(p));

					graph.ReplaceVertex(vertex, (newVertex as BCVertexBlock).nodes.ToList());
				}
				else
				{
					info.Add("[ReplaceVertex]: [" + vertex.ToOneLineString() + "] --> [" + newVertex.ToOneLineString() + "]");

					graph.ReplaceVertex(vertex, newVertex);
				}
			}
		}
	}
}
