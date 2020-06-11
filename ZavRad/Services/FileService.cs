using SkiaSharp;
using SkiaSharp.HarfBuzz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZavRad.Models;

namespace ZavRad.Services
{
    public class FileService
    {
        private readonly DataService _service;
        private const int WIDTH = 15000;
        private string reference = "";

        public FileService(DataService service)
        {
            _service = service;
        }

        public int GetChunks(string referenceName, int zoomLevel)
        {
            return (int)Math.Ceiling(_service._references.FirstOrDefault(r => r.Name == referenceName).Length / (float)WIDTH * zoomLevel);
        }

        public byte[] GetImage(string referenceName, int zoomLevel, int chunk)
        {
            var refer = _service._references.First(r => r.Name == referenceName);
            var height = (_service._histograms[refer.Name].Max() + 1) * 15;

            using var surface = SKSurface.Create(new SKImageInfo(WIDTH, height));
            using SKCanvas canvas = surface.Canvas;
            canvas.Clear();

            using SKPaint paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black,
                StrokeWidth = 1
            };

            using SKPaint fillPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.Blue,
                StrokeWidth = 1
            };

            using SKPaint reverseFillPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.Red,
                StrokeWidth = 1
            };

            var startY = 7;
            var list = new List<long> { 0 };

            using var reader = new StreamReader($"{_service._references.IndexOf(refer)}.tmp");
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var splitLine = line.Split('\t');

                var read = new SamFile
                {
                    QueryName = splitLine[0],
                    ReferenceName = splitLine[1],
                    StartingPos = int.Parse(splitLine[2]),
                    DrawExpressions = DataService.GetDrawExpressions(splitLine[3]),
                    Reverse = bool.Parse(splitLine[4])
                };

                var realStartingX = zoomLevel * read.StartingPos;

                if (realStartingX >= (chunk + 1) * WIDTH)
                {
                    break;
                }

                var index = list.FindIndex(i => realStartingX > i + 1);
                var y = startY * (index != -1 ? index : list.Count);
                var lineLength = read.DrawExpressions.Sum(de => de.Length) * zoomLevel;

                if (index == -1)
                {
                    list.Add(realStartingX + lineLength);
                }
                else
                {
                    list[index] = realStartingX + lineLength;
                }

                realStartingX -= chunk * WIDTH;

                if (realStartingX < 0 && realStartingX + lineLength < 0)
                {
                    continue;
                }

                canvas.DrawLine(new SKPoint(realStartingX, y + 3), new SKPoint(realStartingX + lineLength, y + 3), paint);

                var currentX = realStartingX;
                foreach (var draw in read.DrawExpressions)
                {
                    if (draw.Type == DrawType.Rectangle)
                    {
                        canvas.DrawRect(currentX, y, draw.Length * zoomLevel, 5, read.Reverse ? reverseFillPaint : fillPaint);
                        canvas.DrawRect(currentX, y, draw.Length * zoomLevel, 5, paint);
                    }
                    currentX += draw.Length * zoomLevel;
                }

                canvas.Flush();
            }

            var width = (chunk + 1) * WIDTH * zoomLevel > refer.Length * zoomLevel ? refer.Length * zoomLevel % WIDTH : WIDTH;
            var realHeight = 7 * (list.Count + 1);
            SKPixmap pixmap = surface.Snapshot().Subset(SKRectI.Create(0, 0, width, realHeight)).PeekPixels();

            SKData data;
            if (realHeight > 15000)
            {
                data = pixmap.Encode(SKPngEncoderOptions.Default);
            }
            else
            {
                var options = new SKWebpEncoderOptions(SKWebpEncoderCompression.Lossless, 100);
                data = pixmap.Encode(options);
            }

            return data.ToArray();
        }

        public IEnumerable<Tooltip> GetTooltips(string referenceName, int zoomLevel)
        {
            var refer = _service._references.First(r => r.Name == referenceName);
            var tooltips = new List<Tooltip>();

            var startY = 7;
            var list = new List<long> { 0 };

            string line;
            using var reader = new StreamReader($"{_service._references.IndexOf(refer)}.tmp");
            while ((line = reader.ReadLine()) != null)
            {
                var splitLine = line.Split('\t');

                var read = new SamFile
                {
                    QueryName = splitLine[0],
                    ReferenceName = splitLine[1],
                    StartingPos = int.Parse(splitLine[2]),
                    DrawExpressions = DataService.GetDrawExpressions(splitLine[3]),
                    Reverse = bool.Parse(splitLine[4])
                };
                var realStartingX = zoomLevel * read.StartingPos;

                var index = list.FindIndex(i => realStartingX > i + 1);
                var y = startY * (index != -1 ? index : list.Count);
                var lineLength = read.DrawExpressions.Sum(de => de.Length) * zoomLevel;

                if (index == -1)
                {
                    list.Add(realStartingX + lineLength);
                }
                else
                {
                    list[index] = realStartingX + lineLength;
                }

                tooltips.Add(new Tooltip
                {
                    Height = 7,
                    Width = lineLength,
                    X = realStartingX,
                    Y = y,
                    Text = read.QueryName
                });
            }
            return tooltips;
        }

        public SKData DrawHistogram(string referenceName, int zoom, int chunk)
        {
            var refer = _service._references.FirstOrDefault(r => r.Name == referenceName);
            if (chunk == 0 && zoom == 10)
            {
                LoadReference(referenceName);
            }

            using var surface = SKSurface.Create(new SKImageInfo(refer.Length * zoom, 220));
            using SKCanvas canvas = surface.Canvas;

            canvas.Clear();

            using SKPaint paint = new SKPaint
            {
                Style = SKPaintStyle.StrokeAndFill,
                Color = SKColors.Blue,
                StrokeWidth = 1
            };

            using SKPaint linePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black,
                StrokeWidth = 1
            };

            using SKPaint dashPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black,
                StrokeWidth = 1,
                PathEffect = SKPathEffect.CreateDash(new[] { 20f, 5f }, 0)
            };

            var histData = _service._histograms[refer.Name];

            var scale = (float)(100 / histData.Average());

            for (int i = 0; i < refer.Length; i++)
            {
                canvas.DrawRect(i * zoom + 5, 200 - (histData[i] * scale), zoom, histData[i] * scale, paint);
            }

            canvas.DrawLine(5, 0, 5, 200, linePaint);
            canvas.DrawLine(5, 199, refer.Length * zoom, 199, linePaint);

            var average = (int)(Math.Round(histData.Average()));
            canvas.DrawLine(5, average * scale, refer.Length * zoom, average * scale, dashPaint);

            using SKPaint textPaint = new SKPaint
            {
                Style = SKPaintStyle.StrokeAndFill,
                Color = SKColors.Black,
                StrokeWidth = 1,
                TextSize = 20,
                Typeface = SKTypeface.FromFamilyName("Courier New"),
                SubpixelText = true
            };
            canvas.DrawText(average.ToString(), 6, average * scale - 2, textPaint);

            var cnt = textPaint.MeasureText("ACGT");
            if (zoom == 10)
            {
                textPaint.TextSize = textPaint.TextSize * 40 / cnt;
                var shaper = new SKShaper(SKTypeface.FromFamilyName("Courier New"));

                canvas.DrawShapedText(shaper, reference, 5, 215, textPaint);
            }

            var width = (chunk + 1) * WIDTH * zoom > refer.Length * zoom ? refer.Length * zoom % WIDTH : WIDTH;
            SKPixmap pixmap = surface.Snapshot().Subset(SKRectI.Create(chunk * WIDTH, 0, width, canvas.DeviceClipBounds.Height)).PeekPixels();

            var options = new SKWebpEncoderOptions(SKWebpEncoderCompression.Lossless, 100);
            return pixmap.Encode(options);
        }

        private void LoadReference(string referenceName)
        {
            using var reader = new StreamReader(_service._referencePath);
            string line;
            while (!reader.ReadLine().StartsWith($">{referenceName}"))
            { }
            var text = new StringBuilder();
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith('>'))
                {
                    break;
                }
                text.Append(line.Replace("\n", ""));
            }
            reference = text.ToString();
        }

        public byte[] GetScaledHistogram(string referenceName, int screenSize)
        {
            var refer = _service._references.FirstOrDefault(r => r.Name == referenceName);

            using var surface = SKSurface.Create(new SKImageInfo(screenSize, 100));
            using SKCanvas canvas = surface.Canvas;

            canvas.Clear();

            using SKPaint paint = new SKPaint
            {
                Style = SKPaintStyle.StrokeAndFill,
                Color = SKColors.Blue,
                StrokeWidth = 1
            };

            using SKPaint linePaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = SKColors.Black,
                StrokeWidth = 1
            };

            var histData = _service._histograms[refer.Name];

            var scale = (float)(50 / histData.Average());
            var scaled = refer.Length / screenSize;

            for (int i = 0; i < screenSize; i++)
            {
                var average = (float)histData.Skip(i * scaled).Take(scaled).Average();
                canvas.DrawRect(i + 5, 100 - (average * scale), 1, average * scale, paint);
            }

            canvas.DrawLine(5, 99, screenSize, 99, linePaint);

            return surface.Snapshot().Encode().ToArray();
        }
    }
}