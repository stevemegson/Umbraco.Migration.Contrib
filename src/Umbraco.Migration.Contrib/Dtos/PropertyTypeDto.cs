﻿using NPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Umbraco.Core;
using Umbraco.Core.Persistence.DatabaseAnnotations;
using Umbraco.Core.Persistence.DatabaseModelDefinitions;

namespace Umbraco.Migration.Contrib.Dtos
{
    [TableName(Constants.DatabaseSchema.Tables.PropertyType)]
    [PrimaryKey("id")]
    [ExplicitColumns]
    internal class PropertyTypeDto
    {
        [Column("id")]
        [PrimaryKeyColumn(IdentitySeed = 50)]
        public int Id { get; set; }

        [Column("dataTypeId")]
        public int DataTypeId { get; set; }

        [Column("contentTypeId")]
        [ForeignKey(typeof(ContentTypeDto), Column = "nodeId")]
        public int ContentTypeId { get; set; }

        [Column("propertyTypeGroupId")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? PropertyTypeGroupId { get; set; }

        [Index(IndexTypes.NonClustered, Name = "IX_cmsPropertyTypeAlias")]
        [Column("Alias")]
        public string Alias { get; set; }

        [Column("Name")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Name { get; set; }

        [Column("sortOrder")]
        [Constraint(Default = "0")]
        public int SortOrder { get; set; }

        [Column("mandatory")]
        [Constraint(Default = "0")]
        public bool Mandatory { get; set; }

        [Column("validationRegExp")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string ValidationRegExp { get; set; }

        [Column("Description")]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Length(2000)]
        public string Description { get; set; }

        [Column("variations")]
        [Constraint(Default = "1" /*ContentVariation.InvariantNeutral*/)]
        public byte Variations { get; set; }

        [ResultColumn]
        [Reference(ReferenceType.OneToOne, ColumnName = "DataTypeId")]
        public DataTypeDto DataTypeDto { get; set; }

        [Column("UniqueID")]
        [NullSetting(NullSetting = NullSettings.NotNull)]
        [Constraint(Default = SystemMethods.NewGuid)]
        [Index(IndexTypes.UniqueNonClustered, Name = "IX_cmsPropertyTypeUniqueID")]
        public Guid UniqueId { get; set; }
    }

}
