using Spectre.Console;

namespace FinalProjekt.UI;

public class SlotRenderer
{
    public void AnimateSpin(int target1, int target2, int target3)
    {
        Table table = new Table().Centered().Border(TableBorder.Rounded).HideHeaders();
        table.AddColumn(new TableColumn("").Centered());
        table.AddColumn(new TableColumn("").Centered());
        table.AddColumn(new TableColumn("").Centered());

        table.AddRow(new Markup("[dim]?[/]"), new Markup("[dim]?[/]"), new Markup("[dim]?[/]"));

        AnsiConsole.Live(table)
            .Cropping(VerticalOverflowCropping.Bottom)
            .Start(ctx =>
            {
                int totalFrames = 30;
                int baseDelayMs = 40;

                for (int frame = 0; frame <= totalFrames; frame++)
                {
                    string r1 = Random.Shared.Next(1, 7).ToString();
                    string r2 = Random.Shared.Next(1, 7).ToString();
                    string r3 = Random.Shared.Next(1, 7).ToString();

                    bool lock1 = frame >= (totalFrames / 3);
                    bool lock2 = frame >= (totalFrames / 3) * 2;
                    bool lock3 = frame == totalFrames;

                    string display1 = lock1 ? $"[green]{target1}[/]" : $"[red]{r1}[/]";
                    string display2 = lock2 ? $"[green]{target2}[/]" : $"[red]{r2}[/]";
                    string display3 = lock3 ? $"[green]{target3}[/]" : $"[red]{r3}[/]";

                    table.UpdateCell(0, 0, new Markup(display1));
                    table.UpdateCell(0, 1, new Markup(display2));
                    table.UpdateCell(0, 2, new Markup(display3));

                    ctx.Refresh();

                    int currentDelay = baseDelayMs + (int)(Math.Pow(frame, 1.5) / 2);
                    Thread.Sleep(currentDelay);
                }
            });
    }
}