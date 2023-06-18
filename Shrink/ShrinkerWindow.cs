using Microsoft.Extensions.Configuration;
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
    private readonly Progress<int> progress;
    private readonly ConcurrentBag<string> failedFiles = new();
    private List<string>? nullifyPatterns;
    private List<string>? nullifyBlacklistPatterns;
    private bool isWorking = false;

    private Button scanButton;
    private CheckedListBox fileListBox;
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
    }

#pragma warning restore CS8618
#pragma warning restore CS8602

    private void InitializeComponent()
    {
        folderSelectButton = new Button();
        rootPathLabel = new Label();
        rootPathBox = new TextBox();
        scanButton = new Button();
        fileListBox = new CheckedListBox();
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
        ((System.ComponentModel.ISupportInitialize)checkLargerThanUpDown).BeginInit();
        dangerGroup.SuspendLayout();
        SuspendLayout();
        // 
        // folderSelectButton
        // 
        folderSelectButton.Location = new Point(756, 9);
        folderSelectButton.Name = "folderSelectButton";
        folderSelectButton.Size = new Size(42, 23);
        folderSelectButton.TabIndex = 0;
        folderSelectButton.Text = "...";
        folderSelectButton.UseVisualStyleBackColor = true;
        folderSelectButton.Click += FolderSelectButton_Click;
        // 
        // rootPathLabel
        // 
        rootPathLabel.AutoSize = true;
        rootPathLabel.Location = new Point(12, 13);
        rootPathLabel.Name = "rootPathLabel";
        rootPathLabel.Size = new Size(34, 15);
        rootPathLabel.TabIndex = 1;
        rootPathLabel.Text = "Path:";
        // 
        // rootPathBox
        // 
        rootPathBox.Location = new Point(52, 9);
        rootPathBox.Name = "rootPathBox";
        rootPathBox.Size = new Size(698, 23);
        rootPathBox.TabIndex = 2;
        rootPathBox.KeyDown += RootPathBox_KeyDown;
        // 
        // scanButton
        // 
        scanButton.Location = new Point(804, 9);
        scanButton.Name = "scanButton";
        scanButton.Size = new Size(75, 23);
        scanButton.TabIndex = 3;
        scanButton.Text = "Scan";
        scanButton.UseVisualStyleBackColor = true;
        scanButton.Click += ScanButton_Click;
        // 
        // fileListBox
        // 
        fileListBox.CheckOnClick = true;
        fileListBox.FormattingEnabled = true;
        fileListBox.HorizontalScrollbar = true;
        fileListBox.Location = new Point(12, 97);
        fileListBox.Name = "fileListBox";
        fileListBox.Size = new Size(867, 454);
        fileListBox.TabIndex = 4;
        fileListBox.ItemCheck += FileListBox_ItemCheck;
        fileListBox.SelectedIndexChanged += FileListBox_SelectedIndexChanged;
        fileListBox.MouseDown += FileListBox_MouseDown;
        // 
        // shrinkButton
        // 
        shrinkButton.Location = new Point(745, 566);
        shrinkButton.Name = "shrinkButton";
        shrinkButton.Size = new Size(134, 42);
        shrinkButton.TabIndex = 5;
        shrinkButton.Text = "Compress selected";
        shrinkButton.UseVisualStyleBackColor = true;
        shrinkButton.Click += ShrinkButton_Click;
        // 
        // shrinkProgressBar
        // 
        shrinkProgressBar.Location = new Point(12, 583);
        shrinkProgressBar.Name = "shrinkProgressBar";
        shrinkProgressBar.Size = new Size(724, 23);
        shrinkProgressBar.TabIndex = 6;
        // 
        // checkAllButton
        // 
        checkAllButton.Location = new Point(12, 39);
        checkAllButton.Name = "checkAllButton";
        checkAllButton.Size = new Size(90, 23);
        checkAllButton.TabIndex = 7;
        checkAllButton.Text = "Select all";
        checkAllButton.UseVisualStyleBackColor = true;
        checkAllButton.Click += CheckAllButton_Click;
        // 
        // uncheckAllButton
        // 
        uncheckAllButton.Location = new Point(12, 67);
        uncheckAllButton.Name = "uncheckAllButton";
        uncheckAllButton.Size = new Size(90, 23);
        uncheckAllButton.TabIndex = 8;
        uncheckAllButton.Text = "Unselect all";
        uncheckAllButton.UseVisualStyleBackColor = true;
        uncheckAllButton.Click += UncheckAllButton_Click;
        // 
        // checkLargerThanButton
        // 
        checkLargerThanButton.Location = new Point(108, 67);
        checkLargerThanButton.Name = "checkLargerThanButton";
        checkLargerThanButton.Size = new Size(115, 23);
        checkLargerThanButton.TabIndex = 9;
        checkLargerThanButton.Text = "Select larger than:";
        checkLargerThanButton.UseVisualStyleBackColor = true;
        checkLargerThanButton.Click += CheckLargerThanButton_Click;
        // 
        // checkLargerThanUpDown
        // 
        checkLargerThanUpDown.Location = new Point(229, 67);
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
        bLabel.Location = new Point(342, 71);
        bLabel.Name = "bLabel";
        bLabel.Size = new Size(21, 15);
        bLabel.TabIndex = 11;
        bLabel.Text = "KB";
        // 
        // dangerGroup
        // 
        dangerGroup.BackColor = SystemColors.Control;
        dangerGroup.Controls.Add(nullifyButton);
        dangerGroup.Controls.Add(checkDangerButton);
        dangerGroup.Location = new Point(588, 38);
        dangerGroup.Name = "dangerGroup";
        dangerGroup.Size = new Size(291, 53);
        dangerGroup.TabIndex = 12;
        dangerGroup.TabStop = false;
        dangerGroup.Text = "!!! DANGER ZONE !!!";
        // 
        // nullifyButton
        // 
        nullifyButton.Location = new Point(150, 20);
        nullifyButton.Name = "nullifyButton";
        nullifyButton.Size = new Size(135, 23);
        nullifyButton.TabIndex = 1;
        nullifyButton.Text = "NULLIFY SELECTED";
        nullifyButton.UseVisualStyleBackColor = true;
        nullifyButton.Click += NullifyButton_Click;
        // 
        // checkDangerButton
        // 
        checkDangerButton.Location = new Point(6, 20);
        checkDangerButton.Name = "checkDangerButton";
        checkDangerButton.Size = new Size(140, 23);
        checkDangerButton.TabIndex = 0;
        checkDangerButton.Text = "Select recommended";
        checkDangerButton.UseVisualStyleBackColor = true;
        checkDangerButton.Click += CheckDangerButton_Click;
        // 
        // itemsSelectedLabel
        // 
        itemsSelectedLabel.AutoSize = true;
        itemsSelectedLabel.Location = new Point(128, 559);
        itemsSelectedLabel.Name = "itemsSelectedLabel";
        itemsSelectedLabel.Size = new Size(94, 15);
        itemsSelectedLabel.TabIndex = 13;
        itemsSelectedLabel.Text = "Items selected: 0";
        // 
        // itemsTotalLabel
        // 
        itemsTotalLabel.AutoSize = true;
        itemsTotalLabel.Location = new Point(12, 559);
        itemsTotalLabel.Name = "itemsTotalLabel";
        itemsTotalLabel.Size = new Size(75, 15);
        itemsTotalLabel.TabIndex = 14;
        itemsTotalLabel.Text = "Items total: 0";
        // 
        // ShrinkerWindow
        // 
        ClientSize = new Size(891, 618);
        Controls.Add(itemsTotalLabel);
        Controls.Add(itemsSelectedLabel);
        Controls.Add(dangerGroup);
        Controls.Add(bLabel);
        Controls.Add(checkLargerThanUpDown);
        Controls.Add(checkLargerThanButton);
        Controls.Add(uncheckAllButton);
        Controls.Add(checkAllButton);
        Controls.Add(shrinkProgressBar);
        Controls.Add(shrinkButton);
        Controls.Add(fileListBox);
        Controls.Add(scanButton);
        Controls.Add(rootPathBox);
        Controls.Add(rootPathLabel);
        Controls.Add(folderSelectButton);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        Name = "ShrinkerWindow";
        Text = "File Shrinker and Nullifier";
        ((System.ComponentModel.ISupportInitialize)checkLargerThanUpDown).EndInit();
        dangerGroup.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

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
        }
    }

    private void ScanButton_Click(object? sender, EventArgs e)
    {
        if (!Directory.Exists(rootPathBox.Text))
        {
            MessageBox.Show("The specified path does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        fileSizeTable = new Hashtable();
        failedFiles.Clear();

        // Disable all buttons and the list view until the process is complete
        SetUIEnabled(false);

        string rootPath = rootPathBox.Text;
        string[] files;
        try
        {
            // Recursively get all files in the rootPath
            files = Directory.GetFiles(rootPath, "*", SearchOption.AllDirectories);
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show($"Could not acces some files in the specified path.\n\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetUIEnabled(true);
            return;
        }
        Array.Sort(files);

        // Fill the list box with the files
        fileListBox.Items.Clear();
        fileListBox.Items.AddRange(files);
        UpdateItemsTotalLabel();
        foreach (string fileName in files)
        {
            if (ShouldProcessFile(fileName))
            {
                fileListBox.SetItemChecked(fileListBox.Items.IndexOf(fileName), true);
            }
        }

        // Enable previously disabled elements
        SetUIEnabled(true);
        UpdateItemsTotalLabel();
        UpdateItemsSelectedLabel();
    }

    private async void ShrinkButton_Click(object? sender, EventArgs e)
    {
        if (fileListBox.CheckedItems.Count == 0)
        {
            MessageBox.Show("No files selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        // Disable all buttons and the list view until the process is complete
        SetUIEnabled(false);

        List<string> selectedFileNames = fileListBox.CheckedItems.Cast<string>().ToList();
        shrinkProgressBar.Maximum = selectedFileNames.Count;
        shrinkProgressBar.Value = 0;
        failedFiles.Clear();
        await ShrinkFilesAsync(selectedFileNames, progress);
        long totalBytes = 0;
        foreach (string fileName in selectedFileNames)
        {
            long? fileSize = fileSizeTable.ContainsKey(fileName) ? fileSizeTable[fileName] as long? : null;
            if (fileSize.HasValue)
            {
                totalBytes += fileSize.Value;
            }
        }
        long totalBytesOnDisk = 0;
        foreach (string fileName in selectedFileNames)
        {
            long sizeOnDisk = FileTools.GetFileSizeOnDisk(fileName);
            totalBytesOnDisk += sizeOnDisk;
        }
        long totalBytesSaved = totalBytes - totalBytesOnDisk;
        string savedBytesString = BytesToString(totalBytesSaved);

        if (!failedFiles.IsEmpty)
        {
            MessageBox.Show($"All done, but some files could not be processed. They will still appear checked in the list. Saved {savedBytesString}", "Progress report", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            foreach (string fileName in failedFiles)
            {
                int index = fileListBox.Items.IndexOf(fileName);
                fileListBox.SetItemChecked(index, true);
            }
        }
        else
        {
            MessageBox.Show($"All done! Saved {savedBytesString}", "Progress report");
        }

        // Uncheck all items
        foreach (int i in fileListBox.CheckedIndices)
        {
            fileListBox.SetItemChecked(i, false);
        }

        // Enable previously disabled elements
        SetUIEnabled(true);
    }

    private async Task ShrinkFilesAsync(List<string> files, IProgress<int> progress)
    {
        progress.Report(0);
        for (int i = 0; i < files.Count; i++)
        {
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
        fileListBox.Enabled = enabled;
        rootPathBox.Enabled = enabled;
        checkAllButton.Enabled = enabled;
        uncheckAllButton.Enabled = enabled;
        checkDangerButton.Enabled = enabled;
        nullifyButton.Enabled = enabled;
        checkLargerThanButton.Enabled = enabled;
        checkLargerThanUpDown.Enabled = enabled;

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
        for (int i = 0; i < fileListBox.Items.Count; i++)
        {
            fileListBox.SetItemChecked(i, true);
        }
        SetUIEnabled(true);
    }

    private void UncheckAllButton_Click(object? sender, EventArgs e)
    {
        SetUIEnabled(false);
        for (int i = 0; i < fileListBox.Items.Count; i++)
        {
            fileListBox.SetItemChecked(i, false);
        }
        SetUIEnabled(true);
    }

    private void CheckLargerThanButton_Click(object? sender, EventArgs e)
    {
        SetUIEnabled(false);
        foreach (int i in fileListBox.CheckedIndices)
        {
            fileListBox.SetItemChecked(i, false);
        }
        for (int i = 0; i < fileListBox.Items.Count; i++)
        {
            string? fileName = fileListBox.Items[i] as string;
            if (GetFileSize(fileName) > checkLargerThanUpDown.Value * 1024)
            {
                fileListBox.SetItemChecked(i, true);
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
    }

    private void FileListBox_ItemCheck(object? sender, ItemCheckEventArgs e)
    {
        if (isWorking)
        {
            return;
        }

        int count = fileListBox.CheckedIndices.Count + (e.NewValue == CheckState.Checked ? 1 : -1);
        UpdateItemsSelectedLabel(count);
    }

    private void UpdateLabels()
    {
        UpdateItemsSelectedLabel();
        UpdateItemsTotalLabel();
    }

    private void UpdateItemsTotalLabel()
    {
        itemsTotalLabel.Text = $"Total items: {fileListBox.Items.Count}";
    }

    private void UpdateItemsSelectedLabel(int? count = null)
    {
        int newValue = count ?? fileListBox.CheckedItems.Count;
        itemsSelectedLabel.Text = $"Items selected: {newValue}";
    }

    private void CheckDangerButton_Click(object? sender, EventArgs e)
    {
        Debug.Assert(nullifyPatterns != null);
        Debug.Assert(nullifyBlacklistPatterns != null);
        if (nullifyPatterns == null || nullifyBlacklistPatterns == null || fileListBox.Items.Count == 0)
        {
            return;
        }

        SetUIEnabled(false);

        foreach (int i in fileListBox.CheckedIndices)
        {
            fileListBox.SetItemChecked(i, false);
        }
        for (int i = 0; i < fileListBox.Items.Count; i++)
        {
            string? fileName = fileListBox.Items[i] as string;
            fileListBox.SetItemChecked(i, IsFileRecommendedForNullification(fileName));
        }

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

    private void FileListBox_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            int index = fileListBox.IndexFromPoint(e.Location);
            if (index != ListBox.NoMatches)
            {
                fileListBox.SelectedIndex = index;

                string? filePath = fileListBox.SelectedItem as string;
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                }
            }
        }
    }

    private void NullifyButton_Click(object? sender, EventArgs e)
    {
        SetUIEnabled(false);

        failedFiles.Clear();
        long totalBytesFreed = 0;
        foreach (string fileName in fileListBox.CheckedItems)
        {
            try
            {
                FileInfo fileInfo = new(fileName);
                long fileSize = fileInfo.Length;
                FileTools.NullifyFile(fileName);
                totalBytesFreed += fileSize;
            }
            catch
            {
                failedFiles.Add(fileName);
            }
        }
        if (!failedFiles.Any())
        {
            MessageBox.Show(
                $"Successfully nullified {fileListBox.CheckedItems.Count} files, freeing {BytesToString(totalBytesFreed)} of disk space.", "Success",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }
        else
        {
            MessageBox.Show($"Nullified {fileListBox.CheckedItems.Count} files, freeing {BytesToString(totalBytesFreed)} of disk space. There were errors for some of the files. These files will remain selected.",
                "Finished with errors",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
           );
        }
        // Uncheck all items except for failed files
        for (int i = 0; i < fileListBox.Items.Count; i++)
        {
            string? fileName = fileListBox.Items[i] as string;
            if (failedFiles.Contains(fileName))
            {
                continue;
            }
            fileListBox.SetItemChecked(i, false);
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
}
