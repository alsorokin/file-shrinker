using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace FileShrinker;

public partial class ShrinkerWindow : Form
{
    private const long DEFAULT_FILE_SIZE_THRESHOLD_BYTES = 1024 * 1024 * 10; // 10 MB
    private const long DEFAULT_FILE_SIZE_THRESHOLD_KILOBYTES = DEFAULT_FILE_SIZE_THRESHOLD_BYTES / 1024;

    private Button scanButton;
    private CheckedListBox fileListBox;
    private Button shrinkButton;
    private Button folderSelectButton;
    private ProgressBar shrinkProgressBar;
    private Label rootPathLabel;
    private TextBox rootPathBox;

    private Hashtable fileSizeTable;
    private readonly Progress<int> progress;
    private Button checkAllButton;
    private Button uncheckAllButton;
    private Button checkLargerThanButton;
    private NumericUpDown checkLargerThanUpDown;
    private Label bLabel;
    private GroupBox dangerGroup;
    private Button checkDangerButton;
    private Label itemsSelectedLabel;
    private Label itemsTotalLabel;
    private readonly ConcurrentBag<string> failedFiles = new();

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
        // 
        // shrinkButton
        // 
        shrinkButton.Location = new Point(745, 566);
        shrinkButton.Name = "shrinkButton";
        shrinkButton.Size = new Size(134, 42);
        shrinkButton.TabIndex = 5;
        shrinkButton.Text = "Shrink!";
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
        checkAllButton.Text = "Check all";
        checkAllButton.UseVisualStyleBackColor = true;
        checkAllButton.Click += CheckAllButton_Click;
        // 
        // uncheckAllButton
        // 
        uncheckAllButton.Location = new Point(12, 67);
        uncheckAllButton.Name = "uncheckAllButton";
        uncheckAllButton.Size = new Size(90, 23);
        uncheckAllButton.TabIndex = 8;
        uncheckAllButton.Text = "Uncheck all";
        uncheckAllButton.UseVisualStyleBackColor = true;
        uncheckAllButton.Click += UncheckAllButton_Click;
        // 
        // checkLargerThanButton
        // 
        checkLargerThanButton.Location = new Point(159, 51);
        checkLargerThanButton.Name = "checkLargerThanButton";
        checkLargerThanButton.Size = new Size(115, 23);
        checkLargerThanButton.TabIndex = 9;
        checkLargerThanButton.Text = "Check larger than:";
        checkLargerThanButton.UseVisualStyleBackColor = true;
        checkLargerThanButton.Click += CheckLargerThanButton_Click;
        // 
        // checkLargerThanUpDown
        // 
        checkLargerThanUpDown.Location = new Point(280, 51);
        checkLargerThanUpDown.Maximum = new decimal(new int[] { -1, int.MaxValue, 0, 0 });
        checkLargerThanUpDown.Name = "checkLargerThanUpDown";
        checkLargerThanUpDown.Size = new Size(107, 23);
        checkLargerThanUpDown.TabIndex = 10;
        checkLargerThanUpDown.ThousandsSeparator = true;
        // 
        // bLabel
        // 
        bLabel.AutoSize = true;
        bLabel.Location = new Point(393, 55);
        bLabel.Name = "bLabel";
        bLabel.Size = new Size(21, 15);
        bLabel.TabIndex = 11;
        bLabel.Text = "KB";
        // 
        // dangerGroup
        // 
        dangerGroup.Controls.Add(checkDangerButton);
        dangerGroup.Location = new Point(420, 38);
        dangerGroup.Name = "dangerGroup";
        dangerGroup.Size = new Size(459, 53);
        dangerGroup.TabIndex = 12;
        dangerGroup.TabStop = false;
        dangerGroup.Text = "!DANGER ZONE!";
        // 
        // checkDangerButton
        // 
        checkDangerButton.Location = new Point(6, 24);
        checkDangerButton.Name = "checkDangerButton";
        checkDangerButton.Size = new Size(140, 23);
        checkDangerButton.TabIndex = 0;
        checkDangerButton.Text = "Check recommended";
        checkDangerButton.UseVisualStyleBackColor = true;
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
        ((System.ComponentModel.ISupportInitialize)checkLargerThanUpDown).EndInit();
        dangerGroup.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    private void FolderSelectButton_Click(object sender, EventArgs e)
    {
        string currentPath = rootPathBox.Text;
        if (!Directory.Exists(currentPath))
        {
            currentPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
        FolderBrowserDialog cxtCacheFolderBrowser = new()
        {
            InitialDirectory = currentPath
        };
        DialogResult result = cxtCacheFolderBrowser.ShowDialog();
        if (result == DialogResult.OK)
        {
            string newPath = cxtCacheFolderBrowser.SelectedPath;
            rootPathBox.Text = newPath;
        }
    }

    private void ScanButton_Click(object sender, EventArgs e)
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
                fileListBox.SetItemChecked(fileListBox.Items.Count - 1, true);
            }
        }

        // Enable previously disabled elements
        SetUIEnabled(true);
    }

    private async void ShrinkButton_Click(object sender, EventArgs e)
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
        folderSelectButton.Enabled = enabled;
        scanButton.Enabled = enabled;
        shrinkButton.Enabled = enabled;
        fileListBox.Enabled = enabled;
        rootPathBox.Enabled = enabled;
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

    private void CheckAllButton_Click(object sender, EventArgs e)
    {
        for (int i = 0; i < fileListBox.Items.Count; i++)
        {
            fileListBox.SetItemChecked(i, true);
        }
    }

    private void UncheckAllButton_Click(object sender, EventArgs e)
    {
        for (int i = 0; i < fileListBox.Items.Count; i++)
        {
            fileListBox.SetItemChecked(i, false);
        }
    }

    private void CheckLargerThanButton_Click(object sender, EventArgs e)
    {
        for (int i = 0; i < fileListBox.Items.Count; i++)
        {
            string? fileName = fileListBox.Items[i] as string;
            if (GetFileSize(fileName) > checkLargerThanUpDown.Value * 1024)
            {
                fileListBox.SetItemChecked(i, true);
            }
        }
    }

    private void RootPathBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            ScanButton_Click(sender, e);
        }
    }

    private void FileListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        UpdateItemsSelectedLabel();
    }

    private void AddFilesToFileListBox(string[] files)
    {
        fileListBox.Items.AddRange(files);
        UpdateItemsTotalLabel();
    }

    private void FileListBox_ItemCheck(object sender, ItemCheckEventArgs e)
    {
        int count = fileListBox.CheckedIndices.Count + (e.NewValue == CheckState.Checked ? 1 : -1);
        UpdateItemsSelectedLabel(count);
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
}