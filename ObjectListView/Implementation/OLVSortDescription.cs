using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Forms;

namespace BrightIdeasSoftware {
    /// <summary>
    /// A column and direction in an ordered, multi-column sort.
    /// </summary>
    public sealed class OLVSortDescription {
        /// <summary>
        /// Create a sort description.
        /// </summary>
        public OLVSortDescription(OLVColumn column, SortOrder sortOrder) {
            ArgumentNullException.ThrowIfNull(column, nameof(column));
            if (sortOrder != SortOrder.Ascending && sortOrder != SortOrder.Descending)
            {
                throw new ArgumentOutOfRangeException(nameof(sortOrder));
            }

            this.Column = column;
            this.SortOrder = sortOrder;
        }

        /// <summary>
        /// Gets the column to compare.
        /// </summary>
        public OLVColumn Column { get; private set; }

        /// <summary>
        /// Gets the direction of comparison.
        /// </summary>
        public SortOrder SortOrder { get; private set; }

        private OLVSortDescription() {
        }

        internal static OLVSortDescription CreateConfiguration(OLVColumn column, SortOrder order) {
            if (order != SortOrder.None && order != SortOrder.Ascending && order != SortOrder.Descending)
            {
                throw new ArgumentOutOfRangeException(nameof(order));
            }

            return new OLVSortDescription { Column = column, SortOrder = order };
        }
    }

    internal sealed class SortDescriptionCollection : Collection<OLVSortDescription> {
        internal SortDescriptionCollection() {
        }

        internal SortDescriptionCollection(IEnumerable<OLVSortDescription> descriptions) {
            ArgumentNullException.ThrowIfNull(descriptions, nameof(descriptions));
            foreach (OLVSortDescription description in descriptions)
            {
                this.Add(description);
            }
        }

        internal SortDescriptionCollection(OLVColumn column, SortOrder order, OLVColumn secondaryColumn, SortOrder secondaryOrder) {
            this.SetColumn(0, column);
            this.SetOrder(0, order);
            if (secondaryColumn != null || secondaryOrder != SortOrder.None) {
                this.SetColumn(1, secondaryColumn);
                this.SetOrder(1, secondaryOrder);
            }
        }

        internal OLVColumn GetColumn(int index) {
            return index < this.Count ? this[index].Column : null;
        }

        internal SortOrder GetOrder(int index) {
            return index < this.Count ? this[index].SortOrder : SortOrder.None;
        }

        internal void SetColumn(int index, OLVColumn column) {
            this.SetDescription(index, OLVSortDescription.CreateConfiguration(column, this.GetOrder(index)));
        }

        internal void SetOrder(int index, SortOrder order) {
            this.SetDescription(index, OLVSortDescription.CreateConfiguration(this.GetColumn(index), order));
        }

        private void SetDescription(int index, OLVSortDescription description) {
            // Legacy callers can set a direction before its column, or configure the secondary field first.
            while (this.Count <= index)
            {
                this.Add(OLVSortDescription.CreateConfiguration(null, SortOrder.None));
            }

            this[index] = description;
        }

        internal SortDescriptionCollection WithPrimary(OLVColumn column, SortOrder order) {
            SortDescriptionCollection result = new SortDescriptionCollection(this);
            result.SetColumn(0, column);
            result.SetOrder(0, order);
            return result;
        }

        internal void ReplaceWith(IEnumerable<OLVSortDescription> descriptions) {
            List<OLVSortDescription> snapshot = new List<OLVSortDescription>(descriptions);
            this.Clear();
            foreach (OLVSortDescription description in snapshot)
            {
                this.Add(description);
            }
        }

        internal IList<OLVSortDescription> GetActiveDescriptions() {
            List<OLVSortDescription> result = new List<OLVSortDescription>();
            if (this.GetColumn(0) == null || this.GetOrder(0) == SortOrder.None)
            {
                return result.AsReadOnly();
            }

            foreach (OLVSortDescription description in this) {
                if (description.Column == null || result.Exists(x => x.Column == description.Column))
                {
                    continue;
                }

                SortOrder order = description.SortOrder == SortOrder.None ? this.GetOrder(0) : description.SortOrder;
                result.Add(new OLVSortDescription(description.Column, order));
            }
            return result.AsReadOnly();
        }

        protected override void InsertItem(int index, OLVSortDescription item) {
            ArgumentNullException.ThrowIfNull(item, nameof(item));
            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, OLVSortDescription item) {
            ArgumentNullException.ThrowIfNull(item, nameof(item));
            base.SetItem(index, item);
        }
    }
}
