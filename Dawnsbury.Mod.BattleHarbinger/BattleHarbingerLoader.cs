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
using Dawnsbury.Core.Mechanics.Damage;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.CharacterBuilder.FeatsDb;

namespace Dawnsbury.Mods.BattleHarbinger;

public static class BattleHarbingerLoader
{

    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
        // enable the debugger in debug mode, and assert that the right version of the game's DLL is being built against
#if DEBUG || DEBUG_V2
        //Debugger.Launch();
#endif
#if DAWNSBURY_V2
        ModManager.AssertV2();
#else
        ModManager.AssertV3();
#endif

        ModSpells.RegisterSpells();

        ModManager.AddFeat(BattleHarbingerDoctrine());

        AddFeats(GetClassFeats());
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
        // feat is also a marker for the code that adds the battle font slots to add extra allowed spells
        yield return new TrueFeat(
            ModFeatName.AuraEnhancement,
            4,
            "You've enhanced your training with your battle magic, allowing you access to a more potent divine font.",
            "Add {i}battle benediction{/i} and {i}battle malediction{/i} to your list of battle auras. Like your other battle auras, you can prepare these in the additional slots from your divine font.",
            [ModTrait.BattleHarbinger, Trait.Cleric]
            ).WithPrerequisite(sheet => sheet.HasFeat(ModFeatName.BattleHarbingerDoctrine), "This feat is only available to Battle Harbingers.")
            .WithOnSheet(sheet =>
            {
                // Grant access to the spells for preparation
                sheet.ClericAdditionalPreparableSpells.Add(ModSpellId.BattleBenediction);
                sheet.ClericAdditionalPreparableSpells.Add(ModSpellId.BattleMalediction);
            });
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
        // Exigent Aura
        // feat has no built-in effect, and is just a marker for the code that checks if a creature is mindless
        yield return new TrueFeat(
            ModFeatName.ExigentAura,
            6,
            "The power of your deity and your own convictions leave an impression on creatures even when they normally would be unable to comprehend your words or the feelings spurred on by your god.",
            "Your battle auras can affect mindless creatures, though they gain a +4 circumstance bonus on saving throws to resist the battle aura's effects.",
            [ModTrait.BattleHarbinger, Trait.Cleric]
            ).WithPrerequisite(sheet => sheet.HasFeat(ModFeatName.BattleHarbingerDoctrine), "This feat is only available to Battle Harbingers.");
        // Harbinger's Protection
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Harbinger's Protection"),
            6,
            "You often work on missions alone, making sure to cover up your weaknesses more effectively than other members of your faith.",
            "You are trained in heavy armor. When you gain expert or greater proficiency in any type of armor, you also gain that proficiency in heavy armor.",
            [ModTrait.BattleHarbinger, Trait.Cleric]
            ).WithPrerequisite(sheet => sheet.HasFeat(ModFeatName.BattleHarbingerDoctrine), "This feat is only available to Battle Harbingers.")
            .WithOnSheet(sheet =>
            {
                // set heavy armor proficiency to at least trained, and increase if higher proficiency is had in light or medium.
                sheet.SetProficiency(Trait.HeavyArmor, Proficiency.Trained);
                sheet.SetProficiency(Trait.HeavyArmor, sheet.GetProficiency(Trait.LightArmor));
                sheet.SetProficiency(Trait.HeavyArmor, sheet.GetProficiency(Trait.MediumArmor));
            });
        // Creed Magic
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Creed Magic"),
            8,
            "You've expanded your divine capabilities, granting you magic that better supports your combat focus.",
            "You gain two special 2nd-level creed spell slots, which can be used to prepare {i}resist energy{/i}, {i}see invisibility{/i}, and {i}true strike{/i} as divine spells.",
            [ModTrait.BattleHarbinger, Trait.Cleric]
            ).WithPrerequisite(sheet => sheet.HasFeat(ModFeatName.BattleHarbingerDoctrine), "This feat is only available to Battle Harbingers.")
            .WithOnSheet(sheet =>
            {
                sheet.PreparedSpells[Trait.Cleric].Slots.Add(new CreedMagicSpellSlot(2, "CreedMagic:1"));
                sheet.PreparedSpells[Trait.Cleric].Slots.Add(new CreedMagicSpellSlot(2, "CreedMagic:2"));
                sheet.ClericAdditionalPreparableSpells.Add(SpellId.ResistEnergy);
                sheet.ClericAdditionalPreparableSpells.Add(SpellId.SeeInvisibility);
                sheet.ClericAdditionalPreparableSpells.Add(SpellId.TrueStrike);
            });
        // Harbinger's Armament
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Harbinger's Armament"),
            8,
            "Your deity grants you extra power that you have learned to channel into your weapons.",
            "Any melee Strike you make gains the benefits of the disrupting rune. This does not count against your rune limit for that weapon.\n\n{b}Disrupting{/b} The weapon deals 1d6 additional positive damage to undead, and on a critical hit, a struck undead creature is enfeebled 1 until the end of your next turn.",
            [ModTrait.BattleHarbinger, Trait.Cleric]
            ).WithPrerequisite(sheet => sheet.HasFeat(ModFeatName.BattleHarbingerDoctrine), "This feat is only available to Battle Harbingers.")
            .WithOnCreature((Creature cr) =>
            {
                cr.AddQEffect(new QEffect("Harbinger's Armament", "Your melee strikes are disrupting.") {
                    AddExtraKindedDamageOnStrike = (CombatAction action, Creature target) => (action.HasTrait(Trait.Melee) && target.HasTrait(Trait.Undead)) ? new KindedDamage(DiceFormula.FromText("1d6", "Harbinger's Armament"), DamageKind.Positive) : null,
                    AfterYouDealDamage = async delegate (Creature attacker, CombatAction action, Creature target)
                    {
                        if (action.HasTrait(Trait.Melee) && action.HasTrait(Trait.Strike)
                            && action.CheckResult == CheckResult.CriticalSuccess && target.HasTrait(Trait.Undead))
                        {
                            QEffect enfeebled = QEffect.Enfeebled(1);
                            enfeebled.ExpiresAt = ExpirationCondition.ExpiresAtEndOfSourcesTurn;
                            enfeebled.RoundsLeft = 1;
                            enfeebled.Source = attacker;
                            enfeebled.CannotExpireThisTurn = true;
                            enfeebled.CountsAsBeneficialToSource = true;
                            target.AddQEffect(enfeebled);
                        }
                    }
                });
            });
        // magus can have it so i guess battle harbinger can have it
        //AllFeats.All.FirstOrDefault(feat => feat.FeatName == FeatName.AbundantSpellcastingCleric1)?.Prerequisites.Add(
        //    new Prerequisite(sheet => !sheet.HasFeat(ModFeatName.BattleHarbingerDoctrine), "This feat is not available to Battle Harbingers.")
        //    );
        //AllFeats.All.FirstOrDefault(feat => feat.FeatName == FeatName.AbundantSpellcastingCleric2)?.Prerequisites.Add(
        //    new Prerequisite(sheet => !sheet.HasFeat(ModFeatName.BattleHarbingerDoctrine), "This feat is not available to Battle Harbingers.")
        //    );
        AllFeats.All.FirstOrDefault(feat => feat.FeatName == FeatName.VersatileFont)?.Prerequisites.Add(
            new Prerequisite(sheet => !sheet.HasFeat(ModFeatName.BattleHarbingerDoctrine), "This feat is not available to Battle Harbingers.")
            );
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
            "{b}Divine Font:{/b} Instead of preparing {i}heal{/i} or {i}harm{/i} spells with your divine font, you gain the battle font, which allows you to prepare {i}battle auras{i} - special versions of aura spells such as {i}bless{/i} and {i}bane{/i} that use your class DC instead of your spell DC. You gain access to the {i}battle bless{/i} and {i}battle bane{/i} battle auras, and you gain 4 additional spell slots each day at your highest level of cleric spell slots, in which you can only prepare battle auras. Any feats or effects that refer to battle auras refer to these spells, regardless of whether they were cast from your divine font spell slots or your standard spell slots. At 5th level, the number of additional slots increases to 5.",
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
                        extraAuras = [ModSpellId.BattleBenediction, ModSpellId.BattleMalediction];
                    }
                    for (int i = 1; i <= slotCount; i++)
                    {
                        sheet.PreparedSpells[Trait.Cleric].Slots.Add(new BattleFontSpellSlot(sheet.MaximumSpellLevel, $"BattleFont:{i}", extraAuras));
                    }
                };
                // Grant access to battle bless and battle bane
                sheet.ClericAdditionalPreparableSpells.Add(ModSpellId.BattleBless);
                sheet.ClericAdditionalPreparableSpells.Add(ModSpellId.BattleBane);
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
