using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Migrations;

namespace Umbraco.Migration.Contrib.Migrations
{
    public class DeletePropertyDataIndexes : MigrationBase
    {
        public DeletePropertyDataIndexes(IMigrationContext context)
            : base(context)
        { }

        public override void Migrate()
        {            
            Delete.KeysAndIndexes(Constants.DatabaseSchema.Tables.PropertyData).Do();
        }
    }
}
