using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Migrations.Upgrade.V_8_0_0.DataTypes;

namespace Umbraco.Migration.Contrib.Migrations.DataTypes
{
    class LegacyContentPickerPreValueMigrator : DefaultPreValueMigrator
    {
        public override bool CanMigrate(string editorAlias)
            => editorAlias == /*Constants.PropertyEditors.Legacy.Aliases.ContentPicker*/ "Umbraco.ContentPickerAlias";

        public override string GetNewAlias(string editorAlias)
            => Constants.PropertyEditors.Aliases.ContentPicker;

        protected override object GetPreValueValue(PreValueDto preValue)
        {
            if (preValue.Alias == "showOpenButton" ||
                preValue.Alias == "ignoreUserStartNodes")
                return preValue.Value == "1";

            return base.GetPreValueValue(preValue);
        }
    }
}
