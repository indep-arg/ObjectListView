/*
 * TypedObjectListView - A list view with strongly typed model storage.
 *
 * Author: Phillip Piper
 * Date: 27/09/2008 9:15 AM
 *
 * Change log:
 * v2.6
 * 2012-10-26   JPP  - Handle rare case where a null model object was passed into aspect getters.
 * v2.3
 * 2009-03-31   JPP  - Added Objects property
 * 2008-11-26   JPP  - Added tool tip getting methods
 * 2008-11-05   JPP  - Added CheckState handling methods
 * 2008-10-24   JPP  - Generate dynamic methods MkII. This one handles value types
 * 2008-10-21   JPP  - Generate dynamic methods
 * 2008-09-27   JPP  - Separated from ObjectListView.cs
 * 
 * Copyright (C) 2006-2014 Phillip Piper
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 *
 * If you wish to use this code in a closed source application, please contact phillip.piper@gmail.com.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace BrightIdeasSoftware
{
    /// <summary>
    /// A list view that owns a strongly typed collection of model objects.
    /// </summary>
    /// <remarks>
    /// Columns, rendering and callbacks use the shared non-generic control API.
    /// Use a concrete subclass of this control in the Windows Forms designer.
    /// </remarks>
    /// <typeparam name="T">The class of model object that the list will manage</typeparam>
    /// <example>
    /// To use a TypedObjectListView, you write code like this:
    /// <code>
    /// TypedObjectListView&lt;Person> tlist = new TypedObjectListView&lt;Person>();
    /// tlist.Columns.Add(new OLVColumn("Name", "Name"));
    /// tlist.GetTypedColumn(0).AspectGetter = delegate(Person x) { return x.Name; };
    /// ...
    /// </code>
    /// To iterate over the selected objects, you can write something elegant like this:
    /// <code>
    /// foreach (Person x in tlist.SelectedObjects) {
    ///     x.GrantSalaryIncrease();
    /// }
    /// </code>
    /// </example>
    public class TypedObjectListView<T> : ObjectListViewBase where T : class
    {
        private List<T> objects;
        private System.Collections.ObjectModel.ReadOnlyCollection<T> sourceView;
        private ModelRowCollection<T> rows;
        private TypedObjectListViewAdapter<T> typed;

        /// <inheritdoc/>
        protected override IEnumerable StoredModelObjects {
            get { return this.objects; }
        }

        /// <inheritdoc/>
        protected override ICollection MaterializeModelObjects(IEnumerable collection) {
            return CreateModelList(collection);
        }

        /// <inheritdoc/>
        protected override void SetStoredModelObjects(IEnumerable collection) {
            this.objects = collection == null ? null : collection as List<T> ?? CreateModelList(collection);
            this.sourceView = null;
            this.InvalidateModelRows();
        }

        /// <inheritdoc/>
        protected override void InsertStoredModelObjects(int index, IEnumerable collection, bool includeNulls) {
            List<T> models = collection as List<T> ?? CreateModelList(collection);
            if (!includeNulls)
            {
                models.RemoveAll(model => model == null);
            }

            index = Math.Max(0, Math.Min(index, this.objects.Count));
            this.InvalidateModelRows();
            this.objects.InsertRange(index, models);
        }

        /// <inheritdoc/>
        protected override void RemoveStoredModelObject(object modelObject) {
            int index = this.IndexOfStoredModel((T)modelObject);
            if (index >= 0) {
                this.InvalidateModelRows();
                this.objects.RemoveAt(index);
            }
        }

        /// <inheritdoc/>
        protected override bool ReplaceStoredModelObject(object oldModel, object newModel) {
            T replacement = (T)newModel;
            int index = this.IndexOfStoredModel((T)oldModel);
            if (index < 0)
            {
                return false;
            }

            this.InvalidateModelRows();
            this.objects[index] = replacement;
            return true;
        }

        private int IndexOfStoredModel(T model) {
            if (this.objects == null)
            {
                return -1;
            }

            for (int i = 0; i < this.objects.Count; i++) {
                if (Object.Equals(this.objects[i], model))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Enumerate the source without exposing mutable storage. Override for a custom source.
        /// </summary>
        protected virtual IEnumerable GetModelObjectsCore() {
            return this.objects == null ? null :
                this.sourceView ?? (this.sourceView = this.objects.AsReadOnly());
        }

        /// <inheritdoc/>
        protected override void TakeOwnershipOfObjects() {
            if (this.objects == null)
            {
                this.objects = new List<T>();
            }
        }

        /// <summary>
        /// Assign a source through its normal mutation pipeline, preserving view state.
        /// </summary>
        protected virtual void SetModelObjectsCore(IEnumerable collection) {
            this.SetObjects(collection, true);
        }

        /// <inheritdoc/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public sealed override IEnumerable ModelObjects {
            get { return this.GetModelObjectsCore(); }
            set { this.SetModelObjectsCore(value); }
        }

        /// <summary>
        /// Copy and validate a sequence at the typed model boundary.
        /// </summary>
        protected internal static List<T> CreateModelList(IEnumerable collection) {
            IEnumerable<T> typed = collection as IEnumerable<T>;
            if (typed != null)
            {
                return new List<T>(typed);
            }

            List<T> snapshot = new List<T>();
            if (collection != null) {
                foreach (object model in collection)
                {
                    snapshot.Add((T)model);
                }
            }
            return snapshot;
        }

        //--------------------------------------------------------------------------------------
        // Properties

        /// <summary>
        /// Return the model object that is checked, if only one row is checked.
        /// If zero rows are checked, or more than one row, null is returned.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual T CheckedObject {
            get { return (T)base.CheckedModelObject; }
            set { base.CheckedModelObject = value; }
        }

        /// <summary>
        /// Return the list of all the checked model objects
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual IList<T> CheckedObjects {
            get { return this.GetCheckedObjectsCore(); }
            set { this.SetCheckedObjectsCore(value); }
        }

        /// <summary>
        /// Read checked models through the control's canonical extension point.
        /// </summary>
        protected virtual IList<T> GetCheckedObjectsCore() {
            List<T> checkedObjects = new List<T>();
            if (this.CheckBoxes) {
                for (int i = 0; i < this.GetItemCount(); i++) {
                    if (this.GetItem(i).CheckState == CheckState.Checked)
                    {
                        checkedObjects.Add(this.GetModelObject(i));
                    }
                }
            }
            return checkedObjects;
        }

        /// <summary>
        /// Apply a checked-model snapshot. Null clears the checked state.
        /// </summary>
        protected virtual void SetCheckedObjectsCore(IList<T> value) {
            ((ObjectListViewBase)this).SetCheckedObjects(value);
        }

        /// <summary>
        /// Get or set the list of all model objects
        /// </summary>
        /// <remarks>The getter returns null for an unset/null source, otherwise an independent
        /// snapshot. An assigned sequence is consumed once on the control thread before it
        /// replaces the owned List.</remarks>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IEnumerable<T> Objects {
            get { return this.GetObjectsSnapshot(); }
            set { this.SetModelObjectsCore(value); }
        }

        /// <summary>
        /// Return an independent snapshot of all source models, not just visible rows.
        /// Returns null for an unset/null source.
        /// </summary>
        public List<T> GetObjectsSnapshot() {
            IEnumerable source = this.ModelObjects;
            return source == null ? null : CreateModelList(source);
        }

        /// <summary>
        /// Read the current displayed models without materializing the whole list.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IReadOnlyList<T> Rows {
            get { return this.rows ?? (this.rows = new ModelRowCollection<T>(this)); }
        }

        /// <summary>
        /// Access typed callbacks and compatibility helpers without wrapping another control.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TypedObjectListViewAdapter<T> Typed {
            get { return this.typed ?? (this.typed = new TypedObjectListViewAdapter<T>(this)); }
        }

        /// <summary>
        /// Return the model object that is selected, if only one row is selected.
        /// If zero rows are selected, or more than one row, null is returned.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual T SelectedObject {
            get { return (T)base.SelectedModelObject; }
            set { base.SelectedModelObject = value; }
        }

        /// <summary>
        /// The list of model objects that are selected.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual IList<T> SelectedObjects {
            get { return this.GetSelectedObjectsCore(); }
            set { this.SetSelectedObjectsCore(value); }
        }

        /// <summary>
        /// Read selected models through the control's canonical extension point.
        /// </summary>
        protected virtual IList<T> GetSelectedObjectsCore() {
            List<T> selected = new List<T>(this.SelectedIndices.Count);
            foreach (int index in this.SelectedIndices)
            {
                selected.Add(this.GetModelObject(index));
            }

            return selected;
        }

        /// <summary>
        /// Apply a selection snapshot. Null clears selection.
        /// </summary>
        protected virtual void SetSelectedObjectsCore(IList<T> value) {
            ((ObjectListViewBase)this).SetSelectedObjects(value);
        }

        /// <inheritdoc/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public sealed override object SelectedModelObject {
            get { return this.SelectedObject; }
            set { this.SelectedObject = (T)value; }
        }

        /// <inheritdoc/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public sealed override object CheckedModelObject {
            get { return this.CheckedObject; }
            set { this.CheckedObject = (T)value; }
        }

        /// <inheritdoc/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public sealed override IList SelectedModelObjects {
            get {
                IList<T> selected = this.SelectedObjects;
                return selected as IList ?? CreateModelList(selected);
            }
            set { this.SelectedObjects = value == null ? null : value as IList<T> ?? CreateModelList(value); }
        }

        /// <inheritdoc/>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public sealed override IList CheckedModelObjects {
            get {
                IList<T> checkedObjects = this.CheckedObjects;
                return checkedObjects as IList ?? CreateModelList(checkedObjects);
            }
            set { this.CheckedObjects = value == null ? null : value as IList<T> ?? CreateModelList(value); }
        }

        /// <inheritdoc/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public sealed override object GetModelAt(int index) {
            return this.GetModelObject(index);
        }

        //--------------------------------------------------------------------------------------
        // Accessors

        /// <summary>
        /// Return a typed wrapper around the column at the given index
        /// </summary>
        /// <param name="i">The index of the column</param>
        /// <returns>A typed column or null</returns>
        public virtual TypedColumn<T> GetTypedColumn(int i) {
            return TypedColumn<T>.For(this.GetColumn(i));
        }

        /// <summary>
        /// Return a typed wrapper around the column with the given name
        /// </summary>
        /// <param name="name">The name of the column</param>
        /// <returns>A typed column or null</returns>
        public virtual TypedColumn<T> GetTypedColumn(string name) {
            return TypedColumn<T>.For(this.GetColumn(name));
        }

        /// <summary>
        /// Return the model object at the given index
        /// </summary>
        /// <param name="index">The index of the model object</param>
        /// <returns>The model object or null</returns>
        public virtual T GetModelObject(int index) {
            return (T)base.GetModelAt(index);
        }

        /// <summary>
        /// Replace the model objects without preserving selection.
        /// </summary>
        public void SetObjects(IEnumerable<T> modelObjects) {
            ((ObjectListViewBase)this).SetObjects(modelObjects);
        }

        /// <summary>
        /// Replace the model objects, optionally preserving control state.
        /// </summary>
        public void SetObjects(IEnumerable<T> modelObjects, bool preserveState) {
            ((ObjectListViewBase)this).SetObjects(modelObjects, preserveState);
        }

        /// <summary>
        /// Select the given models and deselect all other models.
        /// </summary>
        public void SetSelectedObjects(IEnumerable<T> modelObjects) {
            ((ObjectListViewBase)this).SetSelectedObjects(modelObjects);
        }

        /// <summary>
        /// Select the given models and deselect all other models.
        /// </summary>
        public void SelectObjects(IEnumerable<T> modelObjects) {
            ((ObjectListViewBase)this).SelectObjects(modelObjects);
        }

        /// <summary>
        /// Check the given models and uncheck all other models.
        /// </summary>
        public void SetCheckedObjects(IEnumerable<T> modelObjects) {
            ((ObjectListViewBase)this).SetCheckedObjects(modelObjects);
        }

        /// <summary>
        /// Add the given models.
        /// </summary>
        public void AddObjects(IEnumerable<T> modelObjects) {
            ((ObjectListViewBase)this).AddObjects(modelObjects);
        }

        /// <summary>
        /// Insert the given models before the specified index.
        /// </summary>
        public void InsertObjects(int index, IEnumerable<T> modelObjects) {
            ((ObjectListViewBase)this).InsertObjects(index, modelObjects);
        }

        /// <summary>
        /// Refresh the rows displaying the given models.
        /// </summary>
        public void RefreshObjects(IEnumerable<T> modelObjects) {
            ((ObjectListViewBase)this).RefreshObjects(modelObjects);
        }

        /// <summary>
        /// Remove the given models.
        /// </summary>
        public void RemoveObjects(IEnumerable<T> modelObjects) {
            ((ObjectListViewBase)this).RemoveObjects(modelObjects);
        }

        /// <summary>
        /// Copy the given models to the clipboard.
        /// </summary>
        public void CopyObjectsToClipboard(IEnumerable<T> modelObjects) {
            ((ObjectListViewBase)this).CopyObjectsToClipboard(modelObjects);
        }

        /// <summary>
        /// This method will generate AspectGetters for any column that has an AspectName.
        /// </summary>
        public virtual void GenerateAspectGetters() {
            for (int i = 0; i < this.Columns.Count; i++)
            {
                this.GetTypedColumn(i).GenerateAspectGetter();
            }
        }
    }

    /// <summary>
    /// A type-safe wrapper around an OLVColumn
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TypedColumn<T> where T : class
    {
        private static readonly ConditionalWeakTable<OLVColumn, TypedColumn<T>> adapters =
            new ConditionalWeakTable<OLVColumn, TypedColumn<T>>();

        internal static TypedColumn<T> For(OLVColumn column) {
            return column == null ? null : adapters.GetValue(column, key => new TypedColumn<T>(key));
        }

        /// <summary>
        /// Creates a TypedColumn
        /// </summary>
        /// <param name="column"></param>
        public TypedColumn(OLVColumn column) {
            ArgumentNullException.ThrowIfNull(column, nameof(column));
            this.column = column;
        }
        private OLVColumn column;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rowObject"></param>
        /// <returns></returns>
        public delegate Object TypedAspectGetterDelegate(T rowObject);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rowObject"></param>
        /// <param name="newValue"></param>
        public delegate void TypedAspectPutterDelegate(T rowObject, Object newValue);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rowObject"></param>
        /// <returns></returns>
        public delegate Object TypedGroupKeyGetterDelegate(T rowObject);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rowObject"></param>
        /// <returns></returns>
        public delegate Object TypedImageGetterDelegate(T rowObject);

        /// <summary>
        /// 
        /// </summary>
        public TypedAspectGetterDelegate AspectGetter {
            get {
                AspectGetterDelegate getter = this.column.AspectGetter;
                return getter == null ? null : new TypedAspectGetterDelegate(model => getter(model));
            }
            set {
                if (value == null)
                {
                    this.column.AspectGetter = null;
                }
                else
                {
                    this.column.AspectGetter = delegate(object x) {
                        return x == null ? null : value((T)x);
                    };
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public TypedAspectPutterDelegate AspectPutter {
            get {
                AspectPutterDelegate putter = this.column.AspectPutter;
                return putter == null ? null : new TypedAspectPutterDelegate((model, value) => putter(model, value));
            }
            set {
                if (value == null)
                {
                    this.column.AspectPutter = null;
                }
                else
                {
                    this.column.AspectPutter = delegate(object x, object newValue) {
                        value((T)x, newValue);
                    };
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public TypedImageGetterDelegate ImageGetter {
            get {
                ImageGetterDelegate getter = this.column.ImageGetter;
                return getter == null ? null : new TypedImageGetterDelegate(model => getter(model));
            }
            set {
                if (value == null)
                {
                    this.column.ImageGetter = null;
                }
                else
                {
                    this.column.ImageGetter = delegate(object x) {
                        return value((T)x);
                    };
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public TypedGroupKeyGetterDelegate GroupKeyGetter {
            get {
                GroupKeyGetterDelegate getter = this.column.GroupKeyGetter;
                return getter == null ? null : new TypedGroupKeyGetterDelegate(model => getter(model));
            }
            set {
                if (value == null)
                {
                    this.column.GroupKeyGetter = null;
                }
                else
                {
                    this.column.GroupKeyGetter = delegate(object x) {
                        return value((T)x);
                    };
                }
            }
        }

        #region Dynamic methods

        /// <summary>
        /// Generate an aspect getter that does the same thing as the AspectName,
        /// except without using reflection.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If you have an AspectName of "Owner.Address.Postcode", this will generate
        /// the equivalent of: <code>this.AspectGetter = delegate (object x) {
        ///     return x.Owner.Address.Postcode;
        /// }
        /// </code>
        /// </para>
        /// <para>
        /// If AspectName is empty, this method will do nothing, otherwise 
        /// this will replace any existing AspectGetter.
        /// </para>
        /// </remarks>
        public void GenerateAspectGetter() {
            if (!String.IsNullOrEmpty(this.column.AspectName))
            {
                this.AspectGetter = this.GenerateAspectGetter(typeof(T), this.column.AspectName);
            }
        }

        /// <summary>
        /// Generates an aspect getter method dynamically. The method will execute
        /// the given dotted chain of selectors against a model object given at runtime.
        /// </summary>
        /// <param name="type">The type of model object to be passed to the generated method</param>
        /// <param name="path">A dotted chain of selectors. Each selector can be the name of a 
        /// field, property or parameter-less method.</param>
        /// <returns>A typed delegate</returns>
        private TypedAspectGetterDelegate GenerateAspectGetter(Type type, string path) {
            DynamicMethod getter = new DynamicMethod(String.Empty,
                typeof(Object), new Type[] { type }, type, true);
            this.GenerateIL(type, path, getter.GetILGenerator());
            return (TypedAspectGetterDelegate)getter.CreateDelegate(typeof(TypedAspectGetterDelegate));
        }

        /// <summary>
        /// This method generates the actual IL for the method.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="path"></param>
        /// <param name="il"></param>
        private void GenerateIL(Type type, string path, ILGenerator il) {
            // Push our model object onto the stack
            il.Emit(OpCodes.Ldarg_0);

            // Generate the IL to access each part of the dotted chain
            string[] parts = path.Split('.');
            for (int i = 0; i < parts.Length; i++) {
                type = this.GeneratePart(il, type, parts[i], (i == parts.Length - 1));
                if (type == null)
                {
                    break;
                }
            }

            // If the object to be returned is a value type (e.g. int, bool), it
            // must be boxed, since the delegate returns an Object
            if (type != null && type.IsValueType && !typeof(T).IsValueType)
            {
                il.Emit(OpCodes.Box, type);
            }

            il.Emit(OpCodes.Ret);
        }

        private Type GeneratePart(ILGenerator il, Type type, string pathPart, bool isLastPart) {
            // TODO: Generate check for null

            // Find the first member with the given name that is a field, property, or parameter-less method
            List<MemberInfo> infos = new List<MemberInfo>(type.GetMember(pathPart));
            MemberInfo info = infos.Find(delegate(MemberInfo x) {
                if (x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property)
                {
                    return true;
                }

                if (x.MemberType == MemberTypes.Method)
                {
                    return ((MethodInfo)x).GetParameters().Length == 0;
                }
                else
                {
                    return false;
                }
            });

            // If we couldn't find anything with that name, pop the current result and return an error
            if (info == null) {
                il.Emit(OpCodes.Pop);
                if (Munger.IgnoreMissingAspects)
                {
                    il.Emit(OpCodes.Ldnull);
                }
                else
                {
                    il.Emit(OpCodes.Ldstr, String.Format("'{0}' is not a parameter-less method, property or field of type '{1}'", pathPart, type.FullName));
                }

                return null;
            }

            // Generate the correct IL to access the member. We remember the type of object that is going to be returned
            // so that we can do a method lookup on it at the next iteration
            Type resultType = null;
            switch (info.MemberType) {
                case MemberTypes.Method:
                    MethodInfo mi = (MethodInfo)info;
                    if (mi.IsVirtual)
                    {
                        il.Emit(OpCodes.Callvirt, mi);
                    }
                    else
                    {
                        il.Emit(OpCodes.Call, mi);
                    }

                    resultType = mi.ReturnType;
                    break;
                case MemberTypes.Property:
                    PropertyInfo pi = (PropertyInfo)info;
                    il.Emit(OpCodes.Call, pi.GetGetMethod());
                    resultType = pi.PropertyType;
                    break;
                case MemberTypes.Field:
                    FieldInfo fi = (FieldInfo)info;
                    il.Emit(OpCodes.Ldfld, fi);
                    resultType = fi.FieldType;
                    break;
            }

            // If the method returned a value type, and something is going to call a method on that value,
            // we need to load its address onto the stack, rather than the object itself.
            if (resultType.IsValueType && !isLastPart) {
                LocalBuilder lb = il.DeclareLocal(resultType);
                il.Emit(OpCodes.Stloc, lb);
                il.Emit(OpCodes.Ldloca, lb);
            }

            return resultType;
        }

        #endregion
    }
}
