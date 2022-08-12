using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DigitalPlatform.WinForm
{
    public class ListViewUtil
    {
        public static void BeginSelectItem(Control control, ListViewItem item)
        {
            control.BeginInvoke(new Action<ListViewItem>(
                (o) =>
                {
                    o.Selected = true;
                    o.EnsureVisible();
                }), item);
        }

        public static int GetColumnHeaderHeight(ListView list)
        {
            RECT rc = new RECT();
            IntPtr hwnd = API.SendMessage(list.Handle, API.LVM_GETHEADER, 0, 0);
            if (hwnd == null)
                return -1;

            if (API.GetWindowRect(new HandleRef(null, hwnd), out rc))
            {
                return rc.bottom - rc.top;
            }

            return -1;
        }

        // 2012/5/9
        // 创建事项名列表
        public static string GetItemNameList(ListView.SelectedListViewItemCollection items,
            string strSep = ",")
        {
            StringBuilder strItemNameList = new StringBuilder();
            foreach (ListViewItem item in items)
            {
                if (strItemNameList.Length > 0)
                    strItemNameList.Append(strSep);
                strItemNameList.Append(item.Text);
            }

            return strItemNameList.ToString();
        }

        // 2012/5/9
        // 创建事项名列表
        public static string GetItemNameList(ListView list,
            string strSep = ",")
        {
            StringBuilder strItemNameList = new StringBuilder();
            foreach (ListViewItem item in list.SelectedItems)
            {
                if (strItemNameList.Length > 0)
                    strItemNameList.Append(strSep);
                strItemNameList.Append(item.Text);
            }

            return strItemNameList.ToString();
        }

        // 上下移动事项的菜单是否应被使能
        public static bool MoveItemEnabled(
            ListView list,
            bool bUp)
        {
            if (list.SelectedItems.Count == 0)
                return false;
            int index = list.SelectedIndices[0];
            if (bUp == true)
            {
                if (index == 0)
                    return false;
                return true;
            }
            else
            {
                if (index >= list.Items.Count - 1)
                    return false;
                return true;
            }
        }

        public static bool MoveSelectedUpDown(
            ListView list,
            bool bUp)
        {
            if (list.SelectedItems.Count == 0)
                return false;

            int index = list.SelectedIndices[0];

            if (bUp)
            {
                index--;
                if (index < 0)
                    return false;
            }
            else
            {
                index++;
                if (index >= list.Items.Count)
                    return false;
            }

            list.SelectedItems.Clear();
            list.Items[index].Selected = true;
            return true;
        }

        // parameters:
        //      indices 返回移动涉及到的下标位置。第一个元素是移动前的位置，第二个元素是移动后的位置
        public static int MoveItemUpDown(
            ListView list,
            bool bUp,
            out List<int> indices,
            out string strError)
        {
            strError = "";
            indices = new List<int>();
            // int nRet = 0;

            if (list.SelectedItems.Count == 0)
            {
                strError = "尚未选定要进行上下移动的事项";
                return -1;
            }

            // ListViewItem item = list.SelectedItems[0];
            // int index = list.Items.IndexOf(item);
            // Debug.Assert(index >= 0 && index <= list.Items.Count - 1, "");
            int index = list.SelectedIndices[0];
            ListViewItem item = list.Items[index];

            indices.Add(index);

            bool bChanged = false;

            if (bUp == true)
            {
                if (index == 0)
                {
                    strError = "到头";
                    return -1;
                }

                list.Items.RemoveAt(index);
                index--;
                list.Items.Insert(index, item);
                indices.Add(index);
                list.FocusedItem = item;

                bChanged = true;
            }

            if (bUp == false)
            {
                if (index >= list.Items.Count - 1)
                {
                    strError = "到尾";
                    return -1;
                }
                list.Items.RemoveAt(index);
                index++;
                list.Items.Insert(index, item);
                indices.Add(index);
                list.FocusedItem = item;

                bChanged = true;
            }

            if (bChanged == true)
                return 1;
            return 0;
        }

        public static void DeleteSelectedItems(ListView list)
        {
            /*
            int[] indices = new int[list.SelectedItems.Count];
            list.SelectedIndices.CopyTo(indices, 0);
            */

            list.BeginUpdate();

            // 2021/6/11 新方法
            var indices = list.SelectedIndices.Cast<int>().Reverse();
            foreach (var index in indices)
            {
                list.Items.RemoveAt(index);
            }

            /*
            for (int i = indices.Length - 1; i >= 0; i--)
            {
                list.Items.RemoveAt(indices[i]);
            }
            */

            list.EndUpdate();

#if NO
            for (int i = list.SelectedIndices.Count - 1;
    i >= 0;
    i--)
            {
                int index = list.SelectedIndices[i];
                list.Items.RemoveAt(index);
            }
#endif
#if NO
            foreach (ListViewItem item in list.SelectedItems)
            {
                list.Items.Remove(item);
            }
#endif
        }

        public static void SelectAllLines(ListView list)
        {
#if NO
            list.BeginUpdate();
            foreach (ListViewItem item in list.Items)
            {
                item.Selected = true;
            }
            list.EndUpdate();
#endif
            SelectAllItems(list);
        }

        #region

        private const int LVM_FIRST = 0x1000;
        private const int LVM_SETITEMSTATE = LVM_FIRST + 43;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct LVITEM
        {
            public int mask;
            public int iItem;
            public int iSubItem;
            public int state;
            public int stateMask;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pszText;
            public int cchTextMax;
            public int iImage;
            public IntPtr lParam;
            public int iIndent;
            public int iGroupId;
            public int cColumns;
            public IntPtr puColumns;
        };

        [DllImport("user32.dll", EntryPoint = "SendMessage", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessageLVItem(IntPtr hWnd, int msg, int wParam, ref LVITEM lvi);

        /// <summary>
        /// Select all rows on the given listview
        /// </summary>
        /// <param name="list">The listview whose items are to be selected</param>
        public static void SelectAllItems(ListView list)
        {
            // 2020/12/10
            if (list.MultiSelect == false)
                throw new ArgumentException("ListView.MultiSelect 为 false，不支持 SelectAllItems()");
            SetItemState(list, -1, 2, 2);
        }

        /// <summary>
        /// Deselect all rows on the given listview
        /// </summary>
        /// <param name="list">The listview whose items are to be deselected</param>
        public static void DeselectAllItems(ListView list)
        {
            SetItemState(list, -1, 2, 0);
        }

        /// <summary>
        /// Set the item state on the given item
        /// </summary>
        /// <param name="list">The listview whose item's state is to be changed</param>
        /// <param name="itemIndex">The index of the item to be changed</param>
        /// <param name="mask">Which bits of the value are to be set?</param>
        /// <param name="value">The value to be set</param>
        public static void SetItemState(ListView list, int itemIndex, int mask, int value)
        {
            LVITEM lvItem = new LVITEM();
            lvItem.stateMask = mask;
            lvItem.state = value;
            SendMessageLVItem(list.Handle, LVM_SETITEMSTATE, itemIndex, ref lvItem);
        }

        #endregion

        // 获得列标题宽度字符串
        public static string GetColumnWidthListString(ListView list)
        {
            string strResult = "";
            for (int i = 0; i < list.Columns.Count; i++)
            {
                ColumnHeader header = list.Columns[i];
                if (i != 0)
                    strResult += ",";
                strResult += header.Width.ToString();
            }

            return strResult;
        }

        // 获得列标题宽度字符串
        // 扩展功能版本。不包含右边连续的没有标题文字的栏
        public static string GetColumnWidthListStringExt(ListView list)
        {
            string strResult = "";
            int nEndIndex = list.Columns.Count;
            for (int i = list.Columns.Count - 1; i >= 0; i--)
            {
                ColumnHeader header = list.Columns[i];
                if (String.IsNullOrEmpty(header.Text) == false)
                    break;
                nEndIndex = i;
            }
            for (int i = 0; i < nEndIndex; i++)
            {
                ColumnHeader header = list.Columns[i];
                if (i != 0)
                    strResult += ",";
                strResult += header.Width.ToString();
            }

            return strResult;
        }

        // 设置列标题的宽度
        // parameters:
        //      bExpandColumnCount  是否要扩展列标题到足够数目？
        public static void SetColumnHeaderWidth(ListView list,
            string strWidthList,
            bool bExpandColumnCount)
        {
            string[] parts = strWidthList.Split(new char[] { ',' });

            if (bExpandColumnCount == true)
                EnsureColumns(list, parts.Length, 100);

            for (int i = 0; i < parts.Length; i++)
            {
                if (i >= list.Columns.Count)
                    break;

                string strValue = parts[i].Trim();
                int nWidth = -1;
                try
                {
                    nWidth = Convert.ToInt32(strValue);
                }
                catch
                {
                    break;
                }

                if (nWidth != -1)
                    list.Columns[i].Width = nWidth;
            }
        }



        // 响应选择标记发生变化的动作，修改栏目标题文字
        // parameters:
        //      protect_column_numbers  需要保护的列的列号数组。列号从0开始计算。所谓保护就是不破坏这样的列的标题，设置标题从它们以外的列开始算起。nRecPathColumn表示的列号不必纳入本数组，也会自动受到保护。如果不需要本参数，可以用null
        public static void OnSelectedIndexChanged(ListView list,
            int nRecPathColumn,
            List<int> protect_column_numbers)
        {
            ListViewProperty prop = GetListViewProperty(list);

            if (prop == null)
            {
                throw new Exception("ListView 的 Tag 没有包含 ListViewProperty 对象");
            }

            if (list.SelectedItems.Count == 0)
            {
                // 清除所有栏目标题为1,2,3...，或者保留以前的残余值?
                return;
            }

            ListViewItem item = list.SelectedItems[0];
            // 获得路径。假定都在第一列？
            string strRecPath = GetItemText(item, nRecPathColumn);

            ColumnPropertyCollection props = null;
            string strDbName = "";

            if (String.IsNullOrEmpty(strRecPath) == true)
            {
                strDbName = "<blank>";  // 特殊的数据库名，表示第一列空的情况
                props = prop.GetColumnName(strDbName);
                goto DO_REFRESH;
            }

            // 取出数据库名
            strDbName = prop.ParseDbName(strRecPath);   //  GetDbName(strRecPath);

            if (String.IsNullOrEmpty(strDbName) == true)
            {
                return;
            }

            if (strDbName == prop.CurrentDbName)
                return; // 没有必要刷新

            props = prop.GetColumnName(strDbName);

        DO_REFRESH:

            if (props == null)
            {
                // not found

                // 清除所有栏目标题为1,2,3...，或者保留以前的残余值?
                props = new ColumnPropertyCollection();
            }

            // 修改文字
            int index = 0;
            for (int i = 0; i < list.Columns.Count; i++)
            {
                ColumnHeader header = list.Columns[i];

                if (i == nRecPathColumn)
                    continue;

                // 越过需要保护的列
                if (protect_column_numbers != null)
                {
                    if (protect_column_numbers.IndexOf(i) != -1)
                        continue;
                }

#if NO
                if (index < props.Count)
                {
                    if (header.Tag != null)
                        header.Tag = props[index];
                    else
                        header.Text = props[index].Title;
                }
                else 
                {
                    ColumnProperty temp = (ColumnProperty)header.Tag;

                    if (temp == null)
                        header.Text = i.ToString();
                    else
                        header.Text = temp.Title;
                }
#endif

                ColumnProperty temp = (ColumnProperty)header.Tag;

                if (index < props.Count)
                {
                    if (temp != props[index])
                    {
                        header.Tag = props[index];
                        temp = props[index];
                    }
                }
                else
                    temp = null;    // 2013/10/5 多出来找不到定义的列，需要显示为数字

                if (temp == null)
                {
                    // 如果 header 以前有文字就沿用，没有时才使用编号填充 2014/9/6 消除 BUG
                    if (string.IsNullOrEmpty(header.Text) == true)
                        header.Text = i.ToString();
                }
                else
                    header.Text = temp.Title;

                index++;
            }

            // 刷新排序列的显示。也就是说刷新那些参与了排序的个别列的显示
            prop.SortColumns.RefreshColumnDisplay(list.Columns);

            prop.CurrentDbName = strDbName; // 记忆
        }

        // 响应点击栏目标题的动作，进行排序
        // parameters:
        //      bClearSorter    是否在排序后清除 sorter 函数
        public static void OnColumnClick(ListView list,
            ColumnClickEventArgs e,
            bool bClearSorter = true)
        {
            int nClickColumn = e.Column;

            ListViewProperty prop = GetListViewProperty(list);

            if (prop == null)
            {
                throw new Exception("ListView的Tag没有包含ListViewProperty对象");
            }

            // 2013/3/31
            // 如果标题栏没有初始化，则需要先初始化
            if (list.SelectedItems.Count == 0 && list.Items.Count > 0)
            {
                list.Items[0].Selected = true;
                OnSelectedIndexChanged(list,
                    0,
                    null);
                list.Items[0].Selected = false;
            }

            ColumnSortStyle sortStyle = prop.GetSortStyle(list, nClickColumn);

            prop.SortColumns.SetFirstColumn(nClickColumn,
                sortStyle,
                list.Columns,
                true);

            // 排序
            SortColumnsComparer sorter = new SortColumnsComparer(prop.SortColumns);
            if (prop.HasCompareColumnEvent() == true)
            {
                sorter.EventCompare += (sender1, e1) =>
                {
                    prop.OnCompareColumn(sender1, e1);
                };
            }
            list.ListViewItemSorter = sorter;

            if (bClearSorter == true)
                list.ListViewItemSorter = null;
        }

        class SetSortStyleParam
        {
            public ColumnSortStyle Style;
            public ListViewProperty prop = null;
            public int ColumnIndex = -1;
        }

        // 响应鼠标右键点击栏目标题的动作，出现上下文菜单
        public static void OnColumnContextMenuClick(ListView list,
            ColumnClickEventArgs e)
        {
            int nClickColumn = e.Column;

            ListViewProperty prop = GetListViewProperty(list);

            if (prop == null)
            {
                throw new Exception("ListView的Tag没有包含ListViewProperty对象");
            }

#if NO
            ColumnSortStyle sortStyle = prop.GetSortStyle(nClickColumn);
            prop.SortColumns.SetFirstColumn(nClickColumn,
                sortStyle,
                list.Columns,
                true);
#endif
            ContextMenuStrip contextMenu = new ContextMenuStrip();

            ToolStripMenuItem menuItem = null;
            ToolStripMenuItem subMenuItem = null;

            // list.Columns[nClickColumn].Text
            menuItem = new ToolStripMenuItem("设置排序方式");
            contextMenu.Items.Add(menuItem);

            ColumnSortStyle sortStyle = prop.GetSortStyle(list, nClickColumn);
            if (sortStyle == null)
                sortStyle = ColumnSortStyle.None;

            List<ColumnSortStyle> all_styles = prop.GetAllSortStyle(list, nClickColumn);

            foreach (ColumnSortStyle style in all_styles)
            {
                subMenuItem = new ToolStripMenuItem();
                subMenuItem.Text = GetSortStyleCaption(style);
                SetSortStyleParam param = new SetSortStyleParam();
                param.ColumnIndex = nClickColumn;
                param.prop = prop;
                param.Style = style;
                subMenuItem.Tag = param;
                subMenuItem.Click += new EventHandler(menu_setSortStyle_Click);
                if (style == sortStyle)
                    subMenuItem.Checked = true;
                menuItem.DropDown.Items.Add(subMenuItem);
            }

            Point p = list.PointToClient(Control.MousePosition);
            contextMenu.Show(list, p);
        }

        static string GetSortStyleCaption(ColumnSortStyle style)
        {
            string strName = style.Name;
            if (string.IsNullOrEmpty(strName) == true)
                return "[None]";

            // 将 call_number 形态转换为 CallNumber 形态
            string[] parts = strName.Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder text = new StringBuilder();
            foreach (string s in parts)
            {
                if (string.IsNullOrEmpty(s) == true)
                    continue;

                text.Append(char.ToUpper(s[0]));
                if (s.Length > 1)
                    text.Append(s.Substring(1));
            }

            return text.ToString();
        }

        static void menu_setSortStyle_Click(object sender, EventArgs e)
        {
            var menu = sender as ToolStripMenuItem;
            var param = menu.Tag as SetSortStyleParam;
            param.prop.SetSortStyle(param.ColumnIndex, param.Style);
        }

        // 清除所有留存的排序信息，刷新list的标题栏上的陈旧的排序标志
        public static void ClearSortColumns(ListView list)
        {
            ListViewProperty prop = GetListViewProperty(list);

            if (prop == null)
                return;

            prop.SortColumns.Clear();
            SortColumns.ClearColumnSortDisplay(list.Columns);

            prop.CurrentDbName = "";    // 清除记忆
        }

        // 获得ListViewProperty对象
        public static ListViewProperty GetListViewProperty(ListView list)
        {
            if (list.Tag == null)
                return null;
            if (!(list.Tag is ListViewProperty))
                return null;

            return (ListViewProperty)list.Tag;
        }

        // 查找一个事项
        public static ListViewItem FindItem(ListView listview,
            string strText,
            int nColumn)
        {
            for (int i = 0; i < listview.Items.Count; i++)
            {
                ListViewItem item = listview.Items[i];
                string strThisText = GetItemText(item, nColumn);
                if (strThisText == strText)
                    return item;
            }

            return null;
        }

        // 检测一个x位置在何列上。
        // return:
        //		-1	没有命中
        //		其他 列号
        public static int ColumnHitTest(ListView listview,
            int x)
        {
            int nStart = 0;
            for (int i = 0; i < listview.Columns.Count; i++)
            {
                ColumnHeader header = listview.Columns[i];
                if (x >= nStart && x < nStart + header.Width)
                    return i;
                nStart += header.Width;
            }

            return -1;
        }

        // 确保列标题数量足够
        public static void EnsureColumns(ListView listview,
            int nCount,
            int nInitialWidth = 200)
        {
            if (listview.Columns.Count >= nCount)
                return;

            for (int i = listview.Columns.Count; i < nCount; i++)
            {
                string strText = "";
                // strText = Convert.ToString(i);

                ColumnHeader col = new ColumnHeader();
                col.Text = strText;
                col.Width = nInitialWidth;
                listview.Columns.Add(col);
            }
        }

        // 2022/5/10
        public static string InvokeGetItemText(ListViewItem item,
            int col)
        {
            return (string)item.ListView.Invoke((Func<string>)(() =>
            {
                return GetItemText(item, col);
            }));
        }

        // 获得一个单元的值
        public static string GetItemText(ListViewItem item,
            int col)
        {
            if (col == 0)
                return item.Text;

            // 2008/5/14。否则会抛出异常
            if (col >= item.SubItems.Count)
                return "";

            return item.SubItems[col].Text;
        }

        // 修改一个单元的值
        public static void ChangeItemText(ListViewItem item,
            int col,
            string strText)
        {
            // 确保线程安全 2014/9/3
            if (item.ListView != null && item.ListView.InvokeRequired)
            {
                item.ListView.BeginInvoke(new Action<ListViewItem, int, string>(ChangeItemText), item, col, strText);
                return;
            }

            if (col == 0)
            {
                item.Text = strText;
                return;
            }

            // 保险
            while (item.SubItems.Count < col + 1)   // 原来为<=, 会造成多加一列的后果 2006/10/9 changed
            {
                item.SubItems.Add("");
            }

#if NO
            item.SubItems.RemoveAt(col);
            item.SubItems.Insert(col, new ListViewItem.ListViewSubItem(item, strText));
#endif
            item.SubItems[col].Text = strText;
        }

        // 2009/10/21
        // 获得一个行的值。即把各个单元的值用\t字符连接起来
        public static string GetLineText(ListViewItem item)
        {
            string strResult = "";
            for (int i = 0; i < item.SubItems.Count; i++)
            {
                if (i > 0)
                    strResult += "\t";

                strResult += item.SubItems[i].Text;
            }

            return strResult;
        }

        // 清除全部选择状态
        public static void ClearSelection(ListView list)
        {
            list.SelectedItems.Clear();
        }

        // 清除全部 Checked 状态
        public static void ClearChecked(ListView list)
        {
            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in list.CheckedItems)
            {
                items.Add(item);
            }

            foreach (ListViewItem item in items)
            {
                item.Checked = false;
            }
        }

        // 选择一行
        // parameters:
        //		nIndex	要设置选择标记的行。如果==-1，表示清除全部选择标记但不选择。
        //		bMoveFocus	是否同时移动focus标志到所选择行
        public static void SelectLine(ListView list,
            int nIndex,
            bool bMoveFocus)
        {
            list.SelectedItems.Clear();

            if (nIndex != -1)
            {
                list.Items[nIndex].Selected = true;

                if (bMoveFocus == true)
                {
                    list.Items[nIndex].Focused = true;
                }
            }
        }

        // 选择一行
        // 2008/9/9
        // parameters:
        //		bMoveFocus	是否同时移动focus标志到所选择行
        public static void SelectLine(ListViewItem item,
            bool bMoveFocus)
        {
            Debug.Assert(item != null, "");

            item.ListView.SelectedItems.Clear();

            item.Selected = true;

            if (bMoveFocus == true)
            {
                item.Focused = true;
            }
        }

        public static List<int> GetSelectedIndices(ListView list)
        {
            return new List<int>(list.SelectedIndices.Cast<int>());
        }

        public static void SelectItems(ListView list, List<int> indices)
        {
            ClearSelection(list);
            foreach (int i in indices)
            {
                var item = list.Items[i];
                item.Selected = true;
            }
        }

        // 查找并选中
        public static int FindAndSelect(ListView list, string text)
        {
            int count = 0;
            foreach (ListViewItem item in list.Items)
            {
                bool found = false;
                foreach (ListViewItem.ListViewSubItem subitem in item.SubItems)
                {
                    if (subitem.Text.Contains(text))
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    item.Selected = true;
                    count++;
                }
                else
                    item.Selected = false;
            }

            return count;
        }
    }

    public static class ListBoxUtil
    {
        public static void EnsureVisible(ListBox listBox, int nItemIndex)
        {
            int visibleItems = listBox.ClientSize.Height / listBox.ItemHeight;
            listBox.TopIndex = Math.Max(nItemIndex - visibleItems + 1, 0);
        }
    }

    public class ListViewProperty
    {
        public string CurrentDbName = ""; // 当前已经显示的标题所对应的数据库名。为了加快速度

        public event GetColumnTitlesEventHandler GetColumnTitles = null;
        public event ParsePathEventHandler ParsePath = null;
        public event CompareEventHandler CompareColumn = null;

        // 参与排序的列号数组
        public SortColumns SortColumns = new SortColumns();

        public List<ColumnSortStyle> SortStyles = new List<ColumnSortStyle>();

        public Hashtable UsedColumnTitles = new Hashtable();   // key为数据库名，value为List<string>

        public void ClearCache()
        {
            this.UsedColumnTitles.Clear();
            this.CurrentDbName = "";
        }

        public void OnCompareColumn(object sender, CompareEventArgs e)
        {
            if (this.CompareColumn != null)
                this.CompareColumn(sender, e);
        }

        public bool HasCompareColumnEvent()
        {
            if (this.CompareColumn != null)
                return true;
            return false;
        }

        // 获得一个列可用的全部 sort style
        public List<ColumnSortStyle> GetAllSortStyle(ListView list, int nColumn)
        {
            List<ColumnSortStyle> styles = new List<ColumnSortStyle>();
            styles.Add(ColumnSortStyle.None); // 没有
            styles.Add(ColumnSortStyle.LeftAlign); // 左对齐字符串
            styles.Add(ColumnSortStyle.RightAlign);// 右对齐字符串
            styles.Add(ColumnSortStyle.RecPath);    // 记录路径。例如“中文图书/1”，以'/'为界，右边部分当作数字值排序。或者“localhost/中文图书/ctlno/1”
            styles.Add(ColumnSortStyle.LongRecPath);  // 记录路径。例如“中文图书/1 @本地服务器”
            // 2021/10/9
            styles.Add(ColumnSortStyle.RFC1123);  // RFC1123 时间字符串
            styles.Add(ColumnSortStyle.IpAddress);
            styles.Add(ColumnSortStyle.Width);

            // 寻找标题 .Tag 中的定义
            if (nColumn < list.Columns.Count)
            {
                ColumnHeader header = list.Columns[nColumn];
                ColumnProperty prop = (ColumnProperty)header.Tag;
                if (prop != null)
                {
                    if (string.IsNullOrEmpty(prop.Type) == false)
                    {
                        ColumnSortStyle default_style = new ColumnSortStyle(prop.Type);
                        if (styles.IndexOf(default_style) == -1)
                            styles.Add(default_style);
                    }
                }
            }
            return styles;
        }

        public ColumnSortStyle GetSortStyle(ListView list, int nColumn)
        {
            ColumnSortStyle result = null;
            if (this.SortStyles.Count <= nColumn)
            {
            }
            else
                result = SortStyles[nColumn];

            if (result == null || result == ColumnSortStyle.None)
            {
                // 寻找标题 .Tag 中的定义
                if (nColumn < list.Columns.Count)
                {
                    ColumnHeader header = list.Columns[nColumn];
                    ColumnProperty prop = (ColumnProperty)header.Tag;
                    if (prop != null)
                    {
                        if (string.IsNullOrEmpty(prop.Type) == false)
                            return new ColumnSortStyle(prop.Type);
                    }
                }
            }
            return result;
        }

        public void SetSortStyle(int nColumn, ColumnSortStyle style)
        {
            // 确保元素足够
            while (this.SortStyles.Count < nColumn + 1)
            {
                this.SortStyles.Add(null); // 或者 .None // 缺省的 ColumnSortStyle.LeftAlign
            }

            this.SortStyles[nColumn] = style;

            // 2013/3/27
            // 刷新 SortColumns
            foreach (Column column in this.SortColumns)
            {
                if (column.No == nColumn)
                    column.SortStyle = style;
            }
        }

        public ColumnPropertyCollection GetColumnName(string strDbName)
        {
            // 先从Hashtable中寻找
            if (this.UsedColumnTitles.Contains(strDbName) == true)
                return (ColumnPropertyCollection)this.UsedColumnTitles[strDbName];

            if (this.GetColumnTitles != null)
            {
                GetColumnTitlesEventArgs e = new GetColumnTitlesEventArgs();
                e.DbName = strDbName;
                e.ListViewProperty = this;
                this.GetColumnTitles(this, e);
                if (e.ColumnTitles != null)
                {
                    this.UsedColumnTitles[strDbName] = e.ColumnTitles;
                }
                return e.ColumnTitles;
            }

            return null;    // not found
        }

        public string ParseDbName(string strPath)
        {
            if (this.ParsePath != null)
            {
                ParsePathEventArgs e = new ParsePathEventArgs();
                e.Path = strPath;
                this.ParsePath(this, e);
                return e.DbName;
            }

            // 如果是 "中文图书/3" 则返回数据库名，如果是"中文图书/1@本地服务器"则返回全路径
            return GetDbName(strPath);
        }

        // 从路径中取出库名部分
        // parammeters:
        //      strPath 路径。例如"中文图书/3"
        public static string GetDbName(string strPath)
        {
            // 看看是否有服务器名部分 2015/8/12
            int nRet = strPath.IndexOf("@");
            if (nRet != -1)
            {
                return strPath; // 返回全路径
            }

            nRet = strPath.LastIndexOf("/");
            if (nRet == -1)
                return strPath;

            return strPath.Substring(0, nRet).Trim();
        }
#if NO
        // 从路径中取出库名部分
        // parammeters:
        //      strPath 路径。例如"中文图书/3"
        public static string GetDbName(string strPath)
        {
            // 看看是否有服务器名部分 2015/8/11
            string strServerName = "";
            int nRet = strPath.IndexOf("@");
            if (nRet != -1)
            {
                strServerName = strPath.Substring(nRet).Trim(); // 包含字符 '@'
                strPath = strPath.Substring(0, nRet).Trim();
            }

            nRet = strPath.LastIndexOf("/");
            if (nRet == -1)
                return strPath + strServerName;

            return strPath.Substring(0, nRet).Trim() + strServerName;
        }
#endif
    }

    /// <summary>
    /// 获得栏目标题
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    public delegate void GetColumnTitlesEventHandler(object sender,
    GetColumnTitlesEventArgs e);

    // 2013/3/31
    /// <summary>
    /// 一个栏目的属性
    /// </summary>
    public class ColumnProperty
    {
        /// <summary>
        /// 栏目标题
        /// </summary>
        public string Title = "";   // 栏目标题

        /// <summary>
        /// 数值类型
        /// </summary>
        public string Type = "";    // 数值类型。排序时有用

        /// <summary>
        /// XPath
        /// </summary>
        public string XPath = "";   // XPath 字符串 2015/8/27

        /// <summary>
        /// 字符串转换方法
        /// </summary>
        public string Convert = ""; // 字符串转换方法 2015/8/27

        public ColumnProperty(string strTitle,
            string strType = "",
            string strXPath = "",
            string strConvert = "")
        {
            this.Title = strTitle;
            this.Type = strType;
            this.XPath = strXPath;
            this.Convert = strConvert;
        }
    }

    /// <summary>
    /// 栏目属性集合
    /// </summary>
    public class ColumnPropertyCollection : List<ColumnProperty>
    {
        /// <summary>
        /// 追加一个栏目属性对象
        /// </summary>
        /// <param name="strTitle">标题</param>
        /// <param name="strType">类型</param>
        public void Add(string strTitle,
            string strType = "",
            string strXPath = "",
            string strConvert = "")
        {
            ColumnProperty prop = new ColumnProperty(strTitle, strType, strXPath, strConvert);
            base.Add(prop);
        }

        /// <summary>
        /// 插入一个栏目属性对象
        /// </summary>
        /// <param name="nIndex">插入位置下标</param>
        /// <param name="strTitle">标题</param>
        /// <param name="strType">类型</param>
        public void Insert(int nIndex, string strTitle, string strType = "")
        {
            ColumnProperty prop = new ColumnProperty(strTitle, strType);
            base.Insert(nIndex, prop);
        }

        /// <summary>
        /// 根据 type 值查找列号
        /// </summary>
        /// <returns>-1: 没有找到; 其他: 列号</returns>
        public int FindColumnByType(string strType)
        {
            int index = 0;
            foreach (ColumnProperty col in this)
            {
                if (col.Type == strType)
                    return index;
                index++;
            }
            return -1;
        }
    }

    /// <summary>
    /// 获得栏目标题的参数
    /// </summary>
    public class GetColumnTitlesEventArgs : EventArgs
    {
        public string DbName = "";  // [in] 如果值为"<blank>"，表示第一列为空的情况，例如keys的情形
        public ListViewProperty ListViewProperty = null;    // [in][out]

        // public List<string> ColumnTitles = null;  // [out] null表示not found；而.Count == 0表示栏目标题为空，并且不是not found
        public ColumnPropertyCollection ColumnTitles = null;  // [out] null表示not found；而.Count == 0表示栏目标题为空，并且不是not found

        // public string ErrorInfo = "";    // [out]
    }

    /// <summary>
    /// 解释路径
    /// </summary>
    /// <param name="sender">发送者</param>
    /// <param name="e">事件参数</param>
    public delegate void ParsePathEventHandler(object sender,
    ParsePathEventArgs e);

    /// <summary>
    /// 解析路径的参数
    /// </summary>
    public class ParsePathEventArgs : EventArgs
    {
        public string Path = "";    // [in]
        public string DbName = "";    // [out]  // 数据库名部分。可能包含服务器名称部分
    }

    // 栏目排序方式
    public class ColumnSortStyle
    {
        public string Name = "";
        public CompareEventHandler CompareFunc = null;

        public ColumnSortStyle(string strStyle)
        {
            this.Name = strStyle;
        }

        public static ColumnSortStyle None
        {
            get
            {
                return new ColumnSortStyle("");
            }
        }

        public static ColumnSortStyle LeftAlign
        {
            get
            {
                return new ColumnSortStyle("LeftAlign");
            }
        }

        public static ColumnSortStyle RightAlign
        {
            get
            {
                return new ColumnSortStyle("RightAlign");
            }
        }

        public static ColumnSortStyle RecPath
        {
            get
            {
                return new ColumnSortStyle("RecPath");
            }
        }

        public static ColumnSortStyle LongRecPath
        {
            get
            {
                return new ColumnSortStyle("LongRecPath");
            }
        }

        public static ColumnSortStyle IpAddress
        {
            get
            {
                return new ColumnSortStyle("IpAddress");
            }
        }

        // 2021/10/9
        public static ColumnSortStyle RFC1123
        {
            get
            {
                return new ColumnSortStyle("RFC1123");
            }
        }

        // 2022/6/4
        public static ColumnSortStyle Width
        {
            get
            {
                return new ColumnSortStyle("Width");
            }
        }

        public override bool Equals(System.Object obj)
        {
            ColumnSortStyle o = obj as ColumnSortStyle;
            if ((object)o == null)
                return false;

            if (this.Name == o.Name)
                return true;
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator ==(ColumnSortStyle a, ColumnSortStyle b)
        {
            if (System.Object.ReferenceEquals(a, b))
                return true;
            if ((object)a == null || (object)b == null)
                return false;

            return a.Name == b.Name;
        }

        public static bool operator !=(ColumnSortStyle a, ColumnSortStyle b)
        {
            return !(a == b);
        }
    }

    public class Column
    {
        public int No = -1;
        public bool Asc = true;
        public ColumnSortStyle SortStyle = ColumnSortStyle.None;   // ColumnSortStyle.LeftAlign;
    }

    public class SortColumns : List<Column>
    {
        // 包装版本，兼容以前的格式
        // 如果针对同一列反复调用此函数，则排序方向会toggle
        // 所以，不能用本函数来设定固定的排序方向
        public void SetFirstColumn(int nFirstColumn,
            ListView.ColumnHeaderCollection columns)
        {
            SetFirstColumn(nFirstColumn,
                columns,
                true);
        }


        // parameters:
        //      bToggleDirection    ==true 若nFirstColumn本来已经是当前第一列，则更换其排序方向
        public void SetFirstColumn(int nFirstColumn,
            ListView.ColumnHeaderCollection columns,
            bool bToggleDirection)
        {
            int nIndex = -1;
            Column column = null;
            // 找到这个列号
            for (int i = 0; i < this.Count; i++)
            {
                column = this[i];
                if (column.No == nFirstColumn)
                {
                    nIndex = i;
                    break;
                }
            }

            ColumnSortStyle firstColumnStyle = ColumnSortStyle.None;   //  ColumnSortStyle.LeftAlign;

            // 自动设置右对齐风格
            // 2008/8/30 changed
            if (columns[nFirstColumn].TextAlign == HorizontalAlignment.Right)
                firstColumnStyle = ColumnSortStyle.RightAlign;

            // 本来已经是第一列，则更换排序方向
            if (nIndex == 0 && bToggleDirection == true)
            {
                if (column.Asc == true)
                    column.Asc = false;
                else
                    column.Asc = true;

                // 修改这一列的视觉
                ColumnHeader header = columns[column.No];

                SetHeaderText(header,
                    nIndex,
                    column);
                return;
            }

            if (nIndex != -1)
            {
                // 从数组中移走已经存在的值
                this.RemoveAt(nIndex);
            }
            else
            {
                column = new Column();
                column.No = nFirstColumn;
                column.Asc = true;  // 初始时为正向排序
                column.SortStyle = firstColumnStyle;    // 2007/12/20
            }

            // 放到首部
            this.Insert(0, column);

            // 修改全部列的视觉
            RefreshColumnDisplay(columns);
        }

        // 修改排序数组，设置第一列，把原来的列号推后
        // parameters:
        //      bToggleDirection    ==true 若nFirstColumn本来已经是当前第一列，则更换其排序方向
        public void SetFirstColumn(int nFirstColumn,
            ColumnSortStyle firstColumnStyle,
            ListView.ColumnHeaderCollection columns,
            bool bToggleDirection)
        {
            int nIndex = -1;
            Column column = null;
            // 找到这个列号
            for (int i = 0; i < this.Count; i++)
            {
                column = this[i];
                if (column.No == nFirstColumn)
                {
                    nIndex = i;
                    break;
                }
            }

            // 本来已经是第一列，则更换排序方向
            if (nIndex == 0 && bToggleDirection == true)
            {
                if (column.Asc == true)
                    column.Asc = false;
                else
                    column.Asc = true;

                column.SortStyle = firstColumnStyle;    // 2008/11/30

                // 修改这一列的视觉
                ColumnHeader header = columns[column.No];

                SetHeaderText(header,
                    nIndex,
                    column);
                return;
            }

            if (nIndex != -1)
            {
                // 从数组中移走已经存在的值
                this.RemoveAt(nIndex);
            }
            else
            {
                column = new Column();
                column.No = nFirstColumn;
                column.Asc = true;  // 初始时为正向排序
                column.SortStyle = firstColumnStyle;    // 2007/12/20
            }

            // 放到首部
            this.Insert(0, column);

            // 修改全部列的视觉
            RefreshColumnDisplay(columns);
        }

        void DisplayColumnsText(ListView.ColumnHeaderCollection columns)
        {
            Debug.WriteLine("***");
            foreach (ColumnHeader column0 in columns)
            {
                Debug.WriteLine(column0.Text);
            }
            Debug.WriteLine("***");
        }

        // 修改全部列的视觉
        public void RefreshColumnDisplay(ListView.ColumnHeaderCollection columns)
        {
#if DEBUG
            DisplayColumnsText(columns);
#endif
            Column column = null;
            for (int i = 0; i < this.Count; i++)
            {
                column = this[i];

                ColumnHeader header = columns[column.No];

                SetHeaderText(header,
                    i,
                    column);
            }
#if DEBUG
            DisplayColumnsText(columns);
#endif
        }

        // 恢复没有任何排序标志的列标题文字内容
        public static void ClearColumnSortDisplay(ListView.ColumnHeaderCollection columns)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                ColumnHeader header = columns[i];

                ColumnProperty prop = (ColumnProperty)header.Tag;
                if (prop != null)
                {
                    header.Text = prop.Title;
                }
            }
        }

        // 设置ColumnHeader文字
        public static void SetHeaderText(ColumnHeader header,
            int nSortNo,
            Column column)
        {
            ColumnProperty prop = (ColumnProperty)header.Tag;

            //string strOldText = "";
            if (prop != null)
            {
                // strOldText = (string)header.Tag;
            }
            else
            {
                // strOldText = header.Text;
                // 记忆下来
                prop = new ColumnProperty(header.Text);
                header.Tag = prop;
            }

            string strNewText =
                (column.Asc == true ? "▲" : "▼")
                + (nSortNo + 1).ToString()
                + " "
                + prop.Title;   //  strOldText;
            header.Text = strNewText;

            // 2008/11/30
            if (column.SortStyle == ColumnSortStyle.RightAlign)
            {
                if (header.TextAlign != HorizontalAlignment.Right)
                    header.TextAlign = HorizontalAlignment.Right;
            }
            else
            {
                if (header.TextAlign != HorizontalAlignment.Left)
                    header.TextAlign = HorizontalAlignment.Left;
            }
        }
    }

    // Implements the manual sorting of items by columns.
    public class SortColumnsComparer : IComparer
    {
        SortColumns SortColumns = new SortColumns();

        // 当一个 SortStyle 不是预知的类型的时候，使用这个 handler 排序
        public event CompareEventHandler EventCompare = null;

        public SortColumnsComparer()
        {
            Column column = new Column();
            column.No = 0;
            this.SortColumns.Add(column);
        }

        public SortColumnsComparer(SortColumns sortcolumns)
        {
            this.SortColumns = sortcolumns;
        }

        // 将记录路径切割为两个部分：左边部分和右边部分。
        // 中文图书/1
        // 右边部分是从右开始找到第一个'/'右边的部分，所以不论路径长短，一定是最右边的数字部分
        static void SplitRecPath(string strRecPath,
            out string strLeft,
            out string strRight)
        {
            int nRet = strRecPath.LastIndexOf("/");
            if (nRet == -1)
            {
                strLeft = strRecPath; // 如果没有斜杠，则当作左边部分。这一点有何意义还需要仔细考察
                strRight = "";
                return;
            }

            strLeft = strRecPath.Substring(0, nRet);
            strRight = strRecPath.Substring(nRet + 1);
        }

        static void SplitLongRecPath(string strRecPath,
            out string strLeft,
            out string strRight,
            out string strServerName)
        {
            int nRet = 0;

            nRet = strRecPath.IndexOf("@");
            if (nRet != -1)
            {
                strServerName = strRecPath.Substring(nRet + 1).Trim();
                strRecPath = strRecPath.Substring(0, nRet).Trim();
            }
            else
                strServerName = "";

            nRet = strRecPath.LastIndexOf("/");
            if (nRet == -1)
            {
                strLeft = strRecPath;
                strRight = "";
                return;
            }

            strLeft = strRecPath.Substring(0, nRet);
            strRight = strRecPath.Substring(nRet + 1);
        }

        // 右对齐比较字符串
        // parameters:
        //      chFill  填充用的字符
        public static int RightAlignCompare(string s1, string s2, char chFill = '0')
        {
            if (s1 == null)
                s1 = "";
            if (s2 == null)
                s2 = "";
            int nMaxLength = Math.Max(s1.Length, s2.Length);
            return string.CompareOrdinal(s1.PadLeft(nMaxLength, chFill),
                s2.PadLeft(nMaxLength, chFill));
        }

        // 比较两个 IP 地址
        public static int CompareIpAddress(string s1, string s2)
        {
            if (s1 == null)
                s1 = "";
            if (s2 == null)
                s2 = "";

            string[] parts1 = s1.Split(new char[] { '.', ':' });
            string[] parts2 = s2.Split(new char[] { '.', ':' });

            for (int i = 0; i < Math.Min(parts1.Length, parts2.Length); i++)
            {
                if (i >= parts1.Length)
                    break;
                if (i >= parts2.Length)
                    break;
                string n1 = parts1[i];
                string n2 = parts2[i];
                int nRet = RightAlignCompare(n1, n2);
                if (nRet != 0)
                    return nRet;
            }

            return (parts1.Length - parts2.Length);
        }

        // 2021/10/9
        public static int CompareRFC1123(string s1, string s2)
        {
            DateTime time1;
            DateTime time2;

            if (string.IsNullOrEmpty(s1))
                time1 = DateTime.MinValue;
            else
            {
                try
                {
                    time1 = FromRfc1123DateTimeString(s1);
                }
                catch
                {
                    time1 = DateTime.MinValue;
                }
            }

            if (string.IsNullOrEmpty(s2))
                time2 = DateTime.MinValue;
            else
            {
                try
                {
                    time2 = FromRfc1123DateTimeString(s2);
                }
                catch
                {
                    time2 = DateTime.MinValue;
                }
            }

            if (time1 == time2)
                return 0;
            if (time1 > time2)
                return 1;
            return -1;
        }

        // 先按照宽度进行比较，然后同宽度的按照普通 string 比较
        public static int CompareWidth(string s1, string s2)
        {
            if (s1.Length != s2.Length)
                return s1.Length - s2.Length;
            return string.CompareOrdinal(s1, s2);
        }

        #region RFC1123 时间处理

        // 把字符串转换为DateTime对象
        // 注意返回的是GMT时间
        // 注意可能抛出异常
        public static DateTime FromRfc1123DateTimeString(string strTime)
        {
            if (string.IsNullOrEmpty(strTime) == true)
                throw new Exception("时间字符串为空");

            string strError = "";
            string strMain = "";
            string strTimeZone = "";
            TimeSpan offset;
            // 将RFC1123字符串中的timezone部分分离出来
            // parameters:
            //      strMain [out]去掉timezone以后的左边部分
            //      strTimeZone [out]timezone部分
            int nRet = SplitRfc1123TimeZoneString(strTime,
            out strMain,
            out strTimeZone,
            out offset,
            out strError);
            if (nRet == -1)
                throw new Exception(strError);

            DateTime parsedBack;
            string[] formats = {
                "ddd, dd MMM yyyy HH':'mm':'ss",   // [ddd, ] 'GMT'
                "dd MMM yyyy HH':'mm':'ss",
                "ddd, dd MMM yyyy HH':'mm",
                "dd MMM yyyy HH':'mm",
                                };

            bool bRet = DateTime.TryParseExact(strMain,
                formats,
                DateTimeFormatInfo.InvariantInfo,
                DateTimeStyles.None,
                out parsedBack);
            if (bRet == false)
            {
                strError = "时间字符串 '" + strTime + "' 不是RFC1123格式";
                throw new Exception(strError);
            }

            return parsedBack - offset;
        }

        static TimeSpan GetOffset(string strDigital)
        {
            if (strDigital.Length != 5)
                throw new Exception("strDigital必须为5字符");

            int hours = Convert.ToInt32(strDigital.Substring(1, 2));
            int minutes = Convert.ToInt32(strDigital.Substring(3, 2));
            TimeSpan offset = new TimeSpan(hours, minutes, 0);
            if (strDigital[0] == '-')
                offset = new TimeSpan(offset.Ticks * -1);

            return offset;
        }

        // 将RFC1123字符串中的timezone部分分离出来
        // parameters:
        //      strMain [out]去掉timezone以后的左边部分// ，并去掉左边逗号以左的部分
        //      strTimeZone [out]timezone部分
        static int SplitRfc1123TimeZoneString(string strTimeParam,
            out string strMain,
            out string strTimeZone,
            out TimeSpan offset,
            out string strError)
        {
            strError = "";
            strMain = "";
            strTimeZone = "";
            offset = new TimeSpan(0);
            int nRet = 0;

            string strTime = strTimeParam.Trim();

            /*
            // 去掉逗号以左的部分
            int nRet = strTime.IndexOf(",");
            if (nRet != -1)
                strTime = strTime.Substring(nRet + 1).Trim();
             * */

            // 一位字母
            if (strTime.Length > 2
                && strTime[strTime.Length - 2] == ' ')
            {
                strMain = strTime.Substring(0, strTime.Length - 2).Trim();
                strTimeZone = strTime.Substring(strTime.Length - 1);
                if (strTimeZone == "J")
                {
                    strError = "RFC1123字符串 '" + strTimeParam + "' 格式错误： 最后一位TimeZone字符，不能为'J'";
                    return -1;
                }

                if (strTimeZone == "Z")
                    return 0;

                int nHours = 0;

                if (strTimeZone[0] >= 'A' && strTimeZone[0] < 'J')
                    nHours = -(strTimeZone[0] - 'A' + 1);
                else if (strTimeZone[0] >= 'K' && strTimeZone[0] <= 'M')
                    nHours = -(strTimeZone[0] - 'B' + 1);
                else if (strTimeZone[0] >= 'N' && strTimeZone[0] <= 'Y')
                    nHours = strTimeZone[0] - 'N' + 1;

                offset = new TimeSpan(nHours, 0, 0);
                return 0;
            }

            // ( "+" / "-") 4DIGIT
            if (strTime.Length > 5
                && (strTime[strTime.Length - 5] == '+' || strTime[strTime.Length - 5] == '-'))
            {
                strMain = strTime.Substring(0, strTime.Length - 5).Trim();
                strTimeZone = strTime.Substring(strTime.Length - 5);

                try
                {
                    offset = GetOffset(strTimeZone);
                }
                catch (Exception ex)
                {
                    strError = ex.Message;
                    return -1;
                }

                return 0;
            }

            string[] modes = {
                            "GMT",
                            "UT",
                            "EST",
                            "EDT",
                            "CST",
                            "CDT",
                            "MST",
                            "MDT",
                            "PST",
                            "PDT"};
            if (strTime.Length <= 3)
            {
                strError = "RFC1123字符串 '" + strTimeParam + "' 格式错误： 字符数不足";
                return -1;
            }

            string strPart = strTime.Substring(strTime.Length - 3);
            foreach (string mode in modes)
            {
                nRet = strPart.LastIndexOf(mode);
                if (nRet != -1)
                {
                    nRet = strTime.LastIndexOf(mode);
                    Debug.Assert(nRet != -1, "");

                    strMain = strTime.Substring(0, nRet).Trim();
                    strTimeZone = mode;

                    if (strTimeZone == "GMT" || strTimeZone == "UT")
                        return 0;

                    string strDigital = "";

                    switch (strTimeZone)
                    {
                        case "EST":
                            strDigital = "-0500";
                            break;
                        case "EDT":
                            strDigital = "-0400";
                            break;
                        case "CST":
                            strDigital = "-0600";
                            break;
                        case "CDT":
                            strDigital = "-0500";
                            break;
                        case "MST":
                            strDigital = "-0700";
                            break;
                        case "MDT":
                            strDigital = "-0600";
                            break;
                        case "PST":
                            strDigital = "-0800";
                            break;
                        case "PDT":
                            strDigital = "-0700";
                            break;
                        default:
                            strError = "error";
                            return -1;
                    }

                    try
                    {
                        offset = GetOffset(strDigital);
                    }
                    catch (Exception ex)
                    {
                        strError = ex.Message;
                        return -1;
                    }

                    return 0;
                }
            }

            strError = "RFC1123字符串 '" + strTimeParam + "' 格式错误： TimeZone部分不合法";
            return -1;
        }

        #endregion

        public int Compare(object x, object y)
        {
            for (int i = 0; i < this.SortColumns.Count; i++)
            {
                Column column = this.SortColumns[i];

                string s1 = "";
                try
                {
                    s1 = ((ListViewItem)x).SubItems[column.No].Text;
                }
                catch
                {
                }
                string s2 = "";
                try
                {
                    s2 = ((ListViewItem)y).SubItems[column.No].Text;
                }
                catch
                {
                }

                int nRet = 0;

                if (column.SortStyle == null)
                {
                    nRet = String.Compare(s1, s2);
                }
                else if (column.SortStyle.CompareFunc != null)
                {
                    // 如果有排序函数，直接用排序函数
                    CompareEventArgs e = new CompareEventArgs();
                    e.Column = column;
                    e.SortColumnIndex = i;
                    // e.ColumnIndex = column.No;
                    e.String1 = s1;
                    e.String2 = s2;
                    column.SortStyle.CompareFunc(this, e);
                    nRet = e.Result;
                }
                else if (column.SortStyle == ColumnSortStyle.None
                    || column.SortStyle == ColumnSortStyle.LeftAlign)
                {
                    nRet = String.Compare(s1, s2);
                }
                else if (column.SortStyle == ColumnSortStyle.RightAlign)
                {
#if NO
                    int nMaxLength = s1.Length;
                    if (s2.Length > nMaxLength)
                        nMaxLength = s2.Length;

                    s1 = s1.PadLeft(nMaxLength, ' ');
                    s2 = s2.PadLeft(nMaxLength, ' ');

                    nRet = String.Compare(s1, s2);
#endif
                    nRet = RightAlignCompare(s1, s2, ' ');
                }
                else if (column.SortStyle == ColumnSortStyle.RecPath)
                {
                    string strLeft1;
                    string strRight1;
                    string strLeft2;
                    string strRight2;
                    SplitRecPath(s1, out strLeft1, out strRight1);
                    SplitRecPath(s2, out strLeft2, out strRight2);

                    nRet = String.Compare(strLeft1, strLeft2);
                    if (nRet != 0)
                        goto END1;

#if NO
                    // 对记录号部分进行右对齐的比较
                    int nMaxLength = strRight1.Length;
                    if (strRight2.Length > nMaxLength)
                        nMaxLength = strRight2.Length;

                    strRight1 = strRight1.PadLeft(nMaxLength, ' ');
                    strRight2 = strRight2.PadLeft(nMaxLength, ' ');

                    nRet = String.Compare(strRight1, strRight2);
#endif
                    nRet = RightAlignCompare(strRight1, strRight2, ' ');
                }
                else if (column.SortStyle == ColumnSortStyle.LongRecPath)
                {
                    string strLeft1;
                    string strRight1;
                    string strServerName1;
                    string strLeft2;
                    string strRight2;
                    string strServerName2;

                    SplitLongRecPath(s1, out strLeft1, out strRight1, out strServerName1);
                    SplitLongRecPath(s2, out strLeft2, out strRight2, out strServerName2);

                    nRet = String.Compare(strServerName1, strServerName2);
                    if (nRet != 0)
                        goto END1;

                    nRet = String.Compare(strLeft1, strLeft2);
                    if (nRet != 0)
                        goto END1;

                    // 对记录号部分进行右对齐的比较
                    int nMaxLength = strRight1.Length;
                    if (strRight2.Length > nMaxLength)
                        nMaxLength = strRight2.Length;

                    strRight1 = strRight1.PadLeft(nMaxLength, ' ');
                    strRight2 = strRight2.PadLeft(nMaxLength, ' ');

                    nRet = String.Compare(strRight1, strRight2);

                }
                else if (column.SortStyle == ColumnSortStyle.IpAddress)
                {
                    nRet = CompareIpAddress(s1, s2);
                }
                else if (column.SortStyle == ColumnSortStyle.RFC1123)
                {
                    nRet = CompareRFC1123(s1, s2);
                }
                else if (column.SortStyle == ColumnSortStyle.Width)
                {
                    nRet = CompareWidth(s1, s2);
                }
                else if (this.EventCompare != null)
                {
                    CompareEventArgs e = new CompareEventArgs();
                    e.Column = column;
                    e.SortColumnIndex = i;
                    e.String1 = s1;
                    e.String2 = s2;
                    this.EventCompare(this, e);
                    nRet = e.Result;
                }
                else
                {
                    // 不能识别的方式，按照左对齐处理
                    nRet = String.Compare(s1, s2);
                }

            END1:
                if (nRet != 0)
                {
                    if (column.Asc == true)
                        return nRet;
                    else
                        return -nRet;
                }
            }

            return 0;
        }
    }

    public delegate void CompareEventHandler(object sender,
        CompareEventArgs e);

    public class CompareEventArgs : EventArgs
    {
        public Column Column = null;    // 排序列
        public int SortColumnIndex = -1;    // 排序列 index。即 Column 在 SortColumns 数组中的下标
        public string String1 = "";
        public string String2 = "";
        public int Result = 0;  // [out]
    }

}
