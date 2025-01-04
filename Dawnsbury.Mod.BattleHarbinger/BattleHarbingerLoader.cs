using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CharacterBuilder.Spellcasting.Slots;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using System.Diagnostics;

namespace Dawnsbury.Mods.BattleHarbinger;

public static class BattleHarbingerLoader
{
    public static readonly Trait BattleHarbingerTrait = ModManager.RegisterTrait("Battle Harbinger", new TraitProperties("Battle Harbinger", false));

    static readonly FeatName BattleHarbingerDoctrineName = ModManager.RegisterFeatName("Battle Harbinger");

    static readonly FeatName BattleHarbingerDedicationName = ModManager.RegisterFeatName("Battle Harbinger Dedication");

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

        ModManager.RegisterNewSpell("Benediction", 1, (SpellId spellId, Creature? caster, int level, bool inCombat, SpellInformation spellInfo) =>
        {
            return ModSpells.Benediction(level);
        });

        ModManager.RegisterNewSpell("Malediction", 1, (SpellId spellId, Creature? caster, int level, bool inCombat, SpellInformation spellInfo) =>
        {
            return ModSpells.Malediction(level);
        });
    }

    static void AddFeats(IEnumerable<Feat> feats)
    {
        foreach (Feat f in feats)
        {
            ModManager.AddFeat(f);
        }
    }

    private static IEnumerable<Feat> GetClassFeats()
    {
        // Battle Harbinger Dedication
        yield return new TrueFeat(
            BattleHarbingerDedicationName,
            2,
            "You have trained extensively in combat, battlefield tactics, and stamina, focusing on being an exceptional warrior for your faith in exchange for less time studying the traditional spells and scriptures.",
            "You gain the {i}Toughness{/i} general feat. If you already have this feat, you gain another general feat of your choice.",
            [BattleHarbingerTrait]
            ).WithPrerequisite(sheet => sheet.HasFeat(BattleHarbingerDoctrineName), "This feat is only available to Battle Harbingers.")
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

    }

    private static Feat BattleHarbingerDoctrine()
    {
        return new Feat(
            BattleHarbingerDoctrineName,
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
                sheet.SelectionOptions.RemoveAll(option => option.Key == "Root:Class:ClericFeat2");
                sheet.AddSelectionOption(new SingleFeatSelectionOption("BattleHarbingerDedicationFeatSelection", "Cleric feat (Battle Harbinger)", 2, feat => feat.FeatName == BattleHarbingerDedicationName));
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
                // TODO: make the battle auras use class DC instead of spell DC, somehow (or change the subclass to increase spell DC i guess)
                // maybe a status/untyped bonus to DC that increases it to class DC
                // Remove divine font selection and feats
                sheet.SelectionOptions.RemoveAll(option => option.Name == "Divine font");
                sheet.AllFeats.RemoveAll(feat => feat.HasTrait(Trait.DivineFont));
                sheet.AtEndOfRecalculation += (CalculatedCharacterSheetValues sheet) =>
                {
                    // Grant battle font slots
                    sheet.PreparedSpells[Trait.Cleric].Slots.Add(new BattleFontSpellSlot(sheet.MaximumSpellLevel, "BattleFont:1"));
                    sheet.PreparedSpells[Trait.Cleric].Slots.Add(new BattleFontSpellSlot(sheet.MaximumSpellLevel, "BattleFont:2"));
                    sheet.PreparedSpells[Trait.Cleric].Slots.Add(new BattleFontSpellSlot(sheet.MaximumSpellLevel, "BattleFont:3"));
                    sheet.PreparedSpells[Trait.Cleric].Slots.Add(new BattleFontSpellSlot(sheet.MaximumSpellLevel, "BattleFont:4"));
                    if (sheet.CurrentLevel >= 5) sheet.PreparedSpells[Trait.Cleric].Slots.Add(new BattleFontSpellSlot(sheet.MaximumSpellLevel, "BattleFont:5"));
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
