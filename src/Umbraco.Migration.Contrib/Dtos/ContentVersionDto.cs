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
    [TableName(TableName)]
    [PrimaryKey("id")]
    [ExplicitColumns]
    internal class ContentVersionDto
    {
        public const string TableName = Constants.DatabaseSchema.Tables.ContentVersion;
        private int? _userId;

        [Column("id")]
        [PrimaryKeyColumn]
        public int Id { get; set; }

        [Column("nodeId")]
        [ForeignKey(typeof(ContentDto))]
        public int NodeId { get; set; }

        [Column("versionDate")] // TODO: db rename to 'updateDate'
        [Constraint(Default = SystemMethods.CurrentDateTime)]
        public DateTime VersionDate { get; set; }

        [Column("userId")] // TODO: db rename to 'updateUserId'
        [NullSetting(NullSetting = NullSettings.Null)]
        public int? UserId { get => _userId == 0 ? null : _userId; set => _userId = value; } //return null if zero

        // TODO: we need an index on this it is used almost always in querying and sorting
        [Column("current")]
        public bool Current { get; set; }

        // about current:
        // there is nothing in the DB that guarantees that there will be one, and exactly one, current version per content item.
        // that would require circular FKs that are impossible (well, it is possible to create them, but not to insert).
        // we could use a content.currentVersionId FK that would need to be nullable, or (better?) an additional table
        // linking a content itemt to its current version (nodeId, versionId) - that would guarantee uniqueness BUT it would
        // not guarantee existence - so, really... we are trusting our code to manage 'current' correctly.

        [Column("text")]
        [NullSetting(NullSetting = NullSettings.Null)]
        public string Text { get; set; }

        [ResultColumn]
        [Reference(ReferenceType.OneToOne, ColumnName = "NodeId", ReferenceMemberName = "NodeId")]
        public ContentDto ContentDto { get; set; }
    }

}
