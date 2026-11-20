using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using MemoBam.Models;
using MemoBam.Services;

namespace MemoBam;

public partial class DiaryDetailWindow : Window
{
    private readonly DiaryService _diaryService;

    public string EditedText => NoteTextBox.Text.Trim();
    public bool EditedIsDone => DoneCheckBox.IsChecked == true;

    public DiaryDetailWindow(DiaryEntry entry, DiaryService diaryService)
    {
        InitializeComponent();
        _diaryService = diaryService;

        TimestampTextBlock.Text = $"📅 {entry.DateFormatted}   🕒 {entry.TimeFormatted}";
        DoneCheckBox.IsChecked = entry.IsDone;
        NoteTextBox.Text = entry.Text;

        if (entry.Attachments.Count > 0)
        {
            AttachmentsSection.Visibility = Visibility.Visible;
            AttachmentsItemsControl.ItemsSource = entry.Attachments;
        }

        Loaded += (_, _) => NoteTextBox.Focus();
    }

    private void OpenAttachmentButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: DiaryAttachment attachment }) return;

        var fullPath = _diaryService.GetAttachmentFullPath(attachment);
        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
        {
            MessageBox.Show($"L'allegato non è più presente nel percorso:\n{fullPath}", "File non trovato", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = fullPath,
            UseShellExecute = true
        });
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NoteTextBox.Text))
        {
            MessageBox.Show("Il testo della nota non può essere vuoto.", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
            NoteTextBox.Focus();
            return;
        }

        DialogResult = true;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
