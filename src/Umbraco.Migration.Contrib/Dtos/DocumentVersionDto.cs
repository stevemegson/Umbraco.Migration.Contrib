using NPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Persistence.DatabaseAnnotations;

namespace Umbraco.Migration.Contrib.Dtos
{
    [TableName(TableName)]
    [PrimaryKey("id", AutoIncrement = false)]
    [ExplicitColumns]
    internal class DocumentVersionDto
    {
        private const string TableName = Constants.DatabaseSchema.Tables.DocumentVersion;

        [Column("id")]
        [PrimaryKeyColumn(AutoIncrement = false)]
        [ForeignKey(typeof(ContentVersionDto))]
        public int Id { get; set; }

        [Column("templateId")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? TemplateId { get; set; }

        [Column("published")]
        public bool Published { get; set; }

        [ResultColumn]
        [Reference(ReferenceType.OneToOne)]
        public ContentVersionDto ContentVersionDto { get; set; }
    }

}
