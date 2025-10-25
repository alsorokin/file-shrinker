using Microsoft.Extensions.Configuration;
using Shrink;
using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace FileShrinker;

public partial class ShrinkerWindow : Form
{
    private const long DEFAULT_FILE_SIZE_THRESHOLD_BYTES = 1024 * 1024 * 10; // 10 MB
    private const long DEFAULT_FILE_SIZE_THRESHOLD_KILOBYTES = DEFAULT_FILE_SIZE_THRESHOLD_BYTES / 1024;

    private Hashtable fileSizeTable;
    private readonly IProgress<int> progress;
    private readonly ConcurrentBag<string> failedFiles = new();
    private List<string>? nullifyPatterns;
    private List<string>? nullifyBlacklistPatterns;
    private bool isWorking = false;
    private long selectedSize = 0;
    private long selectedCount = 0;

    private Button scanButton;
    private Button shrinkButton;
    private Button folderSelectButton;
    private ProgressBar shrinkProgressBar;
    private Label rootPathLabel;
    private TextBox rootPathBox;
    private Button checkAllButton;
    private Button uncheckAllButton;
    private Button checkLargerThanButton;
    private NumericUpDown checkLargerThanUpDown;
    private Label bLabel;
    private GroupBox dangerGroup;
    private Button checkDangerButton;
    private Label itemsSelectedLabel;
    private Label itemsTotalLabel;
    private Label sizeLabel;
    private TableLayoutPanel scanPanel;
    private TableLayoutPanel mainLayoutPanel;
    private Label statusLabel;
    private DataGridView fileDataGrid;
    private GroupBox groupBox1;
    private DataGridViewCheckBoxColumn Selected;
    private DataGridViewTextBoxColumn Path;
    private Button nullifyButton;

    // Suppress SC8618: Non-nullable field is uninitialized. Consider declaring as nullable.
    // This is a false positive, because InitializeComponent() initializes all fields.
#pragma warning disable CS8618
    // Suppress SC8602: Dereference of a possibly null reference.
    // This is also a false positive, because InitializeComponent() initializes all fields.
#pragma warning disable CS8602

    public ShrinkerWindow()
    {
        InitializeComponent();

        progress = new Progress<int>(value =>
        {
            // This lambda is run on context that created progress (usually UI context),
            // so it's safe to work with UI elements.
            if (shrinkProgressBar != null)
            {
                shrinkProgressBar.Value = value;
            }
        });

        checkLargerThanUpDown.Minimum = 0;
        checkLargerThanUpDown.Maximum = long.MaxValue;
        checkLargerThanUpDown.Value = DEFAULT_FILE_SIZE_THRESHOLD_KILOBYTES;

        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");

        IConfiguration config = builder.Build();
        nullifyPatterns = config.GetSection("NullifyPatterns").Get<List<string>>();
        nullifyBlacklistPatterns = config.GetSection("NullifyBlacklistPatterns").Get<List<string>>();

        SetStatus("Click '...' button to select a folder for shrinking.");
    }

#pragma warning restore CS8618
#pragma warning restore CS8602

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
    private void InitializeComponent()
    {
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ShrinkerWindow));
        folderSelectButton = new Button();
        rootPathLabel = new Label();
        rootPathBox = new TextBox();
        scanButton = new Button();
        shrinkButton = new Button();
        shrinkProgressBar = new ProgressBar();
        checkAllButton = new Button();
        uncheckAllButton = new Button();
        checkLargerThanButton = new Button();
        checkLargerThanUpDown = new NumericUpDown();
        bLabel = new Label();
        dangerGroup = new GroupBox();
        nullifyButton = new Button();
        checkDangerButton = new Button();
        itemsSelectedLabel = new Label();
        itemsTotalLabel = new Label();
        sizeLabel = new Label();
        scanPanel = new TableLayoutPanel();
        mainLayoutPanel = new TableLayoutPanel();
        statusLabel = new Label();
        fileDataGrid = new DataGridView();
        Selected = new DataGridViewCheckBoxColumn();
        Path = new DataGridViewTextBoxColumn();
        groupBox1 = new GroupBox();
        ((System.ComponentModel.ISupportInitialize)checkLargerThanUpDown).BeginInit();
        dangerGroup.SuspendLayout();
        scanPanel.SuspendLayout();
        mainLayoutPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)fileDataGrid).BeginInit();
        groupBox1.SuspendLayout();
        SuspendLayout();
        // 
        // folderSelectButton
        // 
        folderSelectButton.Anchor = AnchorStyles.Right;
        folderSelectButton.Location = new Point(775, 3);
        folderSelectButton.Name = "folderSelectButton";
        folderSelectButton.Size = new Size(29, 27);
        folderSelectButton.TabIndex = 0;
        folderSelectButton.Text = "...";
        folderSelectButton.UseVisualStyleBackColor = true;
        folderSelectButton.Click += FolderSelectButton_Click;
        // 
        // rootPathLabel
        // 
        rootPathLabel.Anchor = AnchorStyles.Left;
        rootPathLabel.AutoSize = true;
        rootPathLabel.Location = new Point(3, 9);
        rootPathLabel.Name = "rootPathLabel";
        rootPathLabel.Size = new Size(34, 15);
        rootPathLabel.TabIndex = 1;
        rootPathLabel.Text = "Path:";
        // 
        // rootPathBox
        // 
        rootPathBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        rootPathBox.Location = new Point(63, 3);
        rootPathBox.Name = "rootPathBox";
        rootPathBox.Size = new Size(706, 23);
        rootPathBox.TabIndex = 2;
        rootPathBox.KeyDown += RootPathBox_KeyDown;
        // 
        // scanButton
        // 
        scanButton.Anchor = AnchorStyles.Right;
        scanButton.Location = new Point(810, 3);
        scanButton.Name = "scanButton";
        scanButton.Size = new Size(54, 27);
        scanButton.TabIndex = 3;
        scanButton.Text = "Scan";
        scanButton.UseVisualStyleBackColor = true;
        scanButton.Click += ScanButton_Click;
        // 
        // shrinkButton
        // 
        shrinkButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        shrinkButton.Location = new Point(750, 405);
        shrinkButton.Name = "shrinkButton";
        shrinkButton.Size = new Size(114, 27);
        shrinkButton.TabIndex = 5;
        shrinkButton.Text = "Compress selected";
        shrinkButton.UseVisualStyleBackColor = true;
        shrinkButton.Click += ShrinkButton_Click;
        // 
        // shrinkProgressBar
        // 
        shrinkProgressBar.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        mainLayoutPanel.SetColumnSpan(shrinkProgressBar, 4);
        shrinkProgressBar.Location = new Point(3, 440);
        shrinkProgressBar.Name = "shrinkProgressBar";
        shrinkProgressBar.Size = new Size(861, 23);
        shrinkProgressBar.TabIndex = 6;
        // 
        // checkAllButton
        // 
        checkAllButton.Location = new Point(10, 26);
        checkAllButton.Name = "checkAllButton";
        checkAllButton.Size = new Size(90, 27);
        checkAllButton.TabIndex = 7;
        checkAllButton.Text = "All";
        checkAllButton.UseVisualStyleBackColor = true;
        checkAllButton.Click += CheckAllButton_Click;
        // 
        // uncheckAllButton
        // 
        uncheckAllButton.Location = new Point(106, 26);
        uncheckAllButton.Name = "uncheckAllButton";
        uncheckAllButton.Size = new Size(90, 27);
        uncheckAllButton.TabIndex = 8;
        uncheckAllButton.Text = "None";
        uncheckAllButton.UseVisualStyleBackColor = true;
        uncheckAllButton.Click += UncheckAllButton_Click;
        // 
        // checkLargerThanButton
        // 
        checkLargerThanButton.Location = new Point(217, 26);
        checkLargerThanButton.Name = "checkLargerThanButton";
        checkLargerThanButton.Size = new Size(100, 27);
        checkLargerThanButton.TabIndex = 9;
        checkLargerThanButton.Text = "Larger than:";
        checkLargerThanButton.UseVisualStyleBackColor = true;
        checkLargerThanButton.Click += CheckLargerThanButton_Click;
        // 
        // checkLargerThanUpDown
        // 
        checkLargerThanUpDown.Location = new Point(325, 27);
        checkLargerThanUpDown.Maximum = new decimal(new int[] { -1, int.MaxValue, 0, 0 });
        checkLargerThanUpDown.Name = "checkLargerThanUpDown";
        checkLargerThanUpDown.Size = new Size(107, 23);
        checkLargerThanUpDown.TabIndex = 10;
        checkLargerThanUpDown.ThousandsSeparator = true;
        checkLargerThanUpDown.KeyDown += CheckLargerThanUpDown_KeyDown;
        // 
        // bLabel
        // 
        bLabel.AutoSize = true;
        bLabel.Location = new Point(433, 31);
        bLabel.Name = "bLabel";
        bLabel.Size = new Size(21, 15);
        bLabel.TabIndex = 11;
        bLabel.Text = "KB";
        // 
        // dangerGroup
        // 
        dangerGroup.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        dangerGroup.BackColor = SystemColors.Control;
        dangerGroup.Controls.Add(nullifyButton);
        dangerGroup.Controls.Add(checkDangerButton);
        dangerGroup.Location = new Point(587, 38);
        dangerGroup.Name = "dangerGroup";
        dangerGroup.Size = new Size(291, 66);
        dangerGroup.TabIndex = 12;
        dangerGroup.TabStop = false;
        dangerGroup.Text = "!!! DANGER ZONE !!!";
        // 
        // nullifyButton
        // 
        nullifyButton.Location = new Point(150, 26);
        nullifyButton.Name = "nullifyButton";
        nullifyButton.Size = new Size(135, 27);
        nullifyButton.TabIndex = 1;
        nullifyButton.Text = "NULLIFY SELECTED";
        nullifyButton.UseVisualStyleBackColor = true;
        nullifyButton.Click += NullifyButton_Click;
        // 
        // checkDangerButton
        // 
        checkDangerButton.Location = new Point(6, 26);
        checkDangerButton.Name = "checkDangerButton";
        checkDangerButton.Size = new Size(140, 27);
        checkDangerButton.TabIndex = 0;
        checkDangerButton.Text = "Select recommended";
        checkDangerButton.UseVisualStyleBackColor = true;
        checkDangerButton.Click += CheckDangerButton_Click;
        // 
        // itemsSelectedLabel
        // 
        itemsSelectedLabel.Anchor = AnchorStyles.Left;
        itemsSelectedLabel.AutoSize = true;
        itemsSelectedLabel.Location = new Point(153, 411);
        itemsSelectedLabel.Name = "itemsSelectedLabel";
        itemsSelectedLabel.Size = new Size(94, 15);
        itemsSelectedLabel.TabIndex = 13;
        itemsSelectedLabel.Text = "Items selected: 0";
        // 
        // itemsTotalLabel
        // 
        itemsTotalLabel.Anchor = AnchorStyles.Left;
        itemsTotalLabel.AutoSize = true;
        itemsTotalLabel.Location = new Point(3, 411);
        itemsTotalLabel.Name = "itemsTotalLabel";
        itemsTotalLabel.Size = new Size(75, 15);
        itemsTotalLabel.TabIndex = 14;
        itemsTotalLabel.Text = "Items total: 0";
        // 
        // sizeLabel
        // 
        sizeLabel.Anchor = AnchorStyles.Left;
        sizeLabel.AutoSize = true;
        sizeLabel.Location = new Point(303, 411);
        sizeLabel.Name = "sizeLabel";
        sizeLabel.Size = new Size(95, 15);
        sizeLabel.TabIndex = 15;
        sizeLabel.Text = "Selected size: 0 B";
        // 
        // scanPanel
        // 
        scanPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        scanPanel.AutoSize = true;
        scanPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        scanPanel.ColumnCount = 4;
        scanPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60F));
        scanPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        scanPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 35F));
        scanPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60F));
        scanPanel.Controls.Add(rootPathLabel, 0, 0);
        scanPanel.Controls.Add(rootPathBox, 1, 0);
        scanPanel.Controls.Add(folderSelectButton, 2, 0);
        scanPanel.Controls.Add(scanButton, 3, 0);
        scanPanel.Location = new Point(12, 4);
        scanPanel.Name = "scanPanel";
        scanPanel.RowCount = 1;
        scanPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        scanPanel.Size = new Size(867, 33);
        scanPanel.TabIndex = 16;
        // 
        // mainLayoutPanel
        // 
        mainLayoutPanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        mainLayoutPanel.AutoSize = true;
        mainLayoutPanel.ColumnCount = 4;
        mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
        mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        mainLayoutPanel.Controls.Add(shrinkProgressBar, 0, 2);
        mainLayoutPanel.Controls.Add(itemsTotalLabel, 0, 1);
        mainLayoutPanel.Controls.Add(sizeLabel, 2, 1);
        mainLayoutPanel.Controls.Add(itemsSelectedLabel, 1, 1);
        mainLayoutPanel.Controls.Add(statusLabel, 0, 3);
        mainLayoutPanel.Controls.Add(fileDataGrid, 0, 0);
        mainLayoutPanel.Controls.Add(shrinkButton, 3, 1);
        mainLayoutPanel.Location = new Point(12, 110);
        mainLayoutPanel.Name = "mainLayoutPanel";
        mainLayoutPanel.RowCount = 4;
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 33F));
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 33F));
        mainLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 23F));
        mainLayoutPanel.Size = new Size(867, 491);
        mainLayoutPanel.TabIndex = 17;
        // 
        // statusLabel
        // 
        statusLabel.AutoSize = true;
        mainLayoutPanel.SetColumnSpan(statusLabel, 4);
        statusLabel.Location = new Point(3, 468);
        statusLabel.Name = "statusLabel";
        statusLabel.Size = new Size(259, 15);
        statusLabel.TabIndex = 16;
        statusLabel.Text = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        // 
        // fileDataGrid
        // 
        fileDataGrid.AllowUserToAddRows = false;
        fileDataGrid.AllowUserToDeleteRows = false;
        fileDataGrid.AllowUserToResizeRows = false;
        fileDataGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        fileDataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        fileDataGrid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
        fileDataGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        fileDataGrid.Columns.AddRange(new DataGridViewColumn[] { Selected, Path });
        mainLayoutPanel.SetColumnSpan(fileDataGrid, 4);
        fileDataGrid.EditMode = DataGridViewEditMode.EditOnEnter;
        fileDataGrid.Location = new Point(3, 3);
        fileDataGrid.Name = "fileDataGrid";
        fileDataGrid.RowHeadersVisible = false;
        fileDataGrid.RowHeadersWidth = 51;
        fileDataGrid.RowTemplate.Height = 25;
        fileDataGrid.Size = new Size(861, 396);
        fileDataGrid.TabIndex = 17;
        fileDataGrid.CellContentClick += DataGridView_CellContentClick;
        // 
        // Selected
        // 
        Selected.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
        Selected.FillWeight = 1F;
        Selected.HeaderText = "";
        Selected.MinimumWidth = 20;
        Selected.Name = "Selected";
        Selected.Width = 25;
        // 
        // Path
        // 
        Path.FillWeight = 99F;
        Path.HeaderText = "File Path";
        Path.MinimumWidth = 6;
        Path.Name = "Path";
        Path.ReadOnly = true;
        // 
        // groupBox1
        // 
        groupBox1.Controls.Add(checkAllButton);
        groupBox1.Controls.Add(uncheckAllButton);
        groupBox1.Controls.Add(checkLargerThanButton);
        groupBox1.Controls.Add(checkLargerThanUpDown);
        groupBox1.Controls.Add(bLabel);
        groupBox1.Location = new Point(12, 38);
        groupBox1.Name = "groupBox1";
        groupBox1.Size = new Size(463, 66);
        groupBox1.TabIndex = 18;
        groupBox1.TabStop = false;
        groupBox1.Text = "Select";
        // 
        // ShrinkerWindow
        // 
        ClientSize = new Size(890, 605);
        Controls.Add(groupBox1);
        Controls.Add(mainLayoutPanel);
        Controls.Add(scanPanel);
        Controls.Add(dangerGroup);
        Icon = (Icon)resources.GetObject("$this.Icon");
        MinimumSize = new Size(700, 350);
        Name = "ShrinkerWindow";
        Text = "File Shrinker and Nullifier";
        ((System.ComponentModel.ISupportInitialize)checkLargerThanUpDown).EndInit();
        dangerGroup.ResumeLayout(false);
        scanPanel.ResumeLayout(false);
        scanPanel.PerformLayout();
        mainLayoutPanel.ResumeLayout(false);
        mainLayoutPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)fileDataGrid).EndInit();
        groupBox1.ResumeLayout(false);
        groupBox1.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

    private void FolderSelectButton_Click(object? sender, EventArgs e)
    {
        string currentPath = rootPathBox.Text;
        if (!Directory.Exists(currentPath))
        {
            currentPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
        FolderBrowserDialog folderBrowser = new()
        {
            InitialDirectory = currentPath
        };
        DialogResult result = folderBrowser.ShowDialog();
        if (result == DialogResult.OK)
        {
            string newPath = folderBrowser.SelectedPath;
            rootPathBox.Text = newPath;
            SetStatus("Folder selected. Click on 'Scan' button to scan it. Scanning large folders may take a while.");
        }
    }

    private void ScanButton_Click(object? sender, EventArgs e)
    {
        if (!Directory.Exists(rootPathBox.Text))
        {
            string errorMessage = "The specified path does not exist.";
            MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus(errorMessage);
            return;
        }

        fileSizeTable = new Hashtable();
        failedFiles.Clear();

        // Disable all buttons and the list view until the process is complete
        SetUIEnabled(false);

        string rootPath = rootPathBox.Text;
        string[] files;
        SetStatus("Scanning folder...");
        try
        {
            // Recursively get all files in the rootPath
            files = Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories);
        }
        catch (UnauthorizedAccessException ex)
        {
            string errorMessage = $"Could not acces some files in the specified path.";
            string errorMessageFull = errorMessage + "\n\n" + ex.Message;
            MessageBox.Show(errorMessageFull, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus(errorMessage);
            SetUIEnabled(true);
            return;
        }
        SetStatus("Sorting files...");
        Array.Sort(files);

        SetStatus("Populating list...");
        // Fill the list box with the files
        fileDataGrid.Rows.Clear();
        foreach (string fileName in files)
        {
            fileDataGrid.Rows.Add(ShouldProcessFile(fileName), fileName);
        }

        SetStatus("Folder scanned.");
        // Enable previously disabled elements
        SetUIEnabled(true);
        UpdateItemsTotalLabel();
        UpdateItemsSelectedLabel();
    }

    private string[] GetAllFiles()
    {
        IEnumerable<string?> values = fileDataGrid.Rows.Cast<DataGridViewRow>().Select(row => row.Cells["Path"].Value as string);
        return values.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray()!;
    }

    private string[] GetSelectedFiles()
    {
        return fileDataGrid.Rows.Cast<DataGridViewRow>()
            .Where(row => Convert.ToBoolean(row.Cells["Selected"].Value))
            .Select(row =>
            {
                string? itemText = row.Cells["Path"].Value as string;
                Debug.Assert(!string.IsNullOrWhiteSpace(itemText));
                return itemText;
            })
            .ToArray();
    }

    private int CountSelectedFiles()
    {
        return GetSelectedFiles().Length;
    }

    private void SelectFiles(IEnumerable<string> fileNames, bool value = true)
    {
        foreach (DataGridViewRow row in fileDataGrid.Rows)
        {
            string? itemText = row.Cells["Path"].Value as string;
            Debug.Assert(itemText != null);
            if (itemText == null)
            {
                continue;
            }
            if (fileNames.Contains(itemText))
            {
                row.Cells["Selected"].Value = value;
            }
        }
    }

    private void UnselectFiles(IEnumerable<string> fileNames)
    {
        SelectFiles(fileNames, false);
    }

    private void SelectAll()
    {
        foreach (DataGridViewRow row in fileDataGrid.Rows)
        {
            row.Cells["Selected"].Value = true;
        }
    }

    private void UnselectAll()
    {
        foreach (DataGridViewRow row in fileDataGrid.Rows)
        {
            row.Cells["Selected"].Value = false;
        }
    }

    private long GetSavedBytes(IEnumerable<string> fileNames)
    {
        long totalBytes = 0;
        long totalBytesOnDisk = 0;
        foreach (string fileName in fileNames)
        {
            totalBytes += GetFileSize(fileName);
            long sizeOnDisk = FileTools.GetFileSizeOnDisk(fileName);
            totalBytesOnDisk += sizeOnDisk;
            // Write new file size to the table
            fileSizeTable[fileName] = sizeOnDisk;
        }
        return totalBytes - totalBytesOnDisk;
    }

    private async void ShrinkButton_Click(object? sender, EventArgs e)
    {
        string[] selectedFiles = GetSelectedFiles();
        if (selectedFiles.Length == 0)
        {
            string errorMessage = "No files selected.";
            MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus(errorMessage);
            return;
        }

        SetStatus("Shrinking files...");
        // Disable all buttons and the list view until the process is complete
        SetUIEnabled(false);

        shrinkProgressBar.Maximum = selectedFiles.Length;
        shrinkProgressBar.Value = 0;
        failedFiles.Clear();
        await ShrinkFilesAsync(selectedFiles);
        long totalBytesSaved = GetSavedBytes(selectedFiles);
        string savedBytesString = BytesToString(totalBytesSaved);

        SetStatus("Unselecting successfully processed files.");
        if (!failedFiles.IsEmpty)
        {
            string report = $"All done, but some files could not be processed. They will still appear checked in the list. Saved {savedBytesString}";
            MessageBox.Show(report, "Progress report", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            UnselectFiles(selectedFiles.Except(failedFiles));
            SetStatus(report);
        }
        else
        {
            // Uncheck all items
            UnselectAll();
            string report = $"All done! Freed up {savedBytesString}";
            MessageBox.Show(report, "Progress report");
            SetStatus(report);
        }

        // Enable previously disabled elements
        SetUIEnabled(true);
    }

    private async Task ShrinkFilesAsync(string[] files)
    {
        progress.Report(0);
        for (int i = 0; i < files.Length; i++)
        {
            SetStatus($"Shrinking {files[i]}");
            await Task.Run(() => ShrinkFile(files[i]));
            progress.Report(i + 1);
        }
    }

    private void ShrinkFile(string fileName)
    {
        try
        {
            using FileStream fileToCompress = File.Open(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            FileTools.EnableCompression(fileToCompress.SafeFileHandle.DangerousGetHandle());
        }
        catch
        {
            failedFiles.Add(fileName);
            return;
        }
    }

    private void SetUIEnabled(bool enabled)
    {
        isWorking = !enabled;
        folderSelectButton.Enabled = enabled;
        scanButton.Enabled = enabled;
        shrinkButton.Enabled = enabled;
        fileDataGrid.Enabled = enabled;
        rootPathBox.Enabled = enabled;
        checkAllButton.Enabled = enabled;
        uncheckAllButton.Enabled = enabled;
        checkDangerButton.Enabled = enabled;
        nullifyButton.Enabled = enabled;
        checkLargerThanButton.Enabled = enabled;
        checkLargerThanUpDown.Enabled = enabled;
        fileDataGrid.Enabled = enabled;

        if (enabled)
        {
            UpdateLabels();
        }
    }

    private bool ShouldProcessFile(string? fileName)
    {
        // Get file size and set the box checked if it is larger than 10MB and not compressed
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }
        FileInfo fileInfo = new(fileName);
        if ((fileInfo.Attributes & FileAttributes.Compressed) != 0)
        {
            fileSizeTable[fileName] = FileTools.GetFileSizeOnDisk(fileName);
            return false;
        }
        long fileSize = fileInfo.Length;
        fileSizeTable[fileName] = fileSize;
        if (fileSize > DEFAULT_FILE_SIZE_THRESHOLD_BYTES)
        {
            return true;
        }
        return false;
    }

    private static string BytesToString(long byteCount)
    {
        string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
        if (byteCount == 0)
            return "0" + suf[0];
        long bytes = Math.Abs(byteCount);
        int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        double num = Math.Round(bytes / Math.Pow(1024, place), 1);
        return (Math.Sign(byteCount) * num).ToString() + suf[place];
    }

    private long GetFileSize(string? fileName)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(fileName));

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return 0;
        }
        if (fileSizeTable.ContainsKey(fileName) && fileSizeTable[fileName] != null)
        {
            return (long)fileSizeTable[fileName]!;
        }
        FileInfo fileInfo = new(fileName);
        long fileSize = fileInfo.Length;
        fileSizeTable[fileName] = fileSize;
        return fileSize;
    }

    private void CheckAllButton_Click(object? sender, EventArgs e)
    {
        SetUIEnabled(false);
        SelectAll();
        SetUIEnabled(true);
    }

    private void UncheckAllButton_Click(object? sender, EventArgs e)
    {
        SetUIEnabled(false);
        UnselectAll();
        SetUIEnabled(true);
    }

    private void CheckLargerThanButton_Click(object? sender, EventArgs e)
    {
        SetUIEnabled(false);
        UnselectAll();
        string[] fileNames = GetAllFiles();
        for (int i = 0; i < fileNames.Length; i++)
        {
            if (GetFileSize(fileNames[i]) > checkLargerThanUpDown.Value * 1024)
            {
                // set checked
                fileDataGrid.SetItemChecked(i, true);
            }
        }
        SetUIEnabled(true);
    }

    private void RootPathBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            ScanButton_Click(sender, e);
        }
    }

    private void FileListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (isWorking)
        {
            return;
        }

        UpdateItemsSelectedLabel();
        UpdateSelectedSizeLabel();
    }

    private void FileListBox_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (isWorking)
        {
            return;
        }

        UpdateItemsSelectedLabel();
        UpdateSelectedSizeLabel();
    }

    private void UpdateLabels()
    {
        UpdateItemsTotalLabel();
        UpdateItemsSelectedLabel();
        UpdateSelectedSizeLabel();
    }

    private void UpdateItemsTotalLabel()
    {
        itemsTotalLabel.Text = $"Total items: {fileDataGrid.RowCount}";
    }

    private void UpdateItemsSelectedLabel()
    {
        selectedCount = CountSelectedFiles();
        itemsSelectedLabel.Text = $"Items selected: {selectedCount}";
    }

    private void UpdateItemsSelectedLabel(long count)
    {
        selectedCount = count;
        itemsSelectedLabel.Text = $"Items selected: {count}";
    }

    private void UpdateSelectedSizeLabel()
    {
        // Get total size of selected files
        selectedSize = 0;
        foreach (string fileName in GetSelectedFiles())
        {
            selectedSize += GetFileSize(fileName);
        }
        sizeLabel.Text = $"Selected size: {BytesToString(selectedSize)}";
    }

    private void UpdateSelectedSizeLabel(long size)
    {
        selectedSize = size;
        sizeLabel.Text = $"Selected size: {BytesToString(size)}";
    }

    private void CheckDangerButton_Click(object? sender, EventArgs e)
    {
        Debug.Assert(nullifyPatterns != null);
        Debug.Assert(nullifyBlacklistPatterns != null);
        if (nullifyPatterns == null || nullifyBlacklistPatterns == null)
        {
            return;
        }
        string[] allFiles = GetAllFiles();
        string[] selectedFiles = GetSelectedFiles();
        if (allFiles.Length == 0)
        {
            return;
        }

        SetUIEnabled(false);
        SetStatus("Selecting files recommended for nullification...");
        shrinkProgressBar.Maximum = allFiles.Length;
        shrinkProgressBar.Value = 0;
        for (int i = 0; i < allFiles.Length; i++)
        {
            string? fileName = allFiles[i];
            fileDataGrid.SetItemChecked(i, IsFileRecommendedForNullification(fileName));
            progress.Report(i + 1);
        }
        SetStatus("Ready.");
        SetUIEnabled(true);
    }

    private bool IsFileRecommendedForNullification(string? fileName)
    {
        Debug.Assert(nullifyPatterns != null);
        if (string.IsNullOrWhiteSpace(fileName) || IsFileBlacklisted(fileName))
        {
            return false;
        }

        return nullifyPatterns.Any(pattern => Regex.IsMatch(fileName, pattern));
    }

    private bool IsFileBlacklisted(string fileName)
    {
        Debug.Assert(nullifyBlacklistPatterns != null);
        return nullifyBlacklistPatterns.Any(pattern => Regex.IsMatch(fileName, pattern));
    }

    private void NullifyButton_Click(object? sender, EventArgs e)
    {
        // Ask user if they understand the risks
        DialogResult dialogResult = MessageBox.Show(
            "Nullifying files will set their size to zero, effectively removing their content. Please make sure you absolutely don't need these files. Are you sure you want to continue?",
            "Warning",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning
        );
        if (dialogResult == DialogResult.No)
        {
            return;
        }

        SetUIEnabled(false);

        failedFiles.Clear();
        long totalBytesFreed = 0;
        string[] selectedFiles = GetSelectedFiles();
        foreach (string fileName in selectedFiles)
        {
            try
            {
                FileInfo fileInfo = new(fileName);
                long fileSize = fileInfo.Length;
                FileTools.NullifyFile(fileName);
                totalBytesFreed += fileSize;
                fileSizeTable[fileName] = 0L;
            }
            catch
            {
                failedFiles.Add(fileName);
            }
        }
        if (!failedFiles.Any())
        {
            MessageBox.Show(
                $"Successfully nullified {selectedFiles.Length} files, freeing {BytesToString(totalBytesFreed)} of disk space.", "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        else
        {
            MessageBox.Show($"Nullified {selectedFiles.Length} files, freeing {BytesToString(totalBytesFreed)} of disk space. There were errors for some of the files. These files will remain selected.",
                "Finished with errors",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
           );
        }
        // Uncheck all items except for failed files
        string[] allFiles = GetAllFiles();
        for (int i = 0; i < allFiles.Length; i++)
        {
            if (failedFiles.Contains(allFiles[i]))
            {
                continue;
            }
            fileDataGrid.SetItemChecked(i, false);
        }

        SetUIEnabled(true);
    }

    private void CheckLargerThanUpDown_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            CheckLargerThanButton_Click(sender, e);
        }
    }

    private void SetStatus(string status)
    {
        statusLabel.Text = status;
        statusLabel.Invalidate();
        statusLabel.Update();
        statusLabel.Refresh();
        Application.DoEvents();
    }

    private void DataGridView_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        Debug.Assert(sender is DataGridView);
        var senderGrid = (DataGridView)sender;

        if (e.ColumnIndex == senderGrid.Columns["Selected"].Index && e.RowIndex >= 0)
        {
            // senderGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            senderGrid[e.ColumnIndex, e.RowIndex].Value = !(bool)senderGrid[e.ColumnIndex, e.RowIndex].Value;
            bool isChecked = (bool)senderGrid[e.ColumnIndex, e.RowIndex].Value;
            string fileName = (string)senderGrid[senderGrid.Columns["Path"].Index, e.RowIndex].Value;

            if (isChecked)
            {
                UpdateItemsSelectedLabel(selectedCount + 1);
                UpdateSelectedSizeLabel(selectedSize + GetFileSize(fileName));
            }
            else
            {
                UpdateItemsSelectedLabel(selectedCount - 1);
                UpdateSelectedSizeLabel(selectedSize - GetFileSize(fileName));
            }
        }
    }
}
