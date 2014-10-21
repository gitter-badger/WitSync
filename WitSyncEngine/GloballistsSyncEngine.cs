﻿using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace WitSync
{

    public class GlobalListsSyncEngine : EngineBase
    {
        public GlobalListsSyncEngine(TfsConnection source, TfsConnection dest, IEngineEvents eventHandler)
            : base(source, dest, eventHandler)
        {
            //no-op
        }

        public Func<GlobalListMapping> MapGetter { get; set; }

        protected GlobalListMapping mapping;

        public override int Prepare(bool testOnly)
        {
            mapping = MapGetter();
            return 0;
        }

        public override int Execute(bool testOnly)
        {
            var sourceWIStore = sourceConn.Collection.GetService<WorkItemStore>();
            var destWIStore = destConn.Collection.GetService<WorkItemStore>();

            var updateList = new List<XmlElement>();

            eventSink.ReadingGlobalListsFromSource();

            // get Global Lists from TFS collection 
            var sourceGL = sourceWIStore.ExportGlobalLists();

            eventSink.SelectingGlobalLists();

            // read the XML and get only the GLOBALLIST element. 
            foreach (XmlElement glElement in sourceGL.GetElementsByTagName("GLOBALLIST"))
            {
                string glName = glElement.Attributes["name"].Value.ToString();

                if (mapping.IsIncluded(glName))
                {
                    eventSink.GlobalListQueuedForUpdate(glName);
                    // queue
                    updateList.Add(glElement);
                }//if
            }//for list

            eventSink.BuildingGlobalListUpdateMessage();

            XmlDocument updateDoc = new XmlDocument();
            XmlProcessingInstruction _xmlProcessingInstruction;
            _xmlProcessingInstruction = updateDoc.CreateProcessingInstruction("xml", "version='1.0' encoding='utf-8'");
            updateDoc.AppendChild(_xmlProcessingInstruction);
            XmlElement updateRoot = updateDoc.CreateElement("gl", "GLOBALLISTS", "http://schemas.microsoft.com/VisualStudio/2005/workitemtracking/globallists");
            foreach (var glSourceElement in updateList)
            {
                var glUpdateElement = updateDoc.ImportNode(glSourceElement, true);
                updateRoot.AppendChild(glUpdateElement);
            }//for
            updateDoc.AppendChild(updateRoot);

            eventSink.UpdatingGlobalListsOnDestination();

            destWIStore.ImportGlobalLists(updateDoc.InnerXml);

            eventSink.GlobalListsUpdated();

            return saveErrors;
        }
    }
}
