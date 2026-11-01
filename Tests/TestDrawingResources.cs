using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightIdeasSoftware.Tests
{
    [STATestClass]
    public class TestDrawingResources
    {
        [TestMethod]
        public void Test_GradientBorder_PreservesCallerBrushAndReturnsToSolidFill() {
            using (var image = new Bitmap(40, 40))
            using (var graphics = Graphics.FromImage(image))
            using (var fill = new SolidBrush(Color.Lime))
            using (var border = new BorderProbe {
                BorderPen = null, FillBrush = fill, BoundsPadding = Size.Empty,
                CornerRounding = 0, FillGradientFrom = Color.Red, FillGradientTo = Color.Blue
            }) {
                border.Paint(graphics);
                Assert.AreSame(fill, border.FillBrush);
                Assert.AreNotEqual(Color.Lime.ToArgb(), image.GetPixel(20, 20).ToArgb());

                border.FillGradientFrom = null;
                border.FillGradientTo = null;
                border.Paint(graphics);
                Assert.AreEqual(Color.Lime.ToArgb(), image.GetPixel(20, 20).ToArgb());

                border.Dispose();
                graphics.FillRectangle(fill, new Rectangle(0, 0, 10, 10));
                Assert.AreEqual(Color.Lime.ToArgb(), image.GetPixel(5, 5).ToArgb());
            }
        }

        [TestMethod]
        public void Test_TextAdornment_ReleasesGeneratedFormatButPreservesAssignedResources() {
            using (var font = new Font("Tahoma", 12))
            using (var format = new StringFormat())
            using (var text = new TextAdornment()) {
                StringFormat generated = text.StringFormat;
                Font generatedFont = text.FontOrDefault;
                Assert.AreSame(generatedFont, text.FontOrDefault);
                text.StringFormat = format;
                text.Font = font;

                Assert.ThrowsExactly<ArgumentException>(() => {
                    using (var clone = (StringFormat)generated.Clone()) { }
                });
                text.Dispose();
                Assert.ThrowsExactly<ArgumentException>(() => {
                    using (var clone = (Font)generatedFont.Clone()) { }
                });
                using (var fontClone = (Font)font.Clone())
                using (var formatClone = (StringFormat)format.Clone()) {
                    Assert.AreEqual(font.Size, fontClone.Size);
                    Assert.AreEqual(format.Alignment, formatClone.Alignment);
                }
            }
        }

        [TestMethod]
        public void Test_ControlDisposal_ReleasesGeneratedOverlayButNotAssignedOverlay() {
            using (var custom = new TextOverlay())
            using (var list = new ObjectListView()) {
                TextOverlay generated = list.OverlayText;
                StringFormat generatedFormat = generated.StringFormat;
                list.OverlayText = custom;
                list.Dispose();
                Assert.ThrowsExactly<ArgumentException>(() => {
                    using (var clone = (StringFormat)generatedFormat.Clone()) { }
                });
                using (var clone = (StringFormat)custom.StringFormat.Clone())
                {
                    Assert.AreEqual(custom.StringFormat.Alignment, clone.Alignment);
                }
            }
        }

        [TestMethod]
        public void Test_ControlOwnedDropSink_IsDisposedOnReplacementAndControlDisposal() {
            using (var list = new ObjectListView()) {
                list.IsSimpleDropSink = true;
                var first = (SimpleDropSink)list.DropSink;
                list.IsSimpleDropSink = true;
                Assert.ThrowsExactly<ObjectDisposedException>(() => first.ListView = list);
                var second = (SimpleDropSink)list.DropSink;
                list.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(() => second.ListView = list);
            }
        }

        [TestMethod]
        public void Test_AssignedDropSink_CanBeReusedAfterDetaching() {
            using (var sink = new SimpleDropSink())
            using (var first = new ObjectListView())
            using (var second = new ObjectListView()) {
                first.DropSink = sink;
                first.DropSink = null;
                Assert.IsNull(sink.ListView);
                first.Dispose();
                second.DropSink = sink;
                Assert.AreSame(second, sink.ListView);
                second.Dispose();
                Assert.IsNull(sink.ListView);
            }
        }

        [TestMethod]
        [DataRow(false, 0)]
        [DataRow(false, 1)]
        [DataRow(false, 2)]
        [DataRow(false, 3)]
        [DataRow(true, 0)]
        [DataRow(true, 1)]
        [DataRow(true, 2)]
        [DataRow(true, 3)]
        public void Test_DropHandler_CanDetachReplaceOrDisposeSink(bool modelDrop, int action) {
            using (ObjectListView list = new ObjectListView { FullRowSelect = true })
            using (SimpleDropSink replacement = new SimpleDropSink()) {
                list.IsSimpleDropSink = true;
                SimpleDropSink sink = (SimpleDropSink)list.DropSink;
                DragEventArgs args = BeginDrop(list, sink, modelDrop);
                Assert.IsFalse(list.FullRowSelect);
                bool handled = false;
                Action changeSink = () => {
                    handled = true;
                    switch (action) {
                        case 0:
                            list.DropSink = null;
                            break;
                        case 1:
                            list.DropSink = replacement;
                            break;
                        case 2:
                            list.Dispose();
                            break;
                        case 3:
                            sink.Dispose();
                            break;
                    }
                    Assert.IsTrue(list.FullRowSelect);
                };
                if (modelDrop) {
                    list.ModelDropped += (sender, e) => {
                        e.Handled = true;
                        changeSink();
                    };
                } else {
                    list.Dropped += (sender, e) => changeSink();
                }

                sink.Drop(args);
                Assert.IsTrue(handled);
                Assert.IsTrue(list.FullRowSelect);
                Assert.AreEqual(DropTargetLocation.None, sink.DropTargetLocation);
            }
        }

        [TestMethod]
        public void Test_DropHandler_ReattachesSinkWithoutChangingNewControlSelection() {
            using (SimpleDropSink sink = new SimpleDropSink())
            using (ObjectListView first = new ObjectListView { FullRowSelect = true })
            using (ObjectListView second = new ObjectListView { FullRowSelect = false }) {
                first.DropSink = sink;
                DragEventArgs args = BeginDrop(first, sink, false);
                first.Dropped += (sender, e) => {
                    first.DropSink = null;
                    second.DropSink = sink;
                };

                sink.Drop(args);
                Assert.IsTrue(first.FullRowSelect);
                Assert.IsFalse(second.FullRowSelect);
                Assert.AreSame(second, sink.ListView);
            }
        }

        [TestMethod]
        public void Test_DropHandler_ExceptionRestoresSelectionAndPropagates() {
            using (ObjectListView list = new ObjectListView { FullRowSelect = true }) {
                list.IsSimpleDropSink = true;
                SimpleDropSink sink = (SimpleDropSink)list.DropSink;
                DragEventArgs args = BeginDrop(list, sink, false);
                list.Dropped += (sender, e) => throw new InvalidOperationException("Drop failed.");

                Assert.ThrowsExactly<InvalidOperationException>(() => sink.Drop(args));
                Assert.IsTrue(list.FullRowSelect);
                Assert.AreEqual(DropTargetLocation.None, sink.DropTargetLocation);
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Test_DisposingInactiveSink_PreservesFullRowSelection(bool fullRowSelect) {
            using (SimpleDropSink sink = new SimpleDropSink())
            using (ObjectListView list = new ObjectListView { FullRowSelect = fullRowSelect }) {
                list.DropSink = sink;
                sink.Dispose();
                Assert.AreEqual(fullRowSelect, list.FullRowSelect);
            }
        }

        [TestMethod]
        public void Test_TextSprite_UsesFallbackFontAndPreservesAssignedFont() {
            using (var sprite = new TextSprite("Text") { Scale = 2 })
            using (var font = new Font("Tahoma", 12)) {
                Assert.IsGreaterThan(0, sprite.Size.Width);
                sprite.Font = font;
                Assert.IsGreaterThan(0, sprite.Size.Width);
                sprite.Dispose();
                using (var clone = (Font)font.Clone())
                {
                    Assert.AreEqual(font.Size, clone.Size);
                }
            }
        }

        [TestMethod]
        public void Test_AnimationDisposal_DoesNotRepeatStoppedNotification() {
            using (var animation = new Animation { Interval = 60000 }) {
                int stopped = 0;
                animation.Stopped += delegate {
                    stopped++;
                    animation.Dispose();
                };
                animation.Start();
                animation.Stop();
                Assert.AreEqual(1, stopped);
                Assert.IsFalse(animation.Running);
                Assert.ThrowsExactly<ObjectDisposedException>(() => animation.Start());
            }
        }

        [TestMethod]
        public void Test_AnimationAdapter_ReleasesAnimationWhenIdleControlIsDisposed() {
            using (var panel = new Panel())
            using (var adapter = new AnimationAdapter(panel)) {
                panel.Dispose();
                Assert.ThrowsExactly<ObjectDisposedException>(() => adapter.Animation.Start());
            }
        }

        private static DragEventArgs BeginDrop(ObjectListView list, SimpleDropSink sink, bool modelDrop) {
            list.View = View.Details;
            list.ShowGroups = false;
            list.Columns.Add(new OLVColumn("Name", "Name"));
            list.SetObjects(new[] { new { Name = "Row" } });
            sink.CanDropOnBackground = true;
            sink.AutoScroll = false;
            Point point = list.PointToScreen(new Point(5, list.ClientSize.Height - 5));
            IDataObject data = modelDrop ? new OLVDataObject(list, list.ModelObjects) : new DataObject();
            DragEventArgs args = new DragEventArgs(data, 1, point.X, point.Y,
                DragDropEffects.Copy | DragDropEffects.Move, DragDropEffects.Copy);
            sink.Enter(args);
            sink.DropTargetLocation = DropTargetLocation.Background;
            return args;
        }

        private sealed class BorderProbe : BorderDecoration
        {
            public void Paint(Graphics graphics) {
                this.DrawFilledBorder(graphics, new Rectangle(5, 5, 30, 30));
            }
        }
    }
}
