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
    private readonly List<DraftAttachmentItem> _pendingDraftAttachments = [];
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
        _diaryService = new DiaryService(_archiveService.BaseDirectory)
        {
            IsDarkTheme = settings.IsDarkTheme
        };

        var infoVer = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
            ?? "1.0.0";
        VersionTextBlock.Text = $"v{infoVer.Split('+')[0]}";

        FixedPathTextBlock.Text = $"Percorso: {_archiveService.BaseDirectory}";
        FixedPathTextBlock.ToolTip = _archiveService.BaseDirectory;
        DiaryDatePicker.SelectedDate = DateTime.Today;
        DiaryRichTextHelper.ClearDocument(DiaryRichTextBox);
        ThemeToggleButton.IsChecked = settings.IsDarkTheme;
        ApplyTheme(settings.IsDarkTheme);

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
        RefreshQuickNotesChips();
        Loaded += async (_, _) => await CheckForUpdatesOnStartupAsync();
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

    private void ExcludeDiaryAttachmentsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        RefreshCardsList();
    }

    private static string CombineQueries(string? first, string? second)
    {
        if (string.IsNullOrWhiteSpace(first)) return second?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(second)) return first.Trim();
        return $"{first.Trim()} {second.Trim()}";
    }

    private void GlobalSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RefreshCardsList();
        RefreshDiaryList();
    }

    private void GlobalDatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return;
        RefreshCardsList();
        RefreshDiaryList();
    }

    private void ClearGlobalSearchButton_Click(object sender, RoutedEventArgs e)
    {
        GlobalSearchTextBox.Clear();
        GlobalDatePicker.SelectedDate = null;
        RefreshCardsList();
        RefreshDiaryList();
    }

    private void GlobalArchiveCountButton_Click(object sender, RoutedEventArgs e)
    {
        ArchiveTabRadio.IsChecked = true;
    }

    private void GlobalDiaryCountButton_Click(object sender, RoutedEventArgs e)
    {
        DiaryTabRadio.IsChecked = true;
    }

    private void UpdateGlobalSearchCounts(int archiveCount, int diaryCount)
    {
        bool hasGlobalFilter = !string.IsNullOrWhiteSpace(GlobalSearchTextBox.Text) || GlobalDatePicker.SelectedDate.HasValue;
        GlobalSearchCountsPanel.Visibility = hasGlobalFilter ? Visibility.Visible : Visibility.Collapsed;
        GlobalArchiveCountButton.Content = $"📁 {archiveCount} Archivio";
        GlobalDiaryCountButton.Content = $"📓 {diaryCount} Diario";
    }

    private void RefreshCardsList()
    {
        bool excludeDiary = ExcludeDiaryAttachmentsCheckBox.IsChecked == true;
        var archiveAll = _archiveService.GetAllItems();
        int diaryCount = excludeDiary ? 0 : _diaryService.GetMatchingAttachments(null, null).Count;
        int totalCount = archiveAll.Count + diaryCount;
        TotalCountBadge.Text = totalCount == 1 ? "1 scheda" : $"{totalCount} schede";

        string effectiveQuery = CombineQueries(GlobalSearchTextBox.Text, SearchTextBox.Text);
        DateTime? effectiveDate = FilterDatePicker.SelectedDate ?? GlobalDatePicker.SelectedDate;

        var filtered = _archiveService.Search(effectiveQuery, effectiveDate);

        if (!excludeDiary)
        {
            foreach (var (entry, att) in _diaryService.GetMatchingAttachments(effectiveQuery, effectiveDate))
            {
                string fullPath = _diaryService.GetAttachmentFullPath(att);
                long sizeBytes = !att.IsDirectory && File.Exists(fullPath) ? new FileInfo(fullPath).Length : 0;
                string preview = entry.Text.Replace("\r\n", " ").Replace("\n", " ").Trim();
                if (preview.Length > 80) preview = preview[..80] + "…";

                filtered.Add(new ArchiveItem
                {
                    Id = att.Id,
                    Title = att.IsDirectory ? att.FileName : Path.GetFileNameWithoutExtension(att.FileName),
                    OriginalFileName = att.FileName,
                    ArchivedAt = att.AddedAt,
                    FileSizeBytes = sizeBytes,
                    IsDirectory = att.IsDirectory,
                    IsDone = entry.IsDone,
                    IsDiaryAttachment = true,
                    DiarySourceLabel = $"Riga diario ({entry.DateFormatted} {entry.TimeFormatted}): {preview}",
                    DirectFullPath = fullPath,
                    LinkedDiaryEntry = entry,
                    LinkedDiaryAttachment = att
                });
            }
        }

        filtered = filtered.OrderByDescending(i => i.ArchivedAt).ToList();
        CardsListBox.ItemsSource = filtered;

        ResultsSummaryTextBlock.Text = filtered.Count == 1 ? "1 scheda trovata" : $"{filtered.Count} schede trovate";
        EmptyStateTextBlock.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdateGlobalSearchCounts(filtered.Count, DiaryListBox.ItemsSource is ICollection<DiaryEntry> dList ? dList.Count : 0);
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
                bool isEditingDraft = !DiaryRichTextHelper.IsDocumentEmpty(DiaryRichTextBox.Document);
                if (!isEditingDraft && DiaryListBox.SelectedItem is DiaryEntry selectedEntry)
                {
                    AttachFilesToDiaryEntry(selectedEntry, paths, moveFiles);
                    return;
                }

                int addedDraftCount = 0;
                foreach (var path in paths.Where(p => File.Exists(p) || Directory.Exists(p)))
                {
                    if (_pendingDraftAttachments.Any(d => string.Equals(d.FullPath, path, StringComparison.OrdinalIgnoreCase)))
                        continue;

                    _pendingDraftAttachments.Add(new DraftAttachmentItem
                    {
                        FullPath = path,
                        MoveFile = moveFiles
                    });
                    addedDraftCount++;
                }

                RefreshPendingDraftAttachmentsUI();
                if (addedDraftCount > 0)
                {
                    ShowStatus(
                        addedDraftCount == 1
                            ? $"Allegato pronto per la nota: \"{Path.GetFileName(paths[0])}\" (clicca 💾 Salva)"
                            : $"{addedDraftCount} allegati pronti per la nota (clicca 💾 Salva)",
                        isError: false);
                }
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
                bool isDark = ThemeToggleButton.IsChecked == true;
                SettingsService.Save(new AppSettings
                {
                    ArchiveRootPath = newPath,
                    IsDarkTheme = isDark
                });

                _archiveService = new ArchiveService(newPath);
                _diaryService = new DiaryService(newPath) { IsDarkTheme = isDark };

                FixedPathTextBlock.Text = $"Percorso: {_archiveService.BaseDirectory}";
                FixedPathTextBlock.ToolTip = _archiveService.BaseDirectory;

                RefreshCardsList();
                RefreshDiaryList();
                RefreshQuickNotesChips();
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

    private string GetItemFullPath(ArchiveItem item) =>
        !string.IsNullOrWhiteSpace(item.DirectFullPath)
            ? item.DirectFullPath
            : _archiveService.GetFullPath(item);

    private void OpenItem(ArchiveItem item)
    {
        var fullPath = GetItemFullPath(item);
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

        var fullPath = GetItemFullPath(item);
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

        if (item.IsDiaryAttachment && item.LinkedDiaryEntry is { } diaryEntry)
        {
            OpenDiaryEntryDetail(diaryEntry);
            return;
        }

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
            if (item.IsDiaryAttachment && item.LinkedDiaryEntry is { } entry && item.LinkedDiaryAttachment is { } att)
            {
                var attResult = MessageBox.Show(
                    $"Vuoi eliminare l'allegato \"{att.FileName}\" dalla riga del diario?\n\nVerrà rimosso sia dal diario che dall'archivio.",
                    "Conferma eliminazione allegato",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (attResult == MessageBoxResult.Yes)
                {
                    _diaryService.RemoveAttachment(entry, att);
                    RefreshDiaryList();
                    RefreshCardsList();
                    ShowStatus($"Allegato \"{att.FileName}\" eliminato.", isError: false);
                }
                return;
            }

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

        string effectiveQuery = CombineQueries(GlobalSearchTextBox.Text, DiarySearchTextBox.Text);
        bool hasGlobalQuery = !string.IsNullOrWhiteSpace(GlobalSearchTextBox.Text);
        DateTime? effectiveDate = GlobalDatePicker.SelectedDate ?? (hasGlobalQuery ? null : DiaryDatePicker.SelectedDate);

        var entries = _diaryService.GetEntries(effectiveDate, effectiveQuery);
        DiaryListBox.ItemsSource = entries;

        string dayLabel = effectiveDate.HasValue
            ? $"del {effectiveDate.Value:dd/MM/yyyy}"
            : "in tutti i giorni";

        DiarySummaryTextBlock.Text = entries.Count == 1
            ? $"1 attività registrata {dayLabel}"
            : $"{entries.Count} attività registrate {dayLabel}";

        DiaryEmptyTextBlock.Visibility = entries.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        UpdateGlobalSearchCounts(CardsListBox.ItemsSource is ICollection<ArchiveItem> aList ? aList.Count : 0, entries.Count);
        RefreshPendingAlerts();
    }

    private void RefreshQuickNotesChips()
    {
        QuickNotesChipsControl.ItemsSource = null;
        QuickNotesChipsControl.ItemsSource = _diaryService.GetQuickNotes();
    }

    private void InsertTextIntoDiaryEditor(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        string existing = DiaryRichTextHelper.ExtractPlainText(DiaryRichTextBox.Document);
        if (string.IsNullOrWhiteSpace(existing))
        {
            DiaryRichTextBox.Document = DiaryRichTextHelper.LoadDocument(null, text);
        }
        else
        {
            DiaryRichTextBox.CaretPosition = DiaryRichTextBox.Document.ContentEnd;
            DiaryRichTextBox.CaretPosition.InsertTextInRun($"\n{text}");
        }
        DiaryRichTextBox.Focus();
    }

    private void QuickNoteChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: QuickNote note })
        {
            InsertTextIntoDiaryEditor(note.Text);
        }
    }

    private void ManageQuickNotesButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new QuickNotesWindow(_diaryService)
        {
            Owner = this
        };

        _isDialogOpen = true;
        try
        {
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.SelectedNoteTextToInsert))
            {
                InsertTextIntoDiaryEditor(dialog.SelectedNoteTextToInsert);
            }
            RefreshQuickNotesChips();
        }
        finally
        {
            _isDialogOpen = false;
        }
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
                entry.RichContentXaml = dialog.EditedRichXaml;
                entry.IsDone = dialog.EditedIsDone;
                _diaryService.UpdateEntry(entry);
                RefreshDiaryList();
                RefreshCardsList();
            }
        }
        finally
        {
            _isDialogOpen = false;
        }
    }

    private void InsertDiaryCheckBoxButton_Click(object sender, RoutedEventArgs e)
    {
        DiaryRichTextHelper.InsertSecondaryCheckBox(DiaryRichTextBox);
    }

    private void RefreshPendingDraftAttachmentsUI()
    {
        PendingDraftAttachmentsControl.ItemsSource = null;
        PendingDraftAttachmentsControl.ItemsSource = _pendingDraftAttachments;
        PendingDraftAttachmentsPanel.Visibility = _pendingDraftAttachments.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void RemoveDraftAttachmentButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: DraftAttachmentItem item })
        {
            _pendingDraftAttachments.Remove(item);
            RefreshPendingDraftAttachmentsUI();
        }
    }

    private void RecordDiaryInput()
    {
        string text = DiaryRichTextHelper.ExtractPlainText(DiaryRichTextBox.Document);
        if (string.IsNullOrWhiteSpace(text) && _pendingDraftAttachments.Count == 0) return;

        if (string.IsNullOrWhiteSpace(text) && _pendingDraftAttachments.Count > 0)
        {
            text = $"Allegati ({string.Join(", ", _pendingDraftAttachments.Select(a => Path.GetFileName(a.FullPath)))})";
        }

        string richXaml = DiaryRichTextHelper.SerializeDocument(DiaryRichTextBox.Document);
        var entry = _diaryService.AddEntry(text, DiaryDatePicker.SelectedDate ?? DateTime.Today, richXaml);

        if (_pendingDraftAttachments.Count > 0)
        {
            foreach (var group in _pendingDraftAttachments.GroupBy(a => a.MoveFile))
            {
                _diaryService.AddAttachments(entry, group.Select(g => g.FullPath), group.Key);
            }
            _pendingDraftAttachments.Clear();
            RefreshPendingDraftAttachmentsUI();
        }

        DiaryRichTextHelper.ClearDocument(DiaryRichTextBox);
        RefreshDiaryList();
        RefreshCardsList();
        ShowStatus($"Diario registrato ({entry.DateFormatted} {entry.TimeFormatted})", isError: false);
    }

    private void ThemeToggleButton_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;

        bool isDark = ThemeToggleButton.IsChecked == true;
        var settings = SettingsService.Load();
        settings.IsDarkTheme = isDark;
        SettingsService.Save(settings);

        ApplyTheme(isDark);
    }

    private void ApplyTheme(bool isDark)
    {
        Resources["AppBgBrush"] = BrushFromHex(isDark ? "#090D16" : "#F4F6FB");
        Resources["SurfaceBgBrush"] = BrushFromHex(isDark ? "#E8131B2E" : "#F0FFFFFF");
        Resources["SurfaceBorderBrush"] = BrushFromHex(isDark ? "#26324D" : "#DCE3F0");
        Resources["InputBgBrush"] = BrushFromHex(isDark ? "#0D1322" : "#FFFFFF");
        Resources["DropZoneBgBrush"] = BrushFromHex(isDark ? "#CC171F36" : "#CCEEF2FF");
        Resources["PrimaryTextBrush"] = BrushFromHex(isDark ? "#F8FAFC" : "#0F172A");
        Resources["SecondaryTextBrush"] = BrushFromHex(isDark ? "#CBD5E1" : "#475569");
        Resources["MutedTextBrush"] = BrushFromHex(isDark ? "#94A3B8" : "#64748B");

        _diaryService.IsDarkTheme = isDark;
        if (IsLoaded)
        {
            RefreshDiaryList();
        }
    }

    private static SolidColorBrush BrushFromHex(string hex) =>
        new((Color)ColorConverter.ConvertFromString(hex));

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
        RefreshCardsList();
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
        if (item.IsDiaryAttachment && item.LinkedDiaryEntry is { } entry)
        {
            _diaryService.SetDone(entry, isDone);
            RefreshDiaryList();
        }
        else
        {
            _archiveService.SetDone(item, isDone);
        }
        RefreshCardsList();
    }

    private void DiaryDoneCheckBox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox { Tag: DiaryEntry entry } cb) return;

        bool isDone = cb.IsChecked == true;
        _diaryService.SetDone(entry, isDone);
        RefreshDiaryList();
        RefreshCardsList();
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

    private async Task CheckForUpdatesOnStartupAsync()
    {
        try
        {
            var updater = new UpdateService();
            var update = await updater.CheckForUpdateAsync();
            if (update is null) return;

            var current = UpdateService.CurrentVersion;
            var result = MessageBox.Show(
                $"È disponibile una nuova versione di MemoBam ({update.TagName}).\n" +
                $"Versione attuale: v{current.Major}.{current.Minor}.{current.Build}\n\n" +
                $"Vuoi scaricarla e installarla ora?",
                "Aggiornamento disponibile",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result != MessageBoxResult.Yes) return;

            var progress = new Progress<string>(msg => ShowStatus(msg, false));
            bool shouldRestart = await updater.DownloadAndApplyAsync(update, progress);
            if (shouldRestart)
            {
                Application.Current.Shutdown();
            }
        }
        catch
        {
            // Startup update check is best-effort; ignore offline/network errors silently.
        }
    }

    private void PwaQrButton_Click(object sender, RoutedEventArgs e)
    {
        QrOverlay.Visibility = Visibility.Visible;
    }

    private void CloseQrOverlay_Click(object sender, RoutedEventArgs e)
    {
        QrOverlay.Visibility = Visibility.Collapsed;
    }

    private void QrOverlayBackdrop_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        QrOverlay.Visibility = Visibility.Collapsed;
    }

    private void QrOverlayCard_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void OpenPwaUrl_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://ambruno1968.github.io/MemoBamPWA/")
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ShowStatus($"Impossibile aprire il link: {ex.Message}", true);
        }
    }

    private sealed class DraftAttachmentItem
    {
        public string FullPath { get; init; } = string.Empty;
        public bool MoveFile { get; init; }
        public string DisplayName => Directory.Exists(FullPath)
            ? $"📁 {Path.GetFileName(FullPath)}"
            : $"📎 {Path.GetFileName(FullPath)}";
    }
}
