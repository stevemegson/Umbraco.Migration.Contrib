using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Migrations.Upgrade.V_8_0_0.DataTypes;

namespace Umbraco.Migration.Contrib.Migrations.DataTypes
{
    class LegacyMediaPickerPreValueMigrator : DefaultPreValueMigrator
    {
        private readonly string[] _editors =
        {
            Constants.PropertyEditors.Aliases.MediaPicker,
            Constants.PropertyEditors.Aliases.MultipleMediaPicker
        };

        public override bool CanMigrate(string editorAlias)
            => _editors.Contains(editorAlias);

        public override string GetNewAlias(string editorAlias)
            => Constants.PropertyEditors.Aliases.MediaPicker;

        protected override object GetPreValueValue(PreValueDto preValue)
        {
            if (preValue.Alias == "multiPicker" ||
                preValue.Alias == "onlyImages" ||
                preValue.Alias == "disableFolderSelect")
                return preValue.Value == "1";

            return base.GetPreValueValue(preValue);
        }
    }
}
