using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;

namespace R36S.A11y;

public sealed class Game
{
	private static readonly TimeSpan RepeatDelay = TimeSpan.FromMilliseconds(400.0);

	private static readonly TimeSpan RepeatInterval = TimeSpan.FromMilliseconds(100.0);

	private readonly GameOptions options;

	private readonly InputState input;

	private readonly DateTimeOffset start = DateTimeOffset.UtcNow;

	private readonly Stopwatch clock = Stopwatch.StartNew();

	private readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();

	private readonly AutoResetEvent wake = new AutoResetEvent(initialState: false);

	private readonly List<(TimeSpan At, Action Fn)> timers = new List<(TimeSpan, Action)>();

	private readonly Dictionary<Button, TimeSpan> repeatAt = new Dictionary<Button, TimeSpan>();

	private readonly List<IFocus> focus = new List<IFocus>();

	private volatile bool running;

	public string Id { get; }

	public string Name { get; }

	public string Dir { get; }

	public string SaveDir { get; }

	public string Lang { get; }

	public Audio Sound { get; }

	public Speech Speech { get; }

	public long Ms => (long)(DateTimeOffset.UtcNow - start).TotalMilliseconds;

	public event Action<ButtonEvent>? Button;

	public event Action<AxisEvent>? Axis;

	public event Action<double>? Update;

	public event Action? Start;

	public event Action? Quit;

	public Game(GameOptions? options = null)
	{
		this.options = options ?? new GameOptions();
		Dir = Environment.GetEnvironmentVariable("A11Y_GAME_DIR") ?? AppContext.BaseDirectory;
		string text = null;
		string text2 = null;
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(Dir, "game.json")));
			if (jsonDocument.RootElement.TryGetProperty("id", out var value))
			{
				text = value.GetString();
			}
			if (jsonDocument.RootElement.TryGetProperty("name", out var value2))
			{
				text2 = value2.GetString();
			}
		}
		catch (Exception)
		{
		}
		Id = text ?? new DirectoryInfo(Dir).Name;
		Name = text2 ?? Id;
		Lang = Environment.GetEnvironmentVariable("A11Y_LANG") ?? "pt-BR";
		SaveDir = Environment.GetEnvironmentVariable("A11Y_SAVE_DIR") ?? Path.Combine(new string[5]
		{
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			".local",
			"share",
			"r36s-a11y",
			Id
		});
		Directory.CreateDirectory(SaveDir);
		Speech = new Speech(Id, Lang, Post);
		Sound = new Audio(Dir, this.options.EnableAudio);
		input = new InputState(this.options.Deadzone);
	}

	public void Post(Action action)
	{
		queue.Enqueue(action);
		wake.Set();
	}

	public void Say(string text, bool queue = false, Action<bool>? onDone = null)
	{
		Speech.Say(text, queue, onDone);
	}

	public void SayThen(string text, Action then)
	{
		Speech.Say(text, queue: false, (bool _) =>
		{
			then();
		});
	}

	public void Hush()
	{
		Speech.Hush();
	}

	public void After(double seconds, Action fn)
	{
		timers.Add((clock.Elapsed + TimeSpan.FromSeconds(seconds), fn));
	}

	public double AxisValue(Axis axis)
	{
		return input.AxisValue.GetValueOrDefault(axis);
	}

	public void Exit()
	{
		running = false;
	}

	public void PushFocus(IFocus f)
	{
		focus.Add(f);
	}

	public void PopFocus(IFocus f)
	{
		focus.Remove(f);
	}

	private void DispatchButton(ButtonEvent e)
	{
		if (focus.Count > 0)
		{
			List<IFocus> list = focus;
			list[list.Count - 1].HandleButton(e);
		}
		else
		{
			Button?.Invoke(e);
		}
	}

	private void Handle(object ev)
	{
		if (!(ev is ButtonEvent e))
		{
			if (ev is AxisEvent obj && options.Axes)
			{
				Axis?.Invoke(obj);
			}
			return;
		}
		if (options.Repeat)
		{
			if (e.State == ButtonState.Pressed)
			{
				repeatAt[e.Button] = clock.Elapsed + RepeatDelay;
			}
			else
			{
				repeatAt.Remove(e.Button);
			}
		}
		DispatchButton(e);
	}

	private void Tick()
	{
		TimeSpan now = clock.Elapsed;
		KeyValuePair<Button, TimeSpan>[] array = repeatAt.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			var (button2, timeSpan2) = array[i];
			if (!input.FnHeld && !(now < timeSpan2))
			{
				repeatAt[button2] = timeSpan2 + RepeatInterval;
				DispatchButton(new ButtonEvent(button2, ButtonState.Repeated, Ms));
			}
		}
		if (timers.Count == 0)
		{
			return;
		}
		List<(TimeSpan At, Action Fn)> list = (from t in timers
			where t.At <= now
			orderby t.At
			select t).ToList();
		timers.RemoveAll(((TimeSpan At, Action Fn) t) => t.At <= now);
		foreach (var item in list)
		{
			item.Fn();
		}
	}

	public void Run()
	{
		if (options.EnableInput && GamepadReader.Open(input, start, (ushort type, ushort code, int value, long ms) =>
		{
			Post(() =>
			{
				object obj = input.Feed(type, code, value, ms);
				if (obj != null)
				{
					Handle(obj);
				}
			});
		}) == 0)
		{
			Console.Error.WriteLine("[a11y] nenhum controle encontrado em /dev/input");
		}
		using (PosixSignalRegistration.Create(PosixSignal.SIGTERM, (PosixSignalContext c) =>
		{
			c.Cancel = true;
			Stop();
		}))
		{
			using (PosixSignalRegistration.Create(PosixSignal.SIGINT, (PosixSignalContext c) =>
			{
				c.Cancel = true;
				Stop();
			}))
			{
				running = true;
				Start?.Invoke();
				TimeSpan timeSpan = TimeSpan.FromSeconds(1.0 / (double)Math.Max(1, options.Fps));
				TimeSpan timeSpan2 = clock.Elapsed;
				try
				{
					while (running)
					{
						TimeSpan timeSpan3 = timeSpan2 + timeSpan - clock.Elapsed;
						if (timeSpan3 > TimeSpan.Zero)
						{
							wake.WaitOne(timeSpan3);
						}
						Action result;
						while (running && queue.TryDequeue(out result))
						{
							result();
						}
						TimeSpan elapsed = clock.Elapsed;
						if (!(elapsed - timeSpan2 < timeSpan))
						{
							double totalSeconds = (elapsed - timeSpan2).TotalSeconds;
							timeSpan2 = elapsed;
							Tick();
							Sound.Update(totalSeconds);
							Update?.Invoke(totalSeconds);
						}
					}
				}
				finally
				{
					try
					{
						Quit?.Invoke();
					}
					catch (Exception ex)
					{
						Console.Error.WriteLine("[a11y] erro ao sair: " + ex.Message);
					}
					Speech.Close();
					Sound.Close();
				}
			}
		}
	}

	private void Stop()
	{
		running = false;
		wake.Set();
	}
}
