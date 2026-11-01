using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests
{
    [STATestClass]
    public class TestState : WinFormsTestFixture
    {
        private ObjectListViewBase CreateList(int kind) {
            ObjectListViewBase list = kind == 1 ? (ObjectListViewBase)new FastObjectListView() :
                kind == 2 ? new TreeListView() : new ObjectListView();
            list.View = View.Details;
            list.ShowGroups = false;
            list.AllColumns.Add(new OLVColumn("Name", "Name") { Width = 123 });
            list.AllColumns.Add(new OLVColumn("Occupation", "Occupation") { Width = 87, IsVisible = false });
            list.AllColumns.Add(new OLVColumn("Rating", "CulinaryRating") { Width = 65 });
            list.RebuildColumns();
            mainForm.Controls.Add(list);
            list.CreateControl();
            return list;
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        public void Test_LayoutRoundTrip_PreservesHiddenColumnsAndSorting(int kind) {
            using (var list = CreateList(kind)) {
                list.PrimarySortColumn = list.AllColumns[1];
                list.PrimarySortOrder = SortOrder.Descending;
                list.AllColumns[2].DisplayIndex = 0;
                Application.DoEvents();
                byte[] saved = list.SaveState();
                list.AllColumns[0].Width = 201;
                list.AllColumns[1].IsVisible = true;
                list.PrimarySortColumn = null;
                list.PrimarySortOrder = SortOrder.None;
                list.RebuildColumns();

                Assert.IsTrue(list.RestoreState(saved));
                Assert.AreEqual(123, list.AllColumns[0].Width);
                Assert.AreEqual(87, list.AllColumns[1].Width);
                Assert.IsFalse(list.AllColumns[1].IsVisible);
                Assert.AreEqual(0, list.AllColumns[2].DisplayIndex);
                Assert.AreSame(list.AllColumns[1], list.PrimarySortColumn);
                Assert.AreEqual(SortOrder.Descending, list.PrimarySortOrder);
            }
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        public void Test_LayoutRoundTrip_AfterRemovingColumnBeforeHiddenColumn(int kind) {
            using (var list = CreateList(kind)) {
                foreach (OLVColumn column in list.AllColumns)
                    column.IsVisible = true;
                list.RebuildColumns();
                list.SaveState();
                OLVColumn hidden = list.AllColumns[2];
                hidden.IsVisible = false;
                list.RebuildColumns();
                list.AllColumns.RemoveAt(1);
                list.RebuildColumns();
                Assert.AreEqual(2, hidden.LastDisplayIndex);

                byte[] saved = list.SaveState();
                hidden.LastDisplayIndex = -1;
                hidden.Width = 201;
                Assert.IsTrue(list.RestoreState(saved));
                Assert.AreEqual(2, hidden.LastDisplayIndex);
                Assert.AreEqual(65, hidden.Width);
                Assert.IsFalse(hidden.IsVisible);

                hidden.IsVisible = true;
                list.RebuildColumns();
                Assert.AreEqual(1, hidden.DisplayIndex);
                Assert.AreEqual(0, list.AllColumns[0].DisplayIndex);
            }
        }

        [TestMethod]
        public void Test_LayoutRememberedOrderingKeys_AllowLargeKeysAndSentinels() {
            using (var list = CreateList(0))
            using (var menu = new ContextMenuStrip()) {
                byte[] saved = list.SaveState();
                Buffer.BlockCopy(BitConverter.GetBytes(int.MaxValue), 0, saved, 29, 4);
                Assert.IsTrue(list.RestoreState(saved));
                Assert.AreEqual(1, list.AllColumns[0].DisplayIndex);
                Assert.AreEqual(0, list.AllColumns[2].DisplayIndex);

                list.SelectColumnsOnRightClickBehaviour = ObjectListViewBase.ColumnSelectBehaviour.InlineMenu;
                list.MakeColumnSelectMenu(menu);
                CollectionAssert.AreEqual(
                    new[] { list.AllColumns[2], list.AllColumns[0], list.AllColumns[1] },
                    menu.Items.Cast<ToolStripMenuItem>().Select(item => (OLVColumn)item.Tag).ToArray());
                Assert.AreEqual(-1, list.AllColumns[1].LastDisplayIndex);
            }
        }

        [TestMethod]
        [DataRow(0, false, 1)]
        [DataRow(1, false, 2)]
        [DataRow(2, false, 4)]
        [DataRow(0, true, 4)]
        [DataRow(1, true, 1)]
        [DataRow(2, true, 2)]
        public void Test_ColumnSelectMenu_NeverShownColumnsMatchEnabledOrder(int kind, bool submenu, int enabledIndex) {
            using (var list = CreateList(kind))
            using (var menu = new ContextMenuStrip()) {
                list.AllColumns.Insert(2, new OLVColumn("Second hidden", "Name") { IsVisible = false });
                list.AllColumns.Add(new OLVColumn("Last hidden", "Name") { IsVisible = false });
                list.SelectColumnsOnRightClickBehaviour = submenu ?
                    ObjectListViewBase.ColumnSelectBehaviour.Submenu :
                    ObjectListViewBase.ColumnSelectBehaviour.InlineMenu;

                list.MakeColumnSelectMenu(menu);
                ToolStripItemCollection items = submenu ?
                    ((ToolStripMenuItem)menu.Items[0]).DropDownItems : menu.Items;
                OLVColumn[] menuColumns = items.Cast<ToolStripMenuItem>()
                    .Select(item => (OLVColumn)item.Tag).ToArray();
                CollectionAssert.AreEqual(list.AllColumns.ToArray(), menuColumns);
                foreach (OLVColumn column in list.AllColumns.Where(column => !column.IsVisible))
                    Assert.AreEqual(-1, column.LastDisplayIndex);

                list.AllColumns[enabledIndex].IsVisible = true;
                list.RebuildColumns();
                CollectionAssert.AreEqual(menuColumns.Where(column => column.IsVisible).ToArray(),
                    list.ColumnsInDisplayOrder.ToArray());
            }
        }

        [TestMethod]
        [DataRow(0, false)]
        [DataRow(1, false)]
        [DataRow(2, false)]
        [DataRow(0, true)]
        [DataRow(1, true)]
        [DataRow(2, true)]
        public void Test_ColumnSelectMenu_UsesCurrentOrderAndPreservesHiddenPosition(int kind, bool submenu) {
            using (var list = CreateList(kind))
            using (var menu = new ContextMenuStrip()) {
                OLVColumn hidden = list.AllColumns[1];
                hidden.LastDisplayIndex = 2;
                list.AllColumns[2].DisplayIndex = 0;
                list.SelectColumnsOnRightClickBehaviour = submenu ?
                    ObjectListViewBase.ColumnSelectBehaviour.Submenu :
                    ObjectListViewBase.ColumnSelectBehaviour.InlineMenu;

                list.MakeColumnSelectMenu(menu);
                ToolStripItemCollection items = submenu ?
                    ((ToolStripMenuItem)menu.Items[0]).DropDownItems : menu.Items;
                CollectionAssert.AreEqual(
                    new[] { list.AllColumns[2], list.AllColumns[0], hidden },
                    items.Cast<ToolStripMenuItem>().Select(item => (OLVColumn)item.Tag).ToArray());
                Assert.AreEqual(2, hidden.LastDisplayIndex);
                Assert.IsFalse(((ToolStripMenuItem)items[2]).Checked);

                hidden.IsVisible = true;
                list.RebuildColumns();
                Assert.AreEqual(0, list.AllColumns[2].DisplayIndex);
                Assert.AreEqual(1, list.AllColumns[0].DisplayIndex);
                Assert.AreEqual(2, hidden.DisplayIndex);
            }
        }

        [TestMethod]
        public void Test_ColumnSelectMenu_TiedPositionsPreserveAllColumnsOrder() {
            using (var list = CreateList(0))
            using (var menu = new ContextMenuStrip()) {
                list.SaveState();
                list.AllColumns[1].LastDisplayIndex = 0;
                for (int i = 0; i < 20; i++)
                    list.AllColumns.Add(new OLVColumn("Hidden " + i, "Name") {
                        IsVisible = false, LastDisplayIndex = 0
                    });
                OLVColumn[] expected = list.AllColumns.Where(column => column != list.AllColumns[2])
                    .Concat(new[] { list.AllColumns[2] }).ToArray();

                list.MakeColumnSelectMenu(menu);
                CollectionAssert.AreEqual(
                    expected,
                    menu.Items.Cast<ToolStripMenuItem>().Select(item => (OLVColumn)item.Tag).ToArray());
                Assert.AreEqual(0, list.AllColumns[1].LastDisplayIndex);

                foreach (OLVColumn column in list.AllColumns)
                    column.IsVisible = true;
                list.RebuildColumns();
                CollectionAssert.AreEqual(expected,
                    list.Columns.Cast<OLVColumn>().OrderBy(column => column.DisplayIndex).ToArray());
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_ColumnOrderCapture_PreservesHiddenPosition(bool recreateHandle) {
            using (var list = new ColumnOrderObjectListView { View = View.Details, ShowGroups = false }) {
                list.AllColumns.Add(new OLVColumn("Name", "Name"));
                list.AllColumns.Add(new OLVColumn("Rating", "CulinaryRating"));
                list.AllColumns.Add(new OLVColumn("Occupation", "Occupation"));
                list.RebuildColumns();
                mainForm.Controls.Add(list);
                list.CreateControl();
                OLVColumn hidden = list.AllColumns[2];
                hidden.IsVisible = false;
                list.RebuildColumns();
                list.AllColumns[1].DisplayIndex = 0;

                if (recreateHandle)
                    list.RecreateNativeHandle();
                else {
                    list.NotifyColumnReordered();
                    Application.DoEvents();
                }

                Assert.AreEqual(2, hidden.LastDisplayIndex);
                Assert.AreEqual(0, list.AllColumns[1].LastDisplayIndex);
                Assert.AreEqual(1, list.AllColumns[0].LastDisplayIndex);
                hidden.IsVisible = true;
                list.RebuildColumns();
                Assert.AreEqual(2, hidden.DisplayIndex);
                Assert.AreEqual(0, list.AllColumns[1].DisplayIndex);
                Assert.AreEqual(1, list.AllColumns[0].DisplayIndex);
            }
        }

        private sealed class ColumnOrderObjectListView : ObjectListView
        {
            public void RecreateNativeHandle() { this.RecreateHandle(); }

            public void NotifyColumnReordered() {
                this.OnColumnReordered(new ColumnReorderedEventArgs(1, 0, this.AllColumns[1]));
            }
        }

        [TestMethod]
        public void Test_LayoutRoundTrip_ViewGroupsAndEmptyColumns() {
            using (var list = new ObjectListView()) {
                list.View = View.SmallIcon;
                list.ShowGroups = true;
                byte[] saved = list.SaveState();
                list.View = View.Details;
                list.ShowGroups = false;
                Assert.IsTrue(list.RestoreState(saved));
                Assert.AreEqual(View.SmallIcon, list.View);
                Assert.IsTrue(list.ShowGroups);
            }
        }

        [TestMethod]
        public void Test_LayoutInvalidPayload_DoesNotMutateControl() {
            using (var list = CreateList(0)) {
                byte[] original = list.SaveState();
                foreach (int length in Enumerable.Range(0, original.Length)) {
                    Assert.IsFalse(list.RestoreState(original.Take(length).ToArray()));
                }
                Assert.IsFalse(list.RestoreState(original.Concat(new byte[] { 0 }).ToArray()));
                foreach (var change in new[] {
                    new[] { 0, 0 }, new[] { 4, 2 }, new[] { 8, 2 }, new[] { 12, 5 },
                    new[] { 16, 3 }, new[] { 20, 3 }, new[] { 24, 2 }, new[] { 33, 2 }
                }) {
                    byte[] invalid = (byte[])original.Clone();
                    invalid[change[0]] = (byte)change[1];
                    Assert.IsFalse(list.RestoreState(invalid), "Offset " + change[0]);
                    CollectionAssert.AreEqual(original, list.SaveState());
                }
                byte[] badLastColumn = (byte[])original.Clone();
                Buffer.BlockCopy(BitConverter.GetBytes(-3), 0, badLastColumn, 43, 4);
                Assert.IsFalse(list.RestoreState(badLastColumn));
                CollectionAssert.AreEqual(original, list.SaveState());
                Assert.ThrowsExactly<ArgumentNullException>(() => list.RestoreState(null));
            }
        }

        [TestMethod]
        public void Test_LayoutRejectsLegacyBinaryFormatterAndColumnMismatch() {
            using (var list = CreateList(0)) {
                byte[] saved = list.SaveState();
                byte[] legacy = new byte[saved.Length];
                Buffer.BlockCopy(new byte[] { 0, 1, 0, 0, 0, 255, 255, 255, 255 }, 0, legacy, 0, 9);
                Assert.IsFalse(list.RestoreState(legacy));
                list.AllColumns.Add(new OLVColumn("Extra", "Extra"));
                Assert.IsFalse(list.RestoreState(saved));
            }
        }

        [TestMethod]
        public void Test_LayoutCanonicalFixture_IsPortableAcrossTargets() {
            // A schema-1 record independent of assembly names and runtime serialization.
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            using (var list = CreateList(0)) {
                writer.Write(new byte[] { 0x4F, 0x4C, 0x56, 0x53 });
                writer.Write(1);
                writer.Write(3);
                writer.Write((int)View.Details);
                writer.Write(1);
                writer.Write((int)SortOrder.Ascending);
                writer.Write((byte)0);
                foreach (int index in Enumerable.Range(0, 3)) {
                    writer.Write(100 + index);
                    writer.Write(index == 1 ? -1 : 0);
                    writer.Write((byte)(index == 1 ? 0 : 1));
                }
                Assert.IsTrue(list.RestoreState(stream.ToArray()));
                Assert.AreEqual(101, list.AllColumns[1].Width);
                Assert.IsFalse(list.AllColumns[1].IsVisible);
                Assert.AreSame(list.AllColumns[1], list.PrimarySortColumn);
                Assert.AreEqual(SortOrder.Ascending, list.PrimarySortOrder);
            }
        }
    }
}
