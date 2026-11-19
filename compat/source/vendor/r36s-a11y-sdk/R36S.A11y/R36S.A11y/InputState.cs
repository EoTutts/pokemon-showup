using System;
using System.Collections.Generic;

namespace R36S.A11y;

internal sealed class InputState
{
	internal const ushort EvKey = 1;

	internal const ushort EvAbs = 3;

	internal const ushort FnCode = 708;

	internal const ushort LegacyFnCode = 316;

	internal static readonly Dictionary<ushort, Button> ButtonCodes = new Dictionary<ushort, Button>
	{
		[304] = Button.B,
		[305] = Button.A,
		[307] = Button.X,
		[308] = Button.Y,
		[310] = Button.L1,
		[311] = Button.R1,
		[312] = Button.L2,
		[313] = Button.R2,
		[544] = Button.Up,
		[545] = Button.Down,
		[546] = Button.Left,
		[547] = Button.Right,
		[704] = Button.Select,
		[705] = Button.Start,
		[706] = Button.L3,
		[707] = Button.R3,
		[103] = Button.Up,
		[108] = Button.Down,
		[105] = Button.Left,
		[106] = Button.Right,
		[314] = Button.Select,
		[315] = Button.Start
	};

	internal static readonly Dictionary<ushort, Axis> AxisCodes = new Dictionary<ushort, Axis>
	{
		[0] = Axis.LX,
		[1] = Axis.LY,
		[3] = Axis.RX,
		[4] = Axis.RY
	};

	private readonly double deadzone;

	private bool fnHeld;

	private readonly HashSet<Axis> suppressedAxes = new HashSet<Axis>();

	private readonly HashSet<Button> suppressed = new HashSet<Button>();

	private readonly HashSet<Button> held = new HashSet<Button>();

	private readonly Dictionary<ushort, (int Min, int Max)> axisRange = new Dictionary<ushort, (int, int)>();

	internal readonly Dictionary<Axis, double> AxisValue = new Dictionary<Axis, double>();

	internal bool FnHeld => fnHeld;

	internal InputState(double deadzone)
	{
		this.deadzone = deadzone;
	}

	internal void SetAxisRange(ushort code, int min, int max)
	{
		axisRange[code] = (min, max);
	}

	internal object? Feed(ushort type, ushort code, int value, long ms)
	{
		return type switch
		{
			1 => Key(code, value, ms), 
			3 => Abs(code, value, ms), 
			_ => null, 
		};
	}

	private object? Key(ushort code, int value, long ms)
	{
		if ((code == 316 || code == 708) ? true : false)
		{
			fnHeld = value != 0;
			return null;
		}
		if (ButtonCodes.TryGetValue(code, out var value2))
		{
			switch (value)
			{
			case 2:
				break;
			case 1:
				if (fnHeld)
				{
					suppressed.Add(value2);
					return null;
				}
				held.Add(value2);
				return new ButtonEvent(value2, ButtonState.Pressed, ms);
			default:
				if (suppressed.Remove(value2))
				{
					return null;
				}
				if (!held.Remove(value2))
				{
					return null;
				}
				return new ButtonEvent(value2, ButtonState.Released, ms);
			}
		}
		return null;
	}

	private object? Abs(ushort code, int raw, long ms)
	{
		if (!AxisCodes.TryGetValue(code, out var value))
		{
			return null;
		}
		int num;
		int num2;
		if (axisRange.TryGetValue(code, out (int, int) value2))
		{
			(num, num2) = value2;
		}
		else
		{
			num = -1800;
			num2 = 1800;
		}
		double num3 = (double)(num + num2) / 2.0;
		double num4 = (double)(num2 - num) / 2.0;
		if (num4 == 0.0)
		{
			num4 = 1.0;
		}
		double num5 = Math.Clamp(((double)raw - num3) / num4, -1.0, 1.0);
		num5 = ((Math.Abs(num5) < deadzone) ? 0.0 : Math.CopySign((Math.Abs(num5) - deadzone) / (1.0 - deadzone), num5));
		if (fnHeld)
		{
			if (num5 != 0.0)
			{
				suppressedAxes.Add(value);
			}
			return null;
		}
		if (suppressedAxes.Contains(value))
		{
			if (num5 != 0.0)
			{
				return null;
			}
			suppressedAxes.Remove(value);
		}
		double valueOrDefault = AxisValue.GetValueOrDefault(value);
		if (num5 == valueOrDefault || (num5 != 0.0 && Math.Abs(num5 - valueOrDefault) < 0.02))
		{
			return null;
		}
		AxisValue[value] = num5;
		return new AxisEvent(value, num5, ms);
	}
}
