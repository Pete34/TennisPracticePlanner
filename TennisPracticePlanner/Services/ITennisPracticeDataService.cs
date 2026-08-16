using TennisPracticePlanner.Models;

namespace TennisPracticePlanner.Services;

public interface ITennisPracticeDataService
{
    Task InitializeAsync();

    Task<List<ReusableInstruction>> GetReusableInstructionsAsync();

    Task<ReusableInstruction?> GetReusableInstructionByIdAsync(string instructionId);

    Task AddReusableInstructionAsync(ReusableInstruction instruction);

    Task UpdateReusableInstructionAsync(ReusableInstruction instruction);

    Task DeleteReusableInstructionAsync(string instructionId);

    Task<List<PracticeTemplate>> GetPracticeTemplatesAsync();

    Task<PracticeTemplate?> GetPracticeTemplateByIdAsync(string templateId);

    Task<PracticeTemplate> CreatePracticeTemplateAsync(string title);

    Task UpdatePracticeTemplateTitleAsync(string templateId, string title);

    Task DeletePracticeTemplateAsync(string templateId);

    Task AddInstructionCopyFromLibraryAsync(string templateId, string reusableInstructionId);

    Task AddOneOffInstructionAsync(string templateId, TemplateInstructionItem instructionItem);

    Task UpdateTemplateInstructionAsync(string templateId, TemplateInstructionItem instructionItem);

    Task DeleteTemplateInstructionAsync(string templateId, string templateInstructionId);

    Task MoveTemplateInstructionAsync(string templateId, string templateInstructionId, int direction);
}
