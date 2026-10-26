using System.Reflection;
using System.Text.Json;
using FinalLegacy.Library.Enums;
using FinalLegacy.Library.Localization;
using FinalLegacy.Library.Models;
using FinalLegacy.Library.Story;

namespace FinalLegacy.Library.Content;

/// <summary>Everything the game is made of: the rules, the story and the texts of every language.</summary>
public sealed record GameContent(GameRules Rules, StoryGraph Story, IReadOnlyDictionary<Language, Localizer> Texts)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Loads the content embedded in the Library (Content/*.json).</summary>
    public static GameContent Load()
    {
        var rules = ParseRules(ReadEmbedded("rules.json"));
        var story = ParseStory(ReadEmbedded("story.json"));
        var texts = Enum.GetValues<Language>().ToDictionary(
            language => language,
            language => ParseTexts(language, ReadEmbedded($"{language.Code()}.json")));
        return new GameContent(rules, story, texts);
    }

    public static string ReadEmbedded(string fileName)
    {
        var name = $"FinalLegacy.Content.{fileName}";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new FileNotFoundException($"The content file '{fileName}' is not embedded in the Library.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static GameRules ParseRules(string json)
    {
        var dto = JsonSerializer.Deserialize<RulesDto>(json, Json) ?? throw new InvalidDataException("rules.json is empty.");
        var combat = dto.Combat ?? throw new InvalidDataException("rules.json has no 'combat' section.");
        return new GameRules(
            dto.HeroMaxHp,
            dto.NameMaxLength,
            new CombatSettings(
                Math.Max(1, combat.AttackMultiplier),
                combat.CritChancePerAgility,
                combat.DodgeChancePerAgility,
                combat.FleeBaseChance,
                combat.FleeChancePerAgility,
                Math.Max(1, combat.MagicDivisor),
                combat.MinimumHit),
            dto.Classes.Select(c => new HeroClass(c.Id, c.Color, c.Stats.ToStats(), c.Weapons)).ToList(),
            dto.Weapons.Select(w => new Weapon(w.Id, w.Modifiers.ToStats())).ToList(),
            dto.Enemies.Select(e => new EnemyType(e.Id, e.Hp, e.Stats.ToStats(), e.Aggression)).ToList());
    }

    public static StoryGraph ParseStory(string json)
    {
        var dto = JsonSerializer.Deserialize<StoryDto>(json, Json) ?? throw new InvalidDataException("story.json is empty.");
        var scenes = dto.Scenes.ToDictionary(
            pair => pair.Key,
            pair => new Scene(
                pair.Key,
                pair.Value.Text,
                pair.Value.Choices.Select(c => new StoryChoice(c.Label, c.Goto, c.Reply)).ToList(),
                pair.Value.Goto,
                pair.Value.Battle is { } b ? new BattleSpec(b.Enemy, b.CanFlee) : null,
                pair.Value.Win,
                pair.Value.Flee,
                pair.Value.End));
        return new StoryGraph(dto.Start, scenes);
    }

    public static Localizer ParseTexts(Language language, string json)
    {
        var texts = JsonSerializer.Deserialize<Dictionary<string, string>>(json, Json)
            ?? throw new InvalidDataException($"{language.Code()}.json is empty.");
        return new Localizer(language, texts);
    }

    private sealed class RulesDto
    {
        public int HeroMaxHp { get; set; } = 100;
        public int NameMaxLength { get; set; } = 20;
        public CombatDto? Combat { get; set; }
        public List<ClassDto> Classes { get; set; } = [];
        public List<WeaponDto> Weapons { get; set; } = [];
        public List<EnemyDto> Enemies { get; set; } = [];
    }

    private sealed class CombatDto
    {
        public int AttackMultiplier { get; set; } = 1;
        public double CritChancePerAgility { get; set; }
        public double DodgeChancePerAgility { get; set; }
        public double FleeBaseChance { get; set; }
        public double FleeChancePerAgility { get; set; }
        public int MagicDivisor { get; set; } = 2;
        public int MinimumHit { get; set; } = 1;
    }

    private sealed class StatsDto
    {
        public int Attack { get; set; }
        public int Defense { get; set; }
        public int Agility { get; set; }
        public int Intelligence { get; set; }

        public Stats ToStats() => new(Attack, Defense, Agility, Intelligence);
    }

    private sealed class ClassDto
    {
        public string Id { get; set; } = "";
        public string Color { get; set; } = "";
        public StatsDto Stats { get; set; } = new();
        public List<string> Weapons { get; set; } = [];
    }

    private sealed class WeaponDto
    {
        public string Id { get; set; } = "";
        public StatsDto Modifiers { get; set; } = new();
    }

    private sealed class EnemyDto
    {
        public string Id { get; set; } = "";
        public int Hp { get; set; }
        public StatsDto Stats { get; set; } = new();
        public double Aggression { get; set; } = 0.5;
    }

    private sealed class StoryDto
    {
        public string Start { get; set; } = "";
        public Dictionary<string, SceneDto> Scenes { get; set; } = [];
    }

    private sealed class SceneDto
    {
        public string? Text { get; set; }
        public List<ChoiceDto> Choices { get; set; } = [];
        public string? Goto { get; set; }
        public BattleDto? Battle { get; set; }
        public string? Win { get; set; }
        public string? Flee { get; set; }
        public bool End { get; set; }
    }

    private sealed class ChoiceDto
    {
        public string Label { get; set; } = "";
        public string? Goto { get; set; }
        public string? Reply { get; set; }
    }

    private sealed class BattleDto
    {
        public string Enemy { get; set; } = "";
        public bool CanFlee { get; set; }
    }
}
