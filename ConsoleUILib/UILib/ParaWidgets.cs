using System;
using System.Collections.Generic;

namespace ConsoleUILib.UILib
{
    public enum ItemType { JustString, Double, Triple }

    /// <summary>
    /// 列表项组件，可管理三个字符串字段，支持绑定。
    /// </summary>
    public class ListItem : DataComponent
    {
        public ItemType Type { get; set; } = ItemType.JustString;
        private readonly string[] _fields = new string[2];

        public ListItem(string initialValue = "") : base(() => initialValue) { }

        public void SetField(int index, string value)
        {
            value = value ?? "";
            switch (index)
            {
                case 1: _value = value; break;
                case 2: _fields[0] = value; break;
                case 3: _fields[1] = value; break;
            }
            MarkChange(); // 通知控件数据已变
        }

        private void MarkChange() => base.CheckAndUpdate(); // 触发重新获取值

        public override object GetValue()
        {
            return Type switch
            {
                ItemType.Double => $"{_value}     {_fields[0]}",
                ItemType.Triple => $"{_value}     {_fields[0]}     {_fields[1]}",
                _ => _value
            };
        }

        // 重写 CheckAndUpdate，这里由外部调用 SetField 时直接标记变化
        public override bool CheckAndUpdate() => false; // 不由定时检查驱动，由 SetField 触发
    }

    public abstract class ParaWidgetBase : WidgetBase
    {
        protected readonly List<ListItem> Items = new List<ListItem>();

        public int ItemCount => Items.Count;

        public void AddItem(ListItem item)
        {
            Items.Add(item);
            IsDirty = true;
        }

        public void RemoveItem(int index)
        {
            if (index >= 0 && index < Items.Count)
            {
                Items.RemoveAt(index);
                IsDirty = true;
            }
        }

        public void ClearItems()
        {
            Items.Clear();
            IsDirty = true;
        }

        protected override IEnumerable<IBindableComponent> GetComponents()
        {
            foreach (var item in Items)
                yield return item;
        }

        public override void Bind(Func<object> getter)
        {
            // 列表控件不支持直接 Bind，留给具体实现
            throw new NotSupportedException("ParaWidgetBase does not support global Bind; use item-level binding.");
        }
    }

    public class ListView : ParaWidgetBase
    {
        private readonly ListItem _titleItem = new ListItem("Title");

        public string Title
        {
            get => _titleItem.GetValue().ToString();
            set => _titleItem.SetField(1, value);
        }

        public void AddStringItem(string value)
        {
            AddItem(new ListItem(value) { Type = ItemType.JustString });
        }

        public void ChangeItem(int itemIndex, int fieldIndex, string value)
        {
            if (itemIndex >= 0 && itemIndex < Items.Count)
                Items[itemIndex].SetField(fieldIndex, value);
        }

        public override void Print(ICanvas canvas)
        {
            if (!Visible) return;
            canvas.WriteLine(_titleItem.GetValue().ToString());
            canvas.WriteLine("----------------------------------------");
            foreach (var item in Items)
                canvas.WriteLine(item.GetValue().ToString());
        }

        protected override IEnumerable<IBindableComponent> GetComponents()
        {
            yield return _titleItem;
            foreach (var item in Items)
                yield return item;
        }
    }
}