using System.Text;

namespace BefunCompile.Graph
{
	public class GraphRunner
	{
		public string Output { get { return outbuilder.ToString(); } }
		public long[] Stack { get { return stackbuilder.stack.ToArray(); } }
		public long Steps { get { return stepbuilder; } }

		private StringBuilder outbuilder = new StringBuilder();
		private GraphRunnerStack stackbuilder = new GraphRunnerStack();
		private long stepbuilder = 0;

		private readonly BCGraph graph;
		private BCVertex current = null;

		public GraphRunner(BCGraph g)
		{
			this.graph = g;
		}

		public void run()
		{
			current = graph.root;

			while (current != null)
			{
				current = current.Execute(outbuilder, stackbuilder);

				stepbuilder++;
			}
		}
	}
}
