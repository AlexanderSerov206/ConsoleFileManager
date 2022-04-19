using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager
{
    internal class Program
    {
        const int WINDOW_HEIGHT = 30;
        const int WINDOW_WIDTH = 120;
        private static string currentDir = Directory.GetCurrentDirectory();

        static void Main(string[] args)
        {
            Console.Title = "FileManager";

            Console.SetWindowSize(WINDOW_WIDTH, WINDOW_HEIGHT);
            Console.SetBufferSize(WINDOW_WIDTH, WINDOW_HEIGHT);

            DrawWindow(0, 0, WINDOW_WIDTH, 18);
            DrawWindow(0, 18, WINDOW_WIDTH, 8);

            UpdateConsole();

            Console.ReadKey(true);
        }

        /// <summary>
        /// Обновление консоли
        /// </summary>
        static void UpdateConsole()
        {
            DrawConsole(currentDir, 0, 26, WINDOW_WIDTH, 3);
            ProcessEnterCommand(WINDOW_WIDTH);
        }

        /// <summary>
        /// Возврат координат курсора
        /// </summary>
        /// <returns></returns>
        static (int left, int top) GetCursorPosition()
        {
            return (Console.CursorLeft, Console.CursorTop);
        }


        static void ProcessEnterCommand(int width)
        {
            (int left, int top) = GetCursorPosition();
            StringBuilder command = new StringBuilder();
            char key;

            do
            {
                key = Console.ReadKey().KeyChar;

                if (key != 8 && key != 13)
                {
                    command.Append(key);
                }

                (int currentleft, int currenttop) = GetCursorPosition();

                if (currentleft == width - 2)
                {
                    Console.SetCursorPosition(currentleft - 1, top);
                    Console.Write(" ");
                    Console.SetCursorPosition(currentleft - 1, top);
                }

                if (key == (char)8)
                {
                    if (command.Length > 0)
                    {
                        command.Remove(command.Length - 1, 1);
                    }
                    if (currentleft >= left)
                    {
                        Console.SetCursorPosition(currentleft, top);
                        Console.Write(" ");
                        Console.SetCursorPosition(currentleft, top);
                    }
                    else
                    {
                        Console.SetCursorPosition(left, top);
                    }
                }
            }
            while (key!= (char)13);

            ParseCommandString(command.ToString());
        }

        static void ParseCommandString(string command)
        {
            string[] commandParams = command.ToLower().Split(' ');

            if (commandParams.Length > 0)
            {
                switch (commandParams[0])
                {
                    case "cd":
                        if (commandParams.Length > 1 && Directory.Exists(commandParams[1]))
                        {
                            currentDir = commandParams[1];
                        }

                        break;

                    case "ls":
                        if (commandParams.Length > 1 && Directory.Exists(commandParams[1]))
                        {
                            if (commandParams.Length > 3 && commandParams[2] == "-p" && int.TryParse(commandParams[3], out int page))
                            {
                                DrawTree(new DirectoryInfo(commandParams[1]), page);
                            }
                            else
                            {
                                DrawTree(new DirectoryInfo(commandParams[1]), 1);
                            }
                        }

                        break;

                    default:
                        break;
                }
            }

            UpdateConsole();
        }

        /// <summary>
        /// Отрисовать дерево каталогов
        /// </summary>
        /// <param name="dir">Директория</param>
        /// <param name="page">Страница</param>
        static void DrawTree(DirectoryInfo dir, int page)
        {
            StringBuilder tree = new StringBuilder();
            GetTree(tree, dir, "", true);

            DrawWindow(0, 0, WINDOW_WIDTH, 18);

            (int currentLeft, int currentTop) = GetCursorPosition();

            int pageLines = 16;
            string[] lines = tree.ToString().Split(new char[] { '\n' });
            int pageTotal = (lines.Length + pageLines - 1) / pageLines;

            if (page > pageTotal)
                page = pageTotal;

            for (int i = (page - 1) * pageLines, counter = 0; i < page * pageLines; i++, counter++)
            {
                if (lines.Length - 1 > i)
                {
                    Console.SetCursorPosition(currentLeft + 1, currentTop + 1 + counter);
                    Console.WriteLine(lines[i]);
                }
            }


            //footer

            string footer = $"╡ {page} of {pageTotal} ╞";
            Console.SetCursorPosition(WINDOW_WIDTH / 2 - footer.Length / 2, 17);
            Console.WriteLine(footer);
        }

        static void GetTree(StringBuilder tree, DirectoryInfo dir, string indent, bool lastDirectory)
        {
            tree.Append(indent);

            if (lastDirectory)
            {
                tree.Append("└");
                indent += "  ";
            }
            else
            {
                tree.Append("├");
                indent += "│ ";
            }

            tree.Append($"{dir.Name}\n");

            FileInfo[] subFiles = dir.GetFiles();

            for (int i = 0; i < subFiles.Length; i++)
            {
                if (i == subFiles.Length - 1)
                {
                    tree.Append($"{indent}└{subFiles[i].Name}\n");
                }
                else
                {
                    tree.Append($"{indent}├{subFiles[i].Name}\n");
                }
            }

            DirectoryInfo[] subDirects = dir.GetDirectories();

            for (int i = 0; i < subDirects.Length; i++)
            {
                GetTree(tree, subDirects[i], indent, i == subDirects.Length - 1);
            }
        }

        /// <summary>
        /// Отрисовать консоль для ввода
        /// </summary>
        /// <param name="dir">Текущая директория</param>
        /// <param name="x">Начальная позиция по оси X</param>
        /// <param name="y">Начальная позиция по оси Y</param>
        /// <param name="width">Ширина</param>
        /// <param name="height">Высота</param>
        static void DrawConsole(string dir, int x, int y, int width, int height)
        {
            DrawWindow(x, y, width, height);
            Console.SetCursorPosition(x + 1, y + height / 2);
            Console.Write($"{dir}>");
        }

        /// <summary>
        /// Отрисовать окно
        /// </summary>
        /// <param name="x">Начальная позиция по оси X</param>
        /// <param name="y">Начальная позиция по оси Y</param>
        /// <param name="width">Ширина</param>
        /// <param name="height">Высота</param>
        static void DrawWindow(int x, int y, int width, int height)
        {
            Console.SetCursorPosition(x, y);

            //header 

            Console.Write("╔");
            for (int i = 0; i < width - 2; i++)            
                Console.Write("═");
            Console.Write("╗");

            //body
            Console.SetCursorPosition(x, y + 1);

            for (int i = 0; i < height - 2; i++)
            {
                Console.Write("║");
                for (int j = x + 1; j < x + width - 1; j++)
                {
                    Console.Write(" ");
                }
                Console.Write("║");
            }

            //footer
            Console.Write("╚");
            for (int i = 0; i < width - 2; i++)
                Console.Write("═");
            Console.Write("╝");

            Console.SetCursorPosition(x, y);
        }
    }
}
