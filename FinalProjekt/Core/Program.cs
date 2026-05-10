namespace FinalProjekt.Core;

using System.Threading.Tasks;
using FinalProjekt.UI;
using DotNetEnv;

class Program
{
    static async Task Main() 
    {
        DotNetEnv.Env.TraversePath().Load();
        CasinoApp app = new CasinoApp();
        await app.Initialize();
        app.Loop(); 
    }
}