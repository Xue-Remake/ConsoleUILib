using System;

namespace ConsoleUILib.UILib
{
    public abstract class WidgetBase
    {
        public string Name { get; set; }
        public bool Visible { get; set; } = true;

        // 初次绘制入口（兼容旧代码）
        public abstract void Print();

        // 渲染到指定画布（新接口）
        public virtual void Print(ICanvas canvas)
        {
            if (!Visible) return;
            // 默认实现直接输出到控制台的 ConsoleCanvas（向后兼容）
            Print();
        }

        // 时间驱动或外部触发的更新
        public virtual void Update() { }

        // 脏标记事件，由 Session 订阅
        public event Action<WidgetBase> OnDirty;

        /// <summary>
        /// 子类在需要重绘时调用此方法。
        /// </summary>
        protected void MarkDirty() => OnDirty?.Invoke(this);
    }

    public interface ITimeOperator
    {
        int GetTick();
        TimeSpan Elapsed { get; }
    }

    public enum ItemType
    {
        JustString,
        Double,
        Triple
    }

    public interface ISessionAware
    {
        void OnAttached(Session session);
        void OnDetached(Session session);
    }
}