using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace FileManager
{
    internal class Program
    {
        static int WindowHeight { get; set; }
        static int WindowWidth { get; set; }
        static int TreeHeight { get; set; } = 0;
        static int InfoHeight { get; set; } = 0;
        private static string currentDir;
        private const int MF_BYCOMMAND = 0x00000000;
        public const int SC_MINIMIZE = 0xF020;
        public const int SC_MAXIMIZE = 0xF030;
        public const int SC_SIZE = 0xF000;
        static List<string> SavedCommands { get; set; } = new List<string>();
        static int CommandIndex { get; set; } = -1;

        [DllImport("user32.dll")]
        public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        static void Main(string[] args)
        {
            LockConsoleResizing();

            SetWindowParameters();

            Console.Title = "FileManager";

            Console.SetWindowSize(WindowWidth, WindowHeight);
            Console.SetBufferSize(WindowWidth, WindowHeight);

            TreeHeight = GetPartialHeight(WindowHeight, 0.74);
            InfoHeight = GetPartialHeight(WindowHeight, 0.16);

            DrawWindow(0, 0, WindowWidth, TreeHeight);
            DrawWindow(0, TreeHeight, WindowWidth, InfoHeight);

            LoadSettings();

            UpdateConsole(TreeHeight + InfoHeight);

            Console.ReadKey(true);
        }

        /// <summary>
        /// Читает значения размера окна из файла параметров, если они в допустимых пределах, задаёт их. Иначе, задаёт стандартное значение.
        /// </summary>
        private static void SetWindowParameters()
        {
            if (Properties.Settings.Default.WINDOW_HEIGHT >= 30 && Properties.Settings.Default.WINDOW_HEIGHT <= 70)
                WindowHeight = Properties.Settings.Default.WINDOW_HEIGHT;
            else
                WindowHeight = 60;

            if (Properties.Settings.Default.WINDOW_WIDTH >= 60 && Properties.Settings.Default.WINDOW_WIDTH <= 160)
                WindowWidth = Properties.Settings.Default.WINDOW_WIDTH;
            else
                WindowWidth = 150;
        }

        static void LoadSettings()
        {
            if (Properties.Settings.Default.CurrentDir != "")
                currentDir = Properties.Settings.Default.CurrentDir;
            else
                currentDir = Directory.GetCurrentDirectory();

            if (Properties.Settings.Default.OpenedTree != "")
                DrawTree(new DirectoryInfo(Properties.Settings.Default.OpenedTree), Convert.ToInt32(Properties.Settings.Default.OpenedPage));
        }

        /// <summary>
        /// Блокирует изменение размера консоли.
        /// </summary>
        static void LockConsoleResizing()
        {
            IntPtr handle = GetConsoleWindow();
            IntPtr sysMenu = GetSystemMenu(handle, false);

            if (handle != IntPtr.Zero)
            {
                DeleteMenu(sysMenu, SC_MINIMIZE, MF_BYCOMMAND);
                DeleteMenu(sysMenu, SC_MAXIMIZE, MF_BYCOMMAND);
                DeleteMenu(sysMenu, SC_SIZE, MF_BYCOMMAND);
            }
        }
        static int GetPartialHeight(int sourceHeight, double part)
        {
            int treeHeight = 0;
            return treeHeight = Convert.ToInt32(Math.Round(sourceHeight * part));
        }

        /// <summary>
        /// Обновление консоли
        /// </summary>
        static void UpdateConsole(int startingPosition)
        {
            DrawConsole(currentDir, 0, startingPosition, WindowWidth, 3);
            ProcessEnterCommand(WindowWidth);
        }

        /// <summary>
        /// Показывает информационное сообщение. Если тип сообщения "ошибка", то записывает ошибку в файл errors.txt в корневой папке приложения.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageType"></param>
        static void ShowMessage(string message, MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.Info:
                    DrawWindow(0, TreeHeight, WindowWidth, InfoHeight);
                    Console.SetCursorPosition(2, TreeHeight + 1);
                    Console.WriteLine(message);
                    break;

                case MessageType.Error:
                    DrawWindow(0, TreeHeight, WindowWidth, InfoHeight);
                    Console.SetCursorPosition(2, TreeHeight + 1);
                    Console.WriteLine($"Ошибка. {message}");
                    string currentDateTime = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString();
                    string path = Environment.CurrentDirectory;

                    if (!Directory.Exists($"{path}\\errors\\"))
                        Directory.CreateDirectory($"{path}\\errors\\");

                    File.AppendAllText($"{path}\\errors\\errors.txt", $"{currentDateTime} Ошибка. {message} \n");
                    break;

                default:
                    break;
            }
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
            ConsoleKey key;

            do
            {
                key = Console.ReadKey().Key;

                if (key != ConsoleKey.Enter && key != ConsoleKey.Backspace && key != ConsoleKey.UpArrow && key != ConsoleKey.DownArrow && key != ConsoleKey.LeftArrow && key != ConsoleKey.RightArrow)
                {
                    command.Append((char)key);
                    CommandIndex = -1;
                }

                (int currentLeft, int currentTop) = GetCursorPosition();

                if (currentLeft == width - 2)
                {
                    Console.SetCursorPosition(currentLeft - 1, top);
                    Console.Write(" ");
                    Console.SetCursorPosition(currentLeft - 1, top);
                }

                if (key == ConsoleKey.Backspace)
                {
                    if (command.Length > 0)
                    {
                        command.Remove(command.Length - 1, 1);
                    }
                    if (currentLeft >= left)
                    {
                        Console.SetCursorPosition(currentLeft, top);
                        Console.Write(" ");
                        Console.SetCursorPosition(currentLeft, top);
                    }
                    else
                    {
                        Console.SetCursorPosition(left, top);
                    }
                    CommandIndex = -1;
                }                                
                
                
                if (key == ConsoleKey.DownArrow)
                {
                    Console.SetCursorPosition(currentLeft - 1, top);

                    if (CommandIndex < -1)
                    {
                        CommandIndex = -1;
                    }

                    CommandIndex++;

                    if (SavedCommands.Count > 0 && CommandIndex < SavedCommands.Count)
                    {
                        DrawConsole(currentDir, 0, TreeHeight + InfoHeight, WindowWidth, 3);
                        command.Clear();

                        command = command.Append(SavedCommands[CommandIndex]);

                        Console.SetCursorPosition(left, top);
                        Console.Write(command);
                    }
                    else
                    {
                        CommandIndex = 0;

                        DrawConsole(currentDir, 0, TreeHeight + InfoHeight, WindowWidth, 3);
                        command.Clear();

                        command = command.Append(SavedCommands[CommandIndex]);

                        Console.SetCursorPosition(left, top);
                        Console.Write(command);                        
                    }
                }

                if (key == ConsoleKey.UpArrow)
                {
                    Console.SetCursorPosition(currentLeft - 1, top);

                    if (CommandIndex == -1)
                    {
                        CommandIndex = SavedCommands.Count;
                    }

                    CommandIndex--;

                    if (SavedCommands.Count > 0 && CommandIndex < SavedCommands.Count && CommandIndex >= 0)
                    {
                        DrawConsole(currentDir, 0, TreeHeight + InfoHeight, WindowWidth, 3);
                        command.Clear();

                        command = command.Append(SavedCommands[CommandIndex]);

                        Console.SetCursorPosition(left, top);
                        Console.Write(command);
                    }
                    else
                    {
                        CommandIndex = SavedCommands.Count - 1;

                        DrawConsole(currentDir, 0, TreeHeight + InfoHeight, WindowWidth, 3);
                        command.Clear();

                        command = command.Append(SavedCommands[CommandIndex]);

                        Console.SetCursorPosition(left, top);
                        Console.Write(command);
                    }
                }

                if (key == ConsoleKey.LeftArrow)
                {
                    Console.SetCursorPosition(currentLeft - 1, top);
                }

                if (key == ConsoleKey.RightArrow)
                {
                    Console.SetCursorPosition(currentLeft - 1, top);
                }
            }
            while (key != ConsoleKey.Enter);

            ParseCommandString(command.ToString());
        }
        static void ParseCommandString(string command)
        {
            SavedCommands.Add(command);

            string[] commandParams = command.ToLower().Split(' ');

            if (commandParams.Length > 0)
            {
                try
                {
                    switch (commandParams[0])
                    {
                        case "cd":
                            if (commandParams.Length > 1 && Directory.Exists(commandParams[1]))
                            {
                                currentDir = commandParams[1];
                                Properties.Settings.Default.CurrentDir = commandParams[1];
                                Properties.Settings.Default.Save();
                            }
                            break;

                        case "ls":
                            if (commandParams.Length > 1 && Directory.Exists(commandParams[1]))
                            {
                                if (commandParams.Length > 3 && commandParams[2] == "-p" && int.TryParse(commandParams[3], out int page))
                                {
                                    DrawTree(new DirectoryInfo(commandParams[1]), page);
                                    Properties.Settings.Default.OpenedPage = page;
                                    Properties.Settings.Default.OpenedTree = commandParams[1];
                                    Properties.Settings.Default.Save();
                                }
                                else
                                {
                                    DrawTree(new DirectoryInfo(commandParams[1]), 1);
                                    Properties.Settings.Default.OpenedPage = 1;
                                    Properties.Settings.Default.OpenedTree = commandParams[1];
                                    Properties.Settings.Default.Save();
                                }
                            }
                            else
                            {
                                DrawTree(new DirectoryInfo(currentDir), 1);
                                Properties.Settings.Default.OpenedPage = 1;
                                Properties.Settings.Default.OpenedTree = currentDir;
                                Properties.Settings.Default.Save();
                            }
                            break;

                        case "cp":
                            if (commandParams.Length > 1 && File.Exists(commandParams[1]))
                            {
                                CopyFile(commandParams, out string message);
                                ShowMessage(message, MessageType.Info);
                            }
                            else if (commandParams.Length > 1 && Directory.Exists(commandParams[1]))
                            {
                                CopyDirectory(commandParams[1], commandParams[2]);
                            }
                            else
                            {
                                ShowMessage("Указанный файл или директория не найдены. Проверьте корректность ввода.", MessageType.Info);
                            }
                            break;

                        case "rm":
                            if (commandParams.Length > 1 && File.Exists(commandParams[1]))
                            {
                                File.Delete(commandParams[1]);
                                ShowMessage($"Файл {commandParams[1]} удалён успешно.", MessageType.Info);
                            }
                            else if (commandParams.Length > 1 && Directory.Exists(commandParams[1]))
                            {
                                DeleteDirectory(commandParams[1]);
                                ShowMessage($"Каталог {commandParams[1]} удалён успешно.", MessageType.Info);
                            }
                            else
                            {
                                ShowMessage("Указанный файл или директория не найдены. Проверьте корректность ввода.", MessageType.Info);
                            }
                            break;

                        case "info":
                            if (commandParams.Length > 1 && File.Exists(commandParams[1]))
                            {
                                ShowInfo(commandParams[1], InfoType.File);
                            }
                            else if (commandParams.Length > 1 && Directory.Exists(commandParams[1]))
                            {
                                ShowInfo(commandParams[1], InfoType.Directory);
                            }
                            else
                            {
                                ShowInfo(currentDir, InfoType.Directory);
                            }
                            break;

                        default:
                            ShowMessage("Неизвестная команда.", MessageType.Info);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    ShowMessage(ex.Message, MessageType.Error);
                }                
            }

            UpdateConsole(TreeHeight + InfoHeight);
        }

        /// <summary>
        /// Выводит информацию о каталоке или файле.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="infoType"></param>
        static void ShowInfo(string path, InfoType infoType)
        {            
            switch (infoType)
            {
                case InfoType.Directory:
                    DirectoryInfo directoryInfo = new DirectoryInfo(path);
                    string dirLocation = "Расположение: " + directoryInfo.FullName;
                    string filesCount = "Файлов: " + directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).Count().ToString();
                    string dirCount = "Папок: " + directoryInfo.EnumerateDirectories("*", SearchOption.AllDirectories).Count().ToString();
                    string dirSize = "Размер папки: " + directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories).Sum(x => x.Length / 1024).ToString() + "KB";
                    string dirCreated = "Дата создания: " + directoryInfo.CreationTime.ToShortDateString() + " " + directoryInfo.CreationTime.ToLongTimeString();
                    string dirUpdated = "Дата измененения: " + directoryInfo.LastWriteTime.ToShortDateString() + " " + directoryInfo.LastWriteTime.ToLongTimeString();

                    string[] dirAttributes = { dirSize, dirLocation, filesCount, dirCount, dirCreated, dirUpdated };

                    ShowAttributes(dirAttributes);

                    break;

                case InfoType.File:
                    FileInfo fileInfo = new FileInfo(path);
                    string fileExtension = "Расширение: " + fileInfo.Extension;
                    string fileLocation = "Расположение:" + fileInfo.DirectoryName;
                    string fileSize = "Размер файла: " + Convert.ToInt32(fileInfo.Length / 1024) + "KB";
                    string fileCreated = "Дата создания: " + fileInfo.CreationTime.ToShortDateString() + " " + fileInfo.CreationTime.ToLongTimeString();
                    string fileUpdated = "Дата измененения: " + fileInfo.LastWriteTime.ToShortDateString() + " " + fileInfo.LastWriteTime.ToLongTimeString();

                    string[] fileAttributes = { fileExtension, fileLocation, fileCreated, fileUpdated, fileSize };

                    ShowAttributes(fileAttributes);

                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Печатает в информацию аттрибуты, переданные в массиве строк в зависимости от размеров окна "Информации".
        /// </summary>
        /// <param name="attributes"></param>
        private static void ShowAttributes(string[] attributes)
        {
            DrawWindow(0, TreeHeight, WindowWidth, InfoHeight);
            int infoLines = InfoHeight - 2;
            double attributesPerLine = Math.Ceiling(Convert.ToDouble(attributes.Length) / Convert.ToDouble(infoLines));
            int attributeIndex = 0;

            for (int i = 1; i <= infoLines; i++)
            {
                Console.SetCursorPosition(2, TreeHeight + i);

                for (attributeIndex = attributeIndex; attributeIndex < attributesPerLine * i; attributeIndex++)
                {
                    if (attributeIndex < attributes.Length)
                    {
                        Console.Write($"{attributes[attributeIndex]}\t");
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Удаляет директорию и всё её содержимое.
        /// </summary>
        /// <param name="directory"></param>
        static void DeleteDirectory(string directory)
        {
            DirectoryInfo rootDir = new DirectoryInfo(directory);
            DirectoryInfo[] subDirs = rootDir.GetDirectories();
            FileInfo[] files = rootDir.GetFiles();

            foreach (FileInfo file in files)
            {
                file.Delete();
            }

            foreach (DirectoryInfo dir in subDirs)
            {
                DeleteDirectory(dir.FullName);
            }

            rootDir.Delete();
        }

        /// <summary>
        /// Проверяет наличие файлов в директориях, копирует при возможности, либо выводит сообщение если неуспешно.
        /// </summary>
        /// <param name="commandParams"></param>
        static void CopyFile(string[] commandParams, out string message)
        {
            string endDirectory = commandParams[2].Substring(0, commandParams[2].LastIndexOf('\\'));

            if (!File.Exists(commandParams[1]))
            {
                message = "Исходный файл не найден.";
            }
            else if (commandParams.Length == 3 && !File.Exists(commandParams[2])) //Если указано 3 параметра, конечный файл не существует, копируем файл.
            {
                if (!Directory.Exists(endDirectory))
                    Directory.CreateDirectory(endDirectory);


                File.Copy(commandParams[1], commandParams[2]);

                message = "Копирование успешно.";
            }
            else if (commandParams.Length == 4 && commandParams[3] == "-rw") //Если указано 4 параметра и последний параметр равен "-rw", то копируем файл с перезаписью.
            {
                if (!Directory.Exists(endDirectory))
                    Directory.CreateDirectory(endDirectory);

                File.Copy(commandParams[1], commandParams[2], true);

                message = "Копирование успешно.";
            }
            else if ((commandParams.Length == 3 && File.Exists(commandParams[2])) || (commandParams.Length == 4 && File.Exists(commandParams[2]) && commandParams[3] != "-rw")) //Если указано 4 параметра, конечный файл существует, а последний параметр не "-rw", показываем сообщение.
            {
                message = "Указанный файл в конечной директории уже существует. Для перезаписи файла, укажите параметр \"-rw\".";
            }
            else
            {
                message = "Некорректное количество параметров.";
            }
        }

        /// <summary>
        /// Копирует директорию с содержимым
        /// </summary>
        /// <param name="sourceDirectory">Исходная директория</param>
        /// <param name="destinationDirectory">Итоговая директория</param>
        static void CopyDirectory(string sourceDirectory, string destinationDirectory)
        {
            DirectoryInfo rootDir = new DirectoryInfo(sourceDirectory);
            DirectoryInfo[] subDirs = rootDir.GetDirectories();
            FileInfo[] files = rootDir.GetFiles();

            if (!Directory.Exists(destinationDirectory))
                Directory.CreateDirectory(destinationDirectory);

            foreach (FileInfo file in files)
            {
                file.CopyTo($"{destinationDirectory}\\{file.Name}");
            }

            foreach (DirectoryInfo subDirectory in subDirs)
            {
                CopyDirectory(subDirectory.FullName, $"{destinationDirectory}\\{subDirectory.Name}");
            }
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

            DrawWindow(0, 0, WindowWidth, TreeHeight);

            (int currentLeft, int currentTop) = GetCursorPosition();

            int pageLines = TreeHeight - 2;
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
            Console.SetCursorPosition(WindowWidth / 2 - footer.Length / 2, TreeHeight - 1);
            Console.WriteLine(footer);
        }

        static void GetTree(StringBuilder tree, DirectoryInfo dir, string indent, bool lastDirectory)
        {
            try
            {
                tree.Append(indent);

                if (lastDirectory)
                {
                    tree.Append("└─");
                    indent += "  ";
                }
                else
                {
                    tree.Append("├─");
                    indent += "│ ";
                }

                tree.Append($"{dir.Name}\n");

                FileInfo[] subFiles = dir.GetFiles("*", SearchOption.TopDirectoryOnly);

                for (int i = 0; i < subFiles.Length; i++)
                {
                    if (i == subFiles.Length - 1)
                    {
                        tree.Append($"{indent}└─{subFiles[i].Name}\n");
                    }
                    else
                    {
                        tree.Append($"{indent}├─{subFiles[i].Name}\n");
                    }
                }

                DirectoryInfo[] subDirects = dir.GetDirectories();

                for (int i = 0; i < subDirects.Length; i++)
                {
                    GetTree(tree, subDirects[i], indent, i == subDirects.Length - 1);
                }
            }
            catch (Exception)
            {

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
    enum MessageType
    {
        Info,
        Error, 
    }
    enum InfoType
    {
        Directory,
        File
    }
}
