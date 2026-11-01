using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using BrightIdeasSoftware.Design;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests
{
    [STATestClass]
    public class TestDesignerColumnResize
    {
        [TestMethod]
        public void Test_ResizePreview_CommitsOneBoundedChange() {
            using (var column = new OLVColumn { Width = 160, MaximumWidth = 220 }) {
                var changes = new Changes();
                var transaction = new Transaction();
                using (var resize = new ColumnResizeTransaction(column, changes, () => transaction)) {
                    resize.Preview(200);
                    resize.Preview(240);
                    Assert.AreEqual(0, changes.Changed);
                    resize.Complete(true);
                }
                Assert.AreEqual(220, column.Width);
                Assert.AreEqual(1, changes.Changing);
                Assert.AreEqual(1, changes.Changed);
                Assert.AreEqual(160, changes.OldValue);
                Assert.AreEqual(220, changes.NewValue);
                Assert.IsTrue(transaction.Committed);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_ResizeCancelOrCaptureLoss_RestoresOriginalWidth(bool explicitCancel) {
            using (var column = new OLVColumn { Width = 160 }) {
                var changes = new Changes();
                var transaction = new Transaction();
                using (var resize = new ColumnResizeTransaction(column, changes, () => transaction)) {
                    resize.Preview(240);
                    if (explicitCancel)
                    {
                        resize.Complete(false);
                    }
                }
                Assert.AreEqual(160, column.Width);
                Assert.AreEqual(0, changes.Changed);
                Assert.IsTrue(transaction.Canceled);
            }
        }

        [TestMethod]
        public void Test_ResizeNotificationFailure_RollsBackAndPropagates() {
            using (var column = new OLVColumn { Width = 160 }) {
                var changes = new Changes { RejectChange = true };
                var transaction = new Transaction();
                using (var resize = new ColumnResizeTransaction(column, changes, () => transaction)) {
                    resize.Preview(240);
                    Assert.ThrowsExactly<InvalidOperationException>(() => resize.Complete(true));
                }
                Assert.AreEqual(160, column.Width);
                Assert.IsTrue(transaction.Canceled);
                Assert.IsFalse(transaction.Committed);
            }
        }

        private sealed class Transaction : DesignerTransaction
        {
            protected override void OnCommit() { }
            protected override void OnCancel() { }
        }

        private sealed class Changes : IComponentChangeService
        {
            public int Changing;
            public int Changed;
            public object OldValue;
            public object NewValue;
            public bool RejectChange;
            public void OnComponentChanging(object component, MemberDescriptor member) { this.Changing++; }
            public void OnComponentChanged(object component, MemberDescriptor member, object oldValue, object newValue) {
                if (this.RejectChange)
                {
                    throw new InvalidOperationException("Change rejected.");
                }

                this.Changed++;
                this.OldValue = oldValue;
                this.NewValue = newValue;
            }
            public event ComponentEventHandler ComponentAdded { add { } remove { } }
            public event ComponentEventHandler ComponentAdding { add { } remove { } }
            public event ComponentChangedEventHandler ComponentChanged { add { } remove { } }
            public event ComponentChangingEventHandler ComponentChanging { add { } remove { } }
            public event ComponentEventHandler ComponentRemoved { add { } remove { } }
            public event ComponentEventHandler ComponentRemoving { add { } remove { } }
            public event ComponentRenameEventHandler ComponentRename { add { } remove { } }
        }
    }
}
