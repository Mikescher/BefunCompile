using System.Text;

namespace BefunCompile.CodeGeneration.Generator
{
	public class SourceCodeBuilder
	{
		private readonly StringBuilder builder = new StringBuilder();

		public void AppendLine()
		{
			builder.AppendLine();
		}

		public void AppendLine(string line)
		{
			if (!string.IsNullOrWhiteSpace(line))
				builder.AppendLine(line.TrimEnd());
		}

		public void Append(long value)
		{
			builder.Append(value);
		}

		public void Append(string text)
		{
			builder.Append(text);
		}

		public void Append(char chr)
		{
			builder.Append(chr);
		}

		public override string ToString()
		{
			return builder.ToString();
		}
	}
}
