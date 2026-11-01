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
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests
{
    [STATestClass]
    public class TestColumnDpiScaling
    {
        [TestMethod]
        public void Test_DpiScaling_IncludesHiddenColumnsAndScalesSharedColumnsOnce() {
            using (var list = new DpiList()) {
                var visible = new OLVColumn { Width = 140, MinimumWidth = 100, MaximumWidth = 200 };
                var hidden = new OLVColumn { Width = 80, IsVisible = false };
                var visibleOnly = new OLVColumn { Width = 60 };
                list.AllColumns.Add(visible);
                list.AllColumns.Add(hidden);
                list.Columns.AddRange(new[] { visible, visibleOnly });

                list.ApplyDpi(144);
                list.ApplyDpi(144);

                Assert.AreEqual(210, visible.Width);
                Assert.AreEqual(150, visible.MinimumWidth);
                Assert.AreEqual(300, visible.MaximumWidth);
                Assert.AreEqual(120, hidden.Width);
                Assert.AreEqual(-1, hidden.MinimumWidth);
                Assert.AreEqual(-1, hidden.MaximumWidth);
                Assert.AreEqual(90, visibleOnly.Width);
            }
        }

        [TestMethod]
        public void Test_DpiRoundTrip_PreservesUserResizingAndFixedColumnLimits() {
            using (var list = new DpiList()) {
                var resized = new OLVColumn { Width = 140, MinimumWidth = 100, MaximumWidth = 200 };
                var fixedColumn = new OLVColumn { Width = 80, MinimumWidth = 80, MaximumWidth = 80 };
                list.AllColumns.AddRange(new[] { resized, fixedColumn });
                list.Columns.AddRange(new[] { resized, fixedColumn });

                list.ApplyDpi(144);
                resized.Width = 240;
                list.ApplyDpi(96);

                Assert.AreEqual(160, resized.Width);
                Assert.AreEqual(100, resized.MinimumWidth);
                Assert.AreEqual(200, resized.MaximumWidth);
                Assert.AreEqual(80, fixedColumn.Width);
                Assert.IsTrue(fixedColumn.IsFixedWidth);
                list.ApplyDpi(144);
                Assert.AreEqual(240, resized.Width);
                Assert.AreEqual(120, fixedColumn.Width);
                Assert.AreEqual(120, fixedColumn.MinimumWidth);
                Assert.AreEqual(120, fixedColumn.MaximumWidth);
            }
        }

        [TestMethod]
        [DataRow(-1)]
        [DataRow(-2)]
        public void Test_DpiScaling_PreservesAutosizeSentinelsAndZeroWidths(int sentinel) {
            using (var list = new DpiList()) {
                var auto = new OLVColumn();
                ((ColumnHeader)auto).Width = sentinel;
                var zero = new OLVColumn { Width = 0 };
                list.AllColumns.AddRange(new[] { auto, zero });
                list.ApplyDpi(144);
                Assert.AreEqual(sentinel, auto.Width);
                Assert.AreEqual(0, zero.Width);
            }
        }

        [TestMethod]
        public void Test_RecreatingHandle_DoesNotRescaleUserWidthsAgain() {
            using (var list = new DpiList()) {
                var column = new OLVColumn { Width = 140 };
                list.AllColumns.Add(column);
                list.Columns.Add(column);
                list.CreateControl();
                column.Width = 233;
                list.RecreateNativeHandle();
                Assert.AreEqual(233, column.Width);
            }
        }

        [TestMethod]
        public void Test_DpiScaling_RecalculatesFreeSpaceWithScaledLimits() {
            using (var list = new DpiList { Size = new Size(600, 200) }) {
                var fixedColumn = new OLVColumn { Width = 100 };
                var fill = new OLVColumn { Width = 50, MinimumWidth = 50, MaximumWidth = 500, FillsFreeSpace = true };
                list.AllColumns.AddRange(new[] { fixedColumn, fill });
                list.Columns.AddRange(new[] { fixedColumn, fill });
                list.ApplyDpi(144);
                Assert.AreEqual(150, fixedColumn.Width);
                Assert.AreEqual(75, fill.MinimumWidth);
                Assert.AreEqual(750, fill.MaximumWidth);
                Assert.AreEqual(list.ClientSize.Width - fixedColumn.Width, fill.Width);
            }
        }

        [TestMethod]
        public void Test_DpiScaling_WaitsForColumnInitialization() {
            using (var list = new DpiList()) {
                var column = new OLVColumn { Width = 100 };
                list.AllColumns.Add(column);
                ((ISupportInitialize)list).BeginInit();
                list.ApplyDpi(144);
                Assert.AreEqual(100, column.Width);
                ((ISupportInitialize)list).EndInit();
                list.ApplyDpi(144);
                Assert.AreEqual(150, column.Width);
            }
        }

        [TestMethod]
        public void Test_RestoreState_InitializesDpiBeforeApplyingSavedPixelWidths() {
            using (var source = new DpiList())
            using (var target = new DpiList()) {
                var savedColumn = new OLVColumn { Width = 180 };
                source.AllColumns.Add(savedColumn);
                source.Columns.Add(savedColumn);
                byte[] state = source.SaveState();
                var targetColumn = new OLVColumn { Width = 100, MaximumWidth = 200 };
                target.AllColumns.Add(targetColumn);
                target.Columns.Add(targetColumn);
                int? widthAtCreation = null;
                target.HandleCreated += delegate { widthAtCreation = targetColumn.Width; };

                Assert.IsTrue(target.RestoreState(state));
                Assert.AreEqual(100, widthAtCreation);
                Assert.AreEqual(180, targetColumn.Width);
            }
        }

#if !NETFRAMEWORK
        [TestMethod]
        public void Test_DpiCallback_RescalesCurrentUserWidthOnce() {
            using (var list = new DpiList()) {
                var column = new OLVColumn { Width = 100 };
                list.AllColumns.Add(column);
                list.Columns.Add(column);
                list.CreateControl();
                list.ApplyDpi(96);
                list.ApplyDpiCallback(96, 144);
                Assert.AreEqual(150, column.Width);
                column.Width = 240;
                list.ApplyDpiCallback(144, 192);
                list.ApplyDpiCallback(192, 192);
                Assert.AreEqual(320, column.Width);
            }
        }
#endif

        private sealed class DpiList : ObjectListView
        {
            public void ApplyDpi(int dpi) { this.RescaleColumnWidthsForDpi(dpi); }
            public void RecreateNativeHandle() { this.RecreateHandle(); }
#if !NETFRAMEWORK
            public void ApplyDpiCallback(int oldDpi, int newDpi) {
                this.RescaleConstantsForDpi(oldDpi, newDpi);
            }
#endif
        }
    }

    [STATestClass]
    public class TestVirtualColumnSizing : WinFormsTestFixture
    {
        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_FullContentSizing_MeasuresOffscreenRowsWithoutChangingNativeSizing(bool fast) {
            var list = fast ? mainForm.fastObjectListView1 : CreateVirtualObjectListView();
            var models = Enumerable.Range(0, 200).Select(i => new Person(i == 199 ? new string('W', 100) : "x")).ToArray();
            list.SetObjects(models);
            int expected = TextRenderer.MeasureText(models[199].Name, list.Font, Size.Empty,
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width;

            list.AutoResizeColumnToContents(0);
            Assert.IsGreaterThanOrEqualTo(expected, list.GetColumn(0).Width);
            list.AutoResizeColumn(0, ColumnHeaderAutoResizeStyle.ColumnContent);
            Assert.IsLessThan(expected, list.GetColumn(0).Width);
        }

        [TestMethod]
        public void Test_FullContentSizing_UsesFormattedFontAndColumnLimits() {
            var list = mainForm.fastObjectListView1;
            using (var font = new Font(list.Font.FontFamily, 28)) {
                list.RowFormatter = delegate(OLVListItem item) { item.Font = font; };
                list.SetObjects(new[] { new Person("A wide formatted row") });
                int expected = TextRenderer.MeasureText("A wide formatted row", font, Size.Empty,
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix).Width;
                list.AutoResizeColumnToContents(0);
                Assert.IsGreaterThanOrEqualTo(expected, list.GetColumn(0).Width);
                list.GetColumn(0).MaximumWidth = 80;
                list.AutoResizeColumnToContents(0);
                Assert.AreEqual(80, list.GetColumn(0).Width);
            }
        }

        [TestMethod]
        public void Test_FullContentSizing_FailurePreservesWidth() {
            var list = mainForm.fastObjectListView1;
            list.SetObjects(new[] { new Person("first"), new Person("second") });
            var column = list.GetColumn(0);
            column.Width = 123;
            column.AspectGetter = delegate(object model) {
                if (((Person)model).Name == "second")
                {
                    throw new InvalidOperationException("Cannot fetch this row.");
                }

                return "first";
            };
            try {
                Assert.ThrowsExactly<InvalidOperationException>(() => list.AutoResizeColumnToContents(0));
                Assert.AreEqual(123, column.Width);
            }
            finally {
                column.AspectGetter = null;
            }
        }

        [TestMethod]
        public void Test_FullContentSizing_IncludesImagesCheckboxesAndPadding() {
            var list = mainForm.fastObjectListView1;
            using (var image = new Bitmap(32, 16)) {
                var column = list.GetColumn(0);
                column.ImageGetter = delegate { return image; };
                column.CellPadding = new Rectangle(3, 0, 5, 0);
                list.CheckBoxes = true;
                list.SetObjects(new[] { new Person("Image row") });
                int textWidth = TextRenderer.MeasureText("Image row", list.Font).Width;
                int checkboxWidth;
                using (var graphics = list.CreateGraphics())
                {
                    checkboxWidth = CheckBoxRenderer.GetGlyphSize(graphics, System.Windows.Forms.VisualStyles.CheckBoxState.UncheckedNormal).Width;
                }

                int expected = textWidth + image.Width + 8 + checkboxWidth;
                list.AutoResizeColumnToContents(0);
                Assert.IsGreaterThanOrEqualTo(expected, column.Width);
                list.ClearObjects();
            }
        }
    }

    [STATestClass]
    public class TestColumn
    {
        [TestMethod]
        public void TestAspectToStringFormat() {
            OLVColumn column = new OLVColumn();
            column.AspectName = "BirthDate";
            column.AspectToStringFormat = "{0:dd-mm-yy}";
            Assert.AreEqual(String.Format("{0:dd-mm-yy}", this.person1.BirthDate), column.GetStringValue(this.person1));
        }

        [TestMethod]
        public void TestAspectToStringConverter() {
            OLVColumn column = new OLVColumn();
            column.AspectName = "BirthDate";
            column.AspectToStringConverter = delegate(Object x) { return "AspectToStringConverter called"; };
            Assert.AreEqual("AspectToStringConverter called", column.GetStringValue(this.person1));
        }

        [TestInitialize]
        public void Init() {
            this.person1 = new Person("name", "occupation", 100, DateTime.Now, 1.0, true, "  photo  ", "comments");
            this.person2 = new Person2("name", "occupation", 100, DateTime.Now, 1.0, true, "  photo  ", "comments");
        }

        protected Person person1;
        protected Person2 person2;
    }

    [STATestClass]
    public class TestAspectGetting : AspectTestFixture
    {
        public void ExecuteAspect(string aspectName, object expectedResult, Person person) {
            OLVColumn column = new OLVColumn();
            column.AspectName = aspectName;
            Assert.AreEqual(expectedResult, column.GetValue(person));
        }

        virtual public void ExecuteAspect(string aspectName, object expectedResult) {
            this.ExecuteAspect(aspectName, expectedResult, this.person1);
        }

        virtual public void ExecuteAspect2(string aspectName, object expectedResult) {
            this.ExecuteAspect(aspectName, expectedResult, this.person2);
        }

        [TestMethod]
        public void TestSimpleField() {
            this.ExecuteAspect("Comments", "comments");
        }

        [TestMethod]
        public void TestSimpleProperty() {
            this.ExecuteAspect("Occupation", "occupation");
        }

        [TestMethod]
        public void TestSimpleMethod() {
            this.ExecuteAspect("GetRate", 1.0);
        }

        [TestMethod]
        public void TestChainedField() {
            this.ExecuteAspect("Comments.ToUpper", "COMMENTS");
        }

        [TestMethod]
        public void TestReturningValueType() {
            this.ExecuteAspect("CulinaryRating.ToString.Length", 3);
        }

        [TestMethod]
        public void TestReturningValueType2() {
            this.ExecuteAspect("BirthDate.Year", DateTime.Now.Year);
        }

        [TestMethod]
        public void TestChainingValueTypes() {
            this.ExecuteAspect("BirthDate.Year.ToString.Length", 4);
        }

        [TestMethod]
        public void TestChainedMethod() {
            this.ExecuteAspect("Photo.ToString.Trim", "photo");
        }

        [TestMethod]
        public void TestVirtualMethod() {
            this.ExecuteAspect2("GetRate", 2.0);
        }

        [TestMethod]
        public void TestOverriddenProperty() {
            this.ExecuteAspect("CulinaryRating", 100);
            this.ExecuteAspect2("CulinaryRating", 200);
        }

        [TestMethod]
        public virtual void TestWrongName() {
            this.ExecuteAspect("Unknown", "'Unknown' is not a parameter-less method, property or field of type 'BrightIdeasSoftware.Tests.Person'");
        }

        [TestMethod]
        public void TestChainedWrongName() {
            this.ExecuteAspect("Photo.Unknown", "'Unknown' is not a parameter-less method, property or field of type 'System.String'");
        }

        [TestMethod]
        public virtual void TestWrongNameIgnoreMissingAspects() {
            try {
                ObjectListView.IgnoreMissingAspects = true;
                this.ExecuteAspect("Unknown2", null);
            }
            finally {
                ObjectListView.IgnoreMissingAspects = false;
            }
        }

        [TestInitialize]
        public void Init() {
            this.person1 = new Person("name", "occupation", 100, DateTime.Now, 1.0, true, "  photo  ", "comments");
            this.person2 = new Person2("name", "occupation", 100, DateTime.Now, 1.0, true, "  photo  ", "comments");
        }

        protected Person person1;
        protected Person2 person2;
    }

    [STATestClass]
    public class TestIndexedAspects : TestAspectGetting
    {
        public void ExecuteAspect(string aspectName, object expectedResult, object source) {
            OLVColumn column = new OLVColumn();
            column.AspectName = aspectName;
            Assert.AreEqual(expectedResult, column.GetValue(source));
        }

        override public void ExecuteAspect(string aspectName, object expectedResult) {
            this.ExecuteAspect(aspectName, expectedResult, this.dict1);
        }

        override public void ExecuteAspect2(string aspectName, object expectedResult) {
            this.ExecuteAspect(aspectName, expectedResult, this.dict2);
        }

        [TestMethod]
        public override void TestWrongName() {
            // Hashtables return null when a key is not found
            this.ExecuteAspect("Unknown", null);

            // Dictionaries raise KeyNotFound exceptions
            this.ExecuteAspect2("Unknown", "'Unknown' is not a parameter-less method, property or field of type 'System.Collections.Generic.Dictionary`2[System.String,System.Object]'");
        }

        [TestInitialize]
        public new void Init() {
            this.dict1 = new Hashtable();
            this.dict2 = new Dictionary<string, object>();
            InitializeDictionary(this.dict1);
            InitializeDictionary(this.dict2);
            this.dict2["CulinaryRating"] = 200;
            this.dict2["GetRate"] = 2.0;
        }

        private void InitializeDictionary(IDictionary dict) {
            dict["Name"] = "name";
            dict["Occupation"] = "occupation";
            dict["CulinaryRating"] = 100;
            dict["BirthDate"] = DateTime.Now;
            dict["GetRate"] = 1.0;
            dict["CanTellJokes"] = true;
            dict["Comments"] = "comments";
            dict["Photo"] = "  photo  ";
        }

        protected Hashtable dict1;
        protected Dictionary<string, Object> dict2;
    }

    [STATestClass]
    public class TestAspectGeneration : TestAspectGetting
    {
        public void Execute<T>(string aspectName, object expectedResult, T person) where T : class {
            OLVColumn column = new OLVColumn();
            column.AspectName = aspectName;

            TypedColumn<T> tcolumn = new TypedColumn<T>(column);
            Assert.IsNull(column.AspectGetter);
            tcolumn.GenerateAspectGetter();
            Assert.IsNotNull(column.AspectGetter);
            Assert.AreEqual(expectedResult, column.GetValue(person));
        }

        override public void ExecuteAspect(string aspectName, object expectedResult) {
            this.Execute(aspectName, expectedResult, this.person1);
        }

        override public void ExecuteAspect2(string aspectName, object expectedResult) {
            this.Execute(aspectName, expectedResult, this.person2);
        }

        [TestMethod]
        public void TestPropertyReplacedByNew() {
            OLVColumn column = new OLVColumn();
            column.AspectName = "CulinaryRating";

            TypedColumn<Person2> tcolumn = new TypedColumn<Person2>(column);
            Assert.IsNull(column.AspectGetter);
            tcolumn.GenerateAspectGetter();
            Assert.IsNotNull(column.AspectGetter);
            Assert.AreEqual(200, column.GetValue(this.person2));
        }
    }

    [STATestClass]
    public class TestAspectSetting
    {
        public void ExecuteAspect(string aspectName, object newValue, Person person) {
            OLVColumn column = new OLVColumn();
            column.AspectName = aspectName;
            column.PutValue(person, newValue);
            Assert.AreEqual(newValue, column.GetValue(person));
        }

        virtual public void ExecuteAspect(string aspectName, object newValue) {
            this.ExecuteAspect(aspectName, newValue, this.person1);
        }

        virtual public void ExecuteAspect2(string aspectName, object newValue) {
            this.ExecuteAspect(aspectName, newValue, this.person2);
        }

        [TestMethod]
        public void TestSimpleField() {
            this.ExecuteAspect("Comments", "NEW comments");
        }

        [TestMethod]
        public void TestSimpleProperty() {
            this.ExecuteAspect2("Occupation", "NEW occupation");
        }

        [TestMethod]
        public void TestSimpleMethod() {
            this.person1.SetRate(0.0);
            OLVColumn column = new OLVColumn();
            column.AspectName = "SetRate";
            column.PutValue(this.person1, 10.0);
            Assert.AreEqual(10.0, this.person1.GetRate());
        }

        [TestMethod]
        public void TestChaining() {
            DateTime dt = new DateTime(1965, 8, 28);
            this.ExecuteAspect("Parent.BirthDate", dt);
            Assert.AreEqual(dt, this.person1.Parent.BirthDate);
        }

        [TestMethod]
        public void TestChaining2() {
            this.person2.SetRate(0.0);
            OLVColumn column = new OLVColumn();
            column.AspectName = "Parent.SetRate";
            column.PutValue(this.person1, 10.0); 
            // Person2 is the parent of person1, and Person2 doubles the rate
            Assert.AreEqual(20.0, this.person2.GetRate());
        }

        [TestInitialize]
        public void Init() {
            this.person1 = new Person("name", "occupation", 100, DateTime.Now, 1.0, true, "  photo  ", "comments");
            this.person2 = new Person2("name", "occupation", 100, DateTime.Now, 1.0, true, "  photo  ", "comments");
            person2.AddChild(person1);

        }

        protected Person person1;
        protected Person2 person2;
    }

    [STATestClass]
    public class TestIndexedAspectSetting
    {
        public void ExecuteAspect(string aspectName, object newValue, object dict) {
            OLVColumn column = new OLVColumn();
            column.AspectName = aspectName;
            column.PutValue(dict, newValue);
            Assert.AreEqual(newValue, column.GetValue(dict));
        }

        public void ExecuteAspect(string aspectName, object newValue) {
            this.ExecuteAspect(aspectName, newValue, this.dict1);
        }

        public void ExecuteAspect2(string aspectName, object newValue) {
            this.ExecuteAspect(aspectName, newValue, this.dict2);
        }

        [TestMethod]
        public void TestSimpleField() {
            this.ExecuteAspect("Comments", "NEW comments");
            this.ExecuteAspect2("Comments", "NEW comments2");
        }

        [TestMethod]
        public void TestSimpleProperty() {
            this.ExecuteAspect("Occupation", "NEW occupation");
            this.ExecuteAspect2("Occupation", "NEW occupation2");
        }

        [TestInitialize]
        public void Init() {
            this.dict1 = new Hashtable();
            this.dict2 = new Dictionary<string, object>();
            InitializeDictionary(this.dict1);
            InitializeDictionary(this.dict2);
            this.dict2["CulinaryRating"] = 200;
            this.dict2["GetRate"] = 2.0;
        }

        private void InitializeDictionary(IDictionary dict) {
            dict["Name"] = "name";
            dict["Occupation"] = "occupation";
            dict["CulinaryRating"] = 100;
            dict["BirthDate"] = DateTime.Now;
            dict["GetRate"] = 1.0;
            dict["CanTellJokes"] = true;
            dict["Comments"] = "comments";
            dict["Photo"] = "  photo  ";
        }

        protected Hashtable dict1;
        protected Dictionary<string, Object> dict2;
    }

}
