using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using System.IO;
using System.Runtime.CompilerServices;

namespace cli_life
{
    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> neighbors = new List<Cell>();
        public bool IsAliveNext;
        public void DetermineNextLiveState()
        {
            int liveNeighbors = neighbors.Where(x => x.IsAlive).Count();
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }
        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }
    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;
        public int Generation;

        public int Columns => Cells.GetLength(0);
        public int Rows => Cells.GetLength(1);
        public int Width => Columns * CellSize;
        public int Height => Rows * CellSize;
        readonly Random rand = new Random();

        public Board(int width, int height, int cellSize, double liveDensity = .1)
        {
            Generation = 0;
            CellSize = cellSize;
            Cells = new Cell[width / cellSize, height / cellSize];
            InitializeCells();
            ConnectNeighbors();
            Randomize(liveDensity);
        }

        public Board(int width, int height, int cellSize)
        {
            Generation = 0;
            CellSize = cellSize;
            Cells = new Cell[width / cellSize, height / cellSize];
            InitializeCells();
            ConnectNeighbors();
        }

        private void InitializeCells()
        {
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();
        }

        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            Generation++;
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }
        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;

                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].neighbors.Add(Cells[x, yT]);
                    Cells[x, y].neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].neighbors.Add(Cells[xL, y]);
                    Cells[x, y].neighbors.Add(Cells[xR, y]);
                    Cells[x, y].neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].neighbors.Add(Cells[x, yB]);
                    Cells[x, y].neighbors.Add(Cells[xR, yB]);
                }
            }
        }
    }

    public class ConfigReader
    {
        public static BoardConfig ReadSettings(string filePath)
        {
            return JsonSerializer.Deserialize<BoardConfig>(File.ReadAllText(filePath));
        }
    }

    public class BoardConfig
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int CellSize { get; set; }
        public double LiveDensity { get; set; }

        public BoardConfig(int width, int height, int cellSize, double liveDensity)
        {
            Width = width;
            Height = height;
            CellSize = cellSize;
            LiveDensity = liveDensity;
        }
    }

    public static class FileHandler
    {
        public static void SaveBoardState(string filePath, Board board)
        {
            using var writer = new StreamWriter(filePath);
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                    writer.Write(board.Cells[col, row].IsAlive ? '1' : '0');
                writer.WriteLine();
            }
        }

        public static Board LoadBoardState(string filePath)
        {
            var lines = File.ReadAllLines(filePath);
            var board = new Board(lines[0].Length, lines.Length, 1);

            for (int row = 0; row < lines.Length; row++)
                for (int col = 0; col < lines[row].Length; col++)
                    board.Cells[col, row].IsAlive = lines[row][col] == '1';

            return board;
        }
    }

    public class BoardAnalyzer
    {
        public Board Board { get; private set; }
        public int LiveCellsCount { get; private set; }
        public int StableGeneration { get; private set; }
        public Dictionary<string, int[,]> Patterns { get; }

        private int stablePhaseDuration = 15;
        private int currentStableTime;

        public BoardAnalyzer(Board board, string patternsDirectory)
        {
            Board = board;
            Patterns = LoadPatterns(patternsDirectory);
            LiveCellsCount = CountLiveCells();
        }

        private Dictionary<string, int[,]> LoadPatterns(string directory)
        {
            var patterns = new Dictionary<string, int[,]>();

            foreach (var file in Directory.GetFiles(directory))
            {
                var lines = File.ReadAllLines(file);
                var pattern = new int[lines.Length, lines[0].Length];

                for (int i = 0; i < lines.Length; i++)
                {
                    for (int j = 0; j < lines[i].Length; j++)
                    {
                        pattern[i, j] = lines[i][j] == '1' ? 1 : 0;
                    }
                }

                patterns.Add(Path.GetFileNameWithoutExtension(file), pattern);
            }

            return patterns;
        }

        public bool IsStable()
        {
            int currentCount = CountLiveCells();

            if (currentCount == LiveCellsCount)
                currentStableTime++;
            else
                currentStableTime = 0;

            LiveCellsCount = currentCount;

            if (currentStableTime >= stablePhaseDuration)
            {
                StableGeneration = currentStableTime == stablePhaseDuration ? Board.Generation : StableGeneration;
                return true;
            }

            return false;
        }

        public int CountLiveCells()
        {
            int count = 0;
            foreach (var cell in Board.Cells)
                if (cell.IsAlive) count++;
            return count;
        }

        public Dictionary<string, int> ClassifyPatterns()
        {
            var patternCounts = Patterns.Keys.ToDictionary(name => name, _ => 0);
            patternCounts["Unknown"] = 0;

            var visited = new bool[Board.Columns, Board.Rows];

            for (int x = 0; x < Board.Columns; x++)
            {
                for (int y = 0; y < Board.Rows; y++)
                {
                    if (Board.Cells[x, y].IsAlive && !visited[x, y])
                    {
                        var group = FindCellGroup(x, y, visited);
                        bool found = false;

                        foreach (var pattern in Patterns)
                        {
                            if (IsPatternMatch(group, pattern.Value))
                            {
                                patternCounts[pattern.Key]++;
                                found = true;
                                break;
                            }
                        }

                        if (!found && group.Count > 1)
                            patternCounts["Unknown"]++;
                    }
                }
            }

            return patternCounts;
        }

        public List<(int x, int y)> FindCellGroup(int startX, int startY, bool[,] visited)
        {
            var group = new List<(int x, int y)>();
            var queue = new Queue<(int x, int y)>();

            queue.Enqueue((startX, startY));
            visited[startX, startY] = true;

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                group.Add((x, y));

                // Проверяем всех 8 соседей
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue; // Пропускаем саму клетку

                        int nx = (x + dx + Board.Columns) % Board.Columns;
                        int ny = (y + dy + Board.Rows) % Board.Rows;

                        if (Board.Cells[nx, ny].IsAlive && !visited[nx, ny])
                        {
                            visited[nx, ny] = true;
                            queue.Enqueue((nx, ny));
                        }
                    }
                }
            }

            return group;
        }

        private bool IsPatternMatch(List<(int x, int y)> group, int[,] pattern)
        {
            int patternCells = pattern.Cast<int>().Count(c => c == 1);
            if (group.Count != patternCells)
                return false;

            int minX = group.Min(c => c.x);
            int minY = group.Min(c => c.y);

            for (int rotation = 0; rotation < 4; rotation++)
            {
                var rotated = RotatePattern(pattern, rotation);
                if (CheckPattern(group, rotated, minX, minY)) return true;

                var mirrored = MirrorPattern(rotated);
                if (CheckPattern(group, mirrored, minX, minY)) return true;
            }

            return false;
        }

        private bool CheckPattern(List<(int x, int y)> group, int[,] pattern, int offsetX, int offsetY)
        {
            int patternRows = pattern.GetLength(0);
            int patternCols = pattern.GetLength(1);

            // Проверяем, что группа полностью помещается в шаблон
            if (offsetX + patternRows > Board.Columns || offsetY + patternCols > Board.Rows)
                return false;

            for (int i = 0; i < patternRows; i++)
            {
                for (int j = 0; j < patternCols; j++)
                {
                    bool expected = pattern[i, j] == 1;
                    bool actual = group.Contains((offsetX + i, offsetY + j));
                    if (expected != actual) return false;
                }
            }
            return true;
        }

        private int[,] RotatePattern(int[,] pattern, int rotation)
        {
            int rows = pattern.GetLength(0);
            int cols = pattern.GetLength(1);

            int newRows = (rotation % 2 == 1) ? cols : rows;
            int newCols = (rotation % 2 == 1) ? rows : cols;

            var result = new int[newRows, newCols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    switch (rotation % 4)
                    {
                        case 0:
                            result[i, j] = pattern[i, j];
                            break;
                        case 1:
                            result[j, newCols - 1 - i] = pattern[i, j];
                            break;
                        case 2:
                            result[newRows - 1 - i, newCols - 1 - j] = pattern[i, j];
                            break;
                        case 3:
                            result[newRows - 1 - j, i] = pattern[i, j];
                            break;
                    }
                }
            }

            return result;
        }

        private int[,] MirrorPattern(int[,] pattern)
        {
            int rows = pattern.GetLength(0);
            int cols = pattern.GetLength(1);
            var result = new int[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    result[i, cols - 1 - j] = pattern[i, j];
                }
            }

            return result;
        }
    }


    class Program
    {
        static Board board;
        static BoardAnalyzer analyzer;
        static BoardConfig settings;
        static int generation = 0;
        static bool isPaused = false;


        static void Reset(BoardConfig properties, string patternsDir)
        {
            board = new Board(
                properties.Width,
                properties.Height,
                properties.CellSize,
                properties.LiveDensity);

            analyzer = new BoardAnalyzer(board, patternsDir);
            generation = 0;
        }

        static void Render()
        {
            var display = new StringBuilder();

            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)
                    display.Append(board.Cells[col, row].IsAlive ? '*' : ' ');
                display.AppendLine();
            }

            display.Append(GetStatusInfo());
            Console.SetCursorPosition(0, 0);
            Console.Write(display);
        }

        static string GetStatusInfo()
        {
            var info = new StringBuilder();
            info.AppendLine($"Generation: {generation}");
            info.AppendLine($"Live cells: {analyzer.CountLiveCells()}");

            if (analyzer.IsStable())
            {
                info.AppendLine($"Stable since generation: {analyzer.StableGeneration}");
                var patterns = analyzer.ClassifyPatterns();

                foreach (var pattern in patterns)
                    info.AppendLine($"{pattern.Key}: {pattern.Value}");

                info.AppendLine($"Total combinations: {patterns.Sum(p => p.Value)}");
            }

            return info.ToString();
        }


        static bool HandleInput(string savePath)
        {
            if (!Console.KeyAvailable) return false;

            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.S:
                    FileHandler.SaveBoardState(savePath, board);
                    Console.WriteLine("Board state saved!");
                    return false;
                case ConsoleKey.P:
                    isPaused = !isPaused;
                    return false;
                case ConsoleKey.Escape:
                    return true;
                default:
                    return false;
            }
        }

        static void RunSimulation(string savePath)
        {
            while (true)
            {
                if (HandleInput(savePath)) break;

                if (!isPaused)
                {
                    generation++;
                    Console.Clear();
                    Render();
                    board.Advance();
                    Thread.Sleep(100);
                }
            }
        }

        static void AnalyzeStabilityWriter(string outputPath, double densityStep)
        {
            var results = new StringBuilder();
            string patternsDir = Path.Combine(Environment.CurrentDirectory, "patterns");

            for (double density = 0.05; density <= 1.0; density += densityStep)
            {
                var testBoard = new Board(
                    settings.Width,
                    settings.Height,
                    settings.CellSize,
                    density);

                var testAnalyzer = new BoardAnalyzer(testBoard, patternsDir);

                for (int gen = 0; gen < 10000; gen++)
                {
                    testBoard.Advance();
                    if (testAnalyzer.IsStable())
                    {
                        results.AppendLine($"{density} {testAnalyzer.StableGeneration}");
                        break;
                    }
                }
            }

            File.WriteAllText(outputPath, results.ToString());
        }

        static void Main(string[] args)
        {
            string settingsPath = @"C:\Users\vniki\source\repos\Yurlova-mod-lab04\mod-lab04-life\Life\config.json";
            string projectDir = Environment.CurrentDirectory;
            //string settingsPath = Path.Combine(Environment.CurrentDirectory, "config.json");
            string savePath = Path.Combine(Environment.CurrentDirectory, "board.txt");
            string patternsDir = Path.Combine(projectDir, "patterns");
            string analysisPath = Path.Combine(projectDir, "analysis.txt");


            Reset(ConfigReader.ReadSettings(settingsPath), patternsDir);
            RunSimulation(savePath);

        }
    }
}