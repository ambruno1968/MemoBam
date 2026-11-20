using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MemoBam.Models;
using MemoBam.Services;
using Microsoft.Win32;

namespace MemoBam;

public partial class MainWindow : Window
{
    private ArchiveService _archiveService;
    private DiaryService _diaryService;
    private readonly DispatcherTimer _autoHideTimer;
    private readonly DispatcherTimer _hoverExpandTimer;
    private bool _isCollapsed;
    private bool _isMiniDropMode;
    private bool _isDialogOpen;
    private bool _isProgrammaticMove;
    private double _anchoredLeft;
    private double _anchoredTop;
    private double _expandedWidth = 1120;
    private double _expandedHeight = 720;

    private const double CollapsedStripThickness = 22;
    private const double MiniDropWidth = 270;
    private const double MiniDropHeight = 165;
    private const byte BackgroundIdleAlpha = 175;

    private static readonly Brush DropDefaultBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC"));
    private static readonly Brush DropDefaultBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));
    private static readonly Brush DropActiveBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF"));
    private static readonly Brush DropActiveBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));

    private bool IsAutoHideActive =>
        AutoHideToggle.IsChecked == true && DockNormalRadio.IsChecked != true;

    private bool IsBackgroundMiniModeActive =>
        DesktopModeToggle.IsChecked == true && DockNormalRadio.IsChecked == true;

    public MainWindow()
    {
        InitializeComponent();
        var settings = SettingsService.Load();
        _archiveService = new ArchiveService(settings.ArchiveRootPath);
        _diaryService = new DiaryService(_archiveService.BaseDirectory);

        var infoVer = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
            ?? "1.0.0";
        VersionTextBlock.Text = $"v{infoVer.Split('+')[0]}";

        FixedPathTextBlock.Text = $"Percorso: {_archiveService.BaseDirectory}";
        FixedPathTextBlock.ToolTip = _archiveService.BaseDirectory;
        DiaryDatePicker.SelectedDate = DateTime.Today;

        _autoHideTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(550)
        };
        _autoHideTimer.Tick += AutoHideTimer_Tick;

        _hoverExpandTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(450)
        };
        _hoverExpandTimer.Tick += (_, _) =>
        {
            _hoverExpandTimer.Stop();
            if (_isMiniDropMode && IsMouseOver)
            {
                ExpandFromMiniDrop();
            }
        };

        LocationChanged += (_, _) =>
        {
            if (!_isProgrammaticMove && IsLoaded && WindowState == WindowState.Normal && !_isCollapsed)
            {
                _anchoredLeft = Left;
                _anchoredTop = Top;
            }
        };

        MouseEnter += (_, _) =>
        {
            _autoHideTimer.Stop();
            if (_isCollapsed)
            {
                ExpandFromAutoHide();
            }
        };

        MouseLeave += (_, _) =>
        {
            if (!_isDialogOpen && ((IsAutoHideActive && !_isCollapsed) || (IsBackgroundMiniModeActive && !_isMiniDropMode)))
            {
                _autoHideTimer.Start();
            }
        };

        Deactivated += (_, _) =>
        {
            if (!_isDialogOpen && !IsMouseOver)
            {
                if (IsAutoHideActive && !_isCollapsed)
                {
                    _autoHideTimer.Start();
                }
                else if (IsBackgroundMiniModeActive && !_isMiniDropMode)
                {
                    CollapseToMiniDrop();
                }
            }
        };

        RefreshCardsList();
        RefreshDiaryList();
    }

    #region Custom Window Chrome Controls

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && DockNormalRadio.IsChecked == true)
        {
            MaximizeButton_Click(sender, e);
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed && DockNormalRadio.IsChecked == true)
        {
            DragMove();
            _anchoredLeft = Left;
            _anchoredTop = Top;
        }
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (DockNormalRadio.IsChecked != true)
        {
            DockNormalRadio.IsChecked = true;
            return;
        }

        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    #endregion

    #region Auto-Hide & Background Mini Drop Mode

    private void AutoHideTimer_Tick(object? sender, EventArgs e)
    {
        _autoHideTimer.Stop();
        if (_isDialogOpen || IsMouseOver) return;

        if (IsAutoHideActive && !_isCollapsed)
        {
            CollapseToEdge();
        }
        else if (IsBackgroundMiniModeActive && !_isMiniDropMode)
        {
            CollapseToMiniDrop();
        }
    }

    private void Window_PreviewDragEnter(object sender, DragEventArgs e)
    {
        _autoHideTimer.Stop();
        if (_isCollapsed)
        {
            ExpandFromAutoHide();
        }
        else if (_isMiniDropMode)
        {
            ExpandFromMiniDrop();
        }
    }

    private void CollapsedStrip_MouseEnter(object sender, MouseEventArgs e)
    {
        _autoHideTimer.Stop();
        if (_isCollapsed)
        {
            ExpandFromAutoHide();
        }
    }

    private void CollapsedStrip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _autoHideTimer.Stop();
        if (_isCollapsed)
        {
            ExpandFromAutoHide();
        }
    }

    private void MiniDropOverlay_MouseEnter(object sender, MouseEventArgs e)
    {
        _autoHideTimer.Stop();
        if (_isMiniDropMode)
        {
            WindowBackdropHelper.SetWindowOpacity(this, 255);
            _hoverExpandTimer.Start();
        }
    }

    private void MiniDropOverlay_MouseLeave(object sender, MouseEventArgs e)
    {
        _hoverExpandTimer.Stop();
        if (_isMiniDropMode)
        {
            WindowBackdropHelper.SetWindowOpacity(this, BackgroundIdleAlpha);
        }
    }

    private void MiniDropOverlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _hoverExpandTimer.Stop();
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
            _anchoredLeft = Left;
            _anchoredTop = Top;
        }
    }

    private void MiniDropOpenButton_Click(object sender, RoutedEventArgs e)
    {
        _hoverExpandTimer.Stop();
        if (_isMiniDropMode)
        {
            ExpandFromMiniDrop();
        }
    }

    private void AutoHideToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;

        if (AutoHideToggle.IsChecked == true && DockNormalRadio.IsChecked == true)
        {
            DockRightRadio.IsChecked = true;
            return;
        }

        UpdateWindowChromeForDockMode();
        if (!IsAutoHideActive && _isCollapsed)
        {
            ExpandFromAutoHide();
        }
        else if (IsAutoHideActive && !IsMouseOver)
        {
            _autoHideTimer.Start();
        }
    }

    private void DockMode_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;

        _autoHideTimer.Stop();
        _isMiniDropMode = false;
        MiniDropOverlay.Visibility = Visibility.Collapsed;
        WindowBackdropHelper.SetWindowOpacity(this, 255);

        if (DockRightRadio.IsChecked == true)
        {
            DesktopModeToggle.IsChecked = false;
            AutoHideToggle.IsChecked = true;
        }
        else if (DockNormalRadio.IsChecked == true)
        {
            AutoHideToggle.IsChecked = false;
        }
        else
        {
            DesktopModeToggle.IsChecked = false;
        }

        UpdateWindowChromeForDockMode();
        ApplyExpandedDockGeometry();
    }

    private void UpdateWindowChromeForDockMode()
    {
        if (IsAutoHideActive)
        {
            ResizeMode = ResizeMode.NoResize;
            Topmost = true;
        }
        else if (DockNormalRadio.IsChecked != true)
        {
            ResizeMode = ResizeMode.NoResize;
            Topmost = false;
        }
        else if (IsBackgroundMiniModeActive && _isMiniDropMode)
        {
            ResizeMode = ResizeMode.NoResize;
            Topmost = false;
        }
        else
        {
            ResizeMode = ResizeMode.CanResize;
            Topmost = false;
        }
    }

    private void ApplyExpandedDockGeometry()
    {
        _isCollapsed = false;
        _isMiniDropMode = false;
        CollapsedStripBorder.Visibility = Visibility.Collapsed;
        MiniDropOverlay.Visibility = Visibility.Collapsed;
        RootContentGrid.Visibility = Visibility.Visible;
        WindowBackdropHelper.SetWindowOpacity(this, 255);

        var workArea = WindowBackdropHelper.GetCurrentMonitorWorkArea(this);
        WindowState = WindowState.Normal;

        _isProgrammaticMove = true;
        try
        {
            if (DockBottomRadio.IsChecked == true)
            {
                ApplyHorizontalLayout(compactDropZone: true);
                double barHeight = Math.Min(330, workArea.Height * 0.42);
                Left = workArea.Left;
                Width = workArea.Width;
                Height = barHeight;
                Top = workArea.Bottom - barHeight;
            }
            else if (DockLeftRadio.IsChecked == true)
            {
                ApplyVerticalSidebarLayout();
                double barWidth = Math.Min(440, workArea.Width * 0.36);
                Left = workArea.Left;
                Top = workArea.Top;
                Width = barWidth;
                Height = workArea.Height;
            }
            else if (DockRightRadio.IsChecked == true)
            {
                ApplyVerticalSidebarLayout();
                double barWidth = Math.Min(440, workArea.Width * 0.36);
                Left = workArea.Right - barWidth;
                Top = workArea.Top;
                Width = barWidth;
                Height = workArea.Height;
            }
            else
            {
                ApplyHorizontalLayout(compactDropZone: false);
                Width = _expandedWidth;
                Height = _expandedHeight;
                Left = workArea.Left + (workArea.Width - Width) / 2;
                Top = workArea.Top + (workArea.Height - Height) / 2;
            }
        }
        finally
        {
            _isProgrammaticMove = false;
        }
    }

    private void CollapseToEdge()
    {
        if (!IsAutoHideActive || _isCollapsed) return;

        var workArea = WindowBackdropHelper.GetCurrentMonitorWorkArea(this);
        _isCollapsed = true;
        RootContentGrid.Visibility = Visibility.Collapsed;
        MiniDropOverlay.Visibility = Visibility.Collapsed;
        CollapsedStripBorder.Visibility = Visibility.Visible;

        _isProgrammaticMove = true;
        try
        {
            if (DockRightRadio.IsChecked == true)
            {
                CollapsedStripLabel.Text = "◀ MemoBam";
                CollapsedStripLabel.LayoutTransform = new RotateTransform(-90);
                Left = workArea.Right - CollapsedStripThickness;
                Top = workArea.Top;
                Width = CollapsedStripThickness;
                Height = workArea.Height;
            }
            else if (DockLeftRadio.IsChecked == true)
            {
                CollapsedStripLabel.Text = "MemoBam ▶";
                CollapsedStripLabel.LayoutTransform = new RotateTransform(-90);
                Left = workArea.Left;
                Top = workArea.Top;
                Width = CollapsedStripThickness;
                Height = workArea.Height;
            }
            else if (DockBottomRadio.IsChecked == true)
            {
                CollapsedStripLabel.Text = "▲ MemoBam";
                CollapsedStripLabel.LayoutTransform = Transform.Identity;
                Left = workArea.Left;
                Width = workArea.Width;
                Height = CollapsedStripThickness;
                Top = workArea.Bottom - CollapsedStripThickness;
            }
        }
        finally
        {
            _isProgrammaticMove = false;
        }
    }

    private void ExpandFromAutoHide()
    {
        _autoHideTimer.Stop();
        ApplyExpandedDockGeometry();
    }

    private void DesktopModeToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;

        if (DesktopModeToggle.IsChecked == true)
        {
            if (DockNormalRadio.IsChecked != true)
            {
                DockNormalRadio.IsChecked = true;
            }

            _anchoredLeft = Left;
            _anchoredTop = Top;
            _expandedWidth = Width >= 500 ? Width : 1120;
            _expandedHeight = Height >= 400 ? Height : 720;

            if (!IsMouseOver)
            {
                CollapseToMiniDrop();
            }
            else
            {
                _autoHideTimer.Start();
            }
        }
        else
        {
            _autoHideTimer.Stop();
            if (_isMiniDropMode)
            {
                ExpandFromMiniDrop();
            }
            WindowBackdropHelper.SetWindowOpacity(this, 255);
        }
    }

    private void CollapseToMiniDrop()
    {
        if (!IsBackgroundMiniModeActive || _isMiniDropMode) return;

        _hoverExpandTimer.Stop();
        if (Width >= 500 && Height >= 400)
        {
            _anchoredLeft = Left;
            _anchoredTop = Top;
            _expandedWidth = Width;
            _expandedHeight = Height;
        }

        _isMiniDropMode = true;
        RootContentGrid.Visibility = Visibility.Collapsed;
        CollapsedStripBorder.Visibility = Visibility.Collapsed;
        MiniDropOverlay.Visibility = Visibility.Visible;

        ResizeMode = ResizeMode.NoResize;
        Topmost = false;

        _isProgrammaticMove = true;
        try
        {
            Left = _anchoredLeft;
            Top = _anchoredTop;
            Width = MiniDropWidth;
            Height = MiniDropHeight;
        }
        finally
        {
            _isProgrammaticMove = false;
        }

        WindowBackdropHelper.SetWindowOpacity(this, BackgroundIdleAlpha);
        WindowBackdropHelper.SendToBottom(this);
    }

    private void ExpandFromMiniDrop()
    {
        _autoHideTimer.Stop();
        _hoverExpandTimer.Stop();
        if (!_isMiniDropMode) return;

        _isMiniDropMode = false;
        MiniDropOverlay.Visibility = Visibility.Collapsed;
        CollapsedStripBorder.Visibility = Visibility.Collapsed;
        RootContentGrid.Visibility = Visibility.Visible;

        ResizeMode = ResizeMode.CanResize;
        WindowBackdropHelper.SetWindowOpacity(this, 255);

        var workArea = WindowBackdropHelper.GetCurrentMonitorWorkArea(this);
        _isProgrammaticMove = true;
        try
        {
            Width = _expandedWidth;
            Height = _expandedHeight;
            Left = Math.Max(workArea.Left, Math.Min(_anchoredLeft, workArea.Right - Width));
            Top = Math.Max(workArea.Top, Math.Min(_anchoredTop, workArea.Bottom - Height));
        }
        finally
        {
            _isProgrammaticMove = false;
        }

        Activate();
    }

    private void ApplyHorizontalLayout(bool compactDropZone)
    {
        LeftColumnDef.Width = new GridLength(320);
        SpacerColumnDef.Width = new GridLength(12);
        RightColumnDef.Width = new GridLength(1, GridUnitType.Star);

        TopRowDef.Height = new GridLength(1, GridUnitType.Star);
        SpacerRowDef.Height = new GridLength(0);
        BottomRowDef.Height = new GridLength(0);

        Grid.SetColumn(DropPanelBorder, 0);
        Grid.SetRow(DropPanelBorder, 0);
        Grid.SetColumn(ArchivePanelGrid, 2);
        Grid.SetRow(ArchivePanelGrid, 0);

        OptionalMetadataPanel.Visibility = compactDropZone ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ApplyVerticalSidebarLayout()
    {
        LeftColumnDef.Width = new GridLength(1, GridUnitType.Star);
        SpacerColumnDef.Width = new GridLength(0);
        RightColumnDef.Width = new GridLength(0);

        TopRowDef.Height = GridLength.Auto;
        SpacerRowDef.Height = new GridLength(10);
        BottomRowDef.Height = new GridLength(1, GridUnitType.Star);

        Grid.SetColumn(DropPanelBorder, 0);
        Grid.SetRow(DropPanelBorder, 0);
        Grid.SetColumn(ArchivePanelGrid, 0);
        Grid.SetRow(ArchivePanelGrid, 2);

        OptionalMetadataPanel.Visibility = Visibility.Collapsed;
    }

    #endregion

    #region Section Tabs & Archive

    private void SectionTab_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;

        bool isDiary = DiaryTabRadio.IsChecked == true;
        ArchiveTabContent.Visibility = isDiary ? Visibility.Collapsed : Visibility.Visible;
        DiaryTabContent.Visibility = isDiary ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshCardsList()
    {
        var allItems = _archiveService.GetAllItems();
        TotalCountBadge.Text = allItems.Count == 1 ? "1 scheda" : $"{allItems.Count} schede";

        var filtered = _archiveService.Search(SearchTextBox.Text, FilterDatePicker.SelectedDate);
        CardsListBox.ItemsSource = filtered;

        ResultsSummaryTextBlock.Text = filtered.Count == 1 ? "1 scheda trovata" : $"{filtered.Count} schede trovate";
        EmptyStateTextBlock.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        RefreshPendingAlerts();
    }

    private void ArchiveDroppedOrSelectedPaths(string[] paths)
    {
        if (paths.Length == 0) return;

        try
        {
            bool moveFiles = MoveFilesCheckBox.IsChecked == true;

            if (DiaryTabRadio.IsChecked == true)
            {
                var targetEntry = DiaryListBox.SelectedItem as DiaryEntry
                    ?? (DiaryListBox.Items.Count > 0 ? DiaryListBox.Items[0] as DiaryEntry : null);

                if (targetEntry is null)
                {
                    string fallbackText = !string.IsNullOrWhiteSpace(CustomTitleTextBox.Text)
                        ? CustomTitleTextBox.Text.Trim()
                        : $"Allegati ({string.Join(", ", paths.Select(Path.GetFileName))})";
                    targetEntry = _diaryService.AddEntry(fallbackText, DiaryDatePicker.SelectedDate ?? DateTime.Today);
                    CustomTitleTextBox.Clear();
                }

                AttachFilesToDiaryEntry(targetEntry, paths, moveFiles);
                return;
            }

            var added = _archiveService.AddPaths(
                paths,
                moveFiles,
                CustomTitleTextBox.Text,
                CustomNotesTextBox.Text);

            if (added.Count == 0)
            {
                ShowStatus("Nessun file o cartella valido trovato.", isError: true);
                return;
            }

            CustomTitleTextBox.Clear();
            CustomNotesTextBox.Clear();
            RefreshCardsList();

            if (added.Count == 1)
            {
                var item = added[0];
                ShowStatus($"Archiviato: \"{item.Title}\" ({item.DateFormatted} {item.TimeFormatted})", isError: false);
            }
            else
            {
                ShowStatus($"Archiviati {added.Count} elementi ({DateTime.Now:dd/MM/yyyy HH:mm:ss})", isError: false);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Errore durante l'archiviazione: {ex.Message}", isError: true);
        }
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusBanner.Visibility = Visibility.Visible;
        StatusBanner.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isError ? "#FEF2F2" : "#ECFDF5"));
        StatusBanner.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isError ? "#FECACA" : "#A7F3D0"));
        StatusTextBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isError ? "#991B1B" : "#065F46"));
        StatusTextBlock.Text = message;
    }

    private void DropZone_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = MoveFilesCheckBox.IsChecked == true ? DragDropEffects.Move : DragDropEffects.Copy;
            DropZoneBorder.Background = DropActiveBg;
            DropZoneBorder.BorderBrush = DropActiveBorder;
            DropZoneTitle.Text = "Rilascia per archiviare";
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void DropZone_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = MoveFilesCheckBox.IsChecked == true ? DragDropEffects.Move : DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void DropZone_DragLeave(object sender, DragEventArgs e)
    {
        ResetDropZoneAppearance();
    }

    private void DropZone_Drop(object sender, DragEventArgs e)
    {
        ResetDropZoneAppearance();

        if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
            e.Data.GetData(DataFormats.FileDrop) is string[] files)
        {
            ArchiveDroppedOrSelectedPaths(files);
        }
        e.Handled = true;
    }

    private void ResetDropZoneAppearance()
    {
        DropZoneBorder.Background = DropDefaultBg;
        DropZoneBorder.BorderBrush = DropDefaultBorder;
        DropZoneTitle.Text = "Trascina qui i documenti";
    }

    private void BrowseFilesButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Seleziona documenti da archiviare in MemoBam",
            Multiselect = true
        };

        _isDialogOpen = true;
        try
        {
            if (dialog.ShowDialog() == true)
            {
                ArchiveDroppedOrSelectedPaths(dialog.FileNames);
            }
        }
        finally
        {
            _isDialogOpen = false;
        }
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshCardsList();

    private void FilterDatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e) => RefreshCardsList();

    private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
    {
        SearchTextBox.Clear();
        FilterDatePicker.SelectedDate = null;
        RefreshCardsList();
    }

    private void ChangeArchivePathButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Seleziona la cartella principale di MemoBam",
            InitialDirectory = Directory.Exists(_archiveService.BaseDirectory)
                ? _archiveService.BaseDirectory
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };

        _isDialogOpen = true;
        try
        {
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
            {
                string newPath = dialog.FolderName;
                SettingsService.Save(new AppSettings { ArchiveRootPath = newPath });

                _archiveService = new ArchiveService(newPath);
                _diaryService = new DiaryService(newPath);

                FixedPathTextBlock.Text = $"Percorso: {_archiveService.BaseDirectory}";
                FixedPathTextBlock.ToolTip = _archiveService.BaseDirectory;

                RefreshCardsList();
                RefreshDiaryList();
                ShowStatus($"Percorso aggiornato: {newPath}", isError: false);
            }
        }
        finally
        {
            _isDialogOpen = false;
        }
    }

    private void OpenArchiveFolderButton_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(_archiveService.BaseDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = _archiveService.BaseDirectory,
            UseShellExecute = true
        });
    }

    private void CardsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (CardsListBox.SelectedItem is ArchiveItem item)
        {
            OpenItem(item);
        }
    }

    private void OpenItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ArchiveItem item })
        {
            OpenItem(item);
        }
    }

    private void OpenItem(ArchiveItem item)
    {
        var fullPath = _archiveService.GetFullPath(item);
        if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
        {
            MessageBox.Show($"Il file non è più presente nel percorso:\n{fullPath}", "File non trovato", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = fullPath,
            UseShellExecute = true
        });
    }

    private void RevealItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ArchiveItem item }) return;

        var fullPath = _archiveService.GetFullPath(item);
        if (File.Exists(fullPath) || Directory.Exists(fullPath))
        {
            Process.Start("explorer.exe", $"/select,\"{fullPath}\"");
        }
        else
        {
            OpenArchiveFolderButton_Click(sender, e);
        }
    }

    private void EditItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ArchiveItem item }) return;

        var dialog = new EditItemWindow(item.Title, item.Notes)
        {
            Owner = this
        };

        _isDialogOpen = true;
        try
        {
            if (dialog.ShowDialog() == true)
            {
                item.Title = dialog.EditedTitle;
                item.Notes = dialog.EditedNotes;
                _archiveService.UpdateItem(item);
                RefreshCardsList();
            }
        }
        finally
        {
            _isDialogOpen = false;
        }
    }

    private void DeleteItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ArchiveItem item }) return;

        _isDialogOpen = true;
        try
        {
            var result = MessageBox.Show(
                $"Vuoi eliminare la scheda \"{item.Title}\"?\n\nScegli 'Sì' per eliminare sia la scheda che il file fisico archiviato.\nScegli 'No' per annullare.",
                "Conferma eliminazione",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    _archiveService.DeleteItem(item, deletePhysicalFile: true);
                    RefreshCardsList();
                    ShowStatus($"Scheda \"{item.Title}\" eliminata.", isError: false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Impossibile eliminare il file: {ex.Message}", "Errore", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        finally
        {
            _isDialogOpen = false;
        }
    }

    #endregion

    #region Daily Diary

    private void RefreshDiaryList()
    {
        int todayCount = _diaryService.GetTodayCount();
        DiaryTabLabel.Text = $"📓 Diario Giornaliero ({todayCount})";

        var entries = _diaryService.GetEntries(DiaryDatePicker.SelectedDate, DiarySearchTextBox.Text);
        DiaryListBox.ItemsSource = entries;

        string dayLabel = DiaryDatePicker.SelectedDate.HasValue
            ? $"del {DiaryDatePicker.SelectedDate.Value:dd/MM/yyyy}"
            : "in tutti i giorni";

        DiarySummaryTextBlock.Text = entries.Count == 1
            ? $"1 attività registrata {dayLabel}"
            : $"{entries.Count} attività registrate {dayLabel}";

        DiaryEmptyTextBlock.Visibility = entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        RefreshPendingAlerts();
    }

    private void AddDiaryEntryButton_Click(object sender, RoutedEventArgs e)
    {
        RecordDiaryInput();
    }

    private void DiaryListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DiaryListBox.SelectedItem is DiaryEntry entry)
        {
            OpenDiaryEntryDetail(entry);
        }
    }

    private void OpenDiaryEntryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: DiaryEntry entry })
        {
            OpenDiaryEntryDetail(entry);
        }
    }

    private void OpenDiaryEntryDetail(DiaryEntry entry)
    {
        var dialog = new DiaryDetailWindow(entry, _diaryService)
        {
            Owner = this
        };

        _isDialogOpen = true;
        try
        {
            if (dialog.ShowDialog() == true)
            {
                entry.Text = dialog.EditedText;
                entry.IsDone = dialog.EditedIsDone;
                _diaryService.UpdateEntry(entry);
                RefreshDiaryList();
            }
        }
        finally
        {
            _isDialogOpen = false;
        }
    }

    private void RecordDiaryInput()
    {
        string text = DiaryInputTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        var entry = _diaryService.AddEntry(text, DiaryDatePicker.SelectedDate ?? DateTime.Today);
        DiaryInputTextBox.Clear();
        RefreshDiaryList();
        ShowStatus($"Diario registrato ({entry.DateFormatted} {entry.TimeFormatted})", isError: false);
    }

    private void DiaryDatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RefreshDiaryList();
    }

    private void DiaryTodayButton_Click(object sender, RoutedEventArgs e)
    {
        DiaryDatePicker.SelectedDate = DateTime.Today;
        DiarySearchTextBox.Clear();
        RefreshDiaryList();
    }

    private void DiaryAllDaysButton_Click(object sender, RoutedEventArgs e)
    {
        DiaryDatePicker.SelectedDate = null;
        RefreshDiaryList();
    }

    private void DiarySearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RefreshDiaryList();
    }

    private void OpenDiaryFolderButton_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(_diaryService.DiaryDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = _diaryService.DiaryDirectory,
            UseShellExecute = true
        });
    }

    private void AttachFilesToDiaryEntry(DiaryEntry entry, IEnumerable<string> paths, bool moveFiles)
    {
        var added = _diaryService.AddAttachments(entry, paths, moveFiles);
        if (added.Count == 0)
        {
            ShowStatus("Nessun file o cartella valido trovato.", isError: true);
            return;
        }

        RefreshDiaryList();
        ShowStatus(
            added.Count == 1
                ? $"Allegato \"{added[0].FileName}\" collegato alla riga del diario."
                : $"Collegati {added.Count} allegati alla riga del diario.",
            isError: false);
    }

    private void DiaryRow_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = MoveFilesCheckBox.IsChecked == true ? DragDropEffects.Move : DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void DiaryRow_Drop(object sender, DragEventArgs e)
    {
        if (sender is FrameworkElement { Tag: DiaryEntry entry } &&
            e.Data.GetDataPresent(DataFormats.FileDrop) &&
            e.Data.GetData(DataFormats.FileDrop) is string[] files)
        {
            try
            {
                AttachFilesToDiaryEntry(entry, files, MoveFilesCheckBox.IsChecked == true);
            }
            catch (Exception ex)
            {
                ShowStatus($"Errore allegato diario: {ex.Message}", isError: true);
            }
        }
        e.Handled = true;
    }

    private void AttachToDiaryEntryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: DiaryEntry entry }) return;

        var dialog = new OpenFileDialog
        {
            Title = "Seleziona file da allegare alla riga del diario",
            Multiselect = true
        };

        _isDialogOpen = true;
        try
        {
            if (dialog.ShowDialog() == true)
            {
                AttachFilesToDiaryEntry(entry, dialog.FileNames, MoveFilesCheckBox.IsChecked == true);
            }
        }
        catch (Exception ex)
        {
            ShowStatus($"Errore allegato diario: {ex.Message}", isError: true);
        }
        finally
        {
            _isDialogOpen = false;
        }
    }

    private void OpenDiaryAttachmentButton_Click(object sender, RoutedEventArgs e)
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

    private void RemoveDiaryAttachmentButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: DiaryAttachment attachment } btn) return;

        var listBoxItem = FindVisualParent<ListBoxItem>(btn);
        if (listBoxItem?.DataContext is not DiaryEntry entry) return;

        _isDialogOpen = true;
        try
        {
            var result = MessageBox.Show(
                $"Vuoi eliminare l'allegato \"{attachment.FileName}\" da questa riga del diario?",
                "Conferma eliminazione allegato",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _diaryService.RemoveAttachment(entry, attachment);
                RefreshDiaryList();
            }
        }
        finally
        {
            _isDialogOpen = false;
        }
    }

    private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        var current = VisualTreeHelper.GetParent(child);
        while (current is not null)
        {
            if (current is T match) return match;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private void DeleteDiaryEntryButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: DiaryEntry entry }) return;

        _isDialogOpen = true;
        try
        {
            string attachmentNote = entry.Attachments.Count > 0
                ? $"\n\nVerranno eliminati anche i {entry.Attachments.Count} allegati collegati."
                : string.Empty;

            var result = MessageBox.Show(
                $"Vuoi eliminare questa registrazione dal diario?\n\n\"{entry.Text}\"{attachmentNote}",
                "Conferma eliminazione",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _diaryService.DeleteEntry(entry);
                RefreshDiaryList();
            }
        }
        finally
        {
            _isDialogOpen = false;
        }
    }

    #endregion

    #region Pending Alerts & Done CheckBoxes

    private void RefreshPendingAlerts()
    {
        var pending = new List<PendingAlertItem>();

        foreach (var item in _archiveService.GetPendingItems())
        {
            pending.Add(new PendingAlertItem
            {
                SourceBadge = "📁 Archivio",
                Summary = item.Title,
                DateTimeLabel = $"{item.DateFormatted} {item.TimeFormatted}",
                ArchiveItem = item
            });
        }

        foreach (var entry in _diaryService.GetPendingEntries())
        {
            pending.Add(new PendingAlertItem
            {
                SourceBadge = "📓 Diario",
                Summary = entry.Text,
                DateTimeLabel = $"{entry.DateFormatted} {entry.TimeFormatted}",
                DiaryEntry = entry
            });
        }

        if (pending.Count == 0)
        {
            PendingAlertContainer.Visibility = Visibility.Collapsed;
            PendingAlertToggleButton.IsChecked = false;
            PendingAlertItemsControl.Visibility = Visibility.Collapsed;
            PendingAlertItemsControl.ItemsSource = null;
            return;
        }

        PendingAlertContainer.Visibility = Visibility.Visible;
        PendingAlertSummaryText.Text = pending.Count == 1
            ? "⚠️ Hai 1 elemento non spuntato da terminare"
            : $"⚠️ Hai {pending.Count} elementi non spuntati da terminare";
        PendingAlertItemsControl.ItemsSource = pending;
    }

    private void PendingAlertToggleButton_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        PendingAlertItemsControl.Visibility = PendingAlertToggleButton.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void ArchiveDoneCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { Tag: ArchiveItem item } cb) return;

        bool isDone = cb.IsChecked == true;
        _archiveService.SetDone(item, isDone);
        RefreshCardsList();
    }

    private void DiaryDoneCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { Tag: DiaryEntry entry } cb) return;

        bool isDone = cb.IsChecked == true;
        _diaryService.SetDone(entry, isDone);
        RefreshDiaryList();
    }

    private void OpenPendingAlertItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: PendingAlertItem alert }) return;

        if (alert.ArchiveItem is { } archiveItem)
        {
            ArchiveTabRadio.IsChecked = true;
            CardsListBox.SelectedItem = archiveItem;
            CardsListBox.ScrollIntoView(archiveItem);
            OpenItem(archiveItem);
        }
        else if (alert.DiaryEntry is { } diaryEntry)
        {
            DiaryTabRadio.IsChecked = true;
            DiaryDatePicker.SelectedDate = diaryEntry.Timestamp.Date;
            DiarySearchTextBox.Clear();
            RefreshDiaryList();
            DiaryListBox.SelectedItem = diaryEntry;
            DiaryListBox.ScrollIntoView(diaryEntry);
            OpenDiaryEntryDetail(diaryEntry);
        }
    }

    private void MarkPendingAlertItemDone_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: PendingAlertItem alert }) return;

        if (alert.ArchiveItem is { } archiveItem)
        {
            _archiveService.SetDone(archiveItem, true);
            RefreshCardsList();
        }
        else if (alert.DiaryEntry is { } diaryEntry)
        {
            _diaryService.SetDone(diaryEntry, true);
            RefreshDiaryList();
        }
    }

    #endregion
}
