using Dawnsbury.Core.CharacterBuilder.Spellcasting;
using Dawnsbury.Core.CharacterBuilder;
using Dawnsbury.Display;
using Dawnsbury.Mods.BattleHarbinger.RegisteredValues;
using Dawnsbury.Core.CharacterBuilder.FeatsDb.Spellbook;
using Dawnsbury.Core.Mechanics.Enumerations;

namespace Dawnsbury.Mods.BattleHarbinger
{
    public class BattleFontSpellSlot(int level, string key) : FreePreparedSpellSlot(level, key)
    {
        public override string SlotName => "Battle font";

        public List<SpellId> BattleAuras = [ModSpellId.BattleBless, ModSpellId.BattleBane];

        public BattleFontSpellSlot(int level, string key, List<SpellId> extraBattleAuras) : this(level, key)
        {
            BattleAuras.AddRange(extraBattleAuras);
            // Change the key when more auras are added, to prevent reducing a character's level and leaving illegal spells prepared
            foreach (SpellId spell in extraBattleAuras)
            {
                Key += spell.HumanizeTitleCase2();
            }
        }

        public override string? DisallowsSpellBecause(Spell preparedSpell, CharacterSheet sheet, PreparedSpellSlots preparedSpellSlots)
        {
            if (BattleAuras.Contains(preparedSpell.SpellId))
            {
                return base.DisallowsSpellBecause(preparedSpell, sheet, preparedSpellSlots);
            }
            string allowed = BattleAuras.Select(id => AllSpells.CreateModernSpellTemplate(id, Trait.Cleric).Name).Aggregate((x, y) => $"{x}\n{y}");
            return "You can only prepare these spells in your battle font slots:\n" + allowed;
        }

    }
}

