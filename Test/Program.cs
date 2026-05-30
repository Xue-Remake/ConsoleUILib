using System;
using System.Threading;
using ConsoleUILib.UILib;

namespace ConsoleUITest
{
    class Program
    {
        // 新的测试用例
        static void Main(string[] args)
        {
            // 设置控制台标题和初始大小
            Console.Title = "ConsoleUILib 60FPS Test";
            Console.Clear();

            // 1. 初始化核心系统 (60帧 = 1000ms / 60 ≈ 16ms)
            int targetFPS = 60;
            int intervalMs = 1000 / targetFPS;
            using var session = new Session(intervalMs);
            using var inputHandler = new InputHandler { PollingIntervalMs = 10 };
            var cmdDispatcher = new CommandDispatcher();

            // 用于保存当前用户的输入内容
            string currentInput = "";
            string lastAction = "Welcome! Type 'help' to see commands.";

            // 2. 创建 UI 控件并绑定数据

            // 顶部边框和标题
            var headerTop = new DoubleDivider();
            var title = new StaticText();
            title.Bind(() => $"   --- ConsoleUILib Test App ({targetFPS} FPS) ---   Tick: {session.TickCount}");
            var headerBottom = new Divider();

            // 跑马灯组件 (因为60帧刷新非常快，跑马灯会滚得很快，刚好验证高帧率)
            var marquee = new Marquee(" || Hello World! ConsoleUILib is running smoothly at 60 Frames Per Second! || ", 50);

            // 动态状态文本
            var statusText = new StaticText();
            statusText.Bind(() => $"[Status] Time: {DateTime.Now:HH:mm:ss.fff} | Last Action: {lastAction}");

            // 列表视图 (用于显示日志或任务)
            var listView = new ListView { Title = "Task / Log List" };
            listView.AddStringItem("1. Initialized UI Components");
            listView.AddStringItem("2. Started 60FPS Render Loop");

            // 输入区边框
            var inputDivider = new DoubleDivider();

            // 命令行输入回显组件 (利用数据绑定实时显示 InputHandler 的内容)
            var inputBox = new StaticText();
            inputBox.Bind(() => inputHandler.Prompt + currentInput + "_");

            // 3. 将控件按顺序加入 Session (这决定了从上到下的渲染顺序)
            session.Add(headerTop);
            session.Add(title);
            session.Add(headerBottom);
            session.Add(marquee);
            session.Add(new Divider());
            session.Add(statusText);
            session.Add(new Divider());
            session.Add(listView);
            session.Add(inputDivider);
            session.Add(inputBox);

            // 4. 配置输入与命令逻辑
            inputHandler.OnInputChanged += text =>
            {
                currentInput = text;
                inputBox.IsDirty = true; // 强制标记脏，虽然内容绑定会自动检测，但确保即时响应
            };

            inputHandler.OnCommandSubmitted += cmd =>
            {
                currentInput = ""; // 提交后清空
                cmdDispatcher.Dispatch(cmd);
            };

            // 注册命令
            cmdDispatcher.Register("help", args =>
            {
                lastAction = "Commands: add <text>, clear, quit";
                return true;
            });

            cmdDispatcher.Register("add", args =>
            {
                if (args.Length > 0)
                {
                    string msg = string.Join(" ", args);
                    listView.AddStringItem($"[*] {msg}");
                    lastAction = $"Added item: {msg}";
                }
                return true;
            });

            cmdDispatcher.Register("clear", args =>
            {
                listView.ClearItems();
                lastAction = "List cleared.";
                return true;
            });

            cmdDispatcher.Register("quit", args =>
            {
                lastAction = "Shutting down...";
                session.Stop();
                return true;
            });

            cmdDispatcher.OnUnknownCommand += cmd =>
            {
                lastAction = $"Unknown command: {cmd}. Type 'help'.";
            };

            // 5. 启动系统
            inputHandler.Start();
            session.Start();

            // 6. 保持主线程存活，直到 session 停止 (用户输入 quit)
            while (session.IsRunning)
            {
                Thread.Sleep(100);
            }

            Console.Clear();
            Console.WriteLine("Application exited gracefully.");
        }
    }
}