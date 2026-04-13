using System.Text.Json;
using System.Text.Json.Serialization;
using Raylib_cs;

namespace Tetriorun;

public class HandlingConfig
{
    public int    ARR         { get; set; } = 0;
    public int    DAS         { get; set; } = 10;
    public string MoveLeft    { get; set; } = "Left";
    public string MoveRight   { get; set; } = "Right";
    public string SoftDrop    { get; set; } = "Down";
    public string HardDrop    { get; set; } = "Space";
    public string RotateCW    { get; set; } = "V";
    public string RotateCCW   { get; set; } = "X";
    public string Rotate180   { get; set; } = "C";
    public string Hold        { get; set; } = "Z";

    [JsonIgnore] public KeyboardKey KeyMoveLeft  { get; set; }
    [JsonIgnore] public KeyboardKey KeyMoveRight { get; set; }
    [JsonIgnore] public KeyboardKey KeySoftDrop  { get; set; }
    [JsonIgnore] public KeyboardKey KeyHardDrop  { get; set; }
    [JsonIgnore] public KeyboardKey KeyRotateCW  { get; set; }
    [JsonIgnore] public KeyboardKey KeyRotateCCW { get; set; }
    [JsonIgnore] public KeyboardKey KeyRotate180 { get; set; }
    [JsonIgnore] public KeyboardKey KeyHold      { get; set; }
}

public static class Config
{
    private static readonly JsonSerializerOptions _opts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public static HandlingConfig Load(string path)
    {
        HandlingConfig cfg;
        if (File.Exists(path))
        {
            try { cfg = JsonSerializer.Deserialize<HandlingConfig>(File.ReadAllText(path), _opts) ?? new(); }
            catch { cfg = new(); }
        }
        else
        {
            cfg = new();
            Save(path, cfg);
        }

        cfg.KeyMoveLeft  = Parse(cfg.MoveLeft,  KeyboardKey.Left);
        cfg.KeyMoveRight = Parse(cfg.MoveRight, KeyboardKey.Right);
        cfg.KeySoftDrop  = Parse(cfg.SoftDrop,  KeyboardKey.Down);
        cfg.KeyHardDrop  = Parse(cfg.HardDrop,  KeyboardKey.Space);
        cfg.KeyRotateCW  = Parse(cfg.RotateCW,  KeyboardKey.V);
        cfg.KeyRotateCCW = Parse(cfg.RotateCCW, KeyboardKey.X);
        cfg.KeyRotate180 = Parse(cfg.Rotate180, KeyboardKey.C);
        cfg.KeyHold      = Parse(cfg.Hold,      KeyboardKey.Z);
        return cfg;
    }

    public static void Save(string path, HandlingConfig cfg) =>
        File.WriteAllText(path, JsonSerializer.Serialize(cfg, _opts));

    private static KeyboardKey Parse(string name, KeyboardKey fallback) =>
        Enum.TryParse<KeyboardKey>(name, ignoreCase: true, out var k) ? k : fallback;
}
