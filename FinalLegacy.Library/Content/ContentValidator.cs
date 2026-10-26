using FinalLegacy.Library.Combat;
using FinalLegacy.Library.Enums;
using FinalLegacy.Library.Models;
using FinalLegacy.Library.Story;

namespace FinalLegacy.Library.Content;

/// <summary>
/// Checks the content files against each other, so a new chapter, item or sprite that points at something missing is
/// caught by the tests instead of in the middle of a game.
/// </summary>
public static class ContentValidator
{
    /// <summary>The texts the engine and the screens ask for directly, whatever the story.</summary>
    public static readonly IReadOnlyList<string> FixedKeys =
    [
        "language.name", "common.yes", "common.no",
        "ui.tagline", "ui.welcome", "ui.chooseClass", "ui.classSummary", "ui.askName", "ui.nameInvalid",
        "ui.nameRemembered", "ui.continue", "ui.continueLine", "ui.menuHint", "ui.menuHintLine", "ui.invalidChoice",
        "ui.invalidChoiceAgain", "ui.playAgain", "ui.goodbye", "ui.inventory", "ui.close", "ui.next",
        "stat.attack", "stat.defense", "stat.agility", "stat.intelligence",
        "battle.fighting", "battle.round", "battle.hp", "battle.attack", "battle.defend", "battle.flee",
        "battle.fleeLockedHint", "battle.won", "battle.lost", "battle.fledResult", "battle.opening",
        "game.over", "loot.fromEnemy", "loot.gold", "loot.nothing",
        "gear.title", "gear.equipped", "gear.bag", "gear.empty", "gear.twoHands", "gear.equip", "gear.unequip",
        "gear.drink", "gear.unequipped", "gear.equippedItem", "gear.drank", "gear.fullHp",
        "shop.title", "shop.forSale", "shop.services", "shop.price", "shop.soldOut", "shop.tooExpensive",
        "shop.soldOutMessage", "shop.cantAfford", "shop.bought", "shop.rest", "shop.restFull", "shop.rested",
        "shop.sell", "shop.sellTitle", "shop.back", "shop.leave", "shop.sold",
        "card.hp", "card.ac", "card.attack", "card.damage", "card.gold", "card.untrained", "card.untrainedShort",
        "effect.twoHanded", "effect.Finesse", "effect.Arcane", "effect.keen", "effect.hitBonus", "effect.ac",
        "effect.None", "effect.Light", "effect.Medium", "effect.Heavy", "effect.heal", "effect.untrained",
        "training.shields",
        .. Enum.GetValues<Rarity>().Select(r => $"rarity.{r.ToString().ToLowerInvariant()}"),
        .. Enum.GetValues<ItemSlot>().Select(s => $"slot.{s}"),
        .. BattleNarrator.Keys,
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
            yield return $"training.{heroClass.ArmorTraining}";
        }

        foreach (var item in rules.Items)
        {
            yield return $"item.{item.Id}";
            yield return $"item.{item.Id}.desc";
            if (item.Weapon is { } w) yield return $"damage.{w.DamageType}";
        }

        foreach (var enemy in rules.Enemies)
        {
            yield return $"enemy.{enemy.Id}";
            yield return $"enemy.{enemy.Id}.the";
            yield return $"damage.{enemy.DamageType}";
        }

        yield return "damage.bludgeoning"; // fists

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
        var sprites = content.Sprites;

        void NeedsItem(string id, string where)
        {
            if (!rules.HasItem(id)) problems.Add($"{where} lists an unknown item '{id}'.");
        }

        foreach (var heroClass in rules.Classes)
        {
            if (heroClass.Kits.Count == 0) problems.Add($"Class '{heroClass.Id}' has no kits.");
            foreach (var kit in heroClass.Kits)
            {
                NeedsItem(kit.Show, $"A kit of '{heroClass.Id}'");
                foreach (var id in kit.Items) NeedsItem(id, $"A kit of '{heroClass.Id}'");
                if (!kit.Items.Contains(kit.Show)) problems.Add($"A kit of '{heroClass.Id}' shows '{kit.Show}' but doesn't contain it.");
                var slots = kit.Items.Where(rules.HasItem).Select(id => rules.Item(id).Slot).ToList();
                if (slots.Distinct().Count() != slots.Count) problems.Add($"A kit of '{heroClass.Id}' puts two items in one slot.");
            }
        }

        foreach (var id in rules.StartItems) NeedsItem(id, "startItems");

        if (!sprites.Has("body")) problems.Add("sprites.json has no 'body' sprite.");
        foreach (var item in rules.Items.Where(i => !sprites.Has(i.Sprite)))
            problems.Add($"Item '{item.Id}' has no sprite '{item.Sprite}'.");
        foreach (var item in rules.Items.Where(i => i.Equippable == (i.Consumable is not null)))
            problems.Add($"Item '{item.Id}' must be either worn (a slot) or drunk (heal), not both or neither.");
        foreach (var item in rules.Items.Where(i => i.Weapon is not null && i.Slot != ItemSlot.MainHand))
            problems.Add($"Weapon '{item.Id}' must go in the main hand.");

        foreach (var enemy in rules.Enemies)
        {
            if (enemy.MaxHp <= 0) problems.Add($"Enemy '{enemy.Id}' needs more than 0 HP.");
            if (enemy.Aggression is < 0 or > 1) problems.Add($"Enemy '{enemy.Id}' aggression must be between 0 and 1.");
            if (!sprites.Has(enemy.Sprite)) problems.Add($"Enemy '{enemy.Id}' has no sprite '{enemy.Sprite}'.");
            if (enemy.Loot.GoldMax < enemy.Loot.GoldMin) problems.Add($"Enemy '{enemy.Id}' loot gold goes backwards.");
            foreach (var drop in enemy.Loot.Drops) NeedsItem(drop.ItemId, $"The loot of '{enemy.Id}'");
            foreach (var weight in enemy.Loot.Weights ?? [])
            {
                if (weight.Rarity is { } r && rules.OfRarity(r).Count == 0)
                    problems.Add($"The loot of '{enemy.Id}' can roll {r}, but there are no {r} items.");
            }
        }

        problems.AddRange(sprites.UnknownColors().Select(c => $"Sprite colour not in the palette: {c}."));
        problems.AddRange(sprites.BadRows().Select(r => $"Sprite row must be 16 wide, rows 0–23: {r}."));

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
                    if (scene.TextKey is null) problems.Add($"Choice scene '{scene.Id}' has no text.");
                    if (!scene.Choices.Any(c => c.Goto is not null))
                        problems.Add($"Choice scene '{scene.Id}' has no choice that moves the story on.");
                    foreach (var choice in scene.Choices.Where(c => c.Goto is null && c.ReplyKey is null))
                        problems.Add($"Choice '{choice.LabelKey}' in scene '{scene.Id}' needs a 'goto' or a 'reply'.");
                    break;
                case SceneKind.Narration or SceneKind.Loot or SceneKind.Shop:
                    if (scene.TextKey is null) problems.Add($"Scene '{scene.Id}' has no text.");
                    if (scene.Goto is null) problems.Add($"Scene '{scene.Id}' has no 'goto' and is not an end.");
                    foreach (var id in scene.Loot?.Items ?? []) NeedsItem(id, $"Scene '{scene.Id}'");
                    foreach (var stock in scene.Shop?.Stock ?? []) NeedsItem(stock.ItemId, $"Scene '{scene.Id}'");
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
