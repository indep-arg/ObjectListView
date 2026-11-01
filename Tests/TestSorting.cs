/*
 * [File purpose]
 * Author: Phillip Piper
 * Date: 11/25/2008 11:06 PM
 * 
 * CHANGE LOG:
 * when who what
 * 11/25/2008 JPP  Initial Version
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests
{
    [STATestClass]
    public class TestSorting : WinFormsTestFixture
    {
        [TestInitialize]
        public void InitEachTest()
        {
            this.olv = GetObjectListView();
            this.olv.LastSortColumn = null;
            this.olv.LastSortOrder = SortOrder.None;
            this.olv.SetObjects(PersonDb.All);
        }

        [TestCleanup]
        public void TearDownEachTest()
        {
        }

        [TestMethod]
        public void TestInitialConditions()
        {
        }

        [TestMethod]
        public void TestSecondarySorting()
        {
            this.olv.SecondarySortColumn = this.olv.GetColumn(0);
            this.olv.SecondarySortOrder = SortOrder.Descending;
            this.olv.Sort(this.olv.GetColumn(3), SortOrder.Ascending);
            Assert.AreEqual(PersonDb.LastAlphabeticalName, ((Person)this.olv.GetModelObject(0)).Name);

            this.olv.SecondarySortColumn = this.olv.GetColumn(0);
            this.olv.SecondarySortOrder = SortOrder.Ascending;
            this.olv.Sort(this.olv.GetColumn(3), SortOrder.Ascending);
            Assert.AreEqual(PersonDb.FirstAlphabeticalName, ((Person)this.olv.GetModelObject(0)).Name);
        }

        [TestMethod]
        public void TestSortingByStringColumn()
        {
            this.olv.Sort(this.olv.GetColumn(0), SortOrder.Ascending);
            Assert.AreEqual(PersonDb.FirstAlphabeticalName, ((Person)this.olv.GetModelObject(0)).Name);
            this.olv.Sort(this.olv.GetColumn(0), SortOrder.Descending);
            Assert.AreEqual(PersonDb.LastAlphabeticalName, ((Person)this.olv.GetModelObject(0)).Name);
        }

        [TestMethod]
        public void TestSortingByIntColumn()
        {
            OLVColumn columnToSort = this.olv.GetColumn(2);
            this.olv.Sort(columnToSort, SortOrder.Ascending);
            Assert.AreEqual(PersonDb.All[PersonDb.All.Count - 1].Name, ((Person)this.olv.GetModelObject(0)).Name);
            this.olv.Sort(columnToSort, SortOrder.Descending);
            Assert.AreEqual(PersonDb.All[0].Name, ((Person)this.olv.GetModelObject(0)).Name);
        }

        [TestMethod]
        public void TestNoSorting()
        {
            ArrayList beforeContents = GetContents();

            this.olv.Sort();

            CollectionAssert.AreEqual(beforeContents, GetContents());

            this.olv.LastSortColumn = this.olv.GetColumn(0);
            this.olv.LastSortOrder = SortOrder.Descending;
            this.olv.Sort();

            CollectionAssert.AreNotEqual(beforeContents, GetContents());
        }

        [TestMethod]
        public void TestExplicitNone_DoesNotInvokeCustomSorterOnUpdates() {
            int sortCalls = 0;
            this.olv.PrimarySortColumn = this.olv.GetColumn(0);
            this.olv.PrimarySortOrder = SortOrder.None;
            this.olv.CustomSorter = delegate { sortCalls++; };
            Person extra = new Person("000 extra");

            this.olv.AddObject(extra);
            this.olv.RemoveObject(PersonDb.All[1]);

            Assert.AreEqual(0, sortCalls);
            Assert.AreEqual(SortOrder.None, this.olv.PrimarySortOrder);
            Assert.AreSame(extra, this.olv.GetModelObject(this.olv.GetItemCount() - 1));
        }

        [TestMethod]
        public void TestExplicitNone_DisablesAutomaticResorting() {
            this.olv.Sort(this.olv.GetColumn(0), SortOrder.Ascending);
            this.olv.Sort(this.olv.GetColumn(0), SortOrder.None);
            Person extra = new Person("000 extra");
            this.olv.AddObject(extra);
            Assert.AreSame(extra, this.olv.GetModelObject(this.olv.GetItemCount() - 1));
            Assert.AreEqual(SortOrder.None, this.olv.PrimarySortOrder);
        }

        [TestMethod]
        public void TestSortColumn_DefaultsToAscending() {
            this.olv.Sort(this.olv.GetColumn(0));
            Assert.AreEqual(PersonDb.FirstAlphabeticalName, ((Person)this.olv.GetModelObject(0)).Name);
        }

        [TestMethod]
        public void TestBuildGroups_NoneStillUsesDefaultOrder() {
            this.olv.ShowGroups = true;
            SortOrder groupOrder = SortOrder.None;
            this.olv.BeforeSorting += delegate(object sender, BeforeSortingEventArgs args) {
                groupOrder = args.GroupByOrder;
            };
            this.olv.BuildGroups(this.olv.GetColumn(0), SortOrder.None);
            Assert.AreEqual(SortOrder.Ascending, groupOrder);
        }

        [TestMethod]
        public void TestGrouping_CountsRepeatedNullKeys() {
            Person first = new Person("First");
            Person second = new Person("Second");
            Person third = new Person("Third");
            OLVColumn column = this.olv.GetColumn(0);
            column.GroupKeyGetter = model => ReferenceEquals(model, third) ? "present" : null;
            this.olv.SetObjects(new[] { first, second, third });
            this.olv.ShowGroups = true;
            this.olv.BuildGroups(column, SortOrder.Ascending);

            Assert.AreEqual(2, this.olv.OLVGroups.Count);
            OLVGroup nullGroup = this.olv.OLVGroups.Single(group => group.Key == null);
            OLVGroup presentGroup = this.olv.OLVGroups.Single(group => Equals(group.Key, "present"));
            Assert.AreEqual(2, this.olv.VirtualMode ? nullGroup.VirtualItemCount : nullGroup.Items.Count);
            Assert.AreEqual(1, this.olv.VirtualMode ? presentGroup.VirtualItemCount : presentGroup.Items.Count);
        }

        private ArrayList GetContents()
        {
            ArrayList contents = new ArrayList();
            for (int i = 0; i < this.olv.GetItemCount(); i++)
            {
                contents.Add(this.olv.GetModelObject(i));
            }

            return contents;
        }

        [TestMethod]
        virtual public void TestCustomSorting()
        {
            this.olv.Sort(this.olv.GetColumn(0), SortOrder.Ascending);
            Assert.AreEqual(PersonDb.FirstAlphabeticalName, ((Person)this.olv.GetModelObject(0)).Name);

            try {
                this.olv.CustomSorter = delegate(OLVColumn column, SortOrder order) {
                    this.olv.ListViewItemSorter = new ColumnComparer(new OLVColumn("dummy", "BirthDate"), SortOrder.Descending);
                };
                this.olv.Sort(this.olv.GetColumn(0), SortOrder.Ascending);
                Assert.AreNotEqual(PersonDb.FirstAlphabeticalName, ((Person)this.olv.GetModelObject(0)).Name);
            }
            finally {
                this.olv.CustomSorter = null;
            }
        }

        [TestMethod]
        public void TestAfterSortingEvent()
        {
            try {
                this.olv.AfterSorting += new EventHandler<AfterSortingEventArgs>(olvAfterSorting1);
                this.afterSortingCount = 0;
                this.olv.Sort(this.olv.GetColumn(0), SortOrder.Ascending);
                this.olv.Sort();
                this.olv.Sort(this.olv.GetColumn(0));
            }
            finally {
                this.olv.AfterSorting -= new EventHandler<AfterSortingEventArgs>(olvAfterSorting1);
            }
            Assert.AreEqual(3, this.afterSortingCount);
        }
        int afterSortingCount;

        void olvAfterSorting1(object sender, AfterSortingEventArgs e)
        {
            this.afterSortingCount++;
        }

        [TestMethod]
        public void TestBeforeSortingEvent()
        {
            try {
                this.olv.BeforeSorting += new EventHandler<BeforeSortingEventArgs>(olvBeforeSorting1);
                this.olv.Sort(this.olv.GetColumn(2), SortOrder.Ascending);

                // The BeforeSorting event should have changed the sort to descending by name
                Assert.AreEqual(PersonDb.LastAlphabeticalName, ((Person)this.olv.GetModelObject(0)).Name);
            }
            finally {
                this.olv.BeforeSorting -= new EventHandler<BeforeSortingEventArgs>(olvBeforeSorting1);
            }
        }

        void olvBeforeSorting1(object sender, BeforeSortingEventArgs e)
        {
            Assert.AreEqual(this.olv.GetColumn(2), e.ColumnToSort);
            Assert.AreEqual(SortOrder.Ascending, e.SortOrder);

            e.ColumnToSort = this.olv.GetColumn(0);
            e.SortOrder = SortOrder.Descending;
        }

        [TestMethod]
        public void TestCancelSorting()
        {
            this.olv.Sort(this.olv.GetColumn(0), SortOrder.Descending);
            Assert.AreEqual(PersonDb.LastAlphabeticalName, ((Person)this.olv.GetModelObject(0)).Name);

            try {
                this.olv.BeforeSorting += new EventHandler<BeforeSortingEventArgs>(olvBeforeSorting2);
                this.olv.Sort(this.olv.GetColumn(2), SortOrder.Ascending);

                // The BeforeSorting event should have cancelled the sort so the second Sort() should not have had an effect
                Assert.AreEqual(PersonDb.LastAlphabeticalName, ((Person)this.olv.GetModelObject(0)).Name);
            }
            finally {
                this.olv.BeforeSorting -= new EventHandler<BeforeSortingEventArgs>(olvBeforeSorting2);
            }
        }

        void olvBeforeSorting2(object sender, BeforeSortingEventArgs e)
        {
            Assert.AreEqual(this.olv.GetColumn(2), e.ColumnToSort);
            Assert.AreEqual(SortOrder.Ascending, e.SortOrder);

            e.Canceled = true;
        }

        [TestMethod]
        public void TestPreserveSelection()
        {
            this.olv.SelectedObject = PersonDb.All[0];
            this.olv.Sort(this.olv.GetColumn(2), SortOrder.Ascending);
            Assert.AreEqual(PersonDb.All[0], this.olv.SelectedObject);
        }

        [TestMethod]
        public void TestPreserveSelectionMultiple()
        {
            this.olv.SelectedObjects = PersonDb.All;
            this.olv.Sort(this.olv.GetColumn(1), SortOrder.Ascending);
            Assert.AreEqual(PersonDb.All.Count, this.olv.SelectedObjects.Count);
            foreach (Object x in this.olv.SelectedObjects)
            {
                Assert.Contains(x, PersonDb.All);
            }
        }

        [TestMethod]
        virtual public void TestUnsort() {
            this.olv.Sort(this.olv.GetColumn(0), SortOrder.Ascending);
            Assert.AreEqual(PersonDb.FirstAlphabeticalName, ((Person)this.olv.GetModelObject(0)).Name);
            this.olv.Unsort();
            Assert.IsNull(this.olv.PrimarySortColumn);
            //Assert.AreEqual(SortOrder.None, this.olv.PrimarySortOrder);
            Assert.AreEqual(PersonDb.All[0].Name, ((Person)this.olv.GetModelObject(0)).Name);
        }

        protected virtual ObjectListView GetObjectListView()
        {
            return mainForm.objectListView1;
        }
        protected ObjectListView olv;
    }

    [STATestClass]
    public class TestFastOlvSorting : TestSorting
    {
        [TestMethod]
        override public void TestCustomSorting()
        {
        }
        
        [TestMethod]
        override public void TestUnsort() {
            // FastObjectListViews don't really support Unsort()
        }

        protected override ObjectListView GetObjectListView()
        {
            return mainForm.fastObjectListView1;
        }
    }

    [STATestClass]
    public class TestMultiSorting : WinFormsTestFixture {
        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        public void TestPlainHeaderClickReplacesSortAndTogglesAnySortedColumn(int kind) {
            ObjectListView list = this.CreateList(kind);
            foreach (SortOrder currentOrder in new[] { SortOrder.Ascending, SortOrder.Descending }) {
                foreach (int clickedColumn in new[] { 0, 1, 2 }) {
                    list.Sort(new[] {
                        new OLVSortDescription(list.GetColumn(0), currentOrder),
                        new OLVSortDescription(list.GetColumn(1), currentOrder)
                    });

                    this.ClickHeader(list, clickedColumn, Keys.None);

                    SortOrder expectedOrder = clickedColumn == 2 || currentOrder == SortOrder.Descending ?
                        SortOrder.Ascending : SortOrder.Descending;
                    Assert.AreEqual(1, list.SortDescriptions.Count);
                    Assert.AreSame(list.GetColumn(clickedColumn), list.PrimarySortColumn);
                    Assert.AreEqual(expectedOrder, list.PrimarySortOrder);
                    Assert.IsNull(list.SecondarySortColumn);
                    for (int column = 0; column < list.Columns.Count; column++)
                    {
                        Assert.AreEqual(column == clickedColumn ? 1 : 0, list.GetSortPriority(list.GetColumn(column)));
                    }

                    int[] values = Enumerable.Range(0, list.GetItemCount())
                        .Select(i => ((SortRow)list.GetModelObject(i)).Values[clickedColumn]).ToArray();
                    CollectionAssert.AreEqual(expectedOrder == SortOrder.Ascending ?
                        new[] { 0, 0, 0, 0, 1 } : new[] { 1, 0, 0, 0, 0 }, values);
                }
            }
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        public void TestFourSortFieldsAndChangingDirections(int kind) {
            ObjectListView list = this.CreateList(kind);
            this.SortFourFields(list);
            this.AssertOrder(list, "d,b,a,c,e");
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4 },
                list.Columns.Cast<OLVColumn>().Select(list.GetSortPriority).ToArray());

            list.Sort(list.GetColumn(2), SortOrder.Descending, true);
            this.AssertOrder(list, "d,c,b,a,e");
            Assert.AreEqual(3, list.GetSortPriority(list.GetColumn(2)));
            Assert.AreEqual(4, list.SortDescriptions.Count);

            list.Sort();
            this.AssertOrder(list, "d,c,b,a,e");
            list.AddObject(new SortRow("f", 0, 0, 0, 2));
            this.AssertOrder(list, "d,c,f,b,a,e");

            list.Sort(list.GetColumn(0), SortOrder.Descending, true);
            this.AssertOrder(list, "e,d,c,f,b,a");
            Assert.AreEqual(1, list.GetSortPriority(list.GetColumn(0)));

            this.ClickHeader(list, 3, Keys.None);
            Assert.AreEqual(1, list.SortDescriptions.Count);
            Assert.AreEqual(1, list.GetSortPriority(list.GetColumn(3)));
            Assert.AreEqual(0, list.GetSortPriority(list.GetColumn(0)));
            list.Unsort();
            Assert.AreEqual(0, list.SortDescriptions.Count);
            list.Sort(list.GetColumn(0), SortOrder.Ascending, true);
            Assert.AreEqual(1, list.SortDescriptions.Count);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        public void TestCanceledAndUnsortableFieldsPreserveTheSort(int kind) {
            ObjectListView list = this.CreateList(kind);
            this.SortFourFields(list);
            EventHandler<BeforeSortingEventArgs> cancel = delegate(object sender, BeforeSortingEventArgs args) {
                args.Canceled = true;
            };
            list.BeforeSorting += cancel;
            list.Sort(list.GetColumn(2), SortOrder.Descending, true);
            list.BeforeSorting -= cancel;
            this.AssertOrder(list, "d,b,a,c,e");
            Assert.AreEqual(SortOrder.Ascending, list.SortDescriptions[2].SortOrder);

            list.GetColumn(3).Sortable = false;
            list.Sort(list.GetColumn(3), SortOrder.Descending, false);
            Assert.AreEqual(4, list.SortDescriptions.Count);
            this.AssertOrder(list, "d,b,a,c,e");
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        public void TestSortingEventsCanChangeFourthSortField(int kind) {
            ObjectListView list = this.CreateList(kind);
            this.SortFourFields(list);
            IList<OLVSortDescription> reported = null;
            list.BeforeSorting += delegate(object sender, BeforeSortingEventArgs args) {
                Assert.AreEqual(4, args.SortDescriptions.Count);
                args.SortDescriptions[3] = new OLVSortDescription(list.GetColumn(3), SortOrder.Ascending);
            };
            list.AfterSorting += delegate(object sender, AfterSortingEventArgs args) {
                reported = args.SortDescriptions;
            };
            list.Sort();
            this.AssertOrder(list, "d,a,b,c,e");
            Assert.AreEqual(4, reported.Count);
            Assert.AreEqual(SortOrder.Ascending, reported[3].SortOrder);
            Assert.AreEqual(SortOrder.Ascending, list.SortDescriptions[3].SortOrder);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        public void TestLegacyPropertiesShareTheOrderedSortCollection(int kind) {
            ObjectListView list = this.CreateList(kind);
            list.SecondarySortOrder = SortOrder.Descending;
            list.SecondarySortColumn = list.GetColumn(1);
            list.PrimarySortOrder = SortOrder.Ascending;
            list.PrimarySortColumn = list.GetColumn(0);
            Assert.AreSame(list.GetColumn(1), list.SortDescriptions[1].Column);
            Assert.AreEqual(SortOrder.Descending, list.SortDescriptions[1].SortOrder);
            list.Sort();
            Assert.AreEqual("d", ((SortRow)list.GetModelObject(0)).Name);

            list.Sort(new[] {
                new OLVSortDescription(list.GetColumn(0), SortOrder.Ascending),
                new OLVSortDescription(list.GetColumn(1), SortOrder.Descending),
                new OLVSortDescription(list.GetColumn(2), SortOrder.Ascending),
                new OLVSortDescription(list.GetColumn(3), SortOrder.Descending)
            });
            this.AssertOrder(list, "d,b,a,c,e");
            Assert.AreSame(list.SortDescriptions[0].Column, list.PrimarySortColumn);
            Assert.AreSame(list.SortDescriptions[1].Column, list.SecondarySortColumn);
            Assert.AreEqual(list.SortDescriptions[0].SortOrder, list.PrimarySortOrder);
            Assert.AreEqual(list.SortDescriptions[1].SortOrder, list.SecondarySortOrder);

            list.SecondarySortOrder = SortOrder.Ascending;
            Assert.AreEqual(SortOrder.Ascending, list.SortDescriptions[1].SortOrder);
            list.Sort();
            this.AssertOrder(list, "b,a,c,d,e");
            list.PrimarySortOrder = SortOrder.None;
            list.Sort();
            Assert.AreEqual(0, list.SortDescriptions.Count);
            list.PrimarySortOrder = SortOrder.Descending;
            list.Sort();
            this.AssertOrder(list, "e,b,a,c,d");
            Assert.AreEqual(4, list.SortDescriptions.Count);

            list.PrimarySortColumn = list.GetColumn(2);
            list.Sort();
            Assert.AreEqual(3, list.SortDescriptions.Count);
            CollectionAssert.AreEqual(new[] { list.GetColumn(2), list.GetColumn(1), list.GetColumn(3) },
                list.SortDescriptions.Select(x => x.Column).ToArray());
            list.SecondarySortColumn = null;
            list.Sort();
            Assert.AreEqual(2, list.SortDescriptions.Count);
            Assert.AreSame(list.GetColumn(3), list.SecondarySortColumn);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        public void TestEventCollectionAndLegacyPropertiesStayInSync(int kind) {
            ObjectListView list = this.CreateList(kind);
            IList<OLVSortDescription> reported = null;
            list.BeforeSorting += delegate(object sender, BeforeSortingEventArgs args) {
                args.SortDescriptions.Clear();
                foreach (int index in new[] { 3, 2, 1, 0 })
                {
                    args.SortDescriptions.Add(new OLVSortDescription(list.GetColumn(index), SortOrder.Ascending));
                }

                Assert.AreSame(list.GetColumn(3), args.ColumnToSort);
                Assert.AreSame(list.GetColumn(2), args.SecondaryColumnToSort);
                args.SortOrder = SortOrder.Descending;
                args.SecondarySortOrder = SortOrder.Descending;
                Assert.AreEqual(SortOrder.Descending, args.SortDescriptions[0].SortOrder);
                Assert.AreEqual(SortOrder.Descending, args.SortDescriptions[1].SortOrder);
            };
            list.AfterSorting += delegate(object sender, AfterSortingEventArgs args) {
                reported = args.SortDescriptions;
                Assert.AreSame(reported[0].Column, args.ColumnToSort);
                Assert.AreSame(reported[1].Column, args.SecondaryColumnToSort);
            };
            list.Sort(list.GetColumn(0), SortOrder.Ascending);
            this.AssertOrder(list, "b,c,a,e,d");
            Assert.AreEqual(4, reported.Count);
            Assert.IsTrue(reported.IsReadOnly);
            Assert.AreSame(list.GetColumn(3), list.PrimarySortColumn);
            Assert.AreSame(list.GetColumn(2), list.SecondarySortColumn);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        public void TestPlainClickRemainsSingleColumnOnResort(int kind) {
            ObjectListView list = this.CreateList(kind);
            this.SortFourFields(list);
            this.ClickHeader(list, 2, Keys.None);
            EventHandler<BeforeSortingEventArgs> assertSingleColumn = delegate(object sender, BeforeSortingEventArgs args) {
                Assert.AreEqual(1, args.SortDescriptions.Count);
                Assert.AreSame(list.GetColumn(2), args.SortDescriptions[0].Column);
                Assert.IsNull(args.SecondaryColumnToSort);
            };
            list.BeforeSorting += assertSingleColumn;
            list.Sort();
            list.RebuildColumns();
            list.Sort();
            Assert.AreEqual(1, list.SortDescriptions.Count);
            list.BeforeSorting -= assertSingleColumn;
            list.Sort(new OLVSortDescription[0]);
            Assert.AreEqual(0, list.SortDescriptions.Count);
            Assert.IsNull(list.PrimarySortColumn);
            Assert.IsNull(list.SecondarySortColumn);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(1)]
        public void TestGroupedItemsUseAllSortFields(int kind) {
            ObjectListView list = this.CreateList(kind);
            list.ShowGroups = true;
            this.ClickHeader(list, 1, Keys.None);
            this.ClickHeader(list, 2, Keys.Shift);
            this.ClickHeader(list, 3, Keys.Shift);
            this.ClickHeader(list, 3, Keys.Shift);
            this.ClickHeader(list, 0, Keys.Shift);
            for (int i = 0; i < 2; i++) {
                OLVGroup group = list.OLVGroups[0];
                IEnumerable<SortRow> rows = kind == 0
                    ? group.Items.Cast<OLVListItem>().Select(x => (SortRow)x.RowObject)
                    : group.Contents.Cast<int>().Select(x => (SortRow)((VirtualObjectListView)list).VirtualListDataSource.GetNthObject(x));
                Assert.AreEqual("b,a,e,c", String.Join(",", rows.Select(x => x.Name)));
                list.BuildGroups();
            }
            Assert.AreEqual(4, list.SortDescriptions.Count);
        }

        [TestMethod]
        public void TestTreeChildrenRetainMultiSortWhenExpanded() {
            TreeListView list = (TreeListView)this.CreateList(2);
            this.SortFourFields(list);
            SortRow root = (SortRow)list.GetModelObject(0);
            root.Children.AddRange(new[] { new SortRow("child-a", 0, 0, 0, 0), new SortRow("child-b", 0, 0, 0, 1) });
            list.Expand(root);
            this.AssertOrder(list, "d,child-b,child-a,b,a,c,e");
        }

        [TestMethod]
        [DataRow(true, 1)]
        [DataRow(false, 1)]
        [DataRow(true, 2)]
        [DataRow(false, 2)]
        [DataRow(true, 4)]
        [DataRow(false, 4)]
        [DataRow(true, 10)]
        [DataRow(false, 10)]
        public void TestHeadersRenderPriorityBesideEachSortArrow(bool themes, int fieldCount) {
            ObjectListView list = this.CreateList(0);
            list.HeaderUsesThemes = themes;
            this.SortFourFields(list);
            if (fieldCount < 4) {
                this.ClickHeader(list, 0, Keys.None);
                if (fieldCount == 2)
                {
                    this.ClickHeader(list, 1, Keys.Shift);
                }
            }
            for (int i = 4; i < fieldCount; i++) {
                OLVColumn column = new OLVColumn("Extra field", "") { AspectGetter = x => 0, Width = 110 };
                list.AllColumns.Add(column);
                list.Columns.Add(column);
                this.ClickHeader(list, i, Keys.Shift);
            }
            HeaderControl header = list.HeaderControl;
            MethodInfo draw = typeof(HeaderControl).GetMethod("CustomDrawHeaderCell", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsTrue((bool)typeof(HeaderControl).GetMethod("NeedsCustomDraw",
                BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null).Invoke(header, null));
            using (Bitmap bitmap = new Bitmap(fieldCount * 110, 80))
            using (Font priorityFont = new Font(list.Font.FontFamily, list.Font.Size * 0.8f, list.Font.Style, list.Font.Unit))
            using (Graphics graphics = Graphics.FromImage(bitmap)) {
                for (int i = 0; i < fieldCount; i++) {
                    graphics.Clear(Color.Magenta);
                    list.ShowSortIndicators = false;
                    draw.Invoke(header, new object[] { graphics, i, 0 });
                    using (Bitmap withoutIndicators = (Bitmap)bitmap.Clone()) {
                        list.ShowSortIndicators = true;
                        draw.Invoke(header, new object[] { graphics, i, 0 });
                        string label = (i + 1).ToString();
                        TextFormatFlags flags = TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix;
                        int labelWidth = TextRenderer.MeasureText(graphics, label, priorityFont, Size.Empty, flags).Width;
                        Rectangle bounds = header.GetHeaderDrawRect(i);
                        Rectangle labelBounds = new Rectangle(bounds.Right - labelWidth - 16, bounds.Top, labelWidth, bounds.Height);
                        Rectangle visibleLabelBounds = Rectangle.Intersect(labelBounds, new Rectangle(Point.Empty, bitmap.Size));
                        int inkPixels = 0;
                        for (int x = visibleLabelBounds.Left; x < visibleLabelBounds.Right; x++)
                        {
                            for (int y = visibleLabelBounds.Top; y < visibleLabelBounds.Bottom; y++)
                            {
                                if (bitmap.GetPixel(x, y) != withoutIndicators.GetPixel(x, y))
                                {
                                    inkPixels++;
                                }
                            }
                        }

                        if (fieldCount > 1) {
                            using (Graphics expectedGraphics = Graphics.FromImage(withoutIndicators))
                            {
                                TextRenderer.DrawText(expectedGraphics, label, priorityFont, labelBounds, SystemColors.GrayText,
                                    flags | TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.PreserveGraphicsClipping);
                            }
                        }
                        for (int x = visibleLabelBounds.Left; x < visibleLabelBounds.Right; x++)
                        {
                            for (int y = visibleLabelBounds.Top; y < visibleLabelBounds.Bottom; y++) {
                                Assert.AreEqual(withoutIndicators.GetPixel(x, y), bitmap.GetPixel(x, y));
                            }
                        }

                        if (fieldCount > 1)
                        {
                            Assert.IsGreaterThan(0, inkPixels);
                        }
                        else
                        {
                            Assert.AreEqual(0, inkPixels);
                        }

                        Rectangle arrowBounds = Rectangle.Intersect(
                            new Rectangle(bounds.Right - 16, bounds.Top, 16, bounds.Height),
                            new Rectangle(Point.Empty, bitmap.Size));
                        int arrowPixels = 0;
                        for (int x = arrowBounds.Left; x < arrowBounds.Right; x++)
                        {
                            for (int y = arrowBounds.Top; y < arrowBounds.Bottom; y++)
                            {
                                if (bitmap.GetPixel(x, y) != withoutIndicators.GetPixel(x, y))
                                {
                                    arrowPixels++;
                                }
                            }
                        }

                        Assert.IsGreaterThan(0, arrowPixels);
                    }
                }
            }
        }

        private ObjectListView CreateList(int kind) {
            ObjectListView list;
            if (kind == 2) {
                list = new TreeListView {
                    CanExpandGetter = x => ((SortRow)x).Children.Count > 0,
                    ChildrenGetter = x => ((SortRow)x).Children
                };
            } else {
                list = kind == 3 ? new DataListView { AutoGenerateColumns = false }
                    : kind == 1 ? new FastObjectListView() : new ObjectListView();
            }
            list.View = View.Details;
            list.ShowGroups = false;
            list.Size = new Size(500, 200);
            for (int i = 0; i < 4; i++) {
                int index = i;
                OLVColumn column = new OLVColumn("Field " + i, "") {
                    AspectGetter = x => ((SortRow)x).Values[index],
                    Width = 110
                };
                list.AllColumns.Add(column);
                list.Columns.Add(column);
            }
            mainForm.Controls.Add(list);
            list.CreateControl();
            SortRow[] rows = {
                new SortRow("e", 1, 0, 0, 0), new SortRow("d", 0, 1, 0, 0),
                new SortRow("c", 0, 0, 1, 0), new SortRow("b", 0, 0, 0, 1),
                new SortRow("a", 0, 0, 0, 0)
            };
            if (kind == 2)
            {
                ((TreeListView)list).Roots = rows;
            }
            else if (kind == 3)
            {
                ((DataListView)list).DataSource = new BindingList<SortRow>(rows.ToList());
            }
            else
            {
                list.SetObjects(rows);
            }

            return list;
        }

        private void SortFourFields(ObjectListView list) {
            this.ClickHeader(list, 0, Keys.None);
            this.ClickHeader(list, 1, Keys.Shift);
            this.ClickHeader(list, 1, Keys.Shift);
            this.ClickHeader(list, 2, Keys.Shift);
            this.ClickHeader(list, 3, Keys.Shift);
            this.ClickHeader(list, 3, Keys.Shift);
        }

        private void ClickHeader(ObjectListView list, int columnIndex, Keys modifiers) {
            typeof(ObjectListView).GetMethod("HandleColumnClick", BindingFlags.Instance | BindingFlags.NonPublic,
                null, new[] { typeof(ColumnClickEventArgs), typeof(Keys) }, null)
                .Invoke(list, new object[] { new ColumnClickEventArgs(columnIndex), modifiers });
        }

        private void AssertOrder(ObjectListView list, string expected) {
            Assert.AreEqual(expected, String.Join(",", Enumerable.Range(0, list.GetItemCount())
                .Select(i => ((SortRow)list.GetModelObject(i)).Name)));
        }

        private sealed class SortRow {
            public SortRow(string name, params int[] values) {
                this.Name = name;
                this.Values = values;
            }

            public readonly string Name;
            public readonly int[] Values;
            public readonly List<SortRow> Children = new List<SortRow>();
        }
    }
}
