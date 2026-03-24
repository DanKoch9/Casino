using FinalProjekt.Core;

// Traverse up to find the .env file if running from bin/Debug/net10.0
DotNetEnv.Env.TraversePath().Load();
CasinoApp app = new CasinoApp();
await app.InitializeAsync();
app.Loop();