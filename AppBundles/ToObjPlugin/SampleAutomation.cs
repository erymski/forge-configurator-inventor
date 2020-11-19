/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by Forge Partner Development
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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using Autodesk.Forge.DesignAutomation.Inventor.Utils;
using Inventor;
using Path = System.IO.Path;

namespace ToObjPlugin
{
    [ComVisible(true)]
    public class SampleAutomation
    {
        private const string OutputFile = "result.obj";
        private readonly InventorServer _inventorApplication;

        public SampleAutomation(InventorServer inventorApp)
        {
            _inventorApplication = inventorApp;
        }

        public void Run(Document doc)
        {
            try
            {
                LogTrace("Processing " + doc.FullFileName);

                using (new HeartBeat())
                {
                    if (doc.DocumentType == DocumentTypeEnum.kPartDocumentObject ||
                        doc.DocumentType == DocumentTypeEnum.kAssemblyDocumentObject) // Assembly.
                    {
                        ApplicationAddIn objExportAddin = _inventorApplication
                                            .ApplicationAddIns
                                            .Cast<ApplicationAddIn>()
                                            .FirstOrDefault(item => item.ClassIdString.Equals("{f539fb09-fc01-4260-a429-1818b14d6bac}", StringComparison.OrdinalIgnoreCase));

                        var translator = (TranslatorAddIn)objExportAddin;

                        if (translator != null)
                        {
                            Trace.TraceInformation("OBJ Export translator is available. Preparing options.");

                            TranslationContext context = _inventorApplication.TransientObjects.CreateTranslationContext();
                            context.Type = IOMechanismEnum.kFileBrowseIOMechanism;

                            // Set translation options
                            NameValueMap options = _inventorApplication.TransientObjects.CreateNameValueMap();
                            if (translator.get_HasSaveCopyAsOptions(doc, context, options))
                            {
                                options.set_Value("ExportFileStructure", 0); // one file
                                options.set_Value("RemoveInternalFacets", true);
                                options.set_Value("ExportUnits", 6); // meters... to match default Unity units
                                options.set_Value("Resolution", 2); // low
                            }

                            DataMedium data = _inventorApplication.TransientObjects.CreateDataMedium();
                            data.FileName = Path.Combine(Directory.GetCurrentDirectory(), OutputFile);

                            translator.SaveCopyAs(doc, context, options, data);
                            LogTrace($"Export OBJ to '{data.FileName}'");
                        }
                        else
                        {
                            LogError("Cannot find OBJ Export translator");
                        }
                    }
                    else
                    {
                        LogError("Document type is not supported");
                    }
                }
            }
            catch (Exception e)
            {
                LogError("Processing failed. " + e.ToString());
            }
        }

        public void RunWithArguments(Document doc, NameValueMap _)
        {
            LogTrace("Redirecting to 'Run'");
            Run(doc);
        }

        #region Logging utilities

        /// <summary>
        /// Log message with 'trace' log level.
        /// </summary>
        private static void LogTrace(string message)
        {
            Trace.TraceInformation(message);
        }

        /// <summary>
        /// Log message with 'error' log level.
        /// </summary>
        private static void LogError(string message)
        {
            Trace.TraceError(message);
        }

        #endregion
    }
}