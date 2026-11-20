using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;

namespace MemoBam.Services;

/// <summary>
/// Helpers for WPF RichTextBox formatting (Bold, Italic, Bullets, Secondary CheckBoxes)
/// and serialization between FlowDocument XAML and plain text.
/// </summary>
public static class DiaryRichTextHelper
{
    public static void InsertSecondaryCheckBox(RichTextBox rtb)
    {
        var checkBox = new CheckBox
        {
            IsChecked = false,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 5, 0)
        };

        var caret = rtb.CaretPosition;
        var container = new InlineUIContainer(checkBox, caret);
        rtb.CaretPosition = container.ElementEnd;
        rtb.Focus();
    }

    public static string SerializeDocument(FlowDocument document)
    {
        return XamlWriter.Save(document);
    }

    public static FlowDocument LoadDocument(string? richXaml, string fallbackPlainText)
    {
        if (!string.IsNullOrWhiteSpace(richXaml))
        {
            try
            {
                if (XamlReader.Parse(richXaml) is FlowDocument parsed)
                {
                    return parsed;
                }
            }
            catch
            {
                // Fall back to plain text if XAML cannot be parsed.
            }
        }

        var doc = new FlowDocument
        {
            PagePadding = new Thickness(0)
        };
        if (!string.IsNullOrEmpty(fallbackPlainText))
        {
            foreach (var line in fallbackPlainText.Replace("\r\n", "\n").Split('\n'))
            {
                doc.Blocks.Add(new Paragraph(new Run(line)) { Margin = new Thickness(0, 0, 0, 3) });
            }
        }
        return doc;
    }

    public static string ExtractPlainText(FlowDocument document)
    {
        var sb = new StringBuilder();
        foreach (var block in document.Blocks)
        {
            AppendBlockText(block, sb);
        }
        return sb.ToString().Trim();
    }

    public static void ClearDocument(RichTextBox rtb)
    {
        rtb.Document = new FlowDocument
        {
            PagePadding = new Thickness(0)
        };
    }

    public static bool IsDocumentEmpty(FlowDocument document)
    {
        return string.IsNullOrWhiteSpace(ExtractPlainText(document));
    }

    private static void AppendBlockText(Block block, StringBuilder sb)
    {
        switch (block)
        {
            case Paragraph p:
                AppendInlinesText(p.Inlines, sb);
                sb.AppendLine();
                break;

            case List list:
                foreach (var item in list.ListItems)
                {
                    sb.Append("• ");
                    foreach (var subBlock in item.Blocks)
                    {
                        AppendBlockText(subBlock, sb);
                    }
                }
                break;

            case Section section:
                foreach (var subBlock in section.Blocks)
                {
                    AppendBlockText(subBlock, sb);
                }
                break;
        }
    }

    private static void AppendInlinesText(InlineCollection inlines, StringBuilder sb)
    {
        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case Run run:
                    sb.Append(run.Text);
                    break;
                case LineBreak:
                    sb.AppendLine();
                    break;
                case Span span:
                    AppendInlinesText(span.Inlines, sb);
                    break;
                case InlineUIContainer container when container.Child is CheckBox cb:
                    sb.Append(cb.IsChecked == true ? "[x] " : "[ ] ");
                    break;
            }
        }
    }
}
