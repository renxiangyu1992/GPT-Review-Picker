using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

var output = args[0];
using var image = new Bitmap(96, 96);
using var graphics = Graphics.FromImage(image);
graphics.SmoothingMode = SmoothingMode.AntiAlias;
graphics.Clear(Color.FromArgb(244, 247, 250));
using var folder = new SolidBrush(Color.FromArgb(58, 122, 196));
using var paper = new SolidBrush(Color.White);
using var outline = new Pen(Color.FromArgb(45, 55, 65), 3);
using var check = new Pen(Color.FromArgb(36, 153, 94), 7) { StartCap = LineCap.Round, EndCap = LineCap.Round };
graphics.FillRectangle(folder, 13, 29, 70, 51);
graphics.FillRectangle(folder, 19, 20, 28, 15);
graphics.FillRectangle(paper, 28, 13, 49, 61);
graphics.DrawRectangle(outline, 28, 13, 49, 61);
graphics.DrawLines(check, new Point[] { new(39, 47), new(49, 57), new(67, 35) });
image.Save(output, ImageFormat.Png);
