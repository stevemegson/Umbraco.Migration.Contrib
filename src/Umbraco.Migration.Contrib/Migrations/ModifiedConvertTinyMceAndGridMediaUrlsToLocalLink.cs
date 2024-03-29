﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Umbraco.Core;
using Umbraco.Core.Migrations;
using Umbraco.Core.Migrations.PostMigrations;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.Dtos;
using Umbraco.Core.Services;
using Umbraco.Migration.Contrib.Dtos;

namespace Umbraco.Migration.Contrib.Migrations
{
    public class ModifiedConvertTinyMceAndGridMediaUrlsToLocalLink : MigrationBase
    {
        private readonly IMediaService _mediaService;

        public ModifiedConvertTinyMceAndGridMediaUrlsToLocalLink(IMigrationContext context, IMediaService mediaService) : base(context)
        {
            _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));
        }

        public override void Migrate()
        {
            var mediaLinkPattern = new Regex(
                @"(<a[^>]*href="")(\/ media[^""\?]*)([^>]*>)",
                RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);

            var sqlPropertyData = Sql()
                .Select<PropertyDataDto>(r => r.Select(x => x.PropertyTypeDto, r1 => r1.Select(x => x.DataTypeDto)))
                .From<PropertyDataDto>()
                    .InnerJoin<PropertyTypeDto>().On<PropertyDataDto, PropertyTypeDto>((left, right) => left.PropertyTypeId == right.Id)
                    .InnerJoin<DataTypeDto>().On<PropertyTypeDto, DataTypeDto>((left, right) => left.DataTypeId == right.NodeId)
                .Where<DataTypeDto>(x =>
                    x.EditorAlias == Constants.PropertyEditors.Aliases.TinyMce ||
                    x.EditorAlias == Constants.PropertyEditors.Aliases.Grid);

            var properties = Database.Fetch<PropertyDataDto>(sqlPropertyData);

            var exceptions = new List<Exception>();
            foreach (var property in properties)
            {
                var value = property.TextValue;
                if (string.IsNullOrWhiteSpace(value)) continue;


                bool propertyChanged = false;
                if (property.PropertyTypeDto.DataTypeDto.EditorAlias == Constants.PropertyEditors.Aliases.Grid)
                {
                    try
                    {
                        var obj = JsonConvert.DeserializeObject<JObject>(value);
                        var allControls = obj.SelectTokens("$.sections..rows..areas..controls");

                        foreach (var control in allControls.SelectMany(c => c).OfType<JObject>())
                        {
                            var controlValue = control["value"];
                            if (controlValue?.Type == JTokenType.String)
                            {
                                control["value"] = UpdateMediaUrls(mediaLinkPattern, controlValue.Value<string>(), out var controlChanged);
                                propertyChanged |= controlChanged;
                            }
                        }

                        property.TextValue = JsonConvert.SerializeObject(obj);
                    }
                    catch (JsonException e)
                    {
                        exceptions.Add(new InvalidOperationException(
                            "Cannot deserialize the value as json. This can be because the property editor " +
                            "type is changed from another type into a grid. Old versions of the value in this " +
                            "property can have the structure from the old property editor type. This needs to be " +
                            "changed manually before updating the database.\n" +
                            $"Property info: Id = {property.Id}, LanguageId = {property.LanguageId}, VersionId = {property.VersionId}, Value = {property.Value}"
                            , e));
                        continue;
                    }

                }
                else
                {
                    property.TextValue = UpdateMediaUrls(mediaLinkPattern, value, out propertyChanged);
                }

                if (propertyChanged)
                    Database.Update(property);
            }


            if (exceptions.Any())
            {
                throw new AggregateException("One or more errors related to unexpected data in grid values occurred.", exceptions);
            }

            Context.AddPostMigration<RebuildPublishedSnapshot>();
        }

        private string UpdateMediaUrls(Regex mediaLinkPattern, string value, out bool changed)
        {
            bool matched = false;

            var result = mediaLinkPattern.Replace(value, match =>
            {
                matched = true;

                // match groups:
                // - 1 = from the beginning of the a tag until href attribute value begins
                // - 2 = the href attribute value excluding the querystring (if present)
                // - 3 = anything after group 2 until the a tag is closed
                var href = match.Groups[2].Value;

                var media = _mediaService.GetMediaByPath(href);
                return media == null
                    ? match.Value
                    : $"{match.Groups[1].Value}/{{localLink:{media.GetUdi()}}}{match.Groups[3].Value}";
            });

            changed = matched;

            return result;
        }
    }
}
