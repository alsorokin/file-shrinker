using System.Diagnostics;

namespace Shrink
{
    internal static class DataGridViewExtensions
    {
        internal static void SetItemChecked(this DataGridView view, int index, bool value)
        {
            Debug.Assert(index >= 0 && index < view.Rows.Count);
            // assume that the first column is the checkbox column, set it checked
            view.Rows[index].Cells[0].Value = value;
        }
    }
}
