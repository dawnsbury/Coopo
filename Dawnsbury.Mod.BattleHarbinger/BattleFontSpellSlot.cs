using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CharacterBuilder;

namespace Dawnsbury.Mods.BattleHarbinger
{
    public class BattleFontSpellSlot(int level, string key) : PreparedSpellSlot(level, key)
    {
        public override string SlotName => "Battle font";

        public override string? DisallowsSpellBecause(Spell preparedSpell, CharacterSheet sheet, PreparedSpellSlots preparedSpellSlots)
        {
            if (preparedSpell.SpellId == SpellId.Bless || preparedSpell.SpellId == SpellId.Bane)
            {
                return base.DisallowsSpellBecause(preparedSpell, sheet, preparedSpellSlots);
            }

            return "Battle Font slots only allow {i}battle aura{/i} spells such as {i}bless{/i} and {i}bane{/i}.";
        }
    }
}
