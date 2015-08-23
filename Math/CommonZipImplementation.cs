using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Math
{
	public abstract class CommonZipImplementation
	{
		public List<byte> Compress(List<byte> g)
		{
			int cc = 0;
			return Compress(g, ref cc);
		}

		public abstract List<byte> Compress(List<byte> g, ref int cc);

		public abstract List<byte> Decompress(List<byte> x, int resultsize);

		public string CompressToString(string data)
		{
			return string.Join("", Compress(Encoding.ASCII.GetBytes(data).ToList()).Select(p => (char)p).ToList());
		}

		public string CompressToString(string data, ref int cc)
		{
			return string.Join("", Compress(Encoding.ASCII.GetBytes(data).ToList(), ref cc).Select(p => (char)p).ToList());
		}

		public string DecompressToString(string data, int resultsize)
		{
			return string.Join("", Decompress(Encoding.ASCII.GetBytes(data).ToList(), resultsize).Select(p => (char)p).ToList());
		}

		public string CompressToBase64(string data)
		{
			return Convert.ToBase64String(Compress(Encoding.ASCII.GetBytes(data).ToList()).ToArray());
		}

		public string CompressToHex(string data)
		{
			return string.Join(" ", Compress(Encoding.ASCII.GetBytes(data).ToList()).Select(p => String.Format("{0:X2}", p)));
		}

		public string AnsiCEscaped(string data)
		{
			return data.Replace("\\", "\\\\").Replace("\"", "\\\"");
		}

		public List<string> GenerateBase64StringList(string data)
		{
			data = CompressToBase64(data);

			return Enumerable
				.Range(0, data.Length / 128 + 2)
				.Select(i => (i * 128 > data.Length) ? "" : data.Substring(i * 128, System.Math.Min(i * 128 + 128, data.Length) - i * 128))
				.Where(p => p != "")
				.ToList();
		}

		public List<string> GenerateAnsiCEscapedStringList(string data)
		{
			int tmp;

			return GenerateAnsiCEscapedStringList(data, out tmp);
		}

		public List<string> GenerateAnsiCEscapedStringList(string data, out int size)
		{
			data = CompressToString(data);

			size = data.Length;

			return Enumerable
				.Range(0, data.Length / 128 + 2)
				.Select(i => (i * 128 > data.Length) ? "" : data.Substring(i * 128, System.Math.Min(i * 128 + 128, data.Length) - i * 128))
				.Where(p => p != "")
				.Select(AnsiCEscaped)
				.ToList();
		}
	}
}
