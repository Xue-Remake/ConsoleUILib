using System;
using System.Collections.Generic;

namespace ConsoleUILib.UILib
{
    #region 组成部件（保持不变）
    public class Component
    {
        protected string _value;
        public Component(string val) { _value = val ?? ""; }
        public void ChangeVal(string val) => _value = val ?? "";
        public virtual string GetVal() => _value;
    }

    public class ListItem : Component
    {
        public ItemType Type = ItemType.JustString;
        private readonly string[] _ex = new string[2];

        public ListItem(string val) : base(val) { }

        public void ChangeVal(int vIndex, string val)
        {
            switch (vIndex)
            {
                case 1: _value = val ?? ""; break;
                case 2: _ex[0] = val ?? ""; break;
                case 3: _ex[1] = val ?? ""; break;
            }
        }

        public override string GetVal() => Type switch
        {
            ItemType.Double => _value + "     " + _ex[0],
            ItemType.Triple => _value + "     " + _ex[0] + "     " + _ex[1],
            _ => _value
        };
    }
    #endregion

    public abstract class ParaWidgetBase : WidgetBase
    {
        protected readonly List<Component> Shows = new();
        public int ItemCount => Shows.Count;

        protected void AddShow(Component c)
        {
            Shows.Add(c);
            MarkDirty();
        }

        protected void RemoveShow(int i)
        {
            if (i >= 0 && i < Shows.Count)
            {
                Shows.RemoveAt(i);
                MarkDirty();
            }
        }

        public void ClearShows()
        {
            Shows.Clear();
            MarkDirty();
        }

        public override void Print()
        {
            foreach (var c in Shows) Console.WriteLine(c.GetVal());
        }

        public override void Print(ICanvas canvas)
        {
            if (!Visible) return;
            foreach (var c in Shows)
                canvas.WriteLine(c.GetVal());
        }
    }

    public class ListView : ParaWidgetBase
    {
        private readonly ListItem _titleItem = new("Title");

        public string Title
        {
            get => _titleItem.GetVal();
            set
            {
                _titleItem.ChangeVal(1, value ?? "");
                MarkDirty();
            }
        }

        public void AddItem() => AddShow(new ListItem("Item"));

        public void AddItem(ItemType type, string val) => AddShow(new ListItem(val) { Type = type });

        public void Remove(int index) => RemoveShow(index);

        public void Change(int itemIndex, int valueIndex, string val)
        {
            if (itemIndex >= 0 && itemIndex < Shows.Count && Shows[itemIndex] is ListItem li)
            {
                li.ChangeVal(valueIndex, val);
                MarkDirty();
            }
        }

        public void Change(int itemIndex, int valueIndex, ItemType type)
        {
            if (itemIndex >= 0 && itemIndex < Shows.Count && Shows[itemIndex] is ListItem li)
            {
                li.Type = type;
                MarkDirty();
            }
        }

        public void Print(bool withTitle)
        {
            if (withTitle)
            {
                Console.WriteLine(_titleItem.GetVal());
                Console.WriteLine("----------------------------------------");
            }
            base.Print();
        }

        public override void Print() => Print(true);

        public override void Print(ICanvas canvas)
        {
            if (!Visible) return;
            canvas.WriteLine(_titleItem.GetVal());
            canvas.WriteLine("----------------------------------------");
            base.Print(canvas);
        }
    }
}