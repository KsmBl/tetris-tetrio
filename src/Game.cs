namespace Tetriorun;

public enum GamePhase { Playing, GameOver }

public class GameState
{
    public const int BoardCols    = 10;
    public const int BoardRows    = 24; // 0-3 = buffer, 4-23 = visible
    public const int VisibleStart = 4;
    public const int VisibleRows  = BoardRows - VisibleStart; // 20

    // board[row, col]
    public TetrominoType?[,] Board { get; } = new TetrominoType?[BoardRows, BoardCols];

    public ActivePiece?     CurrentPiece { get; private set; }
    public TetrominoType?   HeldPiece    { get; private set; }
    public bool             CanHold      { get; private set; } = true;
    public GamePhase        Phase        { get; private set; } = GamePhase.Playing;
    public int              LinesCleared { get; private set; }
    public int              PiecesPlaced { get; private set; }
    public double           ElapsedSeconds { get; private set; }

    public IReadOnlyList<TetrominoType> NextQueue => _nextList;

    private readonly Queue<TetrominoType> _next = new();
    private readonly List<TetrominoType>  _nextList = [];

    private const float LockDelayMs  = 500f;
    private const int   MaxLockResets = 15;
    private float _lockElapsedMs;
    private bool  _lockActive;
    private int   _lockResets;
    private bool  _onGround;

    // Gravity accumulator (in cells)
    private double _gravAccum;
    public GameState()
    {
        EnsureQueue();
    }

    public void SpawnNext()
    {
        EnsureQueue();
        var type = _next.Dequeue();
        _nextList.RemoveAt(0);
        EnsureQueue();

        // Spawn at col=3, row=-1 (row -1 relative to board = row 3 in buffer)
        // S and Z spawn at 180° (South); their 4x4 cells are in rows 1-2 so spawn one row higher
        bool southSpawn = type == TetrominoType.S || type == TetrominoType.Z;
        var spawnRot = southSpawn ? RotationState.South : RotationState.North;
        var spawnRow = southSpawn ? VisibleStart - 2 : VisibleStart - 1;
        var piece = new ActivePiece(type, spawnRot, 3, spawnRow);

        // Shift down if top row of 4x4 grid is empty (so piece appears at row 4)
        // Check: does any cell land in row < VisibleStart-1 ?  Just spawn and let it be.
        CurrentPiece = piece;

        if (!IsValid(piece))
        {
            Phase = GamePhase.GameOver;
            CurrentPiece = null;
        }

        CanHold       = true;
        _lockActive   = false;
        _lockElapsedMs = 0;
        _lockResets   = 0;
        _onGround     = false;
        _gravAccum    = 0;
    }

    public void Tick(InputFrame input, float deltaMs)
    {
        if (Phase == GamePhase.GameOver || CurrentPiece == null) return;

        ElapsedSeconds += deltaMs / 1000.0;

        // 1. Hard drop
        if (input.HardDrop)
        {
            HardDrop();
            return;
        }

        // 2. Hold
        if (input.Hold && CanHold)
            DoHold();

        if (CurrentPiece == null) return;

        // 3. Rotate
        if (input.RotateCW)  TryRotate(cw: true);
        if (input.RotateCCW) TryRotate(cw: false);
        if (input.Rotate180) TryRotate180();

        // 4. Horizontal movement
        int leftSteps  = input.MoveLeftSteps;
        int rightSteps = input.MoveRightSteps;
        // Only apply the dominant direction
        if (leftSteps > 0 && rightSteps == 0)
            for (int i = 0; i < leftSteps; i++) TryMove(-1, 0);
        else if (rightSteps > 0 && leftSteps == 0)
            for (int i = 0; i < rightSteps; i++) TryMove(1, 0);

        // 5. Gravity
        if (input.SoftDropHeld)
        {
            // Unlimited soft drop: snap to bottom instantly (does not reset lock delay)
            while (true)
            {
                var below = CurrentPiece with { Row = CurrentPiece.Row + 1 };
                if (!IsValid(below)) break;
                CurrentPiece = below;
            }
        }
        else
        {
            // Normal gravity: 1G = 1 cell/second
            _gravAccum += deltaMs / 1000.0;
            int drop = (int)_gravAccum;
            _gravAccum -= drop;
            for (int i = 0; i < drop; i++)
            {
                if (!TryMove(0, 1)) { _gravAccum = 0; break; }
            }
        }

        // 6. Lock delay
        bool groundNow = !CanMoveDown();
        if (groundNow)
        {
            if (!_onGround)
            {
                _lockElapsedMs = 0;
                _lockActive    = true;
                _onGround      = true;
            }
            else if (_lockActive)
            {
                _lockElapsedMs += deltaMs;
                if (_lockElapsedMs >= LockDelayMs)
                    LockPiece();
            }
        }
        else
        {
            _onGround  = false;
            _lockActive = false;
        }
    }

    private void HardDrop()
    {
        if (CurrentPiece == null) return;
        while (TryMove(0, 1)) { }
        LockPiece();
    }

    private void DoHold()
    {
        if (CurrentPiece == null || !CanHold) return;
        CanHold = false;
        var type = CurrentPiece.Type;
        if (HeldPiece == null)
        {
            HeldPiece    = type;
            CurrentPiece = null;
            SpawnNext();
        }
        else
        {
            var swap     = HeldPiece.Value;
            HeldPiece    = type;
            bool hSouth  = swap == TetrominoType.S || swap == TetrominoType.Z;
            var hRot     = hSouth ? RotationState.South : RotationState.North;
            var hRow     = hSouth ? VisibleStart - 2    : VisibleStart - 1;
            var piece    = new ActivePiece(swap, hRot, 3, hRow);
            CurrentPiece = IsValid(piece) ? piece : null;
            if (CurrentPiece == null) Phase = GamePhase.GameOver;
        }
        _lockActive   = false;
        _lockElapsedMs = 0;
        _lockResets   = 0;
        _onGround     = false;
        _gravAccum    = 0;
    }

    private bool TryMove(int dc, int dr)
    {
        if (CurrentPiece == null) return false;
        var moved = CurrentPiece with { Col = CurrentPiece.Col + dc, Row = CurrentPiece.Row + dr };
        if (!IsValid(moved)) return false;
        bool wasOnGround = !CanMoveDown();
        CurrentPiece = moved;
        if (wasOnGround && _lockResets < MaxLockResets)
        {
            _lockElapsedMs = 0; // reset lock delay on successful move
            _lockResets++;
        }
        return true;
    }

    private void TryRotate(bool cw)
    {
        if (CurrentPiece == null) return;
        var newRot = (RotationState)(((int)CurrentPiece.Rotation + (cw ? 1 : 3)) % 4);
        var kicks  = Tetromino.GetWallKicks(CurrentPiece.Type, CurrentPiece.Rotation, cw);
        foreach (var (dx, dy) in kicks)
        {
            var test = CurrentPiece with { Rotation = newRot, Col = CurrentPiece.Col + dx, Row = CurrentPiece.Row + dy };
            if (IsValid(test)) { ApplyRotated(test); return; }
        }
        // Wall-push: SRS kicks failed; try nudging horizontally to clear the wall.
        // IsValid rejects positions that overlap placed blocks, so this never presses into blocks.
        for (int push = 1; push <= 4; push++)
        {
            foreach (int sign in (ReadOnlySpan<int>)[-1, 1])
            {
                var test = CurrentPiece with { Rotation = newRot, Col = CurrentPiece.Col + push * sign };
                if (IsValid(test)) { ApplyRotated(test); return; }
            }
        }
    }

    private void ApplyRotated(ActivePiece test)
    {
        bool wasOnGround = !CanMoveDown();
        CurrentPiece = test;
        if (wasOnGround && _lockResets < MaxLockResets) { _lockElapsedMs = 0; _lockResets++; }
    }

    private void TryRotate180()
    {
        if (CurrentPiece == null) return;
        var target = (RotationState)(((int)CurrentPiece.Rotation + 2) % 4);
        // Try no-kick, then up/down/left/right offsets
        ReadOnlySpan<(int dx, int dy)> kicks = [(0,0), (0,-1), (0,1), (-1,0), (1,0), (0,-2), (0,2)];
        foreach (var (dx, dy) in kicks)
        {
            var test = CurrentPiece with { Rotation = target, Col = CurrentPiece.Col + dx, Row = CurrentPiece.Row + dy };
            if (IsValid(test)) { ApplyRotated(test); return; }
        }
    }

    private void LockPiece()
    {
        if (CurrentPiece == null) return;
        foreach (var (c, r) in CurrentPiece.GetAbsoluteCells())
            if (r >= 0 && r < BoardRows && c >= 0 && c < BoardCols)
                Board[r, c] = CurrentPiece.Type;
        PiecesPlaced++;
        int cleared = ClearLines();
        LinesCleared += cleared;
        CurrentPiece = null;
        if (LinesCleared >= 40)
        {
            Phase = GamePhase.GameOver;
            return;
        }
        SpawnNext();
    }

    private int ClearLines()
    {
        int cleared = 0;
        for (int r = BoardRows - 1; r >= 0; r--)
        {
            bool full = true;
            for (int c = 0; c < BoardCols; c++)
                if (Board[r, c] == null) { full = false; break; }
            if (!full) continue;
            // Shift rows above down
            for (int rr = r; rr > 0; rr--)
                for (int c = 0; c < BoardCols; c++)
                    Board[rr, c] = Board[rr - 1, c];
            for (int c = 0; c < BoardCols; c++) Board[0, c] = null;
            cleared++;
            r++; // re-check same row index after shift
        }
        return cleared;
    }

    private bool CanMoveDown() =>
        CurrentPiece != null && IsValid(CurrentPiece with { Row = CurrentPiece.Row + 1 });

    public ActivePiece? GetGhostPiece()
    {
        if (CurrentPiece == null) return null;
        var ghost = CurrentPiece;
        while (IsValid(ghost with { Row = ghost.Row + 1 }))
            ghost = ghost with { Row = ghost.Row + 1 };
        return ghost.Row == CurrentPiece.Row ? null : ghost;
    }

    private bool IsValid(ActivePiece p)
    {
        foreach (var (c, r) in p.GetAbsoluteCells())
        {
            if (c < 0 || c >= BoardCols) return false;
            if (r >= BoardRows) return false;
            if (r >= 0 && Board[r, c] != null) return false;
        }
        return true;
    }

    private void EnsureQueue()
    {
        while (_next.Count < 6)
        {
            var bag = Enum.GetValues<TetrominoType>().ToArray();
            Shuffle(bag);
            foreach (var t in bag) _next.Enqueue(t);
        }
        // Sync readable list
        _nextList.Clear();
        _nextList.AddRange(_next.Take(5));
    }

    private static void Shuffle<T>(T[] arr)
    {
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
    }
}
