using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Display;

namespace Dawnsbury.Mods.BattleHarbinger
{
    public class BattleFontSpellSlot(int level, string key) : PreparedSpellSlot(level, key)
    {
        public override string SlotName => "Battle font";

        public List<SpellId> BattleAuras = [SpellId.Bless, SpellId.Bane];

        public BattleFontSpellSlot(int level, string key, List<SpellId> extraBattleAuras) : this(level, key)
        {
            BattleAuras.AddRange(extraBattleAuras);
            // Change the key when more auras are added, to prevent reducing a character's level and leaving illegal spells prepared
            foreach (SpellId spell in extraBattleAuras)
            {
                Key += spell.HumanizeLowerCase2();
            }
        }

        public override string? DisallowsSpellBecause(Spell preparedSpell, CharacterSheet sheet, PreparedSpellSlots preparedSpellSlots)
        {
            if (BattleAuras.Contains(preparedSpell.SpellId))
            {
                return base.DisallowsSpellBecause(preparedSpell, sheet, preparedSpellSlots);
            }

            return "Battle Font slots only allow {i}battle aura{/i} spells such as {i}bless{/i} and {i}bane{/i}.";
        }
    }
}

