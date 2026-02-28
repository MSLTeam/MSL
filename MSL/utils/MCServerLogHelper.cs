using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using static MSL.utils.LogColorizer;

namespace MSL.utils
{
    public class MCServerLogHelper
    {
        public static List<LogSegment> ParseLogSegments(string msg, Color defaultColor)
        {
            var segments = new List<LogSegment>();

            try
            {
                char delimiter = msg.Contains('&') ? '&' : msg.Contains('§') ? '§' : '\0';

                if (delimiter != '\0')
                {
                    // Minecraft & / § 颜色代码
                    int lastIndex = 0;
                    int firstDelimiterIndex = msg.IndexOf(delimiter);

                    if (firstDelimiterIndex > 0)
                        segments.Add(new LogSegment { Text = msg.Substring(0, firstDelimiterIndex), Color = defaultColor });
                    else if (firstDelimiterIndex == -1)
                    {
                        segments.Add(new LogSegment { Text = msg, Color = defaultColor });
                        return segments;
                    }

                    while ((lastIndex = msg.IndexOf(delimiter, lastIndex)) != -1)
                    {
                        int nextIndex = msg.IndexOf(delimiter, lastIndex + 1);
                        if (nextIndex == -1) nextIndex = msg.Length;

                        string segment = msg.Substring(lastIndex + 1, nextIndex - lastIndex - 1);
                        if (segment.Length > 1)
                        {
                            char code = segment[0];
                            string text = segment.Substring(1);
                            segments.Add(new LogSegment
                            {
                                Text = text,
                                Color = GetColorFromMinecraftCode(code)
                            });
                        }
                        lastIndex = nextIndex;
                        if (lastIndex >= msg.Length) break;
                    }
                }
                else if (msg.Contains("\x1B"))
                {
                    // ANSI 转义码
                    string[] parts = msg.Split(new[] { '\x1B' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        int mIndex = part.IndexOf('m');
                        if (mIndex == -1 || mIndex + 1 >= part.Length) continue;

                        string codesPart = part.Substring(0, mIndex).TrimStart('[');
                        string text = part.Substring(mIndex + 1);
                        if (string.IsNullOrEmpty(text)) continue;

                        bool isBold = false, isUnderline = false;
                        Color foreground = Colors.Green;

                        foreach (var code in codesPart.Split(';'))
                        {
                            switch (code)
                            {
                                case "0": isBold = false; isUnderline = false; foreground = Colors.Green; break;
                                case "1": isBold = true; break;
                                case "4": isUnderline = true; break;
                                default:
                                    if (_ansiColorMap.TryGetValue(code, out var c)) foreground = c;
                                    break;
                            }
                        }
                        segments.Add(new LogSegment { Text = text, Color = foreground, IsBold = isBold, IsUnderline = isUnderline });
                    }
                }
                else
                {
                    segments.Add(new LogSegment { Text = msg, Color = defaultColor });
                }
            }
            catch
            {
                segments.Clear();
                segments.Add(new LogSegment { Text = msg, Color = defaultColor });
            }

            return segments;
        }

        private static readonly Dictionary<char, Color> _mcColorMap = new()
        {
            ['0'] = Colors.Black,
            ['1'] = Colors.DarkBlue,
            ['2'] = Colors.DarkGreen,
            ['3'] = Colors.DarkCyan,
            ['4'] = Colors.DarkRed,
            ['5'] = Colors.DarkMagenta,
            ['6'] = Colors.Orange,
            ['7'] = Colors.Gray,
            ['8'] = Colors.DarkGray,
            ['9'] = Colors.Blue,
            ['a'] = Colors.Green,
            ['b'] = Colors.Cyan,
            ['c'] = Colors.Red,
            ['d'] = Colors.Magenta,
            ['e'] = Colors.Gold,
            ['f'] = Colors.White,
        };

        private static readonly Dictionary<string, Color> _ansiColorMap = new()
        {
            ["30"] = Colors.Black,
            ["31"] = Colors.Red,
            ["32"] = Colors.Green,
            ["33"] = Colors.Gold,
            ["34"] = Colors.Blue,
            ["35"] = Colors.Magenta,
            ["36"] = Colors.Cyan,
            ["37"] = Colors.White,
            ["90"] = Colors.Gray,
            ["91"] = Colors.LightPink,
            ["92"] = Colors.LightGreen,
            ["93"] = Colors.LightYellow,
            ["94"] = Colors.LightBlue,
            ["95"] = Colors.LightPink,
            ["96"] = Colors.LightCyan,
            ["97"] = Colors.White,
        };

        private static Color GetColorFromMinecraftCode(char code)
            => _mcColorMap.TryGetValue(code, out var c) ? c : Colors.Green;
    }

    public class LogColorizer : DocumentColorizingTransformer
    {
        public class LogSegment
        {
            public string Text { get; set; }
            public Color Color { get; set; }
            public bool IsBold { get; set; }
            public bool IsUnderline { get; set; }
        }
        public class LogEntry
        {
            public int StartOffset { get; set; }  // 在 Document 中的起始位置
            public List<LogSegment> Segments { get; set; } = new();
        }

        // 存储所有日志条目（offset → segments）
        private readonly List<LogEntry> _entries = new();

        public void AddEntry(LogEntry entry)
        {
            _entries.Add(entry);
        }

        public void Clear()
        {
            _entries.Clear();
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            int lineStart = line.Offset;
            int lineEnd = line.EndOffset;

            foreach (var entry in _entries)
            {
                int segOffset = entry.StartOffset;

                foreach (var seg in entry.Segments)
                {
                    int segEnd = segOffset + seg.Text.Length;

                    // 计算与当前行的交叉区域
                    int overlapStart = Math.Max(lineStart, segOffset);
                    int overlapEnd = Math.Min(lineEnd, segEnd);

                    if (overlapStart < overlapEnd)
                    {
                        var brush = new SolidColorBrush(seg.Color);
                        brush.Freeze();

                        ChangeLinePart(overlapStart, overlapEnd, element =>
                        {
                            element.TextRunProperties.SetForegroundBrush(brush);
                            if (seg.IsBold)
                            {
                                element.TextRunProperties.SetTypeface(
                                    new Typeface(
                                        element.TextRunProperties.Typeface.FontFamily,
                                        FontStyles.Normal,
                                        FontWeights.Bold,
                                        FontStretches.Normal));
                            }
                        });
                    }
                    segOffset = segEnd;
                }
            }
        }
    }
}
