using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Core.Migrations;
using Umbraco.Core.Migrations.PostMigrations;
using Umbraco.Core.Migrations.Upgrade.V_8_0_0;
using Umbraco.Core.Models;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.Dtos;
using Umbraco.Core.PropertyEditors;
using Umbraco.Migration.Contrib.Dtos;

namespace Umbraco.Migration.Contrib.Migrations
{
    public class NestedContentPropertyEditors : PropertyEditorsMigrationBase
    {
        private Dictionary<string, int> _elementTypeIds;
        private Dictionary<int, List<PropertyTypeDto>> _propertyTypes;
        private HashSet<int> _elementTypesInUse;

        private ConfigurationEditor _valueListConfigEditor;

        private Lazy<Dictionary<int, NodeDto>> _nodeIdToKey;

        public NestedContentPropertyEditors(IMigrationContext context)
            : base(context)
        { }

        public override void Migrate()
        {
            Prepare();

            bool refreshCache = UpdatePropertyData();
            refreshCache |= UpdateElementTypes();

            // if some data types have been updated directly in the database (editing DataTypeDto and/or PropertyDataDto),
            // bypassing the services, then we need to rebuild the cache entirely, including the umbracoContentNu table
            if (refreshCache)
                Context.AddPostMigration<RebuildPublishedSnapshot>();
        }

        private void Prepare()
        {
            _elementTypeIds = Database.Fetch<ContentTypeDto>(Sql()
                .Select<ContentTypeDto>(x => x.NodeId, x => x.Alias)
                .From<ContentTypeDto>()
                .InnerJoin<NodeDto>().On<ContentTypeDto, NodeDto>(left => left.NodeId, right => right.NodeId)
                .Where<NodeDto>(node => node.NodeObjectType == Constants.ObjectTypes.DocumentType))
                .ToDictionary(ct => ct.Alias, ct => ct.NodeId);

            _valueListConfigEditor = new ValueListConfigurationEditor();

            _elementTypesInUse = new HashSet<int>();
            _propertyTypes = new Dictionary<int, List<PropertyTypeDto>>();

            _nodeIdToKey = new Lazy<Dictionary<int, NodeDto>>(
                () => Context.Database.Fetch<NodeDto>(
                    Context.SqlContext.Sql()
                        .Select<NodeDto>(x => x.NodeId, x => x.NodeObjectType, x => x.UniqueId)
                        .From<NodeDto>()
                    ).ToDictionary(n => n.NodeId)
                );
        }

        private bool UpdatePropertyData()
        {
            var refreshCache = false;

            var dataTypes = GetDataTypes(Constants.PropertyEditors.Aliases.NestedContent);
            foreach (var dataType in dataTypes)
            {
                // get property data dtos
                var propertyDataDtos = Database.Fetch<PropertyDataDto>(Sql()
                    .Select<PropertyDataDto>()
                    .From<PropertyDataDto>()
                    .InnerJoin<PropertyTypeDto>().On<PropertyTypeDto, PropertyDataDto>((pt, pd) => pt.Id == pd.PropertyTypeId)
                    .InnerJoin<DataTypeDto>().On<DataTypeDto, PropertyTypeDto>((dt, pt) => dt.NodeId == pt.DataTypeId)
                    .Where<PropertyTypeDto>(x => x.DataTypeId == dataType.NodeId));

                // update dtos
                var updatedDtos = propertyDataDtos.Where(x => UpdateNestedPropertyDataDto(x)).ToArray();

                // persist changes
                foreach (var propertyDataDto in updatedDtos)
                {
                    Database.Update(propertyDataDto);
                    refreshCache = true;
                }
            }

            return refreshCache;
        }

        private bool UpdateNestedPropertyDataDto(PropertyDataDto pd)
        {
            if (UpdateNestedContent(pd.TextValue, out string newValue))
            {
                pd.TextValue = newValue;
                return true;
            }

            return false;
        }

        private bool UpdateNestedContent(string inputValue, out string newValue)
        {
            bool changed = false;
            newValue = inputValue;

            if (String.IsNullOrWhiteSpace(inputValue))
                return false;

            var elements = JsonConvert.DeserializeObject<List<JObject>>(inputValue);
            foreach (var element in elements)
            {
                var elementTypeAlias = element["ncContentTypeAlias"]?.ToObject<string>();
                if (string.IsNullOrEmpty(elementTypeAlias))
                    continue;
                changed |= UpdateElement(element, elementTypeAlias);
            }

            if (changed)
                newValue = JsonConvert.SerializeObject(elements);

            return changed;
        }

        private bool UpdateElement(JObject element, string elementTypeAlias)
        {
            bool changed = false;

            var elementTypeId = _elementTypeIds[elementTypeAlias];
            _elementTypesInUse.Add(elementTypeId);

            var propertyValues = element.ToObject<Dictionary<string, string>>();
            if (!propertyValues.TryGetValue("key", out var keyo)
                || !Guid.TryParse(keyo.ToString(), out var key))
            {
                changed = true;
                element["key"] = Guid.NewGuid();
            }

            var propertyTypes = GetPropertyTypes(elementTypeId);

            foreach (var pt in propertyTypes)
            {
                if (!propertyValues.ContainsKey(pt.Alias) || String.IsNullOrWhiteSpace(propertyValues[pt.Alias]))
                    continue;

                var propertyValue = propertyValues[pt.Alias];

                switch (pt.DataTypeDto.EditorAlias)
                {
                    case "Umbraco.ContentPickerAlias":
                    case Constants.PropertyEditors.Aliases.MemberPicker:
                    case Constants.PropertyEditors.Aliases.MediaPicker:
                    case Constants.PropertyEditors.Aliases.MultipleMediaPicker:
                    case Constants.PropertyEditors.Aliases.MultiNodeTreePicker:
                        if (UpdateLegacyPicker(propertyValue, out string newPropertyValue))
                        {
                            element[pt.Alias] = newPropertyValue;
                            changed = true;
                        }
                            
                        break;

                    case Constants.PropertyEditors.Aliases.RadioButtonList:
                    case Constants.PropertyEditors.Aliases.CheckBoxList:
                    case Constants.PropertyEditors.Aliases.DropDownListFlexible:
                        var config = (ValueListConfiguration)_valueListConfigEditor.FromDatabase(pt.DataTypeDto.Configuration);
                        bool isMultiple = true;
                        if (pt.DataTypeDto.EditorAlias == Constants.PropertyEditors.Aliases.RadioButtonList)
                            isMultiple = false;
                        element[pt.Alias] = UpdateValueList(propertyValue, config, isMultiple);
                        changed = true;
                        break;

                    case Constants.PropertyEditors.Aliases.NestedContent:
                        if (UpdateNestedContent(propertyValue, out string newNestedContentValue))
                        {
                            element[pt.Alias] = newNestedContentValue;
                            changed = true;
                        }
                        break;

                    case Constants.PropertyEditors.Aliases.MultiUrlPicker:
                        if (string.IsNullOrWhiteSpace(propertyValue))
                            continue;
                        element[pt.Alias] = ConvertRelatedLinksToMultiUrlPicker(propertyValue);
                        changed = true;
                        break;
                }
            }

            return changed;
        }

        private List<PropertyTypeDto> GetPropertyTypes(int elementTypeId)
        {
            if (_propertyTypes.TryGetValue(elementTypeId, out var result))
            {
                return result;
            }
            else
            {
                result = Database.Fetch<PropertyTypeDto>(Sql()
                        .Select<PropertyTypeDto>(r => r.Select(x => x.DataTypeDto))
                        .From<PropertyTypeDto>()
                        .InnerJoin<DataTypeDto>().On<PropertyTypeDto, DataTypeDto>((pt, dt) => pt.DataTypeId == dt.NodeId)
                        .Where<PropertyTypeDto>(pt => pt.ContentTypeId == elementTypeId)
                        );
                _propertyTypes[elementTypeId] = result;

                return result;
            }
        }

        private string UpdateValueList(string propertyValue, ValueListConfiguration config, bool isMultiple)
        {
            var propData = new PropertyDataDto { VarcharValue = propertyValue };

            if (UpdatePropertyDataDto(propData, config, isMultiple: isMultiple))
            {
                return propData.VarcharValue;
            }

            return propertyValue;
        }

        private bool UpdateLegacyPicker(string propertyValue, out string newPropertyValue)
        {
            newPropertyValue = null;

            //Get the INT ids stored for this property/drop down
            int[] ids = null;
            if (!propertyValue.IsNullOrWhiteSpace())
            {
                ids = ConvertStringValues(propertyValue);
            }

            if (ids == null || ids.Length <= 0) return false;

            // map ids to values
            var values = new List<Udi>();

            foreach (var id in ids)
            {
                if (_nodeIdToKey.Value.TryGetValue(id, out var node))
                {
                    values.Add(Udi.Create(ObjectTypes.GetUdiType(node.NodeObjectType.Value), node.UniqueId));
                }
            }
            newPropertyValue = String.Join(",", values);

            return true;
        }

        private string ConvertRelatedLinksToMultiUrlPicker(string value)
        {
            var relatedLinks = JsonConvert.DeserializeObject<List<RelatedLink>>(value);
            var links = new List<LinkDto>();
            foreach (var relatedLink in relatedLinks)
            {
                GuidUdi udi = null;
                if (relatedLink.IsInternal)
                {
                    var linkIsUdi = GuidUdi.TryParse(relatedLink.Link, out udi);
                    if (linkIsUdi == false)
                    {
                        // oh no.. probably an integer, yikes!
                        if (int.TryParse(relatedLink.Link, out var intId))
                        {
                            var sqlNodeData = Sql()
                                .Select<NodeDto>()
                                .Where<NodeDto>(x => x.NodeId == intId);

                            var node = Database.Fetch<NodeDto>(sqlNodeData).FirstOrDefault();
                            if (node != null)
                                // Note: RelatedLinks did not allow for picking media items,
                                // so if there's a value this will be a content item - hence
                                // the hardcoded "document" here
                                udi = new GuidUdi("document", node.UniqueId);
                        }
                    }
                }

                var link = new LinkDto
                {
                    Name = relatedLink.Caption,
                    Target = relatedLink.NewWindow ? "_blank" : null,
                    Udi = udi,
                    // Should only have a URL if it's an external link otherwise it wil be a UDI
                    Url = relatedLink.IsInternal == false ? relatedLink.Link : null
                };

                links.Add(link);
            }

            return JsonConvert.SerializeObject(links);
        }

        private bool UpdateElementTypes()
        {
            bool refreshCache = false;

            var documentContentTypesInUse = Database.Fetch<ContentDto>(Sql()
                .SelectDistinct<ContentDto>(x => x.ContentTypeId)
                .From<ContentDto>())
                .Select(x => x.ContentTypeId)
                .ToHashSet();

            var childContentTypes = Database.Fetch<ContentType2ContentTypeDto>(Sql()
                .SelectDistinct<ContentType2ContentTypeDto>(x => x.ChildId)
                .From<ContentType2ContentTypeDto>())
                .Select(x => x.ChildId)
                .ToHashSet();

            foreach (var elementTypeId in _elementTypesInUse)
            {
                string elementTypeAlias = _elementTypeIds.First(t => t.Value == elementTypeId).Key;

                if (documentContentTypesInUse.Contains(elementTypeId))
                {
                    Logger.Warn<NestedContentPropertyEditors>("Content type {ContentTypeAlias} is used in nested content but could not be converted to an element type (documents exist)", elementTypeAlias);
                }
                else if (childContentTypes.Contains(elementTypeId))
                {
                    Logger.Warn<NestedContentPropertyEditors>("Content type {ContentTypeAlias} is used in nested content but could not be converted to an element type (has compositions)", elementTypeAlias);
                }
                else
                {
                    Database.Execute(Sql().Update<ContentTypeDto>(u => u.Set(x => x.IsElement, true)).Where<ContentTypeDto>(x => x.NodeId == elementTypeId));
                    Logger.Info<NestedContentPropertyEditors>("Marked content type {ContentTypeAlias} as an element type", elementTypeAlias);
                    refreshCache = true;
                }
            }

            return refreshCache;
        }

        private bool UpdatePropertyDataDto(PropertyDataDto propData, ValueListConfiguration config, bool isMultiple)
        {
            //Get the INT ids stored for this property/drop down
            int[] ids = null;
            if (!propData.VarcharValue.IsNullOrWhiteSpace())
            {
                ids = ConvertStringValues(propData.VarcharValue);
            }
            else if (!propData.TextValue.IsNullOrWhiteSpace())
            {
                ids = ConvertStringValues(propData.TextValue);
            }
            else if (propData.IntegerValue.HasValue)
            {
                ids = new[] { propData.IntegerValue.Value };
            }

            // if there are INT ids, convert them to values based on the configuration
            if (ids == null || ids.Length <= 0) return false;

            // map ids to values
            var values = new List<string>();
            var canConvert = true;

            foreach (var id in ids)
            {
                var val = config.Items.FirstOrDefault(x => x.Id == id);
                if (val != null)
                {
                    values.Add(val.Value);
                    continue;
                }

                Logger.Warn(GetType(), "Could not find PropertyData {PropertyDataId} value '{PropertyValue}' in the datatype configuration: {Values}.",
                    propData.Id, id, string.Join(", ", config.Items.Select(x => x.Id + ":" + x.Value)));
                canConvert = false;
            }

            if (!canConvert) return false;

            propData.VarcharValue = isMultiple ? JsonConvert.SerializeObject(values) : values[0];
            propData.TextValue = null;
            propData.IntegerValue = null;
            return true;
        }

        private List<DataTypeDto> GetDataTypes(string editorAlias, bool strict = true)
        {
            var sql = Sql()
                .Select<DataTypeDto>()
                .From<DataTypeDto>();

            sql = strict
                ? sql.Where<DataTypeDto>(x => x.EditorAlias == editorAlias)
                : sql.Where<DataTypeDto>(x => x.EditorAlias.Contains(editorAlias));

            return Database.Fetch<DataTypeDto>(sql);
        }

        internal class RelatedLink
        {
            public int? Id { get; internal set; }
            internal bool IsDeleted { get; set; }
            [JsonProperty("caption")]
            public string Caption { get; set; }
            [JsonProperty("link")]
            public string Link { get; set; }
            [JsonProperty("newWindow")]
            public bool NewWindow { get; set; }
            [JsonProperty("isInternal")]
            public bool IsInternal { get; set; }
        }

        [DataContract]
        internal class LinkDto
        {
            [DataMember(Name = "name")]
            public string Name { get; set; }

            [DataMember(Name = "target")]
            public string Target { get; set; }

            [DataMember(Name = "udi")]
            public GuidUdi Udi { get; set; }

            [DataMember(Name = "url")]
            public string Url { get; set; }
        }

    }
}
