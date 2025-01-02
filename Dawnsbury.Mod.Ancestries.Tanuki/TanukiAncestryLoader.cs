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

namespace Dawnsbury.Mods.Ancestries.Tanuki;

// IDEA: Homebrew a level 1 version of "hasty celebration", maybe called "premature celebration", and then think up a heritage

public static class TanukiAncestryLoader
{
    static readonly Trait TanukiTrait = ModManager.RegisterTrait("Tanuki", new TraitProperties("Tanuki", true) { IsAncestryTrait = true });

    [DawnsburyDaysModMainMethod]
    public static void LoadMod()
    {
#if DEBUG || DEBUG_V2
        Debugger.Launch();
#endif
#if DAWNSBURY_V2
        ModManager.AssertV2();
#else
        ModManager.AssertV3();
#endif

        Feat TanukiAncestry = new AncestrySelectionFeat(
            ModManager.RegisterFeatName("Tanuki"),
            description: "These shapeshifting raccoon dog–like humanoids use their powers of illusion and transformation in ways more people should: for fun! Tanuki delight in pranks and practical jokes, especially those that allow them to take the high and mighty down a notch and show them what life is like for everyone else. Where other peoples take pride in their storied histories, noble traditions, or intricate ceremonies, tanuki take pride in their simplicity and disregard for the world’s many rules. Though some might claim this outlook reduces tanuki to uncouth rubes, tanuki feel it makes them more cultured; after all, one must know a rule to bend it, and one must understand a norm to break it.",
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

        //yield return new TrueFeat(
        //    ModManager.RegisterFeatName("Hasty Celebration {icon:reaction}"),
        //    5,
        //    "After even the briefest success, you get caught up in the moment and begin to party, cheering your allies on.",
        //    "{b}Frequency{/b}Once per encounter\n{b}Trigger{/b}You critically succeed at an attack roll against an enemy, or an enemy critically fails their saving throw against one of your effects.\nYou grant all allies within 60 feet a +2 circumstance bonus to attack rolls and damage until the end of your next turn. Unfortunately, while you sing and dance, you aren't keeping an eye on your surroundings like you should, making you flat-footed to all enemies until the end of your next turn as well.",
        //    [TanukiTrait]
        //    ).WithOnCreature(delegate (Creature cr)
        //    {
        //        if (!cr.PersistentUsedUpResources.UsedUpActions.Contains("Crackling Mountain"))
        //        {
        //            cr.AddQEffect(new QEffect("Hasty Celebration {icon:reaction}", "When your attack or effect crits, give your allies a +2 circumstance bonus to attack rolls and damage until the end of your next turn. However, you become flat-footed for this duration as well.")
        //            {
        //                Innate = true,
        //                // continue here, probably gotta use AddGrantingOfTechnical to give every enemy an effect that watches for crits from you. 
        //                // maybe you could use AfterYouMakeAttackRoll right here for the attack roll part? but for the saving throw part, thats all on the recipient as far as i can tell

        //            });
        //        }
        //    });
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
                        // TODO: this should only work on saving throws
                        if (action != null && action.HasTrait(Trait.Emotion)) return new Bonus(1, BonusType.Circumstance, "Even-tempered");
                        else return null;
                    },
                    // TODO: use non-obsolete v3 version of this event
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
}
