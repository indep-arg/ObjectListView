#if NETFRAMEWORK
using System;
using System.Drawing;
using System.Windows.Forms;
using BrightIdeasSoftware;

namespace ObjectListViewDemo {
    public class ThemedComboBox : ComboBox {
        private bool hot;

        public ThemedComboBox() {
            this.DropDownStyleChanged += delegate {
                bool managedPainting = ObjectListViewTheme.IsDarkMode && this.DropDownStyle == ComboBoxStyle.DropDownList;
                this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                    ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, managedPainting);
                this.Invalidate();
            };
        }

        protected override void OnPaintBackground(PaintEventArgs e) {
            if (!this.GetStyle(ControlStyles.UserPaint)) {
                base.OnPaintBackground(e);
                return;
            }
            using (Brush brush = new SolidBrush(this.BackColor))
            {
                e.Graphics.FillRectangle(brush, e.ClipRectangle);
            }
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            if (this.GetStyle(ControlStyles.UserPaint))
            {
                this.DrawDarkArrow(e.Graphics);
            }
        }

        protected override void OnMouseEnter(EventArgs e) {
            base.OnMouseEnter(e);
            this.hot = true;
            this.Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e) {
            base.OnMouseLeave(e);
            this.hot = false;
            this.Invalidate();
        }

        protected override void OnSelectedIndexChanged(EventArgs e) {
            base.OnSelectedIndexChanged(e);
            this.Invalidate();
        }

        protected override void OnGotFocus(EventArgs e) {
            base.OnGotFocus(e);
            this.Invalidate();
        }

        protected override void OnLostFocus(EventArgs e) {
            base.OnLostFocus(e);
            this.Invalidate();
        }

        protected override void OnDropDown(EventArgs e) {
            base.OnDropDown(e);
            this.Invalidate();
        }

        protected override void OnDropDownClosed(EventArgs e) {
            base.OnDropDownClosed(e);
            this.Invalidate();
        }

        private void DrawDarkArrow(Graphics graphics) {
            Rectangle bounds = this.ClientRectangle;
            if (bounds.Width < 2 || bounds.Height < 2)
            {
                return;
            }

            int width = Math.Min(bounds.Width - 2, (int)Math.Ceiling(17 * graphics.DpiX / 96f));
            Rectangle arrow = new Rectangle(this.RightToLeft == RightToLeft.Yes ? 1 : bounds.Right - width - 1,
                1, width, bounds.Height - 2);
            Rectangle textBounds = new Rectangle(this.RightToLeft == RightToLeft.Yes ? arrow.Right + 2 : 3,
                1, Math.Max(0, bounds.Width - width - 6), bounds.Height - 2);
            string text = this.SelectedItem == null ? this.Text : this.GetItemText(this.SelectedItem);
            TextRenderer.DrawText(graphics, text, this.Font, textBounds,
                this.Enabled ? this.ForeColor : ObjectListViewTheme.GrayTextColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis |
                (this.RightToLeft == RightToLeft.Yes ? TextFormatFlags.Right | TextFormatFlags.RightToLeft : TextFormatFlags.Left));
            using (Brush brush = new SolidBrush(this.DroppedDown ? ObjectListViewTheme.ControlDarkColor : this.BackColor))
            {
                graphics.FillRectangle(brush, arrow);
            }

            int centerX = arrow.Left + arrow.Width / 2;
            int centerY = arrow.Top + arrow.Height / 2;
            int halfWidth = Math.Max(3, (int)Math.Ceiling(3 * graphics.DpiX / 96f));
            using (Brush brush = new SolidBrush(this.Enabled ? ObjectListViewTheme.TextColor : ObjectListViewTheme.GrayTextColor))
            {
                graphics.FillPolygon(brush, new[] {
                    new Point(centerX - halfWidth, centerY - 2),
                    new Point(centerX + halfWidth, centerY - 2),
                    new Point(centerX, centerY + 2)
                });
            }

            using (Pen pen = new Pen(this.Focused || this.hot || this.DroppedDown ?
                ObjectListViewTheme.AccentColor : ObjectListViewTheme.ControlDarkColor))
            {
                graphics.DrawRectangle(pen, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            }
        }
    }
}
#endif
