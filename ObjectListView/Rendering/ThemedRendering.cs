using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace BrightIdeasSoftware {
    internal static class ThemedRendering {
        internal static void DrawCheckBox(Graphics graphics, Point location, CheckBoxState state) {
            if (!ObjectListViewTheme.IsDarkMode) {
                CheckBoxRenderer.DrawCheckBox(graphics, location, state);
                return;
            }

            Size size = CheckBoxRenderer.GetGlyphSize(graphics, state);
            Rectangle bounds = new Rectangle(location, size);
            bool disabled = state == CheckBoxState.UncheckedDisabled ||
                state == CheckBoxState.CheckedDisabled || state == CheckBoxState.MixedDisabled;
            bool selected = state >= CheckBoxState.CheckedNormal;
            bool mixed = state >= CheckBoxState.MixedNormal;
            bool hot = state == CheckBoxState.UncheckedHot || state == CheckBoxState.CheckedHot || state == CheckBoxState.MixedHot;
            bool pressed = state == CheckBoxState.UncheckedPressed || state == CheckBoxState.CheckedPressed || state == CheckBoxState.MixedPressed;
            Color fill = selected && !disabled ? ObjectListViewTheme.AccentColor :
                hot ? ObjectListViewTheme.HoverColor : ObjectListViewTheme.ControlLightColor;
            if (pressed)
            {
                fill = ControlPaint.Dark(fill);
            }

            GraphicsState savedState = graphics.Save();
            try {
                graphics.SmoothingMode = SmoothingMode.None;
                using (Brush brush = new SolidBrush(fill))
                {
                    graphics.FillRectangle(brush, bounds);
                }

                Color borderColor = disabled ? ObjectListViewTheme.ControlLightLightColor :
                    hot ? ObjectListViewTheme.TextColor : ObjectListViewTheme.GrayTextColor;
                int borderWidth = System.Math.Max(1, (int)System.Math.Round(size.Width / 13f));
                using (Pen border = new Pen(borderColor))
                {
                    for (int inset = 0; inset < borderWidth; inset++)
                    {
                        graphics.DrawRectangle(border, bounds.X + inset, bounds.Y + inset,
                            bounds.Width - 1 - inset * 2, bounds.Height - 1 - inset * 2);
                    }
                }

                if (!selected)
                {
                    return;
                }

                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen mark = new Pen(disabled ? ObjectListViewTheme.GrayTextColor : Color.White, System.Math.Max(1.5f, size.Width / 7f))) {
                    mark.StartCap = LineCap.Round;
                    mark.EndCap = LineCap.Round;
                    if (mixed)
                    {
                        graphics.DrawLine(mark, bounds.Left + size.Width * 0.25f, bounds.Top + size.Height * 0.5f,
                            bounds.Left + size.Width * 0.75f, bounds.Top + size.Height * 0.5f);
                    }
                    else
                    {
                        graphics.DrawLines(mark, new[] {
                            new PointF(bounds.Left + size.Width * 0.22f, bounds.Top + size.Height * 0.5f),
                            new PointF(bounds.Left + size.Width * 0.43f, bounds.Top + size.Height * 0.72f),
                            new PointF(bounds.Left + size.Width * 0.78f, bounds.Top + size.Height * 0.28f)
                        });
                    }
                }
            } finally {
                graphics.Restore(savedState);
            }
        }

        internal static void DrawDarkButtonBackground(Graphics graphics, Rectangle bounds, PushButtonState state) {
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            Color fill = state == PushButtonState.Pressed ? ObjectListViewTheme.ButtonPressedColor :
                state == PushButtonState.Hot ? ObjectListViewTheme.ButtonHoverColor :
                ObjectListViewTheme.ButtonColor;
            using (Brush brush = new SolidBrush(fill))
            {
                graphics.FillRectangle(brush, bounds);
            }

            using (Pen border = new Pen(ObjectListViewTheme.GrayTextColor))
            {
                graphics.DrawRectangle(border, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            }
        }
    }
}
