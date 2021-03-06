﻿using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitSync
{
    class EngineEventHandler : EventHandlerBase, IEngineEvents
    {
        public EngineEventHandler(TraceLevel level, TraceDevice device, string logFile)
            : base(level, device, logFile)
        {
        }

        public void ConnectingSource(TfsConnection sourceConn)
        {
            this.Verbose("Connecting to source {0}.", sourceConn.CollectionUrl);
        }

        public void SourceConnected(TfsConnection sourceConn)
        {
            this.Info("Connected to source {0}\\{1} as {2}.", sourceConn.CollectionUrl, sourceConn.ProjectName, GetUsername(sourceConn));
        }

        public void ConnectingDestination(TfsConnection destConn)
        {
            this.Verbose("Connecting to destination {0}.", destConn.CollectionUrl);
        }

        public void DestinationConnected(TfsConnection destConn)
        {
            this.Info("Connected to destination {0}\\{1} as {2}.", destConn.CollectionUrl, destConn.ProjectName, GetUsername(destConn));
        }

        protected string GetUsername(TfsConnection conn)
        {
            return conn.GetUsername();
        }

        public void SaveError(Exception exception, WorkItem workItem)
        {
            // error message has dot . at the end
            this.Error("While saving workitem #{1} '{2}': {0}", exception.Message, workItem.IsNew ? workItem.TemporaryId : workItem.Id, workItem.Title);
        }

        public void ExecutingSourceQuery(string queryName, TfsConnection sourceConn)
        {
            this.Info("Executing source query {2} on {0}\\{1}.", sourceConn.CollectionUrl, sourceConn.ProjectName, queryName);
        }

        public void ExecutingDestinationQuery(string queryName, TfsConnection destConn)
        {
            this.Info("Executing source query {2} on {0}\\{1}.", destConn.CollectionUrl, destConn.ProjectName, queryName);
        }

        protected DateTimeOffset syncStart;

        public void SyncStarted()
        {
            syncStart = DateTimeOffset.UtcNow;
            this.Info("Synchronization started.");
        }

        public void SyncFinished(int errors)
        {
            var elapsed = DateTimeOffset.UtcNow - syncStart;
            if (errors != 0)
            {
                base.RawOut(ErrorColor, TraceLevel.Normal, "Synchronization completed in {0:d'.'hh':'mm':'ss} with {1} error(s).", elapsed, errors);
            }
            else
            {
                base.RawOut(SuccessColor, TraceLevel.Normal, "Synchronization completed in {0:d'.'hh':'mm':'ss} with no errors.", elapsed);
            }
        }

        public void SavingWorkItems(List<WorkItem> newWorkItems, List<WorkItem> updatedWorkItems)
        {
            int add = newWorkItems.Count;
            int upd = updatedWorkItems.FindAll(w => w.IsDirty).Count;
            this.Info("Saving {0} workitems ({1} new, {2} updated).", add + upd, add, upd);
        }

        public void SavingLinks(List<WorkItemLink> changedLinks, List<WorkItem> validWorkItems)
        {
            // TODO there is more to show here
            int chg = validWorkItems.FindAll(w => w.IsDirty).Count;
            this.Info("Saving {0} links, or {1} workitems.", changedLinks.Count, chg);
        }

        public void SaveErrorInvalidField(WorkItem workItem, Field f)
        {
            this.Verbose("Field '{0}' has invalid value '{1}'.", f.ReferenceName, f.Value);
        }

        public void MakingNewWorkItem(WorkItem source)
        {
            this.Info("Making new Workitem for #{0} [{1}] '{2}'.", source.Id, source.Type.Name, source.Title);
        }

        public void UpdatingExistingWorkItem(WorkItem source, WorkItem target)
        {
            this.Info("Mapping source Workitem #{3} [{4}] '{5}' to target #{0} [{1}] '{2}'."
                , target.Id, target.Type.Name, target.Title
                , source.Id, source.Type.Name, source.Title);
        }

        public void ExistingWorkItemUpdated(WorkItem source, WorkItem target)
        {
            if (target.IsDirty)
            {
                this.Info("Workitem #{0} [{1}] '{2}' updated."
                    , target.Id, target.Type.Name, target.Title);
            }
            else
            {
                this.Info("No changes to Workitem #{0} [{1}] '{2}'."
                    , target.Id, target.Type.Name, target.Title);
            }
        }

        public void NoTargetState(WorkItemMap map, object state)
        {
            this.UniqueWarning("    State '{0}'\'{1}' not found in '{2}'.", map.SourceType, state, map.DestinationType);
        }

        public void NoWildcardAreaRule(WorkItemsStageConfiguration mapping, object sourceValue)
        {
            this.UniqueWarning("    No wildcard Area rule.");
        }

        public void AreaPathNotFoundUsingWildcardRule(WorkItemsStageConfiguration mapping, object sourceValue)
        {
            this.UniqueVerbose("    Area path '{0}' not found: using wildcard rule.", sourceValue);
        }

        public void NoWildcardIterationRule(WorkItemsStageConfiguration mapping, object sourceValue)
        {
            this.UniqueWarning("    No wildcard Iteration rule.");
        }

        public void IterationPathNotFoundUsingWildcardRule(WorkItemsStageConfiguration mapping, object sourceValue)
        {
            this.UniqueVerbose("    Iteration path '{0}' not found: using wildcard rule.", sourceValue);
        }

        public void NoWorkItemTypeMapping(WorkItem source)
        {
            this.Error("WorkItem type '{0}' has no mapping: correct the query or add a mapping.", source.Type.Name);
        }

        public void MappingGenericValidationError(string message, object[] args)
        {
            this.Error(message, args);
        }

        public void TranslatorFunctionNotFoundUsingDefault(FieldMap rule)
        {
            this.UniqueWarning("    Translator Function '{0}' not found, copying unaltered value.", rule.Translate);
        }

        public void AnalyzingSourceLink(WorkItemLinkInfo queryLink)
        {
            this.Info("Analyzing source WorkitemLink {0}->{1} (type {2}).", queryLink.SourceId, queryLink.TargetId, queryLink.LinkTypeId);
        }

        public void MakingNewLink(WorkItemLink relationship)
        {
            this.Info("Adding WorkitemLink from {0} to {1} ({2} End).", relationship.SourceId, relationship.TargetId, relationship.LinkTypeEnd.Name);
        }

        public void LinkExists(WorkItemLinkInfo queryLink, WorkItemLinkInfo match)
        {
            this.Info("WorkitemLink source {0}->{1} (type {2}) already maps to {3}->{4} (type {5})."
                , queryLink.SourceId, queryLink.TargetId, queryLink.LinkTypeId
                , match.SourceId, match.TargetId, match.LinkTypeId);
        }

        public void LinkExists(WorkItemLinkInfo queryLink, WorkItemLink relationship)
        {
            this.Info("WorkitemLink source {0}->{1} (type {2}) already maps to {3}->{4} (type {5})."
                , queryLink.SourceId, queryLink.TargetId, queryLink.LinkTypeId
                , relationship.SourceId, relationship.TargetId, relationship.LinkTypeEnd.LinkType.ReferenceName);
        }

        public void TargetMissingForLink(WorkItemLinkInfo queryLink, int parentId, int childId)
        {
            this.Info("Cannot add source WorkitemLink {0}->{1} (type {2}) as one of the target is not mapped."
                , queryLink.SourceId, queryLink.TargetId, queryLink.LinkTypeId);
        }

        public void InvalidRule(FieldMap rule)
        {
            this.UniqueWarning("    Invalid rule '{0}': copying unaltered value.", rule);
        }

        public void NoRuleFor(WorkItemType sourceType, string field)
        {
            this.UniqueWarning("    No rule for field '{0}\\{1}': skipping.", sourceType.Name, field);
        }

        public void SavingSkipped()
        {
            this.Warning("Saving skipped due to TestOnly option.");
        }

        public void ValidationError(Field item)
        {
            this.Warning("    Value '{1}' is not valid for field {0} (Status {2}).", item.ReferenceName, item.Value, item.Status);
        }

        public void RollbackOnValidationError(Field item)
        {
            this.Warning("    Value '{1}' is not valid for field {0} (Status {2}), rolling back to '{3}'.", item.ReferenceName, item.Value, item.Status, item.OriginalValue);
        }

        public void BypassingRulesOnDestinationWorkItemStore(TfsConnection destConn)
        {
            this.Info("Bypassing validation rules on destination project {0}.", destConn.ProjectName);
        }

        public void UsingThreePassSavingAlgorithm()
        {
            this.Info("Using create-then-update algorithm to save work items on destination.");
        }

        public void SaveFirstPassSavingNewWorkItems(List<WorkItem> newWorkItems)
        {
            this.Verbose("First pass, saving new work items forcing initial state");
        }

        public void SaveSecondPassUpdatingNewWorkItemsState(List<WorkItem> newWorkItems)
        {
            this.Verbose("Second pass, updating new work items with correct state");
        }

        public void SaveThirdPassSavingUpdatedWorkItems(List<WorkItem> updatedWorkItems)
        {
            this.Verbose("Third pass, saving updated work items");
        }

        public void SourceQueryNotFound(string queryName)
        {
            this.Error("Source query {0} not found.", queryName);
        }

        public void DestinationQueryNotFound(string queryName)
        {
            this.Error("Destination query {0} not found.", queryName);
        }

        public void AnalyzingSourceLink(WorkItemLink sourceLink)
        {
            this.Info("Analyzing source WorkitemLink {0}->{1} (type {2})."
                , sourceLink.SourceId, sourceLink.TargetId, sourceLink.LinkTypeEnd.Name);
        }

        public void LinkExists(WorkItemLink sourceLink, WorkItemLink relationship)
        {
            this.Info("WorkitemLink source {0}->{1} (type {2}) already maps to {3}->{4} (type {5})."
                , sourceLink.SourceId, sourceLink.TargetId, sourceLink.LinkTypeEnd.Name
                , relationship.SourceId, relationship.TargetId, relationship.LinkTypeEnd.Name);
        }

        public void TargetMissingForLink(WorkItemLink sourceLink, int parentId, int childId)
        {
            this.Info("Cannot add source WorkitemLink {0}->{1} (type {2}) as one of the target is not mapped."
                , sourceLink.SourceId, sourceLink.TargetId, sourceLink.LinkTypeEnd.Name);
        }

        public void SkippingLink(WorkItemLink sourceLink)
        {
            this.Info("WorkitemLink {0}->{1} (type {2}) does not match any rule."
                , sourceLink.SourceId, sourceLink.TargetId, sourceLink.LinkTypeEnd.Name);
        }

        public void MappingFileNotFoundAssumeDefaults(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                this.Info("No mapping file specified: using default mapping.");
            }
            else
            {
                this.Warning("Mapping file '{0}' not found: using default mapping.", path);
            }
        }

        public void ExceptionWhileMappingWorkItem(Exception ex, WorkItem sourceWorkItem)
        {
            this.Error("While mapping workitem #{2} '{3}': {0}\r\n{1}"
                , ex.Message, ex.StackTrace
                , sourceWorkItem.Id, sourceWorkItem.Title);
        }

        public void ExceptionWhileMappingLink(Exception ex, WorkItemLink sourceLink)
        {
            this.Error("While work item link {2}->{3} (type {4}): {0}\r\n{1}"
                , ex.Message, ex.StackTrace
                , sourceLink.SourceId, sourceLink.TargetId, sourceLink.LinkTypeEnd.Name);
        }

        public void ExceptionWhileMappingLink(Exception ex, ExternalLink link)
        {
            this.Error("While external link {2} (type {3}): {0}\r\n{1}"
                , ex.Message, ex.StackTrace
                , link.ArtifactLinkType.Name, link.LinkedArtifactUri);
        }

        public void ExceptionWhileMappingLink(Exception ex, RelatedLink link)
        {
            this.Error("While related link ->{2} (type {3}): {0}\r\n{1}"
                , ex.Message, ex.StackTrace
                , link.RelatedWorkItemId, link.LinkTypeEnd.Name);
        }

        public void ExceptionWhileMappingLink(Exception ex, Hyperlink link)
        {
            this.Error("While hyperlink {2}: {0}\r\n{1}"
                , ex.Message, ex.StackTrace
                , link.Location);
        }

        public void DumpOptions(WorkItemsStageConfiguration.Modes options)
        {
            this.Verbose("Active options: {0}", options);
        }

        public void ReadingGlobalListsFromSource()
        {
            this.Info("Reading GlobalLists from source Collection.");
        }

        public void SelectingGlobalLists()
        {
            this.Verbose("Selecting GlobalLists to sync.");
        }

        public void GlobalListQueuedForUpdate(string glName)
        {
            this.Verbose("GlobalList {0} queued for sync", glName);
        }

        public void BuildingGlobalListUpdateMessage()
        {
            this.Verbose("Building GlobalList update message.");
        }

        public void UpdatingGlobalListsOnDestination()
        {
            this.Info("Updating GlobalList(s) on destination Collection.");
        }

        public void GlobalListsUpdated()
        {
            this.Info("GlobalList(s) updated on destination Collection.");
        }

        public void ReadingAreaAndIterationInfoFromSource()
        {
            this.Info("Reading Area and Iteration configuration from source Collection.");
        }

        public void SyncingAreas()
        {
            this.Info("Adding Iteration(s) on destination Collection.");
        }

        public void SyncingIterations()
        {
            this.Info("Adding Area(s) on destination Collection.");
        }

        public void InternalError(Exception ex)
        {
            this.Error("Internal: {0}\r\n{1}", ex.Message, ex.StackTrace);
        }

        protected DateTimeOffset stageStart;

        public void ExecutingStage(PipelineStage stage)
        {
            stageStart = DateTimeOffset.UtcNow;
            this.Info("Stage {0} started.", stage.Name);
        }

        public void StageCompleted(PipelineStage stage, int stageErrors)
        {
            var elapsed = DateTimeOffset.UtcNow - stageStart;
            if (stageErrors > 0)
            {
                base.RawOut(WarningColor, TraceLevel.Normal, "Stage {0} completed in {1:d'.'hh':'mm':'ss} with {2} error(s).", stage.Name, elapsed, stageErrors);
            }
            else
            {
                base.RawOut(SuccessColor, TraceLevel.Normal, "Stage {0} successfully completed in {1:d'.'hh':'mm':'ss}.", stage.Name, elapsed);
            }
        }

        public void StageError(PipelineStage stage, Exception ex)
        {
            var elapsed = DateTimeOffset.UtcNow - stageStart;
            this.Warning("Stage {0} failed at {1:d'.'hh':'mm':'ss}, continuing with next stages; error was {1}", stage.Name, ex.Message);
            this.Verbose(ex.StackTrace);
        }

        public void PreparingStage(PipelineStage stage)
        {
            this.Verbose("Preparing stage {0}.", stage.Name);
        }

        public void StagePrepared(PipelineStage stage, int stageErrors)
        {
            if (stageErrors > 0)
            {
                this.Warning("Stage {0} failed with {1} error(s).", stage.Name, stageErrors);
            }
            else
            {
                this.Verbose("Stage {0} ready.", stage.Name);
            }
        }

        public void StagePreparationError(PipelineStage stage, Exception ex)
        {
            this.Warning("Stage {0} not ready, continuing with next stages; error was {1}", stage.Name, ex.Message);
            this.Verbose(ex.StackTrace);
        }

        public void DumpMapping(WorkItemsStageConfiguration mapping)
        {
            var output = new System.IO.StringWriter();
            output.WriteLine();
            output.WriteLine("# mapping dump start #");
            var serializer = new YamlDotNet.Serialization.Serializer(YamlDotNet.Serialization.SerializationOptions.EmitDefaults, new YamlDotNet.Serialization.NamingConventions.CamelCaseNamingConvention());
            serializer.Serialize(output, mapping);
            output.WriteLine("# mapping dump  end  #");
            output.Flush();

            this.Verbose("Dumping Mapping");
            base.RawOut(VerboseColor, TraceLevel.Verbose, output.ToString());
        }

        internal void SavingChangeLogToFile(string filename)
        {
            this.Info("Saving Changelog to '{0}'.", filename);
        }

        internal void SavedChangeLog(int numRecords)
        {
            this.Info("{0} record(s) saved.", numRecords);
        }
        /* TODO
        internal void DumpOptions(WitSyncCommandLineOptions options)
        {
            this.Verbose(options.ToString());
        }
        */
        internal void FirstMessage(string logHeader)
        {
            // empty line
            base.RawOut(InfoColor, TraceLevel.Normal, string.Empty);
            base.RawOut(InfoColor, TraceLevel.Normal, logHeader);
        }

        internal void LastMessage(int rc)
        {
            this.Info("Exiting with code {0}.", rc);
        }

        public void TraceRule(string msg, params object[] args)
        {
            base.Out(VerboseColor, TraceLevel.Diagnostic, "RULE: ", msg, args);
        }

        public void ExceptionWhileRemovingAttachment(Exception ex, Attachment a, WorkItem target)
        {
            this.Error("While removing Attachment '{2}' from #{3}: {0}\r\n{1}", ex.Message, ex.StackTrace, a.Name, target.Id);
        }

        public void ExceptionWhileAddingAttachment(Exception ex, Attachment a, WorkItem source)
        {
            this.Error("While adding Attachment '{2}' from #{3}: {0}\r\n{1}", ex.Message, ex.StackTrace, a.Name, source.Id);
        }

        public void ExceptionWhileCopyingField(Exception ex, Field fromField)
        {
            this.Error("While copying Field {1}: {0}", ex.Message, fromField.ReferenceName);
        }

        public void ExceptionWhileActingOnField(Exception ex, string targetFieldName)
        {
            this.Error("While acting on Field {1}: {0}}", ex.Message, targetFieldName);
        }
    }
}
