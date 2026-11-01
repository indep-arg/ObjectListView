using System;
using System.ComponentModel;
using System.ComponentModel.Design;

namespace BrightIdeasSoftware.Design
{
    internal sealed class ColumnResizeTransaction : IDisposable
    {
        private readonly OLVColumn column;
        private readonly IComponentChangeService changes;
        private readonly PropertyDescriptor width;
        private readonly int originalWidth;
        private DesignerTransaction transaction;

        public ColumnResizeTransaction(OLVColumn column, IComponentChangeService changes,
            Func<DesignerTransaction> createTransaction) {
            this.column = column;
            this.changes = changes;
            this.width = TypeDescriptor.GetProperties(column)["Width"];
            this.originalWidth = column.Width;
            this.transaction = createTransaction();
            try {
                this.changes.OnComponentChanging(column, this.width);
            } catch {
                this.Complete(false);
                throw;
            }
        }

        public void Preview(int value) {
            this.column.Width = value;
        }

        public void Complete(bool accept) {
            DesignerTransaction current = this.transaction;
            if (current == null)
            {
                return;
            }

            this.transaction = null;
            try {
                if (accept && this.column.Width != this.originalWidth) {
                    this.changes.OnComponentChanged(this.column, this.width, this.originalWidth, this.column.Width);
                    current.Commit();
                } else {
                    this.column.Width = this.originalWidth;
                    current.Cancel();
                }
            } catch {
                this.column.Width = this.originalWidth;
                current.Cancel();
                throw;
            } finally {
                ((IDisposable)current).Dispose();
            }
        }

        public void Dispose() {
            this.Complete(false);
        }
    }
}
