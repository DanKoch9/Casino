using Spectre.Console;
using FinalProjekt.Core;
using Spectre.Console.Rendering;

namespace FinalProjekt.UI;

public class RouletteRenderer : IRenderer
{
    public void PlayAnim(params object[] args)
    {
        int target = (int)args[0];

        int[] wheel = { 
            0, 32, 15, 19, 4, 21, 2, 25, 17, 34, 
            6, 27, 13, 36, 11, 30, 8, 23, 10, 5, 
            24, 16, 33, 1, 20, 14, 31, 9, 22, 18, 
            29, 7, 28, 12, 35, 3, 26 
        };

        int targetIndex = Array.IndexOf(wheel, target);
        
        int slowdownSteps = 30;
        int fastSteps = (wheel.Length * 2) + Random.Shared.Next(0, wheel.Length);
        int totalSteps = fastSteps + slowdownSteps;
        
        int startPos = (targetIndex - totalSteps) % wheel.Length;
        if (startPos < 0) startPos += wheel.Length;

        AnsiConsole.Live(new Text("Spinning..."))
            .Start(ctx =>
            {
                int baseDelayMs = 50;

                for (int i = 0; i <= totalSteps; i++)
                {
                    int currentWheelIndex = (startPos + i) % wheel.Length;

                    var table = new Table().Centered().Border(TableBorder.Rounded).HideHeaders();
                    for (int c = 0; c < 10; c++) table.AddColumn(new TableColumn("").Centered());

                    IRenderable[,] cells = new IRenderable[10, 10];
                    for (int r = 0; r < 10; r++)
                        for (int c = 0; c < 10; c++)
                            cells[r, c] = new Text("  ");

                    for (int w = 0; w < wheel.Length; w++)
                    {
                        var (row, col) = GetGridPos(w);
                        bool isBall = w == currentWheelIndex;
                        string numStr = $" {wheel[w].ToString().PadLeft(2)} ";
                        
                        if (isBall)
                        {
                            cells[row, col] = new Text(numStr, new Style(Color.Black, Color.White, Decoration.Bold));
                        }
                        else
                        {
                            cells[row, col] = new Text(numStr, new Style(GetColor(wheel[w]), null, Decoration.Dim));
                        }
                    }

                    for (int r = 0; r < 10; r++)
                    {
                        var rowItems = new List<IRenderable>();
                        for (int c = 0; c < 10; c++) rowItems.Add(cells[r, c]);
                        table.AddRow(rowItems.ToArray());
                    }

                    ctx.UpdateTarget(table);
                    ctx.Refresh();
                    
                    if (i > fastSteps)
                    {
                        float progress = (float)(i - fastSteps) / slowdownSteps;
                        int extraDelay = (int)(Math.Pow(progress, 2.5) * 800); 
                        Thread.Sleep(baseDelayMs + extraDelay);
                    }
                    else
                    {
                        Thread.Sleep(baseDelayMs);
                    }
                }
            });
    }

    private (int row, int col) GetGridPos(int index)
    {
        if (index < 10) return (0, index);
        if (index < 19) return (index - 9, 9);
        if (index < 28) return (9, 9 - (index - 18));
        return (9 - (index - 27), 0);
    }

    private Color GetColor(int num)
    {
        if (num == 0) return Color.Green;
        int[] redNumbers = { 1, 3, 5, 7, 9, 12, 14, 16, 18, 19, 21, 23, 25, 27, 30, 32, 34, 36 };
        return redNumbers.Contains(num) ? Color.Red : Color.Grey;
    }
}
