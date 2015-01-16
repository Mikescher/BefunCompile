using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BefunCompile.Math
{
	public class MSZipImplementation
	{
		const int MAX_REPEAT = 64;

		const byte REPL_OPEN = 0xF0;
		const byte REPL_CLOSE = 0xF1;
		const byte REPL_ESCAPE = 0xF2;

		private MSZipImplementation()
		{
			// priv
		}

		public static List<byte> Compress(List<byte> g)
		{
			List<byte> current = g;

			for (int cc = 0; ; cc++)
			{
				List<byte> next = new List<byte>();

				CompressSingle(next, current, 0, current.Count);

				if (Escape(next).Count < Escape(current).Count)
				{
					current = next;
				}
				else
				{
					return Escape(current);
				}
			}
		}

		public static void CompressSingle(List<byte> result, List<byte> data, int idatapos, int datacount)
		{
			for (int datapos = idatapos; datapos < idatapos + datacount; datapos++)
			{
				byte current = (byte)data[datapos];

				bool replacement = false;
				for (int repLength = 1; repLength < MAX_REPEAT; repLength++)
				{
					int repetitions = getRepetitions(data, datapos, repLength, idatapos + datacount);

					if ((repetitions * repLength) > (repLength + 5))
					{
						replacement = true;

						result.Add(REPL_OPEN);

						CompressSingle(result, data, datapos, repLength);

						result.Add(REPL_CLOSE);
						result.Add(chrEscape(' ' + (repetitions / (95 * 95)) % 95));
						result.Add(chrEscape(' ' + (repetitions / 95) % 95));
						result.Add(chrEscape(' ' + (repetitions / 1) % 95));

						datapos += (repetitions * repLength) - 1;

						break;
					}
				}
				if (!replacement)
					result.Add(current);
			}
		}

		public static byte chrEscape(int c)
		{
			switch (c)
			{
				case '{':
					return REPL_OPEN;
				case '}':
					return REPL_CLOSE;
				case ';':
					return REPL_ESCAPE;
				default:
					return (byte)c;
			}
		}

		public static List<byte> Escape(List<byte> data)
		{
			List<byte> result = new List<byte>();

			foreach (var current in data)
			{
				switch (current)
				{
					case (byte)'{':
					case (byte)'}':
					case (byte)';':
						result.Add((byte)';');
						result.Add(current);
						break;
					case REPL_OPEN:
						result.Add((byte)'{');
						break;
					case REPL_CLOSE:
						result.Add((byte)'}');
						break;
					case REPL_ESCAPE:
						result.Add((byte)';');
						break;
					default:
						result.Add(current);
						break;
				}
			}

			return result;
		}

		public static int getRepetitions(List<byte> data, int start, int length, int datacount)
		{
			int rep = 1;

			for (int pos = length; start + pos + length <= datacount; pos += length)
			{
				for (int i = 0; i < length; i++)
				{
					if (data[start + i] != data[start + pos + i] || data[start + i] == REPL_CLOSE || data[start + i] == REPL_ESCAPE || data[start + i] == REPL_OPEN)
						return rep;
				}
				rep++;

				if (rep == (95 * 95 * 95 - 1))
					return rep;
			}

			return rep;
		}

		public static byte[] Decompress(byte[] x, int resultsize)
		{
			byte[] result = new byte[resultsize];
			int rpos = 0;
			int xpos = 0;

			DecompressSingle(x, ref xpos, ref result, ref rpos);

			return result;
		}

		public static int DecompressSingle(byte[] x, ref int xpos, ref byte[] result, ref int rpos)
		{
			int irpos = rpos;
			for (; xpos < x.Length; xpos++)
			{
				if (x[xpos] == ';')
				{
					result[rpos++] = x[++xpos];
				}
				else if (x[xpos] == '}')
				{
					return rpos - irpos;
				}
				else if (x[xpos] == '{')
				{
					xpos++;

					int startrpos = rpos;
					int size = DecompressSingle(x, ref xpos, ref result, ref rpos);

					int repetitions = (x[xpos + 1] - ' ') * (95 * 95);
					repetitions += (x[xpos + 2] - ' ') * (95);
					repetitions += (x[xpos + 3] - ' ');

					for (int i = 1; i < repetitions; i++)
					{
						for (int j = 0; j < size; j++)
						{
							result[rpos++] = result[startrpos + j];
						}
					}

					xpos += 3;
				}
				else
				{
					result[rpos++] = x[xpos];
				}
			}

			return rpos - irpos;
		}

		public static string CompressToString(string data)
		{
			return string.Join("", Compress(Encoding.ASCII.GetBytes(data).ToList()).Select(p => (char)p).ToList());
		}
	}
}
