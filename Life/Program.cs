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
        private bool IsAliveNext;
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

        public int Columns { get { return Cells.GetLength(0); } }
        public int Rows { get { return Cells.GetLength(1); } }
        public int Width { get { return Columns * CellSize; } }
        public int Height { get { return Rows * CellSize; } }

        public Board(int width, int height, int cellSize, double liveDensity = .1)
        {
            CellSize = cellSize;

            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            Randomize(liveDensity);
        }

        readonly Random rand = new Random();
        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
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

    class Program
    {
        static Board board;
        static BoardConfig settings;
        static int generation = 0;
        static bool isPaused = false;
        

        static void Reset(BoardConfig properties)
        {
            board = new Board(
                properties.Width,
                properties.Height,
                properties.CellSize,
                properties.LiveDensity);
            
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
            
            //display.Append(GetStatusInfo());
            Console.SetCursorPosition(0, 0);
            Console.Write(display);
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
        
        static void Main(string[] args)
        {
            string settingsPath = @"C:\Users\vniki\source\repos\Yurlova-mod-lab04\mod-lab04-life\Life\config.json";
            string projectDir = Environment.CurrentDirectory;
            //string settingsPath = Path.Combine(Environment.CurrentDirectory, "config.json");
            string savePath = Path.Combine(Environment.CurrentDirectory, "board.txt");
            //string patternsDir = Path.Combine(projectDir, "patterns");
            //string analysisPath = Path.Combine(projectDir, "analysis.txt");


            Reset(ConfigReader.ReadSettings(settingsPath));
            RunSimulation(savePath);
            
        }
    }
}