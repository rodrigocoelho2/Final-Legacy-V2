using FinalLegacy.Library.Combat;
using FinalLegacy.Library.Content;
using FinalLegacy.Library.Enums;
using FinalLegacy.Library.Models;
using FinalLegacy.Library.Story;
using FinalLegacy.Library.Text;
using static FinalLegacy.UnitTest.TestSupport;

namespace FinalLegacy.UnitTest;

/// <summary>The content files: rules, items, sprites, story and both languages fit together.</summary>
public class ContentTests
{
    [Fact]
    public void The_content_has_no_problems()
    {
        Assert.Empty(ContentValidator.Problems(Content));
    }

    [Theory]
    [InlineData("warrior", 4, 5, 2, 2)]
    [InlineData("mage", 3, 3, 1, 6)]
    [InlineData("assassin", 5, 1, 5, 2)]
    public void The_classes_keep_the_original_stats(string classId, int attack, int defense, int agility, int intelligence)
    {
        Assert.Equal(new Stats(attack, defense, agility, intelligence), Rules.Class(classId).BaseStats);
    }

    [Theory]
    [InlineData("warrior", "doransShield", 3, 7, 2, 2)]
    [InlineData("warrior", "arturiasSword", 6, 4, 2, 2)]
    [InlineData("mage", "doransRing", 2, 3, 1, 8)]
    [InlineData("mage", "largeStaff", 3, 3, 0, 8)]
    [InlineData("assassin", "doransKnife", 7, 1, 5, 1)]
    [InlineData("assassin", "shojinBlade", 7, 0, 5, 2)]
    public void The_original_weapon_choices_keep_their_bonuses(
        string classId, string show, int attack, int defense, int agility, int intelligence)
    {
        Assert.Equal(new Stats(attack, defense, agility, intelligence), Hero(classId, show).Stats);
    }

    [Fact]
    public void The_original_story_is_all_there()
    {
        var story = Content.Story;

        Assert.Equal("cave", story.Start);
        Assert.Equal("skeleton", story["skeleton-fight"].Battle!.EnemyId);
        Assert.Equal("shadow", story["shadow-fight"].Battle!.EnemyId);
        Assert.Equal("golem", story["golem-fight"].Battle!.EnemyId);
        Assert.True(story.Scenes.Values.Where(s => s.Kind == SceneKind.Battle).All(s => !s.Battle!.CanFlee));
        Assert.Equal(SceneKind.Shop, story["merchant"].Kind);
        Assert.Equal(SceneKind.End, story["chapter-end"].Kind);
    }

    [Fact]
    public void The_three_enemies_are_different()
    {
        // In the original the Mysterious Shadow and the Stone Golem had exactly the same stats.
        var shadow = Rules.Enemy("shadow");
        var golem = Rules.Enemy("golem");

        Assert.True(shadow.Agility > golem.Agility);
        Assert.True(golem.ArmorClass > shadow.ArmorClass);
        Assert.True(shadow.Aggression > golem.Aggression);
    }

    [Fact]
    public void There_is_gear_of_every_rarity_for_every_slot_to_find()
    {
        foreach (var rarity in Enum.GetValues<Rarity>())
            Assert.NotEmpty(Rules.OfRarity(rarity));

        foreach (var slot in Enum.GetValues<ItemSlot>())
            Assert.True(Rules.Items.Count(i => i.Slot == slot) >= 2, $"Only one item for {slot}.");

        Assert.True(Rules.Items.Count >= 40);
    }

    [Fact]
    public void Every_item_has_a_picture_and_every_enemy_too()
    {
        foreach (var item in Rules.Items) Assert.False(Content.Sprites.Draw(item.Sprite).IsEmpty, $"{item.Id} has an empty sprite.");
        foreach (var enemy in Rules.Enemies) Assert.False(Content.Sprites.Draw(enemy.Sprite).IsEmpty, $"{enemy.Id} has an empty sprite.");
    }

    [Theory]
    [InlineData(Language.Portuguese)]
    [InlineData(Language.English)]
    public void Every_text_only_uses_known_tags(Language language)
    {
        var texts = Content.Texts[language];
        foreach (var key in texts.Keys)
        {
            // Whatever is left in brackets after parsing is a typo in a tag, like [gld].
            var shown = Markup.Strip(texts[key]);
            Assert.False(shown.Contains('[') || shown.Contains(']'), $"{language} '{key}' has an unknown tag: {texts[key]}");
        }
    }

    [Theory]
    [InlineData(Language.Portuguese)]
    [InlineData(Language.English)]
    public void Battle_texts_fill_every_placeholder(Language language)
    {
        var narrator = new BattleNarrator(Content.Texts[language]);
        var hero = Hero("warrior", "arturiasSword");
        var potion = Rules.Item("potion");
        var hit = new D20Roll([14], RollMode.Normal, 5, 13);
        var crit = new D20Roll([20, 3], RollMode.Advantage, 5, 13);
        var miss = new D20Roll([2, 9], RollMode.Disadvantage, 5, 13);
        var fumble = new D20Roll([1], RollMode.Normal, 5, 13);
        var damage = new DamageRoll(new Dice(1, 8, 3), [5]);

        foreach (var enemy in Rules.Enemies)
        {
            foreach (var kind in Enum.GetValues<RoundEventKind>())
            {
                foreach (var roll in new[] { hit, crit, miss, fumble })
                {
                    var line = narrator.Describe(enemy, hero, new RoundEvent(kind, roll, roll.Success ? damage : null, potion));
                    Assert.DoesNotContain("{", line);
                    Assert.DoesNotContain("«", line);
                }
            }
        }
    }

    [Fact]
    public void Dice_are_shown_with_the_kept_one_highlighted()
    {
        var narrator = new BattleNarrator(Content.Texts[Language.Portuguese]);

        var text = Markup.Strip(narrator.RollText(new D20Roll([4, 15], RollMode.Advantage, 5, 13)));

        Assert.Equal("d20 4|15 (vantagem)+5 = 20", text);
    }

    [Fact]
    public void The_validator_catches_a_broken_story()
    {
        var story = GameContent.ParseStory("""
            {
              "start": "a",
              "scenes": {
                "a": { "text": "cave.awake", "choices": [ { "label": "common.yes", "goto": "nowhere" } ] },
                "b": { "battle": { "enemy": "dragon", "canFlee": true } },
                "c": { "text": "missing.text", "loot": { "items": [ "excalibur" ] }, "goto": "a" }
              }
            }
            """);

        var problems = ContentValidator.Problems(Content with { Story = story });

        Assert.Contains(problems, p => p.Contains("unknown scene 'nowhere'"));
        Assert.Contains(problems, p => p.Contains("unknown enemy 'dragon'"));
        Assert.Contains(problems, p => p.Contains("has no 'win' scene"));
        Assert.Contains(problems, p => p.Contains("has no 'flee' scene"));
        Assert.Contains(problems, p => p.Contains("'b' can never be reached"));
        Assert.Contains(problems, p => p.Contains("unknown item 'excalibur'"));
        Assert.Contains(problems, p => p.Contains("missing the text 'missing.text'"));
        Assert.Contains(problems, p => p.Contains("no end scene"));
    }

    [Theory]
    [InlineData("pt", Language.Portuguese)]
    [InlineData("EN", Language.English)]
    public void Languages_parse_from_their_code(string code, Language expected)
    {
        Assert.True(LanguageExtensions.TryParse(code, out var language));
        Assert.Equal(expected, language);
        Assert.False(LanguageExtensions.TryParse("fr", out _));
    }
}
