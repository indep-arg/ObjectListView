using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using BrightIdeasSoftware;
using ObjectListViewDemo.Models;

namespace ObjectListViewDemo {
    /// <summary>
    /// Hackish renderer that draw a fancy version of a person for a Tile view.
    /// </summary>
    /// <remarks>This is not the way to write a professional level renderer.
    /// It is hideously inefficient (we should at least cache the images),
    /// but it is obvious</remarks>
    public class BusinessCardRenderer : AbstractRenderer
    {
        private const int Spacing = 8;
        private const int BorderInset = 2;
        private const float TitleFontSize = 11;
        private const float DetailFontSize = 8;

        internal static Size ScaleSize(Graphics graphics, Size size) {
            return Size.Ceiling(new SizeF(size.Width * graphics.DpiX / 96f, size.Height * graphics.DpiY / 96f));
        }

        internal static Size MeasureCardSize(Graphics graphics, ObjectListViewBase list, int width) {
            Size size = ScaleSize(graphics, new Size(width, 120));
            Size spacing = ScaleSize(graphics, new Size(Spacing, Spacing));
            Size inset = ScaleSize(graphics, new Size(BorderInset, BorderInset));
            int rows = 0;
            foreach (OLVColumn column in list.Columns) {
                if (column.IsTileViewColumn)
                {
                    rows++;
                }
            }
            using (var titleFont = new Font("Tahoma", TitleFontSize))
            using (var detailFont = new Font("Tahoma", DetailFontSize))
            using (var format = new StringFormat(StringFormatFlags.NoWrap)) {
                float titleHeight = graphics.MeasureString("Wj", titleFont, size.Width, format).Height;
                float rowHeight = graphics.MeasureString("Wj", detailFont, size.Width, format).Height;
                int contentHeight = (int)Math.Ceiling(inset.Height * 2 + spacing.Height * 3 +
                    titleHeight + rows * rowHeight);
                return new Size(size.Width, Math.Max(size.Height, contentHeight));
            }
        }

        public override bool RenderItem(DrawListViewItemEventArgs e, Graphics g, Rectangle itemBounds, object rowObject)
        {
            // If we're in any other view than Tile, return false to say that we haven't done
            // the rendering and the default process should do it's stuff
            ObjectListViewBase olv = e.Item.ListView as ObjectListViewBase;
            if (olv == null || olv.View != View.Tile)
            {
                return false;
            }

            // Use buffered graphics to kill flickers
            using (BufferedGraphics buffered = BufferedGraphicsManager.Current.Allocate(g, itemBounds)) {
                Graphics graphics = buffered.Graphics;
                graphics.Clear(olv.BackColor);
                graphics.SmoothingMode = ObjectListView.SmoothingMode;
                graphics.TextRenderingHint = ObjectListView.TextRenderingHint;
                Color borderColor = e.Item.Selected ? Color.Blue : this.BorderColor;
                Color headerBackColor = e.Item.Selected ? olv.SelectedBackColorOrDefault : this.HeaderBackColor;
                this.DrawBusinessCard(graphics, itemBounds, rowObject, olv, (OLVListItem)e.Item, borderColor, headerBackColor);
                buffered.Render();
            }

            // Return true to say that we've handled the drawing
            return true;
        }

        internal Color BorderColor = Color.FromArgb(0x33, 0x33, 0x33);
        internal float BorderWidth = 1;
        internal Color TextColor = DemoTheme.IsDarkMode ? ObjectListViewTheme.TextColor : Color.FromArgb(0x22, 0x22, 0x22);
        internal Color HeaderTextColor = Color.AliceBlue;
        internal Color HeaderBackColor = DemoTheme.IsDarkMode ? ObjectListViewTheme.ControlLightColor : Color.FromArgb(0x33, 0x33, 0x33);
        internal Color BackColor = DemoTheme.IsDarkMode ? ObjectListViewTheme.ControlColor : Color.LemonChiffon;

        public void DrawBusinessCard(Graphics g, Rectangle itemBounds, object rowObject, ObjectListViewBase olv, OLVListItem item)
        {
            this.DrawBusinessCard(g, itemBounds, rowObject, olv, item, this.BorderColor, this.HeaderBackColor);
        }

        private void DrawBusinessCard(Graphics g, Rectangle itemBounds, object rowObject, ObjectListViewBase olv,
            OLVListItem item, Color borderColor, Color headerBackColor)
        {
            GraphicsState state = g.Save();
            try {
                using (var borderPen = new Pen(borderColor, this.BorderWidth))
                using (var backBrush = new SolidBrush(this.BackColor))
                using (var textBrush = new SolidBrush(this.TextColor))
                using (var headerTextBrush = new SolidBrush(this.HeaderTextColor))
                using (var headerBackBrush = new SolidBrush(headerBackColor))
                using (var format = new StringFormat(StringFormatFlags.NoWrap) {
                    Trimming = StringTrimming.EllipsisCharacter,
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Near
                }) {
                    Size spacing = ScaleSize(g, new Size(Spacing, Spacing));
                    Size inset = ScaleSize(g, new Size(BorderInset, BorderInset));
                    itemBounds.Inflate(-inset.Width, -inset.Height);
                    using (GraphicsPath path = this.GetRoundedRect(itemBounds, ScaleSize(g, new Size(20, 20)).Width)) {
                        g.FillPath(backBrush, path);
                        g.DrawPath(borderPen, path);
                    }
                    g.SetClip(itemBounds, CombineMode.Intersect);

                    Rectangle photoRect = itemBounds;
                    photoRect.Inflate(-spacing.Width, -spacing.Height);
                    Person person = rowObject as Person;
                    if (person != null) {
                        photoRect.Width = ScaleSize(g, new Size(80, 0)).Width;
                        string photoFile = String.Format(@".\Photos\{0}.png", person.Photo);
                        if (File.Exists(photoFile)) {
                            using (Image photo = Image.FromFile(photoFile)) {
                                photoRect.Height = (int)Math.Round(photo.Height * ((float)photoRect.Width / photo.Width));
                                g.DrawImage(photo, photoRect);
                            }
                        } else {
                            g.DrawRectangle(Pens.DarkGray, photoRect);
                        }
                    }

                    RectangleF textBounds = photoRect;
                    textBounds.X += photoRect.Width + spacing.Width;
                    textBounds.Width = itemBounds.Right - textBounds.X - spacing.Width;
                    using (Font font = new Font("Tahoma", TitleFontSize)) {
                        SizeF size = g.MeasureString(item.Text, font, (int)textBounds.Width, format);
                        RectangleF titleBounds = textBounds;
                        titleBounds.Height = size.Height;
                        using (GraphicsPath path = this.GetRoundedRect(titleBounds, ScaleSize(g, new Size(15, 15)).Width))
                        {
                            g.FillPath(headerBackBrush, path);
                        }

                        g.DrawString(item.Text, font, headerTextBrush, textBounds, format);
                        textBounds.Y += size.Height + spacing.Height;
                    }

                    using (Font font = new Font("Tahoma", DetailFontSize)) {
                        textBounds.Height = g.MeasureString("Wj", font, itemBounds.Width, format).Height;
                        format.Alignment = StringAlignment.Near;
                        foreach (OLVColumn column in olv.Columns) {
                            if (column.IsTileViewColumn) {
                                g.DrawString(column.GetStringValue(rowObject), font, textBrush, textBounds, format);
                                textBounds.Y += textBounds.Height;
                            }
                        }
                    }
                }
            } finally {
                g.Restore(state);
            }
        }

        private GraphicsPath GetRoundedRect(RectangleF rect, float diameter)
        {
            GraphicsPath path = new GraphicsPath();

            RectangleF arc = new RectangleF(rect.X, rect.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();

            return path;
        }
    }
}