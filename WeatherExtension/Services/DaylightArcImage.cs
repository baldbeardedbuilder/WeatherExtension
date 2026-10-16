// Copyright (c) Bald Bearded Builder LLC
// Licensed under the MIT license. See LICENSE in the project root.

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Microsoft.CmdPal.Ext.Weather.Services;

internal static class DaylightArcImage
{
    internal static double Progress(DateTime now, DateTime sunrise, DateTime sunset) =>
        sunset > sunrise ? Math.Clamp((now - sunrise).TotalSeconds / (sunset - sunrise).TotalSeconds, 0, 1)
            : throw new ArgumentException("Sunset must be after sunrise.", nameof(sunset));

    public static string Create(DateTime now, DateTime sunrise, DateTime sunset)
    {
        var progress = Progress(now, sunrise, sunset);
        using var bitmap = new Bitmap(900, 400, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var track = new Pen(Color.FromArgb(160, 130, 140, 150), 3) { DashStyle = DashStyle.Dash };
        using var elapsed = new Pen(Color.FromArgb(218, 153, 40), 5);
        graphics.DrawLine(track, 60, 350, 840, 350);
        graphics.DrawArc(track, 150, 50, 600, 600, 180, 180);
        if (progress > 0)
        {
            graphics.DrawArc(elapsed, 150, 50, 600, 600, 180, (float)(180 * progress));
        }

        // Outside daylight hours there is no sun above the horizon.
        if (now >= sunrise && now <= sunset)
        {
            using var sun = Image.FromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "Flyouts", "sun.png"));
            var x = 450 - 300 * Math.Cos(Math.PI * progress);
            var y = 350 - 300 * Math.Sin(Math.PI * progress);
            graphics.DrawImage(sun, (float)x - 30, (float)y - 30, 60, 60);
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return $"data:image/png;base64,{Convert.ToBase64String(stream.ToArray())}";
    }
}
