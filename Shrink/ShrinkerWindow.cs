using System.Collections;
using System.Collections.Concurrent;

namespace FileShrinker;

public partial class ShrinkerWindow : Form
{
    private const long DEFAULT_FILE_SIZE_THRESHOLD_BYTES = 1024 * 1024 * 10; // 10 MB

    private Button scanButton;
    private CheckedListBox fileListBox;
    private Button shrinkButton;
    private Button folderSelectButton;
    private ProgressBar shrinkProgressBar;
    private Label rootPathLabel;
    private TextBox rootPathBox;

    private Hashtable fileSizeTable;
    private readonly Progress<int> progress;
    private readonly ConcurrentBag<string> failedFiles = new();

    // Suppress SC8618: Non-nullable field is uninitialized. Consider declaring as nullable.
    // This is a false positive, because InitializeComponent() initializes all fields.
#pragma warning disable CS8618
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
    }
#pragma warning restore CS8618

    private void InitializeComponent()
    {
        folderSelectButton = new Button();
        rootPathLabel = new Label();
        rootPathBox = new TextBox();
        scanButton = new Button();
        fileListBox = new CheckedListBox();
        shrinkButton = new Button();
        shrinkProgressBar = new ProgressBar();
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
        fileListBox.Location = new Point(12, 43);
        fileListBox.Name = "fileListBox";
        fileListBox.Size = new Size(867, 508);
        fileListBox.TabIndex = 4;
        // 
        // shrinkButton
        // 
        shrinkButton.Location = new Point(745, 557);
        shrinkButton.Name = "shrinkButton";
        shrinkButton.Size = new Size(134, 42);
        shrinkButton.TabIndex = 5;
        shrinkButton.Text = "Shrink!";
        shrinkButton.UseVisualStyleBackColor = true;
        shrinkButton.Click += ShrinkButton_Click;
        // 
        // shrinkProgressBar
        // 
        shrinkProgressBar.Location = new Point(12, 569);
        shrinkProgressBar.Name = "shrinkProgressBar";
        shrinkProgressBar.Size = new Size(724, 23);
        shrinkProgressBar.TabIndex = 6;
        // 
        // ShrinkerWindow
        // 
        ClientSize = new Size(891, 604);
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
        FolderBrowserDialog cxtCacheFolderBrowser = new();
        cxtCacheFolderBrowser.InitialDirectory = currentPath;
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
        foreach (string fileName in files)
        {
            if (ShouldProcessFile(fileName))
            {
                fileListBox.Items.Add(fileName);
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

}