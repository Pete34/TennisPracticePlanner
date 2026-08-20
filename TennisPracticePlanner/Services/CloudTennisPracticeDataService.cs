using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;
using TennisPracticePlanner.Models;

namespace TennisPracticePlanner.Services;

/// <summary>
/// Firestore-backed data service used only when a user is signed in and allow-listed.
/// Each instruction/template is stored as its own document under users/{uid}/... .
/// </summary>
public class CloudTennisPracticeDataService : ITennisPracticeDataService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly IAuthService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    private IJSObjectReference? _module;
    private List<ReusableInstruction> _instructions = [];
    private List<PracticeTemplate> _templates = [];
    private bool _isInitialized;
    private string? _loadedForUid;

    public CloudTennisPracticeDataService(IJSRuntime jsRuntime, IAuthService authService)
    {
        _jsRuntime = jsRuntime;
        _authService = authService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    public async Task InitializeAsync()
    {
        string uid = RequireUid();

        if (_isInitialized && _loadedForUid == uid)
        {
            return;
        }

        _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/firebase-interop.js");

        List<JsonElement> rawInstructions = await _module.InvokeAsync<List<JsonElement>>("getUserInstructions", uid);
        List<JsonElement> rawTemplates = await _module.InvokeAsync<List<JsonElement>>("getUserTemplates", uid);

        _instructions = rawInstructions
            .Select(item => item.Deserialize<ReusableInstruction>(_jsonOptions)!)
            .ToList();

        _templates = rawTemplates
            .Select(item => item.Deserialize<PracticeTemplate>(_jsonOptions)!)
            .ToList();

        foreach (PracticeTemplate template in _templates)
        {
            NormalizeSortOrder(template);
        }

        _loadedForUid = uid;
        _isInitialized = true;
    }

    public async Task<List<ReusableInstruction>> GetReusableInstructionsAsync()
    {
        await InitializeAsync();
        return _instructions.OrderBy(instruction => instruction.Description).ToList();
    }

    public async Task<ReusableInstruction?> GetReusableInstructionByIdAsync(string instructionId)
    {
        await InitializeAsync();
        return _instructions.FirstOrDefault(instruction => instruction.Id == instructionId);
    }

    public async Task AddReusableInstructionAsync(ReusableInstruction instruction)
    {
        await InitializeAsync();

        instruction.Id = Guid.NewGuid().ToString("N");
        instruction.CreatedUtc = DateTime.UtcNow;
        instruction.UpdatedUtc = DateTime.UtcNow;
        ApplySharingMetadata(instruction);

        _instructions.Add(instruction);
        await SaveInstructionAsync(instruction);
    }

    public async Task UpdateReusableInstructionAsync(ReusableInstruction instruction)
    {
        await InitializeAsync();

        ReusableInstruction? existingInstruction = _instructions.FirstOrDefault(item => item.Id == instruction.Id);

        if (existingInstruction is null)
        {
            return;
        }

        existingInstruction.Description = instruction.Description;
        existingInstruction.DurationMinutes = instruction.DurationMinutes;
        existingInstruction.BallCount = instruction.BallCount;
        existingInstruction.YoutubeUrl = instruction.YoutubeUrl;
        existingInstruction.YoutubeStartTime = instruction.YoutubeStartTime;
        existingInstruction.IsShared = instruction.IsShared;
        existingInstruction.Tag = instruction.Tag;
        existingInstruction.UpdatedUtc = DateTime.UtcNow;
        ApplySharingMetadata(existingInstruction);

        await SaveInstructionAsync(existingInstruction);
    }

    private void ApplySharingMetadata(ReusableInstruction instruction)
    {
        if (instruction.IsShared)
        {
            instruction.OwnerUid = _authService.CurrentUser?.Uid;
            instruction.OwnerDisplayName = _authService.CurrentUser?.DisplayName ?? _authService.CurrentUser?.Email;
            instruction.SharedAtUtc ??= DateTime.UtcNow;
        }
        else
        {
            instruction.OwnerUid = null;
            instruction.OwnerDisplayName = null;
            instruction.SharedAtUtc = null;
        }
    }

    public async Task DeleteReusableInstructionAsync(string instructionId)
    {
        await InitializeAsync();

        ReusableInstruction? instruction = _instructions.FirstOrDefault(item => item.Id == instructionId);

        if (instruction is null)
        {
            return;
        }

        _instructions.Remove(instruction);
        await _module!.InvokeVoidAsync("deleteUserInstruction", RequireUid(), instructionId);
    }

    public async Task<List<PracticeTemplate>> GetPracticeTemplatesAsync()
    {
        await InitializeAsync();

        foreach (PracticeTemplate template in _templates)
        {
            NormalizeSortOrder(template);
        }

        return _templates.OrderBy(template => template.Title).ToList();
    }

    public async Task<PracticeTemplate?> GetPracticeTemplateByIdAsync(string templateId)
    {
        await InitializeAsync();

        PracticeTemplate? template = _templates.FirstOrDefault(item => item.Id == templateId);

        if (template is not null)
        {
            NormalizeSortOrder(template);
        }

        return template;
    }

    public async Task<PracticeTemplate> CreatePracticeTemplateAsync(string title)
    {
        await InitializeAsync();

        PracticeTemplate newTemplate = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Title = title.Trim(),
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        _templates.Add(newTemplate);
        await SaveTemplateAsync(newTemplate);

        return newTemplate;
    }

    public async Task UpdatePracticeTemplateTitleAsync(string templateId, string title)
    {
        await InitializeAsync();

        PracticeTemplate? template = _templates.FirstOrDefault(item => item.Id == templateId);

        if (template is null)
        {
            return;
        }

        template.Title = title.Trim();
        template.UpdatedUtc = DateTime.UtcNow;
        await SaveTemplateAsync(template);
    }

    public async Task DeletePracticeTemplateAsync(string templateId)
    {
        await InitializeAsync();

        PracticeTemplate? template = _templates.FirstOrDefault(item => item.Id == templateId);

        if (template is null)
        {
            return;
        }

        _templates.Remove(template);
        await _module!.InvokeVoidAsync("deleteUserTemplate", RequireUid(), templateId);
    }

    public async Task AddInstructionCopyFromLibraryAsync(string templateId, string reusableInstructionId)
    {
        await InitializeAsync();

        PracticeTemplate? template = _templates.FirstOrDefault(item => item.Id == templateId);
        ReusableInstruction? instruction = _instructions.FirstOrDefault(item => item.Id == reusableInstructionId);

        if (template is null || instruction is null)
        {
            return;
        }

        TemplateInstructionItem copiedItem = TemplateInstructionItem.FromReusableInstruction(instruction, template.InstructionItems.Count);
        template.InstructionItems.Add(copiedItem);
        template.UpdatedUtc = DateTime.UtcNow;

        await SaveTemplateAsync(template);
    }

    public async Task AddOneOffInstructionAsync(string templateId, TemplateInstructionItem instructionItem)
    {
        await InitializeAsync();

        PracticeTemplate? template = _templates.FirstOrDefault(item => item.Id == templateId);

        if (template is null)
        {
            return;
        }

        instructionItem.Id = Guid.NewGuid().ToString("N");
        instructionItem.SourceInstructionId = null;
        instructionItem.SortOrder = template.InstructionItems.Count;

        template.InstructionItems.Add(instructionItem);
        template.UpdatedUtc = DateTime.UtcNow;

        await SaveTemplateAsync(template);
    }

    public async Task UpdateTemplateInstructionAsync(string templateId, TemplateInstructionItem instructionItem)
    {
        await InitializeAsync();

        PracticeTemplate? template = _templates.FirstOrDefault(item => item.Id == templateId);

        if (template is null)
        {
            return;
        }

        TemplateInstructionItem? existingItem = template.InstructionItems.FirstOrDefault(item => item.Id == instructionItem.Id);

        if (existingItem is null)
        {
            return;
        }

        existingItem.Description = instructionItem.Description;
        existingItem.DurationMinutes = instructionItem.DurationMinutes;
        existingItem.BallCount = instructionItem.BallCount;
        existingItem.YoutubeUrl = instructionItem.YoutubeUrl;
        existingItem.YoutubeStartTime = instructionItem.YoutubeStartTime;

        template.UpdatedUtc = DateTime.UtcNow;
        await SaveTemplateAsync(template);
    }

    public async Task DeleteTemplateInstructionAsync(string templateId, string templateInstructionId)
    {
        await InitializeAsync();

        PracticeTemplate? template = _templates.FirstOrDefault(item => item.Id == templateId);

        if (template is null)
        {
            return;
        }

        TemplateInstructionItem? existingItem = template.InstructionItems.FirstOrDefault(item => item.Id == templateInstructionId);

        if (existingItem is null)
        {
            return;
        }

        template.InstructionItems.Remove(existingItem);
        NormalizeSortOrder(template);
        template.UpdatedUtc = DateTime.UtcNow;

        await SaveTemplateAsync(template);
    }

    public async Task MoveTemplateInstructionAsync(string templateId, string templateInstructionId, int direction)
    {
        await InitializeAsync();

        PracticeTemplate? template = _templates.FirstOrDefault(item => item.Id == templateId);

        if (template is null)
        {
            return;
        }

        List<TemplateInstructionItem> orderedItems = template.InstructionItems.OrderBy(item => item.SortOrder).ToList();
        int currentIndex = orderedItems.FindIndex(item => item.Id == templateInstructionId);

        if (currentIndex < 0)
        {
            return;
        }

        int newIndex = currentIndex + direction;

        if (newIndex < 0 || newIndex >= orderedItems.Count)
        {
            return;
        }

        (orderedItems[currentIndex], orderedItems[newIndex]) = (orderedItems[newIndex], orderedItems[currentIndex]);

        for (int index = 0; index < orderedItems.Count; index++)
        {
            orderedItems[index].SortOrder = index;
        }

        template.InstructionItems = orderedItems;
        template.UpdatedUtc = DateTime.UtcNow;

        await SaveTemplateAsync(template);
    }

    /// <summary>Fetches instructions any allow-listed user has opted to share, optionally filtered by tag.</summary>
    public async Task<List<ReusableInstruction>> GetSharedInstructionsAsync(InstructionTag? tag)
    {
        _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/firebase-interop.js");

        string? tagArg = tag?.ToString();
        List<JsonElement> rawInstructions = await _module.InvokeAsync<List<JsonElement>>("getSharedInstructions", tagArg);

        return rawInstructions
            .Select(item => item.Deserialize<ReusableInstruction>(_jsonOptions)!)
            .OrderByDescending(instruction => instruction.SharedAtUtc)
            .ToList();
    }

    /// <summary>Clones a shared instruction into the current user's own private library.</summary>
    public async Task CopySharedInstructionToLibraryAsync(ReusableInstruction sharedInstruction)
    {
        await InitializeAsync();

        ReusableInstruction copy = new()
        {
            Id = Guid.NewGuid().ToString("N"),
            Description = sharedInstruction.Description,
            DurationMinutes = sharedInstruction.DurationMinutes,
            BallCount = sharedInstruction.BallCount,
            YoutubeUrl = sharedInstruction.YoutubeUrl,
            YoutubeStartTime = sharedInstruction.YoutubeStartTime,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };

        _instructions.Add(copy);
        await SaveInstructionAsync(copy);
    }

    private async Task SaveInstructionAsync(ReusableInstruction instruction)
    {
        _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/firebase-interop.js");
        string json = JsonSerializer.Serialize(instruction, _jsonOptions);
        await _module.InvokeVoidAsync("saveUserInstruction", RequireUid(), json);
    }

    private async Task SaveTemplateAsync(PracticeTemplate template)
    {
        _module ??= await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/firebase-interop.js");
        string json = JsonSerializer.Serialize(template, _jsonOptions);
        await _module.InvokeVoidAsync("saveUserTemplate", RequireUid(), json);
    }

    private string RequireUid()
    {
        return _authService.CurrentUser?.Uid
            ?? throw new InvalidOperationException("Cloud data service requires a signed-in user.");
    }

    private static void NormalizeSortOrder(PracticeTemplate template)
    {
        List<TemplateInstructionItem> orderedItems = template.InstructionItems
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Description)
            .ToList();

        for (int index = 0; index < orderedItems.Count; index++)
        {
            orderedItems[index].SortOrder = index;
        }

        template.InstructionItems = orderedItems;
    }
}
