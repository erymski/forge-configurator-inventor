﻿/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Design Automation team for Inventor
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Shared;
using WebApplication.Definitions;
using WebApplication.Middleware;
using WebApplication.State;

namespace WebApplication.Utilities
{
    /// <summary>
    /// Utility class to deal with stuff like URLs in generated DTOs.
    /// </summary>
    public class DtoGenerator
    {
        private readonly LinkGenerator _linkGenerator;
        private readonly LocalCache _localCache;
        private readonly UserResolver _userResolver;

        // HACK!
        public static string Host { get; set; }

        public DtoGenerator(LinkGenerator linkGenerator, LocalCache localCache, UserResolver userResolver)
        {
            _linkGenerator = linkGenerator;
            _localCache = localCache;
            _userResolver = userResolver;
        }

        /// <summary>
        /// Create ProjectDTOBase based DTO and fill its properties.
        /// </summary>
        public async Task<TProjectDTOBase> MakeProjectDTOAsync<TProjectDTOBase>(ProjectStorage projectStorage, string hash, OssBucket bucket) where TProjectDTOBase: ProjectDTOBase, new()
        {
            
            Project project = projectStorage.Project;
            // TODO: fix workaround for `_linkGenerator` check for null
            var modelDownloadUrl = _linkGenerator?.GetPathByAction(controller: "Download",
                                                                    action: "Model",
                                                                    values: new { projectName = project.Name, hash });

            var bomDownloadUrl = _linkGenerator?.GetPathByAction(controller: "Download",
                                                                    action: "BOM",
                                                                    values: new { projectName = project.Name, hash });

            var bomJsonUrl = _linkGenerator?.GetPathByAction(controller: "ProjectData",
                                                                    action: "GetBOM",
                                                                    values: new { projectName = project.Name, hash });

            var localNames = project.LocalNameProvider(hash);
            var ossNames = projectStorage.GetOssNames(hash);
            return new TProjectDTOBase
                {
                    Svf = _localCache.ToDataUrl(localNames.SvfDir),
                    BomDownloadUrl = bomDownloadUrl,
                    BomJsonUrl = bomJsonUrl,
                    ObjDownloadUrl = await MakeObjDeepLink(bucket, ossNames),
                    ModelDownloadUrl = modelDownloadUrl,
                    Hash = hash,
                };
        }

        /// <summary>
        /// Generate a deep link URI to be used for handling in Android app.
        /// https://developer.android.com/training/app-links/deep-linking
        /// </summary>
        private static async Task<string> MakeObjDeepLink(OssBucket bucket, OSSObjectNameProvider ossNames)
        {
            var ossUrl = await bucket.CreateSignedUrlAsync(ossNames.Obj);
            return ossUrl.Replace("https", "ld2020");
        }

        /// <summary>
        /// Generate project DTO.
        /// </summary>
        public async Task<ProjectDTO> ToDTOAsync(ProjectStorage projectStorage)
        {
            Project project = projectStorage.Project;
            var localAttributes = project.LocalAttributes;

            var dto = await MakeProjectDTOAsync<ProjectDTO>(projectStorage, projectStorage.Metadata.Hash, await _userResolver.GetBucketAsync());
            dto.Id = project.Name;
            dto.Label = !Regex.Match(project.Name, @"[\u0030-\u007a]").Success ? "_" + project.Name : project.Name;
            dto.Image = _localCache.ToDataUrl(localAttributes.Thumbnail);
            dto.IsAssembly = projectStorage.IsAssembly;
            dto.HasDrawing = projectStorage.Metadata.HasDrawings;
            dto.DrawingsListUrl = _localCache.ToDataUrl(localAttributes.DrawingsList);

            // fill array with adoption messages
            if (File.Exists(localAttributes.AdoptMessages))
            {
                // we are interested only in warning messages
                var allMessages = Json.DeserializeFile<List<Message>>(localAttributes.AdoptMessages);
                dto.AdoptWarnings = allMessages
                    .Where(m => m.Severity == Severity.Warning)
                    .Select(m => m.Text)
                    .ToArray();
            }
            else
            {
                dto.AdoptWarnings = Array.Empty<string>();
            }

            return dto;
        }
    }
}
