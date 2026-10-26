using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using FinalLegacy.Library.Combat;
using FinalLegacy.Library.Enums;
using FinalLegacy.Library.Items;
using FinalLegacy.Library.Localization;
using FinalLegacy.Library.Models;
using FinalLegacy.Library.Sprites;
using FinalLegacy.Library.Story;

namespace FinalLegacy.Library.Content;

/// <summary>
/// Everything the game is made of: the rules (rules.json and items.json), the story, the pixel art and the texts of
/// every language.
/// </summary>
public sealed record GameContent(
    GameRules Rules,
    StoryGraph Story,
    SpriteBook Sprites,
    IReadOnlyDictionary<Language, Localizer> Texts)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>Loads the content embedded in the Library (Content/*.json).</summary>
    public static GameContent Load()
    {
        var rules = ParseRules(ReadEmbedded("rules.json"), ReadEmbedded("items.json"));
        var story = ParseStory(ReadEmbedded("story.json"));
        var sprites = ParseSprites(ReadEmbedded("sprites.json"));
        var texts = Enum.GetValues<Language>().ToDictionary(
            language => language,
            language => ParseTexts(language, ReadEmbedded($"{language.Code()}.json")));
        return new GameContent(rules, story, sprites, texts);
    }

    public static string ReadEmbedded(string fileName)
    {
        var name = $"FinalLegacy.Content.{fileName}";
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new FileNotFoundException($"The content file '{fileName}' is not embedded in the Library.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static GameRules ParseRules(string rulesJson, string itemsJson)
    {
        var dto = Deserialize<RulesDto>(rulesJson, "rules.json");
        var items = Deserialize<List<ItemDto>>(itemsJson, "items.json");
        var combat = dto.Combat ?? throw new InvalidDataException("rules.json has no 'combat' section.");

        return new GameRules(
            dto.NameMaxLength,
            dto.StartGold,
            dto.StartItems,
            new CombatSettings(combat.BaseArmorClass, combat.Proficiency, combat.MediumArmorAgilityCap, combat.FleeDifficulty),
            dto.Classes.Select(c => new HeroClass(
                c.Id, c.Color, c.Hair, c.Hp, c.Stats.ToStats(), c.Armor, c.Shields,
                c.Kits.Select(k => new Kit(k.Show, k.Items)).ToList())).ToList(),
            items.Select(i => new Item(
                i.Id,
                i.Slot,
                i.Rarity,
                i.Price,
                i.Modifiers.ToStats(),
                i.Weapon is { } w
                    ? new WeaponInfo(Dice.Parse(w.Damage), w.Type, w.Stat, w.TwoHanded, w.CritFrom, w.HitBonus)
                    : null,
                i.Armor is { } a ? new ArmorInfo(a.Ac, a.Weight) : null,
                i.Heal is { } heal ? new ConsumableInfo(Dice.Parse(heal)) : null,
                i.Sprite ?? i.Id)).ToList(),
            dto.Enemies.Select(e => new EnemyType(
                e.Id,
                e.Hp,
                e.Ac,
                e.Hit,
                Dice.Parse(e.Damage),
                e.Type,
                e.Agility,
                e.Aggression,
                e.Loot.ToTable(),
                e.Sprite ?? e.Id)).ToList());
    }

    public static StoryGraph ParseStory(string json)
    {
        var dto = Deserialize<StoryDto>(json, "story.json");
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
                pair.Value.End,
                pair.Value.Loot is { } l ? new LootSpec(l.Gold, l.Items, new LootTable(0, 0, [], l.Rolls, Weights(l.Rarity))) : null,
                pair.Value.Shop is { } s
                    ? new ShopSpec(s.Stock.Select(x => new ShopStock(x.Item, x.Count)).ToList(), s.Rest)
                    : null));
        return new StoryGraph(dto.Start, scenes);
    }

    public static SpriteBook ParseSprites(string json)
    {
        var dto = Deserialize<SpritesDto>(json, "sprites.json");
        return new SpriteBook(
            dto.Palette.ToDictionary(p => p.Key[0], p => Rgb.Parse(p.Value)),
            dto.Sprites.ToDictionary(
                s => s.Key,
                s => new Sprite(s.Value.ToDictionary(r => int.Parse(r.Key), r => r.Value))));
    }

    public static Localizer ParseTexts(Language language, string json) =>
        new(language, Deserialize<Dictionary<string, string>>(json, $"{language.Code()}.json"));

    private static IReadOnlyList<RarityWeight> Weights(Dictionary<string, int> weights) => weights
        .Select(w => new RarityWeight(
            w.Key.Equals("none", StringComparison.OrdinalIgnoreCase) ? null : Enum.Parse<Rarity>(w.Key, ignoreCase: true),
            w.Value))
        .ToList();

    private static T Deserialize<T>(string json, string file) =>
        JsonSerializer.Deserialize<T>(json, Json) ?? throw new InvalidDataException($"{file} is empty.");

    private sealed class RulesDto
    {
        public int NameMaxLength { get; set; } = 20;
        public int StartGold { get; set; }
        public List<string> StartItems { get; set; } = [];
        public CombatDto? Combat { get; set; }
        public List<ClassDto> Classes { get; set; } = [];
        public List<EnemyDto> Enemies { get; set; } = [];
    }

    private sealed class CombatDto
    {
        public int BaseArmorClass { get; set; } = 10;
        public int Proficiency { get; set; } = 2;
        public int MediumArmorAgilityCap { get; set; } = 2;
        public int FleeDifficulty { get; set; } = 12;
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
        public string Hair { get; set; } = "#4a2c17";
        public int Hp { get; set; }
        public StatsDto Stats { get; set; } = new();
        public ArmorWeight Armor { get; set; }
        public bool Shields { get; set; }
        public List<KitDto> Kits { get; set; } = [];
    }

    private sealed class KitDto
    {
        public string Show { get; set; } = "";
        public List<string> Items { get; set; } = [];
    }

    private sealed class ItemDto
    {
        public string Id { get; set; } = "";
        public ItemSlot Slot { get; set; }
        public Rarity Rarity { get; set; }
        public int Price { get; set; }
        public StatsDto Modifiers { get; set; } = new();
        public WeaponDto? Weapon { get; set; }
        public ArmorDto? Armor { get; set; }
        public string? Heal { get; set; }
        public string? Sprite { get; set; }
    }

    private sealed class WeaponDto
    {
        public string Damage { get; set; } = "1d4";
        public string Type { get; set; } = "bludgeoning";
        public AttackStat Stat { get; set; }
        public bool TwoHanded { get; set; }
        public int CritFrom { get; set; } = 20;
        public int HitBonus { get; set; }
    }

    private sealed class ArmorDto
    {
        public int Ac { get; set; }
        public ArmorWeight Weight { get; set; }
    }

    private sealed class EnemyDto
    {
        public string Id { get; set; } = "";
        public int Hp { get; set; }
        public int Ac { get; set; }
        public int Hit { get; set; }
        public string Damage { get; set; } = "1d4";
        public string Type { get; set; } = "bludgeoning";
        public int Agility { get; set; }
        public double Aggression { get; set; } = 0.5;
        public LootDto Loot { get; set; } = new();
        public string? Sprite { get; set; }
    }

    private sealed class LootDto
    {
        public List<int> Gold { get; set; } = [0];
        public List<DropDto> Drops { get; set; } = [];
        public int Rolls { get; set; }
        public Dictionary<string, int> Rarity { get; set; } = [];

        public LootTable ToTable() => new(
            Gold[0], Gold[^1], Drops.Select(d => new LootDrop(d.Item, d.Chance)).ToList(), Rolls, Weights(Rarity));
    }

    private sealed class DropDto
    {
        public string Item { get; set; } = "";
        public double Chance { get; set; } = 1;
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
        public SceneLootDto? Loot { get; set; }
        public ShopDto? Shop { get; set; }
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

    private sealed class SceneLootDto
    {
        public int Gold { get; set; }
        public List<string> Items { get; set; } = [];
        public int Rolls { get; set; }
        public Dictionary<string, int> Rarity { get; set; } = [];
    }

    private sealed class ShopDto
    {
        public List<StockDto> Stock { get; set; } = [];
        public int Rest { get; set; }
    }

    private sealed class StockDto
    {
        public string Item { get; set; } = "";
        public int? Count { get; set; }
    }

    private sealed class SpritesDto
    {
        public Dictionary<string, string> Palette { get; set; } = [];
        public Dictionary<string, Dictionary<string, string>> Sprites { get; set; } = [];
    }
}
