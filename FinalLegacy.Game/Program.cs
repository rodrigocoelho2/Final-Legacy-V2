using FinalLegacy.Game;
using FinalLegacy.Game.Terminal;
using FinalLegacy.Game.Ui;
using FinalLegacy.Library.Adventures;
using FinalLegacy.Library.Combat;
using FinalLegacy.Library.Content;

var options = GameOptions.Parse(args);
if (options.Help || options.Error is not null)
{
    if (options.Error is not null) Console.Error.WriteLine(options.Error + Environment.NewLine);
    Console.WriteLine(GameOptions.Usage);
    return options.Error is null ? 0 : 2;
}

var terminal = new GameTerminal(options.Color, animate: !options.Fast, charDelayMs: 18);
terminal.Start();

try
{
    var content = GameContent.Load();
    var random = new SystemRandomSource(options.Seed);
    var language = options.Language ?? TerminalUi.ChooseLanguage(terminal);
    var texts = content.Texts[language];
    var ui = new TerminalUi(terminal, texts);

    do
    {
        new Adventure(content, language, ui, random).Run();
    }
    while (ui.Choose(texts["ui.playAgain"], [texts["common.yes"], texts["common.no"]]) == 0);

    ui.Goodbye();
    return 0;
}
catch (EndOfInputException)
{
    // The input was closed (a piped script ran out, or Ctrl+D / Ctrl+Z): leave without a stack trace.
    Console.WriteLine();
    return 0;
}
finally
{
    terminal.Restore();
}
