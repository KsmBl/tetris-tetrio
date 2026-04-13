using Raylib_cs;

namespace Tetriorun;

public record InputFrame(
    int  MoveLeftSteps,
    int  MoveRightSteps,
    bool SoftDropHeld,
    bool HardDrop,
    bool RotateCW,
    bool RotateCCW,
    bool Rotate180,
    bool Hold
);

public sealed class InputHandler
{
    private readonly HandlingConfig _cfg;

    private float _leftHoldMs,  _leftArrMs;
    private float _rightHoldMs, _rightArrMs;

    public InputHandler(HandlingConfig cfg) => _cfg = cfg;

    public InputFrame ConsumeFrame(float deltaMs)
    {
        // Freeze both timers while both keys are held simultaneously — prevents the
        // opposite direction from silently charging its snap threshold during the overlap.
        float horizDelta = (Raylib.IsKeyDown(_cfg.KeyMoveLeft) && Raylib.IsKeyDown(_cfg.KeyMoveRight))
                           ? 0f : deltaMs;

        int leftSteps  = ComputeHoriz(_cfg.KeyMoveLeft,  ref _leftHoldMs,  ref _leftArrMs,  horizDelta);
        int rightSteps = ComputeHoriz(_cfg.KeyMoveRight, ref _rightHoldMs, ref _rightArrMs, horizDelta);

        return new InputFrame(
            leftSteps,
            rightSteps,
            SoftDropHeld: Raylib.IsKeyDown(_cfg.KeySoftDrop),
            HardDrop:     Raylib.IsKeyPressed(_cfg.KeyHardDrop),
            RotateCW:     Raylib.IsKeyPressed(_cfg.KeyRotateCW),
            RotateCCW:    Raylib.IsKeyPressed(_cfg.KeyRotateCCW),
            Rotate180:    Raylib.IsKeyPressed(_cfg.KeyRotate180),
            Hold:         Raylib.IsKeyPressed(_cfg.KeyHold)
        );
    }

    private int ComputeHoriz(KeyboardKey key, ref float holdMs, ref float arrMs, float deltaMs)
    {
        if (Raylib.IsKeyPressed(key))
        {
            holdMs = 0;
            arrMs  = 0;
            return 1;
        }
        if (!Raylib.IsKeyDown(key))
        {
            holdMs = 0;
            arrMs  = 0;
            return 0;
        }
        holdMs += deltaMs;
        if (holdMs >= 120f) return 10;
        if (holdMs < _cfg.DAS) return 0;

        if (_cfg.ARR == 0) return 10;

        arrMs += deltaMs;
        int steps = (int)(arrMs / _cfg.ARR);
        if (steps > 0) arrMs -= steps * _cfg.ARR;
        return steps;
    }
}
