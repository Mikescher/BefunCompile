using System.Collections.Generic;

namespace BefunCompile.Graph.Optimizations.StacksizePredictor
{
	class StackSizeProgramState
	{
		public readonly BCVertex Source;

		public readonly BCVertex Vertex;
		public readonly int Stacksize;

		public readonly HashSet<BCVertex> Dependencies;

		public StackSizeProgramState(BCVertex v, int s, StackSizeProgramState dep)
		{
			Vertex = v;
			Stacksize = s;

			if (dep != null)
			{
				Source = dep.Vertex;
				Dependencies = dep.Dependencies;
				Dependencies.Add(dep.Vertex);
			}
			else
			{
				Source = null;
				Dependencies = new HashSet<BCVertex>();
			}
		}

		public void AddDependencies(IEnumerable<BCVertex> d)
		{
			foreach (var dep in d)
			{
				Dependencies.Add(dep);
			}
		}
	}
}
