﻿using Microsoft.AspNetCore.Components;

using System.Diagnostics;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;

using WireGuardCommand.Extensions;
using WireGuardCommand.IO;
using WireGuardCommand.Services;
using WireGuardCommand.Services.Models;

namespace WireGuardCommand.Pages.Project;

public partial class ProjectView
{
    [Inject]
    public ProjectManager ProjectManager { get; set; } = default!;

    [Inject]
    public ProjectCache Cache { get; set; } = default!;

    [Inject]
    public NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    public ILogger<ProjectView> Logger { get; set; } = default!;

    public string? Status { get; set; }
    public string? Error { get; set; }

    public bool HasUnsavedChanges
    {
        get => HasChanges();
    }

    private ProjectData? originalData;

    protected override void OnInitialized()
    {
        if(Cache.CurrentProject.ProjectData is null)
        {
            return;
        }

        originalData = Cache.CurrentProject.ProjectData.Copy();
        Logger.LogInformation("Loaded project.");
    }

    private void GoBack()
    {
        NavigationManager.NavigateTo("/");
        Cache.Clear();
    }

    private void GenerateSeed()
    {
        var project = Cache.CurrentProject;
        if(project.ProjectData is null)
        {
            Error = "Failed to generate seed.";
            return;
        }

        project.ProjectData.Seed = RandomNumberGenerator.GetBytes(32).ToBase64();
    }

    private bool HasChanges()
    {
        if(originalData is null || 
            Cache.CurrentProject.ProjectData is null)
        {
            Error = "Unable to determine changes.";
            return false;
        }

        return JsonSerializer.Serialize(originalData) != JsonSerializer.Serialize(Cache.CurrentProject.ProjectData);
    }

    public async Task SaveChangesAsync()
    {
        if(Cache.CurrentProject.ProjectData is null)
        {
            return;
        }

        var project = Cache.CurrentProject.ProjectData;

        try
        {
            await ProjectManager.SaveProjectAsync(Cache.CurrentProject);
            Status = "Saved changes.";
        }
        catch (Exception ex)
        {
            Error = $"Failed to save project: {ex.Message}";
            StateHasChanged();
            return;
        }

        originalData = project.Copy();
        StateHasChanged();
    }

    public async Task GenerateConfigsAsync()
    {
        if (Cache.CurrentProject.ProjectData is null ||
            Cache.CurrentProject.Metadata is null)
        {
            return;
        }

        var project = Cache.CurrentProject.ProjectData;
        var metadata = Cache.CurrentProject.Metadata;

        if(string.IsNullOrWhiteSpace(metadata.Path))
        {
            Error = "Failed to generate: Failed to find path to project.";
            return;
        }

        try
        {
            var outputPath = Path.Combine(metadata.Path, "Output");
            if(!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            var writer = new ProjectWriter(project);
            await writer.WriteConfigsAsync(outputPath);

            Status = "Generated configuration.";
        }
        catch(Exception ex)
        {
            Error = $"Failed to generate configs: {ex.Message}";
        }
    }

    [SupportedOSPlatform("Windows")]
    private void BrowseProject()
    {
        var metadata = Cache.CurrentProject.Metadata;

        if (metadata is null ||
            string.IsNullOrWhiteSpace(metadata.Path))
        {
            return;
        }

        Process.Start("explorer.exe", metadata.Path);
    }
}
