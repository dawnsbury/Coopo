using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace Dawnsbury.Mods.Ancestries.Kitsune;

// TODO:
// level 9 feat: Fox Trick - Once per encounter, you Create a Diversion or Hide as a free action.

public static class KitsuneAncestryLoader
{
    static readonly public Trait KitsuneTrait = ModManager.RegisterTrait("Kitsune", new TraitProperties("Kitsune", true) { IsAncestryTrait = true });

    static Trait SpellFamiliaritySubfeatTrait = ModManager.RegisterTrait("spellFamiliaritySubfeat", new TraitProperties("", false));

    static FeatName FrozenWindKitsuneFeatName = ModManager.RegisterFeatName("Frozen Wind Kitsune");

    static FeatName KitsuneSpellFamiliarityFeatName = ModManager.RegisterFeatName("Kitsune Spell Familiarity");

    static Illustration BlueFlameArt = new ModdedIllustration("KitsuneAssets/blueflame.png");

    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
#if DEBUG
        //Debugger.Launch();
#endif
        ModManager.AssertV3();

        Feat KitsuneAncestry = new AncestrySelectionFeat(
            ModManager.RegisterFeatName("Kitsune"),
            description: "Kitsune are a charismatic and witty people with a connection to the spiritual that grants them many magical abilities, chiefly the power to shapechange into other forms. Whether they pass unseen among other peoples or hold their tails high, kitsune are clever observers of the societies around them.",
            traits: [Trait.Humanoid, KitsuneTrait],
            hp: 8,
            speed: 5,
            abilityBoosts: [
                new EnforcedAbilityBoost(Ability.Charisma),
                new FreeAbilityBoost()
            ],
            heritages: GetHeritages().ToList());

        

        ModManager.AddFeat(KitsuneAncestry);

        AddFeats(GetAncestryFeats());
    }

    static void AddFeats(IEnumerable<Feat> feats)
    {
        foreach (Feat f in feats)
        {
            ModManager.AddFeat(f);
        }
    }

    static IEnumerable<Feat> GetAncestryFeats()
    {
        Feat FoxfireSubfeat(DamageKind damageKind, SfxName sfx, string damageDescription) =>
            new Feat(
                    ModManager.RegisterFeatName("Foxfire" + damageKind.ToString(), damageKind.ToString()),
                    $"Your tail produces sparks of {damageDescription}.",
                    $"Your foxfire deals {damageKind.ToString().ToLower()} damage.",
                    [KitsuneTrait], null).WithPermanentQEffect(null, (QEffect self) =>
                    {
                        self.AdditionalUnarmedStrike = new Item(BlueFlameArt, "foxfire", [Trait.Unarmed, Trait.Ranged, Trait.Weapon, Trait.Magical])
                            .WithWeaponProperties(
                                new WeaponProperties("1d4", damageKind)
                                {
                                    Sfx = sfx,
                                    VfxStyle = new VfxStyle(1, Core.Animations.ProjectileKind.Arrow, BlueFlameArt)
                                }.WithMaximumRange(4).WithRangeIncrement(4));
                    });
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Foxfire"),
            level: 1,
            "A crack of your tail sparks wisps of blue energy.",
            "Choose either electricity or fire when you gain this feat. You gain a foxfire ranged unarmed attack with a maximum range of 20 feet. The attack deals 1d4 damage of the chosen type. Your foxfire is in the sling weapon group and has the magical trait. Like other unarmed attacks, you can improve this attack with handwraps of mighty blows.\n\n{b}Special{/b} If you are a frozen wind kitsune, your foxfire deals cold damage instead of electricity or fire.",
            [KitsuneTrait],
            [
                FoxfireSubfeat(DamageKind.Fire, SfxName.FireRay, "flame")
                    .WithPrerequisite((charSheet) => charSheet.Sheet.Heritage?.FeatName != FrozenWindKitsuneFeatName, "Frozen wind kitsune must have a cold foxfire."),
                FoxfireSubfeat(DamageKind.Electricity, Audio.SfxName.ShockingGrasp, "electricity")
                    .WithPrerequisite((charSheet) => charSheet.Sheet.Heritage?.FeatName != FrozenWindKitsuneFeatName, "Frozen wind kitsune must have a cold foxfire."),
                FoxfireSubfeat(DamageKind.Cold, Audio.SfxName.RayOfFrost, "frost")
                    .WithPrerequisite((charSheet) => charSheet.Sheet.Heritage?.FeatName == FrozenWindKitsuneFeatName, "Only frozen wind kitsune can have a cold foxfire.")
                ]);
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Retractable Claws"),
            level: 1,
            null,
            "You gain a claw unarmed attack that deals 1d4 slashing damage. Your claws are in the brawling group and have the agile, finesse, and unarmed traits.",
            [KitsuneTrait]).WithPermanentQEffect(null, (QEffect self) =>
            {
                self.AdditionalUnarmedStrike = new Item(IllustrationName.DragonClaws, "claw", [Trait.Weapon, Trait.Melee, Trait.Brawling, Trait.Agile, Trait.Finesse, Trait.Unarmed])
                    .WithWeaponProperties(new WeaponProperties("1d4", DamageKind.Slashing));
            });

        Feat SpellFamiliaritySubfeat(string featName, SpellId spellId)
        {
            Spell spell = AllSpells.CreateModernSpellTemplate(spellId, KitsuneTrait);
            return new Feat(
                ModManager.RegisterFeatName(featName, spell.Name),
                null,
                $"You can cast {AllSpells.CreateSpellLink(spellId, KitsuneTrait)} as a divine innate spell at will. Your spellcasting ability for this cantrip is Charisma.",
                [KitsuneTrait, SpellFamiliaritySubfeatTrait],
                null).WithIllustration(spell.Illustration).WithRulesBlockForSpell(spellId).WithOnCreature(delegate (Creature cr)
                {
                    cr.GetOrCreateSpellcastingSource(SpellcastingKind.Innate, KitsuneTrait, Ability.Charisma, Trait.Divine).WithSpells([spellId], cr.MaximumSpellRank);
                });
        }
        yield return new TrueFeat(
            KitsuneSpellFamiliarityFeatName,
            level: 1,
            "You've picked up a few magical tricks.",
            $"Choose {AllSpells.CreateSpellLink(SpellId.Daze, KitsuneTrait)} or {AllSpells.CreateSpellLink(SpellId.ForbiddingWard, KitsuneTrait)}. You can cast this cantrip as a divine innate spell at will. Your spellcasting ability for this cantrip is Charisma.",
            [KitsuneTrait],
            [
                SpellFamiliaritySubfeat("KitsuneSpellFamiliarityDaze", SpellId.Daze),
                SpellFamiliaritySubfeat("KitsuneSpellFamiliarityForbiddingWard", SpellId.ForbiddingWard)
                ]
            ).WithPrerequisite(sheet => sheet.Heritage?.Name != "Empty Sky Kitsune", "You already have this feat from your heritage.")
            .WithOnSheet((sheet) =>
            {
                sheet.SetProficiency(Trait.Spell, Proficiency.Trained);
            });
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Star Orb"),
            level: 1,
            "Your magic has crystallized into a spherical stone.",
            "You gain a Star Orb, a magical item that resembles a mundane stone. Once per day, you can activate the item in one of two ways:\n\n{b}Activate - Orb Restoration{/b} {icon:Action}; You recover 1d8 hit points times half your level (minimum 1d8)\n\n{b}Activate - Orb Focus{/b} {icon:Action}; You restore 1 focus point to your focus pool, up to your usual maximum.",
            [KitsuneTrait]
            ).WithOnCreature((CalculatedCharacterSheetValues sheet, Creature cr) =>
            {
                if (cr.PersistentUsedUpResources.UsedUpActions.Contains("StarOrb")) return;
                else cr.AddQEffect(new QEffect("Star Orb", $"Once per day, drain your star orb to regain {S.HeightenedVariable(cr.MaximumSpellRank, 1)}d8 HP or recover 1 focus point.")
                {
                    Id = StarOrb.StarOrbQEffectId,
                    ProvideActionIntoPossibilitySection = (QEffect self, PossibilitySection section) =>
                    {
                        if (!(section.PossibilitySectionId == PossibilitySectionId.ItemActions)) return null;
                        else return new SubmenuPossibility(IllustrationName.Rock, "Star Orb")
                        {
                            Subsections = [
                                new PossibilitySection("Star Orb") {
                                    Possibilities = [
                                        new ActionPossibility(StarOrb.OrbRestoration(self.Owner)),
                                        new ActionPossibility(StarOrb.OrbFocus(self.Owner, sheet.FocusPointCount))
                                        ]
                                }
                                ]
                        };
                    }
                });
            });
        
        Feat SpellMysteriesSubfeat(string featName, SpellId spellId)
        {
            Spell spell = AllSpells.CreateModernSpellTemplate(spellId, KitsuneTrait);
            return new Feat(
                ModManager.RegisterFeatName(featName, spell.Name),
                null,
                $"You can cast {AllSpells.CreateSpellLink(spellId, KitsuneTrait)} as a divine innate spell at will. Your spellcasting ability for this spell is Charisma.",
                [KitsuneTrait],
                null).WithIllustration(spell.Illustration).WithRulesBlockForSpell(spellId).WithOnCreature(delegate (Creature cr)
                {
                    cr.GetOrCreateSpellcastingSource(SpellcastingKind.Innate, KitsuneTrait, Ability.Charisma, Trait.Divine).WithSpells([spellId], 1);
                });
        }
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Kitsune Spell Mysteries"),
            level: 5,
            "You know more kitsune magic.",
            $"{{b}}Prerequisites{{/b}} You have at least one innate kitsune spell\n\nChoose {AllSpells.CreateSpellLink(SpellId.Bane, KitsuneTrait)} or {AllSpells.CreateSpellLink(SpellId.Sanctuary, KitsuneTrait)}. You can cast this spell as a 1st-level divine innate spell once per day. Your spellcasting ability for this spell is Charisma.",
            [KitsuneTrait],
            [
                SpellMysteriesSubfeat("KitsuneSpellMysteriesBane", SpellId.Bane),
                SpellMysteriesSubfeat("KitsuneSpellMysteriesSanctuary", SpellId.Sanctuary)
                ])
            .WithPrerequisite(sheet => sheet.HasFeat(KitsuneSpellFamiliarityFeatName) || sheet.Heritage?.Name == "Empty Sky Kitsune", "You must have at least one innate kitsune spell.")
            .WithOnSheet((sheet) =>
            {
                sheet.SetProficiency(Trait.Spell, Proficiency.Trained);
            });
    }

    static IEnumerable<Feat> GetHeritages()
    {
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Earthly Wilds Kitsune"),
            "You are a creature of the material world, with an affinity closer to the wilds than urban society.",
            "You gain a jaws unarmed attack that deals 1d6 piercing damage. Your jaws are in the brawling group and have the finesse and unarmed traits."
            ).WithOnCreature(delegate (Creature cr)
            {
                cr.AddQEffect(new QEffect()
                {
                    AdditionalUnarmedStrike = new Item(IllustrationName.Jaws, "bite", [Trait.Finesse, Trait.Weapon, Trait.Melee, Trait.Unarmed, Trait.Brawling])
                    .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Piercing))
                });
            });
        yield return new HeritageSelectionFeat(
            FrozenWindKitsuneFeatName,
            "Your ancestors resided on snowy peaks.",
            "You gain cold resistance equal to half your level (minimum 1)."
            ).WithOnCreature(delegate (Creature cr)
            {
                cr.AddQEffect(QEffect.DamageResistance(DamageKind.Cold, cr.MaximumSpellRank));
            });
        // TODO: i dont know if this is actually working; focus spells don't trigger it and i think spells might keep ALL of their tradition traits when cast, meaning a spell that is on the primal and divine list will have the divine trait when a druid casts it. may need some redo-ing, or maybe its actually fine - needs to be tested.
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Celestial Envoy Kitsune"),
            "Whether due to a deity's grace or faithful forebears, you have a strong connection to the divine, affording you certain protections.",
            "You gain the Invoke Celestial Privilege reaction.\n\n{b}Invoke Celestial Privilege{/b} {icon:Reaction}\n{b}Trigger{/b} You attempt a saving throw against a divine effect.\nYou rise above the triggering effect, refusing to be harmed by it. You gain a +1 circumstance bonus to the triggering saving throw and to any other saving throws you attempt against divine effects until the start of your next turn."
            ).WithOnCreature(delegate (Creature cr)
            {
                cr.AddQEffect(new QEffect("Invoke Celestial Privilege {icon:Reaction}", "Gain a +1 bonus to saving throws against divine effects until the start of your next turn.")
                {
                    BeforeYourSavingThrow = async (QEffect self, CombatAction action, Creature you) =>
                    {
                        // action must be divine and have a saving throw
                        if (!action.HasTrait(Trait.Divine)) return;
                        if (action.SavingThrow == null) return;
                        if (!action.SavingThrow.Defense.IsSavingThrow()) return;
                        bool takeReaction = await you.AskToUseReaction("You are about to make a saving throw against a divine effect.\nDo you want to use {b}Invoke Celestial Privilege{/b} to gain a +1 circumstance bonus on all such saves until the start of your next turn?");
                        if (!takeReaction) return;
                        you.Overhead("Celestial Privilege", Color.Gold, $"{you.Name} invokes their Celestial Privilege against {action.Name}, gaining a +1 bonus against divine effects.");
                        cr.AddQEffect(new QEffect("Celestial Privilege", "You have a +1 circumstance bonus to saving throws against divine effects until the start of your next turn.")
                        {
                            BonusToDefenses = (QEffect self, CombatAction? action, Defense defense) => action?.HasTrait(Trait.Divine) == true ? new Bonus(1, BonusType.Circumstance, "Celestial Privilege") : null,
                            ExpiresAt = ExpirationCondition.ExpiresAtStartOfYourTurn,
                            Illustration = IllustrationName.Sanctuary
                        });
                    }
                });
            });
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Dark Fields Kitsune"),
            "You can exert your unsettling presence to subtly Demoralize others.",
            "You gain the Intimidating Glare general feat, which removes the -4 penalty when attempting to Demoralize a creature that doesn't understand your language. You also gain the Invigorating Fear reaction.\n\n{b}Invigorating Fear{/b} {icon:Reaction}\n{b}Frequency{/b} Once per encounter\n{b}Trigger{/b} A creature within 60 feet gains the frightened condition.\nYou are invigorated by the shock of a prank or the thrum of terror. You gain temporary Hit Points equal to the creature's level or 3, whichever is higher. You lose any temporary Hit Points after 1 minute."
            ).WithOnSheet(delegate (CalculatedCharacterSheetValues sheet)
            {
                sheet.GrantFeat(FeatName.IntimidatingGlare);
            }).WithOnCreature(delegate (Creature cr)
            {
                QEffect kitsuneEffect = new QEffect("Invigorating Fear {icon:Reaction}", "Gain temporary Hit Points in response to a nearby creature becoming frightened.");
                kitsuneEffect.AddGrantingOfTechnical((cr) => true, (QEffect otherEffect) =>
                {
                    otherEffect.AfterYouAcquireEffect = async (QEffect otherEffect, QEffect received) =>
                    {
                        Creature kitsune = kitsuneEffect.Owner;
                        Creature other = otherEffect.Owner;
                        if (received.Id != QEffectId.Frightened) return;
                        int tempHP = Math.Max(otherEffect.Owner.Level, 3);
                        if (!await kitsune.AskToUseReaction($"A nearby creature ({other.Name}) has become frightened.\nUse Invigorating Fear to gain {tempHP} temporary Hit Points?")) return;
                        kitsune.GainTemporaryHP(tempHP);
                        kitsuneEffect.ExpiresAt = ExpirationCondition.Immediately;
                    };
                });

                cr.AddQEffect(kitsuneEffect);
            });
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Empty Sky Kitsune"),
            "Your spirit is open to the secrets of beyond, granting you greater access to kitsune magic.",
            $"You gain the {{b}}Kitsune Spell Familiarity{{/b}} ancestry feat (gain {AllSpells.CreateSpellLink(SpellId.Daze, KitsuneTrait)} or {AllSpells.CreateSpellLink(SpellId.ForbiddingWard, KitsuneTrait)} as an innate cantrip)."
            ).WithOnSheet((CalculatedCharacterSheetValues sheet) =>
            {
                sheet.AddSelectionOptionRightNow(new SingleFeatSelectionOption("emptySkySpellSelection", "Kitsune Spell Familiarity spell choice", 0, (Feat feat) => feat.HasTrait(SpellFamiliaritySubfeatTrait)));
            });
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Unusual Kitsune"),
            "You're not quite like the other kitsune.",
            "You have two free ability boosts instead of a kitsune's normal ability boosts."
            ).WithOnSheet(sheet =>
            {
                sheet.AbilityBoostsFabric.AncestryBoosts = [
                        new FreeAbilityBoost(),
                        new FreeAbilityBoost()
                    ];
            });
    }
}
