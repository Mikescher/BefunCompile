using BefunCompile.Graph.Expression;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BefunCompile.Graph.Optimizations.Unstackify
{
	public class UnstackifyStateHistory
	{
		public Dictionary<BCVertex, UnstackifyState> StateDict;

		public HashSet<UnstackifyValue> StackValues;

		public UnstackifyStateHistory()
		{
			StateDict = new Dictionary<BCVertex, UnstackifyState>();
			StackValues = new HashSet<UnstackifyValue>();
		}

		public void AddState(BCVertex vertex, UnstackifyState state)
		{
			if (Contains(vertex))
				throw new Exception();

			StateDict[vertex] = state;

			state.Stack.ForEach(p => StackValues.Add(p));
		}

		public bool Contains(BCVertex vx)
		{
			return StateDict.ContainsKey(vx);
		}

		public void UpdatePoison()
		{
			StackValues.ToList().ForEach(p => p.UpdatePoison());
		}

		public void RemovePoison()
		{
			StackValues = new HashSet<UnstackifyValue>(StackValues.Where(p => !p.IsPoisoned));
		}

		public void CreateVariables(BCGraph graph, ref int identity, ref List<string> info)
		{
			var timetable = new List<List<UnstackifyValue>>();

			foreach (var variable in StackValues)
			{
				var found = false;
				foreach (var tab in timetable)
				{
					if (tab.All(p => variable.IsDistinctScope(p)))
					{
						tab.Add(variable);
						found = true;

						break;
					}
				}
				if (!found)
				{
					timetable.Add(new List<UnstackifyValue> { variable });
				}
			}
			
			foreach (var row in timetable)
			{
				var systemvar = ExpressionVariable.CreateSystemVariable(identity++, row.Select(p => p.Scope.ToList()).ToList());
				graph.Variables.Add(systemvar);

				info.Add("new var: " + systemvar.Identifier);

				row.ForEach(p => p.Replacement = systemvar);
			}
		}

		public int ValuesCount()
		{
			return StackValues.Count;
		}

		internal void PoisonVertex(BCVertex vertex)
		{
			StackValues.Where(p => p.Scope.Contains(vertex)).ToList().ForEach(p => p.Poison());

			UpdatePoison();
		}
	}
}
