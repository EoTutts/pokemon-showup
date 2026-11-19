using System;

namespace R36S.A11y;

public sealed class Voice
{
	private readonly Audio audio;

	internal readonly uint SourceId;

	internal bool Active;

	private double volume = 1.0;

	private (double From, double To, double Total, double Elapsed, bool Stop)? fade;

	internal bool Fading => fade.HasValue;

	public bool Playing
	{
		get
		{
			if (!Active)
			{
				return false;
			}
			Al.alGetSourcei(SourceId, 4112, out var value);
			if ((uint)(value - 4114) <= 1u)
			{
				return true;
			}
			return false;
		}
	}

	internal Voice(Audio audio, uint source, bool active)
	{
		this.audio = audio;
		SourceId = source;
		Active = active;
	}

	internal void Gain(double v)
	{
		volume = v;
		Al.alSourcef(SourceId, 4106, (float)v);
	}

	public void SetVolume(double v, double fadeSeconds = 0.0)
	{
		if (Active)
		{
			if (fadeSeconds > 0.0)
			{
				fade = (volume, v, fadeSeconds, 0.0, false);
				return;
			}
			fade = null;
			Gain(v);
		}
	}

	public void SetPitch(double p)
	{
		if (Active)
		{
			Al.alSourcef(SourceId, 4099, (float)p);
		}
	}

	public void SetPosition(double x, double y, double z)
	{
		if (Active)
		{
			Audio.Place(SourceId, (x, y, z), null);
		}
	}

	public void SetPan(double pan)
	{
		if (Active)
		{
			Audio.Place(SourceId, null, pan);
		}
	}

	public void Pause()
	{
		if (Active)
		{
			Al.alSourcePause(SourceId);
		}
	}

	public void Resume()
	{
		if (Active)
		{
			Al.alGetSourcei(SourceId, 4112, out var value);
			if (value == 4115)
			{
				Al.alSourcePlay(SourceId);
			}
		}
	}

	public void Stop(double fadeSeconds = 0.0)
	{
		if (Active)
		{
			if (fadeSeconds > 0.0)
			{
				fade = (volume, 0.0, fadeSeconds, 0.0, true);
			}
			else
			{
				Al.alSourceStop(SourceId);
			}
		}
	}

	internal void Tick(double dt)
	{
		(double, double, double, double, bool)? tuple = fade;
		if (!tuple.HasValue)
		{
			return;
		}
		(double, double, double, double, bool) valueOrDefault = tuple.GetValueOrDefault();
		double num = valueOrDefault.Item4 + dt;
		double num2 = Math.Min(1.0, num / valueOrDefault.Item3);
		Gain(valueOrDefault.Item1 + (valueOrDefault.Item2 - valueOrDefault.Item1) * num2);
		if (num2 < 1.0)
		{
			(double, double, double, double, bool) value = valueOrDefault;
			value.Item4 = num;
			fade = value;
			return;
		}
		fade = null;
		if (valueOrDefault.Item5)
		{
			Al.alSourceStop(SourceId);
		}
	}
}
