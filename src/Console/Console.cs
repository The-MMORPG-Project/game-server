using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using Terminal.Gui;

namespace Valk.Networking
{
    public enum LogType
    {
        Info,
        Debug,
        Warning,
        Error
    }

    public class Console
    {
        public static Dictionary<string, Commands> Commands = typeof(Commands).Assembly.GetTypes().Where(x => typeof(Commands).IsAssignableFrom(x) && !x.IsAbstract).Select(Activator.CreateInstance).Cast<Commands>().ToDictionary(x => x.GetType().Name.ToLower(), x => x);
        private static readonly ConcurrentDictionary<LogType, (Terminal.Gui.Attribute Color, string Name)> typeColor = new ConcurrentDictionary<LogType, (Terminal.Gui.Attribute Color, string Name)>();

        public static TextField Input;
        public static ConsoleView View;
        public static List<ConsoleMessage> Messages;
        public static int ViewOffset = 0;

        public static List<string> CommandHistory;
        public static int CommandHistoryIndex = 0;

        public void Start()
        {
            Application.Init();
            Application.OnResized += UpdatePositions;

            CommandHistory = new List<string>();
            Messages = new List<ConsoleMessage>();
            View = new ConsoleView();

            Application.GrabMouse(View); // This way we can scroll even when our mouse is over Label views
            Application.Top.Add(View);

            CreateInputField();

            StartServer(); // Finished setting up console, we can now start the server

            // Populate concurrent type color dictionary
            typeColor[LogType.Info] = (Application.Driver.MakeAttribute(Color.White, Color.Black), "INFO");
            typeColor[LogType.Debug] = (Application.Driver.MakeAttribute(Color.Cyan, Color.Black), "DEBUG");
            typeColor[LogType.Warning] = (Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black), "WARNING");
            typeColor[LogType.Error] = (Application.Driver.MakeAttribute(Color.Red, Color.Black), "ERROR");

            Application.Run();
        }

        public void StartServer()
        {
            Program.Server = new Server(7777, 100);
            new Thread(Program.Server.Start).Start(); // Initialize server on thread 2
        }

        private void CreateInputField()
        {
            Input = new TextField("")
            {
                X = 0,
                Y = ConsoleView.Driver.Clip.Bottom - 1,
                Width = ConsoleView.Driver.Clip.Width
            };

            View.Add(Input);
        }

        public static void Log(LogType type, object obj)
        {
            var time = $"{DateTime.Now:HH:mm:ss}";
            var message = new ConsoleMessage($"{time} [{typeColor[type].Name}] {obj.ToString()}");
            message.TextColor = typeColor[type].Color;

            const int BOTTOM_PADDING = 3;
            if (GetTotalLines() > ConsoleView.Driver.Clip.Bottom - BOTTOM_PADDING)
                ViewOffset -= message.GetLines();

            Messages.Add(message);
            View.Add(message);
            UpdatePositions();
        }

        public static void UpdatePositions()
        {
            // Update message positions
            var index = 0;

            foreach (var message in Messages)
            {
                message.Y = index + ViewOffset;
                index += message.GetLines();
            }

            // Update input text field position
            Input.Y = index + ViewOffset;

            Application.Refresh();
        }

        public static int GetTotalLines()
        {
            var totalLines = 0;
            foreach (var message in Messages)
            {
                totalLines += message.GetLines();
            }
            return totalLines;
        }

        public static void HandleCommands(string input)
        {
            var cmd = input.ToLower().Split(' ')[0];
            var args = input.ToLower().Split(' ').Skip(1).ToArray();

            if (Commands.ContainsKey(cmd))
                Commands[cmd].Run(args);
            else
                Log(LogType.Info, $"Unknown Command: '{cmd}'");
        }
    }
}