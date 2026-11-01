using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

namespace BrightIdeasSoftware {
    /// <summary>
    /// Typed access to an existing control. The adapter never creates or owns a control.
    /// </summary>
    public class TypedObjectListViewAdapter<T> where T : class {
        private ModelRowCollection<T> rows;

        /// <summary>
        /// Adapt an existing regular, virtual, or tree control.
        /// </summary>
        public TypedObjectListViewAdapter(ObjectListViewBase listView) {
            ArgumentNullException.ThrowIfNull(listView, nameof(listView));
            this.ListView = listView;
        }

        /// <summary>The existing control.</summary>
        public ObjectListViewBase ListView { get; private set; }

        /// <summary>A snapshot of all source models, or null for an unset/null source.</summary>
        public IEnumerable<T> Objects {
            get {
                IEnumerable source = this.ListView.ModelObjects;
                return source == null ? null : Copy(source);
            }
            set { this.ListView.ModelObjects = value; }
        }

        /// <summary>A live, read-only view of the displayed rows.</summary>
        public IReadOnlyList<T> Rows {
            get { return this.rows ?? (this.rows = new ModelRowCollection<T>(this.ListView)); }
        }

        /// <summary>The selected model, or null unless exactly one model is selected.</summary>
        public T SelectedObject {
            get { return (T)this.ListView.SelectedModelObject; }
            set { this.ListView.SelectedModelObject = value; }
        }

        /// <summary>A snapshot of selected models. Null clears the selection.</summary>
        public IList<T> SelectedObjects {
            get { return Copy(this.ListView.SelectedModelObjects); }
            set { this.ListView.SelectedModelObjects = value == null ? null : value as IList ?? Copy(value); }
        }

        /// <summary>The checked model, or null unless exactly one model is checked.</summary>
        public T CheckedObject {
            get { return (T)this.ListView.CheckedModelObject; }
            set { this.ListView.CheckedModelObject = value; }
        }

        /// <summary>A snapshot of checked models. Null clears the checked state.</summary>
        public IList<T> CheckedObjects {
            get { return Copy(this.ListView.CheckedModelObjects); }
            set { this.ListView.CheckedModelObjects = value == null ? null : value as IList ?? Copy(value); }
        }

        /// <summary>Read or configure the control's check-state callback.</summary>
        public Func<T, CheckState> CheckStateGetter {
            get {
                CheckStateGetterDelegate getter = this.ListView.CheckStateGetter;
                return getter == null ? null : new Func<T, CheckState>(model => getter(model));
            }
            set {
                this.ListView.CheckStateGetter = value == null ? null :
                    new CheckStateGetterDelegate(model => value((T)model));
            }
        }

        /// <summary>Configure the control's Boolean check-state callback.</summary>
        public Func<T, bool> BooleanCheckStateGetter {
            set {
                this.ListView.BooleanCheckStateGetter = value == null ? null :
                    new BooleanCheckStateGetterDelegate(model => value((T)model));
            }
        }

        /// <summary>Read or configure the control's check-state update callback.</summary>
        public Func<T, CheckState, CheckState> CheckStatePutter {
            get {
                CheckStatePutterDelegate putter = this.ListView.CheckStatePutter;
                return putter == null ? null : new Func<T, CheckState, CheckState>((model, state) => putter(model, state));
            }
            set {
                this.ListView.CheckStatePutter = value == null ? null :
                    new CheckStatePutterDelegate((model, state) => value((T)model, state));
            }
        }

        /// <summary>Configure the control's Boolean check-state update callback.</summary>
        public Func<T, bool, bool> BooleanCheckStatePutter {
            set {
                this.ListView.BooleanCheckStatePutter = value == null ? null :
                    new BooleanCheckStatePutterDelegate((model, state) => value((T)model, state));
            }
        }

        /// <summary>Read or configure the control's cell tooltip callback.</summary>
        public Func<OLVColumn, T, string> CellToolTipGetter {
            get {
                CellToolTipGetterDelegate getter = this.ListView.CellToolTipGetter;
                return getter == null ? null : new Func<OLVColumn, T, string>((column, model) => getter(column, model));
            }
            set {
                this.ListView.CellToolTipGetter = value == null ? null :
                    new CellToolTipGetterDelegate((column, model) => value(column, (T)model));
            }
        }

        /// <summary>Read or configure the control's header tooltip callback.</summary>
        public HeaderToolTipGetterDelegate HeaderToolTipGetter {
            get { return this.ListView.HeaderToolTipGetter; }
            set { this.ListView.HeaderToolTipGetter = value; }
        }

        /// <summary>Return a cached typed adapter for a column.</summary>
        public TypedColumn<T> GetColumn(int index) {
            return TypedColumn<T>.For(this.ListView.GetColumn(index));
        }

        /// <summary>Return a cached typed adapter for a named column.</summary>
        public TypedColumn<T> GetColumn(string name) {
            return TypedColumn<T>.For(this.ListView.GetColumn(name));
        }

        /// <summary>Return the model at a displayed row index.</summary>
        public T GetModelObject(int index) {
            return (T)this.ListView.GetModelAt(index);
        }

        /// <summary>Generate aspect getters for the existing columns.</summary>
        public void GenerateAspectGetters() {
            foreach (OLVColumn column in this.ListView.Columns)
            {
                TypedColumn<T>.For(column).GenerateAspectGetter();
            }
        }

        /// <summary>Replace the existing selection.</summary>
        public void SetSelectedObjects(IEnumerable<T> models) { this.ListView.SetSelectedObjects(models); }

        /// <summary>Replace the existing selection.</summary>
        public void SelectObjects(IEnumerable<T> models) { this.ListView.SelectObjects(models); }

        /// <summary>Replace the existing checked-model set.</summary>
        public void SetCheckedObjects(IEnumerable<T> models) { this.ListView.SetCheckedObjects(models); }

        /// <summary>Replace the source models.</summary>
        public void SetObjects(IEnumerable<T> models) { this.ListView.SetObjects(models); }

        /// <summary>Replace the source models, optionally preserving view state.</summary>
        public void SetObjects(IEnumerable<T> models, bool preserveState) { this.ListView.SetObjects(models, preserveState); }

        /// <summary>Add models through the control's mutation pipeline.</summary>
        public void AddObjects(IEnumerable<T> models) { this.ListView.AddObjects(models); }

        /// <summary>Insert models through the control's mutation pipeline.</summary>
        public void InsertObjects(int index, IEnumerable<T> models) { this.ListView.InsertObjects(index, models); }

        /// <summary>Remove models through the control's mutation pipeline.</summary>
        public void RemoveObjects(IEnumerable<T> models) { this.ListView.RemoveObjects(models); }

        /// <summary>Refresh the rows displaying the given models.</summary>
        public void RefreshObjects(IEnumerable<T> models) { this.ListView.RefreshObjects(models); }

        /// <summary>Copy the given models to the clipboard.</summary>
        public void CopyObjectsToClipboard(IEnumerable<T> models) { this.ListView.CopyObjectsToClipboard(models); }

        private static List<T> Copy(IEnumerable models) {
            return TypedObjectListView<T>.CreateModelList(models);
        }
    }

    internal sealed class ModelRowCollection<T> : IReadOnlyList<T> where T : class {
        private readonly ObjectListViewBase listView;

        internal ModelRowCollection(ObjectListViewBase listView) {
            this.listView = listView;
        }

        public int Count { get { return this.listView.GetItemCount(); } }

        public T this[int index] {
            get {
                if (index < 0 || index >= this.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                if (this.listView.ShowGroups && (this.listView.Groups.Count > 0 ||
                    (this.listView.OLVGroups != null && this.listView.OLVGroups.Count > 0)))
                {
                    index = this.listView.GetNthItemInDisplayOrder(index).Index;
                }

                return (T)this.listView.GetModelAt(index);
            }
        }

        public IEnumerator<T> GetEnumerator() {
            return this.Enumerate(this.listView.ModelRowsVersion).GetEnumerator();
        }

        private IEnumerable<T> Enumerate(int version) {
            for (int i = 0; ; i++) {
                if (version != this.listView.ModelRowsVersion)
                {
                    throw new InvalidOperationException("The displayed rows changed during enumeration.");
                }

                if (i >= this.Count)
                {
                    yield break;
                }

                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() { return this.GetEnumerator(); }
    }
}
