using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core;
using System.Diagnostics;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Mechanics.Targeting.Targets;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Tiles;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Display.Text;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Auxiliary;
using Dawnsbury.Audio;
using Microsoft.Xna.Framework;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.Creatures.Parts;

namespace Dawnsbury.Mods.Ancestries.Tengu;

// TODO: port some of the nice QoL stuff to the other projects. That is:
// * the copying stuff in the project file
// * the preprocessor directive stuff and v2/v3 stuff, when thats in there

public static class TenguAncestryLoader
{
    public static readonly Trait TenguTrait = ModManager.RegisterTrait("Tengu", new TraitProperties("Tengu", true) { IsAncestryTrait = true });

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

        Feat TenguAncestry = new AncestrySelectionFeat(
            ModManager.RegisterFeatName("Tengu"),
            description: "Tengus are gregarious and resourceful avian humanoids who collect knowledge and treasures alike. They are natural survivalists and conversationalists, equally at home living off the wilderness and finding a niche in dense cities. Tengu are known to accumulate knowledge, tools, and companions, adding them to their collection as they travel.\n\n{b}Sharp Beak{/b} With your sharp beak, you are never without a weapon. You have a beak unarmed attack that deals 1d6 piercing damage. Your beak is in the brawling weapon group and has the finesse and unarmed traits.",
            traits: [Trait.Humanoid, TenguTrait],
            hp: 6,
            speed: 5,
            abilityBoosts: [
                new EnforcedAbilityBoost(Ability.Dexterity),
                new FreeAbilityBoost()
            ],
            heritages: GetHeritages().ToList())
            .WithOnCreature((Creature cr) =>
            {
                cr.AddQEffect(new QEffect()
                {
                    AdditionalUnarmedStrike = new Item(new ModdedIllustration("TenguAssets/beak.png"), "beak", [Trait.Finesse, Trait.Weapon, Trait.Melee, Trait.Unarmed, Trait.Brawling])
                    .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Piercing))
                });
            });

        ModManager.AddFeat(TenguAncestry);

        AddFeats(GetAncestryFeats());

        Items.RegisterItems();
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
        Spell produceFlame = AllSpells.CreateModernSpellTemplate(SpellId.ProduceFlame, TenguTrait);
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Mariner's Fire"),
            1,
            "You conjure uncanny orbs of spiritual flame that float above or below the water's surface.",
            $"You can cast the {produceFlame.ToSpellLink()} cantrip as a primal innate spell at will. Your spellcasting ability for this spell is Charisma. You can cast this cantrip underwater.",
            [TenguTrait]
            ).WithIllustration(produceFlame.Illustration).WithRulesBlockForSpell(produceFlame.SpellId).WithOnCreature(delegate (Creature cr)
            {
                cr.GetOrCreateSpellcastingSource(SpellcastingKind.Innate, TenguTrait, Ability.Charisma, Trait.Primal).WithSpells([produceFlame.SpellId], cr.MaximumSpellRank);
            }).WithOnSheet(calculatedSheet =>
            {
                calculatedSheet.SetProficiency(Trait.Spell, Proficiency.Trained);
            });
        yield return new TrueFeat(
            ModManager.RegisterFeatName("One-Toed Hop", "One-Toed Hop {icon:Action}"),
            1,
            "Assuming a peculiar stance, you make a short hop on each toe.",
            "You make a short 5ft Leap which does not trigger reactions that are triggered by movement, such as Attack of Opportunity.\n\n{b}Special{/b} If you also have the Powerful Leap feat, your Leap goes 5ft further.",
            [TenguTrait]
            ).WithOnCreature((Creature cr) =>
            {
                cr.AddQEffect(new QEffect("One-Toed Hop {icon:Action}", "Make a short Leap which doesn't trigger reactions to movement.")
                {
                    ProvideActionIntoPossibilitySection = (QEffect self, PossibilitySection section) =>
                    {
#if DAWNSBURY_V2
                        PossibilitySectionId location = PossibilitySectionId.OtherManeuvers;
#else
                        PossibilitySectionId location = PossibilitySectionId.Movement;
#endif
                        if (section.PossibilitySectionId == location)
                        {
                            return new ActionPossibility(OneToedHop(cr));
                        }
                        else return null;
                    }
                });
            });
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Scavenger's Search"),
            1,
            "You're always on the lookout for supplies and valuables.",
            "You gain a +2 circumstance bonus to locate objects (such as secret doors and hazards) you search for within 30 feet with a Seek action.",
            [TenguTrait]
            ).WithOnCreature((Creature cr) =>
            {
                cr.AddQEffect(new QEffect("Scavenger's Search", "You have a +2 circumstance bonus when Seeking objects.")
                {
                    // TODO: i think the new update added a way to make this work smoother
                    BonusToAttackRolls = (QEffect self, CombatAction action, Creature? target) =>
                    {
                        if (target == null) return null;
                        // triggers on Seek actions, and Pseudocreature is the secret sauce that indicates it's a tile being seeked
                        if (action.ActionId == ActionId.Seek && target.HasTrait(Trait.Pseudocreature)) return new Bonus(2, BonusType.Circumstance, "Scavenger's Search");
                        else return null;
                    }
                });
            });
        // Cannot be implemented in v2; RerollActiveRoll isn't present
#if !DAWNSBURY_V2
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Squawk!", "Squawk! {icon:Reaction}"),
            1,
            "You let out an awkward squawk, ruffle your feathers, or fake some other birdlike tic to cover up a poor attempt to intimidate.",
            "{b}Frequency{/b} Once per day\n{b}Trigger{/b} You fail or critically fail an Intimidation check to Demoralize a creature without the tengu trait\n\nReroll the failed Intimidation check and keep the better result.",
            [TenguTrait]
            ).WithOnCreature((Creature cr) =>
            {
                if (cr.PersistentUsedUpResources.UsedUpActions.Contains("squawk")) return;

                cr.AddQEffect(new QEffect("Squawk! {icon:Reaction}", "Reroll a failed check to Demoralize.")
                {
                    RerollActiveRoll = async (QEffect self, CheckBreakdownResult result, CombatAction action, Creature target) =>
                    {
                        if (action.ActionId != ActionId.Demoralize) return RerollDirection.DoNothing;
                        if (result.CheckResult == CheckResult.Success || result.CheckResult == CheckResult.CriticalSuccess) return RerollDirection.DoNothing;
                        bool playerConfirms = await self.Owner.AskToUseReaction("You failed your check to Demoralize. Do you want to use {b}Squawk!{/b} to reroll the check and take the better result?");
                        if (!playerConfirms) return RerollDirection.DoNothing;
                        self.ExpiresAt = ExpirationCondition.Immediately;
                        self.Owner.PersistentUsedUpResources.UsedUpActions.Add("squawk");
                        return RerollDirection.RerollAndKeepBest;
                    }
                });
            });
#endif
        Spell electricArc = AllSpells.CreateModernSpellTemplate(SpellId.ElectricArc, TenguTrait);
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Storm's Lash"),
            1,
            "Wind and lightning have always been friends to you.",
            $"You can cast the {electricArc.ToSpellLink()} cantrip as a primal innate spell at will. Your spellcasting ability for this spell is Charisma.",
            [TenguTrait]
            ).WithIllustration(electricArc.Illustration).WithRulesBlockForSpell(electricArc.SpellId).WithOnCreature(delegate (Creature cr)
            {
                cr.GetOrCreateSpellcastingSource(SpellcastingKind.Innate, TenguTrait, Ability.Charisma, Trait.Primal).WithSpells([electricArc.SpellId], cr.MaximumSpellRank);
            }).WithOnSheet(calculatedSheet =>
            {
                calculatedSheet.SetProficiency(Trait.Spell, Proficiency.Trained);
            });
        // Tengu Weapon Familiarity and Subfeats
        List<Trait> familiarWeapons = [Items.Katana, Items.Khakkara, Items.TempleSword, Items.Wakizashi, Items.TenguGaleBlade];
        Trait TenguWeaponFamiliaritySwordChoiceTrait = ModManager.RegisterTrait("TenguWeaponFamiliaritySwordChoiceTrait", new TraitProperties("TWFCT", false));
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Tengu Weapon Familiarity"),
            1,
            "You have eclectic experience with all sorts of weapons.",
            "You have familiarity with all weapons with the tengu trait, plus the katana, khakkara, temple sword, and wakizashi. For the purpose of proficiency, you treat any of these that are martial weapons as simple weapons and any that are advanced weapons as martial weapons.\n\nIn addition, you may choose another weapon of your choice from the sword group: You are also familiar with this weapon, and gain the same benefits.\n\nAt 5th level, whenever you get a critical hit with one of these weapons, you get its {tooltip:criteffect}critical specialization effect{/}.",
            [TenguTrait]
            ).WithOnSheet((calculatedSheet) =>
            {
                foreach (Trait t in familiarWeapons)
                {
                    calculatedSheet.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(t) && traits.Contains(Trait.Martial), Trait.Simple);
                    calculatedSheet.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(t) && traits.Contains(Trait.Advanced), Trait.Martial);
                }
                calculatedSheet.AddSelectionOptionRightNow(
                    new SingleFeatSelectionOption("TenguWeaponFamiliaritySwordChoice", "Tengu Weapon Familiarity", 1,
                        feat => feat.HasTrait(TenguWeaponFamiliaritySwordChoiceTrait)
                        ).WithIsOptional()
                    );
            }).WithOnCreature((Creature cr) =>
            {
                // grant crit spec with listed weapons at level 5
#if !DAWNSBURY_V2
                if (cr.Level < 5) return;
                cr.AddQEffect(new QEffect()
                {
                    YouHaveCriticalSpecialization = (QEffect self, Item weapon, CombatAction _, Creature _) => familiarWeapons.Any(trait => weapon.HasTrait(trait))
                });
#endif
            });
        foreach (Item item in Core.Mechanics.Treasure.Items.ShopItems)
        {
            if (!item.HasTrait(Trait.Sword)) continue;
            if (item.MainTrait == Trait.None) continue;
#if DAWNSBURY_V2
            if (item.ItemModifications.Count != 0) continue;
#else
            if (item.Runes.Count != 0) continue;
#endif
            if (familiarWeapons.Where(weaponTrait => item.HasTrait(weaponTrait)).Any()) continue; // if this weapon is anything already covered by the base feat, don't list it
            if (item.HasTrait(Trait.Simple)) continue; // this feat doesn't achieve anything for simple weapons
            yield return new Feat(
                ModManager.RegisterFeatName($"TenguWeaponFamiliarity:{item.Name}", item.Name.Capitalize()),
                $"You have experience with {item.Name}s.",
                $"For the purpose of proficiency, you treat {item.Name}s as {(item.HasTrait(Trait.Advanced) ? "martial" : "simple")} weapons.",
                traits: [TenguWeaponFamiliaritySwordChoiceTrait],
                subfeats: null).WithOnSheet((calculatedSheet) =>
                {
                    calculatedSheet.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(item.MainTrait) && traits.Contains(Trait.Martial), Trait.Simple);
                    calculatedSheet.Proficiencies.AddProficiencyAdjustment(traits => traits.Contains(item.MainTrait) && traits.Contains(Trait.Advanced), Trait.Martial);
                }).WithOnCreature((Creature cr) =>
                {
                    // grant crit spec with chosen weapon at level 5
#if !DAWNSBURY_V2
                    if (cr.Level < 5) return;
                    cr.AddQEffect(new QEffect()
                    {
                        YouHaveCriticalSpecialization = (QEffect self, Item weapon, CombatAction _, Creature _) => weapon.HasTrait(item.MainTrait)
                    });
#endif
                });
        }
        // end of Tengu Weapon Familiarity
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Uncanny Agility"),
            1,
            "You have near-supernatural poise that lets you move swiftly across the most unsteady surfaces.",
            "You gain the Feather Step skill feat, which allows you to Step into difficult terrain.",
            [TenguTrait]
            ).WithOnSheet((calculatedSheet) =>
            {
                calculatedSheet.GrantFeat(FeatName.FeatherStep);
            });
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Waxed Feathers"),
            1,
            "Your feathers are coated in a waxy substance that repels water.",
            "You gain a +1 circumstance bonus to saving throws against effects that have the water trait.",
            [TenguTrait]
            ).WithPermanentQEffect((QEffect self) =>
            {
                self.Name = "Waxed Feathers";
                self.Description = "You have a +1 circumstance bonus to saving throws against water effects.";
                self.BonusToDefenses = (QEffect self, CombatAction? action, Defense defense) =>
                {
                    if (!defense.IsSavingThrow()) return null;
                    else if (action != null && action.HasTrait(Trait.Water)) return new Bonus(1, BonusType.Circumstance, "Waxed Feathers");
                    else return null;
                };
            }).WithPrerequisite(calculatedSheet => calculatedSheet.Sheet.Heritage?.Name == "Wavediver Tengu", "You must be a Wavediver Tengu to choose this feat.");
        // TODO: add 5th level feats (and in the other ancestry mods, too!)
    }

    static IEnumerable<Feat> GetHeritages()
    {
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Dogtooth Tengu"),
            "In addition to a beak, your mouth also features a number of vicious, pointed teeth. Some legends claim your powerful jaws can even bite through steel. While you aren't that strong yet, your fangs can still leave terrible wounds.",
            "Your beak unarmed attack gains the deadly d8 trait."
            ).WithOnCreature(delegate (Creature cr)
            {
                cr.GetAttackItem("beak")?.Traits.Add(Trait.DeadlyD8);
            });
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Jinxed Tengu"),
            "Your lineage has been exposed to curse after curse, and now they slide off your feathers like rain.",
            "If you succeed at a saving throw against a curse effect, you get a critical success instead. When you would gain the doomed condition, attempt a DC 17 flat check. On a success, reduce the value of the doomed condition you would gain by 1."
            ).WithOnCreature(delegate (Creature cr)
            {
                cr.AddQEffect(new QEffect("Jinxed Tengu", "When saving against curse effects, your successes are upgraded to critical successes. When you gain the doomed condition, make a DC17 flat check to reduce the gained value by 1.")
                {
#if DAWNSBURY_V2
                    AdjustSavingThrowResult = (QEffect self, CombatAction action, CheckResult result) =>
                    {
                        if (action.HasTrait(Trait.Curse) && action.SavingThrow != null && action.SavingThrow.Defense.IsSavingThrow() && result == CheckResult.Success)
                            return CheckResult.CriticalSuccess;
                        else return result;
                    }
                    // Doomed effect only exists in v3, and AdjustSavingThrowCheckResult is not in v3
#else
                    AdjustSavingThrowCheckResult = (QEffect self, Defense defense, CombatAction action, CheckResult result) =>
                    {
                        if (action.HasTrait(Trait.Curse) && defense.IsSavingThrow() && result == CheckResult.Success)
                            return CheckResult.CriticalSuccess;
                        else return result;
                    },
                    YouAcquireQEffect = (QEffect self, QEffect applied) =>
                    {
                        if (applied.Id == QEffectId.Doomed)
                        {
                            (CheckResult result, string breakdown) = Checks.RollFlatCheck(17);
                            if (result == CheckResult.Success || result == CheckResult.CriticalSuccess)
                            {
                                self.Owner.Occupies.Overhead("Jinx!", Color.DarkGreen, self.Owner.Name + " reduced an incoming doomed condition by 1 via Jinxed Tengu.", "Jinxed Tengu", "DC 17 flat check = " + breakdown);
                                applied.Value--;
                                if (applied.Value == 0) return null;
                                else return applied;
                            }
                            else
                            {
                                self.Owner.Battle.Log(self.Owner.Name + "'s Jinxed Tengu failed to reduce an incoming doomed condition.", "Jinxed Tengu", "DC 17 flat check = " + breakdown);
                                return applied;
                            }
                        }
                        else
                        {
                            return applied;
                        }
                    }
#endif
                });
            });
        Spell disruptUndead = AllSpells.CreateModernSpellTemplate(SpellId.DisruptUndead, TenguTrait);
        Trait MountainkeeperTraditionSelectionFeat = ModManager.RegisterTrait("Mountainkeeper Tradition Selection Feat", new TraitProperties("", false));
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Mountainkeeper Tengu"),
            "You come from a line of tengu ascetics, leaving you with a link to the spirits of the world.",
            $"You can cast the {disruptUndead.ToSpellLink()} cantrip as a primal innate spell at will. Your spellcasting ability for this spell is Charisma."
            ).WithOnCreature((Creature cr) =>
            {
                cr.GetOrCreateSpellcastingSource(SpellcastingKind.Innate, TenguTrait, Ability.Charisma, Trait.Primal).WithSpells([disruptUndead.SpellId], cr.MaximumSpellRank);
            }).WithOnSheet((calculatedSheet) =>
            {
                calculatedSheet.SetProficiency(Trait.Spell, Proficiency.Trained);
            });
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Skyborn Tengu"),
            "Your bones may be especially light, you may be a rare tengu with wings, or your connection to the spirits of wind and sky might be stronger than most, slowing your descent through the air.",
            $"You gain a +1 circumstance bonus to saving throws against effects that have the air trait, and if you roll a success on a save against an air effect, you get a critical success instead. In addition, you gain the Powerful Leap feat."
            ).WithOnSheet((calculatedSheet) =>
            {
                calculatedSheet.GrantFeat(FeatName.PowerfulLeap);
            }).WithOnCreature((Creature cr) =>
            {
                cr.AddQEffect(new QEffect("Skyborn Tengu", "You have a +1 bonus to saves against air effects, and successful saves become critical successes.")
                {
#if DAWNSBURY_V2
                    AdjustSavingThrowResult = (QEffect self, CombatAction action, CheckResult result) =>
                    {
                        if (action.HasTrait(Trait.Air) && action.SavingThrow != null && action.SavingThrow.Defense.IsSavingThrow() && result == CheckResult.Success)
                            return CheckResult.CriticalSuccess;
                        else return result;
                    },
#else
                    AdjustSavingThrowCheckResult = (QEffect self, Defense defense, CombatAction action, CheckResult result) =>
                    {
                        if (action.HasTrait(Trait.Air) && defense.IsSavingThrow() && result == CheckResult.Success)
                            return CheckResult.CriticalSuccess;
                        else return result;
                    },
#endif
                    BonusToDefenses = (QEffect self, CombatAction? action, Defense defense) =>
                    {
                        if (!defense.IsSavingThrow()) return null;
                        else if (action != null && action.HasTrait(Trait.Air)) return new Bonus(1, BonusType.Circumstance, "Skyborn Tengu");
                        else return null;
                    }
                });
            });
        // TODO: maybe add more to this heritage later to replace the missing concealment benefit
        // maybe a reaction or triggered free action that lets you ignore concealment once per fight or something
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Stormtossed Tengu"),
            "Whether due to a storm god's blessing or hatching from your egg during a squall, you are resistant to storms.",
            "You gain electricity resistance equal to half your level (minimum 1)."
            ).WithOnCreature(delegate (Creature cr)
            {
                int resistance = Math.Max(1, cr.Level / 2);
                cr.WeaknessAndResistance.AddResistance(DamageKind.Electricity, resistance);
            });
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Taloned Tengu"),
            "Your talons are every bit as sharp and strong as your beak.",
            "You gain a talons unarmed attack that deals 1d4 slashing damage. Your talons are in the brawling group and have the agile, finesse, unarmed, and versatile P traits."
            ).WithOnCreature(delegate (Creature cr)
            {
                cr.AddQEffect(new QEffect()
                {
                    AdditionalUnarmedStrike = new Item(new ModdedIllustration("TenguAssets/talons.png"), "talons", [Trait.Brawling, Trait.Agile, Trait.Finesse, Trait.Weapon, Trait.Melee, Trait.Unarmed, Trait.VersatileP])
                    .WithWeaponProperties(new WeaponProperties("1d4", DamageKind.Slashing)).WithSoundEffect(SfxName.Fist2)
                });
            });
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Wavediver Tengu"),
            "You're one of the rare tengu who can cut through water like a bird through air, and you often lurk in rivers or oceans where few expect you.",
            "You gain a swim Speed. This allows you to:\n• Move through deep water tiles.\n• Ignore difficult terrain imposed by shallow water.\n• Ignore the flat-footed and difficult terrain penalties from being underwater."
            ).WithOnCreature(delegate (Creature cr)
            {
                cr.AddQEffect(QEffect.Swimming());
                //cr.AddQEffect(new QEffect("Swimming", "While swimming, your speed is reduced to 15ft.")
                //{
                //    Id = QEffectId.Swimming,
                //    BonusToAllSpeeds = (QEffect self) =>
                //    {
                //        // reduce speed to 15ft, or apply no reduction if speed is already that low
                //        int speedDiffWhenSwimming = Math.Min(3 - self.Owner.BaseSpeed, 0);
                //        // if in deep water, shallow water, or underwater, reduce speed to 15ft
                //        if (self.Owner.Occupies.Kind == TileKind.Water || self.Owner.Occupies.Kind == TileKind.ShallowWater)
                //        {
                //            return new Bonus(speedDiffWhenSwimming, BonusType.Untyped, "Swimming");
                //        }
                //        // allow to move at the speed of difficult terrain when in aquatic combat, in case the base speed halved is more than the swim speed
                //        // (because it would be weird for a fast swimming race to be slower at swimming than anyone else)
                //        if (self.Owner.FindQEffect(QEffectId.AquaticCombat) != null)
                //        {
                //            speedDiffWhenSwimming = Math.Max(speedDiffWhenSwimming, self.Owner.BaseSpeed / 2);
                //            return new Bonus(speedDiffWhenSwimming, BonusType.Untyped, "Swimming");
                //        }
                //        else return null;
                //    }
                //});
            });
    }

    static CombatAction OneToedHop(Creature self)
    {

        int leapDistance = self.HasEffect(QEffectId.PowerfulLeap) ? 2 : 1;
        return new CombatAction(self, IllustrationName.Jump, "One-Toed Hop",
            [Trait.Move, Trait.Basic],
            "{i}Assuming a peculiar stance, you make a short hop on each toe.{/i}\n\nMake a short " + S.HeightenedVariable(leapDistance * 5, 5) + "ft Leap which does not trigger reactions which are triggered by movement.",
            new TileTarget((Creature jumper, Tile tile) => jumper.Occupies != null && tile.IsTrulyGenuinelyFreeTo(jumper) && jumper.DistanceTo(tile) <= leapDistance && jumper.Occupies.HasLineOfEffectToIgnoreLesser(tile) != CoverKind.Blocked, null)
            ).WithEffectOnChosenTargets(async delegate (CombatAction action, Creature jumper, ChosenTargets target)
            {
                if (target.ChosenTile == null) return;
                await jumper.SingleTileMove(target.ChosenTile, action);
            }).WithActionId(ActionId.Leap);
    }
}
