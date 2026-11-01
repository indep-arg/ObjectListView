using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests
{
    [TestClass]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "TestCleanup disposes the fixture form and its owned controls.")]
    public abstract class WinFormsTestFixture
    {
        protected MainForm mainForm;

        protected VirtualObjectListView CreateVirtualObjectListView() {
            var list = new VirtualObjectListView { View = View.Details, ShowGroups = false };
            list.Columns.Add(new OLVColumn("Name", "Name"));
            list.Columns.Add(new OLVColumn("Occupation", "Occupation"));
            list.VirtualListDataSource = new EnumerableVirtualDataSource(list);
            mainForm.Controls.Add(list);
            list.CreateControl();
            return list;
        }

        [TestInitialize]
        public void CreateTestForm()
        {
            PersonDb.Reset();
            mainForm = new MainForm();
            mainForm.Size = Size.Empty;
            mainForm.Opacity = 0;
            mainForm.ShowInTaskbar = false;
            mainForm.Show();
        }

        [TestCleanup]
        public void DisposeTestForm()
        {
            if (mainForm != null)
            {
                mainForm.Dispose();
            }
        }

        internal sealed class SingleUseEnumerable<T> : IEnumerable<T>
        {
            private readonly IEnumerable<T> source;
            private bool enumerated;

            public SingleUseEnumerable(IEnumerable<T> source) {
                this.source = source;
            }

            public IEnumerator<T> GetEnumerator() {
                if (enumerated)
                {
                    throw new InvalidOperationException("The sequence was enumerated more than once.");
                }

                enumerated = true;
                return source.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return GetEnumerator();
            }
        }

        internal static class EnumerableTestData
        {
            public static IEnumerable<T> FailAfter<T>(T first) {
                yield return first;
                throw new InvalidOperationException("Enumeration failed.");
            }
        }

        internal sealed class EnumerableVirtualDataSource : AbstractVirtualListDataSource
        {
            private ArrayList objects = new ArrayList();

            public EnumerableVirtualDataSource(VirtualObjectListView list) : base(list) {
            }

            public override int GetObjectCount() { return objects.Count; }
            public override object GetNthObject(int index) { return objects[index]; }
            public override int GetObjectIndex(object model) { return objects.IndexOf(model); }

            public override void SetObjects(IEnumerable models) {
                objects = ObjectListView.EnumerableToArray(models, true);
            }

            public override void AddObjects(IEnumerable models) {
                objects.AddRange(ObjectListView.EnumerableToArray(models, true));
            }

            public override void InsertObjects(int index, IEnumerable models) {
                objects.InsertRange(index, ObjectListView.EnumerableToArray(models, true));
            }

            public override void RemoveObjects(IEnumerable models) {
                foreach (object model in models)
                {
                    objects.Remove(model);
                }
            }

            public override void UpdateObject(int index, object model) {
                objects[index] = model;
            }
        }
    }
}
