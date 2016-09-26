using System.Collections.Generic;
using System.Linq;

namespace BefunCompile.Math
{
	public class MSZipImplementation : CommonZipImplementation
	{
		const int MAX_REPEAT = 64;

		const byte REPL_OPEN = 0xF0;
		const byte REPL_CLOSE = 0xF1;

		const uint MASK_COUNTER = 0x800;

		public override List<byte> Compress(List<byte> g, ref int cc)
		{
			List<uint> current = g.Select(p => (uint)p).ToList();

			for (cc = 0; ; cc++)
			{
				List<uint> next = new List<uint>();

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

		public override List<byte> Decompress(List<byte> x, int resultsize)
		{
			byte[] result = new byte[resultsize];
			int rpos = 0;
			int xpos = 0;

			DecompressSingle(x.ToArray(), ref xpos, ref result, ref rpos);

			return result.TakeWhile(p => p != 0).ToList();
		}

		public void CompressSingle(List<uint> result, List<uint> data, int idatapos, int datacount)
		{
			for (int datapos = idatapos; datapos < idatapos + datacount; datapos++)
			{
				uint current = data[datapos];

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
						result.Add(counterDataEscape((uint)(' ' + (repetitions / (95 * 95)) % 95)));
						result.Add(counterDataEscape((uint)(' ' + (repetitions / 95) % 95)));
						result.Add(counterDataEscape((uint)(' ' + (repetitions / 1) % 95)));

						datapos += (repetitions * repLength) - 1;

						break;
					}
				}
				if (!replacement)
					result.Add(current);
			}
		}

		public uint counterDataEscape(uint c)
		{
			return c | MASK_COUNTER;
		}

		public bool isCounterData(uint c)
		{
			return (c & MASK_COUNTER) != 0;
		}

		public List<byte> Escape(List<uint> data)
		{
			List<byte> result = new List<byte>();

			foreach (var current in data)
			{
				if (isCounterData(current))
				{
					result.Add((byte)(current & ~MASK_COUNTER));
				}
				else
				{
					switch (current)
					{
						case (byte)'{':
						case (byte)'}':
						case (byte)';':
							result.Add((byte)';');
							result.Add((byte)current);
							break;
						case REPL_OPEN:
							result.Add((byte)'{');
							break;
						case REPL_CLOSE:
							result.Add((byte)'}');
							break;
						default:
							result.Add((byte)current);
							break;
					}
				}
			}

			return result;
		}

		public int getRepetitions(List<uint> data, int start, int length, int datacount)
		{
			int rep = 1;

			if (start + length * 2 > datacount)
				return 1;

			int height = 0;
			for (int i = 0; i < length; i++)
			{
				if (height < 0)
					return 1;

				if (data[start + i] == REPL_OPEN)
				{
					height++;
				}
				else if (data[start + i] == REPL_CLOSE)
				{
					if (i + 3 < length && isCounterData(data[start + i + 1]) && isCounterData(data[start + i + 2]) && isCounterData(data[start + i + 3]))
					{
						height--;
						i += 3;
					}
					else
					{
						return 1;
					}
				}
				else if (isCounterData(data[start + i]))
				{
					return 1;
				}
			}
			if (height != 0)
				return 1;

			for (int pos = length; start + pos + length <= datacount; pos += length)
			{
				for (int i = 0; i < length; i++)
				{
					if (data[start + i] != data[start + pos + i])
						return rep;
				}
				rep++;

				if (rep == (95 * 95 * 95 - 1))
					return rep;
			}

			return rep;
		}

		public int DecompressSingle(byte[] x, ref int xpos, ref byte[] result, ref int rpos)
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
	}
}
