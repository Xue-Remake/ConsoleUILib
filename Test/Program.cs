using ConsoleUILib.UILib;

class Program
{
    static void Main()
    {
        var session = Session.Default;
        session.UpdateIntervalMs = 200;

        // 静态文本
        var title = new CustomStaticWidget("实时行情");
        session.AddWidget(title);
        session.AddWidget(new Divider());

        // 列表视图
        var list = new ListView();
        list.Title = "股票列表";
        list.AddItem(ItemType.Double, "AAPL");
        list.AddItem(ItemType.Double, "MSFT");
        session.AddWidget(list);

        // 跑马灯
        var marquee = new Marquee(">> 欢迎使用控制台UI库 v2.0 <<", 40, session);
        session.AddWidget(marquee);

        // 启动会话
        session.Start();

        // 模拟外部事件：5秒后修改数据，仅部分区域刷新
        Task.Delay(5000).ContinueWith(_ =>
        {
            title.Change("行情已更新");
            list.Change(0, 2, "150.23");   // 修改苹果股价
            list.AddItem(ItemType.Double, "GOOG");
        });

        Console.ReadKey();
        session.Stop();
    }
}