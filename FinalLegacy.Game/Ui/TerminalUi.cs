using FinalLegacy.Game.Terminal;
using System.Text;
using FinalLegacy.Library.Adventures;
using FinalLegacy.Library.Enums;
using FinalLegacy.Library.Localization;
using FinalLegacy.Library.Text;

namespace FinalLegacy.Game.Ui;

/// <summary>The game's screens in the console: the title, the menus, the story and the battles.</summary>
public sealed class TerminalUi : IGameUi
{
    private readonly GameTerminal _terminal;
    private readonly Localizer _texts;
    private readonly MenuPrompt _menu;
    private string _title = string.Empty;
    private bool _welcomePending;

    public TerminalUi(GameTerminal terminal, Localizer texts)
    {
        _terminal = terminal;
        _texts = texts;
        _menu = new MenuPrompt(terminal, new MenuTexts(
            texts["ui.menuHint"], texts["ui.menuHintLine"], texts["ui.invalidChoice"], texts["ui.invalidChoiceAgain"]));
    }

    /// <summary>The very first screen, before there is a language: both languages side by side.</summary>
    public static Language ChooseLanguage(GameTerminal terminal)
    {
        var menu = new MenuPrompt(terminal, new MenuTexts(
            "↑↓ escolher / choose · Enter",
            "Escreve o número / Type the number",
            "Escolha inválida! / Invalid choice!",
            "Escolha inválida! / Invalid choice!"));
        var languages = new[] { Language.Portuguese, Language.English };
        var picked = menu.Run(
            "\n  Escolhe a língua · Choose your language:\n\n",
            selected => Draw.Options(["Português", "English"], selected, vertical: true),
            languages.Length,
            before: Draw.TitleBox("Um RPG de texto · A text-based RPG"));
        return languages[picked];
    }

    /// <summary>The title box; the welcome line is typed under it on the class screen.</summary>
    public void ShowTitle()
    {
        _title = Draw.TitleBox(_texts["ui.tagline"]) + "\n";
        _welcomePending = true;
    }

    public int ChooseClass(IReadOnlyList<ClassOption> options)
    {
        var nameWidth = options.Max(o => o.Name.Length);
        var welcome = $"  {_texts["ui.welcome"]}\n";
        var before = _title;
        var header = (_welcomePending ? welcome : "") + $"\n  {_texts["ui.chooseClass"]}\n\n";
        if (_welcomePending) _title += welcome;
        _welcomePending = false;
        return _menu.Run(
            header,
            selected =>
            {
                var body = new StringBuilder();
                for (var i = 0; i < options.Count; i++)
                {
                    var o = options[i];
                    var marker = i == selected ? "[gold]›[/] [gold][[" + (i + 1) + "][/]" : "  [[" + (i + 1) + "]";
                    body.Append($" {marker} {Draw.Pad(Markup.Colorize(o.Name, o.Color), nameWidth)}   ");
                    body.Append(Draw.StatLine(o.Stats, Label));
                    if (selected < 0) body.Append($"\n       [dim]{o.Blurb}[/]");
                    body.Append('\n');
                }

                if (selected >= 0)
                {
                    var o = options[selected];
                    body.Append($"\n  {Markup.Colorize(o.Name, o.Color)} [dim]· {o.Blurb}[/]");
                }

                return body.ToString().TrimEnd('\n');
            },
            options.Count,
            before: before);
    }

    public int ChooseWeapon(string intro, IReadOnlyList<WeaponOption> options)
    {
        var nameWidth = options.Max(o => o.Name.Length);
        return _menu.Run(
            $"\n{Draw.Indent(intro)}\n\n",
            selected =>
            {
                var body = new StringBuilder();
                for (var i = 0; i < options.Count; i++)
                {
                    var o = options[i];
                    var marker = i == selected ? "[gold]›[/] [gold][[" + (i + 1) + "][/]" : "  [[" + (i + 1) + "]";
                    body.Append($" {marker} {Draw.Pad($"[cyan]{o.Name}[/]", nameWidth)}   {o.ModifiersText}");
                    if (selected < 0) body.Append($"\n       [dim]{Draw.StatLine(o.ResultingStats, Label, numbers: true)}[/]");
                    body.Append('\n');
                }

                if (selected >= 0)
                {
                    body.Append($"\n  {Draw.StatLine(options[selected].ResultingStats, Label, numbers: true)}");
                }

                return body.ToString().TrimEnd('\n');
            },
            options.Count,
            before: _title);
    }

    public string AskName(string prompt, string? error)
    {
        _terminal.Clear();
        _terminal.Write("\n");
        _terminal.Type($"{Draw.Indent(prompt)}\n\n");
        if (error is not null)
        {
            _terminal.WriteLine($"  [red]{error}[/]\n");
        }

        _terminal.Write("  [gold]›[/] ");
        return _terminal.ReadLine();
    }

    public void Narrate(string text)
    {
        _terminal.Clear();
        _terminal.Write("\n");
        _terminal.Type(Draw.Indent(text) + "\n");
        Continue();
    }

    public int Choose(string text, IReadOnlyList<string> options, string? note = null)
    {
        var story = "\n" + Draw.Indent(text) + "\n";
        Func<int, string> menu = selected => Draw.Options(options, selected, vertical: false);

        // After a reply ("Ahmnnn..."), the story is already on screen: only the reply is typed out.
        return note is null
            ? _menu.Run(story + "\n", menu, options.Count)
            : _menu.Run("\n" + Draw.Indent($"[dim]{note}[/]") + "\n\n", menu, options.Count, before: story);
    }

    public CombatAction ChooseAction(BattleScreen screen)
    {
        var actions = new[] { CombatAction.Attack, CombatAction.Defend, CombatAction.Flee };
        var labels = new[] { _texts["battle.attack"], _texts["battle.defend"], _texts["battle.flee"] };
        var hints = new[] { "", "", screen.CanFlee ? "" : _texts["battle.fleeLockedHint"] };
        var picked = _menu.Run(
            "\n",
            selected => Draw.Options(labels, selected, vertical: false, hints),
            actions.Length,
            before: BattleFrame(screen));
        return actions[picked];
    }

    public void ShowBattleEnd(BattleScreen screen, string result)
    {
        _terminal.Clear();
        _terminal.Write(BattleFrame(screen) + "\n");
        _terminal.Type(Draw.Indent(result) + "\n");
        Continue();
    }

    /// <summary>The goodbye line after the last game.</summary>
    public void Goodbye()
    {
        _terminal.Clear();
        _terminal.Write("\n");
        _terminal.Type($"  {_texts["ui.goodbye"]}\n\n");
    }

    /// <summary>The battle screen of the mock: who fights, both HP bars and what happened last round.</summary>
    private string BattleFrame(BattleScreen s)
    {
        var frame = new StringBuilder("\n");
        var fighting = _texts.Format("battle.fighting", ("enemy", s.EnemyName));
        var round = s.Round > 0 ? $" · {_texts.Format("battle.round", ("round", s.Round))}" : "";
        frame.Append($"  [dim]{fighting}{round}[/]\n\n");
        frame.Append($"  [amber]{s.HeroName}[/] [dim]·[/] {Markup.Colorize(s.ClassName, s.ClassColor)} [dim]·[/] [cyan]{s.WeaponName}[/]\n");
        frame.Append($"  {Draw.HpBar(_texts["battle.hp"], s.HeroHp, s.HeroMaxHp, "green")}\n\n");
        frame.Append($"  [red]{s.EnemyName}[/]\n");
        frame.Append($"  {Draw.HpBar(_texts["battle.hp"], s.EnemyHp, s.EnemyMaxHp, "red")}\n\n");
        frame.Append($"  {Draw.Rule}\n");
        foreach (var line in s.Log)
        {
            frame.Append(Draw.Indent(line)).Append('\n');
        }

        if (s.Log.Count == 0) frame.Append('\n');
        frame.Append($"  {Draw.Rule}\n");
        return frame.ToString();
    }

    private void Continue()
    {
        _terminal.Write($"\n  [dim]{_texts[_terminal.InputIsKeys ? "ui.continue" : "ui.continueLine"]}[/]");
        _terminal.WaitForContinue();
    }

    private string Label(string key) => _texts[key];
}
