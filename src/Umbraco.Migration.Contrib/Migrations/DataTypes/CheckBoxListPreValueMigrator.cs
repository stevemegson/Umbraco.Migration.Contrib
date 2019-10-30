using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core.Migrations.Upgrade.V_8_0_0.DataTypes;
using Umbraco.Core.PropertyEditors;

namespace Umbraco.Migration.Contrib.Migrations.DataTypes
{
    class CheckBoxListPreValueMigrator : IPreValueMigrator
    {
        public bool CanMigrate(string editorAlias)
            => editorAlias == "Umbraco.CheckBoxList";

        public string GetNewAlias(string editorAlias)
            => editorAlias;

        public object GetConfiguration(int dataTypeId, string editorAlias, Dictionary<string, PreValueDto> preValues)
        {
            var config = new ValueListConfiguration();
            foreach (var preValue in preValues.Values)
                config.Items.Add(new ValueListConfiguration.ValueListItem { Id = preValue.Id, Value = preValue.Value });
            return config;
        }
    }
}
