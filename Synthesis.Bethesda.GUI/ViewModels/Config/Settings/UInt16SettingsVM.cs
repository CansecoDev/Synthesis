using System.Text.Json;

namespace Synthesis.Bethesda.GUI
{
    public class UInt16SettingsVM : BasicSettingsVM<ushort>
    {
        public UInt16SettingsVM(SettingsMeta memberName, object? defaultVal)
            : base(memberName, defaultVal)
        {
        }

        public UInt16SettingsVM()
            : base(SettingsMeta.Empty, default)
        {
        }

        public override ushort Get(JsonElement property) => property.GetUInt16();

        public override ushort GetDefault() => default(ushort);

        public override SettingsNodeVM Duplicate() => new UInt16SettingsVM(Meta, DefaultValue);
    }
}
