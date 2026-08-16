using System.Text.Json;
using Microsoft.JSInterop;
using TennisPracticePlanner.Models;

namespace TennisPracticePlanner.Services;

public class TennisPracticeDataService : ITennisPracticeDataService
{
    private const string StorageKey = "tennis-practice-planner-data-v1";

    private readonly IJSRuntime _jsRuntime;
    private readonly JsonSerializerOptions _jsonOptions;

    private AppDataStore _appDataStore = new();
    private bool _isInitialized;

    public TennisPracticeDataService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        string? rawJson = await _jsRuntime.InvokeAsync<string?>("tennisPracticeStorage.get", StorageKey);

        if (string.IsNullOrWhiteSpace(rawJson))
        {
            _appDataStore = CreateStarterData();
            await SaveAsync();
            _isInitialized = true;
            return;
        }

        try
        {
            _appDataStore = JsonSerializer.Deserialize<AppDataStore>(rawJson, _jsonOptions) ?? new AppDataStore();
            EnsureDataIntegrity(_appDataStore);
            await SaveAsync();
        }
        catch
        {
            // If local storage data becomes invalid JSON, the app should recover gracefully.
            _appDataStore = CreateStarterData();
            await SaveAsync();
        }

        _isInitialized = true;
    }

    public async Task<List<ReusableInstruction>> GetReusableInstructionsAsync()
    {
        await InitializeAsync();
        return _appDataStore.ReusableInstructions
            .OrderBy(instruction => instruction.Description)
            .ToList();
    }

    public async Task<ReusableInstruction?> GetReusableInstructionByIdAsync(string instructionId)
    {
        await InitializeAsync();
        return _appDataStore.ReusableInstructions.FirstOrDefault(instruction => instruction.Id == instructionId);
    }

    public async Task AddReusableInstructionAsync(ReusableInstruction instruction)
    {
        await InitializeAsync();

        instruction.Id = Guid.NewGuid().ToString("N");
        instruction.CreatedUtc = DateTime.UtcNow;
        instruction.UpdatedUtc = DateTime.UtcNow;

        _appDataStore.ReusableInstructions.Add(instruction);
        await SaveAsync();
    }

    public async Task UpdateReusableInstructionAsync(ReusableInstruction instruction)
    {
        await InitializeAsync();

        ReusableInstruction? existingInstruction = _appDataStore.ReusableInstructions.FirstOrDefault(item => item.Id == instruction.Id);

        if (existingInstruction is null)
        {
            return;
        }

        existingInstruction.Description = instruction.Description;
        existingInstruction.DurationMinutes = instruction.DurationMinutes;
        existingInstruction.BallCount = instruction.BallCount;
        existingInstruction.YoutubeUrl = instruction.YoutubeUrl;
        existingInstruction.YoutubeStartTime = instruction.YoutubeStartTime;
        existingInstruction.UpdatedUtc = DateTime.UtcNow;

        await SaveAsync();
    }

    public async Task DeleteReusableInstructionAsync(string instructionId)
    {
        await InitializeAsync();

        ReusableInstruction? instruction = _appDataStore.ReusableInstructions.FirstOrDefault(item => item.Id == instructionId);

        if (instruction is null)
        {
            return;
        }

        _appDataStore.ReusableInstructions.Remove(instruction);
        await SaveAsync();
    }

    public async Task<List<PracticeTemplate>> GetPracticeTemplatesAsync()
    {
        await InitializeAsync();

        foreach (PracticeTemplate practiceTemplate in _appDataStore.PracticeTemplates)
        {
            NormalizeSortOrder(practiceTemplate);
        }

        return _appDataStore.PracticeTemplates
            .OrderBy(practiceTemplate => practiceTemplate.Title)
            .ToList();
    }

    public async Task<PracticeTemplate?> GetPracticeTemplateByIdAsync(string templateId)
    {
        await InitializeAsync();

        PracticeTemplate? practiceTemplate = _appDataStore.PracticeTemplates.FirstOrDefault(item => item.Id == templateId);

        if (practiceTemplate is not null)
        {
            NormalizeSortOrder(practiceTemplate);
        }

        return practiceTemplate;
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

        _appDataStore.PracticeTemplates.Add(newTemplate);
        await SaveAsync();

        return newTemplate;
    }

    public async Task UpdatePracticeTemplateTitleAsync(string templateId, string title)
    {
        await InitializeAsync();

        PracticeTemplate? practiceTemplate = _appDataStore.PracticeTemplates.FirstOrDefault(item => item.Id == templateId);

        if (practiceTemplate is null)
        {
            return;
        }

        practiceTemplate.Title = title.Trim();
        practiceTemplate.UpdatedUtc = DateTime.UtcNow;
        await SaveAsync();
    }

    public async Task DeletePracticeTemplateAsync(string templateId)
    {
        await InitializeAsync();

        PracticeTemplate? practiceTemplate = _appDataStore.PracticeTemplates.FirstOrDefault(item => item.Id == templateId);

        if (practiceTemplate is null)
        {
            return;
        }

        _appDataStore.PracticeTemplates.Remove(practiceTemplate);
        await SaveAsync();
    }

    public async Task AddInstructionCopyFromLibraryAsync(string templateId, string reusableInstructionId)
    {
        await InitializeAsync();

        PracticeTemplate? practiceTemplate = _appDataStore.PracticeTemplates.FirstOrDefault(item => item.Id == templateId);
        ReusableInstruction? reusableInstruction = _appDataStore.ReusableInstructions.FirstOrDefault(item => item.Id == reusableInstructionId);

        if (practiceTemplate is null || reusableInstruction is null)
        {
            return;
        }

        TemplateInstructionItem copiedItem = TemplateInstructionItem.FromReusableInstruction(
            reusableInstruction,
            practiceTemplate.InstructionItems.Count);

        practiceTemplate.InstructionItems.Add(copiedItem);
        practiceTemplate.UpdatedUtc = DateTime.UtcNow;

        await SaveAsync();
    }

    public async Task AddOneOffInstructionAsync(string templateId, TemplateInstructionItem instructionItem)
    {
        await InitializeAsync();

        PracticeTemplate? practiceTemplate = _appDataStore.PracticeTemplates.FirstOrDefault(item => item.Id == templateId);

        if (practiceTemplate is null)
        {
            return;
        }

        instructionItem.Id = Guid.NewGuid().ToString("N");
        instructionItem.SourceInstructionId = null;
        instructionItem.SortOrder = practiceTemplate.InstructionItems.Count;

        practiceTemplate.InstructionItems.Add(instructionItem);
        practiceTemplate.UpdatedUtc = DateTime.UtcNow;

        await SaveAsync();
    }

    public async Task UpdateTemplateInstructionAsync(string templateId, TemplateInstructionItem instructionItem)
    {
        await InitializeAsync();

        PracticeTemplate? practiceTemplate = _appDataStore.PracticeTemplates.FirstOrDefault(item => item.Id == templateId);

        if (practiceTemplate is null)
        {
            return;
        }

        TemplateInstructionItem? existingItem = practiceTemplate.InstructionItems.FirstOrDefault(item => item.Id == instructionItem.Id);

        if (existingItem is null)
        {
            return;
        }

        existingItem.Description = instructionItem.Description;
        existingItem.DurationMinutes = instructionItem.DurationMinutes;
        existingItem.BallCount = instructionItem.BallCount;
        existingItem.YoutubeUrl = instructionItem.YoutubeUrl;
        existingItem.YoutubeStartTime = instructionItem.YoutubeStartTime;

        practiceTemplate.UpdatedUtc = DateTime.UtcNow;
        await SaveAsync();
    }

    public async Task DeleteTemplateInstructionAsync(string templateId, string templateInstructionId)
    {
        await InitializeAsync();

        PracticeTemplate? practiceTemplate = _appDataStore.PracticeTemplates.FirstOrDefault(item => item.Id == templateId);

        if (practiceTemplate is null)
        {
            return;
        }

        TemplateInstructionItem? existingItem = practiceTemplate.InstructionItems.FirstOrDefault(item => item.Id == templateInstructionId);

        if (existingItem is null)
        {
            return;
        }

        practiceTemplate.InstructionItems.Remove(existingItem);
        NormalizeSortOrder(practiceTemplate);
        practiceTemplate.UpdatedUtc = DateTime.UtcNow;

        await SaveAsync();
    }

    public async Task MoveTemplateInstructionAsync(string templateId, string templateInstructionId, int direction)
    {
        await InitializeAsync();

        PracticeTemplate? practiceTemplate = _appDataStore.PracticeTemplates.FirstOrDefault(item => item.Id == templateId);

        if (practiceTemplate is null)
        {
            return;
        }

        List<TemplateInstructionItem> orderedItems = practiceTemplate.InstructionItems
            .OrderBy(item => item.SortOrder)
            .ToList();

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

        practiceTemplate.InstructionItems = orderedItems;
        practiceTemplate.UpdatedUtc = DateTime.UtcNow;

        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        string serializedData = JsonSerializer.Serialize(_appDataStore, _jsonOptions);
        await _jsRuntime.InvokeVoidAsync("tennisPracticeStorage.set", StorageKey, serializedData);
    }

    private static void EnsureDataIntegrity(AppDataStore dataStore)
    {
        dataStore.ReusableInstructions ??= [];
        dataStore.PracticeTemplates ??= [];

        foreach (ReusableInstruction instruction in dataStore.ReusableInstructions)
        {
            instruction.Id = string.IsNullOrWhiteSpace(instruction.Id) ? Guid.NewGuid().ToString("N") : instruction.Id;
            instruction.Description = instruction.Description?.Trim() ?? string.Empty;
            instruction.CreatedUtc = instruction.CreatedUtc == default ? DateTime.UtcNow : instruction.CreatedUtc;
            instruction.UpdatedUtc = instruction.UpdatedUtc == default ? DateTime.UtcNow : instruction.UpdatedUtc;
        }

        foreach (PracticeTemplate practiceTemplate in dataStore.PracticeTemplates)
        {
            practiceTemplate.Id = string.IsNullOrWhiteSpace(practiceTemplate.Id) ? Guid.NewGuid().ToString("N") : practiceTemplate.Id;
            practiceTemplate.Title = practiceTemplate.Title?.Trim() ?? string.Empty;
            practiceTemplate.CreatedUtc = practiceTemplate.CreatedUtc == default ? DateTime.UtcNow : practiceTemplate.CreatedUtc;
            practiceTemplate.UpdatedUtc = practiceTemplate.UpdatedUtc == default ? DateTime.UtcNow : practiceTemplate.UpdatedUtc;
            practiceTemplate.InstructionItems ??= [];

            NormalizeSortOrder(practiceTemplate);
        }
    }

    private static void NormalizeSortOrder(PracticeTemplate practiceTemplate)
    {
        List<TemplateInstructionItem> orderedItems = practiceTemplate.InstructionItems
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Description)
            .ToList();

        for (int index = 0; index < orderedItems.Count; index++)
        {
            TemplateInstructionItem item = orderedItems[index];
            item.Id = string.IsNullOrWhiteSpace(item.Id) ? Guid.NewGuid().ToString("N") : item.Id;
            item.Description = item.Description?.Trim() ?? string.Empty;
            item.SortOrder = index;
        }

        practiceTemplate.InstructionItems = orderedItems;
    }

    private static AppDataStore CreateStarterData()
    {
        ReusableInstruction crossCourtDrill = new()
        {
            Description = "Cross-court rally with backhands",
            DurationMinutes = 8,
            BallCount = 60,
            YoutubeUrl = "https://www.youtube.com/watch?v=ylNfmh8h7pM",
            YoutubeStartTime = "00:15"
        };

        ReusableInstruction serveDrill = new()
        {
            Description = "Serve placement: deuce wide and body",
            DurationMinutes = 10,
            BallCount = 40,
            YoutubeUrl = null,
            YoutubeStartTime = null
        };

        PracticeTemplate starterTemplate = new()
        {
            Title = "Quick 20-Minute Baseline Session",
            InstructionItems =
            [
                TemplateInstructionItem.FromReusableInstruction(crossCourtDrill, 0),
                TemplateInstructionItem.FromReusableInstruction(serveDrill, 1),
                new TemplateInstructionItem
                {
                    Description = "Cooldown mini-tennis",
                    DurationMinutes = 2,
                    BallCount = null,
                    YoutubeUrl = null,
                    YoutubeStartTime = null,
                    SortOrder = 2
                }
            ]
        };

        return new AppDataStore
        {
            ReusableInstructions = [crossCourtDrill, serveDrill],
            PracticeTemplates = [starterTemplate]
        };
    }
}
