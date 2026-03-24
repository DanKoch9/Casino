using FinalProjekt.Core;

DotNetEnv.Env.Load();
CasinoApp app = new CasinoApp();
await app.InitializeAsync();
app.Loop();