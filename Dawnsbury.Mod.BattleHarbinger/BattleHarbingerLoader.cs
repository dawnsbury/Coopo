using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Coroutines.Options;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Modding;
using System.Diagnostics;
using Dawnsbury.Mods.BattleHarbinger.RegisteredValues;

namespace Dawnsbury.Mods.BattleHarbinger;

public static class BattleHarbingerLoader
{

    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        // enable the debugger in debug mode, and assert that the right version of the game's DLL is being built against
#if DEBUG || DEBUG_V2
        Debugger.Launch();
#endif
#if DAWNSBURY_V2
        ModManager.AssertV2();
#else
        ModManager.AssertV3();
#endif

        ModManager.AddFeat(BattleHarbingerDoctrine());

        AddFeats(GetClassFeats());

        ModSpells.RegisterSpells();
    }

    static void AddFeats(IEnumerable<Feat> feats)
    {
        foreach (Feat f in feats)
        {
            ModManager.AddFeat(f);
        }
    }

    // extension method for Creature that allows getting just the class DC (unlike ClassOrSpellDC)
    public static int ClassDC(this Creature self)
    {
        return ((self.PersistentCharacterSheet?.Class != null) ? (self.Proficiencies.Get(self.PersistentCharacterSheet.Class.ClassTrait).ToNumber(self.Level) + self.Abilities.Get(self.Abilities.KeyAbility) + 10) : (12 + self.Level));
    }
    private static IEnumerable<Feat> GetClassFeats()
    {
        // Battle Harbinger Dedication
        yield return new TrueFeat(
        ModFeatName.BattleHarbingerDedication,
        2,
        "You have trained extensively in combat, battlefield tactics, and stamina, focusing on being an exceptional warrior for your faith in exchange for less time studying the traditional spells and scriptures.",
            "You gain the {i}Toughness{/i} general feat. If you already have this feat, you gain another general feat of your choice.",
            [ModTrait.BattleHarbinger, Trait.Cleric]
            ).WithPrerequisite(sheet => sheet.HasFeat(ModFeatName.BattleHarbingerDoctrine), "This feat is only available to Battle Harbingers.")
            .WithOnSheet(sheet =>
            {
                if (!sheet.HasFeat(FeatName.Toughness))
                {
                    sheet.GrantFeat(FeatName.Toughness);
                }
                else
                {
                    sheet.AddSelectionOptionRightNow(new SingleFeatSelectionOption("BHDedicationGeneralFeat", "Battle Harbinger Dedication - General Feat", 2, feat => feat.HasTrait(Trait.General)));
                }
            });
        // Aura Enhancement
        // feat has no built-in effect, and is just a marker for the code that adds the battle font slots to add extra allowed spells
        yield return new TrueFeat(
            ModFeatName.AuraEnhancement,
            4,
            "You've enhanced your training with your battle magic, allowing you access to a more potent divine font.",
            "Add {i}benediction{/i} and {i}malediction{/i} to the spells you can prepare with your additional slots from your divine font. These spells are also battle auras.",
            [ModTrait.BattleHarbinger, Trait.Cleric]
            ).WithPrerequisite(sheet => sheet.HasFeat(ModFeatName.BattleHarbingerDoctrine), "This feat is only available to Battle Harbingers.");
        // Tandem Onslaught
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Tandem Onslaught"),
            4,
            "You have trained your body and mind to work in tandem, and you can combine your combat and spellcasting prowess to better support yourself in battle.",
            "The first time each round that you successfully hit and deal damage to an enemy creature with a Strike using a weapon or unarmed attack, you can automatically Sustain a single battle aura that you currently have active, applying any additional effects that come with Sustaining the spell.",
            [ModTrait.BattleHarbinger, Trait.Cleric]
            ).WithPrerequisite(sheet => sheet.HasFeat(ModFeatName.BattleHarbingerDoctrine), "This feat is only available to Battle Harbingers.")
            .WithOnCreature((Creature cr) =>
            {
                cr.AddQEffect(new QEffect()
                {
                    AfterYouDealDamage = async (Creature attacker, CombatAction action, Creature target) =>
                    {
                        if (attacker.HasEffect(ModQEffectId.TandemOnslaughtOncePerTurn)) return;
                        if (!action.HasTrait(Trait.Strike)) return;
                        if (!(action.HasTrait(Trait.Weapon) || action.HasTrait(Trait.Unarmed))) return;
                        IEnumerable<CombatAction> battleAuraSustains = attacker.Possibilities.CreateActions(false)
                            .Where(iAction => iAction.CanBeginToUse(attacker).CanBeUsed || iAction.CanBeginToUse(attacker) == Usability.CommonReasons.NoActions)
                            .Select(iAction => iAction.Action)
                            .Where(action => action.HasTrait(Trait.SustainASpell) && action.HasTrait(ModTrait.BattleAura));
                        if (battleAuraSustains.Any())
                        {
                            ChoiceButtonOption choice = await attacker.AskForChoiceAmongButtons(IllustrationName.None, "You successfully damaged an enemy. Which battle aura would you like to Sustain with Tandem Onslaught?", battleAuraSustains.Select(action => action.Name).Append("None").ToArray());
                            if (choice.Text == "None") return;
                            CombatAction? chosen = battleAuraSustains.FirstOrDefault(action => action.Name == choice.Text);
                            if (chosen == null) return;
                            // execute the chosen sustain action for free
                            await chosen.WithActionCost(0).AllExecute();
                            // Cannot happen again until start of next turn
                            attacker.AddQEffect(new QEffect()
                            {
                                Id = ModQEffectId.TandemOnslaughtOncePerTurn,
                                ExpiresAt = ExpirationCondition.ExpiresAtStartOfYourTurn
                            });
                        }
                    }
                });
            });
    }

    private static Feat BattleHarbingerDoctrine()
    {
        return new Feat(
            ModFeatName.BattleHarbingerDoctrine,
            "You've dedicated yourself to the battle creed, a specific doctrine that puts combat prowess first, even at the expense of a cleric's typical spellcasting and restorative abilities.",
            "This doctrine is a {i}class archetype{/i}: You gain Battle Harbinger Dedication instead of your 2nd-level class feat.\n" +
            "{b}Initial Creed (at level 1):{/b} You're trained in light and medium armor. You have expert proficiency in Fortitude saves. You're trained in martial weapons. If your deity's favored weapon is a simple weapon or an unarmed attack, you gain the Deadly Simplicity cleric feat.\n" +
            "{b}Battle Harbinger Dedication (at level 2):{/b} You gain the Toughness general feat. If you already have this feat, you gain another general feat of your choice.\n" +
            "{b}Lesser Creed (at level 5):{/b} You gain expert proficiency with your deity's favored weapon, martial weapons, simple weapons, and unarmed attacks. When you critically succeed at an attack roll using your deity's favored weapon, you apply the weapon's {tooltip:criteffect}critical specialization effect{/tooltip}. Your proficiency rank for your class DC increases to expert.\n\n" +
            "In addition, the following class features are modified:\n" +
            "{b}Spellcasting:{/b} Your spellcasting capabilities are more restricted. At 1st level, you can prepare only one 1st-level spell and five cantrips, and at 3rd level, you gain only one 2nd-level spell slot; You gain one more spell slot at 2nd and 4th level as normal. From 5th level onwards, you always have two spell slots of your highest level and two more of your second highest level.\n" +
            "{b}Divine Font:{/b} Instead of preparing {i}heal{/i} or {i}harm{/i} spells with your divine font, you gain the battle font, which allows you to prepare battle aura spells. You gain 4 additional spell slots each day at your highest level of cleric spell slots, in which you can only prepare {i}bane{/i} or {i}bless{/i}. Any feats or effects that refer to battle auras refer to these spells, regardless of whether they were cast from your divine font spell slots or your standard spell slots. Your battle auras use your class DC instead of your spell DC. At 5th level, the number of additional slots increases to 5.",
            [Trait.DoctrineSelection],
            null).WithOnSheet(sheet =>
            {
                // Initial Creed (level 1)
                sheet.SetProficiency(Trait.LightArmor, Proficiency.Trained);
                sheet.SetProficiency(Trait.MediumArmor, Proficiency.Trained);
                sheet.SetProficiency(Trait.Fortitude, Proficiency.Expert);
                sheet.SetProficiency(Trait.Martial, Proficiency.Trained);
                Item? favoredWeapon = (sheet.Deity != null) ? Items.CreateNew(sheet.Deity.FavoredWeapon) : null;
                if (favoredWeapon != null && (favoredWeapon.HasTrait(Trait.Simple) || favoredWeapon.HasTrait(Trait.Unarmed)))
                {
                    sheet.GrantFeat(FeatName.DeadlySimplicity);
                }
                // Dedication Feat (level 2)
                // Remove 2nd level class feat
                sheet.SelectionOptions.RemoveAll(option => option.Key == "Root:Class:ClericFeat2");
                sheet.AddSelectionOption(new SingleFeatSelectionOption("BattleHarbingerDedicationFeatSelection", "Cleric feat (Battle Harbinger)", 2, feat => feat.FeatName == ModFeatName.BattleHarbingerDedication));
                // Lesser Creed (level 5)
                sheet.AddAtLevel(5, sheet =>
                {
                    sheet.SetProficiency(Trait.Unarmed, Proficiency.Expert);
                    sheet.SetProficiency(Trait.Simple, Proficiency.Expert);
                    sheet.SetProficiency(Trait.Martial, Proficiency.Expert);
                    Item? favoredWeapon = (sheet.Deity != null) ? Items.CreateNew(sheet.Deity.FavoredWeapon) : null;
                    if (favoredWeapon != null && favoredWeapon.MainTrait != Trait.None)
                    {
                        sheet.SetProficiency(favoredWeapon.MainTrait, Proficiency.Expert);
                    }
                    sheet.SetProficiency(Trait.Cleric, Proficiency.Expert);
                });
                // Spellcasting Modifications
                sheet.AtEndOfRecalculation += (CalculatedCharacterSheetValues sheet) =>
                {
                    // remove last spell slot of each level, to reduce the max per spell level to 2
                    sheet.PreparedSpells[Trait.Cleric].Slots.RemoveAll((PreparedSpellSlot slot) => slot.Key.EndsWith("-3"));
                    // remove one more for odd levels less than 5
                    if (sheet.CurrentLevel <= 4 && sheet.CurrentLevel % 2 == 1)
                        sheet.PreparedSpells[Trait.Cleric].Slots.RemoveAll((PreparedSpellSlot slot) => slot.Key == $"Cleric:Spell{sheet.MaximumSpellLevel}-2");
                    // remove spell slots of levels below maxSpellLevel - 1
                    for (int i = 1; i < sheet.MaximumSpellLevel - 1; i++)
                    {
                        sheet.PreparedSpells[Trait.Cleric].Slots.RemoveAll((PreparedSpellSlot slot) => slot.Key.StartsWith("Cleric:Spell" + i));
                    }
                };

                // Divine Font Modifications
                // Remove divine font selection and feats
                sheet.SelectionOptions.RemoveAll(option => option.Name == "Divine font");
                sheet.AllFeats.RemoveAll(feat => feat.HasTrait(Trait.DivineFont));
                sheet.AtEndOfRecalculation += (CalculatedCharacterSheetValues sheet) =>
                {
                    // Grant battle font slots
                    int slotCount = sheet.CurrentLevel >= 5 ? 5 : 4;
                    List<SpellId> extraAuras = [];
                    if (sheet.HasFeat(ModFeatName.AuraEnhancement))
                    {
                        extraAuras = [ModSpellId.Benediction, ModSpellId.Malediction];
                    }
                    for (int i = 1; i <= slotCount; i++)
                    {
                        sheet.PreparedSpells[Trait.Cleric].Slots.Add(new BattleFontSpellSlot(sheet.MaximumSpellLevel, $"BattleFont:{i}", extraAuras));
                    }
                };
            }).WithOnCreature((CalculatedCharacterSheetValues sheet, Creature cr) =>
            {
                // grant crit spec with favoured weapon at level 5 and on
                if (cr.Level >= 5)
                    cr.AddQEffect(new QEffect()
                    {
                        YouHaveCriticalSpecialization = (QEffect self, Item weapon, CombatAction action, Creature target) =>
                        {
                            Item? favoredWeapon = (sheet.Deity != null) ? Items.CreateNew(sheet.Deity.FavoredWeapon) : null;
                            if (favoredWeapon != null && weapon.MainTrait == favoredWeapon.MainTrait) return true;
                            else return false;
                        }
                    });
            });
    }
}
