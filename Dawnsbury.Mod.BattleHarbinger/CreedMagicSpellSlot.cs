using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Display;

namespace Dawnsbury.Mods.BattleHarbinger
{
    public class CreedMagicSpellSlot(int level, string key) : PreparedSpellSlot(level, key)
    {
        public override string SlotName => "Creed magic";

        public List<SpellId> CreedSpells = [SpellId.ResistEnergy,
#if !DAWNSBURY_V2
            SpellId.SeeInvisibility,
#endif
            SpellId.TrueStrike];

#if !DAWNSBURY_V2
        public override string? DisallowsSpellBecause(Spell preparedSpell, CharacterSheet sheet, PreparedSpellSlots preparedSpellSlots)
        {
            if (CreedSpells.Contains(preparedSpell.SpellId))
            {
                return base.DisallowsSpellBecause(preparedSpell, sheet, preparedSpellSlots);
            }

            return "Creed slots only allow {i}resist energy{/i}, {i}see invisibility{/i} and {i}true strike{/i} spells.";
        }
#else
        public override bool AdmitsSpell(Spell preparedSpell, CharacterSheet sheet, PreparedSpellSlots preparedSpellSlots)
        {
            if (CreedSpells.Contains(preparedSpell.SpellId))
            {
                return base.AdmitsSpell(preparedSpell, sheet, preparedSpellSlots);
            }

            return false;
        }
#endif
    }
}

