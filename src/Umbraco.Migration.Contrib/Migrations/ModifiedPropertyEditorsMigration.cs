using System;
using Umbraco.Core;
using Umbraco.Core.Migrations;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.Dtos;
using Umbraco.Migration.Contrib.Dtos;

namespace Umbraco.Migration.Contrib.Migrations
{
    public class ModifiedPropertyEditorsMigration : MigrationBase
    {
        public ModifiedPropertyEditorsMigration(IMigrationContext context)
            : base(context)
        { }

        public override void Migrate()
        {
            RenameDataType(/* Constants.PropertyEditors.Legacy.Aliases.ContentPicker */ "Umbraco.ContentPickerAlias", Constants.PropertyEditors.Aliases.ContentPicker);
            RenameDataType(Constants.PropertyEditors.Legacy.Aliases.ContentPicker2, Constants.PropertyEditors.Aliases.ContentPicker);
            RenameDataType(Constants.PropertyEditors.Legacy.Aliases.MediaPicker2, Constants.PropertyEditors.Aliases.MediaPicker);
            RenameDataType(Constants.PropertyEditors.Aliases.MultipleMediaPicker, Constants.PropertyEditors.Aliases.MediaPicker);
            RenameDataType(Constants.PropertyEditors.Legacy.Aliases.MemberPicker2, Constants.PropertyEditors.Aliases.MemberPicker);
            RenameDataType(Constants.PropertyEditors.Legacy.Aliases.MultiNodeTreePicker2, Constants.PropertyEditors.Aliases.MultiNodeTreePicker);
            RenameDataType(Constants.PropertyEditors.Legacy.Aliases.TextboxMultiple, Constants.PropertyEditors.Aliases.TextArea);
            RenameDataType(Constants.PropertyEditors.Legacy.Aliases.Textbox, Constants.PropertyEditors.Aliases.TextBox);
        }

        private void RenameDataType(string fromAlias, string toAlias)
        {
            Database.Execute(Sql()
                .Update<DataTypeDto>(u => u.Set(x => x.EditorAlias, toAlias))
                .Where<DataTypeDto>(x => x.EditorAlias == fromAlias));
        }
    }
}
