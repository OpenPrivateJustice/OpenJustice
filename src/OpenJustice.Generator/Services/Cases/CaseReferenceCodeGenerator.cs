using Microsoft.EntityFrameworkCore;
using OpenJustice.Generator.Infrastructure.Persistence;

namespace OpenJustice.Generator.Services.Cases;

/// <summary>
/// Service for generating deterministic ATRO-YYYY-NNNN reference codes.
/// </summary>
public interface ICaseReferenceCodeGenerator
{
    /// <summary>
    /// Generates a unique ATRO-YYYY-NNNN reference code for a new case.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A unique reference code in format ATRO-YYYY-NNNN.</returns>
    Task<string> GenerateAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of reference code generator using database sequence.
/// </summary>
public class CaseReferenceCodeGenerator : ICaseReferenceCodeGenerator
{
    private readonly AppDbContext _context;
    private const string Prefix = "ATRO-";

    public CaseReferenceCodeGenerator(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        
        // Get the count of cases created this year to generate sequential number
        var casesThisYear = await _context.Cases
            .CountAsync(c => c.RegistrationDate.Year == year, cancellationToken);
        
        // Generate next sequence number (1-indexed)
        var sequenceNumber = casesThisYear + 1;
        
        // Format: ATRO-YYYY-NNNN (NNN is 3-digit zero-padded)
        var referenceCode = $"{Prefix}{year}-{sequenceNumber:D4}";
        
        // Ensure uniqueness (edge case: race condition)
        var isUnique = await _context.Cases
            .AllAsync(c => c.ReferenceCode != referenceCode, cancellationToken);
        
        if (!isUnique)
        {
            // Find the next available number
            var maxSequence = await _context.Cases
                .Where(c => c.RegistrationDate.Year == year)
                .Select(c => int.Parse(c.ReferenceCode.Split('-').Last()))
                .MaxAsync(cancellationToken);
            
            sequenceNumber = maxSequence + 1;
            referenceCode = $"{Prefix}{year}-{sequenceNumber:D4}";
        }
        
        return referenceCode;
    }
}
