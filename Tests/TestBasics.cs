/*
 * [File purpose]
 * Author: Phillip Piper
 * Date: 10/25/2008 11:06 PM
 * 
 * CHANGE LOG:
 * when who what
 * 10/25/2008 JPP  Initial Version
 */

using System;
using System.Collections;
using System.Drawing;
using System.Linq;
using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace BrightIdeasSoftware.Tests
{
    [STATestClass]
    public class TestOlvBasics : WinFormsTestFixture
    {
        [TestInitialize]
        public void SetupTest() {
            this.olv = GetObjectListView();
        }

        protected virtual ObjectListView GetObjectListView() {
            return mainForm.objectListView1;
        }

        protected ObjectListView olv;

        [TestMethod]
        public void Test_SetObjects_All() {
            this.olv.SetObjects(PersonDb.All);
            Assert.AreEqual(PersonDb.All.Count, this.olv.GetItemCount());
        }

        [TestMethod]
        public void Test_SetObjects_Null() {
            this.olv.SetObjects(null);
            Assert.AreEqual(0, this.olv.GetItemCount());
        }

        [TestMethod]
        public void Test_GetModelObject() {
            this.olv.SetObjects(PersonDb.All);
            for (int i = 0; i < PersonDb.All.Count; i++)
            {
                Assert.AreEqual(PersonDb.All[i], this.olv.GetModelObject(i));
            }
        }

        [TestMethod]
        public void Test_AddObject() {
            this.olv.SetObjects(null);
            this.olv.AddObject(PersonDb.All[0]);
            Assert.AreNotEqual(-1, this.olv.IndexOf(PersonDb.All[0]));
            this.olv.AddObject(PersonDb.All[1]);
            Assert.AreNotEqual(-1, this.olv.IndexOf(PersonDb.All[1]));
            Assert.AreEqual(2, this.olv.GetItemCount());
        }

        [TestMethod]
        public void Test_AddObjects() {
            this.olv.SetObjects(null);
            this.olv.AddObjects(PersonDb.All);
            foreach (object x in PersonDb.All)
            {
                Assert.AreNotEqual(-1, this.olv.IndexOf(x));
            }

            Assert.AreEqual(PersonDb.All.Count, this.olv.GetItemCount());
        }

        [TestMethod]
        public void Test_AddObjects_SingleUseQuery() {
            this.olv.ClearObjects();
            this.olv.AddObjects(new SingleUseEnumerable<Person>(PersonDb.All.Where(p => p.CulinaryRating > 100)));
            CollectionAssert.AreEqual(PersonDb.All.Where(p => p.CulinaryRating > 100).ToArray(),
                ObjectListView.EnumerableToArray(this.olv.Objects, true));
        }

        [TestMethod]
        public void Test_RemoveObjects_SelfReferentialQuery() {
            this.olv.SetObjects(new ArrayList(PersonDb.All));
            this.olv.RemoveObjects(new SingleUseEnumerable<Person>(this.olv.Objects.Cast<Person>()));
            Assert.AreEqual(0, this.olv.GetItemCount());
        }

        [TestMethod]
        public void Test_CollectionChanges_EnumerationFailureIsAtomic() {
            this.olv.SetObjects(PersonDb.All);
            Person extra = new Person("extra");
            Assert.ThrowsExactly<InvalidOperationException>(() => this.olv.AddObjects(EnumerableTestData.FailAfter(extra)));
            Assert.AreEqual(-1, this.olv.IndexOf(extra));
            Assert.ThrowsExactly<InvalidOperationException>(() => this.olv.RemoveObjects(EnumerableTestData.FailAfter(PersonDb.All[0])));
            Assert.AreEqual(PersonDb.All.Count, this.olv.GetItemCount());
        }

        [TestMethod]
        public void Test_CollectionChanges_EventsCanReplaceAndCancel() {
            this.olv.ClearObjects();
            EventHandler<ItemsAddingEventArgs> replace = delegate(object sender, ItemsAddingEventArgs args) {
                CollectionAssert.AreEqual(new[] { PersonDb.All[0] }, (ICollection)args.ObjectsToAdd);
                args.ObjectsToAdd = new[] { PersonDb.All[1] };
            };
            this.olv.ItemsAdding += replace;
            this.olv.AddObjects(PersonDb.All.Take(1));
            this.olv.ItemsAdding -= replace;
            Assert.AreEqual(PersonDb.All[1], this.olv.GetModelObject(0));

            this.olv.ItemsRemoving += delegate(object sender, ItemsRemovingEventArgs args) { args.Canceled = true; };
            this.olv.RemoveObjects(PersonDb.All.Skip(1).Take(1));
            this.olv.ItemsAdding += delegate(object sender, ItemsAddingEventArgs args) { args.Canceled = true; };
            this.olv.AddObjects(PersonDb.All.Take(1));
            Assert.AreEqual(1, this.olv.GetItemCount());
        }

        [TestMethod]
        public void Test_CollectionChanges_EmptyAndNullInputs() {
            this.olv.SetObjects(PersonDb.All);
            this.olv.AddObjects(Enumerable.Empty<Person>());
            this.olv.RemoveObjects(Enumerable.Empty<Person>());
            this.olv.AddObjects(null);
            this.olv.RemoveObjects(null);
            Assert.AreEqual(PersonDb.All.Count, this.olv.GetItemCount());
        }

        [TestMethod]
        public virtual void Test_InsertObjects_SingleUseQuery() {
            this.olv.SetObjects(new[] { PersonDb.All[0], PersonDb.All[3] });
            this.olv.InsertObjects(1, new SingleUseEnumerable<Person>(PersonDb.All.Skip(1).Take(2)));
            CollectionAssert.AreEqual(PersonDb.All.Take(4).ToArray(),
                ObjectListView.EnumerableToArray(this.olv.Objects, true));
            Assert.ThrowsExactly<InvalidOperationException>(() => this.olv.InsertObjects(0,
                EnumerableTestData.FailAfter(new Person("extra"))));
            Assert.AreEqual(4, this.olv.GetItemCount());
        }

        [TestMethod]
        public void Test_RefreshObjects_SingleUseAndFailingSequences() {
            this.olv.SetObjects(PersonDb.All);
            Person replacement = new Person(PersonDb.All[0].Name) { Occupation = "replacement occupation" };
            Assert.ThrowsExactly<InvalidOperationException>(() => this.olv.RefreshObjects(EnumerableTestData.FailAfter(replacement)));
            Assert.AreSame(PersonDb.All[0], this.olv.GetModelObject(0));

            this.olv.RefreshObjects(new SingleUseEnumerable<Person>(new[] { replacement }));
            Assert.AreSame(replacement, this.olv.GetModelObject(0));
            Assert.AreEqual(replacement.Occupation, this.olv.GetItem(0).SubItems[1].Text);
        }

        [TestMethod]
        public virtual void Test_AddObject_ModelFilter() {
            ArrayList somePeople = new ArrayList(PersonDb.All);
            Person first = PersonDb.All[0];
            somePeople.Remove(first);
            this.olv.SetObjects(somePeople);

            this.olv.UseFiltering = true;
            this.olv.ModelFilter = new ModelFilter(delegate(object x) { return false; });

            this.olv.AddObject(first);

            this.olv.UseFiltering = false;
            ArrayList contents = ObjectListView.EnumerableToArray(this.olv.Objects, false);
            Assert.AreEqual(somePeople.Count + 1, contents.Count);

            // The added object should have been added at the end of all contents,
            // not at the end of the filtered contents
            Assert.AreEqual(somePeople.Count, this.olv.IndexOf(first));
        }

        [TestMethod]
        public void Test_RemoveObject() {
            this.olv.SetObjects(PersonDb.All);
            this.olv.RemoveObject(PersonDb.All[1]);
            Assert.AreEqual(-1, this.olv.IndexOf(PersonDb.All[1]));
            Assert.AreEqual(PersonDb.All.Count - 1, this.olv.GetItemCount());
        }

        [TestMethod]
        public void Test_RemoveObjects() {
            this.olv.SetObjects(PersonDb.All);
            List<Person> toRemove = new List<Person>();
            toRemove.Add(PersonDb.All[1]);
            toRemove.Add(PersonDb.All[2]);
            toRemove.Add(PersonDb.All[5]);
            this.olv.RemoveObjects(toRemove);
            foreach (Person x in toRemove)
            {
                Assert.AreEqual(-1, this.olv.IndexOf(x));
            }

            Assert.AreEqual(PersonDb.All.Count - toRemove.Count, this.olv.GetItemCount());
        }

        [TestMethod]
        public void Test_EffectiveRowHeight() {
            this.olv.RowHeight = 32;
            Assert.AreEqual(32, this.olv.RowHeightEffective);
            this.olv.RowHeight = -1;
        }

        [TestMethod]
        public void Test_RefreshObject() {
            this.olv.SetObjects(PersonDb.All);
            Person another = new Person(PersonDb.All[1].Name);
            another.Occupation = "a new occupation";

            OLVListItem item = this.olv.ModelToItem(another);
            Assert.IsNotNull(item);
            Assert.AreNotEqual(another.Occupation, item.SubItems[1].Text);

            this.olv.RefreshObject(another);

            OLVListItem item2 = this.olv.ModelToItem(another);
            Assert.IsNotNull(item2);
            Assert.AreEqual(another.Occupation, item2.SubItems[1].Text);
        }

        [TestMethod]
        public void Test_UpdateObject_AddsWhenNew() {
            List<Person> people = new List<Person>();
            people.Add(PersonDb.All[0]);
            people.Add(PersonDb.All[1]);
            people.Add(PersonDb.All[2]);
            this.olv.SetObjects(people);

            Person newGuy = PersonDb.All[3];
            OLVListItem item = this.olv.ModelToItem(newGuy);
            Assert.IsNull(item);

            this.olv.UpdateObject(newGuy);

            OLVListItem item2 = this.olv.ModelToItem(newGuy);
            Assert.IsNotNull(item2);
            Assert.AreEqual(newGuy.Occupation, item2.SubItems[1].Text);
        }

        [TestMethod]
        public void Test_UpdateObject_AddsWhenNew_WithFilterInstalled() {
            List<Person> people = new List<Person>();
            people.Add(PersonDb.All[0]);
            people.Add(PersonDb.All[1]);
            people.Add(PersonDb.All[2]);
            this.olv.SetObjects(people);

            Person newGuy = PersonDb.All[3];
            this.olv.UseFiltering = true;
            this.olv.ModelFilter = new ModelFilter(delegate(object x) { return ((Person) x).Name == newGuy.Name; });
            Assert.AreEqual(0, this.olv.GetItemCount());

            this.olv.UpdateObject(newGuy);

            OLVListItem item2 = this.olv.ModelToItem(newGuy);
            Assert.IsNotNull(item2);
            Assert.AreEqual(newGuy.Occupation, item2.SubItems[1].Text);
        }

    }

    [STATestClass]
    public class TestFastOlvBasics : TestOlvBasics
    {
        protected override ObjectListView GetObjectListView() {
            return mainForm.fastObjectListView1;
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(2)]
        public void Test_MoveObjects_ToCurrentPosition(int index) {
            this.olv.SetObjects(PersonDb.All.Take(4).ToArray());
            this.olv.MoveObjects(index, new[] { PersonDb.All[index] });
            CollectionAssert.AreEqual(PersonDb.All.Take(4).ToArray(),
                ObjectListView.EnumerableToArray(this.olv.Objects, true));
        }

        [TestMethod]
        public void Test_MoveObjects_FirstObjectsToStart() {
            this.olv.SetObjects(PersonDb.All.Take(4).ToArray());
            this.olv.MoveObjects(0, PersonDb.All.Take(2).ToArray());
            CollectionAssert.AreEqual(PersonDb.All.Take(4).ToArray(),
                ObjectListView.EnumerableToArray(this.olv.Objects, true));
        }

        [TestMethod]
        public void Test_DataSource_DirectEnumerableMutations() {
            var source = ((FastObjectListView)this.olv).VirtualListDataSource;
            this.olv.ClearObjects();
            source.AddObjects(new SingleUseEnumerable<Person>(PersonDb.All.Take(2)));
            source.InsertObjects(1, new SingleUseEnumerable<Person>(PersonDb.All.Skip(2).Take(1)));
            this.olv.BuildList();
            CollectionAssert.AreEqual(new[] { PersonDb.All[0], PersonDb.All[2], PersonDb.All[1] },
                ObjectListView.EnumerableToArray(this.olv.Objects, true));
            Assert.ThrowsExactly<InvalidOperationException>(() => source.AddObjects(
                EnumerableTestData.FailAfter(new Person("extra"))));
            Assert.ThrowsExactly<InvalidOperationException>(() => source.RemoveObjects(
                EnumerableTestData.FailAfter(PersonDb.All[0])));
            Assert.AreEqual(3, source.GetObjectCount());
            source.RemoveObjects(new SingleUseEnumerable<Person>(this.olv.Objects.Cast<Person>()));
            Assert.AreEqual(0, source.GetObjectCount());
        }
    }

    [STATestClass]
    public class TestVirtualOlvEnumerables : WinFormsTestFixture
    {
        [TestMethod]
        public void Test_CustomDataSource_EnumerableCommands() {
            var list = CreateVirtualObjectListView();
            list.AddObjects(new SingleUseEnumerable<Person>(PersonDb.All.Take(2)));
            list.InsertObjects(1, new SingleUseEnumerable<Person>(PersonDb.All.Skip(2).Take(1)));
            Assert.AreEqual(3, list.GetItemCount());
            Assert.AreSame(PersonDb.All[2], list.GetModelObject(1));
            Assert.ThrowsExactly<InvalidOperationException>(() => list.RemoveObjects(
                EnumerableTestData.FailAfter(PersonDb.All[0])));
            Assert.AreEqual(3, list.GetItemCount());
            list.RemoveObjects(new SingleUseEnumerable<Person>(PersonDb.All.Take(1)));
            Assert.AreEqual(2, list.GetItemCount());
            Person replacement = new Person(PersonDb.All[1].Name);
            list.RefreshObjects(new SingleUseEnumerable<Person>(new[] { replacement }));
            Assert.AreSame(replacement, list.GetModelObject(1));
        }

        [TestMethod]
        public void Test_CustomDataSource_SelectionAndCheckingSnapshots() {
            var list = CreateVirtualObjectListView();
            list.SetObjects(PersonDb.All);
            list.SetSelectedObjects(new SingleUseEnumerable<Person>(PersonDb.All.Take(2)));
            IList selection = list.SelectedObjects;
            list.SetSelectedObjects(new SingleUseEnumerable<Person>(
                list.SelectedIndices.Cast<int>().Select(i => (Person)list.GetModelObject(i)).Take(1)));
            Assert.AreSame(PersonDb.All[0], list.SelectedObject);
            Assert.AreEqual(2, ObjectListView.EnumerableCount(selection));

            list.CheckBoxes = true;
            list.SetCheckedObjects(new SingleUseEnumerable<Person>(PersonDb.All.Take(2)));
            IList checkedObjects = list.CheckedObjects;
            list.CheckedObjects = null;
            Assert.IsEmpty(list.CheckedObjects);
            Assert.AreEqual(2, ObjectListView.EnumerableCount(checkedObjects));
        }
    }

    [STATestClass]
    public class TestDataOlvEnumerables : WinFormsTestFixture
    {
        [TestMethod]
        public void Test_BoundCollectionCommands_RemainNoOps() {
            using (var list = new DataListView()) {
                mainForm.Controls.Add(list);
                var people = new BindingList<Person>(PersonDb.All.Take(2).ToList());
                list.DataSource = people;
                Assert.AreEqual(2, list.GetItemCount());
                list.AddObjects(EnumerableTestData.FailAfter(PersonDb.All[2]));
                list.InsertObjects(0, EnumerableTestData.FailAfter(PersonDb.All[2]));
                list.RemoveObjects(EnumerableTestData.FailAfter(PersonDb.All[0]));
                Assert.AreEqual(2, list.GetItemCount());
                Assert.AreEqual(2, people.Count);
                people.Add(PersonDb.All[2]);
                Assert.AreEqual(3, list.GetItemCount());
            }
        }
    }

    [STATestClass]
    public class TestTreeListViewBasics : TestOlvBasics
    {
        protected override ObjectListView GetObjectListView() {
            return mainForm.treeListView1;
        }

        [TestMethod]
        public override void Test_InsertObjects_SingleUseQuery() {
            Assert.ThrowsExactly<NotImplementedException>(() =>
                this.olv.InsertObjects(0, new SingleUseEnumerable<Person>(PersonDb.All.Take(1))));
        }
    }
}
