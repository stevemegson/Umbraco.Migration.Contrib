﻿using NPoco;
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
    [PrimaryKey("id")]
    [ExplicitColumns]
    internal class PropertyDataDto
    {
        public const string TableName = Constants.DatabaseSchema.Tables.PropertyData;
        public const int VarcharLength = 512;
        public const int SegmentLength = 256;

        private decimal? _decimalValue;

        // pk, not used at the moment (never updating)
        [Column("id")]
        [PrimaryKeyColumn]
        public int Id { get; set; }

        [Column("versionId")]
        [Index(IndexTypes.UniqueNonClustered, Name = "IX_" + TableName + "_VersionId", ForColumns = "versionId,propertyTypeId,languageId,segment")]
        public int VersionId { get; set; }

        [Column("propertyTypeId")]
        [ForeignKey(typeof(PropertyTypeDto))]
        [Index(IndexTypes.NonClustered, Name = "IX_" + TableName + "_PropertyTypeId")]
        public int PropertyTypeId { get; set; }

        [Column("languageId")]
        [Index(IndexTypes.NonClustered, Name = "IX_" + TableName + "_LanguageId")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? LanguageId { get; set; }

        [Column("segment")]
        [Index(IndexTypes.NonClustered, Name = "IX_" + TableName + "_Segment")]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Length(SegmentLength)]
        public string Segment { get; set; }

        [Column("intValue")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? IntegerValue { get; set; }

        [Column("decimalValue")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public decimal? DecimalValue
        {
            get => _decimalValue;
            set => _decimalValue = value?.Normalize();
        }

        [Column("dateValue")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public DateTime? DateValue { get; set; }

        [Column("varcharValue")]
        [NullSetting(NullSetting = NullSettings.Null)]
        [Length(VarcharLength)]
        public string VarcharValue { get; set; }

        [Column("textValue")]
        [NullSetting(NullSetting = NullSettings.Null)]
        [SpecialDbType(SpecialDbTypes.NTEXT)]
        public string TextValue { get; set; }

        [ResultColumn]
        [Reference(ReferenceType.OneToOne, ColumnName = "PropertyTypeId")]
        public PropertyTypeDto PropertyTypeDto { get; set; }

        [Ignore]
        public object Value
        {
            get
            {
                if (IntegerValue.HasValue)
                    return IntegerValue.Value;

                if (DecimalValue.HasValue)
                    return DecimalValue.Value;

                if (DateValue.HasValue)
                    return DateValue.Value;

                if (!string.IsNullOrEmpty(VarcharValue))
                    return VarcharValue;

                if (!string.IsNullOrEmpty(TextValue))
                    return TextValue;

                return null;
            }
        }

        public PropertyDataDto Clone(int versionId)
        {
            return new PropertyDataDto
            {
                VersionId = versionId,
                PropertyTypeId = PropertyTypeId,
                LanguageId = LanguageId,
                Segment = Segment,
                IntegerValue = IntegerValue,
                DecimalValue = DecimalValue,
                DateValue = DateValue,
                VarcharValue = VarcharValue,
                TextValue = TextValue,
                PropertyTypeDto = PropertyTypeDto
            };
        }

        protected bool Equals(PropertyDataDto other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object other)
        {
            return
                !ReferenceEquals(null, other) // other is not null
                && (ReferenceEquals(this, other) // and either ref-equals, or same id
                    || other is PropertyDataDto pdata && pdata.Id == Id);
        }

        public override int GetHashCode()
        {
            // ReSharper disable once NonReadonlyMemberInGetHashCode
            return Id;
        }
    }

}
