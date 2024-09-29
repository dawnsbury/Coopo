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
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Roller;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Microsoft.Xna.Framework.Graphics;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Audio;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Display.Text;
using System.Reflection.Emit;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using System.Diagnostics;

namespace Dawnsbury_Days_Tanuki_Ancestry
{

    public static class Mod
    {
        public static Trait TanukiTrait;

        [DawnsburyDaysModMainMethod]
        public static void ModEntry()
        {
            Debugger.Launch();
            TanukiTrait = ModManager.RegisterTrait("Tanuki", new TraitProperties("Tanuki", true) { IsAncestryTrait = true });

            Feat TanukiAncestry = new AncestrySelectionFeat(
                ModManager.RegisterFeatName("Tanuki"),
                "These shapeshifting raccoon dog–like humanoids use their powers of illusion and transformation in ways more people should: for fun! Tanuki delight in pranks and practical jokes, especially those that allow them to take the high and mighty down a notch and show them what life is like for everyone else. Where other peoples take pride in their storied histories, noble traditions, or intricate ceremonies, tanuki take pride in their simplicity and disregard for the world’s many rules. Though some might claim this outlook reduces tanuki to uncouth rubes, tanuki feel it makes them more cultured; after all, one must know a rule to bend it, and one must understand a norm to break it.",
                [Trait.Humanoid, TanukiTrait],
                10,
                5,
                [
                    new EnforcedAbilityBoost(Ability.Constitution),
                    new EnforcedAbilityBoost(Ability.Charisma),
                    new FreeAbilityBoost()
                ],
                GetHeritages().ToList()).WithAbilityFlaw(Ability.Wisdom);
            /*.WithOnCreature((CalculatedCharacterSheetValues sheet, Creature cr) =>
            {
                cr.AddQEffect(new QEffect("Change Shape", "Transform into a mundane raccoon dog, using the statistics of pest form.")
                {
                    ProvideMainAction = (QEffect ef) =>
                    {
                        CombatAction action = new CombatAction(cr, IllustrationName.WildShape, "Change Shape",
                            [TanukiTrait, Trait.Concentrate, Trait.Polymorph, Trait.Primal],
                            "description", Target.Self())
                        .WithActionCost(1)
                        .WithEffectOnSelf((Creature self) =>
                        {
                            QEffect effect = CommonSpellEffects.EnterBattleform(self, IllustrationName.Dawnsbury, 15 + self.ProficiencyLevel, 4, false);
                            effect.StateCheck = (Action<QEffect>)Delegate.Combine(effect.StateCheck, (Action<QEffect>)delegate (QEffect qfForm)
                            {
                                qfForm.Owner.WeaknessAndResistance.AddWeakness(DamageKind.Bludgeoning, 5);
                                qfForm.Owner.WeaknessAndResistance.AddWeakness(DamageKind.Slashing, 5);
                                qfForm.Owner.WeaknessAndResistance.AddWeakness(DamageKind.Piercing, 5);
                            });
                        });
                        return new ActionPossibility(action);
                    }
                });
            });*/

            ModManager.AddFeat(TanukiAncestry);

            AddFeats(GetAncestryFeats());

            ModManager.RegisterNewSpell("Test Fleeing", 1, (SpellId spellId, Creature? spellcaster, int spellLevel, bool inCombat, SpellInformation spellInfo) =>
            {
                return Spells.CreateModern(IllustrationName.Fear, "Test Fleeing", [
                Trait.Emotion,
                Trait.Enchantment,
                Trait.Fear,
                Trait.Mental,
                Trait.Arcane,
                Trait.Divine,
                Trait.Occult,
                Trait.Primal],
                "You ask the target nicely to gain the fleeing condition.", "The target automatically gains the fleeing condition for 3 turns.", Target.FifteenFootCone(), spellLevel, null)
                    .WithEffectOnEachTarget(async (CombatAction spell, Creature caster, Creature target, CheckResult checkResult) =>
                    {
                        target.AddQEffect(QEffect.Fleeing(caster).WithExpirationAtStartOfSourcesTurn(caster, 3));
                    });
            });
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
                "You have a belly melee unarmed Strike, which deals 1d6 damage and has the forceful trait.",
                [TanukiTrait]
                ).WithOnCreature((Creature cr) =>
                {
                    cr.AddQEffect(new QEffect("Iron Belly", "You have a belly attack.")
                    {
                        Innate = true,
                        AdditionalUnarmedStrike = new Item(IllustrationName.Boneshaker, "belly", [Trait.Forceful, Trait.Weapon, Trait.Melee])
                        .WithWeaponProperties(new WeaponProperties("1d6", DamageKind.Bludgeoning))
                    });
                });

            yield return new TrueFeat(
                ModManager.RegisterFeatName("Scorched on the Crackling Mountain"),
                1,
                "By ritualistically marking your fur with fire, like an infamous tanuki of legend, you protect yourself against future flames. You gain a black stripe down your back that looks charred.",
                "Your flat check to remove persistent fire damage is DC 10 instead of DC 15. The first time each day you would be reduced to 0 Hit Points by a fire effect, you avoid being knocked out and remain at 1 Hit Point, and your wounded condition increases by 1.",
                [TanukiTrait]
                ).WithOnCreature(delegate (Creature cr)
                {
                    cr.AddQEffect(new QEffect("Scorched Tanuki", "Your flat check to remove persistent fire damage is DC 10. The first time each day you are reduced to 0 Hit Points by a fire effect, you remain at 1 Hit Point and increase your wounded condition by 1 instead of being knocked out.")
                    {
                        Innate = true,
                        YouAcquireQEffect = (QEffect effect, QEffect recieved) =>
                        {
                            if (recieved.Id == QEffectId.PersistentDamage && recieved.Key == $"PersistentDamage:{DamageKind.Fire}")
                            {
                                // what a load of horrible jank
                                var damage = recieved.Name.Split(' ', 2)[0];
                                // replace the end-of-turn effect of persistent fire damage with the same thing, but it uses the assisted DC
                                // (this doesnt actually implement the feat as-written, as there is no way to achieve DC 5, but an upcoming update will allow for this)
                                // the upcoming update will add QEffect.ReducesPersistentDamageRecoveryCheckDC. in v3 you can also add an effect with ID CharhideGoblin, but thats temporary.
                                recieved.EndOfYourTurn = async (QEffect qf, Creature self) =>
                                {
                                    await self.DealDirectDamage(CombatAction.CreateSimple(self.Battle.Pseudocreature, "Persistent damage"), DiceFormula.FromText(damage), self, CheckResult.Failure, DamageKind.Fire);
                                    if (!self.DeathScheduledForNextStateCheck && (self.Actions.HasDelayedYieldingTo == null || self.HasTrait(Trait.AnimalCompanion)))
                                    {
                                        qf.RollPersistentDamageRecoveryCheck(assisted: true);
                                    }
                                };
                                return recieved;
                            }
                            else return recieved;
                        }
                    });
                    if (!cr.PersistentUsedUpResources.UsedUpActions.Contains("Scorched Tanuki"))
                    {
                        cr.AddQEffect(new QEffect()
                        {
                            Innate = true,
                            YouAreDealtLethalDamage = async (QEffect qEffect, Creature attacker, DamageStuff damageStuff, Creature you) =>
                            {
                                // only triggers when you are reduced to 0HP, not when you are dying
                                // (will not be necessary in an upcoming update)
                                if (damageStuff.Kind == DamageKind.Fire && you.HP < 1)
                                {
                                    you.Occupies.Overhead("kachi-kachi!!", Color.Red, you.ToString() + " resists dying through Scorched on the Crackling Mountain!");
                                    you.IncreaseWounded();
                                    you.PersistentUsedUpResources.UsedUpActions.Add("Scorched Tanuki");
                                    qEffect.ExpiresAt = ExpirationCondition.Immediately;
                                    int damage = you.HP - 1; // damage to reduce the player to 1hp
                                    return new SetToTargetNumberModification(damage, "Scorched on the Crackling Mountain");
                                }
                                else return null;
                            }
                        });
                    }
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
                            if (action != null && action.HasTrait(Trait.Emotion)) return new Bonus(1, BonusType.Circumstance, "Even-tempered Tanuki");
                            else return null;
                        },
                        AdjustSavingThrowResult = (QEffect effect, CombatAction action, CheckResult result) =>
                        {
                            if (!action.HasTrait(Trait.Emotion)) return result;
                            if (result == CheckResult.Success) return CheckResult.CriticalSuccess;
                            if (result == CheckResult.Failure) return CheckResult.CriticalFailure;
                            else return result;
                        }
                    });
                });

            yield return new HeritageSelectionFeat(
                ModManager.RegisterFeatName("Virtuous Tanuki"),
                "Many tanuki carry a gourd of alcohol to remind themselves to act with virtue, and by these standards, you’re quite virtuous indeed.",
                "You gain poison resistance equal to half your level (minimum 1). You can eat and drink things when you’re sickened."
                ).WithOnCreature(delegate (Creature cr)
                {
                    cr.AddQEffect(QEffect.DamageResistance(DamageKind.Poison, cr.MaximumSpellRank));
                    cr.AddQEffect(new QEffect("Virtuous", "You can eat and drink when sickened.")
                    {
                        YouAcquireQEffect = (QEffect effect, QEffect recieved) =>
                        {
                            if (recieved.Id == QEffectId.Sickened)
                            {
                                recieved.PreventTakingAction = null; // remove the function preventing the player from using potions
                                recieved.Name = "Sickened (Virtuous)";
                                recieved.Description = "You take a status penalty equal to the value to all your checks and DCs. You can still eat and drink due to your virtuous heritage.";
                                return recieved;
                            }
                            else
                            {
                                return recieved;
                            }
                        }
                    });
                });

            // as for implementation...
            // the fleeing condition doesnt do anything by itself. it acts as a tag, which the forcedactions code checks for. therefore, perhaps it would be a good solution
            // to check for the condition on the start of each turn and remove it for a single action, if possible.

            // Dawnsbury.Core.Mechanics.Core.ForcedActions

            // current plan - when gaining fleeing, replace with a new condition called "Courageous Flee" (or similar). This SHOULD NOT share an id with fleeing or fleeingfromdanger,
            // so that forced movement isnt applied, but after an action is taken with that condition, it should inflict real fleeing (probably hidden) for the rest of the turn.
            //yield return new HeritageSelectionFeat(
            //    ModManager.RegisterFeatName("Courageous Tanuki"),
            //    "Your heart beats with the courage of those who came before you, giving you the kind of bravery only a tanuki can demonstrate.",
            //    "Whenever you gain the fleeing condition, you also gain a +10-foot circumstance bonus to your Speed. When you have the fleeing condition, instead of having to spend all your actions trying to escape, you can act normally for one action but must still spend the remainder of your actions fleeing. You also gain the Tactical Retreat ability."
            //    ).WithPermanentQEffect("When you are fleeing, you gain a +10-foot speed bonus and you can take 1 action per turn normally.", (QEffect effect) =>
            //    {
            //        effect.YouAcquireQEffect = (QEffect qe, QEffect recieved) =>
            //        {
            //            if (recieved.Id == QEffectId.Fleeing)
            //            {
            //                if (recieved.Source == null)
            //                {
            //                    qe.Owner.Occupies.Overhead("recieved.Source was null!!!", Color.Red);
            //                    return recieved;
            //                }
            //                //return CourageousFleeing(recieved.Source, recieved.ExpiresAt);
            //                return QEffect.Frightened(50);
            //            }
            //            else return recieved;
            //        };
            //    });
            yield return new HeritageSelectionFeat(
                ModManager.RegisterFeatName("Courageous Tanuki"),
                "Your heart beats with the courage of those who came before you, giving you the kind of bravery only a tanuki can demonstrate.",
                "Whenever you gain the fleeing condition, you also gain a +10-foot circumstance bonus to your Speed. When you have the fleeing condition, instead of having to spend all your actions trying to escape, you can act normally for one action but must still spend the remainder of your actions fleeing. You also gain the Tactical Retreat ability."
                ).WithOnCreature((Creature cr) =>
                {
                    cr.AddQEffect(new QEffect("Courageous Tanuki", "When you are fleeing, you gain a +10-foot speed bonus and you can take 1 action per turn normally.")
                    {
                        Innate = true,
                        YouAcquireQEffect = (QEffect qe, QEffect recieved) =>
                        {
                            if (recieved.Id == QEffectId.Fleeing)
                            {
                                //return CourageousFleeing(recieved.Source, recieved.ExpiresAt);
                                return QEffect.Frightened(2);
                            }
                            else return recieved;
                        }
                    });
                });
        }

        // RESUME HERE RESUME HERE
        // i have no idea how to actually test if this works? theres not many ways to get fleeing. maybe just make a spell that automatically inflicts fleeing for 3 rounds, just to test
        static QEffect CourageousFleeing(Creature sourceOfFear, ExpirationCondition expiresAt)
        {
            return new QEffect("Courageous Fleeing", "description", expiresAt, sourceOfFear, IllustrationName.Fleeing)
            {
                CountsAsADebuff = true,
                AfterYouTakeAction = async (QEffect qe, CombatAction action) =>
                {
                    if (qe.Owner.Actions.ActionsLeft != 3)
                    {
                        if (qe.Source == null)
                        {
                            qe.Owner.Occupies.Overhead("qe.Source was null!!!", Color.Red);
                            return;
                        }
                        qe.Owner.AddQEffect(QEffect.Fleeing(qe.Source).WithExpirationAtEndOfOwnerTurn());
                    }
                },
                BonusToAllSpeeds = (QEffect qe) => new Bonus(2, BonusType.Circumstance, "Courageous Tanuki")
            };
        }
    }
}
