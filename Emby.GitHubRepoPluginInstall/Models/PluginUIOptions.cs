using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Elements;
using Emby.Web.GenericEdit.Elements.DxGrid;
using Emby.Web.GenericEdit.Elements.List;
using MediaBrowser.Model.Attributes;

namespace Emby.GitHubRepoPluginInstall.Models;

public class PluginUIOptions : EditableOptionsBase
{
    [DontSave]
    public override string EditorTitle => "Download Plugsin From GitHub";

    [DontSave]
    public override string EditorDescription =>
        "This plugin allows you to download and install plugins from GitHub repositories.";

    [DontSave]
    public SpacerItem Spacer1 { get; set; } = new SpacerItem();

    [DontSave]
    public CaptionItem CaptionBasic { get; set; } = new CaptionItem("Basic Settings");

    [DisplayName("GitHub Token (PAT)")]
    [Description("Generate a GitHub Personal Access Token (PAT) and enter it here.")]
    [Required]
    public string GitHubToken { get; set; }

    public bool RestartServerAfterInstall { get; set; }

    [DontSave]
    [VisibleCondition(nameof(GitHubToken), SimpleCondition.IsNotNullOrEmpty)]
    public ButtonItem SaveTokenBtn =>
        new ButtonItem
        {
            Icon    = IconNames.save_alt,
            Caption = "Save Settings",
            Data1   = "Save"
        };

    [DontSave]
    public SpacerItem Spacer1a { get; set; } = new SpacerItem();

    [DontSave]
    public ButtonItem Add =>
        new ButtonItem
        {
            Icon    = IconNames.add,
            Caption = "Add Repository",
            Data1   = "Add"
        };

    [Browsable(false)]
    [DontSave]
    public IList<string> SelectedItemId { get; set; }

    [DontSave]
    public SpacerItem Spacer2 { get; set; } = new SpacerItem();

    [Browsable(false)]
    public List<ReposToProcess> Repos { get; set; } = new List<ReposToProcess>();

    [DisplayName("Current Repositories")]
    [DontSave]
    [GridDataSource(nameof(Repos))]
    [GridSelectionSource(nameof(SelectedItemId))]
    public DxDataGrid Grid
    {
        get
        {
            var options = new DxGridOptions(new ReposToProcess(), "Id", false, true, true, false);

            options.selection.mode         = DxGridSelection.SelectionMode.single;
            options.columnResizingMode     = DxGridOptions.ColumnResizingMode.nextColumn;
            options.heightMode             = DxGridOptions.GridHeightMode.medium;
            options.allowColumnReordering  = true;
            options.grouping.autoExpandAll = true;
            options.focusedRowEnabled      = true;

            foreach (var column in options.columns) column.visible = false;

            var repo = options.columns.FirstOrDefault(e => e.dataField == nameof(ReposToProcess.Repository));
            if (repo != null)
            {
                repo.caption      = "Repository";
                repo.width        = 200;
                repo.visibleIndex = 7;
                repo.visible      = true;
            }

            var owner = options.columns.FirstOrDefault(e => e.dataField == nameof(ReposToProcess.Owner));
            if (owner != null)
            {
                owner.caption      = "Owner";
                owner.width        = 150;
                owner.visibleIndex = 7;
                owner.groupIndex   = 0;
                owner.visible      = true;
            }

            var lastVersionDownloaded =
                options.columns.FirstOrDefault(e => e.dataField == nameof(ReposToProcess.LastVersionDownloaded));
            if (lastVersionDownloaded != null)
            {
                lastVersionDownloaded.caption      = "Last Version Downloaded";
                lastVersionDownloaded.width        = 150;
                lastVersionDownloaded.visibleIndex = 9;
                lastVersionDownloaded.visible      = true;
            }

            var preRelCol = options.columns.FirstOrDefault(e => e.dataField == nameof(ReposToProcess.GetPreRelease));
            if (preRelCol != null)
            {
                preRelCol.caption      = "Get PreRelease";
                preRelCol.width        = 100;
                preRelCol.visibleIndex = 20;
                preRelCol.visible      = true;
            }

            var autoUpdateCol = options.columns.FirstOrDefault(e => e.dataField == nameof(ReposToProcess.AutoUpdate));
            if (autoUpdateCol != null)
            {
                autoUpdateCol.caption      = "Auto Update";
                autoUpdateCol.width        = 100;
                autoUpdateCol.visibleIndex = 30;
                autoUpdateCol.visible      = true;
            }

            var lastDateTimeChecked =
                options.columns.FirstOrDefault(e => e.dataField == nameof(ReposToProcess.LastDateTimeChecked));
            if (lastDateTimeChecked != null)
            {
                lastDateTimeChecked.caption      = "Last Checked";
                lastDateTimeChecked.width        = 200;
                lastDateTimeChecked.dataType     = DxGridColumn.ColumnDataType.datetime;
                lastDateTimeChecked.visibleIndex = 40;
                lastDateTimeChecked.visible      = true;
            }

            var urlCol = options.columns.FirstOrDefault(e => e.dataField == nameof(ReposToProcess.Url));
            if (urlCol != null)
            {
                urlCol.caption      = "Url";
                urlCol.width        = 400;
                urlCol.visibleIndex = 50;
                urlCol.visible      = true;
            }

            return new DxDataGrid(options);
        }
    }

    [DontSave]
    public ButtonItem Delete =>
        new ButtonItem
        {
            Icon               = IconNames.delete_forever,
            Caption            = "Remove Selected",
            Data1              = "Remove",
            ConfirmationPrompt = "Are you sure you want to remove the selected item?"
        };

    [DontSave]
    public ButtonItem Edit =>
        new ButtonItem
        {
            Icon    = IconNames.edit,
            Caption = "Edit Selected",
            Data1   = "Edit"
        };

    [DontSave]
    public SpacerItem Spacer3 { get; set; } = new SpacerItem();

    [DontSave]
    public CaptionItem CaptionLatestReleases { get; set; } = new CaptionItem("Latest Releases");

    [DisplayName("Releases")]
    [DontSave]
    public GenericItemList Releases { get; set; }
}