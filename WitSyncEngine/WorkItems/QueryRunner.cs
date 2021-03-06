﻿using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    class QueryResult
    {
        public IDictionary<int,WorkItem> WorkItems;
        public IList<WorkItemLinkInfo> Links;
        // this is interesting when reasoning on links
        public QueryType QueryType;
    }

    // derived from http://www.colinsalmcorner.com/post/using-the-tfs-api-to-display-results-of-a-hierarchical-work-item-query
    class QueryRunner
    {
        public WorkItemStore WorkItemStore { get; private set; }
        public string TeamProjectName { get; private set; }
        public string CurrentUserDisplayName { get; private set; }

        public QueryRunner(WorkItemStore workItemStore, string teamProjectName)
        {
            WorkItemStore = workItemStore;
            TeamProjectName = teamProjectName;
        }

        public QueryResult RunQuery(string queryPathOrText)
        {
            string queryText = string.Empty;
            var queryType = QueryType.Invalid;

            if (queryPathOrText.StartsWith("SELECT ", StringComparison.InvariantCultureIgnoreCase))
            {
                // explicit query
                queryText = queryPathOrText;
                queryType = QueryType.List;
            }
            else
            {
                // name of existing query definition
                var rootQueryFolder = WorkItemStore.Projects[TeamProjectName].QueryHierarchy as QueryFolder;
                var queryDef = FindQuery(queryPathOrText, rootQueryFolder);
                // query not found
                if (queryDef == null)
                    return null;
                queryText = queryDef.QueryText;
                queryType = queryDef.QueryType;
            }

            // get the query
            var query = new Query(WorkItemStore, queryText, GetParamsDictionary());

            // get the link types
            var linkTypes = new List<WorkItemLinkType>(WorkItemStore.WorkItemLinkTypes);

            // run the query
            var dict = new Dictionary<int, WorkItem>();
            var list = new List<WorkItemLinkInfo>();
            if (queryType == QueryType.List)
            {
                foreach (WorkItem wi in query.RunQuery())
                {
                    dict.Add(wi.Id, wi);
                }
            }
            else
            {
                list.AddRange(query.RunLinkQuery());
                foreach (var k in list)
                {
                    if (k.SourceId!=0
                        && !dict.ContainsKey(k.SourceId))
                            dict.Add(k.SourceId, WorkItemStore.GetWorkItem(k.SourceId));
                    if (!dict.ContainsKey(k.TargetId))
                        dict.Add(k.TargetId, WorkItemStore.GetWorkItem(k.TargetId));
                }
            }
            return new QueryResult() { WorkItems = dict, Links = list, QueryType = queryType };
        }

        private static QueryDefinition FindQuery(string queryPath, QueryFolder queryFolder)
        {
            var parts = queryPath.Split('\\');
            for (int i = 0; i < parts.Length - 1; i++)
            {
                queryFolder = queryFolder[parts[i]] as QueryFolder;
            }
            string queryName = parts[parts.Length - 1];
            if (!queryFolder.Contains(queryName))
                return null;
            var queryDef = queryFolder[queryName] as QueryDefinition;
            return queryDef;
        }

        private IDictionary GetParamsDictionary()
        {
            return new Dictionary<string, string>() 
			{
				{ "project", TeamProjectName }, 
				{ "me", CurrentUserDisplayName } 
			};
        }
    }
}
