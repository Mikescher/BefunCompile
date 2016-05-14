using System.Collections.Generic;
using System.Linq;

namespace BefunCompile.Graph.Optimizations.StacksizePredictor
{
	class StacksizePredictor
	{
		private readonly BCGraph graph;

		private readonly Stack<StackSizeProgramState> work = new Stack<StackSizeProgramState>();
		private readonly Dictionary<BCVertex, List<StackSizeProgramState>> vertices;

		public StacksizePredictor(BCGraph graph)
		{
			this.graph = graph;
			this.vertices = new Dictionary<BCVertex, List<StackSizeProgramState>>();
		}

		public int? Predict()
		{
			work.Push(new StackSizeProgramState(graph.Root, 0, null));
			
			while (work.Any())
			{
				var result = Walk(work.Pop());

				if (result == StackSizePredictorIntermediateResult.UnboundedGrowth) return null;
			}

			return vertices.SelectMany(p => p.Value).Max(p => p.Stacksize);
		}

		private StackSizePredictorIntermediateResult Walk(StackSizeProgramState pstate)
		{
			if (! vertices.ContainsKey(pstate.Vertex)) vertices.Add(pstate.Vertex, new List<StackSizeProgramState>());

			var matchState = vertices[pstate.Vertex].FirstOrDefault(s => s.Stacksize == pstate.Stacksize);
			if (matchState != null)
			{
				matchState.AddDependencies(pstate.Dependencies);
				return StackSizePredictorIntermediateResult.FinishedStableLoop;
			}
			
			var errorState = vertices[pstate.Vertex].Any(s => s.Stacksize < pstate.Stacksize && s.Dependencies.Contains(pstate.Source));
			if (errorState)
			{
				// ERROR
				return StackSizePredictorIntermediateResult.UnboundedGrowth;
			}

			vertices[pstate.Vertex].Add(pstate);

			int? delta = pstate.Vertex.GetStacksizePredictorDelta();

			if (delta == null)
				return StackSizePredictorIntermediateResult.UnboundedGrowth;
			
			if (!pstate.Vertex.Children.Any())
			{
				return StackSizePredictorIntermediateResult.FinishedLeaf;
			}

			int outputSize = pstate.Stacksize + delta.Value;

			if (outputSize < 0) outputSize = 0;

			foreach (var child in pstate.Vertex.Children)
			{
				work.Push(new StackSizeProgramState(child, outputSize, pstate));
			}

			return StackSizePredictorIntermediateResult.ProcessedVertex;
		}

		public static int? StacksizeAdd(int? a, int? b)
		{
			return (a == null || b == null) ? null : (a + b);
		}

		public static int? StacksizeAdd(int? a, params int?[] additional)
		{
			if (a == null) return null;

			foreach (var x in additional)
			{
				if (x == null) return null;
				a += x.Value;
			}

			return a;
		}
	}
}
