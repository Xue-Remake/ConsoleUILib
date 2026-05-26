using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleUILib.UILib
{
    /// <summary>
    /// 控件注册表：按索引追踪控件集合，方便按序号增删查。
    /// 用于构建命令行式动态控件管理。
    /// </summary>
    public class WidgetRegistry
    {
        private readonly List<WidgetBase> _widgets = new();
        public int Count => _widgets.Count;
        public void Register(WidgetBase widget) => _widgets.Add(widget);
        public WidgetBase GetAt(int index)
            => (index >= 0 && index < _widgets.Count) ? _widgets[index] : null;
        public bool RemoveAt(int index)
        {
            if (index >= 0 && index < _widgets.Count)
            { _widgets.RemoveAt(index); return true; }
            return false;
        }
        /// <summary>按 ListView 标题查找</summary>
        public ListView GetListViewByTitle(string title)
        {
            foreach (var w in _widgets)
                if (w is ListView lv && lv.Title == title) return lv;
            return null;
        }
        public bool HasListViewWithTitle(string title) => GetListViewByTitle(title) != null;
        public void Clear() => _widgets.Clear();
        public IReadOnlyList<WidgetBase> GetAll() => _widgets.AsReadOnly();
    }
}
