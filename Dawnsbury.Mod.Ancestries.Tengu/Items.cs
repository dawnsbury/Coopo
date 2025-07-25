﻿using Dawnsbury.Core;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Common;
using Dawnsbury.Core.CharacterBuilder.Selections.Options;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Creatures.Parts;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using Microsoft.Xna.Framework;
using System;

namespace Dawnsbury.Mods.Ancestries.Tengu
{
    internal static class Items
    {
        static public readonly Trait TenguWeaponTrait = ModManager.RegisterTrait("Tengu Weapon", new TraitProperties("Tengu Weapon", false) { ProficiencyName = "Tengu weapons" });

        static public readonly Trait Katana = ModManager.RegisterTrait("Katana", new TraitProperties("Katana", false));
        static public readonly Trait Khakkara = ModManager.RegisterTrait("Khakkara", new TraitProperties("Khakkara", false));
        static public readonly Trait TempleSword = ModManager.RegisterTrait("TempleSword", new TraitProperties("Temple Sword", false));
        static public readonly Trait Wakizashi = ModManager.RegisterTrait("Wakizashi", new TraitProperties("Wakizashi", false));
        static public readonly Trait TenguGaleBlade = ModManager.RegisterTrait("TenguGaleBlade", new TraitProperties("Tengu Gale Blade", false));
        static public readonly Trait Nodachi = ModManager.RegisterTrait("Nodachi", new TraitProperties("Nodachi", false));
        static public readonly Trait BastardSword = ModManager.RegisterTrait("BastardSword", new TraitProperties("Bastard Sword", false));

        static public readonly Illustration ChangeGripArt = new ModdedIllustration("TenguAssets/changeGrip.png");

        public static void RegisterItems()
        {
            ModManager.RegisterNewItemIntoTheShop("katana", (ItemName name) =>
            {
                return new Item(name, new ModdedIllustration("TenguAssets/katana.png"), "katana", 0, 2, [Trait.Weapon, Trait.Melee, Trait.Martial, Trait.Sword, Trait.DeadlyD8, TwoHandD10, Trait.VersatileP, Trait.Mod])
                {
                    WeaponProperties = new WeaponProperties("1d6", DamageKind.Slashing)
                }.WithMainTrait(Katana).ImplementTwoHand(6, 10);
            });
            ModManager.RegisterNewItemIntoTheShop("khakkara", (ItemName name) =>
            {
                return new Item(name, new ModdedIllustration("TenguAssets/khakkara.png"), "khakkara", 0, 2, [Trait.Weapon, Trait.Melee, Trait.Martial, Trait.Club, Trait.MonkWeapon, TwoHandD10, Trait.VersatileP, Trait.Mod])
                {
                    WeaponProperties = new WeaponProperties("1d6", DamageKind.Bludgeoning)
                }.WithMainTrait(Khakkara).ImplementTwoHand(6, 10);
            });
            ModManager.RegisterNewItemIntoTheShop("temple sword", (ItemName name) =>
            {
                return new Item(name, new ModdedIllustration("TenguAssets/templeSword.png"), "temple sword", 0, 2, [Trait.Weapon, Trait.Melee, Trait.Martial, Trait.Sword, Trait.MonkWeapon, Trait.Trip, Trait.Mod])
                {
                    WeaponProperties = new WeaponProperties("1d8", DamageKind.Slashing)
                }.WithMainTrait(TempleSword);
            });
            ModManager.RegisterNewItemIntoTheShop("wakizashi", (ItemName name) =>
            {
                return new Item(name, new ModdedIllustration("TenguAssets/wakizashi.png"), "wakizashi", 0, 2, [Trait.Weapon, Trait.Melee, Trait.Martial, Trait.Sword, Trait.Agile, Trait.DeadlyD8, Trait.Finesse, Trait.VersatileP, Trait.Mod])
                {
                    WeaponProperties = new WeaponProperties("1d4", DamageKind.Slashing)
                }.WithMainTrait(Wakizashi);
            });
            ModManager.RegisterNewItemIntoTheShop("tengu gale blade", (ItemName name) =>
            {
                return new Item(name, new ModdedIllustration("TenguAssets/tenguGaleBlade.png"), "tengu gale blade", 0, 2, [Trait.Weapon, Trait.Melee, Trait.Martial, Trait.Sword, Trait.Agile, Trait.Disarm, Trait.Finesse, TenguAncestryLoader.TenguTrait, Trait.Mod])
                {
                    WeaponProperties = new WeaponProperties("1d6", DamageKind.Slashing)
                }.WithMainTrait(TenguGaleBlade);
            });
            ModManager.RegisterNewItemIntoTheShop("nodachi", (ItemName name) =>
            {
                return new Item(name, new ModdedIllustration("TenguAssets/nodachi.png"), "nodachi", level: 0, price: 6,
                    [Trait.Weapon, Trait.Melee, Trait.Advanced, Trait.Sword, Trait.TwoHanded, Trait.DeadlyD12, Trait.Reach, Brace, Trait.Mod])
                {
                    WeaponProperties = new WeaponProperties("1d8", DamageKind.Slashing)
                }.WithMainTrait(Nodachi).ImplementBrace();
            });
            ModManager.RegisterNewItemIntoTheShop("bastard sword", (ItemName name) =>
            {
                return new Item(name, new ModdedIllustration("TenguAssets/bastardSword.png"), "bastard sword", level: 0, price: 4,
                    [Trait.Weapon, Trait.Melee, Trait.Martial, Trait.Sword, TwoHandD12, Trait.Mod])
                {
                    WeaponProperties = new WeaponProperties("1d8", DamageKind.Slashing)
                }.WithMainTrait(BastardSword).ImplementTwoHand(8, 12);
            });
        }
        public static Trait Brace = ModManager.RegisterTrait("Brace",
            new TraitProperties("Brace", true, "A brace weapon is effective at damaging moving opponents. You gain the {i}Brace Your Weapon{/i} action, which allows you to immediately end your turn in exchange for an extra 2 precision damage per weapon die on reaction Strikes."));
        public static Trait TwoHandD10 = ModManager.RegisterTrait("Two-Hand 1d10",
            new TraitProperties("Two-Hand 1d10", true,
                "This weapon can be wielded with two hands to change its weapon damage die to the indicated value. This change applies to all the weapon's damage dice."));
        public static Trait TwoHandD12 = ModManager.RegisterTrait("Two-Hand 1d12",
            new TraitProperties("Two-Hand 1d12", true,
                "This weapon can be wielded with two hands to change its weapon damage die to the indicated value. This change applies to all the weapon's damage dice."));

        public static bool WeaponHasTwoHand(Item weapon, out Dice d)
        {
            if (weapon.HasTrait(TwoHandD10))
            {
                d = Dice.D10;
                return true;
            }
            if (weapon.HasTrait(TwoHandD12))
            {
                d = Dice.D12;
                return true;
            }
            else
            {
                d = Dice.D1;
                return false;
            }
        }

        public static bool WeaponHasTwoHand(Item weapon)
        {
            return WeaponHasTwoHand(weapon, out _);
        }

        // extension method that implements the functionality of Two-Hand on a weapon. Doesn't add the trait - do that yourself.
        private static Item ImplementTwoHand(this Item item, int baseDamageDiceSize, int upgradedDamageDiceSize)
        {
            item.ProvidesItemAction = (Creature holder, Item self) =>
                new ActionPossibility(self.TwoHanded ? SwapToOneHand(holder, self, baseDamageDiceSize) : SwapToTwoHands(holder, self, upgradedDamageDiceSize));
            item.StateCheckWhenWielded = (Creature wielder, Item item) =>
            {
                //wielder.PersistentCharacterSheet.Calculated.AddSelectionOption(new SingleFeatSelectionOption("twoHandBeingHeldTwoHandedInitially", "THIS IS THE NAME", 0, (feat => feat.HasTrait(Trait.MonkPathToPerfection))));
                wielder.AddQEffect(new QEffect()
                {
                    StartOfCombat = async (QEffect self) =>
                    {
                        if (self.Owner.HasFreeHand &&
                            await self.Owner.AskForConfirmation(item.Illustration, $"How is {self.Owner.Name} holding their {item.Name} at the start of combat?", "Two-handed", "One-handed"))
                        {
                            if (item.WeaponProperties == null) return;
                            item.WeaponProperties.DamageDieSize = upgradedDamageDiceSize;
                            item.Traits.Add(Trait.TwoHanded);
                        }
                        // remove statecheck entirely afterwards, since it's only required at the very start of battle
                        item.StateCheckWhenWielded = null;
                    }
                }.WithExpirationEphemeral());
                
            };
            return item;
        }

        // Produce a CombatAction for the given Item, which changes the item to its two-handed form.
        private static CombatAction SwapToTwoHands(Creature owner, Item item, int diceSize)
        {
            bool lastActionWasToDraw = owner.Actions.ActionHistoryThisTurn.Count != 0 && owner.Actions.ActionHistoryThisTurn.Last().Name == $"Draw {item.Name}";
            return new CombatAction(owner, ChangeGripArt, $"Change Grip ({item.Name})", [Trait.Interact, Trait.Manipulate],
                "You Interact to put another hand on the weapon, increasing its weapon damage die to the value indicated in the Two-Hand trait. You must have a free hand.\n\n{b}Special{/b} If your last action was to draw the weapon, you can Change Grip to wield it two-handed as a free action.",
                Target.Self().WithAdditionalRestriction((Creature self) =>
                {
                    if (!self.HasFreeHand) return "You must have a free hand.";
                    else return null;
                })).WithEffectOnSelf((Creature self) =>
                {
                    if (item.WeaponProperties == null) return;
                    item.WeaponProperties.DamageDieSize = diceSize;
                    item.Traits.Add(Trait.TwoHanded);
                }).WithActionCost(lastActionWasToDraw ? 0 : 1).WithShortDescription("Wield your weapon two-handed to deal more damage.");
        }

        // Produce a CombatAction for the given Item, which changes the item to its one-handed form.
        private static CombatAction SwapToOneHand(Creature owner, Item item, int diceSize)
        {
            return new CombatAction(owner, ChangeGripArt, $"Change Grip ({item.Name})", [Trait.Interact, Trait.Manipulate, Trait.DoesNotProvoke], // releasing technically has the manipulate trait, but doesnt prompt AoOs.
                "You Release a hand from the weapon, decreasing its weapon damage die to its usual value.",
                Target.Self()).WithEffectOnSelf((Creature self) =>
                {
                    if (item.WeaponProperties == null) return;
                    item.WeaponProperties.DamageDieSize = diceSize;
                    item.Traits.Remove(Trait.TwoHanded);
                }).WithActionCost(0).WithShortDescription("Wield your weapon one-handed, at the expense of reduced damage.");
        }
        private static Item ImplementBrace(this Item item)
        {
            // warning doesnt matter, ProvidesItemAction has incorrect nullability
            item.ProvidesItemAction = (Creature cr, Item self) => BraceYourWeapon(cr, self);
            return item;
        }

        private static ActionPossibility? BraceYourWeapon(Creature owner, Item item)
        {
            if (!owner.HasEffect(QEffectId.AttackOfOpportunity)) return null;
            if (item.WeaponProperties == null) return null;
            int extraDamage = item.WeaponProperties.DamageDieCount * 2;
            string damageString = S.HeightenedVariable(extraDamage, 2);
            return new CombatAction(owner, ChangeGripArt, $"Brace Your Weapon", [Trait.Concentrate],
                "{i}You ready your weapon to hit unattentive foes where it really hurts.{/i}\n\nYour turn ends immediately when you use this action. Until the start of your next turn, Strikes you make as part of a reaction deal an additional 2 precision damage per weapon damage die (total " + damageString + " damage).",
                Target.Self()).WithEffectOnSelf((Creature self) =>
                {
                    self.AddQEffect(new QEffect("Weapon braced", $"Reaction Strikes deal {damageString} extra precision damage until the start of your next turn.")
                    {
                        YouDealDamageWithStrike = (QEffect self, CombatAction action, DiceFormula diceFormula, Creature defender) =>
                        {
                            if (defender.IsImmuneTo(Trait.PrecisionDamage)) return diceFormula;
                            return diceFormula.Add(DiceFormula.FromText(extraDamage.ToString(), "Weapon braced"));
                        },
                        ExpiresAt = ExpirationCondition.ExpiresAtStartOfYourTurn,
                        Illustration = ChangeGripArt
                    });
                    // end turn immediately
                    self.Actions.ActionsLeft = 0;
                    self.Actions.UsedQuickenedAction = true;
                    self.Actions.WishesToEndTurn = true;
                }).WithActionCost(1).WithShortDescription($"End your turn immediately, then deal {damageString} extra damage on reaction Strikes until the start of your next turn.");
        }
    }
}
