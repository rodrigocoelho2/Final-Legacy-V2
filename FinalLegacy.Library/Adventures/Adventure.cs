using FinalLegacy.Library.Combat;
using FinalLegacy.Library.Content;
using FinalLegacy.Library.Enums;
using FinalLegacy.Library.Items;
using FinalLegacy.Library.Localization;
using FinalLegacy.Library.Models;
using FinalLegacy.Library.Sprites;
using FinalLegacy.Library.Story;
using FinalLegacy.Library.Text;

namespace FinalLegacy.Library.Adventures;

/// <summary>How a run ended, the hero that played it and the scenes it went through.</summary>
public sealed record AdventureResult(AdventureOutcome Outcome, Hero Hero, IReadOnlyList<string> Path);

/// <summary>
/// One playthrough: builds the hero (class, kit, name), then walks the story scene by scene — asking the screen for
/// every choice, running battles, handing out loot and opening the inventory and the merchant.
/// </summary>
public sealed class Adventure(GameContent content, Language language, IGameUi ui, IRandomSource random)
{
    /// <summary>The order the hero's pieces are drawn in, from the body outwards.</summary>
    private static readonly ItemSlot[] DrawOrder = [ItemSlot.Legs, ItemSlot.Chest, ItemSlot.Head, ItemSlot.OffHand, ItemSlot.MainHand];

    private static readonly ItemSlot[] Slots =
        [ItemSlot.Head, ItemSlot.Chest, ItemSlot.Legs, ItemSlot.MainHand, ItemSlot.OffHand, ItemSlot.Ring];

    private readonly Localizer _texts = content.Texts[language];
    private readonly GameRules _rules = content.Rules;

    public AdventureResult Run()
    {
        ui.ShowTitle();
        return Play(CreateHero());
    }

    // ── The hero ───────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Class, weapon (kit) and name, in the original's order.</summary>
    public Hero CreateHero()
    {
        var classes = _rules.Classes;
        var classIndex = ui.ChooseClass(classes.Select(c => new ClassOption(
            _texts[$"class.{c.Id}.name"],
            c.Color,
            _texts[$"class.{c.Id}.blurb"],
            c.BaseStats.AtLeastZero(),
            c.MaxHp,
            Training(c),
            Look(_rules.NewHero("", c, c.Kits[0])))).ToList());
        var heroClass = classes[Checked(classIndex, classes.Count)];

        var kitIndex = ui.ChooseKit(
            _texts[$"class.{heroClass.Id}.weapons"],
            heroClass.Kits.Select(k => new KitOption(
                ItemName(_rules.Item(k.Show)),
                ModifiersText(_rules.Item(k.Show).Modifiers),
                string.Join(" · ", k.Items.Select(id => ItemName(_rules.Item(id)))),
                Card(_rules.NewHero("", heroClass, k)))).ToList());
        var kit = heroClass.Kits[Checked(kitIndex, heroClass.Kits.Count)];

        var preview = _rules.NewHero("", heroClass, kit);
        ui.Narrate(_texts.Format(
            "ui.classSummary",
            ("class", Markup.Colorize(_texts[$"class.{heroClass.Id}.name"], heroClass.Color)),
            ("weapon", Markup.Colorize(ItemName(_rules.Item(kit.Show)), "cyan"))), Card(preview));

        var hero = _rules.NewHero(AskName(), heroClass, kit);
        ui.Narrate(Fill(_texts["ui.nameRemembered"], hero), Card(hero));
        return hero;
    }

    /// <summary>A name is trimmed and must be 1 to NameMaxLength characters long.</summary>
    public static bool IsValidName(string? name, int maxLength) =>
        !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= maxLength;

    private string AskName()
    {
        string? error = null;
        while (true)
        {
            var answer = ui.AskName(_texts["ui.askName"], error);
            if (IsValidName(answer, _rules.NameMaxLength)) return answer.Trim();
            error = _texts.Format("ui.nameInvalid", ("max", _rules.NameMaxLength));
        }
    }

    // ── The story ──────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Walks the story from its start scene with an existing hero.</summary>
    public AdventureResult Play(Hero hero)
    {
        var path = new List<string>();
        var stock = new Dictionary<string, Dictionary<string, int?>>();
        var scene = content.Story[content.Story.Start];

        while (true)
        {
            path.Add(scene.Id);
            switch (scene.Kind)
            {
                case SceneKind.Narration:
                    ui.Narrate(Text(scene, hero));
                    scene = content.Story[scene.Goto!];
                    break;

                case SceneKind.Choice:
                    scene = content.Story[Choose(scene, hero)];
                    break;

                case SceneKind.Loot:
                    var found = scene.Loot!;
                    var extra = RollLoot(found.Random).Items;
                    Loot(hero, Text(scene, hero), found.Gold, found.Items.Select(_rules.Item).Concat(extra).ToList());
                    scene = content.Story[scene.Goto!];
                    break;

                case SceneKind.Shop:
                    if (!stock.ContainsKey(scene.Id))
                        stock[scene.Id] = scene.Shop!.Stock.ToDictionary(s => s.ItemId, s => s.Count);
                    Shop(hero, Text(scene, hero), scene.Shop!, stock[scene.Id]);
                    scene = content.Story[scene.Goto!];
                    break;

                case SceneKind.Battle:
                    var enemy = _rules.Enemy(scene.Battle!.EnemyId);
                    var outcome = Fight(hero, scene.Battle);
                    if (outcome == BattleOutcome.Lost)
                    {
                        ui.Narrate(_texts["game.over"], Card(hero));
                        return new AdventureResult(AdventureOutcome.Defeated, hero, path);
                    }

                    if (outcome == BattleOutcome.Won)
                    {
                        var (gold, items) = RollLoot(enemy.Loot);
                        var the = _texts[$"enemy.{enemy.Id}.the"];
                        Loot(hero, _texts.Format("loot.fromEnemy", ("The", Localizer.Capitalize(the)), ("the", the)), gold, items);
                    }

                    scene = content.Story[outcome == BattleOutcome.Fled ? scene.Flee! : scene.Win!];
                    break;

                case SceneKind.End:
                    ui.Narrate(Text(scene, hero), Card(hero));
                    return new AdventureResult(AdventureOutcome.ChapterComplete, hero, path);
            }
        }
    }

    private string Choose(Scene scene, Hero hero)
    {
        var text = Text(scene, hero);
        var labels = scene.Choices.Select(c => _texts[c.LabelKey]).Append(_texts["ui.inventory"]).ToList();
        string? note = null;

        while (true)
        {
            var index = Checked(ui.Choose(text, labels, note), labels.Count);
            if (index == scene.Choices.Count)
            {
                Inventory(hero);
                continue;
            }

            var choice = scene.Choices[index];
            if (choice.Goto is not null) return choice.Goto;
            note = Fill(_texts[choice.ReplyKey!], hero);
        }
    }

    // ── Battles ────────────────────────────────────────────────────────────────────────────────────────────────

    /// <summary>Runs a battle on the screen until someone wins or the hero gets away.</summary>
    public BattleOutcome Fight(Hero hero, BattleSpec spec)
    {
        var enemy = _rules.Enemy(spec.EnemyId);
        var battle = new Battle(hero, enemy, spec.CanFlee, _rules.Combat, random);
        var narrator = new BattleNarrator(_texts);
        IReadOnlyList<string> log = [];

        while (battle.Outcome == BattleOutcome.Ongoing)
        {
            var potions = hero.Bag.Where(s => s.Item.Consumable is not null).ToList();
            var actions = new List<(ActionOption Option, CombatAction Action, Item? Item)>
            {
                (new ActionOption(_texts["battle.attack"], null), CombatAction.Attack, null),
                (new ActionOption(_texts["battle.defend"], null), CombatAction.Defend, null),
            };
            actions.AddRange(potions.Select(p => (
                new ActionOption($"{ItemName(p.Item)} ×{p.Count}", null), CombatAction.UseItem, (Item?)p.Item)));
            actions.Add((new ActionOption(_texts["battle.flee"], spec.CanFlee ? null : _texts["battle.fleeLockedHint"]), CombatAction.Flee, null));

            var picked = actions[Checked(ui.ChooseAction(Screen(battle, log, actions.Select(a => a.Option).ToList())), actions.Count)];
            log = narrator.Describe(battle, battle.Play(picked.Action, picked.Item));
        }

        var result = battle.Outcome switch
        {
            BattleOutcome.Won => _texts["battle.won"],
            BattleOutcome.Lost => _texts["battle.lost"],
            _ => _texts["battle.fledResult"],
        };
        ui.ShowBattleEnd(Screen(battle, log, []), result);
        return battle.Outcome;
    }

    private BattleScreen Screen(Battle battle, IReadOnlyList<string> log, IReadOnlyList<ActionOption> actions) => new(
        Card(battle.Hero),
        _texts[$"enemy.{battle.Enemy.Id}"],
        battle.EnemyHp,
        battle.Enemy.MaxHp,
        battle.Enemy.ArmorClass,
        content.Sprites.Draw(battle.Enemy.Sprite),
        battle.Round,
        battle.HeroHasOpening,
        log,
        actions);

    /// <summary>
    /// The gold and items of a loot table: the set drops each on their own chance, then every random roll draws a
    /// rarity by its weight and a random item of that rarity.
    /// </summary>
    public (int Gold, IReadOnlyList<Item> Items) RollLoot(LootTable table)
    {
        var gold = table.GoldMin + Dice.Die(random, table.GoldMax - table.GoldMin + 1) - 1;
        var items = table.Drops.Where(d => random.NextDouble() < d.Chance).Select(d => _rules.Item(d.ItemId)).ToList();

        var weights = table.Weights ?? [];
        var total = weights.Sum(w => w.Weight);
        for (var roll = 0; roll < table.Rolls && total > 0; roll++)
        {
            var pick = random.NextDouble() * total;
            var rarity = weights.First(w => (pick -= w.Weight) < 0).Rarity;
            if (rarity is not { } r) continue;
            var pool = _rules.OfRarity(r);
            if (pool.Count > 0) items.Add(pool[Math.Min(pool.Count - 1, (int)(random.NextDouble() * pool.Count))]);
        }

        return (gold, items);
    }

    // ── Loot, inventory and the merchant ───────────────────────────────────────────────────────────────────────

    private void Loot(Hero hero, string text, int gold, IReadOnlyList<Item> items)
    {
        hero.Gold += gold;
        foreach (var item in items) hero.Add(item);

        var cards = items.GroupBy(i => i.Id).Select(g => ItemCard(g.First(), g.Count())).ToList();
        var choice = ui.ShowLoot(new LootScreen(text, gold, cards, [_texts["ui.next"], _texts["ui.inventory"]]));
        if (choice == 1) Inventory(hero);
    }

    /// <summary>The inventory: wear, take off and drink, with a preview of every change.</summary>
    public void Inventory(Hero hero)
    {
        string? message = null;
        while (true)
        {
            var entries = new List<(GearEntry Entry, Func<string?>? Act)>();
            foreach (var slot in Slots)
            {
                var worn = hero.Worn(slot);
                var section = _texts["gear.equipped"];
                var label = _texts[$"slot.{slot}"];
                if (worn is null)
                {
                    var blocked = slot == ItemSlot.OffHand && hero.Worn(ItemSlot.MainHand)?.TwoHanded == true;
                    entries.Add((new GearEntry(section, label, _texts[blocked ? "gear.twoHands" : "gear.empty"], null, false, null, null), null));
                    continue;
                }

                entries.Add((new GearEntry(section, label, ItemName(worn), _texts["gear.unequip"], true, ItemCard(worn, 1, hero),
                    Preview(hero, h => h.Unequip(slot))), () =>
                    {
                        hero.Unequip(slot);
                        return _texts.Format("gear.unequipped", ("item", ItemName(worn)));
                    }));
            }

            foreach (var stack in hero.Bag.ToList())
            {
                var item = stack.Item;
                var section = _texts["gear.bag"];
                var name = stack.Count > 1 ? $"{ItemName(item)} ×{stack.Count}" : ItemName(item);
                if (item.Consumable is not null)
                {
                    entries.Add((new GearEntry(section, name, null, _texts["gear.drink"], true, ItemCard(item, stack.Count, hero), null), () =>
                    {
                        if (hero.Hp >= hero.MaxHp) return _texts["gear.fullHp"];
                        var roll = hero.Drink(item, random);
                        return _texts.Format("gear.drank", ("item", ItemName(item)), ("heal", roll.Total));
                    }));
                }
                else
                {
                    entries.Add((new GearEntry(section, name, _texts[$"slot.{item.Slot}"], _texts["gear.equip"], true, ItemCard(item, 1, hero),
                        Preview(hero, h => h.Equip(item))), () =>
                        {
                            hero.Equip(item);
                            return _texts.Format("gear.equippedItem", ("item", ItemName(item)));
                        }));
                }
            }

            entries.Add((new GearEntry("", _texts["ui.close"], null, null, true, null, null), null));

            var screen = new GearScreen(_texts["gear.title"], null, Card(hero), entries.Select(e => e.Entry).ToList(), message);
            var index = Checked(ui.Gear(screen), entries.Count);
            if (index == entries.Count - 1) return;
            message = entries[index].Act?.Invoke();
        }
    }

    private void Shop(Hero hero, string intro, ShopSpec shop, Dictionary<string, int?> stock)
    {
        string? message = null;
        while (true)
        {
            var entries = new List<(GearEntry Entry, Func<string?>? Act)>();
            var section = _texts["shop.forSale"];
            foreach (var (id, left) in stock)
            {
                var item = _rules.Item(id);
                var soldOut = left is 0;
                var affordable = hero.Gold >= item.Price;
                entries.Add((new GearEntry(
                    section,
                    ItemName(item),
                    soldOut ? _texts["shop.soldOut"] : _texts.Format("shop.price", ("price", item.Price)),
                    null,
                    !soldOut && affordable,
                    ItemCard(item, 1, hero),
                    item.Equippable ? Preview(hero, h => { h.Add(item); h.Equip(item); }) : null), () =>
                    {
                        if (soldOut) return _texts["shop.soldOutMessage"];
                        if (!affordable) return _texts["shop.cantAfford"];
                        hero.Gold -= item.Price;
                        hero.Add(item);
                        if (stock[id] is { } n) stock[id] = n - 1;
                        return _texts.Format("shop.bought", ("item", ItemName(item)));
                    }));
            }

            var services = _texts["shop.services"];
            if (shop.RestPrice > 0)
            {
                var canRest = hero.Gold >= shop.RestPrice && hero.Hp < hero.MaxHp;
                entries.Add((new GearEntry(services, _texts["shop.rest"], _texts.Format("shop.price", ("price", shop.RestPrice)),
                    hero.Hp >= hero.MaxHp ? _texts["shop.restFull"] : hero.Gold < shop.RestPrice ? _texts["shop.tooExpensive"] : null,
                    canRest, null, null), () =>
                    {
                        if (hero.Hp >= hero.MaxHp) return _texts["gear.fullHp"];
                        if (hero.Gold < shop.RestPrice) return _texts["shop.cantAfford"];
                        hero.Gold -= shop.RestPrice;
                        hero.Hp = hero.MaxHp;
                        return _texts["shop.rested"];
                    }));
            }

            entries.Add((new GearEntry(services, _texts["shop.sell"], null, null, hero.Bag.Count > 0, null, null), () =>
            {
                Sell(hero);
                return null;
            }));
            entries.Add((new GearEntry(services, _texts["ui.inventory"], null, null, true, null, null), () =>
            {
                Inventory(hero);
                return null;
            }));
            entries.Add((new GearEntry("", _texts["shop.leave"], null, null, true, null, null), null));

            var screen = new GearScreen(_texts["shop.title"], intro, Card(hero), entries.Select(e => e.Entry).ToList(), message);
            var index = Checked(ui.Gear(screen), entries.Count);
            if (index == entries.Count - 1) return;
            message = entries[index].Act?.Invoke();
        }
    }

    private void Sell(Hero hero)
    {
        string? message = null;
        while (true)
        {
            var entries = hero.Bag.Select(stack => (Entry: new GearEntry(
                _texts["gear.bag"],
                stack.Count > 1 ? $"{ItemName(stack.Item)} ×{stack.Count}" : ItemName(stack.Item),
                _texts.Format("shop.price", ("price", stack.Item.SellPrice)),
                null,
                true,
                ItemCard(stack.Item, stack.Count, hero),
                null), Item: stack.Item)).ToList();
            entries.Add((new GearEntry("", _texts["shop.back"], null, null, true, null, null), null!));

            var screen = new GearScreen(_texts["shop.sellTitle"], null, Card(hero), entries.Select(e => e.Entry).ToList(), message);
            var index = Checked(ui.Gear(screen), entries.Count);
            if (index == entries.Count - 1) return;

            var item = entries[index].Item;
            hero.Remove(item);
            hero.Gold += item.SellPrice;
            message = _texts.Format("shop.sold", ("item", ItemName(item)), ("price", item.SellPrice));
        }
    }

    // ── Cards and pictures ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>How the hero looks: the body (with the class's hair), then what they wear, from the inside out.</summary>
    public PixelGrid Look(Hero hero)
    {
        var layers = new List<string> { "body" };
        layers.AddRange(DrawOrder.Select(hero.Worn).OfType<Item>().Select(i => i.Sprite));
        return content.Sprites.Compose(layers, new Dictionary<char, Rgb> { ['h'] = Rgb.Parse(hero.Class.Hair) });
    }

    public HeroCard Card(Hero hero) => new(
        Markup.Escape(hero.Name),
        _texts[$"class.{hero.Class.Id}.name"],
        hero.Class.Color,
        hero.Hp,
        hero.MaxHp,
        hero.ArmorClass,
        hero.AttackBonus,
        $"{hero.Damage} {_texts[$"damage.{hero.Weapon.DamageType}"]}",
        hero.Stats,
        hero.Gold,
        hero.Proficient,
        Look(hero));

    private HeroCard Preview(Hero hero, Action<Hero> change)
    {
        var copy = hero.Clone();
        change(copy);
        return Card(copy);
    }

    public ItemCard ItemCard(Item item, int count, Hero? wearer = null)
    {
        var effects = new List<string>();
        if (item.Weapon is { } w)
        {
            var weapon = $"{w.Damage} {_texts[$"damage.{w.DamageType}"]}";
            if (w.TwoHanded) weapon += $" · {_texts["effect.twoHanded"]}";
            if (w.Stat != AttackStat.Might) weapon += $" · {_texts[$"effect.{w.Stat}"]}";
            if (w.CritFrom < 20) weapon += $" · {_texts.Format("effect.keen", ("from", w.CritFrom))}";
            if (w.HitBonus != 0) weapon += $" · {_texts.Format("effect.hitBonus", ("bonus", w.HitBonus))}";
            effects.Add(weapon);
        }

        if (item.Armor is { } a)
        {
            var armor = _texts.Format("effect.ac", ("ac", a.ArmorClass));
            if (item.Slot == ItemSlot.Chest)
                armor += $" · {_texts.Format($"effect.{a.Weight}", ("cap", _rules.Combat.MediumArmorAgilityCap))}";
            effects.Add(armor);
        }

        if (item.Consumable is { } c) effects.Add(_texts.Format("effect.heal", ("dice", c.Heal)));
        if (item.Modifiers != Stats.Zero) effects.Add(ModifiersText(item.Modifiers));
        if (wearer is not null && !wearer.IsProficientWith(item)) effects.Add($"[red]{_texts["effect.untrained"]}[/]");

        var rarity = item.Rarity.ToString().ToLowerInvariant();
        return new ItemCard(
            ItemName(item),
            _texts[$"rarity.{rarity}"],
            RarityColor(item.Rarity),
            $"{_texts[$"rarity.{rarity}"]} · {_texts[$"slot.{item.Slot}"]}",
            effects,
            _texts[$"item.{item.Id}.desc"],
            content.Sprites.Draw(item.Sprite),
            count);
    }

    public static string RarityColor(Rarity rarity) => rarity switch
    {
        Rarity.Uncommon => "green",
        Rarity.Rare => "blue",
        Rarity.Epic => "violet",
        Rarity.Legendary => "gold",
        _ => "white",
    };

    /// <summary>An item's name in its rarity's colour.</summary>
    public string ItemName(Item item) => Markup.Colorize(_texts[$"item.{item.Id}"], RarityColor(item.Rarity));

    /// <summary>"+2 DEF  −1 ATK": the gains first, then the losses.</summary>
    public string ModifiersText(Stats modifiers) => string.Join("  ", modifiers.Entries()
        .Where(e => e.Value != 0)
        .OrderByDescending(e => e.Value > 0)
        .Select(e => $"{(e.Value > 0 ? "+" : "−")}{Math.Abs(e.Value)} {_texts[e.Key]}"));

    private string Training(HeroClass heroClass)
    {
        var text = _texts[$"training.{heroClass.ArmorTraining}"];
        return heroClass.ShieldTraining ? $"{text} · {_texts["training.shields"]}" : text;
    }

    private string Text(Scene scene, Hero hero) => Fill(_texts[scene.TextKey!], hero);

    private static string Fill(string text, Hero hero) => Localizer.Fill(text, ("name", Markup.Escape(hero.Name)));

    private static int Checked(int index, int count) =>
        index >= 0 && index < count
            ? index
            : throw new ArgumentOutOfRangeException(nameof(index), index, $"The screen answered option {index} of {count}.");
}
