using System.Text;
using FinalLegacy.Game.Terminal;
using FinalLegacy.Library.Adventures;
using FinalLegacy.Library.Enums;
using FinalLegacy.Library.Localization;
using FinalLegacy.Library.Models;
using FinalLegacy.Library.Sprites;
using FinalLegacy.Library.Text;
using static FinalLegacy.Game.Ui.Draw;

namespace FinalLegacy.Game.Ui;

/// <summary>
/// The game's screens in the console: the title, the menus, the story, the battles side by side, the inventory with
/// the hero trying things on, the merchant and the loot. Pixel art is drawn when the terminal has colours; otherwise
/// every screen still works as text.
/// </summary>
public sealed class TerminalUi : IGameUi
{
    /// <summary>The widest a screen gets (fits an 80-column window).</summary>
    private const int ScreenWidth = 78;

    private const int ListRows = 13;

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

    private ArtMode Art => _terminal.Art;

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
            selected => Options(["Português", "English"], selected, vertical: true),
            languages.Length,
            before: TitleBox("Um RPG de texto · A text-based RPG"));
        return languages[picked];
    }

    /// <summary>The title box; the welcome line is typed under it on the class screen.</summary>
    public void ShowTitle()
    {
        _title = TitleBox(_texts["ui.tagline"]) + "\n";
        _welcomePending = true;
    }

    // ── Creating the hero ──────────────────────────────────────────────────────────────────────────────────────

    public int ChooseClass(IReadOnlyList<ClassOption> options)
    {
        var welcome = $"  {_texts["ui.welcome"]}\n";
        var before = _title;
        var header = (_welcomePending ? welcome : "") + $"\n  {_texts["ui.chooseClass"]}\n\n";
        if (_welcomePending) _title += welcome;
        _welcomePending = false;
        var nameWidth = options.Max(o => o.Name.Length);

        return _menu.Run(header, selected =>
        {
            var list = options.Select((o, i) =>
                $"{Marker(i, selected)} {Pad(Markup.Colorize(o.Name, o.Color), nameWidth)}   [dim]{_texts["card.hp"]}[/] {o.MaxHp}").ToList();

            if (selected < 0 || Art == ArtMode.None)
            {
                var text = new List<string>();
                foreach (var (o, i) in options.Select((o, i) => (o, i)))
                {
                    text.Add(list[i]);
                    text.Add($"      [dim]{o.Blurb} · {o.Training}[/]");
                    text.Add($"      {StatLine(o.Stats, Label)}");
                }

                return Columns([], 0, text);
            }

            var chosen = options[selected];
            list.Add("");
            list.AddRange(WrapText($"{Markup.Colorize(chosen.Name, chosen.Color)} [dim]·[/] {chosen.Blurb}", 55));
            list.Add($"[dim]{chosen.Training}[/]");
            list.Add("");
            list.AddRange(StatPairs(chosen.Stats));
            return Columns(SpriteCells(PixelArt.Lines(chosen.Look, Art)), 16, list);
        }, options.Count, before: before);
    }

    public int ChooseKit(string intro, IReadOnlyList<KitOption> options)
    {
        var nameWidth = options.Max(o => Markup.Strip(o.Name).Length);
        return _menu.Run($"\n{Indent(intro)}\n\n", selected =>
        {
            var list = options.Select((o, i) => $"{Marker(i, selected)} {Pad(o.Name, nameWidth)}   {o.ModifiersText}").ToList();
            if (selected < 0 || Art == ArtMode.None)
            {
                var text = new List<string>();
                foreach (var (o, i) in options.Select((o, i) => (o, i)))
                {
                    text.Add(list[i]);
                    text.Add($"      [dim]{Markup.Strip(o.ItemsText)}[/]");
                    text.Add($"      {Summary(o.Preview)}");
                }

                return Columns([], 0, text);
            }

            var chosen = options[selected];
            list.Add("");
            list.AddRange(WrapText(chosen.ItemsText, 55));
            list.Add("");
            list.Add(Summary(chosen.Preview));
            list.AddRange(StatPairs(chosen.Preview.Stats));
            return Columns(SpriteCells(PixelArt.Lines(chosen.Preview.Look, Art)), 16, list);
        }, options.Count, before: _title);
    }

    public string AskName(string prompt, string? error)
    {
        _terminal.Clear();
        _terminal.Write("\n");
        _terminal.Type($"{Indent(prompt)}\n\n");
        if (error is not null) _terminal.WriteLine($"  [red]{error}[/]\n");
        _terminal.Write("  [gold]›[/] ");
        return _terminal.ReadLine();
    }

    // ── The story ──────────────────────────────────────────────────────────────────────────────────────────────

    public void Narrate(string text, HeroCard? portrait = null)
    {
        _terminal.Clear();
        _terminal.Write("\n");
        _terminal.Type(Indent(text) + "\n");
        if (portrait is not null)
        {
            _terminal.Write("\n" + Columns(SpriteCells(PixelArt.Lines(portrait.Look, Art)), 16, CardLines(portrait, null)) + "\n");
        }

        Continue();
    }

    public int Choose(string text, IReadOnlyList<string> options, string? note = null)
    {
        var story = "\n" + Indent(text) + "\n";
        Func<int, string> menu = selected => Options(options, selected, vertical: false, separator: "   ");

        // After a reply ("Ahmnnn..."), the story is already on screen: only the reply is typed out.
        return note is null
            ? _menu.Run(story + "\n", menu, options.Count)
            : _menu.Run("\n" + Indent($"[dim]{note}[/]") + "\n\n", menu, options.Count, before: story);
    }

    // ── Battles ────────────────────────────────────────────────────────────────────────────────────────────────

    public int ChooseAction(BattleScreen screen) => _menu.Run(
        "\n",
        selected => Options(
            screen.Actions.Select(a => a.Label).ToList(),
            selected,
            vertical: false,
            screen.Actions.Select(a => a.Hint ?? "").ToList(),
            separator: "   "),
        screen.Actions.Count,
        before: BattleFrame(screen));

    public void ShowBattleEnd(BattleScreen screen, string result)
    {
        _terminal.Clear();
        _terminal.Write(BattleFrame(screen) + "\n");
        _terminal.Type(Indent(result) + "\n");
        Continue();
    }

    /// <summary>The hero on the left and the enemy on the right, each with HP and CA under them, then the dice.</summary>
    private string BattleFrame(BattleScreen s)
    {
        var frame = new StringBuilder("\n");
        var round = s.Round > 0 ? $" · {_texts.Format("battle.round", ("round", s.Round))}" : "";
        frame.Append($"  [dim]{_texts.Format("battle.fighting", ("enemy", s.EnemyName))}{round}[/]\n\n");

        const int column = 36;
        var left = new List<Cell>();
        var right = new List<string>();
        var heroArt = PixelArt.Lines(s.Hero.Look, Art);
        var enemyArt = PixelArt.Lines(s.EnemyLook, Art);
        for (var i = 0; i < Math.Max(heroArt.Count, enemyArt.Count); i++)
        {
            left.Add(i < heroArt.Count ? new Cell(new string(' ', 8) + heroArt[i], 24) : new Cell("", 0));
            right.Add(i < enemyArt.Count ? new string(' ', 5) + enemyArt[i] : "");
        }

        if (heroArt.Count > 0)
        {
            left.Add(new Cell("", 0));
            right.Add("");
        }

        var heroInfo = new List<string>
        {
            $"[amber]{s.Hero.Name}[/] [dim]·[/] {Markup.Colorize(s.Hero.ClassName, s.Hero.ClassColor)}",
            HpBar(_texts["battle.hp"], s.Hero.Hp, s.Hero.MaxHp, "green", 16),
            $"[dim]{_texts["card.ac"]}[/] {s.Hero.ArmorClass}  [dim]{_texts["card.attack"]}[/] +{s.Hero.AttackBonus}  [dim]{s.Hero.DamageText}[/]",
        };
        if (s.Opening) heroInfo.Add($"[green]{_texts["battle.opening"]}[/]");

        var enemyInfo = new List<string>
        {
            $"[red]{s.EnemyName}[/]",
            HpBar(_texts["battle.hp"], s.EnemyHp, s.EnemyMaxHp, "red", 16),
            $"[dim]{_texts["card.ac"]}[/] {s.EnemyArmorClass}",
        };

        for (var i = 0; i < Math.Max(heroInfo.Count, enemyInfo.Count); i++)
        {
            left.Add(Cell.Of(i < heroInfo.Count ? heroInfo[i] : ""));
            right.Add(i < enemyInfo.Count ? enemyInfo[i] : "");
        }

        frame.Append(Columns(left, column, right, indent: 2, gap: 2)).Append("\n\n");
        frame.Append($"  {Rule}\n");
        foreach (var line in s.Log) frame.Append(Indent(line)).Append('\n');
        if (s.Log.Count == 0) frame.Append('\n');
        frame.Append($"  {Rule}\n");
        return frame.ToString();
    }

    // ── Loot, inventory and the merchant ───────────────────────────────────────────────────────────────────────

    public int ShowLoot(LootScreen screen)
    {
        var found = new List<string>();
        foreach (var item in screen.Items)
        {
            found.Add("");
            found.Add(ItemBlock(item, showCount: true));
        }

        if (screen.Gold > 0) found.Add($"\n  [gold]{_texts.Format("loot.gold", ("gold", screen.Gold))}[/]");
        if (screen.Items.Count == 0 && screen.Gold == 0) found.Add($"\n  [dim]{_texts["loot.nothing"]}[/]");
        var block = string.Join("\n", found);

        return _menu.Run(
            "\n" + Indent(screen.Text) + "\n",
            selected => block + "\n\n" + Options(screen.Options, selected, vertical: false, separator: "   "),
            screen.Options.Count);
    }

    public int Gear(GearScreen screen)
    {
        var header = $"\n  [gold]{screen.Title.ToUpperInvariant()}[/]\n";
        if (screen.Intro is not null) header += "\n" + Indent(screen.Intro) + "\n";
        header += "\n";

        return _menu.Run(header, selected =>
        {
            var entry = selected >= 0 ? screen.Entries[selected] : null;
            var hero = screen.Hero;
            var preview = entry?.Preview;

            // Left: the hero (trying the highlighted item on) and their numbers.
            var left = SpriteCells(PixelArt.Lines((preview ?? hero).Look, Art)).ToList();
            if (left.Count > 0) left.Add(new Cell("", 0));
            left.AddRange(StatColumn(hero, preview).Select(Cell.Of));

            // Right: the list, then the highlighted item.
            var right = ListLines(screen.Entries, selected);
            if (entry?.Item is { } item && Art != ArtMode.None)
            {
                right.Add("");
                right.AddRange(ItemBlock(item, showCount: false, width: 36).Split('\n').Select(l => l.Length >= 2 ? l[2..] : l));
            }

            var body = Columns(left, 18, right);
            if (screen.Message is not null) body += $"\n\n  [gold]»[/] {screen.Message}";
            return body;
        }, screen.Entries.Count);
    }

    private List<string> ListLines(IReadOnlyList<GearEntry> entries, int selected)
    {
        var lines = new List<string>();
        var selectedLine = 0;
        string? section = null;
        for (var i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e.Section != section && e.Section.Length > 0)
            {
                if (section is not null) lines.Add("");
                lines.Add($"[dim]{e.Section.ToUpperInvariant()}[/]");
            }
            else if (e.Section.Length == 0 && section is { Length: > 0 })
            {
                lines.Add("");
            }

            section = e.Section;
            if (i == selected) selectedLine = lines.Count;

            var label = e.Enabled ? e.Label : $"[dim]{Markup.Strip(e.Label)}[/]";
            var rightText = e.Right is null ? "" : e.Enabled ? e.Right : $"[dim]{Markup.Strip(e.Right)}[/]";
            var hint = e.Hint is null ? "" : $" [dim]· {e.Hint}[/]";
            var line = e.Right is null ? $"{Marker(i, selected)} {label}{hint}" : $"{Marker(i, selected)} {Pad(label, 17)} {rightText}{hint}";
            lines.Add(line);
        }

        // Long bags scroll with the highlighted line.
        if (lines.Count <= ListRows) return lines;
        var start = Math.Clamp(selectedLine - ListRows / 2, 0, lines.Count - ListRows);
        var window = lines.Skip(start).Take(ListRows).ToList();
        if (start > 0) window[0] = "[dim]  ↑[/]";
        if (start + ListRows < lines.Count) window[^1] = "[dim]  ↓[/]";
        return window;
    }

    /// <summary>An item with its picture on the left: name, rarity and slot, what it does, its description.</summary>
    private string ItemBlock(ItemCard item, bool showCount, int width = 55)
    {
        var count = showCount && item.Count > 1 ? $" ×{item.Count}" : "";
        var text = new List<string>
        {
            $"{item.Name}{count}",
            $"[dim]{item.TypeLine}[/]",
        };
        foreach (var effect in item.Effects) text.AddRange(WrapText(effect, width));
        text.AddRange(WrapText(item.Description, width).Select(line => $"[dim]{line}[/]"));
        return Columns(SpriteCells(PixelArt.Cropped(item.Icon, Art)), 16, text);
    }

    /// <summary>The numbers under the hero in the inventory, with what the highlighted choice would change.</summary>
    private IEnumerable<string> StatColumn(HeroCard hero, HeroCard? preview)
    {
        yield return $"[amber]{hero.Name}[/]";
        yield return $"{_texts["card.hp"]} {hero.Hp}/{hero.MaxHp}";
        yield return $"{_texts["card.ac"]} {hero.ArmorClass}{Change(hero.ArmorClass, preview?.ArmorClass)}";
        yield return $"{_texts["card.attack"]} +{hero.AttackBonus}{Change(hero.AttackBonus, preview?.AttackBonus)}";
        var damage = preview is not null && preview.DamageText != hero.DamageText
            ? $"[gold]{preview.DamageText}[/]"
            : hero.DamageText;
        yield return damage;
        var stats = preview?.Stats ?? hero.Stats;
        yield return $"{Stat("stat.attack", stats.Attack, hero.Stats.Attack)} {Stat("stat.defense", stats.Defense, hero.Stats.Defense)}";
        yield return $"{Stat("stat.agility", stats.Agility, hero.Stats.Agility)} {Stat("stat.intelligence", stats.Intelligence, hero.Stats.Intelligence)}";
        yield return $"[gold]{hero.Gold}[/] {_texts["card.gold"]}";
        if (!(preview ?? hero).Proficient) yield return $"[red]{_texts["card.untrainedShort"]}[/]";
    }

    private string Stat(string key, int value, int now)
    {
        var color = value > now ? "green" : value < now ? "red" : null;
        return color is null ? $"[dim]{_texts[key]}[/] {value}" : $"[dim]{_texts[key]}[/] [{color}]{value}[/]";
    }

    private static string Change(int now, int? then) => then is { } t && t != now
        ? $" → [{(t > now ? "green" : "red")}]{t}[/]"
        : "";

    // ── Pieces ─────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The hero's card next to their portrait.</summary>
    private List<string> CardLines(HeroCard hero, string? extra)
    {
        var lines = new List<string>
        {
            "",
            $"[amber]{hero.Name}[/] [dim]·[/] {Markup.Colorize(hero.ClassName, hero.ClassColor)}".Replace("[amber][/] [dim]·[/] ", ""),
            HpBar(_texts["card.hp"], hero.Hp, hero.MaxHp, "green", 16),
            Summary(hero),
        };
        lines.AddRange(StatPairs(hero.Stats));
        lines.Add($"[gold]{hero.Gold}[/] {_texts["card.gold"]}");
        if (!hero.Proficient) lines.Add($"[red]{_texts["card.untrained"]}[/]");
        if (extra is not null) lines.Add(extra);
        return lines;
    }

    private string Summary(HeroCard hero) =>
        $"[dim]{_texts["card.ac"]}[/] {hero.ArmorClass}  [dim]{_texts["card.attack"]}[/] +{hero.AttackBonus}  [dim]{_texts["card.damage"]}[/] {hero.DamageText}";

    private IEnumerable<string> StatPairs(Stats stats)
    {
        var entries = stats.Entries();
        yield return $"{_texts[entries[0].Key]} {StatBar(entries[0].Value)} {entries[0].Value}   {_texts[entries[1].Key]} {StatBar(entries[1].Value)} {entries[1].Value}";
        yield return $"{_texts[entries[2].Key]} {StatBar(entries[2].Value)} {entries[2].Value}   {_texts[entries[3].Key]} {StatBar(entries[3].Value)} {entries[3].Value}";
    }

    private static string Marker(int index, int selected) =>
        index == selected ? $"[gold]›[/] [gold][[{index + 1}][/]" : $"  [[{index + 1}]";

    /// <summary>The goodbye line after the last game.</summary>
    public void Goodbye()
    {
        _terminal.Clear();
        _terminal.Write("\n");
        _terminal.Type($"  {_texts["ui.goodbye"]}\n\n");
    }

    private void Continue()
    {
        _terminal.Write($"\n  [dim]{_texts[_terminal.InputIsKeys ? "ui.continue" : "ui.continueLine"]}[/]");
        _terminal.WaitForContinue();
    }

    private string Label(string key) => _texts[key];
}
