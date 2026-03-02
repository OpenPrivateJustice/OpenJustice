using FluentValidation;
using OpenJustice.Generator.Contracts.Cases;

namespace OpenJustice.Generator.Validation.Cases;

/// <summary>
/// Validator for CreateCaseRequest - enforces required fields and consistency rules.
/// </summary>
public class CreateCaseRequestValidator : AbstractValidator<CreateCaseRequest>
{
    public CreateCaseRequestValidator()
    {
        // Required field: CrimeTypeId must be positive
        RuleFor(x => x.CrimeTypeId)
            .GreaterThan(0)
            .WithMessage("CrimeTypeId is required.");

        // Required field: CaseTypeId must be positive
        RuleFor(x => x.CaseTypeId)
            .GreaterThan(0)
            .WithMessage("CaseTypeId is required.");

        // Required field: JudicialStatusId must be positive
        RuleFor(x => x.JudicialStatusId)
            .GreaterThan(0)
            .WithMessage("JudicialStatusId is required.");

        // Confidence scores must be between 0 and 100
        RuleFor(x => x.VictimConfidence)
            .InclusiveBetween(0, 100)
            .WithMessage("VictimConfidence must be between 0 and 100.");

        RuleFor(x => x.AccusedConfidence)
            .InclusiveBetween(0, 100)
            .WithMessage("AccusedConfidence must be between 0 and 100.");

        RuleFor(x => x.CrimeConfidence)
            .InclusiveBetween(0, 100)
            .WithMessage("CrimeConfidence must be between 0 and 100.");

        RuleFor(x => x.JudicialConfidence)
            .InclusiveBetween(0, 100)
            .WithMessage("JudicialConfidence must be between 0 and 100.");

        // Number of victims/accused must be positive
        RuleFor(x => x.NumberOfVictims)
            .GreaterThan(0)
            .WithMessage("NumberOfVictims must be at least 1.");

        RuleFor(x => x.NumberOfAccused)
            .GreaterThan(0)
            .WithMessage("NumberOfAccused must be at least 1.");

        // Age validation (if provided)
        RuleFor(x => x.VictimAge)
            .InclusiveBetween(0, 150)
            .When(x => x.VictimAge.HasValue)
            .WithMessage("VictimAge must be between 0 and 150.");

        RuleFor(x => x.AccusedAge)
            .InclusiveBetween(0, 150)
            .When(x => x.AccusedAge.HasValue)
            .WithMessage("AccusedAge must be between 0 and 150.");

        // Date consistency: CrimeDate cannot be in the future
        RuleFor(x => x.CrimeDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.CrimeDate.HasValue)
            .WithMessage("CrimeDate cannot be in the future.");

        // Date consistency: ReportDate cannot be in the future
        RuleFor(x => x.ReportDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.ReportDate.HasValue)
            .WithMessage("ReportDate cannot be in the future.");

        // Date consistency: EstimatedCrimeDateTime cannot be in the future
        RuleFor(x => x.EstimatedCrimeDateTime)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.EstimatedCrimeDateTime.HasValue)
            .WithMessage("EstimatedCrimeDateTime cannot be in the future.");

        // Date consistency: SentencingDate cannot be in the future
        RuleFor(x => x.SentencingDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.SentencingDate.HasValue)
            .WithMessage("SentencingDate cannot be in the future.");

        // Date consistency: JudicialReportDate cannot be in the future
        RuleFor(x => x.JudicialReportDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.JudicialReportDate.HasValue)
            .WithMessage("JudicialReportDate cannot be in the future.");

        // Chronology: If CrimeDate is provided, it should be before or equal to ReportDate
        RuleFor(x => x.CrimeDate)
            .LessThanOrEqualTo(x => x.ReportDate)
            .When(x => x.CrimeDate.HasValue && x.ReportDate.HasValue)
            .WithMessage("CrimeDate must be before or equal to ReportDate.");

        // Chronology: If CrimeDate is provided, it should be before or equal to RegistrationDate
        RuleFor(x => x.CrimeDate)
            .LessThanOrEqualTo(DateTime.UtcNow)
            .When(x => x.CrimeDate.HasValue)
            .WithMessage("CrimeDate cannot be in the future.");

        // Length validations for string fields
        RuleFor(x => x.VictimName)
            .MaximumLength(200)
            .WithMessage("VictimName cannot exceed 200 characters.");

        RuleFor(x => x.AccusedName)
            .MaximumLength(200)
            .WithMessage("AccusedName cannot exceed 200 characters.");

        RuleFor(x => x.CrimeDescription)
            .MaximumLength(5000)
            .WithMessage("CrimeDescription cannot exceed 5000 characters.");

        RuleFor(x => x.CrimeLocationCity)
            .MaximumLength(100)
            .WithMessage("CrimeLocationCity cannot exceed 100 characters.");

        RuleFor(x => x.CrimeLocationState)
            .MaximumLength(2)
            .WithMessage("CrimeLocationState cannot exceed 2 characters (UF).");

        RuleFor(x => x.CrimeLocationAddress)
            .MaximumLength(500)
            .WithMessage("CrimeLocationAddress cannot exceed 500 characters.");

        RuleFor(x => x.ProcessNumber)
            .MaximumLength(50)
            .WithMessage("ProcessNumber cannot exceed 50 characters.");

        RuleFor(x => x.Court)
            .MaximumLength(200)
            .WithMessage("Court cannot exceed 200 characters.");

        RuleFor(x => x.County)
            .MaximumLength(200)
            .WithMessage("County cannot exceed 200 characters.");

        // Gender validation (if provided) - Brazilian standard
        RuleFor(x => x.VictimGender)
            .Must(BeAValidGender)
            .When(x => !string.IsNullOrEmpty(x.VictimGender))
            .WithMessage("VictimGender must be one of: M, F, Other, Unknown.");

        RuleFor(x => x.AccusedGender)
            .Must(BeAValidGender)
            .When(x => !string.IsNullOrEmpty(x.AccusedGender))
            .WithMessage("AccusedGender must be one of: M, F, Other, Unknown.");
    }

    private bool BeAValidGender(string? gender)
    {
        if (string.IsNullOrEmpty(gender))
            return true;
        
        var validGenders = new[] { "M", "F", "Other", "Unknown" };
        return validGenders.Contains(gender);
    }
}
