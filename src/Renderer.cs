using Raylib_cs;

namespace Tetriorun;

public static class Colors
{
    public static readonly Color[] Piece =
    [
        new Color(40,  200, 255, 255), // I  — cyan
        new Color(255, 210, 0,   255), // O  — yellow
        new Color(180, 70,  240, 255), // T  — purple
        new Color(60,  215, 90,  255), // S  — green
        new Color(240, 50,  60,  255), // Z  — red
        new Color(55,  100, 240, 255), // J  — blue
        new Color(255, 140, 20,  255), // L  — orange
    ];

    // Per-piece highlight tint (lighter version for bevel)
    public static readonly Color[] PieceHi =
    [
        new Color(160, 235, 255, 200),
        new Color(255, 240, 140, 200),
        new Color(220, 150, 255, 200),
        new Color(150, 245, 170, 200),
        new Color(255, 140, 140, 200),
        new Color(130, 165, 255, 200),
        new Color(255, 205, 120, 200),
    ];

    public static readonly Color Ghost    = new(255, 255, 255, 22);
    public static readonly Color GhostBorder = new(255, 255, 255, 80);
    public static readonly Color Bg       = new(12,  12,  22,  255);
    public static readonly Color BoardBg  = new(22,  22,  38,  255);
    public static readonly Color GridLine = new(38,  38,  58,  255);
    public static readonly Color Border   = new(70,  70,  100, 255);
    public static readonly Color BorderHi = new(110, 110, 160, 255);
    public static readonly Color Panel    = new(18,  18,  30,  255);
    public static readonly Color PanelBorder = new(55, 55, 85, 255);
    public static readonly Color Text     = new(220, 220, 235, 255);
    public static readonly Color Dim      = new(100, 100, 125, 255);
    public static readonly Color Accent   = new(255, 210, 0,   255);
    public static readonly Color AccentDim= new(180, 148, 0,   255);
}

public sealed class Renderer
{
    // Layout
    private const int CellSize   = 30;
    private const int MiniCell   = 20;
    private const int HoldPanelX = 10;
    private const int BoardLeft  = HoldPanelX + 4 * MiniCell + 24;   // = 114
    private const int BoardTop   = 72;
    private const int PanelLeft  = BoardLeft + GameState.BoardCols * CellSize + 18;
    private const int PanelW     = 112;
    public  const int WindowW    = PanelLeft + PanelW + 14;
    public  const int WindowH    = BoardTop + GameState.VisibleRows * CellSize + 48;

    // Hold panel box dimensions
    private const int HoldBoxW = 4 * MiniCell + 4;
    private const int HoldBoxH = 4 * MiniCell + 4;

    // -----------------------------------------------------------------------
    public void DrawGame(GameState state)
    {
        Raylib.ClearBackground(Colors.Bg);
        DrawTitle();
        DrawBoardBackground();
        DrawLinesRemainingIndicator(state);
        DrawLockedCells(state);
        DrawGhost(state);
        DrawActivePiece(state);
        DrawBoardBorder();
        DrawHold(state);
        DrawNext(state);
        DrawStats(state);
    }

    public void DrawCountdown(int value, GameState state)
    {
        DrawGame(state);
        DrawOverlay(0, 0, 0, 150);
        string label = value == 0 ? "GO!" : value.ToString();
        int cx = BoardLeft + GameState.BoardCols * CellSize / 2;
        int cy = BoardTop  + GameState.VisibleRows * CellSize / 2;
        int fs = value == 0 ? 72 : 90;
        int tw = Raylib.MeasureText(label, fs);
        // shadow
        Raylib.DrawText(label, cx - tw / 2 + 3, cy - fs / 2 + 3, fs, new Color(0, 0, 0, 120));
        Raylib.DrawText(label, cx - tw / 2,     cy - fs / 2,     fs, Colors.Accent);
    }

    public void DrawGameOver(GameState state)
    {
        Raylib.ClearBackground(Colors.Bg);
        DrawOverlay(0, 0, 0, 210);

        int cx         = WindowW / 2;
        string timeStr = FormatTime(state.ElapsedSeconds);

        // Central card
        int cardW = 380; int cardH = 160;
        int cardX = cx - cardW / 2; int cardY = 60;
        Raylib.DrawRectangle(cardX, cardY, cardW, cardH, new Color(22, 22, 38, 245));
        Raylib.DrawRectangleLines(cardX, cardY, cardW, cardH, Colors.PanelBorder);
        // accent top bar
        Raylib.DrawRectangle(cardX, cardY, cardW, 3, Colors.Accent);

        DrawCenteredText("40 LINES COMPLETE", cx, cardY + 18, 26, Colors.Accent);
        // time — large
        int timeFontSize = 46;
        int tw = Raylib.MeasureText(timeStr, timeFontSize);
        Raylib.DrawText(timeStr, cx - tw / 2 + 2, cardY + 58, timeFontSize, new Color(0, 0, 0, 100));
        Raylib.DrawText(timeStr, cx - tw / 2,     cardY + 56, timeFontSize, Colors.Text);

        DrawCenteredText("press any key to exit", cx, cardY + cardH - 22, 13, Colors.Dim);
    }

    // -----------------------------------------------------------------------
    private static void DrawTitle()
    {
        const string title = "tetriorun";
        const int fs = 30;
        int tw = Raylib.MeasureText(title, fs);
        int tx = BoardLeft;
        int ty = 16;
        // shadow
        Raylib.DrawText(title, tx + 2, ty + 2, fs, new Color(0, 0, 0, 100));
        Raylib.DrawText(title, tx,     ty,     fs, Colors.Accent);
        // accent underline
        Raylib.DrawRectangle(tx, ty + fs + 3, tw, 2, Colors.AccentDim);
    }

    private static void DrawBoardBackground()
    {
        int bw = GameState.BoardCols * CellSize;
        int bh = GameState.VisibleRows * CellSize;
        Raylib.DrawRectangle(BoardLeft, BoardTop, bw, bh, Colors.BoardBg);

        // subtle grid lines
        for (int c = 1; c < GameState.BoardCols; c++)
            Raylib.DrawRectangle(BoardLeft + c * CellSize, BoardTop, 1, bh, Colors.GridLine);
        for (int r = 1; r < GameState.VisibleRows; r++)
            Raylib.DrawRectangle(BoardLeft, BoardTop + r * CellSize, bw, 1, Colors.GridLine);
    }

    private static void DrawBoardBorder()
    {
        int bw = GameState.BoardCols * CellSize;
        int bh = GameState.VisibleRows * CellSize;
        // outer glow border
        Raylib.DrawRectangleLines(BoardLeft - 2, BoardTop - 2, bw + 4, bh + 4,
            new Color(55, 55, 85, 120));
        // main border
        Raylib.DrawRectangleLines(BoardLeft - 1, BoardTop - 1, bw + 2, bh + 2, Colors.Border);
        // inner bright line (top + left only for subtle bevel)
        Raylib.DrawRectangle(BoardLeft, BoardTop, bw, 1, Colors.BorderHi);
        Raylib.DrawRectangle(BoardLeft, BoardTop, 1, bh, Colors.BorderHi);
    }

    private static void DrawLinesRemainingIndicator(GameState state)
    {
        int linesLeft = 40 - state.LinesCleared;
        if (linesLeft >= 20) return;

        int lineY  = BoardTop + (GameState.VisibleRows - linesLeft) * CellSize;
        int boardW = GameState.BoardCols * CellSize;
        int totalH = linesLeft * CellSize;

        const int Step = 2;
        for (int dy = 0; dy < totalH; dy += Step)
        {
            float t     = (float)dy / totalH;
            byte  alpha = (byte)(50 * (1f - t));
            Raylib.DrawRectangle(BoardLeft, lineY + dy, boardW, Step,
                new Color((byte)255, (byte)200, (byte)0, alpha));
        }
        // bright marker line
        Raylib.DrawRectangle(BoardLeft, lineY,     boardW, 2, new Color(255, 220, 0, 200));
        // tiny tick marks at each column boundary for clarity
        for (int c = 0; c <= GameState.BoardCols; c++)
            Raylib.DrawRectangle(BoardLeft + c * CellSize - 1, lineY - 2, 2, 5,
                new Color(255, 220, 0, 160));
    }

    private static void DrawLockedCells(GameState state)
    {
        for (int r = GameState.VisibleStart; r < GameState.BoardRows; r++)
        {
            int visRow = r - GameState.VisibleStart;
            for (int c = 0; c < GameState.BoardCols; c++)
            {
                var cell = state.Board[r, c];
                if (cell.HasValue)
                    DrawCell(c, visRow, Colors.Piece[(int)cell.Value],
                                        Colors.PieceHi[(int)cell.Value]);
            }
        }
    }

    private static void DrawGhost(GameState state)
    {
        var ghost = state.GetGhostPiece();
        if (ghost == null) return;
        foreach (var (gc, gr) in ghost.GetAbsoluteCells())
        {
            int visRow = gr - GameState.VisibleStart;
            if (visRow < 0 || visRow >= GameState.VisibleRows) continue;
            int x = BoardLeft + gc * CellSize;
            int y = BoardTop  + visRow * CellSize;
            // faint fill
            Raylib.DrawRectangle(x + 1, y + 1, CellSize - 2, CellSize - 2, Colors.Ghost);
            // outline in piece color (dimmed)
            Raylib.DrawRectangleLines(x + 1, y + 1, CellSize - 2, CellSize - 2, Colors.GhostBorder);
        }
    }

    private static void DrawActivePiece(GameState state)
    {
        if (state.CurrentPiece == null) return;
        int idx   = (int)state.CurrentPiece.Type;
        var color = Colors.Piece[idx];
        var hi    = Colors.PieceHi[idx];
        foreach (var (pc, pr) in state.CurrentPiece.GetAbsoluteCells())
        {
            int visRow = pr - GameState.VisibleStart;
            if (visRow < 0 || visRow >= GameState.VisibleRows) continue;
            DrawCell(pc, visRow, color, hi);
        }
    }

    private static void DrawCell(int col, int visRow, Color color, Color hi)
    {
        int x = BoardLeft + col * CellSize;
        int y = BoardTop  + visRow * CellSize;
        int s = CellSize;

        Raylib.DrawRectangle(x + 1, y + 1, s - 2, s - 2, color);
        // top highlight strip
        Raylib.DrawRectangle(x + 2, y + 2, s - 4, 3, hi);
        // left highlight strip
        Raylib.DrawRectangle(x + 2, y + 2, 3, s - 4, hi);
        // bottom-right shadow
        Raylib.DrawRectangle(x + 2,     y + s - 3, s - 3, 2, new Color(0, 0, 0, 80));
        Raylib.DrawRectangle(x + s - 3, y + 2,     2, s - 3, new Color(0, 0, 0, 80));
    }

    // -----------------------------------------------------------------------
    private static void DrawPanelBox(int x, int y, int w, int h, string label)
    {
        Raylib.DrawRectangle(x, y, w, h, Colors.Panel);
        Raylib.DrawRectangleLines(x, y, w, h, Colors.PanelBorder);
        // label tab above
        int lw = Raylib.MeasureText(label, 12);
        Raylib.DrawText(label, x + (w - lw) / 2, y - 16, 12, Colors.Dim);
    }

    private static void DrawHold(GameState state)
    {
        int boxX = HoldPanelX;
        int boxY = BoardTop;
        DrawPanelBox(boxX, boxY, HoldBoxW, HoldBoxH, "HOLD");

        if (state.HeldPiece.HasValue)
        {
            var color  = state.CanHold ? Colors.Piece[(int)state.HeldPiece.Value]
                                       : new Color(80, 80, 80, 255);
            var hiCol  = state.CanHold ? Colors.PieceHi[(int)state.HeldPiece.Value]
                                       : new Color(120, 120, 120, 180);
            DrawMiniPiece(state.HeldPiece.Value, boxX + 2, boxY + 2, color, hiCol);
        }
    }

    private static void DrawNext(GameState state)
    {
        int x    = PanelLeft;
        int boxW = PanelW;
        int boxH = MiniCell * 3 + 8;
        int y    = BoardTop;

        int lw = Raylib.MeasureText("NEXT", 12);
        Raylib.DrawText("NEXT", x + (boxW - lw) / 2, y - 16, 12, Colors.Dim);

        foreach (var t in state.NextQueue.Take(5))
        {
            DrawPanelBox(x, y, boxW, boxH, "");
            DrawMiniPiece(t, x + 2, y + 2,
                Colors.Piece[(int)t], Colors.PieceHi[(int)t]);
            y += boxH + 5;
        }
    }

    private static void DrawMiniPiece(TetrominoType type, int ox, int oy,
                                       Color color, Color hi)
    {
        // Centre the 4x4 grid inside the box
        int gridW = 4 * MiniCell;
        int boxW  = HoldBoxW - 4; // same for next boxes (both ≥ gridW)
        int offX  = (boxW - gridW) / 2;

        foreach (var (c, r) in Tetromino.GetCells(type, RotationState.North))
        {
            int x = ox + offX + c * MiniCell;
            int y = oy + 2    + r * MiniCell;
            int s = MiniCell;
            Raylib.DrawRectangle(x + 1, y + 1, s - 2, s - 2, color);
            Raylib.DrawRectangle(x + 2, y + 2, s - 4, 2,     hi);
            Raylib.DrawRectangle(x + 2, y + 2, 2,     s - 4, hi);
        }
    }

    private static void DrawStats(GameState state)
    {
        int x = PanelLeft;
        // place stats below the 5 next boxes (5 * (60+5) = 325) + 10 margin
        int y = BoardTop + 5 * (MiniCell * 3 + 8 + 5) + 10;

        DrawStatBlock(x, ref y, "TIME",
            FormatTime(state.ElapsedSeconds), Colors.Text);
        DrawStatBlock(x, ref y, "LINES",
            $"{state.LinesCleared} / 40", state.LinesCleared >= 30 ? Colors.Accent : Colors.Text);
        DrawStatBlock(x, ref y, "PIECES",
            $"{state.PiecesPlaced}", Colors.Text);
    }

    private static void DrawStatBlock(int x, ref int y, string label, string value, Color valColor)
    {
        Raylib.DrawText(label, x, y, 12, Colors.Dim);
        y += 15;
        Raylib.DrawText(value, x, y, 20, valColor);
        y += 28;
        // separator
        Raylib.DrawRectangle(x, y, PanelW, 1, Colors.PanelBorder);
        y += 10;
    }

    // -----------------------------------------------------------------------
    private static string FormatTime(double seconds)
    {
        int m    = (int)(seconds / 60);
        double s = seconds - m * 60;
        return $"{m}:{s:00.00}";
    }

    private static void DrawOverlay(byte r, byte g, byte b, byte a) =>
        Raylib.DrawRectangle(0, 0, WindowW, WindowH, new Color(r, g, b, a));

    private static void DrawCenteredText(string text, int cx, int y, int fontSize, Color color)
    {
        int tw = Raylib.MeasureText(text, fontSize);
        Raylib.DrawText(text, cx - tw / 2, y, fontSize, color);
    }
}
