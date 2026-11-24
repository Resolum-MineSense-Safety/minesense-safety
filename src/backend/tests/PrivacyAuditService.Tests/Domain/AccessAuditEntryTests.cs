using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Domain.Model.Commands;
using PrivacyAuditService.Domain.Model.ValueObjects;
using PrivacyAuditService.Domain.Services;
using MineSenseSafety.Shared.Domain.Model;

namespace PrivacyAuditService.Tests.Domain;

public class AccessAuditEntryTests
{
    private static RegisterAccessAttemptCommand AttemptCommand(
        AccessDecision decision = AccessDecision.Granted) =>
        new(Guid.NewGuid(), "FatigueAssessment", Guid.NewGuid(),
            ["perclos", "riskLevel"], "shift-supervision", decision);

    [Fact]
    public void Constructor_WithGrantedDecision_SealsGenesisHash()
    {
        // Arrange
        var command = AttemptCommand();

        // Act
        var entry = new AccessAuditEntry(command, DateTimeOffset.UtcNow, AccessAuditHashChain.GenesisPrevHash);

        // Assert
        Assert.Equal(AccessDecision.Granted, entry.Decision);
        Assert.Equal(AccessAuditHashChain.GenesisPrevHash, entry.PrevHash);
        Assert.False(string.IsNullOrWhiteSpace(entry.Hash));
        Assert.True(AccessAuditHashChain.Verify([entry]));
    }

    [Fact]
    public void Constructor_WithDeniedDecision_RecordsDenial()
    {
        // Arrange
        var command = AttemptCommand(AccessDecision.Denied);

        // Act
        var entry = new AccessAuditEntry(command, DateTimeOffset.UtcNow, AccessAuditHashChain.GenesisPrevHash);

        // Assert
        Assert.Equal(AccessDecision.Denied, entry.Decision);
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ThrowsDomainException()
    {
        // Arrange
        var command = new RegisterAccessAttemptCommand(Guid.Empty, "FatigueAssessment",
            Guid.NewGuid(), ["riskLevel"], "shift-supervision", AccessDecision.Granted);

        // Act
        Action act = () => new AccessAuditEntry(command, DateTimeOffset.UtcNow, AccessAuditHashChain.GenesisPrevHash);

        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Verify_WithBrokenLink_ReturnsFalse()
    {
        // Arrange: two chained entries, then tamper the stored hash of the second
        var first = new AccessAuditEntry(AttemptCommand(), new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
            AccessAuditHashChain.GenesisPrevHash);
        var second = new AccessAuditEntry(AttemptCommand(), new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero),
            first.Hash);
        var tampered = new AccessAuditEntry(AttemptCommand(), new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero),
            "TAMPERED");

        // Act & Assert
        Assert.True(AccessAuditHashChain.Verify([first, second]));
        Assert.False(AccessAuditHashChain.Verify([first, tampered]));
    }
}
