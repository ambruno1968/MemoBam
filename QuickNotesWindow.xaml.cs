using System.Windows;
using System.Windows.Controls;
using MemoBam.Models;
using MemoBam.Services;

namespace MemoBam;

public partial class QuickNotesWindow : Window
{
    private readonly DiaryService _diaryService;
    private QuickNote? _editingNote;

    public string? SelectedNoteTextToInsert { get; private set; }

    public QuickNotesWindow(DiaryService diaryService)
    {
        InitializeComponent();
        _diaryService = diaryService;
        RefreshTable();
        Loaded += (_, _) => QuickNoteInputTextBox.Focus();
    }

    private void RefreshTable()
    {
        QuickNotesDataGrid.ItemsSource = null;
        QuickNotesDataGrid.ItemsSource = _diaryService.GetQuickNotes();
    }

    private void SaveQuickNoteButton_Click(object sender, RoutedEventArgs e)
    {
        string text = QuickNoteInputTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show("Inserisci il testo della nota pronta.", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
            QuickNoteInputTextBox.Focus();
            return;
        }

        if (_editingNote is not null)
        {
            _editingNote.Text = text;
            _diaryService.UpdateQuickNote(_editingNote);
            ResetEditMode();
        }
        else
        {
            _diaryService.AddQuickNote(text);
            QuickNoteInputTextBox.Clear();
        }

        RefreshTable();
        QuickNoteInputTextBox.Focus();
    }

    private void EditQuickNoteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: QuickNote note }) return;
        _editingNote = note;
        QuickNoteInputTextBox.Text = note.Text;
        SaveQuickNoteButton.Content = "💾 Salva";
        CancelEditQuickNoteButton.Visibility = Visibility.Visible;
        QuickNoteInputTextBox.Focus();
        QuickNoteInputTextBox.SelectAll();
    }

    private void CancelEditQuickNoteButton_Click(object sender, RoutedEventArgs e)
    {
        ResetEditMode();
    }

    private void ResetEditMode()
    {
        _editingNote = null;
        QuickNoteInputTextBox.Clear();
        SaveQuickNoteButton.Content = "➕ Aggiungi";
        CancelEditQuickNoteButton.Visibility = Visibility.Collapsed;
    }

    private void DeleteQuickNoteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: QuickNote note }) return;

        var result = MessageBox.Show(
            $"Vuoi eliminare la nota pronta \"{note.Text}\"?",
            "Conferma eliminazione",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            if (_editingNote?.Id == note.Id)
            {
                ResetEditMode();
            }
            _diaryService.DeleteQuickNote(note);
            RefreshTable();
        }
    }

    private void UseQuickNoteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: QuickNote note }) return;
        SelectedNoteTextToInsert = note.Text;
        DialogResult = true;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
