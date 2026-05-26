using System;

namespace ConsoleUILib.UILib
{
    public class StaticWidgetBase : WidgetBase
    {
        protected string _value;

        public string Value
        {
            get => _value;
            set
            {
                if (_value != value)
                {
                    _value = value ?? "";
                    MarkDirty();
                }
            }
        }

        public StaticWidgetBase(string val = "")
        {
            _value = val ?? "";
        }

        public override void Print()
            => Console.WriteLine(_value);

        public override void Print(ICanvas canvas)
        {
            if (!Visible) return;
            canvas.WriteLine(_value);
        }
    }

    public class Divider : StaticWidgetBase
    {
        public Divider(string val = "----------------------------------------") : base(val) { }
    }

    public class DoubleDivider : StaticWidgetBase
    {
        public DoubleDivider(string val = "========================================") : base(val) { }
    }

    public class CustomStaticWidget : StaticWidgetBase
    {
        public CustomStaticWidget(string val = "") : base(val) { }

        // 保持 Change 方法，但内部调用属性即可触发 MarkDirty
        public void Change(string val) => Value = val;
    }
}