using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace R36S.A11y;

public sealed class Audio
{
	private const int MaxSources = 32;

	private static readonly string[] ReverbNames = new string[10] { "density", "diffusion", "gain", "gain_hf", "decay", "decay_hf_ratio", "reflections_gain", "reflections_delay", "late_gain", "late_delay" };

	public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, double>> ReverbPresets = new Dictionary<string, IReadOnlyDictionary<string, double>>
	{
		["quarto"] = P(0.43, 1.0, 0.32, 0.6, 0.4, 0.83, 0.15, 0.002, 1.06, 0.003),
		["corredor"] = P(1.0, 1.0, 0.32, 0.71, 1.49, 0.59, 0.25, 0.007, 1.66, 0.011),
		["estacionamento"] = P(1.0, 1.0, 0.32, 1.0, 1.65, 1.5, 0.21, 0.008, 0.27, 0.012),
		["cozinha"] = P(1.0, 0.9, 0.32, 0.9, 1.0, 1.2, 0.5, 0.004, 1.2, 0.006),
		["metro"] = P(1.0, 1.0, 0.32, 0.71, 3.1, 1.0, 0.15, 0.03, 1.4, 0.03),
		["igreja"] = P(1.0, 1.0, 0.32, 0.6, 5.5, 0.6, 0.2, 0.04, 1.3, 0.05),
		["floresta"] = P(1.0, 0.3, 0.32, 0.02, 1.49, 0.54, 0.05, 0.16, 0.2, 0.09)
	};

	private readonly string baseDir;

	private readonly nint device;

	private readonly nint context;

	private uint slot;

	private uint effect;

	private readonly Dictionary<string, Sound> sounds = new Dictionary<string, Sound>();

	private readonly List<Voice> voices = new List<Voice>();

	private readonly Stack<uint> free = new Stack<uint>();

	public bool Available { get; private set; }

	public bool Hrtf { get; }

	public Sound? this[string name] => sounds.GetValueOrDefault(name);

	private static IReadOnlyDictionary<string, double> P(params double[] v)
	{
		return ReverbNames.Select((string n, int i) => (n: n, v[i])).ToDictionary(((string n, double) x) => x.n, ((string n, double) x) => x.Item2);
	}

	internal Audio(string baseDir, bool enable = true)
	{
		this.baseDir = baseDir;
		if (!enable || !NativeLibrary.TryLoad("libopenal.so.1", out var _))
		{
			return;
		}
		try
		{
			device = Al.alcOpenDevice(IntPtr.Zero);
			if (device == IntPtr.Zero)
			{
				return;
			}
			int[] array = ((Al.alcIsExtensionPresent(device, "ALC_SOFT_HRTF") == 0) ? null : new int[3] { 6546, 1, 0 });
			context = Al.alcCreateContext(device, array);
			if (context == IntPtr.Zero)
			{
				return;
			}
			Al.alcMakeContextCurrent(context);
			if (array != null)
			{
				Al.alcGetIntegerv(device, 6546, 1, out var data);
				Hrtf = data != 0;
			}
			Al.alDistanceModel(53250);
			Available = true;
			Listener((X: 0.0, Y: 0.0, Z: 0.0), 0.0);
		}
		catch (Exception)
		{
			Available = false;
			return;
		}
		try
		{
			if (Al.alcIsExtensionPresent(device, "ALC_EXT_EFX") != 0)
			{
				Al.alGenAuxiliaryEffectSlots(1, out slot);
				Al.alGenEffects(1, out effect);
				Al.alEffecti(effect, 32769, 1);
			}
		}
		catch (EntryPointNotFoundException)
		{
			slot = 0u;
		}
	}

	public Sound Load(string name, string path)
	{
		if (!Path.IsPathRooted(path))
		{
			path = Path.Combine(baseDir, path);
		}
		Sound sound = new Sound(this, name);
		sounds[name] = sound;
		if (!Available)
		{
			return sound;
		}
		int num;
		int num2;
		int freq;
		byte[] array;
		if (path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
		{
			(num, num2, freq, array) = DecodeOgg(path);
		}
		else
		{
			(num, num2, freq, array) = DecodeWav(path);
		}
		int num3;
		if (num != 1)
		{
			if (num != 2)
			{
				goto IL_00d9;
			}
			if (num2 != 1)
			{
				if (num2 != 2)
				{
					goto IL_00d9;
				}
				num3 = 4355;
			}
			else
			{
				num3 = 4354;
			}
		}
		else if (num2 != 1)
		{
			if (num2 != 2)
			{
				goto IL_00d9;
			}
			num3 = 4353;
		}
		else
		{
			num3 = 4352;
		}
		int format = num3;
		Al.alGenBuffers(1, out var buffer);
		Al.alBufferData(buffer, format, array, array.Length, freq);
		sound.Buffer = buffer;
		sound.Channels = num;
		return sound;
		IL_00d9:
		throw new InvalidDataException(path + ": só mono ou estéreo, 8 ou 16 bits");
	}

	internal Voice Play(Sound sound, bool loop, double volume, double pitch, (double X, double Y, double Z)? pos, double? pan, bool? reverb)
	{
		if (!Available || sound.Buffer == 0)
		{
			return new Voice(this, 0u, active: false);
		}
		uint num = Source();
		Al.alSourceStop(num);
		Al.alSourcei(num, 4105, (int)sound.Buffer);
		Al.alSourcei(num, 4103, loop ? 1 : 0);
		Al.alSourcef(num, 4099, (float)pitch);
		Al.alSourcef(num, 4128, 1f);
		Al.alSourcef(num, 4129, 1f);
		Al.alSourcef(num, 4131, 100f);
		Place(num, pos, pan);
		if (slot != 0)
		{
			Al.alSource3i(num, 131078, (int)((reverb ?? pos.HasValue) ? slot : 0), 0, 0);
		}
		Voice voice = new Voice(this, num, active: true);
		voice.Gain(volume);
		voices.Add(voice);
		Al.alSourcePlay(num);
		return voice;
	}

	private uint Source()
	{
		if (free.Count > 0)
		{
			return free.Pop();
		}
		if (voices.Count < 32)
		{
			Al.alGenSources(1, out var source);
			return source;
		}
		Voice voice = voices.FirstOrDefault((Voice v) => !v.Playing) ?? voices[0];
		Al.alSourceStop(voice.SourceId);
		voices.Remove(voice);
		voice.Active = false;
		return voice.SourceId;
	}

	internal static void Place(uint src, (double X, double Y, double Z)? pos, double? pan)
	{
		if (pos.HasValue)
		{
			(double, double, double) valueOrDefault = pos.GetValueOrDefault();
			Al.alSourcei(src, 514, 0);
			Al.alSource3f(src, 4100, (float)valueOrDefault.Item1, (float)valueOrDefault.Item2, (float)valueOrDefault.Item3);
		}
		else if (pan.HasValue)
		{
			double valueOrDefault2 = pan.GetValueOrDefault();
			valueOrDefault2 = Math.Clamp(valueOrDefault2, -1.0, 1.0);
			Al.alSourcei(src, 514, 1);
			Al.alSource3f(src, 4100, (float)valueOrDefault2, 0f, (float)(0.0 - Math.Sqrt(1.0 - valueOrDefault2 * valueOrDefault2)));
		}
		else
		{
			Al.alSourcei(src, 514, 1);
			Al.alSource3f(src, 4100, 0f, 0f, 0f);
		}
	}

	public void Listener((double X, double Y, double Z) pos, double facing)
	{
		if (Available)
		{
			Al.alListener3f(4100, (float)pos.X, (float)pos.Y, (float)pos.Z);
			double num = facing * Math.PI / 180.0;
			Al.alListenerfv(4111, new float[6]
			{
				(float)Math.Sin(num),
				0f,
				(float)(0.0 - Math.Cos(num)),
				0f,
				1f,
				0f
			});
		}
	}

	public void SetReverb(string? preset)
	{
		SetReverb((preset == null) ? null : ReverbPresets[preset]);
	}

	public void SetReverb(IReadOnlyDictionary<string, double>? parameters)
	{
		if (!Available || slot == 0)
		{
			return;
		}
		if (parameters == null)
		{
			Al.alAuxiliaryEffectSloti(slot, 1, 0);
			return;
		}
		foreach (var (value, num2) in parameters)
		{
			int num3 = Array.IndexOf(ReverbNames, value);
			if (num3 >= 0)
			{
				Al.alEffectf(effect, num3 + 1, (float)num2);
			}
		}
		Al.alAuxiliaryEffectSloti(slot, 1, (int)effect);
	}

	public void PauseAll()
	{
		voices.ForEach((Voice v) =>
		{
			v.Pause();
		});
	}

	public void ResumeAll()
	{
		voices.ForEach((Voice v) =>
		{
			v.Resume();
		});
	}

	internal void Update(double dt)
	{
		if (!Available)
		{
			return;
		}
		Voice[] array = voices.ToArray();
		foreach (Voice voice in array)
		{
			voice.Tick(dt);
			if (!voice.Fading && !voice.Playing)
			{
				voices.Remove(voice);
				voice.Active = false;
				free.Push(voice.SourceId);
			}
		}
	}

	internal void Close()
	{
		if (Available)
		{
			Al.alcMakeContextCurrent(IntPtr.Zero);
			Al.alcDestroyContext(context);
			Al.alcCloseDevice(device);
			Available = false;
		}
	}

	internal static (int Channels, int Width, int Rate, byte[] Data) DecodeWav(string path)
	{
		byte[] array = File.ReadAllBytes(path);
		if (array.Length < 12 || array[0] != 82 || array[8] != 87)
		{
			throw new InvalidDataException(path + ": não é WAV");
		}
		int num = 0;
		int item = 0;
		int item2 = 0;
		byte[] array2 = null;
		int num2;
		for (int i = 12; i + 8 <= array.Length; i += 8 + num2 + num2 % 2)
		{
			string text = Encoding.ASCII.GetString(array, i, 4);
			num2 = BinaryPrimitives.ReadInt32LittleEndian(array.AsSpan(i + 4));
			Span<byte> span = array.AsSpan(i + 8, Math.Min(num2, array.Length - i - 8));
			if (text == "fmt ")
			{
				if (BinaryPrimitives.ReadUInt16LittleEndian(span) != 1)
				{
					throw new InvalidDataException(path + ": WAV precisa ser PCM");
				}
				num = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(2));
				item2 = BinaryPrimitives.ReadInt32LittleEndian(span.Slice(4));
				item = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(14)) / 8;
			}
			else if (text == "data")
			{
				array2 = span.ToArray();
			}
		}
		if (array2 == null || num == 0)
		{
			throw new InvalidDataException(path + ": WAV incompleto");
		}
		return (Channels: num, Width: item, Rate: item2, Data: array2);
	}

	private static (int Channels, int Width, int Rate, byte[] Data) DecodeOgg(string path)
	{
		nint num = Marshal.AllocHGlobal(8192);
		try
		{
			if (Vorbis.ov_fopen(path, num) != 0)
			{
				throw new InvalidDataException(path + ": não é um OGG Vorbis válido");
			}
			try
			{
				nint ptr = Vorbis.ov_info(num, -1);
				int item = Marshal.ReadInt32(ptr, 4);
				int item2 = (int)Marshal.ReadInt64(ptr, 8);
				using MemoryStream memoryStream = new MemoryStream();
				byte[] array = new byte[65536];
				while (true)
				{
					long num2 = Vorbis.ov_read(num, array, array.Length, 0, 2, 1, out var _);
					if (num2 <= 0)
					{
						break;
					}
					memoryStream.Write(array, 0, (int)num2);
				}
				return (Channels: item, Width: 2, Rate: item2, Data: memoryStream.ToArray());
			}
			finally
			{
				Vorbis.ov_clear(num);
			}
		}
		finally
		{
			Marshal.FreeHGlobal(num);
		}
	}
}
