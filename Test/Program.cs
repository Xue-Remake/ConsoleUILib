using System;
using System.Threading;
using System.Threading.Tasks;
using ConsoleUILib.UILib;

class Program
{
    static double[] pos = new double[2] { 0.0, 0.0 };
    static double speed = 5.0;
    static double direction = 45.0;

    static async Task Main(string[] args)
    {
        // ---- 在启动 Session 之前完成所有 Console 输出 ----
        Console.WriteLine("坐标监控程序启动中...");
        Console.WriteLine("可用命令: speed <值> | dir <值> | exit");
        Console.WriteLine();

        // ---- 初始化 Session ----
        var session = Session.Default;
        session.UpdateIntervalMs = 100;

        // ---- 监控控件 ----
        var monitor = new VarMonitor("坐标监控");
        monitor.Bind("X", () => pos[0], "F2");
        monitor.Bind("Y", () => pos[1], "F2");
        monitor.Bind("Speed", () => speed, "F3");
        monitor.Bind("Direction", () => direction + "°", null);

        // ---- 状态栏（用 StaticWidgetBase 承载，通过 Session 渲染） ----
        var statusLine = new CustomStaticWidget("模拟已启动。输入命令调整参数。按 Ctrl+C 退出。");
        // 留一个空行分隔
        var spacer = new CustomStaticWidget(" ");

        session.AddWidget(monitor);
        session.AddWidget(spacer);
        session.AddWidget(statusLine);

        // ---- 输入处理 ----
        var input = new InputHandler { Prompt = "> " };
        var dispatcher = new CommandDispatcher();
        dispatcher.Register("speed", parts =>
        {
            if (parts.Length > 0 && double.TryParse(parts[0], out double s))
            {
                speed = s;
                statusLine.Value = $"Speed 已设为 {s}";  // 通过 Widget 更新，触发正确重绘
            }
            return true;
        });
        dispatcher.Register("dir", parts =>
        {
            if (parts.Length > 0 && double.TryParse(parts[0], out double d))
            {
                direction = d % 360;
                statusLine.Value = $"Direction 已设为 {direction}°";
            }
            return true;
        });
        dispatcher.Register("exit", _ =>
        {
            statusLine.Value = "正在退出...";
            return true;
        });
        input.OnCommandSubmitted += cmd =>
        {
            if (cmd == "exit")
            {
                // 通过修改 status 示意退出（实际退出逻辑见下方 cts）
            }
            dispatcher.Dispatch(cmd);
        };
        input.Start();

        // ---- 启动渲染 ----
        session.Start();

        // ---- 高速模拟循环（120 FPS） ----
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        double fps = 120.0;
        double dt = 1.0 / fps;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        double simTime = 0;
        double radPerFrame;

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                radPerFrame = direction * Math.PI / 180.0;
                pos[0] += speed * Math.Cos(radPerFrame) * dt;
                pos[1] += speed * Math.Sin(radPerFrame) * dt;

                simTime += dt;
                var targetMs = simTime * 1000;
                var sleepMs = (int)(targetMs - sw.Elapsed.TotalMilliseconds);
                if (sleepMs > 0)
                    await Task.Delay(Math.Min(sleepMs, 8));
            }
        }
        catch (OperationCanceledException) { }

        // ---- 收尾 ----
        input.Stop();
        session.Stop();

        // Session 停止后可以安全写 Console
        Console.WriteLine("程序已退出。");
    }
}
