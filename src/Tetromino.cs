namespace Tetriorun;

public enum TetrominoType { I, O, T, S, Z, J, L }
public enum RotationState { North = 0, East = 1, South = 2, West = 3 }

public static class Tetromino
{
    // 4x4 bitmasks, row-major, bit 15 = top-left cell
    // Each index: [type][rotation]
    private static readonly ushort[][] BitGrids =
    [
        [0x0F00, 0x2222, 0x00F0, 0x4444], // I  (row1 / col2 / row2 / col1)
        [0x0660, 0x0660, 0x0660, 0x0660], // O
        [0x4E00, 0x4640, 0x0E40, 0x4C40], // T  N:.T./TTT  E:.T./.TT/.T.  S:TTT/.T.  W:.T./TT./.T.
        [0x6C00, 0x8C40, 0x06C0, 0x4620], // S  N:.SS/SS.  E:S./SS./.S.   S:.SS/SS.→dn  W:.S./.SS/..S
        [0xC600, 0x4C80, 0x0C60, 0x2640], // Z  N:ZZ./.ZZ  E:.Z/ZZ/Z.     S:ZZ./.ZZ→dn  W:..Z/.ZZ/.Z.
        [0x8E00, 0x6440, 0x0E20, 0x44C0], // J  N:J./JJJ   E:.JJ/.J./.J.  S:JJJ/..J  W:.J./.J./JJ.
        [0x2E00, 0x4460, 0x0E80, 0xC440], // L  N:..L/LLL  E:.L./.L./.LL  S:LLL/L..  W:LL./.L./.L.
    ];

    public static IEnumerable<(int col, int row)> GetCells(TetrominoType type, RotationState rotation)
    {
        ushort grid = BitGrids[(int)type][(int)rotation];
        for (int i = 0; i < 16; i++)
            if ((grid & (0x8000 >> i)) != 0)
                yield return (i % 4, i / 4);
    }

    // ---- SRS Wall Kick Tables ----
    // Guideline uses Y-up; our board uses Y-down, so Y is negated.
    // Index: [fromRotation (0-3)][testIndex (0-4)]
    // CW rotation transitions: N->E=0, E->S=1, S->W=2, W->N=3
    // CCW rotation transitions: N->W=0, E->N=1, S->E=2, W->S=3

    private static readonly (int dx, int dy)[][] KicksCW_JLSTZ =
    [
        [(0,0), (-1,0), (-1,-1), (0, 2), (-1, 2)], // N->E
        [(0,0), ( 1,0), ( 1, 1), (0,-2), ( 1,-2)], // E->S
        [(0,0), ( 1,0), ( 1,-1), (0, 2), ( 1, 2)], // S->W
        [(0,0), (-1,0), (-1, 1), (0,-2), (-1,-2)], // W->N
    ];

    private static readonly (int dx, int dy)[][] KicksCCW_JLSTZ =
    [
        [(0,0), ( 1,0), ( 1,-1), (0, 2), ( 1, 2)], // N->W
        [(0,0), ( 1,0), ( 1, 1), (0,-2), ( 1,-2)], // E->N
        [(0,0), (-1,0), (-1,-1), (0, 2), (-1, 2)], // S->E
        [(0,0), (-1,0), (-1, 1), (0,-2), (-1,-2)], // W->S
    ];

    private static readonly (int dx, int dy)[][] KicksCW_I =
    [
        [(0,0), (-2,0), ( 1,0), (-2, 1), ( 1,-2)], // N->E
        [(0,0), (-1,0), ( 2,0), (-1,-2), ( 2, 1)], // E->S
        [(0,0), ( 2,0), (-1,0), ( 2,-1), (-1, 2)], // S->W
        [(0,0), ( 1,0), (-2,0), ( 1, 2), (-2,-1)], // W->N
    ];

    private static readonly (int dx, int dy)[][] KicksCCW_I =
    [
        [(0,0), (-1,0), ( 2,0), (-1,-2), ( 2, 1)], // N->W
        [(0,0), ( 2,0), (-1,0), ( 2,-1), (-1, 2)], // E->N
        [(0,0), (-2,0), ( 1,0), (-2, 1), ( 1,-2)], // S->E
        [(0,0), ( 1,0), (-2,0), ( 1, 2), (-2,-1)], // W->S
    ];

    public static (int dx, int dy)[] GetWallKicks(TetrominoType type, RotationState from, bool cw)
    {
        if (type == TetrominoType.O) return [(0, 0)];
        int idx = (int)from;
        if (type == TetrominoType.I)
            return cw ? KicksCW_I[idx] : KicksCCW_I[idx];
        return cw ? KicksCW_JLSTZ[idx] : KicksCCW_JLSTZ[idx];
    }
}

public record ActivePiece(TetrominoType Type, RotationState Rotation, int Col, int Row)
{
    public IEnumerable<(int col, int row)> GetAbsoluteCells() =>
        GetAbsoluteCells(Rotation);

    public IEnumerable<(int col, int row)> GetAbsoluteCells(RotationState r)
    {
        foreach (var (dc, dr) in Tetromino.GetCells(Type, r))
            yield return (Col + dc, Row + dr);
    }
}
