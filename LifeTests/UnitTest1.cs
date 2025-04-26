using Xunit;
using cli_life;


namespace LifeTests;

public class CellTests
{
    [Fact]
    public void Cell_StaysAlive_With2or3Neighbors()
    {
        var cell = new Cell { IsAlive = true };
        cell.neighbors.AddRange(Enumerable.Repeat(new Cell { IsAlive = true }, 2));
        cell.DetermineNextLiveState();
        Assert.True(cell.IsAliveNext);
    }

    [Fact]
    public void Cell_Dies_WithLessThan2Neighbors()
    {
        var cell = new Cell { IsAlive = true };
        cell.neighbors.Add(new Cell { IsAlive = true });
        cell.DetermineNextLiveState();
        Assert.False(cell.IsAliveNext);
    }

    [Fact]
    public void Cell_Dies_WithMoreThan3Neighbors()
    {
        var cell = new Cell { IsAlive = true };
        cell.neighbors.AddRange(Enumerable.Repeat(new Cell { IsAlive = true }, 4));
        cell.DetermineNextLiveState();
        Assert.False(cell.IsAliveNext);
    }

    [Fact]
    public void Cell_BecomesAlive_WithExactly3Neighbors()
    {
        var cell = new Cell { IsAlive = false };
        cell.neighbors.AddRange(Enumerable.Repeat(new Cell { IsAlive = true }, 3));
        cell.DetermineNextLiveState();
        Assert.True(cell.IsAliveNext);
    }

    [Fact]
    public void Cell_WithNoNeighbors_Dies()
    {
        var cell = new Cell { IsAlive = true };
        cell.DetermineNextLiveState();
        Assert.False(cell.IsAliveNext);
    }
}

public class BoardTests
{
    [Fact]
    public void Board_Init_CorrectDimensions()
    {
        var board = new Board(100, 50, 10);
        Assert.Equal(10, board.Columns);
        Assert.Equal(5, board.Rows);
    }

    [Fact]
    public void Board_ConnectsNeighbors()
    {
        var board = new Board(30, 30, 10);
        var cell = board.Cells[1, 1];
        Assert.Equal(8, cell.neighbors.Count);
    }

    [Fact]
    public void Board_Randomize_SetCellsAlive()
    {
        var board = new Board(100, 100, 10, 0.5);
        int aliveCount = board.Cells.Cast<Cell>().Count(c => c.IsAlive);
        Assert.True(aliveCount > 0);
    }

    [Fact]
    public void Board_Advance_UpdatesGeneration()
    {
        var board = new Board(100, 100, 10);
        board.Advance();
        Assert.Equal(1, board.Generation);
    }
}

public class FileHandlerTests
{
    [Fact]
    public void FileHandler_SaveAndLoad()
    {
        var board1 = new Board(30, 30, 10);
        board1.Cells[1, 1].IsAlive = true;
        board1.Cells[2, 2].IsAlive = true;

        FileHandler.SaveBoardState("test_board.txt", board1);
        var board2 = FileHandler.LoadBoardState("test_board.txt");

        Assert.Equal(board1.Cells[1, 1].IsAlive, board2.Cells[1, 1].IsAlive);
        Assert.Equal(board1.Cells[2, 2].IsAlive, board2.Cells[2, 2].IsAlive);

        File.Delete("test_board.txt");
    }
}

public class ConfigReaderTests
{
    [Fact]
    public void ConfigReader_ReadsSettings_Correctly()
    {
        File.WriteAllText("testConfig.json",
            "{\"Width\":100,\"Height\":50,\"CellSize\":5,\"LiveDensity\":0.3}");

        var config = ConfigReader.ReadSettings("testConfig.json");

        Assert.Equal(100, config.Width);
        Assert.Equal(50, config.Height);
        Assert.Equal(5, config.CellSize);
        Assert.Equal(0.3, config.LiveDensity);

        File.Delete("test_config.json");
    }
}


public class BoardAnalyzerTests
{
    readonly string patternsdir = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Life", "patterns");

    [Fact]
    public void BoardAnalyzer_CountLiveCells_ReturnsCorrectCount()
    {
        var board = new Board(5, 5, 1);
        board.Cells[1, 1].IsAlive = true;
        board.Cells[2, 2].IsAlive = true;

        var analyzer = new BoardAnalyzer(board, patternsdir);
        Assert.Equal(2, analyzer.CountLiveCells());
    }

    [Fact]
    public void BoardAnalyzer_IsStable_DetectsStableState()
    {
        var board = new Board(30, 30, 10);
        var analyzer = new BoardAnalyzer(board, patternsdir);

        for (int i = 0; i < 20; i++)
            analyzer.IsStable();

        Assert.True(analyzer.IsStable());
    }

    [Fact]
    public void BoardAnalyzer_FindCellGroup_FindsConnectedCells()
    {
        var board = new Board(3, 3, 1);
        board.Cells[1, 1].IsAlive = true;
        board.Cells[1, 2].IsAlive = true;
        board.Cells[2, 1].IsAlive = true;

        var analyzer = new BoardAnalyzer(board, patternsdir);
        var group = analyzer.FindCellGroup(1, 1, new bool[board.Columns, board.Rows]);

        Assert.Equal(3, group.Count);
    }

    [Fact]
    public void BoardAnalyzer_CountsLiveCells_InEmptyBoard()
    {
        var board = new Board(3, 3, 1);
        var analyzer = new BoardAnalyzer(board, patternsdir);

        var count = analyzer.CountLiveCells();

        Assert.Equal(0, count);
    }

    [Fact]
    public void BoardAnalyzer_CountsLiveCells_InFullBoard()
    {
        var board = new Board(3, 3, 1);
        foreach (var cell in board.Cells) cell.IsAlive = true;
        var analyzer = new BoardAnalyzer(board, patternsdir);
        var count = analyzer.CountLiveCells();

        Assert.Equal(9, count);
    }

    [Fact]
    public void BoardAnalysis_GetPatterns_Correctly()
    {
        var board = new Board(10, 10, 1);
        var analyzer = new BoardAnalyzer(board, patternsdir);
        var patterns = analyzer.Patterns;

        Assert.Equal(7, patterns.Count);
        Assert.Contains(patterns, p => p.Key == "blinker");
        Assert.Contains(patterns, p => p.Key == "block");
        Assert.Contains(patterns, p => p.Key == "boat");
        Assert.Contains(patterns, p => p.Key == "bond");
        Assert.Contains(patterns, p => p.Key == "box");
        Assert.Contains(patterns, p => p.Key == "glider");
        Assert.Contains(patterns, p => p.Key == "hive");
    }
}
