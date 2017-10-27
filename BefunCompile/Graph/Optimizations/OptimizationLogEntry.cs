namespace BefunCompile.Graph.Optimizations
{
	public sealed class OptimizationLogEntry
	{
		public readonly int Level;
		public readonly string Name;
		public readonly string Info;

		public OptimizationLogEntry(int l, string n, string i = "")
		{
			Level = l;
			Name = n;
			Info = i;
		}

		public override string ToString() => $"[{Level}] {Name}";
	}
}
