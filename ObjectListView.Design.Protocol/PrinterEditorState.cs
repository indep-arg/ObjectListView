using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace BrightIdeasSoftware.Design
{
    [DataContract]
    internal sealed class PrinterEditorState
    {
        [DataMember] public bool HasValue;
        [DataMember] public int BrushKind;
        [DataMember] public string Color;
        [DataMember] public string SecondColor;
        [DataMember] public int Alpha;
        [DataMember] public int Pattern;
        [DataMember] public float Width;
        [DataMember] public int StartCap;
        [DataMember] public int EndCap;
        [DataMember] public int DashCap;
        [DataMember] public int DashStyle;
        [DataMember] public int LineJoin;

        public static string Encode(object value, bool pen) {
            var state = new PrinterEditorState { HasValue = value != null };
            if (pen && value != null && value.GetType() != typeof(PenData))
            {
                throw new NotSupportedException("Custom pen data requires a custom editor.");
            }

            PenData penData = pen ? (PenData)value : null;
            IBrushData brush = pen ? penData?.Brush : (IBrushData)value;
            if (brush != null && brush.GetType() != typeof(SolidBrushData) &&
                brush.GetType() != typeof(LinearGradientBrushData) && brush.GetType() != typeof(HatchBrushData))
            {
                throw new NotSupportedException("Custom and texture brush data require a custom editor.");
            }

            var converter = new ColorConverter();
            if (brush is SolidBrushData solid) {
                state.BrushKind = 1;
                state.Color = converter.ConvertToInvariantString(solid.Color);
                state.Alpha = solid.Alpha;
            } else if (brush is LinearGradientBrushData gradient) {
                state.BrushKind = 2;
                state.Color = converter.ConvertToInvariantString(gradient.FromColor);
                state.SecondColor = converter.ConvertToInvariantString(gradient.ToColor);
                state.Pattern = (int)gradient.GradientMode;
            } else if (brush is HatchBrushData hatch) {
                state.BrushKind = 3;
                state.Color = converter.ConvertToInvariantString(hatch.ForegroundColor);
                state.SecondColor = converter.ConvertToInvariantString(hatch.BackgroundColor);
                state.Pattern = (int)hatch.HatchStyle;
            } else if (brush != null) {
                throw new NotSupportedException("The brush editor supports solid, gradient, and hatch brushes.");
            }
            if (penData != null) {
                state.Width = penData.Width;
                state.StartCap = (int)penData.StartCap;
                state.EndCap = (int)penData.EndCap;
                state.DashCap = (int)penData.DashCap;
                state.DashStyle = (int)penData.DashStyle;
                state.LineJoin = (int)penData.LineJoin;
            }
            using (var text = new StringWriter())
            using (var writer = XmlWriter.Create(text)) {
                new DataContractSerializer(typeof(PrinterEditorState)).WriteObject(writer, state);
                writer.Flush();
                return text.ToString();
            }
        }

        public static object Decode(string text, bool pen) {
            PrinterEditorState state;
            using (var input = new StringReader(text))
            using (var reader = XmlReader.Create(input, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit })) {
                state = (PrinterEditorState)new DataContractSerializer(typeof(PrinterEditorState)).ReadObject(reader);
            }
            if (!state.HasValue)
            {
                return null;
            }

            var converter = new ColorConverter();
            IBrushData brush;
            switch (state.BrushKind) {
                case 0:
                    brush = null;
                    break;
                case 1:
                    brush = new SolidBrushData {
                        Color = (Color)converter.ConvertFromInvariantString(state.Color), Alpha = state.Alpha
                    };
                    break;
                case 2:
                    brush = new LinearGradientBrushData {
                        FromColor = (Color)converter.ConvertFromInvariantString(state.Color),
                        ToColor = (Color)converter.ConvertFromInvariantString(state.SecondColor),
                        GradientMode = (LinearGradientMode)state.Pattern
                    };
                    break;
                case 3:
                    brush = new HatchBrushData {
                        ForegroundColor = (Color)converter.ConvertFromInvariantString(state.Color),
                        BackgroundColor = (Color)converter.ConvertFromInvariantString(state.SecondColor),
                        HatchStyle = (HatchStyle)state.Pattern
                    };
                    break;
                default:
                    throw new SerializationException("Unknown brush editor value.");
            }
            return pen ? (object)new PenData(brush) {
                Width = state.Width, StartCap = (LineCap)state.StartCap, EndCap = (LineCap)state.EndCap,
                DashCap = (DashCap)state.DashCap, DashStyle = (DashStyle)state.DashStyle,
                LineJoin = (LineJoin)state.LineJoin
            } : brush;
        }
    }
}
