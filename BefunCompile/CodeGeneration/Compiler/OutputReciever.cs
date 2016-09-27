using System;
using System.Text;

namespace BefunCompile.CodeGeneration.Compiler
{
	public interface IOutputReciever
	{
		void AppendLine();
		void AppendLine(string line);
	}

	public class StandardOutReciever : IOutputReciever
	{
		public void AppendLine() => Console.Out.WriteLine();
		public void AppendLine(string line) => Console.Out.WriteLine(line);
	}

	public class StringBuilderReciever : IOutputReciever
	{
		private readonly StringBuilder _builder = new StringBuilder();

		public void AppendLine() => _builder.AppendLine();
		public void AppendLine(string line) => _builder.AppendLine(line);

		public int Length => _builder.Length;
		public override string ToString() => _builder.ToString();
	}

	public class DummyReciever : IOutputReciever
	{
		public void AppendLine() { }
		public void AppendLine(string line) { }
	}
}
