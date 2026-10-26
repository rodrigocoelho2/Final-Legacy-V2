using FinalLegacy.Library.Localization;
using FinalLegacy.Library.Models;
using FinalLegacy.Library.Story;

namespace FinalLegacy.Library.Content;

/// <summary>
/// Checks the content files against each other, so a new chapter that points at a missing scene, enemy or text is
/// caught by the tests instead of in the middle of a game.
/// </summary>
public static class ContentValidator
{
    /// <summary>The texts the engine and the screens ask for directly, whatever the story.</summary>
    public static readonly IReadOnlyList<string> FixedKeys =
    [
        "language.name",
        "common.yes", "common.no",
        "ui.tagline", "ui.welcome", "ui.chooseClass", "ui.classSummary", "ui.askName", "ui.nameInvalid",
        "ui.nameRemembered", "ui.continue", "ui.continueLine", "ui.menuHint", "ui.menuHintLine", "ui.invalidChoice",
        "ui.invalidChoiceAgain", "ui.playAgain", "ui.goodbye",
        "stat.attack", "stat.defense", "stat.agility", "stat.intelligence",
        "battle.fighting", "battle.round", "battle.hp", "battle.attack", "battle.defend", "battle.flee",
        "battle.fleeLockedHint", "battle.heroHit", "battle.heroCrit", "battle.heroCounter", "battle.counterReady", "battle.magicPart", "battle.armorPart", "battle.enemyDodged",
        "battle.enemyDefended", "battle.enemyHit", "battle.heroBlocked", "battle.heroDodged", "battle.bothDefend",
        "battle.fleeLocked", "battle.fleeFailed", "battle.fled", "battle.won", "battle.lost",
        "game.over",
    ];

    /// <summary>Every text key the rules and the story need, besides <see cref="FixedKeys"/>.</summary>
    public static IEnumerable<string> RequiredKeys(GameRules rules, StoryGraph story)
    {
        foreach (var key in FixedKeys) yield return key;

        foreach (var heroClass in rules.Classes)
        {
            yield return $"class.{heroClass.Id}.name";
            yield return $"class.{heroClass.Id}.blurb";
            yield return $"class.{heroClass.Id}.weapons";
        }

        foreach (var weapon in rules.Weapons) yield return $"weapon.{weapon.Id}";

        foreach (var enemy in rules.Enemies)
        {
            yield return $"enemy.{enemy.Id}";
            yield return $"enemy.{enemy.Id}.the";
            yield return $"enemy.{enemy.Id}.of";
        }

        foreach (var scene in story.Scenes.Values)
        {
            if (scene.TextKey is not null) yield return scene.TextKey;
            foreach (var choice in scene.Choices)
            {
                yield return choice.LabelKey;
                if (choice.ReplyKey is not null) yield return choice.ReplyKey;
            }
        }
    }

    /// <summary>Everything wrong with the content, as readable lines (empty when all is well).</summary>
    public static IReadOnlyList<string> Problems(GameContent content)
    {
        var problems = new List<string>();
        var rules = content.Rules;
        var story = content.Story;

        foreach (var heroClass in rules.Classes)
        {
            if (heroClass.WeaponIds.Count == 0)
                problems.Add($"Class '{heroClass.Id}' has no weapons.");
            foreach (var weaponId in heroClass.WeaponIds.Where(id => !rules.HasWeapon(id)))
                problems.Add($"Class '{heroClass.Id}' lists an unknown weapon '{weaponId}'.");
        }

        foreach (var enemy in rules.Enemies)
        {
            if (enemy.MaxHp <= 0) problems.Add($"Enemy '{enemy.Id}' needs more than 0 HP.");
            if (enemy.Aggression is < 0 or > 1) problems.Add($"Enemy '{enemy.Id}' aggression must be between 0 and 1.");
        }

        if (!story.Scenes.ContainsKey(story.Start))
            problems.Add($"The story starts at an unknown scene '{story.Start}'.");

        foreach (var scene in story.Scenes.Values)
        {
            foreach (var exit in scene.Exits().Where(e => !story.Scenes.ContainsKey(e)))
                problems.Add($"Scene '{scene.Id}' leads to an unknown scene '{exit}'.");

            switch (scene.Kind)
            {
                case SceneKind.Battle:
                    if (!rules.HasEnemy(scene.Battle!.EnemyId))
                        problems.Add($"Scene '{scene.Id}' fights an unknown enemy '{scene.Battle.EnemyId}'.");
                    if (scene.Win is null)
                        problems.Add($"Battle scene '{scene.Id}' has no 'win' scene.");
                    if (scene.Battle.CanFlee && scene.Flee is null)
                        problems.Add($"Battle scene '{scene.Id}' lets the hero flee but has no 'flee' scene.");
                    break;
                case SceneKind.Choice:
                    if (scene.TextKey is null)
                        problems.Add($"Choice scene '{scene.Id}' has no text.");
                    if (!scene.Choices.Any(c => c.Goto is not null))
                        problems.Add($"Choice scene '{scene.Id}' has no choice that moves the story on.");
                    foreach (var choice in scene.Choices.Where(c => c.Goto is null && c.ReplyKey is null))
                        problems.Add($"Choice '{choice.LabelKey}' in scene '{scene.Id}' needs a 'goto' or a 'reply'.");
                    break;
                case SceneKind.Narration:
                    if (scene.TextKey is null) problems.Add($"Scene '{scene.Id}' has no text.");
                    if (scene.Goto is null) problems.Add($"Scene '{scene.Id}' has no 'goto' and is not an end.");
                    break;
                case SceneKind.End:
                    if (scene.TextKey is null) problems.Add($"End scene '{scene.Id}' has no text.");
                    break;
            }
        }

        foreach (var unreachable in story.Scenes.Keys.Except(Reachable(story)))
            problems.Add($"Scene '{unreachable}' can never be reached.");

        if (!story.Scenes.Values.Any(s => s.Kind == SceneKind.End))
            problems.Add("The story has no end scene.");

        var required = RequiredKeys(rules, story).Distinct().ToList();
        foreach (var texts in content.Texts.Values)
        {
            foreach (var key in required.Where(k => !texts.Has(k)))
                problems.Add($"{texts.Language} is missing the text '{key}'.");
        }

        var languages = content.Texts.Values.ToList();
        for (var i = 1; i < languages.Count; i++)
        {
            foreach (var key in languages[0].Keys.Except(languages[i].Keys))
                problems.Add($"{languages[i].Language} is missing the text '{key}' that {languages[0].Language} has.");
            foreach (var key in languages[i].Keys.Except(languages[0].Keys))
                problems.Add($"{languages[0].Language} is missing the text '{key}' that {languages[i].Language} has.");
        }

        return problems;
    }

    private static HashSet<string> Reachable(StoryGraph story)
    {
        var seen = new HashSet<string>();
        if (!story.Scenes.ContainsKey(story.Start)) return seen;

        var queue = new Queue<string>([story.Start]);
        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            if (!seen.Add(id) || !story.Scenes.TryGetValue(id, out var scene)) continue;
            foreach (var exit in scene.Exits()) queue.Enqueue(exit);
        }

        return seen;
    }
}
