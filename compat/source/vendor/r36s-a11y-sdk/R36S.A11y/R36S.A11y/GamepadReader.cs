using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace R36S.A11y;

internal static class GamepadReader
{
	private static readonly int EventSize = EventRecordSizeForPointer(IntPtr.Size);

	internal static int EventRecordSizeForPointer(int pointerSize)
	{
		if (pointerSize != 4)
		{
			return 24;
		}
		return 16;
	}

	[DllImport("libc", SetLastError = true)]
	private static extern int ioctl(int fd, nuint request, int[] data);

	private static nuint Eviocgabs(ushort axis)
	{
		return (uint)(-2145893120 | (64 + axis));
	}

	internal static int Open(InputState state, DateTimeOffset start, Action<ushort, ushort, int, long> onEvent)
	{
		if (!OperatingSystem.IsLinux() || !Directory.Exists("/dev/input"))
		{
			return 0;
		}
		int num = 0;
		foreach (string item in Directory.GetFiles("/dev/input", "event*").Order())
		{
			string fileName = Path.GetFileName(item);
			string path = Path.Combine("/sys/class/input", fileName, "device/name");
			try
			{
				string text = File.ReadAllText(path).Trim();
				if ((!(text == "GO-Super Gamepad") && !(text == "GA36-MB Gamepad")) || 1 == 0)
				{
					continue;
				}
			}
			catch (Exception)
			{
				continue;
			}
			FileStream fs;
			try
			{
				fs = new FileStream(item, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 0);
			}
			catch (Exception)
			{
				continue;
			}
			num++;
			int fd = (int)fs.SafeFileHandle.DangerousGetHandle();
			foreach (ushort key in InputState.AxisCodes.Keys)
			{
				int[] array = new int[6];
				try
				{
					if (ioctl(fd, Eviocgabs(key), array) == 0 && array[2] > array[1])
					{
						state.SetAxisRange(key, array[1], array[2]);
					}
				}
				catch (Exception)
				{
				}
			}
			Thread thread = new Thread(() =>
			{
				ReadLoop(fs, start, onEvent);
			});
			thread.IsBackground = true;
			thread.Start();
		}
		return num;
	}

	private static void ReadLoop(FileStream fs, DateTimeOffset start, Action<ushort, ushort, int, long> onEvent)
	{
		byte[] array = new byte[EventSize * 64 + EventSize];
		int num = 0;
		long num2 = start.ToUnixTimeMilliseconds();
		try
		{
			while (true)
			{
				int num3 = fs.Read(array, num, array.Length - num);
				if (num3 <= 0)
				{
					break;
				}
				int num4 = num + num3;
				int num5 = 0;
				for (int i = 0; i + EventSize <= num4; i += EventSize)
				{
					Span<byte> span = array.AsSpan(i, EventSize);
					long num6 = ((EventSize == 16) ? BinaryPrimitives.ReadInt32LittleEndian(span) : BinaryPrimitives.ReadInt64LittleEndian(span));
					long num7 = ((EventSize == 16) ? BinaryPrimitives.ReadInt32LittleEndian(span.Slice(4)) : BinaryPrimitives.ReadInt64LittleEndian(span.Slice(8)));
					int num8 = EventSize - 8;
					onEvent(BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(num8)), BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(num8 + 2)), BinaryPrimitives.ReadInt32LittleEndian(span.Slice(num8 + 4)), num6 * 1000 + num7 / 1000 - num2);
					num5 = i + EventSize;
				}
				num = num4 - num5;
				if (num > 0)
				{
					Buffer.BlockCopy(array, num5, array, 0, num);
				}
			}
		}
		catch (Exception)
		{
		}
		finally
		{
			fs.Dispose();
		}
	}
}
