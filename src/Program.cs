using Raylib_cs;
using Tetriorun;

// AppContext.BaseDirectory points to the temp extraction dir for single-file binaries;
// use the real executable's directory so data files sit next to the binary.
var exeDir     = Path.GetDirectoryName(Environment.ProcessPath) ?? AppContext.BaseDirectory;
var configPath = Path.Combine(exeDir, "handling.json");

var cfg      = Config.Load(configPath);
var renderer = new Renderer();
var input    = new InputHandler(cfg);

Raylib.InitWindow(Renderer.WindowW, Renderer.WindowH, "tetriorun");
Raylib.SetTargetFPS(60);

var   appState      = AppState.Countdown;
float countdownSecs = 5.0f;
var   game          = new GameState();

while (!Raylib.WindowShouldClose())
{
    float delta    = Raylib.GetFrameTime() * 1000f; // ms
    float deltaSec = Raylib.GetFrameTime();

    Raylib.BeginDrawing();

    switch (appState)
    {
        // ----------------------------------------------------------------
        case AppState.Countdown:
        {
            countdownSecs -= deltaSec;
            int num = (int)Math.Ceiling(countdownSecs); // 5,4,3,2,1
            if (countdownSecs <= 0)
            {
                game.SpawnNext();
                appState = AppState.Playing;
            }
            renderer.DrawCountdown(num, game);
            break;
        }

        // ----------------------------------------------------------------
        case AppState.Playing:
        {
            var frame = input.ConsumeFrame(delta);
            game.Tick(frame, delta);
            renderer.DrawGame(game);

            if (game.Phase == GamePhase.GameOver)
                appState = AppState.PostGame;
            break;
        }

        // ----------------------------------------------------------------
        case AppState.PostGame:
        {
            if (Raylib.GetKeyPressed() != 0)
            {
                Raylib.CloseWindow();
                return;
            }
            renderer.DrawGameOver(game);
            break;
        }
    }

    Raylib.EndDrawing();
}

Raylib.CloseWindow();
