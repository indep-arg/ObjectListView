/*
 * TestExport - Test that exporting rows of ObjectListView
 *
 * Author: Phillip Piper
 * Date: 7 August 2012, 10:35pm
 *
 * Change log:
 * 2012-08-07  JPP  Initial code
 * 
 * Copyright (C) 2012 Phillip Piper
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
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests
{
    [STATestClass]
    public class TestExport : WinFormsTestFixture {

        [TestInitialize]
        public void TestSetup() {
            this.olv = mainForm.objectListView1;
        }

        [TestMethod]
        public void Test_TabSeparated_Simple() {
            this.olv.SetObjects(PersonDb.All);
            OLVExporter exporter = new OLVExporter(this.olv);
            string tabSeparated = exporter.ExportTo(OLVExporter.ExportFormat.TSV);
            Assert.AreEqual(string.Join(Environment.NewLine, new[] {
                "Name\tOccupation\tCulinary Rating\tCanTellJokes\tIsActive",
                "name\toccupation\t300\tTrue\t",
                "name2\toccupation\t200\tTrue\t",
                "aaa First Alphabetical Name\toccupation3\t90\tTrue\t",
                "name4\toccupation4\t80\tTrue\t",
                "name5\toccupation5\t140\tTrue\t",
                "name6\toccupation6\t65\tTrue\t",
                "name7\toccupation7\t62\tTrue\t",
                "zzz Last Alphabetical Name\toccupation6\t60\tTrue\t",
                ""
            }), tabSeparated);
        }

        [TestMethod]
        public void Test_DataObject_SingleUseEnumerableAndSnapshot() {
            this.olv.SetObjects(PersonDb.All);
            var source = PersonDb.All.Take(2).ToList();
            var data = new OLVDataObject(this.olv, new SingleUseEnumerable<Person>(source.Where(p => p != null)));
            source.Clear();
            data.CreateTextFormats();
            Assert.AreEqual(2, data.ModelObjects.Count);
#if NETFRAMEWORK
            string text = (string)data.GetData(typeof(string));
#else
            Assert.IsTrue(data.TryGetData<string>(out string text));
#endif
            Assert.Contains("name\toccupation\t300", text);
            Assert.Contains("name2\toccupation\t200", text);
            Assert.DoesNotContain("name4\t", text);
        }

        [TestMethod]
        public void Test_CopyObjectsToClipboard_EmptyAndFailingEnumerables() {
            this.olv.CopyObjectsToClipboard(new SingleUseEnumerable<Person>(Enumerable.Empty<Person>()));
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                this.olv.CopyObjectsToClipboard(EnumerableTestData.FailAfter(PersonDb.All[0])));
            Assert.ThrowsExactly<ArgumentNullException>(() => this.olv.CopyObjectsToClipboard(null));
            Assert.ThrowsExactly<ArgumentNullException>(() => new OLVDataObject(this.olv, null));
        }

        [TestMethod]
        public void Test_CopyObjectsToClipboard_SingleUseQuery() {
            IDataObject clipboard = Clipboard.GetDataObject();
            DataObject previous = null;
            if (clipboard != null) {
                previous = new DataObject();
                // Materialize before replacing the OLE clipboard; its IDataObject is not a snapshot.
                foreach (string format in clipboard.GetFormats(false))
                {
                    previous.SetData(format, false, clipboard.GetData(format, false));
                }
            }
            try {
                this.olv.SetObjects(PersonDb.All);
                this.olv.CopyObjectsToClipboard(new SingleUseEnumerable<Person>(PersonDb.All.Take(2)));
                IDataObject copied = Clipboard.GetDataObject();
                string text = (string)copied.GetData(typeof(string));
                Assert.Contains("name\toccupation\t300", text);
                Assert.Contains("name2\toccupation\t200", text);
                Assert.DoesNotContain("name4\t", text);
                Assert.Contains("<td>name2</td>", (string)copied.GetData(DataFormats.Html));
            }
            finally {
                if (previous == null)
                {
                    Clipboard.Clear();
                }
                else
                {
                    Clipboard.SetDataObject(previous, true);
                }
            }
        }

        [TestMethod]
        public void Test_TabSeparated_WithoutHeader() {
            this.olv.SetObjects(PersonDb.All);
            this.olv.SelectedObject = PersonDb.All[0];
            OLVExporter exporter = new OLVExporter(this.olv, this.olv.SelectedObjects);
            exporter.IncludeColumnHeaders = false;
            string tabSeparated = exporter.ExportTo(OLVExporter.ExportFormat.TSV);
            Assert.AreEqual(string.Join(Environment.NewLine, new[] {
                "name\toccupation\t300\tTrue\t",
                ""
            }), tabSeparated);
        }

        [TestMethod]
        public void Test_Csv_Simple() {
            this.olv.SetObjects(PersonDb.All);
            OLVExporter exporter = new OLVExporter(this.olv);
            string csv = exporter.ExportTo(OLVExporter.ExportFormat.CSV);
            Assert.AreEqual(string.Join(Environment.NewLine, new[] {
                "\"Name\",\"Occupation\",\"Culinary Rating\",\"CanTellJokes\",\"IsActive\"",
                "\"name\",\"occupation\",\"300\",\"True\",\"\"",
                "\"name2\",\"occupation\",\"200\",\"True\",\"\"",
                "\"aaa First Alphabetical Name\",\"occupation3\",\"90\",\"True\",\"\"",
                "\"name4\",\"occupation4\",\"80\",\"True\",\"\"",
                "\"name5\",\"occupation5\",\"140\",\"True\",\"\"",
                "\"name6\",\"occupation6\",\"65\",\"True\",\"\"",
                "\"name7\",\"occupation7\",\"62\",\"True\",\"\"",
                "\"zzz Last Alphabetical Name\",\"occupation6\",\"60\",\"True\",\"\"",
                ""
            }), csv);
        }

        [TestMethod]
        public void Test_Csv_EscapedValues() {
            PersonDb.All[0].Occupation = @"Some, ""value""";
            this.olv.SetObjects(PersonDb.All);
            this.olv.SelectedObject = PersonDb.All[0];
            OLVExporter exporter = new OLVExporter(this.olv, this.olv.SelectedObjects);
            string csv = exporter.ExportTo(OLVExporter.ExportFormat.CSV);
            Assert.AreEqual(string.Join(Environment.NewLine, new[] {
                "\"Name\",\"Occupation\",\"Culinary Rating\",\"CanTellJokes\",\"IsActive\"",
                "\"name\",\"Some, \"\"value\"\"\",\"300\",\"True\",\"\"",
                ""
            }), csv);
        }

        [TestMethod]
        public void Test_Html_Simple() {
            this.olv.SetObjects(PersonDb.All);
            OLVExporter exporter = new OLVExporter(this.olv);
            string html = exporter.ExportTo(OLVExporter.ExportFormat.HTML);
            Assert.AreEqual(string.Join(Environment.NewLine, new[] {
                "<table><tr><td>Name</td><td>Occupation</td><td>Culinary Rating</td><td>CanTellJokes</td><td>IsActive</td></tr>",
                "<tr><td>name</td><td>occupation</td><td>300</td><td>True</td><td></td></tr>",
                "<tr><td>name2</td><td>occupation</td><td>200</td><td>True</td><td></td></tr>",
                "<tr><td>aaa First Alphabetical Name</td><td>occupation3</td><td>90</td><td>True</td><td></td></tr>",
                "<tr><td>name4</td><td>occupation4</td><td>80</td><td>True</td><td></td></tr>",
                "<tr><td>name5</td><td>occupation5</td><td>140</td><td>True</td><td></td></tr>",
                "<tr><td>name6</td><td>occupation6</td><td>65</td><td>True</td><td></td></tr>",
                "<tr><td>name7</td><td>occupation7</td><td>62</td><td>True</td><td></td></tr>",
                "<tr><td>zzz Last Alphabetical Name</td><td>occupation6</td><td>60</td><td>True</td><td></td></tr>",
                "</table>",
                ""
            }), html);
        }

        [TestMethod]
        public void Test_Html_EscapedValues() {
            PersonDb.All[0].Occupation = @"Complex <T> "" & string";
            this.olv.SetObjects(PersonDb.All);
            this.olv.SelectedObject = PersonDb.All[0];
            OLVExporter exporter = new OLVExporter(this.olv, this.olv.SelectedObjects);
            string html = exporter.ExportTo(OLVExporter.ExportFormat.HTML);
            Assert.AreEqual(string.Join(Environment.NewLine, new[] {
                "<table><tr><td>Name</td><td>Occupation</td><td>Culinary Rating</td><td>CanTellJokes</td><td>IsActive</td></tr>",
                "<tr><td>name</td><td>Complex &lt;T&gt; &quot; &amp; string</td><td>300</td><td>True</td><td></td></tr>",
                "</table>",
                ""
            }), html);
        }

        protected ObjectListView olv;
    }
}
