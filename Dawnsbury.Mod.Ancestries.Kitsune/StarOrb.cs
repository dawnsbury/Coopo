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
                [KitsuneAncestryLoader.KitsuneTrait, Trait.Concentrate],
                $"Recover {S.HeightenedVariable(owner.MaximumSpellRank, 1)}d8 hit points.",
                Target.Self()).WithEffectOnSelf(async (CombatAction self, Creature cr) =>
                {
                    int diceCount = Math.Max(1, cr.MaximumSpellRank);
                    await cr.HealAsync(DiceFormula.FromText(diceCount + "d8", "Orb Restoration"), self);
                    cr.PersistentUsedUpResources.UsedUpActions.Add("StarOrb");
                    QEffect? starOrbEffect = cr.FindQEffect(StarOrbQEffectId);
                    if (starOrbEffect != null) starOrbEffect.ExpiresAt = ExpirationCondition.Immediately;
                }).WithActionCost(1).WithSoundEffect(SfxName.MinorHealing);
        }

        static public CombatAction OrbFocus(Creature owner, int focusPointMax)
        {
            var action = new CombatAction(owner, IllustrationName.Blur, "Orb Focus",
                [KitsuneAncestryLoader.KitsuneTrait, Trait.Concentrate],
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
    }
}
