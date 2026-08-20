using TennisPracticePlanner.Models;

namespace TennisPracticePlanner.Services;

/// <summary>
/// Routes data operations to local storage (Guest Mode) or Firestore (signed-in + allow-listed),
/// switching automatically based on current auth state.
/// </summary>
public class CompositeTennisPracticeDataService : ITennisPracticeDataService
{
    private readonly TennisPracticeDataService _guestService;
    private readonly CloudTennisPracticeDataService _cloudService;
    private readonly IAuthService _authService;

    public CompositeTennisPracticeDataService(
        TennisPracticeDataService guestService,
        CloudTennisPracticeDataService cloudService,
        IAuthService authService)
    {
        _guestService = guestService;
        _cloudService = cloudService;
        _authService = authService;
    }

    public bool IsCloudActive => _authService.IsSignedIn && _authService.IsAllowed == true;

    public CloudTennisPracticeDataService CloudService => _cloudService;

    private ITennisPracticeDataService Active => IsCloudActive ? _cloudService : _guestService;

    public Task InitializeAsync() => Active.InitializeAsync();

    public Task<List<ReusableInstruction>> GetReusableInstructionsAsync() => Active.GetReusableInstructionsAsync();

    public Task<ReusableInstruction?> GetReusableInstructionByIdAsync(string instructionId) => Active.GetReusableInstructionByIdAsync(instructionId);

    public Task AddReusableInstructionAsync(ReusableInstruction instruction) => Active.AddReusableInstructionAsync(instruction);

    public Task UpdateReusableInstructionAsync(ReusableInstruction instruction) => Active.UpdateReusableInstructionAsync(instruction);

    public Task DeleteReusableInstructionAsync(string instructionId) => Active.DeleteReusableInstructionAsync(instructionId);

    public Task<List<PracticeTemplate>> GetPracticeTemplatesAsync() => Active.GetPracticeTemplatesAsync();

    public Task<PracticeTemplate?> GetPracticeTemplateByIdAsync(string templateId) => Active.GetPracticeTemplateByIdAsync(templateId);

    public Task<PracticeTemplate> CreatePracticeTemplateAsync(string title) => Active.CreatePracticeTemplateAsync(title);

    public Task UpdatePracticeTemplateTitleAsync(string templateId, string title) => Active.UpdatePracticeTemplateTitleAsync(templateId, title);

    public Task DeletePracticeTemplateAsync(string templateId) => Active.DeletePracticeTemplateAsync(templateId);

    public Task AddInstructionCopyFromLibraryAsync(string templateId, string reusableInstructionId) => Active.AddInstructionCopyFromLibraryAsync(templateId, reusableInstructionId);

    public Task AddOneOffInstructionAsync(string templateId, TemplateInstructionItem instructionItem) => Active.AddOneOffInstructionAsync(templateId, instructionItem);

    public Task UpdateTemplateInstructionAsync(string templateId, TemplateInstructionItem instructionItem) => Active.UpdateTemplateInstructionAsync(templateId, instructionItem);

    public Task DeleteTemplateInstructionAsync(string templateId, string templateInstructionId) => Active.DeleteTemplateInstructionAsync(templateId, templateInstructionId);

    public Task MoveTemplateInstructionAsync(string templateId, string templateInstructionId, int direction) => Active.MoveTemplateInstructionAsync(templateId, templateInstructionId, direction);
}
