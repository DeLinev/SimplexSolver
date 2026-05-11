using System;
using System.Collections.Generic;
using System.Text;

namespace SimplexMethodApp.Controls
{
    public class CurlyBraceView : GraphicsView, IDrawable
    {
        public static readonly BindableProperty BraceColorProperty =
        BindableProperty.Create(
            nameof(BraceColor),
            typeof(Color),
            typeof(CurlyBraceView),
            Colors.CornflowerBlue,
            propertyChanged: (b, _, _) => ((CurlyBraceView)b).Invalidate());

        public static readonly BindableProperty StrokeWidthProperty =
            BindableProperty.Create(
                nameof(StrokeWidth),
                typeof(float),
                typeof(CurlyBraceView),
                2.5f,
                propertyChanged: (b, _, _) => ((CurlyBraceView)b).Invalidate());

        public Color BraceColor
        {
            get => (Color)GetValue(BraceColorProperty);
            set => SetValue(BraceColorProperty, value);
        }

        public float StrokeWidth
        {
            get => (float)GetValue(StrokeWidthProperty);
            set => SetValue(StrokeWidthProperty, value);
        }

        public CurlyBraceView()
        {
            Drawable = this;
        }

        public void Draw(ICanvas canvas, RectF r)
        {
            if (r.Width <= 0 || r.Height <= 0)
                return;

            canvas.Antialias = true;
            canvas.StrokeColor = BraceColor;
            canvas.StrokeSize = StrokeWidth;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.StrokeLineJoin = LineJoin.Round;

            float w = r.Width;
            float h = r.Height;
            float radius = Math.Min(w * 0.8f, h * 0.15f);
            float rightX = w * 0.85f;
            float midX = w * 0.5f;
            float leftX = w * 0.1f;
            float midY = h * 0.5f;

            var path = new PathF();

            path.MoveTo(rightX, 0);
            path.CurveTo(
                rightX - radius * 0.5f, 0,
                midX, radius * 0.5f,
                midX, radius
            );

            path.LineTo(midX, midY - radius);
            path.CurveTo(
                midX, midY - radius * 0.2f,
                leftX, midY - radius * 0.2f,
                leftX, midY
            );

            path.CurveTo(
                leftX, midY + radius * 0.2f,
                midX, midY + radius * 0.2f,
                midX, midY + radius
            );

            path.LineTo(midX, h - radius);
            path.CurveTo(
                midX, h - radius * 0.5f,
                rightX - radius * 0.5f, h,
                rightX, h
            );

            canvas.DrawPath(path);
        }
    }
}
