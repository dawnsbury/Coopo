using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.Animations;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Intelligence;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Display.Illustrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;

// TODO: make remastered versions of Bless and Bane to go with Bene/Malediction, then check the other TODOs in this file (making unique magic circles, maybe pick different sounds)
// then, get back to making the feat that you needed these to exist for ()

namespace Dawnsbury.Mods.BattleHarbinger
{
    internal class ModSpells
    {
        static readonly QEffectId RolledAgainstMalediction = ModManager.RegisterEnumMember<QEffectId>("RolledAgainstMalediction");
        static readonly QEffectId FailedAgainstMalediction = ModManager.RegisterEnumMember<QEffectId>("FailedAgainstMalediction");

        static readonly Illustration BenedictionArt = new ModdedIllustration("BattleHarbingerAssets/benediction.png");
        static readonly Illustration MaledictionArt = new ModdedIllustration("BattleHarbingerAssets/malediction.png");
        static readonly Illustration BenedictionCircleArt = new ModdedIllustration("BattleHarbingerAssets/benedictionCircle.png");
        static readonly Illustration MaledictionCircleArt = new ModdedIllustration("BattleHarbingerAssets/maledictionCircle.png");

        public static void RegisterSpells()
        {
            ModManager.RegisterNewSpell("Benediction", 1, (SpellId spellId, Creature? caster, int level, bool inCombat, SpellInformation spellInfo) =>
            {
                return Benediction(level);
            });

            ModManager.RegisterNewSpell("Malediction", 1, (SpellId spellId, Creature? caster, int level, bool inCombat, SpellInformation spellInfo) =>
            {
                return Malediction(level);
            });

            ModManager.ReplaceExistingSpell(SpellId.Bless, 1, (Creature? caster, int level, bool inCombat, SpellInformation spellInfo) =>
            {
                return Bless(level);
            });

            ModManager.ReplaceExistingSpell(SpellId.Bane, 1, (Creature? caster, int level, bool inCombat, SpellInformation spellInfo) =>
            {
                return Bane(level);
            });
        }
        public static CombatAction Benediction(int level)
        {
            return Spells.CreateModern(BenedictionArt,
                "Benediction",
                [Trait.Enchantment, Trait.Mental, Trait.Divine],
                "Divine protection helps protect your companions.",
                "For the rest of the encounter, you and your allies gain a +1 status bonus to AC while within a 15-foot-radius emanation around you.\n\nOnce per turn, starting the turn after you cast benediction, you can Sustain the spell to increase the emanation's radius by 10 feet.",
                Target.Self((Creature cr, AI ai) => ai.BlessBane()),
                level,
                null).WithSoundEffect(SfxName.Bless)
                .WithEffectOnSelf(async (CombatAction action, Creature cr) =>
                {
                    BeneMaledictionEffectOnSelf(action, cr, BenedictionArt, true, 3, 2);
                });
        }

        public static CombatAction Malediction(int level)
        {
            return Spells.CreateModern(MaledictionArt,
                "Malediction",
                [Trait.Enchantment, Trait.Mental, Trait.Divine],
                "You incite distress in the minds of your enemies, making it more difficult for them to defend themselves.",
                "You create a 10-foot-radius emanation around you. Enemies within it must succeed at a Will save or take a –1 status penalty to AC as long as they are in the area.\n\nAn enemy who failed and then leaves the area and reenters later is automatically affected again. An enemy who enters the area for the first time rolls a Will save as they enter.\n\nOnce per turn, starting the turn after you cast malediction, you can Sustain the spell to increase the emanation's radius by 10 feet and force enemies in the area that weren't yet affected to attempt another saving throw.",
                Target.Self((Creature cr, AI ai) => ai.BlessBane()),
                level,
                null).WithSoundEffect(SfxName.Fear)
                .WithEffectOnSelf(async (CombatAction action, Creature cr) =>
                {
                    BeneMaledictionEffectOnSelf(action, cr, MaledictionArt, false, 2, 2);
                });
        }

        public static CombatAction Bless(int level)
        {
            return Spells.CreateModern(IllustrationName.Bless,
                "Bless",
                [Trait.Enchantment, Trait.Mental, Trait.Divine, Trait.Occult],
                "Blessings from beyond help your companions strike true.",
                "For the rest of the encounter, you and your allies gain a +1 status bonus to attack rolls while within a 15-foot-radius emanation around you.\n\nOnce per turn, starting the turn after you cast bless, you can Sustain the spell to increase the emanation's radius by 10 feet.",
                Target.Self((Creature cr, AI ai) => ai.BlessBane()),
                level,
                null).WithSoundEffect(SfxName.Bless)
                .WithEffectOnSelf(async (CombatAction action, Creature cr) =>
                {
                    BlessBaneEffectOnSelf(action, cr, IllustrationName.Bless, true, 3, 2);
                });
        }

        public static CombatAction Bane(int level)
        {
            return Spells.CreateModern(IllustrationName.Bane,
                "Bane",
                [Trait.Enchantment, Trait.Mental, Trait.Divine, Trait.Occult],
                "You fill the minds of your enemies with doubt.",
                "You create a 10-foot-radius emanation around you. Enemies within it must succeed at a Will save or take a –1 status penalty to attack rolls as long as they are in the area.\n\nAn enemy who failed and then leaves the area and reenters later is automatically affected again. An enemy who enters the area for the first time rolls a Will save as they enter.\n\nOnce per turn, starting the turn after you cast bane, you can Sustain the spell to increase the emanation's radius by 10 feet and force enemies in the area that weren't yet affected to attempt another saving throw.",
                Target.Self((Creature cr, AI ai) => ai.BlessBane()),
                level,
                null).WithSoundEffect(SfxName.Bless)
                .WithEffectOnSelf(async (CombatAction action, Creature cr) =>
                {
                    BlessBaneEffectOnSelf(action, cr, IllustrationName.Bane, false, 2, 2);
                });
        }

        // TODO: see if this slop can be cleaned up somewhat
        private static void BeneMaledictionEffectOnSelf(CombatAction action, Creature caster, Illustration illustration, bool isBenediction, int initialRadius, int sustainExpansion)
        {
            // TODO: make benediction and malediction magic circles
            AuraAnimation auraAnimation = caster.AnimationData.AddAuraAnimation(isBenediction ? IllustrationName.BlessCircle : IllustrationName.BaneCircle, initialRadius);
            QEffect casterEffect = new QEffect(isBenediction ? "Benediction" : "Malediction", "[this condition has no description]", ExpirationCondition.Never, caster, IllustrationName.None)
            {
                WhenExpires = delegate
                {
                    auraAnimation.MoveTo(0f);
                },
                Tag = (initialRadius, true),
                StartOfYourEveryTurn = async delegate (QEffect self, Creature _)
                {
                    self.Tag = ((((int, bool))self.Tag).Item1, false);
                },
                ProvideContextualAction = delegate (QEffect self)
                {
                    (int auraSize, bool sustainedThisTurn) = ((int, bool))self.Tag;
                    if (!sustainedThisTurn)
                        return new ActionPossibility(
                            new CombatAction(self.Owner, illustration, isBenediction ? "Increase Benediction radius" : "Increase Malediction radius",
                            [Trait.Concentrate, Trait.SustainASpell], //TODO: check that making this sustain doesnt change anything significant
                            $"Increase the radius of the {(isBenediction ? "benediction" : "malediction")} emanation by {5 * sustainExpansion} feet.",
                            Target.Self((Creature cr, AI ai) => -2.14748365E+09f)).WithEffectOnSelf((Creature _) =>
                            {
                                int newAuraSize = auraSize + sustainExpansion;
                                self.Tag = (newAuraSize, true);
                                auraAnimation.MoveTo(newAuraSize);
                                if (!isBenediction)
                                {
                                    foreach (Creature enemy in self.Owner.Battle.AllCreatures.Where((Creature cr) => cr.DistanceTo(self.Owner) <= newAuraSize && cr.EnemyOf(self.Owner)))
                                    {
                                        enemy.RemoveAllQEffects((QEffect qf) => qf.Id == RolledAgainstMalediction && qf.Tag == self);
                                    }
                                }
                            })).WithPossibilityGroup("Maintain an activity");
                    else return null;
                }
            };
            if (isBenediction)
            {
                auraAnimation.Color = Color.Green;
                casterEffect.StateCheck = (QEffect self) =>
                {
                    int auraSize = (((int, bool))self.Tag).Item1;
                    foreach (Creature friend in self.Owner.Battle.AllCreatures.Where((Creature cr) => cr.DistanceTo(self.Owner) <= auraSize && cr.FriendOf(self.Owner) && !cr.HasTrait(Trait.Mindless) && !cr.HasTrait(Trait.Object)))
                    {
                        friend.AddQEffect(new QEffect("Benediction", "You gain a +1 status bonus to AC.", ExpirationCondition.Ephemeral, self.Owner, BenedictionArt)
                        {
                            CountsAsABuff = true,
                            BonusToDefenses = (QEffect _, CombatAction? _, Defense defense) => defense == Defense.AC ? new Bonus(1, BonusType.Status, "benediction") : null
                        });
                    }
                };
            }
            else
            {
                auraAnimation.Color = Color.MediumPurple;
                casterEffect.StateCheckWithVisibleChanges = async delegate (QEffect self)
                {
                    int auraSize = (((int, bool))self.Tag).Item1;
                    foreach (Creature enemy in self.Owner.Battle.AllCreatures.Where((Creature cr) => cr.DistanceTo(self.Owner) <= auraSize && cr.EnemyOf(self.Owner) && !cr.HasTrait(Trait.Mindless) && !cr.HasTrait(Trait.Object)))
                    {
                        if (!enemy.QEffects.Any((QEffect qf) => qf.ImmuneToTrait == Trait.Mental))
                        {
                            if (enemy.QEffects.Any((QEffect qf) => qf.Id == FailedAgainstMalediction && qf.Tag == self))
                            {
                                ApplyMaledictionEffect(enemy);
                            }
                            else if (!enemy.QEffects.Any((QEffect qf) => qf.Id == RolledAgainstMalediction && qf.Tag == self))
                            {
                                CheckResult checkResult = CommonSpellEffects.RollSpellSavingThrow(enemy, action, Defense.Will);
                                enemy.AddQEffect(new QEffect(ExpirationCondition.Never)
                                {
                                    Id = RolledAgainstMalediction,
                                    Tag = self
                                });
                                if (checkResult <= CheckResult.Failure)
                                {
                                    enemy.AddQEffect(new QEffect(ExpirationCondition.Never)
                                    {
                                        Id = FailedAgainstMalediction,
                                        Tag = self
                                    });
                                    ApplyMaledictionEffect(enemy);
                                }
                            }
                        }
                    }

                    void ApplyMaledictionEffect(Creature enemy)
                    {
                        enemy.AddQEffect(new QEffect("Malediction", "You take a -1 status penalty to AC.", ExpirationCondition.Ephemeral, self.Owner, MaledictionArt)
                        {
                            Key = "MaledictionPenalty",
                            BonusToDefenses = (QEffect _, CombatAction? _, Defense defense) => defense == Defense.AC ? new Bonus(-1, BonusType.Status, "malediction") : null
                        });
                    }
                };
            }
            caster.AddQEffect(casterEffect);
        }

        private static void BlessBaneEffectOnSelf(CombatAction action, Creature caster, Illustration illustration, bool isBless, int initialRadius, int sustainExpansion)
        {
            AuraAnimation auraAnimation = caster.AnimationData.AddAuraAnimation(isBless ? IllustrationName.BlessCircle : IllustrationName.BaneCircle, initialRadius);
            QEffect casterEffect = new QEffect(isBless ? "Bless" : "Bane", "[this condition has no description]", ExpirationCondition.Never, caster, IllustrationName.None)
            {
                WhenExpires = delegate
                {
                    auraAnimation.MoveTo(0f);
                },
                Tag = (initialRadius, true),
                StartOfYourEveryTurn = async delegate (QEffect self, Creature _)
                {
                    self.Tag = ((((int, bool))self.Tag).Item1, false);
                },
                ProvideContextualAction = delegate (QEffect self)
                {
                    (int auraSize, bool sustainedThisTurn) = ((int, bool))self.Tag;
                    if (!sustainedThisTurn)
                        return new ActionPossibility(
                            new CombatAction(self.Owner, illustration, isBless ? "Increase Bless radius" : "Increase Bane radius",
                            [Trait.Concentrate, Trait.SustainASpell], //TODO: check that making this sustain doesnt change anything significant
                            $"Increase the radius of the {(isBless ? "bless" : "bane")} emanation by {5 * sustainExpansion} feet.",
                            Target.Self((Creature cr, AI ai) => -2.14748365E+09f)).WithEffectOnSelf((Creature _) =>
                            {
                                int newAuraSize = auraSize + sustainExpansion;
                                self.Tag = (newAuraSize, true);
                                auraAnimation.MoveTo(newAuraSize);
                                if (!isBless)
                                {
                                    foreach (Creature enemy in self.Owner.Battle.AllCreatures.Where((Creature cr) => cr.DistanceTo(self.Owner) <= newAuraSize && cr.EnemyOf(self.Owner)))
                                    {
                                        enemy.RemoveAllQEffects((QEffect qf) => qf.Id == QEffectId.RolledAgainstBane && qf.Tag == self);
                                    }
                                }
                            })).WithPossibilityGroup("Maintain an activity");
                    else return null;
                }
            };
            if (isBless)
            {
                casterEffect.StateCheck = (QEffect self) =>
                {
                    int auraSize = (((int, bool))self.Tag).Item1;
                    foreach (Creature friend in self.Owner.Battle.AllCreatures.Where((Creature cr) => cr.DistanceTo(self.Owner) <= auraSize && cr.FriendOf(self.Owner) && !cr.HasTrait(Trait.Mindless) && !cr.HasTrait(Trait.Object)))
                    {
                        friend.AddQEffect(new QEffect("Bless", "You gain a +1 status bonus to attack rolls.", ExpirationCondition.Ephemeral, self.Owner, IllustrationName.Bless)
                        {
                            CountsAsABuff = true,
                            BonusToAttackRolls = (QEffect _, CombatAction action, Creature? _) => action.HasTrait(Trait.Attack) ? new Bonus(1, BonusType.Status, "bless") : null
                        });
                    }
                };
            }
            else
            {
                casterEffect.StateCheckWithVisibleChanges = async delegate (QEffect self)
                {
                    int auraSize = (((int, bool))self.Tag).Item1;
                    foreach (Creature enemy in self.Owner.Battle.AllCreatures.Where((Creature cr) => cr.DistanceTo(self.Owner) <= auraSize && cr.EnemyOf(self.Owner) && !cr.HasTrait(Trait.Mindless) && !cr.HasTrait(Trait.Object)))
                    {
                        if (!enemy.QEffects.Any((QEffect qf) => qf.ImmuneToTrait == Trait.Mental))
                        {
                            if (enemy.QEffects.Any((QEffect qf) => qf.Id == QEffectId.FailedAgainstBane && qf.Tag == self))
                            {
                                ApplyBaneEffect(enemy);
                            }
                            else if (!enemy.QEffects.Any((QEffect qf) => qf.Id == QEffectId.RolledAgainstBane && qf.Tag == self))
                            {
                                CheckResult checkResult = CommonSpellEffects.RollSpellSavingThrow(enemy, action, Defense.Will);
                                enemy.AddQEffect(new QEffect(ExpirationCondition.Never)
                                {
                                    Id = QEffectId.RolledAgainstBane,
                                    Tag = self
                                });
                                if (checkResult <= CheckResult.Failure)
                                {
                                    enemy.AddQEffect(new QEffect(ExpirationCondition.Never)
                                    {
                                        Id = QEffectId.FailedAgainstBane,
                                        Tag = self
                                    });
                                    ApplyBaneEffect(enemy);
                                }
                            }
                        }
                    }

                    void ApplyBaneEffect(Creature enemy)
                    {
                        enemy.AddQEffect(new QEffect("Bane", "You take a -1 status penalty to attack rolls.", ExpirationCondition.Ephemeral, self.Owner, IllustrationName.Bane)
                        {
                            Key = "BanePenalty",
                            BonusToAttackRolls = (QEffect _, CombatAction action, Creature? _) => action.HasTrait(Trait.Attack) ? new Bonus(-1, BonusType.Status, "bane") : null
                        });
                    }
                };
            }
            caster.AddQEffect(casterEffect);
        }
    }
}
