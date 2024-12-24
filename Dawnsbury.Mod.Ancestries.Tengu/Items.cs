using Dawnsbury.Core;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Display.Illustrations;
using Dawnsbury.Modding;

// TODO: continue adding more weapons, then the weapon familiarity feat

namespace Dawnsbury.Mods.Ancestries.Tengu
{
    internal static class Items
    {
        static readonly Illustration ChangeGripArt = new ModdedIllustration("TenguAssets/changeGrip.png");

        public static void RegisterItems()
        {
            ModManager.RegisterNewItemIntoTheShop("katana", (ItemName name) =>
            {
                return new Item(name, new ModdedIllustration("TenguAssets/katana.png"), "katana", 0, 2, [Trait.Weapon, Trait.Melee, Trait.Martial, Trait.Sword, Trait.DeadlyD8, TwoHandD10, Trait.VersatileP])
                {
                    WeaponProperties = new WeaponProperties("1d6", DamageKind.Slashing)
                }.ImplementTwoHand(6, 10);
            });
            ModManager.RegisterNewItemIntoTheShop("khakkara", (ItemName name) =>
            {
                return new Item(name, new ModdedIllustration("TenguAssets/khakkara.png"), "khakkara", 0, 2, [Trait.Weapon, Trait.Melee, Trait.Martial, Trait.Club, Trait.Monk, TwoHandD10, Trait.VersatileP])
                {
                    WeaponProperties = new WeaponProperties("1d6", DamageKind.Bludgeoning)
                }.ImplementTwoHand(6, 10);
            });
            ModManager.RegisterNewItemIntoTheShop("temple sword", (ItemName name) =>
            {
                return new Item(name, new ModdedIllustration("TenguAssets/templeSword.png"), "temple sword", 0, 2, [Trait.Weapon, Trait.Melee, Trait.Martial, Trait.Sword, Trait.Monk, Trait.Trip])
                {
                    WeaponProperties = new WeaponProperties("1d8", DamageKind.Slashing)
                };
            });
            // TODO: get art
            ModManager.RegisterNewItemIntoTheShop("wakizashi", (ItemName name) =>
            {
                return new Item(name, new ModdedIllustration("TenguAssets/wakizashi.png"), "wakizashi", 0, 2, [Trait.Weapon, Trait.Melee, Trait.Martial, Trait.Sword, Trait.Agile, Trait.DeadlyD8, Trait.Finesse, Trait.VersatileP])
                {
                    WeaponProperties = new WeaponProperties("1d4", DamageKind.Slashing)
                };
            });
        }

        // TODO: add the Dual-Handed Assault feat, which gives actual purpose to Two-Hand weapons
        public static Trait TwoHandD10 = ModManager.RegisterTrait("Two-Hand 1d10",
            new TraitProperties("Two-Hand 1d10", true,
                "This weapon can be wielded with two hands to change its weapon damage die to the indicated value. This change applies to all the weapon's damage dice."));

        // extension method that implements the functionality of Two-Hand on a weapon. Doesn't add the trait - do that yourself.
        private static Item ImplementTwoHand(this Item item, int baseDamageDiceSize, int upgradedDamageDiceSize)
        {
            item.ProvidesItemAction = (Creature holder, Item self) =>
                new ActionPossibility(self.TwoHanded ? SwapToOneHand(holder, self, baseDamageDiceSize) : SwapToTwoHands(holder, self, upgradedDamageDiceSize));
            item.StateCheckWhenWielded = (Creature wielder, Item item) =>
            {
                wielder.AddQEffect(new QEffect()
                {
                    StartOfCombat = async (QEffect self) =>
                    {
                        if (self.Owner.HasFreeHand &&
                            await self.Owner.AskForConfirmation(item.Illustration, $"How is {self.Owner.Name} holding their {item.Name} at the start of combat?", "Two-handed", "One-handed"))
                        {
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
            bool lastActionWasToDraw = owner.Actions.ActionHistoryThisTurn.Count() != 0 && owner.Actions.ActionHistoryThisTurn.Last().Name == $"Draw {item.Name}";
            return new CombatAction(owner, ChangeGripArt, $"Change Grip ({item.Name})", [Trait.Interact, Trait.Manipulate],
                "You Interact to put another hand on the weapon, increasing its weapon damage die to the value indicated in the Two-Hand trait. You must have a free hand.\n\n{b}Special{/b} If your last action was to draw the weapon, you can Change Grip to wield it two-handed as a free action.",
                Target.Self().WithAdditionalRestriction((Creature self) =>
                {
                    if (!self.HasFreeHand) return "You must have a free hand.";
                    else return null;
                })).WithEffectOnSelf((Creature self) =>
                {
                    item.WeaponProperties.DamageDieSize = diceSize;
                    item.Traits.Add(Trait.TwoHanded);
                }).WithActionCost(lastActionWasToDraw ? 0 : 1);
        }

        // Produce a CombatAction for the given Item, which changes the item to its one-handed form.
        private static CombatAction SwapToOneHand(Creature owner, Item item, int diceSize)
        {
            return new CombatAction(owner, ChangeGripArt, $"Change Grip ({item.Name})", [Trait.Interact, Trait.Manipulate],
                "You Release a hand from the weapon, decreasing its weapon damage die to its usual value.",
                Target.Self()).WithEffectOnSelf((Creature self) =>
                {
                    item.WeaponProperties.DamageDieSize = diceSize;
                    item.Traits.Remove(Trait.TwoHanded);
                }).WithActionCost(0);
        }
    }
}
