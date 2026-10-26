using FinalLegacy.Library.Combat;
using FinalLegacy.Library.Content;
using FinalLegacy.Library.Enums;
using FinalLegacy.Library.Models;
using FinalLegacy.Library.Story;
using FinalLegacy.Library.Text;
using static FinalLegacy.UnitTest.TestSupport;

namespace FinalLegacy.UnitTest;

/// <summary>The content files: rules, story and both languages fit together.</summary>
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
    public void The_weapons_keep_the_original_bonuses(
        string classId, string weaponId, int attack, int defense, int agility, int intelligence)
    {
        Assert.Contains(weaponId, Rules.Class(classId).WeaponIds);
        Assert.Equal(new Stats(attack, defense, agility, intelligence), Hero(classId, weaponId).Stats);
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
        Assert.Equal(SceneKind.End, story["chapter-end"].Kind);
    }

    [Fact]
    public void The_three_enemies_are_different()
    {
        // In the original the Mysterious Shadow and the Stone Golem had exactly the same stats.
        var shadow = Rules.Enemy("shadow");
        var golem = Rules.Enemy("golem");

        Assert.NotEqual(shadow.Stats, golem.Stats);
        Assert.True(shadow.Stats.Agility > golem.Stats.Agility);
        Assert.True(golem.Stats.Defense > shadow.Stats.Defense);
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
        foreach (var kind in Enum.GetValues<RoundEventKind>())
        {
            foreach (var enemy in Rules.Enemies)
            {
                var line = narrator.Describe(enemy, new RoundEvent(kind, Damage: 7, Blocked: 3, Magic: 2));
                Assert.DoesNotContain("{", line);
                Assert.DoesNotContain("«", line);
            }
        }
    }

    [Fact]
    public void Enemy_names_start_sentences_in_capitals()
    {
        var line = new BattleNarrator(Content.Texts[Language.Portuguese])
            .Describe(Rules.Enemy("shadow"), new RoundEvent(RoundEventKind.EnemyHit, Damage: 10));

        Assert.StartsWith("A Sombra Misteriosa atacou", Markup.Strip(line));
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
                "c": { "text": "missing.text", "end": true }
              }
            }
            """);

        var problems = ContentValidator.Problems(Content with { Story = story });

        Assert.Contains(problems, p => p.Contains("unknown scene 'nowhere'"));
        Assert.Contains(problems, p => p.Contains("unknown enemy 'dragon'"));
        Assert.Contains(problems, p => p.Contains("has no 'win' scene"));
        Assert.Contains(problems, p => p.Contains("has no 'flee' scene"));
        Assert.Contains(problems, p => p.Contains("'b' can never be reached"));
        Assert.Contains(problems, p => p.Contains("missing the text 'missing.text'"));
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
