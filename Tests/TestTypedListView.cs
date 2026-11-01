using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests {
    [STATestClass]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "The fixture form owns tolv and disposes its child controls during TestCleanup.")]
    public class TestTypedListView : WinFormsTestFixture {
        [TestMethod]
        public void Test_Objects_All() {
            this.tolv.Objects = PersonDb.All;
            Assert.AreEqual(PersonDb.All.Count, this.tolv.Objects.Count());
        }

        [TestMethod]
        public void Test_Objects_PreserveNullAndExplicitEmptySourcesAcrossStaticTypes() {
            using (var list = new ObjectListView { View = View.Details, ShowGroups = false, CheckBoxes = true }) {
                list.Columns.Add(new OLVColumn("Name", "Name"));
                mainForm.Controls.Add(list);
                list.CreateControl();
                TypedObjectListView<object> typed = list;
                var adapter = new TypedObjectListViewAdapter<Person>(list);

                Assert.IsNull(list.Objects);
                Assert.IsNull(typed.Objects);
                Assert.IsNull(adapter.Objects);
                Assert.IsNull(typed.GetObjectsSnapshot());
                Assert.AreEqual(0, list.Rows.Count);
                adapter.CheckedObjects = null;

                adapter.Objects = Enumerable.Empty<Person>();
                Assert.IsNotNull(list.Objects);
                Assert.IsEmpty(typed.Objects);
                Assert.IsEmpty(adapter.Objects);
                Assert.IsNotNull(typed.GetObjectsSnapshot());
                Assert.IsEmpty(typed.GetObjectsSnapshot());
                ((IList)list.Objects).Add(PersonDb.All[0]);
                Assert.IsEmpty(list.Objects);

                typed.Objects = null;
                Assert.IsNull(list.Objects);
                Assert.IsNull(typed.Objects);
                Assert.IsNull(adapter.Objects);
                Assert.IsNull(typed.GetObjectsSnapshot());

                list.AddObject(PersonDb.All[0]);
                CollectionAssert.AreEqual(PersonDb.All.Take(1).ToArray(), adapter.Objects.ToArray());
                list.RemoveObject(PersonDb.All[0]);
                Assert.IsNotNull(list.Objects);
                Assert.IsEmpty(list.Objects);
            }
        }

        [TestMethod]
        public void Test_TypedObjects_ClearAndEmptySourceSemantics() {
            Assert.IsNull(this.tolv.Objects);
            this.tolv.Objects = Enumerable.Empty<Person>();
            Assert.IsNotNull(this.tolv.Objects);
            Assert.IsEmpty(this.tolv.Objects);
            Assert.IsNotNull(this.tolv.GetObjectsSnapshot());
            Assert.IsEmpty(this.tolv.GetObjectsSnapshot());
            this.tolv.Objects = PersonDb.All.Take(1);
            this.tolv.Objects = null;
            Assert.IsNull(this.tolv.Objects);
            Assert.IsNull(this.tolv.GetObjectsSnapshot());
            Assert.AreEqual(0, this.tolv.Rows.Count);

            this.tolv.ClearObjects();
            Assert.IsNull(this.tolv.Objects);
            Assert.IsNull(this.tolv.GetObjectsSnapshot());
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_Objects_NullStateFollowsAcceptedEventSource(bool replaceWithNull) {
            this.tolv.ItemsChanging += delegate(object sender, ItemsChangingEventArgs args) {
                args.NewObjects = replaceWithNull ? null : Enumerable.Empty<Person>();
            };
            this.tolv.Objects = replaceWithNull ? PersonDb.All : null;
            Assert.AreEqual(replaceWithNull, this.tolv.Objects == null);
            Assert.AreEqual(replaceWithNull, this.tolv.GetObjectsSnapshot() == null);
            if (!replaceWithNull)
            {
                Assert.IsEmpty(this.tolv.GetObjectsSnapshot());
            }

            this.tolv.ItemsChanging += delegate(object sender, ItemsChangingEventArgs args) {
                args.Canceled = true;
            };
            this.tolv.Objects = PersonDb.All;
            Assert.AreEqual(replaceWithNull, this.tolv.Objects == null);
        }

        [TestMethod]
        public void Test_EnumerableCollections_QueriesAndSnapshots() {
            this.tolv.UseNotifyPropertyChanged = true;
            this.tolv.Objects = new SingleUseEnumerable<Person>(PersonDb.All.Where(p => p.CulinaryRating >= 100));
            this.tolv.SetSelectedObjects(new SingleUseEnumerable<Person>(this.tolv.Objects.Take(2)));
            IList<Person> selected = this.tolv.SelectedObjects;
            this.tolv.SelectedObjects = new List<Person>();
            CollectionAssert.AreEqual(PersonDb.All.Take(2).ToArray(), selected.ToArray());

            this.tolv.CheckBoxes = true;
            this.tolv.SetCheckedObjects(new SingleUseEnumerable<Person>(this.tolv.Objects.Take(2)));
            IList<Person> checkedObjects = this.tolv.CheckedObjects;
            this.tolv.CheckedObjects = null;
            Assert.AreEqual(2, checkedObjects.Count);
            Assert.IsEmpty(this.tolv.CheckedObjects);
        }

        [TestMethod]
        public void Test_EnumerableCommands_ModifyTypedStorage() {
            this.tolv.Objects = Enumerable.Empty<Person>();
            this.tolv.AddObjects(new SingleUseEnumerable<Person>(PersonDb.All.Take(2)));
            this.tolv.InsertObjects(1, new SingleUseEnumerable<Person>(PersonDb.All.Skip(2).Take(1)));
            CollectionAssert.AreEqual(new[] { PersonDb.All[0], PersonDb.All[2], PersonDb.All[1] },
                this.tolv.Objects.ToArray());
            this.tolv.RefreshObjects(new SingleUseEnumerable<Person>(PersonDb.All.Take(1)));
            this.tolv.RemoveObjects(new SingleUseEnumerable<Person>(this.tolv.Objects.Where(p => p == PersonDb.All[2])));
            CollectionAssert.AreEqual(PersonDb.All.Take(2).ToArray(), this.tolv.Objects.ToArray());
            Assert.IsInstanceOfType<List<Person>>(this.tolv.Storage);
            CollectionAssert.AreEqual(PersonDb.All.Take(2).ToArray(), this.tolv.Storage);
            this.tolv.CopyObjectsToClipboard(Enumerable.Empty<Person>());
        }

        [TestMethod]
        [DataRow(View.Details, false)]
        [DataRow(View.Details, true)]
        [DataRow(View.List, false)]
        public void Test_InsertRange_PreservesOrderAndNullHandling(View view, bool useFiltering) {
            this.tolv.View = view;
            this.tolv.Objects = new[] { PersonDb.All[0], PersonDb.All[3] };
            this.tolv.UseFiltering = useFiltering;
            if (useFiltering)
            {
                this.tolv.ModelFilter = new ModelFilter(model => model != null && model != PersonDb.All[2]);
            }

            this.tolv.ItemsAdding += delegate(object sender, ItemsAddingEventArgs args) {
                args.ObjectsToAdd = new[] { PersonDb.All[1], null, PersonDb.All[2] };
            };

            this.tolv.InsertObjects(1, new SingleUseEnumerable<Person>(PersonDb.All.Skip(1).Take(2)));

            Person[] expected = view == View.Details && !useFiltering ?
                PersonDb.All.Take(4).ToArray() :
                new[] { PersonDb.All[0], PersonDb.All[1], null, PersonDb.All[2], PersonDb.All[3] };
            CollectionAssert.AreEqual(expected, this.tolv.Storage);
            if (useFiltering)
            {
                CollectionAssert.AreEqual(new[] { PersonDb.All[0], PersonDb.All[1], PersonDb.All[3] },
                    Enumerable.Range(0, this.tolv.GetItemCount()).Select(this.tolv.GetModelObject).ToArray());
            }
            else
            {
                Assert.AreEqual(expected.Length, this.tolv.GetItemCount());
            }
        }

        [TestMethod]
        public void Test_InsertRange_EventCanUseOwnedModelsAsInput() {
            this.tolv.Objects = PersonDb.All.Take(2);
            this.tolv.UseFiltering = true;
            this.tolv.ItemsAdding += delegate(object sender, ItemsAddingEventArgs args) {
                args.ObjectsToAdd = this.tolv.Storage;
            };

            this.tolv.InsertObjects(1, new SingleUseEnumerable<Person>(PersonDb.All.Skip(2).Take(1)));

            CollectionAssert.AreEqual(new[] { PersonDb.All[0], PersonDb.All[0], PersonDb.All[1], PersonDb.All[1] },
                this.tolv.Storage);
        }

        [TestMethod]
        public void Test_SetObjects_DoesNotRetainEventPayloadAsStorage() {
            IList accepted = null;
            this.tolv.ItemsChanging += delegate(object sender, ItemsChangingEventArgs args) {
                accepted = (IList)args.NewObjects;
            };
            this.tolv.Objects = new SingleUseEnumerable<Person>(PersonDb.All.Take(2));
            accepted.Clear();

            CollectionAssert.AreEqual(PersonDb.All.Take(2).ToArray(), this.tolv.Storage);
            Assert.AreEqual(2, this.tolv.GetItemCount());
        }

        [TestMethod]
        public void Test_Objects_OwnsListAndReturnsUnfilteredSnapshots() {
            List<Person> source = PersonDb.All.Take(3).ToList();
            this.tolv.Objects = source;
            source.Clear();
            IList<Person> snapshot = (IList<Person>)this.tolv.Objects;
            snapshot.Clear();
            this.tolv.UseFiltering = true;
            this.tolv.ModelFilter = new ModelFilter(model => model == PersonDb.All[0]);

            Assert.AreEqual(1, this.tolv.GetItemCount());
            CollectionAssert.AreEqual(PersonDb.All.Take(3).ToArray(), this.tolv.Objects.ToArray());
            this.tolv.AddObject(PersonDb.All[3]);
            CollectionAssert.AreEqual(PersonDb.All.Take(4).ToArray(), this.tolv.Storage);
            Assert.IsEmpty(source);
        }

        [TestMethod]
        public void Test_Objects_MaterializesBeforeEventsAndPreservesStateOnFailure() {
            this.tolv.Objects = PersonDb.All.Take(1);
            this.tolv.ItemsChanging += delegate(object sender, ItemsChangingEventArgs args) {
                Assert.AreEqual(2, args.NewObjects.Cast<Person>().Count());
            };
            this.tolv.Objects = new SingleUseEnumerable<Person>(PersonDb.All.Take(2));

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                this.tolv.Objects = EnumerableTestData.FailAfter(PersonDb.All[2]));
            CollectionAssert.AreEqual(PersonDb.All.Take(2).ToArray(), this.tolv.Objects.ToArray());
            Assert.AreEqual(2, this.tolv.GetItemCount());
        }

        [TestMethod]
        public void Test_UntypedMutations_RejectInvalidModelsBeforeChangingRows() {
            this.tolv.Objects = PersonDb.All.Take(2);
            ObjectListViewBase shared = this.tolv;
            object[] invalid = { PersonDb.All[0], "not a person" };

            Assert.ThrowsExactly<InvalidCastException>(() => shared.SetObjects(invalid));
            Assert.ThrowsExactly<InvalidCastException>(() => shared.AddObjects(invalid));
            Assert.ThrowsExactly<InvalidCastException>(() => shared.RemoveObjects(invalid));
            Assert.ThrowsExactly<InvalidCastException>(() => shared.UpdateObjects(invalid));
            Assert.ThrowsExactly<InvalidCastException>(() => shared.RefreshObjects(invalid));
            Assert.ThrowsExactly<InvalidCastException>(() => shared.MoveObjects(2, invalid));

            CollectionAssert.AreEqual(PersonDb.All.Take(2).ToArray(), this.tolv.Storage);
            Assert.AreSame(PersonDb.All[0], this.tolv.GetModelObject(0));
            Assert.AreSame(PersonDb.All[1], this.tolv.GetModelObject(1));
        }

        [TestMethod]
        public void Test_Events_ValidateReplacementModelsAndHonorCancellation() {
            this.tolv.Objects = PersonDb.All.Take(1);
            EventHandler<ItemsAddingEventArgs> invalid = delegate(object sender, ItemsAddingEventArgs args) {
                args.ObjectsToAdd = new object[] { PersonDb.All[1], "not a person" };
            };
            this.tolv.ItemsAdding += invalid;
            Assert.ThrowsExactly<InvalidCastException>(() => this.tolv.AddObject(PersonDb.All[1]));
            this.tolv.ItemsAdding -= invalid;

            EventHandler<ItemsChangingEventArgs> invalidReplacement = delegate(object sender, ItemsChangingEventArgs args) {
                args.NewObjects = new object[] { PersonDb.All[1], "not a person" };
            };
            this.tolv.ItemsChanging += invalidReplacement;
            Assert.ThrowsExactly<InvalidCastException>(() => this.tolv.Objects = PersonDb.All);
            this.tolv.ItemsChanging -= invalidReplacement;

            this.tolv.ItemsChanging += delegate(object sender, ItemsChangingEventArgs args) {
                args.Canceled = true;
            };
            this.tolv.Objects = PersonDb.All;
            CollectionAssert.AreEqual(PersonDb.All.Take(1).ToArray(), this.tolv.Storage);
            Assert.AreEqual(1, this.tolv.GetItemCount());
        }

        [TestMethod]
        public void Test_MoveUpdateAndRefresh_KeepTypedBackingListInSync() {
            this.tolv.Objects = PersonDb.All.Take(3);
            this.tolv.MoveObjects(0, new[] { PersonDb.All[2] });
            CollectionAssert.AreEqual(new[] { PersonDb.All[2], PersonDb.All[0], PersonDb.All[1] },
                this.tolv.Storage);

            Person updated = new Person(PersonDb.All[0].Name) { Occupation = "updated" };
            this.tolv.UpdateObjects(new[] { updated });
            Assert.AreSame(updated, this.tolv.GetModelObject(1));
            Assert.AreSame(updated, this.tolv.Storage[1]);

            Person refreshed = new Person(updated.Name) { Occupation = "refreshed" };
            this.tolv.RefreshObjects(new[] { refreshed });
            Assert.AreSame(refreshed, this.tolv.Storage[1]);
            Assert.AreEqual("refreshed", this.tolv.GetItem(1).SubItems[1].Text);
        }

        [TestMethod]
        public void Test_SharedUi_SupportsTypedColumnsSortingAndChecking() {
            this.tolv.GetTypedColumn(0).AspectGetter = person => person.Name;
            this.tolv.CheckBoxes = true;
            this.tolv.BooleanCheckStateGetter = model => ((Person)model).Name == PersonDb.All[0].Name;
            this.tolv.Objects = PersonDb.All;
            this.tolv.Sort(this.tolv.GetColumn(0), SortOrder.Descending);

            Assert.AreEqual(PersonDb.LastAlphabeticalName, this.tolv.GetModelObject(0).Name);
            Assert.AreSame(PersonDb.All[0], this.tolv.CheckedObject);
            this.tolv.SelectedObject = PersonDb.All[1];
            Assert.AreSame(PersonDb.All[1], this.tolv.SelectedObject);
            Assert.AreEqual(PersonDb.All[0].Name, this.tolv.GetColumn(0).GetStringValue(PersonDb.All[0]));
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_ModelOverride_IsUsedBySharedUiRowsAndAdapter(bool showGroups) {
            using (var list = new ProjectedPersonListView { View = View.Details, ShowGroups = showGroups }) {
                list.Columns.Add(new OLVColumn("Name", "Name"));
                mainForm.Controls.Add(list);
                list.CreateControl();
                list.Objects = PersonDb.All.Take(1);
                if (showGroups)
                {
                    list.BuildGroups(list.GetColumn(0), SortOrder.Ascending);
                }

                list.SelectedIndices.Add(0);
                list.Projection = new Person("projected");

                ObjectListViewBase shared = list;
                Assert.AreSame(list.Projection, shared.GetModelAt(0));
                Assert.AreSame(list.Projection, list.SelectedObject);
                Assert.AreSame(list.Projection, shared.SelectedModelObjects[0]);
                Assert.AreSame(list.Projection, list.Rows[0]);
                Assert.AreSame(list.Projection, list.Typed.Rows[0]);
                Assert.AreSame(list.Projection, list.Typed.GetModelObject(0));
            }
        }

        [TestMethod]
        public void Test_TypedStateOverrides_AreUsedThroughSharedBridge() {
            using (var list = new TypedStateGuardListView { ShowGroups = false, CheckBoxes = true }) {
                list.Columns.Add(new OLVColumn("Name", "Name"));
                mainForm.Controls.Add(list);
                list.CreateControl();
                list.Objects = PersonDb.All.Take(2);
                list.SelectedObject = PersonDb.All[0];
                list.CheckedObject = PersonDb.All[0];
                ObjectListViewBase shared = list;

                list.RejectChanges = true;
                shared.SelectedModelObject = PersonDb.All[1];
                shared.CheckedModelObject = PersonDb.All[1];
                shared.SelectedModelObjects = new[] { PersonDb.All[1] };
                shared.CheckedModelObjects = new[] { PersonDb.All[1] };
                Assert.AreSame(PersonDb.All[0], list.SelectedObject);
                Assert.AreSame(PersonDb.All[0], list.CheckedObject);

                list.RejectChanges = false;
                list.Typed.SelectedObjects = new[] { PersonDb.All[1] };
                list.Typed.CheckedObjects = new[] { PersonDb.All[1] };
                Assert.AreSame(PersonDb.All[1], shared.SelectedModelObject);
                Assert.AreSame(PersonDb.All[1], shared.CheckedModelObject);
                Assert.ThrowsExactly<InvalidCastException>(() => shared.SelectedModelObject = "wrong type");
                Assert.ThrowsExactly<InvalidCastException>(() => shared.CheckedModelObjects = new object[] { PersonDb.All[0], "wrong type" });
                Assert.ThrowsExactly<InvalidCastException>(() => shared.SetSelectedObjects(new object[] { PersonDb.All[0], "wrong type" }));
                Assert.ThrowsExactly<InvalidCastException>(() => shared.SetCheckedObjects(new object[] { PersonDb.All[0], "wrong type" }));
                Assert.AreSame(PersonDb.All[1], list.SelectedObject);
                Assert.AreSame(PersonDb.All[1], list.CheckedObject);
            }
        }

        [TestMethod]
        public void Test_LegacyStateOverrides_AreUsedThroughTypedUpcastAndAdapter() {
            using (var list = new PropertyGuardListView { ShowGroups = false, CheckBoxes = true }) {
                list.Columns.Add(new OLVColumn("Name", "Name"));
                mainForm.Controls.Add(list);
                list.CreateControl();
                list.SetObjects(PersonDb.All.Take(2));
                list.SelectedObject = PersonDb.All[0];
                list.CheckedObject = PersonDb.All[0];
                TypedObjectListView<object> typed = list;
                int controlCount = mainForm.Controls.Count;
                var adapter = new TypedObjectListViewAdapter<Person>(list);

                list.RejectPropertyChanges = true;
                typed.SelectedObjects = new object[] { PersonDb.All[1] };
                typed.CheckedObjects = new object[] { PersonDb.All[1] };
                adapter.SelectedObjects = null;
                adapter.CheckedObjects = null;
                Assert.AreSame(PersonDb.All[0], typed.SelectedObjects[0]);
                Assert.AreSame(PersonDb.All[0], typed.CheckedObjects[0]);
                Assert.AreSame(PersonDb.All[0], adapter.SelectedObject);
                Assert.AreEqual(controlCount, mainForm.Controls.Count);
                Assert.AreSame(list, adapter.ListView);
            }
        }

        [TestMethod]
        public void Test_SourceSnapshots_AreIsolatedAcrossControlKindsAndStaticTypes() {
            using (var virtualList = CreateVirtualObjectListView()) {
                ObjectListView[] lists = {
                    mainForm.objectListView1, mainForm.fastObjectListView1, mainForm.treeListView1, virtualList
                };
                foreach (ObjectListView list in lists) {
                    list.SetObjects(PersonDb.All.Take(2));
                    IList legacySnapshot = (IList)list.Objects;
                    IList<object> typedSnapshot = (IList<object>)((TypedObjectListView<object>)list).Objects;
                    legacySnapshot.Clear();
                    typedSnapshot.Clear();
                    IList source = ((ObjectListViewBase)list).ModelObjects as IList;
                    if (source != null)
                    {
                        Assert.ThrowsExactly<NotSupportedException>(() => source.Clear());
                    }

                    Assert.AreEqual(2, list.GetItemCount(), list.GetType().Name);
                    CollectionAssert.AreEqual(PersonDb.All.Take(2).ToArray(), (ICollection)list.Objects);
                }
            }
            this.tolv.Objects = PersonDb.All.Take(2);
            IList readOnlySource = (IList)((ObjectListViewBase)this.tolv).ModelObjects;
            Assert.ThrowsExactly<NotSupportedException>(() => readOnlySource.Clear());
            Assert.AreEqual(2, this.tolv.GetItemCount());
        }

        [TestMethod]
        public void Test_Rows_AreLiveReadOnlyAndInvalidateEnumerators() {
            this.tolv.Objects = PersonDb.All.Take(3);
            IReadOnlyList<Person> rows = this.tolv.Rows;
            List<Person> snapshot = this.tolv.GetObjectsSnapshot();
            Assert.IsFalse(rows is IList<Person>);
            using (IEnumerator<Person> iterator = rows.GetEnumerator()) {
                Assert.IsTrue(iterator.MoveNext());
                this.tolv.AddObject(PersonDb.All[3]);
                Assert.ThrowsExactly<InvalidOperationException>(() => iterator.MoveNext());
            }
            Assert.AreEqual(4, rows.Count);
            Assert.AreEqual(3, snapshot.Count);
            this.tolv.Sort(this.tolv.GetColumn(0), SortOrder.Descending);
            Assert.AreSame(this.tolv.GetModelObject(0), rows[0]);
            this.tolv.Objects = PersonDb.All.Take(1);
            Assert.AreEqual(1, rows.Count);
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => { var unused = rows[1]; });
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_Rows_FollowGroupedDisplayOrder(bool useVirtualList) {
            using (TypedObjectListView<object> list = useVirtualList ?
                new FastObjectListView() : new TypedObjectListView<object>()) {
                list.View = View.Details;
                list.ShowGroups = true;
                var column = new OLVColumn("Name", "Name");
                list.Columns.Add(column);
                mainForm.Controls.Add(list);
                list.CreateControl();
                var z = new Person("Z");
                var a = new Person("A");
                var m = new Person("M");
                list.Objects = new[] { z, a, m };
                var adapter = new TypedObjectListViewAdapter<Person>(list);
                IReadOnlyList<object> rows = list.Rows;
                IReadOnlyList<Person> adaptedRows = adapter.Rows;

                foreach (SortOrder order in new[] { SortOrder.Ascending, SortOrder.Descending }) {
                    list.BuildGroups(column, order);
                    Person[] expected = order == SortOrder.Ascending ?
                        new[] { a, m, z } : new[] { z, m, a };
                    for (int i = 0; i < expected.Length; i++) {
                        Assert.AreSame(expected[i], rows[i]);
                        Assert.AreSame(expected[i], adaptedRows[i]);
                    }
                    CollectionAssert.AreEqual(expected, rows.Cast<Person>().ToArray());
                    CollectionAssert.AreEqual(expected, adaptedRows.ToArray());
                }
            }
        }

        [TestMethod]
        public void Test_Rows_OnLargeVirtualSource_DoNotMaterializeModels() {
            using (var list = new CountingVirtualListView()) {
                list.VirtualListSize = 1000000;
                list.ReadCount = 0;
                IReadOnlyList<object> rows = list.Rows;
                Assert.AreEqual(1000000, rows.Count);
                Assert.AreEqual(0, list.ReadCount);
                Assert.AreEqual("row 999999", rows[999999]);
                Assert.AreEqual(1, list.ReadCount);
            }
        }

        [TestMethod]
        public void Test_VirtualSourceSnapshot_IsUnfilteredAndRestoresVisibleRows() {
            using (var list = CreateVirtualObjectListView()) {
                list.VirtualListDataSource = new FilteredVirtualSource(list);
                list.SetObjects(PersonDb.All.Take(3));
                list.UseFiltering = true;
                list.ModelFilter = new ModelFilter(model => model == PersonDb.All[1]);
                Assert.AreEqual(1, list.Rows.Count);

                var adapter = new TypedObjectListViewAdapter<Person>(list);
                CollectionAssert.AreEqual(PersonDb.All.Take(3).ToArray(), adapter.Objects.ToArray());
                Assert.AreEqual(1, list.Rows.Count);
                Assert.AreSame(PersonDb.All[1], list.Rows[0]);
                list.ModelFilter = null;
                Assert.AreEqual(3, list.Rows.Count);
            }
        }

        [TestMethod]
        public void Test_LegacyVirtualRowGetter_SupportsSnapshotsAndTypedAdapter() {
            using (var list = new VirtualObjectListView()) {
                Assert.IsEmpty(list.Objects.Cast<object>());
                list.RowGetter = index => PersonDb.All[index];
                list.VirtualListSize = 3;
                var adapter = new TypedObjectListViewAdapter<Person>(list);
                CollectionAssert.AreEqual(PersonDb.All.Take(3).ToArray(), adapter.Objects.ToArray());
                Assert.AreSame(PersonDb.All[1], adapter.Rows[1]);
            }
        }

        [TestMethod]
        public void Test_TypedAdapters_ReuseColumnsAndReadCanonicalCallbacks() {
            var adapter = this.tolv.Typed;
            var column = adapter.GetColumn(0);
            Assert.AreSame(adapter, this.tolv.Typed);
            Assert.AreSame(column, this.tolv.GetTypedColumn(0));
            column.AspectGetter = person => person.Name;
            Assert.AreEqual(PersonDb.All[0].Name, this.tolv.GetColumn(0).GetValue(PersonDb.All[0]));
            this.tolv.GetColumn(0).AspectGetter = model => "raw callback";
            Assert.AreEqual("raw callback", column.AspectGetter(PersonDb.All[0]));

            adapter.CheckStateGetter = person => CheckState.Checked;
            Assert.AreEqual(CheckState.Checked, this.tolv.CheckStateGetter(PersonDb.All[0]));
            this.tolv.CheckStateGetter = model => CheckState.Unchecked;
            Assert.AreEqual(CheckState.Unchecked, adapter.CheckStateGetter(PersonDb.All[0]));
            adapter.CellToolTipGetter = (col, person) => person.Name;
            Assert.AreEqual(PersonDb.All[0].Name, this.tolv.CellToolTipGetter(this.tolv.GetColumn(0), PersonDb.All[0]));
            adapter.CheckStateGetter = null;
            Assert.IsNull(this.tolv.CheckStateGetter);
        }

        [TestMethod]
        public void Test_StorageEquality_MatchesLegacyUiEquality() {
            using (var list = new TypedObjectListView<EquatableModel> { ShowGroups = false }) {
                list.Columns.Add(new OLVColumn("Name", "Name"));
                mainForm.Controls.Add(list);
                list.CreateControl();
                var original = new EquatableModel();
                var other = new EquatableModel();
                list.Objects = new[] { original };
                list.RemoveObject(other);
                Assert.AreSame(original, list.Objects.Single());
                Assert.AreSame(original, list.GetModelObject(0));
                list.UpdateObject(other);
                Assert.AreEqual(2, list.Objects.Count());
                Assert.AreEqual(2, list.GetItemCount());
            }
        }

        [TestMethod]
        public void Test_SourceHooks_AvoidSnapshotGettersInUiPipeline() {
            using (var list = new SourceHookListView { ShowGroups = false }) {
                list.Columns.Add(new OLVColumn("Name", "Name"));
                mainForm.Controls.Add(list);
                list.CreateControl();
                TypedObjectListView<object> typed = list;
                ObjectListViewBase shared = list;
                typed.Objects = PersonDb.All.Take(1);
                shared.ModelObjects = PersonDb.All.Take(2);
                list.Objects = PersonDb.All.Take(3);
                Assert.AreEqual(3, list.SourceAssignments);
                Assert.AreEqual(3, list.Rows.Count);
                Assert.AreEqual(0, list.SnapshotReads);
                Assert.AreEqual(3, typed.GetObjectsSnapshot().Count);
                Assert.AreEqual(0, list.SnapshotReads);
                CollectionAssert.AreEqual(PersonDb.All.Take(3).ToArray(), (ICollection)list.Objects);
                Assert.AreEqual(1, list.SnapshotReads);
            }
        }

        [TestMethod]
        public void Test_TypedCommandOverloads_HonorLegacyMutationOverrides() {
            using (var list = new CommandHookListView { ShowGroups = false }) {
                list.Columns.Add(new OLVColumn("Name", "Name"));
                mainForm.Controls.Add(list);
                list.CreateControl();
                list.AddObjects(PersonDb.All.Take(1));
                ((ObjectListViewBase)list).AddObjects(PersonDb.All.Skip(1).Take(1));
                list.Typed.AddObjects(PersonDb.All.Skip(2).Take(1));
                Assert.AreEqual(3, list.AddCalls);
                CollectionAssert.AreEqual(PersonDb.All.Take(3).ToArray(), list.GetObjectsSnapshot());
            }
        }

#if NETFRAMEWORK
        [TestMethod]
        public void Test_ConcreteTypedControl_CanBeHostedByDesigner() {
            using (var surface = new DesignSurface(typeof(Form))) {
                var host = (IDesignerHost)surface.GetService(typeof(IDesignerHost));
                var list = (PersonListView)host.CreateComponent(typeof(PersonListView));
                Assert.IsNotNull(host.GetDesigner(list));
                foreach (string name in new[] { "Objects", "Rows", "Typed", "ModelObjects" }) {
                    PropertyDescriptor property = TypeDescriptor.GetProperties(list)[name];
                    Assert.IsFalse(property.IsBrowsable, name);
                    Assert.AreEqual(DesignerSerializationVisibility.Hidden, property.SerializationVisibility, name);
                }
                list.Columns.Add(new OLVColumn("Name", "Name"));
                Assert.AreEqual(1, list.Columns.Count);
            }
        }
#endif

        [TestMethod]
        public void Test_ConcreteTypedControl_ModelPropertiesAreNotSerialized() {
            using (var list = new PersonListView()) {
                foreach (string name in new[] { "Objects", "Rows", "Typed", "ModelObjects" }) {
                    PropertyDescriptor property = TypeDescriptor.GetProperties(list)[name];
                    Assert.IsFalse(property.IsBrowsable, name);
                    Assert.AreEqual(DesignerSerializationVisibility.Hidden, property.SerializationVisibility, name);
                }
            }
        }

        [TestMethod]
        public void Test_CollectionProperties_GenericListsAndSnapshots() {
            this.tolv.Objects = PersonDb.All;
            this.tolv.CheckBoxes = true;
            IList<Person> models = new ArraySegment<Person>(PersonDb.All.ToArray(), 0, 2);
            this.tolv.SelectedObjects = models;
            this.tolv.CheckedObjects = models;

            IList<Person> selected = this.tolv.SelectedObjects;
            IList<Person> checkedObjects = this.tolv.CheckedObjects;
            Assert.AreEqual(2, selected.Count);
            Assert.AreSame(PersonDb.All[0], selected[0]);
            Assert.AreEqual(2, checkedObjects.Count);
            Assert.AreSame(PersonDb.All[0], checkedObjects[0]);

            selected.Clear();
            checkedObjects.Clear();
            Assert.AreEqual(2, this.tolv.SelectedObjects.Count);
            Assert.AreEqual(2, this.tolv.CheckedObjects.Count);
        }

        [TestMethod]
        public void Test_CollectionProperties_HonorControlPropertyOverrides() {
            using (var list = new PropertyGuardListView { View = View.Details, ShowGroups = false }) {
                list.Columns.Add(new OLVColumn("Name", "Name"));
                mainForm.Controls.Add(list);
                list.CreateControl();
                list.SetObjects(PersonDb.All);
                list.CheckBoxes = true;
                list.SelectedObject = PersonDb.All[1];
                list.CheckedObject = PersonDb.All[1];
                ObjectListViewBase shared = list;
                IList<Person> models = new ArraySegment<Person>(PersonDb.All.ToArray(), 0, 1);

                list.RejectPropertyChanges = true;
                shared.SelectedModelObjects = models.ToArray();
                shared.CheckedModelObjects = models.ToArray();
                Assert.AreSame(PersonDb.All[1], list.SelectedObject);
                Assert.AreSame(PersonDb.All[1], list.CheckedObject);
                shared.SelectedModelObjects = null;
                shared.CheckedModelObjects = null;
                Assert.AreSame(PersonDb.All[1], list.SelectedObject);
                Assert.AreSame(PersonDb.All[1], list.CheckedObject);

                list.RejectPropertyChanges = false;
                shared.SelectedModelObjects = models.ToArray();
                shared.CheckedModelObjects = models.ToArray();
                Assert.AreSame(PersonDb.All[0], list.SelectedObject);
                Assert.AreSame(PersonDb.All[0], list.CheckedObject);
                shared.SelectedModelObjects = null;
                shared.CheckedModelObjects = null;
                Assert.IsEmpty(list.SelectedObjects);
                Assert.IsEmpty(list.CheckedObjects);
            }
        }

        [TestMethod]
        public void Test_CheckedObjectsEnumerable_HonorsControlPropertyOverride() {
            using (var list = new PropertyGuardListView { View = View.Details, ShowGroups = false }) {
                list.Columns.Add(new OLVColumn("Name", "Name"));
                mainForm.Controls.Add(list);
                list.CreateControl();
                list.SetObjects(PersonDb.All);
                list.CheckBoxes = true;
                list.CheckedObject = PersonDb.All[1];

                list.RejectPropertyChanges = true;
                list.CheckedObjectsEnumerable = new SingleUseEnumerable<Person>(PersonDb.All.Take(1));
                Assert.AreSame(PersonDb.All[1], list.CheckedObject);

                list.RejectPropertyChanges = false;
                list.CheckedObjectsEnumerable = new SingleUseEnumerable<Person>(PersonDb.All.Take(1));
                Assert.AreSame(PersonDb.All[0], list.CheckedObject);
                list.CheckedObjectsEnumerable = null;
                Assert.IsEmpty(list.CheckedObjects);
            }
        }

        [TestMethod]
        public void Test_GenerateAspectGetters_ExtractsData() {
            this.tolv.GenerateAspectGetters();
            this.tolv.Objects = PersonDb.All;
            Person p = this.tolv.GetModelObject(0);
            Assert.AreEqual(p.Name, this.tolv.Items[0].SubItems[0].Text);
        }

        [TestMethod]
        public void Test_GenerateAspectGetters_NullDataObject() {
            this.tolv.GenerateAspectGetters();
            List<Person> list = new List<Person>();
            list.Add(null);
            this.tolv.Objects = list;
            Assert.AreEqual(list.Count, this.tolv.Objects.Count());
            Assert.IsNull(this.tolv.GetModelObject(0));
        }

        [TestMethod]
        public void Test_GenerateAspectGetters_ClearObjects() {
            this.tolv.GenerateAspectGetters();
            this.tolv.Objects = PersonDb.All;
            this.tolv.ClearObjects();
            Assert.IsNull(this.tolv.Objects);
            this.tolv.Objects = PersonDb.All;
            Assert.AreEqual(PersonDb.All.Count, this.tolv.Objects.Count());
        }

        [TestInitialize]
        public void Init() {
            this.tolv = new PersonListView { View = View.Details, ShowGroups = false };
            this.tolv.Columns.Add(new OLVColumn("Name", "Name"));
            this.tolv.Columns.Add(new OLVColumn("Occupation", "Occupation"));
            this.tolv.Columns.Add(new OLVColumn("Culinary Rating", "CulinaryRating"));
            this.tolv.Columns.Add(new OLVColumn("Can Tell Jokes", "CanTellJokes"));
            mainForm.Controls.Add(this.tolv);
            this.tolv.CreateControl();
        }

        protected PersonListView tolv;

        public sealed class PersonListView : TypedObjectListView<Person> {
            public List<Person> Storage { get { return (List<Person>)this.StoredModelObjects; } }
        }

        private sealed class ProjectedPersonListView : TypedObjectListView<Person> {
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public Person Projection { get; set; }
            public override Person GetModelObject(int index) {
                return this.Projection ?? base.GetModelObject(index);
            }
        }

        private sealed class TypedStateGuardListView : TypedObjectListView<Person> {
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public bool RejectChanges { get; set; }
            public override Person SelectedObject {
                get { return base.SelectedObject; }
                set { if (!this.RejectChanges)
                    {
                        base.SelectedObject = value;
                    }
                }
            }
            public override Person CheckedObject {
                get { return base.CheckedObject; }
                set { if (!this.RejectChanges)
                    {
                        base.CheckedObject = value;
                    }
                }
            }
            public override IList<Person> SelectedObjects {
                get { return base.SelectedObjects; }
                set { if (!this.RejectChanges)
                    {
                        base.SelectedObjects = value;
                    }
                }
            }
            public override IList<Person> CheckedObjects {
                get { return base.CheckedObjects; }
                set { if (!this.RejectChanges)
                    {
                        base.CheckedObjects = value;
                    }
                }
            }
        }

        private sealed class CountingVirtualListView : VirtualObjectListView {
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public int ReadCount { get; set; }
            public override object GetModelObject(int index) {
                this.ReadCount++;
                return "row " + index;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1067", Justification = "This test model verifies that IEquatable alone does not change object-based identity semantics.")]
        private sealed class EquatableModel : IEquatable<EquatableModel> {
            public string Name { get { return "same"; } }
            public bool Equals(EquatableModel other) { return other != null; }
        }

        private sealed class FilteredVirtualSource : AbstractVirtualListDataSource {
            private List<Person> source = new List<Person>();
            private List<Person> visible = new List<Person>();
            public FilteredVirtualSource(VirtualObjectListView list) : base(list) { }
            public override int GetObjectCount() { return this.visible.Count; }
            public override object GetNthObject(int index) { return this.visible[index]; }
            public override int GetObjectIndex(object model) { return this.visible.IndexOf((Person)model); }
            public override void SetObjects(IEnumerable models) {
                this.source = models.Cast<Person>().ToList();
                this.visible = this.source;
            }
            public override void ApplyFilters(IModelFilter modelFilter, IListFilter listFilter) {
                this.visible = modelFilter == null ? this.source :
                    this.source.Where(model => modelFilter.Filter(model)).ToList();
            }
        }

        private sealed class SourceHookListView : ObjectListView {
            public int SourceAssignments { get; private set; }
            public int SnapshotReads { get; private set; }
            public override IEnumerable Objects {
                get { this.SnapshotReads++; return base.Objects; }
                set { base.Objects = value; }
            }
            protected override void SetModelObjectsCore(IEnumerable models) {
                this.SourceAssignments++;
                base.SetModelObjectsCore(models);
            }
        }

        private sealed class CommandHookListView : TypedObjectListView<Person> {
            public int AddCalls { get; private set; }
            public override void AddObjects(IEnumerable models) {
                this.AddCalls++;
                base.AddObjects(models);
            }
        }

        private sealed class PropertyGuardListView : ObjectListView {
            [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
            public bool RejectPropertyChanges { get; set; }

            public override IList SelectedObjects {
                get { return base.SelectedObjects; }
                set {
                    if (!RejectPropertyChanges)
                    {
                        base.SelectedObjects = value;
                    }
                }
            }

            public override IList CheckedObjects {
                get { return base.CheckedObjects; }
                set {
                    if (!RejectPropertyChanges)
                    {
                        base.CheckedObjects = value;
                    }
                }
            }
        }
    }
}