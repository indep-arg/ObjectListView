using System;
using System.ComponentModel.Design;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.DotNet.DesignTools.Designers.Behaviors;

namespace BrightIdeasSoftware.Design
{
    internal sealed class HeaderResizeGlyph : Glyph
    {
        private readonly ObjectListViewBase list;
        private readonly BehaviorService service;
        private readonly ResizeBehavior resize;

        public HeaderResizeGlyph(ObjectListViewBase list, BehaviorService service,
            IDesignerHost host, IComponentChangeService changes)
            : this(list, service, new ResizeBehavior(list, service, host, changes)) { }

        private HeaderResizeGlyph(ObjectListViewBase list, BehaviorService service, ResizeBehavior resize)
            : base(resize) {
            this.list = list;
            this.service = service;
            this.resize = resize;
        }

        public override Rectangle Bounds {
            get {
                if (this.list.IsDisposed || !this.list.IsHandleCreated || !this.list.Visible ||
                    this.list.View != View.Details || this.list.HeaderStyle == ColumnHeaderStyle.None)
                {
                    return Rectangle.Empty;
                }

                Rectangle client = this.list.HeaderControl.ClientRectangle;
                Point first = this.service.PointToAdornerWindow(this.list.HeaderControl.Handle, client.Location);
                Point last = this.service.PointToAdornerWindow(this.list.HeaderControl.Handle,
                    new Point(client.Right, client.Bottom));
                Rectangle header = Rectangle.FromLTRB(Math.Min(first.X, last.X), Math.Min(first.Y, last.Y),
                    Math.Max(first.X, last.X), Math.Max(first.Y, last.Y));
                return Rectangle.Intersect(header, this.service.ControlRectInAdornerWindow(this.list));
            }
        }

        public override Cursor GetHitTest(Point point) {
            if (!this.Bounds.Contains(point))
            {
                return null;
            }

            int radius = Math.Max(3, this.list.DeviceDpi / 32);
            for (int i = 0; i < this.list.Columns.Count; i++) {
                OLVColumn column = this.list.GetColumn(i);
                if (column.IsFixedWidth || column.FillsFreeSpace)
                {
                    continue;
                }

                Rectangle bounds = this.list.HeaderControl.GetItemRect(i);
                Point edge = this.service.PointToAdornerWindow(this.list.HeaderControl.Handle,
                    new Point(bounds.Right, bounds.Top));
                if (Math.Abs((long)point.X - edge.X) <= radius) {
                    this.resize.Column = column;
                    return Cursors.VSplit;
                }
            }
            return null;
        }

        public override void Paint(PaintEventArgs e) { }

        public void Cancel() {
            this.resize.Complete(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "Complete disposes each gesture transaction; capture loss and designer disposal cancel active gestures.")]
        private sealed class ResizeBehavior : Behavior
        {
            private readonly ObjectListViewBase list;
            private readonly BehaviorService service;
            private readonly IDesignerHost host;
            private readonly IComponentChangeService changes;
            private ColumnResizeTransaction transaction;
            private int startWidth;
            private int startX;
            private bool rightToLeft;

            public ResizeBehavior(ObjectListViewBase list, BehaviorService service,
                IDesignerHost host, IComponentChangeService changes) {
                this.list = list;
                this.service = service;
                this.host = host;
                this.changes = changes;
            }

            public OLVColumn Column { get; set; }
            public override Cursor Cursor { get { return Cursors.VSplit; } }
            public override bool DisableAllCommands { get { return this.transaction != null; } }

            public override bool OnMouseDown(Glyph glyph, MouseButtons button, Point point) {
                if (button != MouseButtons.Left || this.Column == null || this.transaction != null)
                {
                    return false;
                }

                this.startWidth = this.Column.Width;
                this.startX = point.X;
                this.rightToLeft = this.list.RightToLeftLayout && this.list.RightToLeft == RightToLeft.Yes;
                this.transaction = new ColumnResizeTransaction(this.Column, this.changes,
                    () => this.host.CreateTransaction("Resize ObjectListView column"));
                try {
                    this.service.PushCaptureBehavior(this);
                } catch {
                    this.Complete(false);
                    throw;
                }
                return true;
            }

            public override bool OnMouseMove(Glyph glyph, MouseButtons button, Point point) {
                if (this.transaction == null)
                {
                    return false;
                }

                long delta = (long)point.X - this.startX;
                long requested = this.startWidth + (this.rightToLeft ? -delta : delta);
                this.transaction.Preview((int)Math.Max(0, Math.Min(int.MaxValue, requested)));
                this.service.Invalidate();
                return true;
            }

            public override bool OnMouseUp(Glyph glyph, MouseButtons button, Point point) {
                if (button != MouseButtons.Left || this.transaction == null)
                {
                    return false;
                }

                this.OnMouseMove(glyph, button, point);
                this.Complete(true);
                return true;
            }

            public override void OnLoseCapture(Glyph glyph, EventArgs e) {
                this.Complete(false);
            }

            public void Complete(bool accept) {
                ColumnResizeTransaction current = this.transaction;
                if (current == null)
                {
                    return;
                }

                this.transaction = null;
                try {
                    current.Complete(accept);
                } finally {
                    try {
                        this.service.PopBehavior(this);
                    } finally {
                        current.Dispose();
                    }
                    this.service.Invalidate();
                }
            }
        }
    }
}
