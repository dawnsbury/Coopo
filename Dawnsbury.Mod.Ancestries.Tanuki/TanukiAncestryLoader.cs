using Dawnsbury.Core.CharacterBuilder.AbilityScores;
using Dawnsbury.Core.CharacterBuilder.Feats;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Modding;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core;
using Microsoft.Xna.Framework;
using System.Diagnostics;
using Dawnsbury.Audio;
using Dawnsbury.Auxiliary;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Display.Text;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.CharacterBuilder;
using System;

namespace Dawnsbury.Mods.Ancestries.Tanuki;

// TODO:
// * Homebrew some feats:
//   * tanuki weapon familiarity; new tanuki weapon Umbrella, quarterstaff, halfling frying pan? or maybe more staffs?
//   * turn statue form into some kind of stance, something like mountain stance. or maybe just natural armor
//   * Turn Failure Into Joke, Look on the Bright Side, Find Good in Bad, Get Serious. Tanuki 5. Whenever you roll a critical failure on an attack, as a reaction, you can gain temporary HP equal to your level.

// non-tanuki TODO:
// * dragonblood versatile heritage? could even use the dawnsbury dragon types, would be cool
// * spirit warrior archetype
// * bring kitsune up to 5th level, too
// * oscillating wave psychic subclass?
public static class TanukiAncestryLoader
{
    static readonly Trait TanukiTrait = ModManager.RegisterTrait("Tanuki", new TraitProperties("Tanuki", true) { IsAncestryTrait = true });

    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
#if DEBUG
        Debugger.Launch();
#endif
        ModManager.AssertV3();

        Feat TanukiAncestry = new AncestrySelectionFeat(
            ModManager.RegisterFeatName("Tanuki"),
            description: "These shapeshifting raccoon dog–like humanoids use their powers of illusion and transformation in ways more people should: for fun! Tanuki delight in pranks and practical jokes, especially those that allow them to take the high and mighty down a notch and show them what life is like for everyone else. Where other peoples take pride in their storied histories, noble traditions, or intricate ceremonies, tanuki take pride in their simplicity and disregard for the world's many rules. Though some might claim this outlook reduces tanuki to uncouth rubes, tanuki feel it makes them more cultured; after all, one must know a rule to bend it, and one must understand a norm to break it.",
            traits: [Trait.Humanoid, TanukiTrait],
            hp: 10,
            speed: 5,
            abilityBoosts: [
                new EnforcedAbilityBoost(Ability.Constitution),
                new EnforcedAbilityBoost(Ability.Charisma),
                new FreeAbilityBoost()
            ],
            heritages: GetHeritages().ToList())
            .WithAbilityFlaw(Ability.Wisdom);

        ModManager.AddFeat(TanukiAncestry);

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
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Iron Belly"),
            1,
            "A good laugh comes from the belly, and by laughing every day, yours has grown quite strong.",
            "You have a belly melee unarmed Strike, which deals 1d6 damage, is in the brawling group, and has the forceful trait.",
            [TanukiTrait]
            ).WithOnCreature((Creature cr) =>
            {
                cr.AddQEffect(new QEffect()
                {
                    AdditionalUnarmedStrike = new Item(IllustrationName.ShakeItOff, "belly", [Trait.Forceful, Trait.Weapon, Trait.Melee, Trait.Unarmed, Trait.Brawling])
                    .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Bludgeoning)).WithSoundEffect(SfxName.Fist2)
                });
            });

        yield return new TrueFeat(
            ModManager.RegisterFeatName("Scorched on the Crackling Mountain"),
            1,
            "By ritualistically marking your fur with fire, like an infamous tanuki of legend, you protect yourself against future flames. You gain a black stripe down your back that looks charred.",
            "Your flat check to remove persistent fire damage is reduced by 5, to DC 10 (or DC 5 with assistance). The first time each day you would be reduced to 0 Hit Points by a fire effect, you avoid being knocked out and remain at 1 Hit Point, and your wounded condition increases by 1.",
            [TanukiTrait]
            ).WithOnCreature(delegate (Creature cr)
            {
                cr.AddQEffect(new QEffect("Scorched Tanuki", "Your flat check to remove persistent fire damage is DC 10 (DC 5 with assistance).")
                {
                    Innate = true,
                    ReducesPersistentDamageRecoveryCheckDc = (QEffect self, QEffect inflictor, DamageKind damageKind) => damageKind == DamageKind.Fire
                });
                if (!cr.PersistentUsedUpResources.UsedUpActions.Contains("Crackling Mountain"))
                {
                    cr.AddQEffect(new QEffect("Crackling Mountain", "The first time each day you are reduced to 0 Hit Points by a fire effect, you remain at 1 Hit Point and increase your wounded condition by 1 instead of being knocked out.")
                    {
                        Innate = true,
                        YouAreDealtLethalDamage = async (QEffect qEffect, Creature attacker, DamageStuff damageStuff, Creature you) =>
                        {
                            // only triggers when you are reduced to 0HP, not when you are dying
                            // (will not be necessary in an upcoming update)
                            if (damageStuff.Kind == DamageKind.Fire && you.HP > 0)
                            {
                                you.Occupies.Overhead("kachi-kachi!!", Color.Red, you.ToString() + " resists dying through Scorched on the Crackling Mountain!");
                                you.IncreaseWounded();
                                you.PersistentUsedUpResources.UsedUpActions.Add("Crackling Mountain");
                                qEffect.ExpiresAt = ExpirationCondition.Immediately;
                                int damage = you.HP - 1; // damage to reduce the player to 1hp
                                return new SetToTargetNumberModification(damage, "Crackling Mountain");
                            }
                            else return null;
                        }
                    });
                }
            });

        yield return new TrueFeat(
            ModManager.RegisterFeatName("Ponpoko"),
            1,
            "If you play especially vigorously, the music of your belly drum can physically wound your foes.",
            "You deal 1d4 sonic damage to all creatures in a 15-foot cone, with a basic Fortitude save equal to your spell DC or class DC, whichever is higher. Such vigorous drumming does leave your belly a bit sore, though, preventing you from using this ability again for 1d4 rounds.\n\nAt 3rd level and every 2 levels thereafter, the damage increases by 1d4.",
            [TanukiTrait, Trait.Primal, Trait.Sonic, Trait.Homebrew]
            ).WithOnCreature(cr =>
            {
                cr.AddQEffect(new QEffect("Ponpoko", $"Drum your belly to deal {S.HeightenedVariable(cr.MaximumSpellRank, 1)}d4 damage in a 15ft cone.")
                {
                    ProvideMainAction = (self) => new ActionPossibility(Ponpoko(cr)).WithPossibilityGroup(Constants.POSSIBILITY_GROUP_ADDITIONAL_NATURAL_STRIKE)
                });
            });

        yield return new TrueFeat(
            ModManager.RegisterFeatName("Hasty Celebration {icon:Reaction}"),
            5,
            "After even the briefest success, you get caught up in the moment and begin to party, cheering your allies on.",
            "{b}Frequency{/b} Once per encounter\n{b}Trigger{/b} You critically succeed at an attack roll against an enemy, or an enemy critically fails their saving throw against one of your effects.\nYou grant all allies within 60 feet a +2 circumstance bonus to attack rolls and damage until the end of your next turn. Unfortunately, while you sing and dance, you aren't keeping an eye on your surroundings like you should, making you flat-footed to all enemies until the end of your next turn as well.",
            [TanukiTrait]
            ).WithOnCreature(delegate (Creature tanuki)
            {
                // TODO: test more thoroughly
                tanuki.AddQEffect(new QEffect("Hasty Celebration {icon:Reaction}", "When your attack or effect crits, give your allies a +2 circumstance bonus to attack rolls and damage until the end of your next turn. However, you become flat-footed for this duration as well.")
                {
                    Innate = true,
                    StartOfCombat = async (qf) =>
                    {
                        var enemies = tanuki.Battle.AllCreatures.Where(cr => cr.EnemyOf(tanuki));
                        var allies = tanuki.Battle.AllCreatures.Where(cr => !cr.EnemyOf(tanuki) && cr != tanuki); // allies does not include yourself
                        List<QEffect> trackingEffects = [];
                        foreach (Creature enemy in enemies)
                        {
                            QEffect trackingEffect = new QEffect()
                            {
                                // TODO: this works *okay*, but it has some issues. for example, it doesnt work on reactions (e.g. blood vendetta crit), or on off-turn effects (e.g. bane saving throw). the reactions thing is worse, but neither thing is absolutely required. could just change the feat text to say "action" instead of "effect"
                                YouAreTargetedByARoll = async (qf, action, resultBreakdown) =>
                                {
                                    if (action.Owner != tanuki) return false;
                                    string messageStarter;
                                    if (action.SavingThrow != null && resultBreakdown.CheckResult == CheckResult.CriticalFailure)
                                        messageStarter = "An enemy critically failed a saving throw against you!";
                                    else if (action.ActiveRollSpecification != null && action.HasTrait(Trait.Attack) && resultBreakdown.CheckResult == CheckResult.CriticalSuccess)
                                        messageStarter = "You got a critical hit!";
                                    else
                                        return false;

                                    bool used = await tanuki.AskToUseReaction(messageStarter + " Spend your reaction to use {b}Hasty Celebration{/b}, granting allies a +2 circumstance bonus to attack rolls and damage until the end of your next turn, but making yourself flat-footed for that same duration?");
                                    if (!used) return false;
                                    trackingEffects.ForEach(tqf => tqf.WithExpirationEphemeral()); // once per encounter - remove triggering effect after the first use
                                    tanuki.AddQEffect(QEffect.FlatFooted("Hasty Celebration").WithExpirationAtEndOfSourcesNextTurn(tanuki, false));
                                    allies.ForEach(ally => ally.AddQEffect(new QEffect("Hasty Celebration", $"+2 circumstance bonus to attack rolls and damage. Expires at the  end of {tanuki.Name}'s next turn.")
                                    {
                                        Illustration = IllustrationName.WinningStreak,
                                        BonusToAttackRolls = (_, action, _) => action.HasTrait(Trait.Attack) ? new Bonus(2, BonusType.Circumstance, "Hasty Celebration", true) : null,
                                        BonusToDamage = (_, _, _) => new Bonus(2, BonusType.Circumstance, "Hasty Celebration", true)
                                    }.WithExpirationAtEndOfSourcesNextTurn(tanuki, true)));

                                    return false;
                                }
                            };
                            trackingEffects.Add(trackingEffect);
                            enemy.AddQEffect(trackingEffect);
                        }
                    },
                    
                });
            });
        // Tanuki Tenacity
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Tanuki Tenacity", "Tanuki Tenacity {icon:Action}"),
            5,
            "You spend a moment daydreaming of the celebration you'll have once victory is achieved, and the thought invigorates you.",
            "{b}Frequency {/b}Once per day\nYou gain 15 temporary Hit Points, which last until the end of the encounter.\n\nAt 7th level and every 2 levels thereafter, the temporary Hit Points increase by 5.",
            [TanukiTrait, Trait.Concentrate, Trait.Homebrew]
            ).WithOnCreature(cr =>
            {
                int tempHp = 5 * cr.MaximumSpellRank;
                cr.AddQEffect(new QEffect("Tanuki Tenacity {icon:Action}", $"Once per day, gain {S.HeightenedVariable(tempHp, 15)} temporary Hit Points.")
                {
                    ProvideMainAction = (self) => cr.PersistentUsedUpResources.UsedUpActions.Contains("Tanuki Tenacity") ? null : new ActionPossibility(
                        new CombatAction(cr, IllustrationName.EnduringMight, "Tanuki Tenacity", [TanukiTrait, Trait.Concentrate, Trait.Homebrew, Trait.Basic],
                        "{i}You spend a moment daydreaming of the celebration you'll have once victory is achieved, and the thought invigorates you.{/i}\n\n" +
                        $"{{b}}Frequency {{/b}}Once per day\nYou gain {S.HeightenedVariable(tempHp, 15)} temporary Hit Points, which last until the end of the encounter.",
                        Target.Self())
                            .WithActionCost(1)
                            .WithSoundEffect(SfxName.MinorHealing)
                            .WithEffectOnSelf(async (action, cr) =>
                            {

                                cr.GainTemporaryHP(5 * cr.MaximumSpellRank);
                                cr.PersistentUsedUpResources.UsedUpActions.Add("Tanuki Tenacity");
                            })
                        ).WithPossibilityGroup(Constants.POSSIBILITY_GROUP_ANCESTRY_POWERS)
                });
            });
        // False Priest Form
        Spell divineLance = AllSpells.CreateModernSpellTemplate(SpellId.DivineLance, TanukiTrait);
        Spell hauntingHymn = AllSpells.CreateModernSpellTemplate(SpellId.HauntingHymn, TanukiTrait);
        yield return new TrueFeat(
            ModManager.RegisterFeatName("False Priest Form"),
            5,
            "Nobody respects tanuki, but most everyone respects an esteemed priest, so what better form to take if you want to get by a little easier?",
            $"Your Religion proficiency increases to equal your Deception proficiency, and you use your Deception modifier for Religion checks if it is higher. You can cast {divineLance.ToSpellLink()} and {hauntingHymn.ToSpellLink()} as primal innate cantrips at will. Your spellcasting ability for these spells is Charisma.",
            [TanukiTrait]
            ).WithOnSheet(calculatedSheet =>
            {
                // increase religion proficiency to match deception (maybe change later to be more strong)
                calculatedSheet.AtEndOfRecalculation += (CalculatedCharacterSheetValues sheet) =>
                {
                    if (calculatedSheet.HasFeat(FeatName.ExpertDeception))
                    {
                        calculatedSheet.GrantFeat(FeatName.ExpertReligion);
                    }
                    else if (calculatedSheet.HasFeat(FeatName.Deception))
                    {
                        calculatedSheet.GrantFeat(FeatName.Religion);
                    }
                };

                calculatedSheet.SetProficiency(Trait.Spell, Proficiency.Trained);
            }).WithOnCreature(cr =>
            {
                cr.GetOrCreateSpellcastingSource(SpellcastingKind.Innate, TanukiTrait, Ability.Charisma, Trait.Primal)
                    .WithSpells([divineLance.SpellId, hauntingHymn.SpellId], cr.MaximumSpellRank);
            }).WithPermanentQEffect("You use your Deception modifier for Religion checks, if it's higher.", qEffect =>
            {
                // TODO: do SOMETHING about this to make it less jank, idk what but do something
                qEffect.BonusToAttackRolls = (qEffect, action, target) =>
                {
                    if (action.Action.ActiveRollSpecification is null) return null;
                    if (action.Action.ActiveRollSpecification.TaggedDetermineBonus.InvolvedSkill == Skill.Religion)
                    {
                        int difference = qEffect.Owner.Skills.Get(Skill.Deception) - qEffect.Owner.Skills.Get(Skill.Religion);
                        if (difference > 0) return new Bonus(difference, BonusType.Untyped, "Use Deception instead (False Priest)");
                    }
                    return null;
                };
            });
        // Statue Form
        yield return new TrueFeat(
            ModManager.RegisterFeatName("Statue Form {icon:Reaction}"),
            5,
            "Tanuki tend to be on the squishier side, but you know how to toughen up when it counts.",
            "{b}Frequency{/b} Once per day\n{b}Trigger{/b} You are hit with a Strike that deals physical damage.\nYou turn into a stone statue, gaining resistance 5 to physical damage until the beginning of your next turn.",
            [TanukiTrait, Trait.Concentrate, Trait.Primal, Trait.Polymorph, Trait.Homebrew]
            ).WithOnCreature(cr =>
            {
                int tempHp = 5 * cr.MaximumSpellRank;
                cr.AddQEffect(new QEffect("Statue Form {icon:Reaction}", $"Once per day when hit by a Strike, turn to stone, gaining resistance 5 to physical damage.")
                {
                    YouAreDealtDamageEvent = async (self, damageEvent) =>
                    {
                        if (cr.PersistentUsedUpResources.UsedUpActions.Contains("Statue Form")) return;
                        if (damageEvent.CombatAction == null) return;
                        if (!damageEvent.CombatAction.HasTrait(Trait.Strike)) return;
                        if (!damageEvent.KindedDamages.Where(kd => kd.DamageKind == DamageKind.Slashing ||
                            kd.DamageKind == DamageKind.Piercing ||
                            kd.DamageKind == DamageKind.Bludgeoning).Any()) return;
                        bool reactionUsed = await self.Owner.AskToUseReaction("You are about to take physical damage. Use Statue Form to gain resistance 5 to physical damage until the beginning of your next turn?", IllustrationName.Stoneskin);
                        if (!reactionUsed) return;
                        // TODO: need to reduce the triggering damage also
                        damageEvent.ReduceBy(5, "Statue Form");
                        cr.AddQEffect(new QEffect("Statue Form", "You have resistance 5 to physical damage until the beginning of your next turn.")
                        {
                            ExpiresAt = ExpirationCondition.ExpiresAtStartOfYourTurn,
                            StateCheck = (qEffect) =>
                            {
                                qEffect.Owner.WeaknessAndResistance.AddResistance(DamageKind.Bludgeoning, 5);
                                qEffect.Owner.WeaknessAndResistance.AddResistance(DamageKind.Slashing, 5);
                                qEffect.Owner.WeaknessAndResistance.AddResistance(DamageKind.Piercing, 5);
                            },
                            CountsAsABuff = true,
                            Illustration = IllustrationName.Stoneskin
                        });
                        cr.PersistentUsedUpResources.UsedUpActions.Add("Statue Form");
                    },
                });
            });
    }

    static IEnumerable<Feat> GetHeritages()
    {
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Even-tempered Tanuki"),
            "You're possessed of a serenity uncommon to other tanuki, who always seem to be flying off the handle.",
            "You gain a +1 circumstance bonus to saving throws against emotion effects. If you roll a success at a saving throw against an emotion effect, you get a critical success instead, but when you roll a failure at a saving throw against an emotion effect, you get a critical failure instead."
            ).WithOnCreature(delegate (Creature cr)
            {
                cr.AddQEffect(new QEffect("Even-tempered Tanuki", "You have +1 to saves against emotion effects. Successes become critical successes, but failures become critical failures.")
                {
                    BonusToDefenses = (QEffect effect, CombatAction? action, Defense defense) =>
                    {
                        if (action == null) return null;
                        if (!action.HasTrait(Trait.Emotion)) return null;
                        if (action.SavingThrow == null) return null;
                        if (!action.SavingThrow.Defense.IsSavingThrow()) return null;
                        return new Bonus(1, BonusType.Circumstance, "Even-tempered");
                    },
                    AdjustSavingThrowCheckResult = (QEffect effect, Defense defense, CombatAction action, CheckResult result) =>
                    {
                        if (!action.HasTrait(Trait.Emotion)) return result;
                        if (!defense.IsSavingThrow()) return result;
                        if (result == CheckResult.Success) return CheckResult.CriticalSuccess;
                        if (result == CheckResult.Failure) return CheckResult.CriticalFailure;
                        else return result;
                    }
                });
            });

        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Virtuous Tanuki"),
            "Many tanuki carry a gourd of alcohol to remind themselves to act with virtue, and by these standards, you're quite virtuous indeed.",
            "You gain poison resistance equal to half your level (minimum 1). You can eat and drink things when you're sickened."
            ).WithOnCreature(delegate (Creature cr)
            {
                cr.AddQEffect(QEffect.DamageResistance(DamageKind.Poison, cr.MaximumSpellRank));
                cr.AddQEffect(new QEffect("Virtuous Tanuki", "You can eat and drink when sickened.")
                {
                    YouAcquireQEffect = (QEffect effect, QEffect received) =>
                    {
                        if (received.Id == QEffectId.Sickened)
                        {
                            received.PreventTakingAction = null; // remove the function preventing the player from using potions
                            received.Name = "Sickened (Virtuous)";
                            received.Description = "You take a status penalty equal to the value to all your checks and DCs. You can still eat and drink due to your virtuous heritage.";
                            return received;
                        }
                        else
                        {
                            return received;
                        }
                    }
                });
            });

        // TODO: add an action block for Tactical Retreat - it doesn't actually show what it does right now.
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Courageous Tanuki"),
            "Your heart beats with the courage of those who came before you, giving you the kind of bravery only a tanuki can demonstrate.",
            "Whenever you gain the fleeing condition, you also gain a +10-foot circumstance bonus to your Speed. When you have the fleeing condition, instead of having to spend all your actions trying to escape, you can act normally for one action but must still spend the remainder of your actions fleeing. You also gain the Tactical Retreat ability."
            ).WithOnCreature((Creature cr) =>
            {
                cr.AddQEffect(new QEffect("Courageous Tanuki", "When you are fleeing, you gain a +10-foot speed bonus and you can take 1 action per turn normally.")
                {
                    Innate = true,
                    YouAcquireQEffect = (QEffect self, QEffect received) =>
                    {
                        // replace Fleeing with Courageous Fleeing...
                        if (received.Id != QEffectId.Fleeing) return received;
                        if (received.Source == null) return received;
                        // ... unless it's been marked by Courageous Fleeing, then leave it be.
                        if (received.Tag?.GetType() == typeof(string) && (string)received.Tag == "Fleeing (Courageously)") return received;
                        return CourageousFleeing(received.Source, received.ExpiresAt);
                    }
                });
                cr.AddQEffect(new QEffect("Tactical Retreat {icon:Reaction}", "Once per encounter, when you receive the frightened condition, you can choose to Stride immediately with a +10ft circumstance bonus to speed, as if fleeing.")
                {
                    Innate = true,
                    AfterYouAcquireEffect = async (QEffect self, QEffect received) =>
                    {
                        if (received.Id != QEffectId.Frightened) return;
                        bool takenReaction = await self.Owner.AskToUseReaction("You have gained the frightened condition.\nDo you want to use {b}Tactical Retreat{/b} to Stride with a +10ft speed bonus?");
                        if (!takenReaction) return;
                        // maybe remove the name and description from this, might stop it from showing on the character sheet while it's active
                        self.Owner.AddQEffect(new QEffect("Retreating", "+10ft bonus to speed while making a Tactical Retreat.")
                        {
                            BonusToAllSpeeds = (QEffect qe) => new Bonus(2, BonusType.Circumstance, "Retreating")
                        }.WithExpirationEphemeral());
                        await self.Owner.StrideAsync("Choose where to Stride using Tactical Retreat.");
                        self.ExpiresAt = ExpirationCondition.Immediately; // only usable once per encounter
                    }
                });
            });
        yield return new HeritageSelectionFeat(
            ModManager.RegisterFeatName("Completely Average Tanuki"),
            "You're as typical as a tanuki can be. You don't deviate from the norm {b}in any way.{/b}",
            "You have two free ability boosts instead of a tanuki's normal ability boosts and flaw."
            ).WithOnSheet(sheet =>
            {
                sheet.AbilityBoostsFabric.AbilityFlaw = null;
                sheet.AbilityBoostsFabric.AncestryBoosts = [
                        new FreeAbilityBoost(),
                        new FreeAbilityBoost()
                    ];
            });
    }

    static QEffect CourageousFleeing(Creature sourceOfFear, ExpirationCondition expiresAt)
    {
        return new QEffect($"Fleeing {sourceOfFear} (Courageously)", "You can spend one action normally, then must spend the rest moving away from the source of your fear as expediently as possible.", expiresAt, sourceOfFear, IllustrationName.Fleeing)
        {
            CountsAsADebuff = true,
            AfterYouTakeAction = async (QEffect qe, CombatAction action) =>
            {
                if (qe.Owner.Actions.ActionsLeft != 3)
                {
                    if (qe.Source == null)
                    {
                        Debugger.Log(1, null, "qe.Source was null in CourageousFleeing!");
                        qe.Owner.Occupies.Overhead("qe.Source was null!", Color.Crimson, "qe.Source was null in CourageousFleeing!");
                        return;
                    }
                    QEffect tempFleeing = QEffect.Fleeing(qe.Source).WithExpirationAtEndOfOwnerTurn();
                    tempFleeing.Tag = "Fleeing (Courageously)";
                    tempFleeing.Illustration = null;
                    qe.Owner.AddQEffect(tempFleeing);
                }
            },
            BonusToAllSpeeds = (QEffect qe) => new Bonus(2, BonusType.Circumstance, "Courageous Tanuki"),
            PreventTakingAction = (CombatAction action) =>
            {
                if (action.ActionCost > 1) return "Because you are Fleeing (Courageously), you can only take a single action this turn.";
                else return null;
            }
        };
    }
    static CombatAction Ponpoko(Creature cr)
    {
        int dc = cr.ClassOrSpellDC();
        return new CombatAction(cr, IllustrationName.StarHit, "Ponpoko", [TanukiTrait, Trait.Primal, Trait.Sonic],
            $"You deal {S.HeightenedVariable(cr.MaximumSpellRank, 1)}d4 sonic damage to all creatures in a 15-foot cone, with a DC {{b}}{dc}{{/b}} basic Fortitude save. You then cannot use this ability again for 1d4 rounds.",
            Target.FifteenFootCone()
            ).WithSavingThrow(new SavingThrow(Defense.Fortitude, dc))
            .WithActionCost(2)
            .WithProjectileCone(IllustrationName.StarHit, 15, ProjectileKind.Cone)
            .WithSoundEffect(SfxName.Drum)
            .WithEffectOnEachTarget(async (spell, caster, target, result) =>
            {
                await CommonSpellEffects.DealBasicDamage(spell, caster, target, result, cr.MaximumSpellRank + "d4", DamageKind.Sonic);
            })
            .WithEffectOnSelf(cr =>
            {
                cr.AddQEffect(QEffect.CannotUseForXRound("Ponpoko", cr, R.Next(2, 5)));
            });
    }
}
