using System.Windows;

namespace MemoBam;

public partial class EditItemWindow : Window
{
    public string EditedTitle => TitleTextBox.Text.Trim();
    public string EditedNotes => NotesTextBox.Text.Trim();

    public EditItemWindow(string currentTitle, string currentNotes)
    {
        InitializeComponent();
        TitleTextBox.Text = currentTitle;
        NotesTextBox.Text = currentNotes;
        Loaded += (_, _) =>
        {
            TitleTextBox.Focus();
            TitleTextBox.SelectAll();
        };
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
        {
            MessageBox.Show("Il titolo non può essere vuoto.", "Attenzione", MessageBoxButton.OK, MessageBoxImage.Warning);
            TitleTextBox.Focus();
            return;
        }

        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
