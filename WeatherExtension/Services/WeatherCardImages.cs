// Copyright (c) Bald Bearded Builder LLC
// Bald Bearded Builder LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.CmdPal.Ext.Weather.Models;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Microsoft.CmdPal.Ext.Weather.Services;

internal static class WeatherCardImages
{
	private static readonly ConcurrentDictionary<(string Kind, bool Night), string> ConditionImages = new();
	private static readonly Rgb Gold = new(173, 121, 32);
	private static readonly Rgb Cloud = new(100, 133, 154);
	private static readonly Rgb Blue = new(58, 130, 179);
	private static readonly Rgb Track = new(115, 132, 147);

	public static async Task PopulateAsync(WeatherCardData data)
	{
		if (data.CurrentWeatherCode is int currentCode)
		{
			data.CurrentImage = await ConditionAsync(currentCode, data.IsNight).ConfigureAwait(false);
		}

		foreach (var item in data.Hours.Concat(data.Days))
		{
			if (item.WeatherCode is int code)
			{
				item.Image = await ConditionAsync(code, item.IsNight).ConfigureAwait(false);
			}
		}

		if (data.Daylight.Phase != DaylightPhase.Unavailable)
		{
			data.Daylight.Image = await DaylightAsync(data.Daylight.Progress, data.Daylight.Phase).ConfigureAwait(false);
		}
	}

	internal static async Task<string> ConditionAsync(int code, bool isNight)
	{
		var kind = code switch
		{
			0 => "clear",
			1 or 2 => "partly",
			3 => "cloud",
			45 or 48 => "fog",
			51 or 53 or 55 or 56 or 57 => "drizzle",
			61 or 63 or 65 or 66 or 67 or 80 or 81 or 82 => "rain",
			71 or 73 or 75 or 77 or 85 or 86 => "snow",
			95 or 96 or 99 => "storm",
			_ => "unknown",
		};
		var key = (kind, isNight);
		if (ConditionImages.TryGetValue(key, out var image))
		{
			return image;
		}

		var raster = new Raster(96, 96);
		if (kind == "clear")
		{
			if (isNight)
			{
				raster.Moon(48, 45, 24, Gold);
			}
			else
			{
				raster.Sun(48, 45, 20, Gold);
			}
		}
		else if (kind == "unknown")
		{
			raster.Line(48, 25, 48, 51, 4, Track);
			raster.Circle(48, 66, 3, Track);
		}
		else
		{
			if (kind == "partly")
			{
				if (isNight)
				{
					raster.Moon(34, 30, 16, Gold);
				}
				else
				{
					raster.Sun(34, 30, 15, Gold);
				}
			}

			raster.Circle(28, 53, 15, Cloud);
			raster.Circle(46, 42, 22, Cloud);
			raster.Circle(70, 53, 15, Cloud);
			raster.Line(28, 59, 70, 59, 13, Cloud);
			if (kind is "rain" or "drizzle")
			{
				foreach (var x in new[] { 30, 48, 66 })
				{
					raster.Line(x, 75, x - 4, kind == "drizzle" ? 80 : 85, 3, Blue);
				}
			}
			else if (kind == "snow")
			{
				foreach (var x in new[] { 30, 48, 66 })
				{
					raster.Line(x - 4, 80, x + 4, 80, 2, Blue);
					raster.Line(x, 76, x, 84, 2, Blue);
					raster.Line(x - 3, 77, x + 3, 83, 2, Blue);
				}
			}
			else if (kind == "fog")
			{
				raster.Line(20, 76, 76, 76, 3, Track);
				raster.Line(28, 84, 68, 84, 3, Track);
			}
			else if (kind == "storm")
			{
				raster.Line(53, 70, 44, 80, 4, Gold);
				raster.Line(44, 80, 54, 80, 4, Gold);
				raster.Line(54, 80, 45, 90, 4, Gold);
			}
		}

		image = await raster.EncodeAsync().ConfigureAwait(false);
		ConditionImages.TryAdd(key, image);
		return image;
	}

	internal static Task<string> DaylightAsync(double progress, DaylightPhase phase)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(progress, 0);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(progress, 1);
		if (!double.IsFinite(progress))
		{
			throw new ArgumentOutOfRangeException(nameof(progress));
		}

		var raster = new Raster(720, 96);
		var previous = ArcPoint(0);
		for (var i = 1; i <= 200; i++)
		{
			var fraction = i / 200.0;
			var point = ArcPoint(fraction);
			raster.Line(previous.X, previous.Y, point.X, point.Y, 2, Track);
			previous = point;
		}

		previous = ArcPoint(0);
		for (var i = 1; i <= 200; i++)
		{
			var fraction = progress * i / 200;
			var point = ArcPoint(fraction);
			raster.Line(previous.X, previous.Y, point.X, point.Y, 3, Gold);
			previous = point;
		}

		if (phase == DaylightPhase.Daytime)
		{
			var marker = ArcPoint(progress);
			raster.Sun(marker.X, marker.Y, 7, Gold);
		}

		return raster.EncodeAsync();
	}

	private static (double X, double Y) ArcPoint(double t)
		=> (18 + (684 * t), (80 * (1 - t) * (1 - t)) - (130 * (1 - t) * t) + (80 * t * t));

	private readonly record struct Rgb(byte R, byte G, byte B);

	private sealed class Raster(int width, int height)
	{
		private const int Scale = 2;
		private readonly byte[] _pixels = new byte[width * height * Scale * Scale * 4];

		public void Circle(double x, double y, double radius, Rgb color)
			=> Disc(x, y, radius, color, false);

		public void Moon(double x, double y, double radius, Rgb color)
			=> Disc(x, y, radius, color, true);

		private void Disc(double x, double y, double radius, Rgb color, bool crescent)
		{
			var minX = Math.Max(0, (int)((x - radius) * Scale));
			var maxX = Math.Min((width * Scale) - 1, (int)((x + radius) * Scale));
			var minY = Math.Max(0, (int)((y - radius) * Scale));
			var maxY = Math.Min((height * Scale) - 1, (int)((y + radius) * Scale));
			for (var py = minY; py <= maxY; py++)
			{
				for (var px = minX; px <= maxX; px++)
				{
					var dx = ((px + 0.5) / Scale) - x;
					var dy = ((py + 0.5) / Scale) - y;
					if ((dx * dx) + (dy * dy) > radius * radius ||
						(crescent && (((dx - (radius * 0.55)) * (dx - (radius * 0.55))) +
							((dy + (radius * 0.3)) * (dy + (radius * 0.3))) < radius * radius)))
					{
						continue;
					}

					var index = ((py * width * Scale) + px) * 4;
					_pixels[index] = color.B;
					_pixels[index + 1] = color.G;
					_pixels[index + 2] = color.R;
					_pixels[index + 3] = 255;
				}
			}
		}

		public void Line(double x1, double y1, double x2, double y2, double thickness, Rgb color)
		{
			var steps = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(((x2 - x1) * (x2 - x1)) + ((y2 - y1) * (y2 - y1))) * Scale));
			for (var i = 0; i <= steps; i++)
			{
				var t = (double)i / steps;
				Circle(x1 + ((x2 - x1) * t), y1 + ((y2 - y1) * t), thickness / 2, color);
			}
		}

		public void Sun(double x, double y, double radius, Rgb color)
		{
			Circle(x, y, radius, color);
			for (var i = 0; i < 8; i++)
			{
				var angle = i * Math.PI / 4;
				Line(x + (Math.Cos(angle) * (radius + 4)), y + (Math.Sin(angle) * (radius + 4)),
					x + (Math.Cos(angle) * (radius + 8)), y + (Math.Sin(angle) * (radius + 8)), 2, color);
			}
		}

		public async Task<string> EncodeAsync()
		{
			using var stream = new InMemoryRandomAccessStream();
			var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
			encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Straight,
				(uint)(width * Scale), (uint)(height * Scale), 96, 96, _pixels);
			await encoder.FlushAsync();
			stream.Seek(0);
			using var reader = new DataReader(stream);
			await reader.LoadAsync((uint)stream.Size);
			var bytes = new byte[(int)stream.Size];
			reader.ReadBytes(bytes);
			return "data:image/png;base64," + Convert.ToBase64String(bytes);
		}
	}
}
