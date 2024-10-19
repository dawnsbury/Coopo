using Dawnsbury.Audio;
using Dawnsbury.Core;
using Dawnsbury.Core.CombatActions;
using Dawnsbury.Core.Creatures;
using Dawnsbury.Core.Mechanics;
using Dawnsbury.Core.Mechanics.Core;
using Dawnsbury.Core.Mechanics.Enumerations;
using Dawnsbury.Core.Mechanics.Rules;
using Dawnsbury.Core.Mechanics.Targeting;
using Dawnsbury.Core.Mechanics.Treasure;
using Dawnsbury.Core.Possibilities;
using Dawnsbury.Core.Roller;
using Dawnsbury.Display.Text;
using Dawnsbury.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dawnsbury.Mods.Ancestries.Kitsune
{
    internal class StarOrb
    {
        static public QEffectId StarOrbQEffectId = ModManager.RegisterEnumMember<QEffectId>("StarOrbEffect");
        static public CombatAction OrbRestoration(Creature owner)
        {
            return new CombatAction(owner, IllustrationName.Heal, "Orb Restoration",
                [KitsuneAncestryLoader.KitsuneTrait],
                $"Recover {S.HeightenedVariable(owner.MaximumSpellRank, 1)}d8 hit points.",
                Target.Self()).WithEffectOnSelf(async (CombatAction self, Creature cr) =>
                {
                    int diceCount = Math.Max(1, cr.MaximumSpellRank);
                    cr.Heal(DiceFormula.FromText(diceCount + "d8", "dice formula source, where does this show up?"), self);
                    cr.PersistentUsedUpResources.UsedUpActions.Add("StarOrb");
                    QEffect? starOrbEffect = cr.FindQEffect(StarOrbQEffectId);
                    if (starOrbEffect != null) starOrbEffect.ExpiresAt = ExpirationCondition.Immediately;
                }).WithActionCost(1).WithSoundEffect(SfxName.MinorHealing);
        }

        static public CombatAction OrbFocus(Creature owner, int focusPointMax)
        {
            var action = new CombatAction(owner, IllustrationName.Blur, "Orb Focus",
                [KitsuneAncestryLoader.KitsuneTrait],
                $"Recover 1 focus point.",
                Target.Self().WithAdditionalRestriction((Creature cr) =>
                {
                    if (focusPointMax == 0 || cr.Spellcasting == null) return "You don't have a focus point pool to refill.";
                    if (cr.Spellcasting.FocusPoints == focusPointMax) return "You are already at your maximum focus points.";

                    return null;
                }))
                .WithEffectOnSelf(async (CombatAction self, Creature cr) =>
                {
                    if (cr.Spellcasting == null) return; // this will never happen because of the casting restriction
                    cr.Spellcasting.FocusPoints += 1;
                    cr.PersistentUsedUpResources.UsedUpActions.Add("StarOrb");
                    QEffect? starOrbEffect = cr.FindQEffect(StarOrbQEffectId);
                    if (starOrbEffect != null) starOrbEffect.ExpiresAt = ExpirationCondition.Immediately;
                }).WithActionCost(1).WithSoundEffect(SfxName.MinorHealing);
            return action;
        }

        //static public ItemName StarOrbItemName = ModManager.RegisterNewItemIntoTheShop("starOrb", (ItemName name) =>
        //{
        //    return new Item(name, IllustrationName.Rock, "Star Orb", 1, 0, [Trait.DoNotAddToShop, KitsuneAncestryLoader.KitsuneTrait]).WithDescription("{i}This unassuming stone radiates magical power.{/i}\n\nOnce per day, you can activate this star orb in one of two ways:\n\n{b}Activate - Restorative Orb{/b} {icon:Action}; You recover 1d8 hit points times half your level (minimum 1d8)\n\n{b}Activate - Orb Focus{/b} {icon:Action}; You restore 1 focus point to your focus pool, up to your usual maximum.")
        //    .WithPermanentQEffectWhenWorn((QEffect effect, Item item) =>
        //    {
        //        effect.ProvideActionIntoPossibilitySection = (QEffect self, PossibilitySection section) =>
        //        {
        //            if (!(section.PossibilitySectionId == PossibilitySectionId.ItemActions)) return null;
        //            // wanna continue from here and use SubmenuPossibility to expand a menu. then you can have two options in there
        //            else return new SubmenuPossibility(IllustrationName.Rock, "Star Orb Outer")
        //            {
        //                Subsections = [
        //                    new PossibilitySection("Star Orb Inner") {
        //                            Possibilities = [
        //                                new ActionPossibility(RestorativeOrb(self.Owner)),
        //                                new ActionPossibility(CombatAction.CreateSimple(self.Owner, "Orb Focus"))
        //                                ]
        //                        }
        //                    ]
        //            };
        //        };
        //    });
        //});
    }
}
