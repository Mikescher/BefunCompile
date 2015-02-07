using BefunCompile.Graph.Expression;
using BefunCompile.Math;
using System.Collections.Generic;
using System.Text;

namespace BefunCompile.Graph
{
	public class GraphRunner : CalculateInterface
	{
		public string Output { get { return outbuilder.ToString(); } }
		public long[] Stack { get { return stackbuilder.stack.ToArray(); } }
		public long Steps { get { return stepbuilder; } }

		private StringBuilder outbuilder = new StringBuilder();
		private GraphRunnerStack stackbuilder = new GraphRunnerStack();
		private long stepbuilder = 0;

		private readonly BCGraph graph;
		private BCVertex current = null;

		private Dictionary<ExpressionVariable, long> varDic = new Dictionary<ExpressionVariable, long>();
		private Dictionary<Vec2l, long> varGrid = new Dictionary<Vec2l, long>();

		public GraphRunner(BCGraph g)
		{
			this.graph = g;

			foreach (var var in graph.Variables)
			{
				varDic.Add(var, var.initial);
			}

			for (int x = 0; x < graph.Width; x++)
			{
				for (int y = 0; y < graph.Height; y++)
				{
					varGrid.Add(new Vec2l(x, y), graph.SourceGrid[x, y]);
				}
			}
		}

		public void run()
		{
			current = graph.Root;

			while (current != null)
			{
				current = current.Execute(outbuilder, stackbuilder, this);

				stepbuilder++;
			}
		}

		public long GetVariableValue(ExpressionVariable v)
		{
			return varDic[v];
		}

		public void SetVariableValue(ExpressionVariable v, long value)
		{
			varDic[v] = value;
		}

		public void SetGridValue(long xx, long yy, long value)
		{
			Vec2l pos = new Vec2l(xx, yy);

			if (varGrid.ContainsKey(pos))
				varGrid[pos] = value;
			else
				varGrid.Add(pos, value);
		}

		public long GetGridValue(long xx, long yy)
		{
			Vec2l pos = new Vec2l(xx, yy);

			if (varGrid.ContainsKey(pos))
				return varGrid[pos];
			else
				return 0;
		}
	}
}
